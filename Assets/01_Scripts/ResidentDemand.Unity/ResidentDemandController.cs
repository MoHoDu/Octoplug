using System;
using System.Collections.Generic;
using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class ResidentDemandController : MonoBehaviour
    {
        [SerializeField]
        private ProductionRoomGenerationController roomGeneration;

        [SerializeField]
        private int randomSeed = 1;

        [SerializeField]
        [Min(0)]
        private int initialResidentCount = 1;

        private readonly ResidentNumberSequence _residentNumbers = new();
        private readonly List<ResidentDemandState> _residents = new();
        private readonly List<ApplianceSource> _products = new();
        private readonly Dictionary<int, long> _requestSequenceByResident = new();
        private WeightedDemandSelector _demandSelector;
        private ProductAssignmentPlan _currentAssignments =
            new ProductAssignmentPlan(
                Array.Empty<ResidentProductAssignment>(),
                Array.Empty<ResidentAssignmentRequest>());
        private IReadOnlyList<ProductUsageSnapshot> _productUsage =
            Array.Empty<ProductUsageSnapshot>();
        private bool _initialized;
        private int? _verificationRoomCount;
        private long _nextRequestSequence;

        public event Action ResidentsChanged;
        public event Action AssignmentsChanged;
        public event Action<DemandOutcome> DemandResolved;

        public IReadOnlyList<ResidentDemandState> Residents => _residents;
        public ProductAssignmentPlan CurrentAssignments => _currentAssignments;
        public IReadOnlyList<ProductUsageSnapshot> ProductUsage => _productUsage;
        public ResidentNeedType AvailableNeedTypes { get; private set; }
        public int UnlockedRoomCount => GetUnlockedRoomCount();

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (!_initialized)
            {
                if (roomGeneration != null && roomGeneration.IsInitialized)
                {
                    Initialize();
                }

                return;
            }

            var changed = false;
            var progressed = false;
            for (var index = 0; index < _residents.Count; index++)
            {
                var resident = _residents[index];
                if (resident.Status == ResidentDemandStatus.None)
                {
                    continue;
                }

                var previousNeedIndex = resident.ActiveNeedIndex;
                if (resident.Status == ResidentDemandStatus.Waiting ||
                    resident.Status == ResidentDemandStatus.Using)
                {
                    progressed = true;
                }

                var wasCooldown = resident.Status == ResidentDemandStatus.Cooldown;
                var outcome = resident.Advance(Time.deltaTime);
                if (outcome != null)
                {
                    _requestSequenceByResident.Remove(resident.ResidentNumber.Value);
                    DemandResolved?.Invoke(outcome);
                    changed = true;
                }
                else if (resident.Status == ResidentDemandStatus.Waiting &&
                         resident.ActiveNeedIndex != previousNeedIndex)
                {
                    _requestSequenceByResident[resident.ResidentNumber.Value] =
                        _nextRequestSequence++;
                    changed = true;
                }
                else if (wasCooldown && resident.Status == ResidentDemandStatus.None)
                {
                    if (TryStartDemand(resident))
                    {
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                RecomputeAssignments();
            }

            if (changed || progressed)
            {
                ResidentsChanged?.Invoke();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeProducts();
            PlugSocketConnection.GraphChanged -= HandleAvailabilityChanged;
            if (roomGeneration != null)
            {
                roomGeneration.RoomContentReady -= HandleRoomContentReady;
            }
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (roomGeneration == null)
            {
                throw new InvalidOperationException("Resident Demand requires the production Room Generation controller.");
            }

            if (!roomGeneration.IsInitialized)
            {
                return;
            }

            _demandSelector = new WeightedDemandSelector(
                Octoplug.Balance.DemandBalanceRegistryMapper.MapAll(),
                new SystemRandomSource(randomSeed));
            _initialized = true;
            RefreshProducts();
            PlugSocketConnection.GraphChanged += HandleAvailabilityChanged;
            roomGeneration.RoomContentReady += HandleRoomContentReady;

            for (var index = 0; index < initialResidentCount; index++)
            {
                AddResident();
            }
        }

        public void InitializeForVerification(int unlockedRoomCount)
        {
            if (unlockedRoomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(unlockedRoomCount));
            }

            if (_initialized)
            {
                throw new InvalidOperationException("Resident Demand is already initialized.");
            }

            _verificationRoomCount = unlockedRoomCount;
            _demandSelector = new WeightedDemandSelector(
                Octoplug.Balance.DemandBalanceRegistryMapper.MapAll(),
                new SystemRandomSource(randomSeed));
            _initialized = true;
            RefreshProducts();
            PlugSocketConnection.GraphChanged += HandleAvailabilityChanged;
        }

        public void RecomputeAssignmentsForVerification()
        {
            RecomputeAssignments();
        }

        public ResidentDemandState AddResident()
        {
            EnsureInitialized();
            var resident = new ResidentDemandState(_residentNumbers.Next());
            _residents.Add(resident);
            TryStartDemand(resident);
            RecomputeAssignments();
            ResidentsChanged?.Invoke();
            return resident;
        }

        public void RefreshDemandAvailability()
        {
            EnsureInitialized();
            RefreshProducts();
            var changed = TryStartIdleDemands();
            RecomputeAssignments();
            if (changed)
            {
                ResidentsChanged?.Invoke();
            }
        }

        [ContextMenu("Debug/Add Resident")]
        public void AddResidentFromInspector()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Residents can be added only in Play Mode.", this);
                return;
            }

            AddResident();
        }

        [ContextMenu("Debug/Refresh Demand Availability")]
        public void RefreshDemandAvailabilityFromInspector()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Demand availability can be refreshed only in Play Mode.", this);
                return;
            }

            RefreshDemandAvailability();
        }

        [ContextMenu("Debug/Inspect Demand State")]
        public void InspectDemandStateFromInspector()
        {
            if (!Application.isPlaying || !_initialized)
            {
                Debug.LogWarning("Demand state is available only after Play Mode initialization.", this);
                return;
            }

            Debug.Log(
                $"Resident Demand: rooms={GetUnlockedRoomCount()}, available={AvailableNeedTypes}, " +
                $"residents={_residents.Count}.",
                this);
            foreach (var resident in _residents)
            {
                var needs = new List<string>();
                for (var index = 0; index < resident.NeedCount; index++)
                {
                    needs.Add(resident.GetNeed(index).ToString());
                }

                Debug.Log(
                    $"{resident.ResidentNumber.DisplayValue}: status={resident.Status}, " +
                    $"needs=[{string.Join(", ", needs)}].",
                    this);
            }
        }

        public ResidentDemandState AddResidentForVerification(
            DemandBalanceRecord demand)
        {
            EnsureInitialized();
            return AddResident(demand);
        }

        private ResidentDemandState AddResident(DemandBalanceRecord demand)
        {
            var resident = new ResidentDemandState(_residentNumbers.Next());
            resident.StartDemand(demand);
            _requestSequenceByResident[resident.ResidentNumber.Value] =
                _nextRequestSequence++;
            _residents.Add(resident);
            RecomputeAssignments();
            ResidentsChanged?.Invoke();
            return resident;
        }

        private void HandleRoomContentReady(Octoplug.RoomGeneration.RoomPlacement placement)
        {
            RefreshProducts();
            var changed = TryStartIdleDemands();
            RecomputeAssignments();
            if (changed)
            {
                ResidentsChanged?.Invoke();
            }
        }

        private void HandleAvailabilityChanged()
        {
            RefreshProducts();
            RecomputeAssignments();
        }

        private void HandlePoweredChanged(ApplianceSource product, bool powered)
        {
            RecomputeAssignments();
        }

        private void RefreshProducts()
        {
            UnsubscribeProducts();
#if UNITY_2023_1_OR_NEWER
            var found = FindObjectsByType<ApplianceSource>(FindObjectsSortMode.None);
#else
            var found = FindObjectsOfType<ApplianceSource>();
#endif
            AvailableNeedTypes = ResidentNeedType.None;
            foreach (var product in found)
            {
                if (!IsGameplayProduct(product))
                {
                    continue;
                }

                _products.Add(product);
                AvailableNeedTypes |= UsageTypeMapper.Map(product.UsageTypes);
            }

            _products.Sort(CompareHierarchyOrder);
            foreach (var product in _products)
            {
                product.PoweredChanged += HandlePoweredChanged;
            }
        }

        private bool IsGameplayProduct(ApplianceSource product)
        {
            if (product == null || !product.gameObject.activeInHierarchy)
            {
                return false;
            }

            var roomArea = product.GetComponentInParent<RoomArea>();
            if (roomArea != null)
            {
                return roomArea.IsGameplayEnabled;
            }

            if (_verificationRoomCount.HasValue)
            {
                return true;
            }

#if UNITY_2023_1_OR_NEWER
            var roomAreas = FindObjectsByType<RoomArea>(FindObjectsSortMode.None);
#else
            var roomAreas = FindObjectsOfType<RoomArea>();
#endif
            foreach (var candidate in roomAreas)
            {
                if (candidate.IsGameplayEnabled &&
                    candidate.FloorArea != null &&
                    candidate.FloorArea.OverlapPoint(product.transform.position))
                {
                    return true;
                }
            }

            return false;
        }

        private void UnsubscribeProducts()
        {
            foreach (var product in _products)
            {
                if (product != null)
                {
                    product.PoweredChanged -= HandlePoweredChanged;
                }
            }

            _products.Clear();
        }

        private void RecomputeAssignments()
        {
            if (!_initialized)
            {
                return;
            }

            var products = new ProductAvailability[_products.Count];
            for (var index = 0; index < _products.Count; index++)
            {
                var product = _products[index];
                products[index] = new ProductAvailability(
                    BuildStableHierarchyId(product.transform),
                    UsageTypeMapper.Map(product.UsageTypes),
                    product.Capacity,
                    product.IsPowered && product.IsConnected);
            }

            var requests = new List<ResidentAssignmentRequest>();
            foreach (var resident in _residents)
            {
                if (resident.Status != ResidentDemandStatus.Waiting &&
                    resident.Status != ResidentDemandStatus.Using)
                {
                    continue;
                }

                if (!_requestSequenceByResident.TryGetValue(
                        resident.ResidentNumber.Value,
                        out var sequence))
                {
                    sequence = _nextRequestSequence++;
                    _requestSequenceByResident.Add(
                        resident.ResidentNumber.Value,
                        sequence);
                }

                requests.Add(new ResidentAssignmentRequest(
                    resident.ResidentNumber,
                    resident.ActiveNeedIndex,
                    resident.GetNeed(resident.ActiveNeedIndex),
                    sequence));
            }

            _currentAssignments =
                DeterministicAssignmentPlanner.Compute(products, requests);
            foreach (var resident in _residents)
            {
                resident.SetUsing(false);
            }

            foreach (var assignment in _currentAssignments.Assignments)
            {
                FindResident(assignment.Request.ResidentNumber).SetUsing(true);
            }

            _productUsage = BuildProductUsage();
            AssignmentsChanged?.Invoke();
        }

        private IReadOnlyList<ProductUsageSnapshot> BuildProductUsage()
        {
            var activeByProduct = new Dictionary<string, List<ResidentNumber>>(StringComparer.Ordinal);
            var waitingByProduct = new Dictionary<string, List<ResidentNumber>>(StringComparer.Ordinal);
            foreach (var assignment in _currentAssignments.Assignments)
            {
                AddResident(activeByProduct, assignment.ProductId, assignment.Request.ResidentNumber);
            }

            foreach (var request in _currentAssignments.Waiting)
            {
                var matchingProduct = FindFirstMatchingProduct(request.NeedType);
                if (matchingProduct != null)
                {
                    AddResident(
                        waitingByProduct,
                        BuildStableHierarchyId(matchingProduct.transform),
                        request.ResidentNumber);
                }
            }

            var snapshots = new List<ProductUsageSnapshot>(_products.Count);
            foreach (var product in _products)
            {
                var productId = BuildStableHierarchyId(product.transform);
                activeByProduct.TryGetValue(productId, out var activeResidents);
                waitingByProduct.TryGetValue(productId, out var waitingResidents);
                snapshots.Add(new ProductUsageSnapshot(
                    productId,
                    product,
                    activeResidents ?? new List<ResidentNumber>(),
                    waitingResidents ?? new List<ResidentNumber>()));
            }

            return snapshots;
        }

        private ApplianceSource FindFirstMatchingProduct(ResidentNeedType needType)
        {
            foreach (var product in _products)
            {
                if (product.IsConnected &&
                    product.IsPowered &&
                    (UsageTypeMapper.Map(product.UsageTypes) & needType) != 0)
                {
                    return product;
                }
            }

            return null;
        }

        private static void AddResident(
            IDictionary<string, List<ResidentNumber>> residentsByProduct,
            string productId,
            ResidentNumber residentNumber)
        {
            if (!residentsByProduct.TryGetValue(productId, out var residents))
            {
                residents = new List<ResidentNumber>();
                residentsByProduct.Add(productId, residents);
            }

            residents.Add(residentNumber);
        }

        private bool TryStartDemand(ResidentDemandState resident)
        {
            if (!_demandSelector.TrySelect(
                    GetUnlockedRoomCount(),
                    AvailableNeedTypes,
                    out var demand))
            {
                return false;
            }

            resident.StartDemand(demand);
            _requestSequenceByResident[resident.ResidentNumber.Value] =
                _nextRequestSequence++;
            return true;
        }

        private bool TryStartIdleDemands()
        {
            var changed = false;
            foreach (var resident in _residents)
            {
                if (resident.Status == ResidentDemandStatus.None &&
                    TryStartDemand(resident))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private ResidentDemandState FindResident(ResidentNumber number)
        {
            foreach (var resident in _residents)
            {
                if (resident.ResidentNumber.Equals(number))
                {
                    return resident;
                }
            }

            throw new InvalidOperationException($"Resident {number} is not registered.");
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                throw new InvalidOperationException(
                    "Resident Demand must be initialized before adding residents.");
            }
        }

        private int GetUnlockedRoomCount()
        {
            if (_verificationRoomCount.HasValue)
            {
                return _verificationRoomCount.Value;
            }

            if (!roomGeneration.IsInitialized)
            {
                throw new InvalidOperationException(
                    "Room Generation must initialize before Resident Demand.");
            }

            return roomGeneration.State.UnlockedLayout.Rooms.Count;
        }

        private static int CompareHierarchyOrder(ApplianceSource left, ApplianceSource right)
        {
            return string.CompareOrdinal(
                BuildStableHierarchyId(left.transform),
                BuildStableHierarchyId(right.transform));
        }

        private static string BuildStableHierarchyId(Transform target)
        {
            var indices = new Stack<int>();
            for (var current = target; current != null; current = current.parent)
            {
                indices.Push(current.GetSiblingIndex());
            }

            var path = new List<string>(indices.Count);
            while (indices.Count > 0)
            {
                path.Add(indices.Pop().ToString("D8"));
            }

            return $"{target.gameObject.scene.path}:{string.Join(".", path)}";
        }
    }
}
