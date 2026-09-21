using System;
using System.Collections.Generic;
using System.Linq;
using Octoplug.Balance;
using Octoplug.Power;
using Octoplug.Power.Grid;
using Octoplug.Power.Routing;
using UnityEngine;

namespace Octoplug.RoomGeneration.Unity
{
    public class RoomContentGenerationController : MonoBehaviour
    {
        private const int RequiredWallOutletCount = 1;

        [SerializeField] private HousePowerBudget housePowerBudget;
        [SerializeField] private ProductionRoomGenerationController roomGeneration;
        [SerializeField] private ApplianceSource tvPrefab;
        [SerializeField] private ApplianceSource fanPrefab;
        [SerializeField] private ApplianceSource heaterPrefab;
        [SerializeField] private ApplianceSource inductionPrefab;
        [SerializeField] private ApplianceSource airConditionerPrefab;
        [SerializeField] private WallOutlet wallOutletPrefab;

        [Header("Debug")]
        [SerializeField] private string lastSelectedRoomConfigId;
        [SerializeField] private int generatedProductCount;
        [SerializeField] private int generatedWallOutletCount;
        [SerializeField] private string generatedWallOutletWalls;
        [SerializeField] private string wallOutletPlacementWarning;

        private void EnsureHousePowerBudget()
        {
            if (housePowerBudget == null)
            {
#if UNITY_6000_0_OR_NEWER
                housePowerBudget = UnityEngine.Object.FindAnyObjectByType<HousePowerBudget>();
#else
                housePowerBudget = FindObjectOfType<HousePowerBudget>();
#endif
            }
        }

        private float GetHouseAllowedPower()
        {
            EnsureHousePowerBudget();
            return housePowerBudget != null ? housePowerBudget.AllowedPowerWatts : 10f;
        }

        public void RedistributeProducts()
        {
            var archive = BalanceRegistry.Instance;
            if (archive == null) return;

            int roomCount = roomGeneration.State.UnlockedLayout.Rooms.Count;
            if (roomCount < 4) return;

            var matchingRules = archive.ProductRedistributionRows
                .Where(r => roomCount >= r.minRoomCount && roomCount <= r.maxRoomCount)
                .ToList();
            if (matchingRules.Count != 1) return;

            var rule = matchingRules[0];
            int extraProducts = UnityEngine.Random.Range(rule.minExtraProducts, rule.maxExtraProducts + 1);
            if (extraProducts <= 0) return;

            float housePower = GetHouseAllowedPower();
            var validPool = archive.ProductSpawnPoolRows
                .Where(p => p.enabled && p.weight > 0 && p.minRoomCount <= roomCount && IsProductPowerFeasible(p.productType, housePower))
                .ToList();

            if (validPool.Count == 0) return;

            var gridService = CableRoutingGridService.Instance;
            if (gridService == null) return;
            var grid = gridService.Grid;
            var targetRooms = SelectTargetRooms(roomGeneration.State.UnlockedLayout.Rooms, extraProducts);
            int placed = 0;

            for (int i = 0; i < targetRooms.Count; i++)
            {
                var targetRoom = targetRooms[i];
                var roomPool = new List<ProductSpawnPoolSheetRow>(validPool);
                while (roomPool.Count > 0)
                {
                    int selectedIndex = SelectProductIndexFromPool(
                        roomPool,
                        UnityEngine.Random.Range(0, roomPool.Sum(p => p.weight)));
                    var selected = roomPool[selectedIndex];
                    var prefab = GetPrefab(selected.productType);
                    if (prefab != null
                        && TryPlaceProduct(
                            prefab,
                            targetRoom,
                            grid,
                            generatedObjects: null,
                            out _))
                    {
                        placed++;
                        break;
                    }

                    roomPool.RemoveAt(selectedIndex);
                }

                if (roomPool.Count == 0)
                {
                    Debug.LogWarning($"[Redistribution] No eligible Product could be placed in Room {targetRoom.Id}.");
                }
            }

            if (targetRooms.Count < extraProducts)
            {
                Debug.LogWarning($"[Redistribution] Requested {extraProducts} distinct Rooms, but only {targetRooms.Count} were available.");
            }

            if (placed < extraProducts)
            {
                Debug.LogWarning($"[Redistribution] Requested {extraProducts}, Placed {placed}.");
            }
        }

        public static List<RoomPlacement> SelectTargetRooms(
            IReadOnlyList<RoomPlacement> rooms,
            int count,
            Func<RoomId, int> productCount = null)
        {
            if (rooms == null || count <= 0) return new List<RoomPlacement>();
            productCount ??= CountActiveProductsInRoom;
            return rooms
                .OrderBy(room => productCount(room.Id))
                .ThenByDescending(RoomArea)
                .ThenBy(room => room.Id)
                .Take(count)
                .ToList();
        }

        private static float RoomArea(RoomPlacement room) =>
            room.Bounds.Width * room.Bounds.Height;

        private static int CountActiveProductsInRoom(RoomId roomId)
        {
            int count = 0;
            foreach (var product in RuntimeWorldRegistry.GetProducts())
            {
                if (RuntimeWorldRegistry.TryGetRoomOwner(product, out var owner) && owner == roomId)
                {
                    count++;
                }
            }
            return count;
        }

        public static int SelectProductIndexFromPool(
            IReadOnlyList<ProductSpawnPoolSheetRow> pool,
            int roll)
        {
            if (pool == null || pool.Count == 0)
            {
                throw new ArgumentException("Product pool must not be empty.", nameof(pool));
            }

            int totalWeight = pool.Sum(p => p.weight);
            if (roll < 0 || roll >= totalWeight)
            {
                throw new ArgumentOutOfRangeException(nameof(roll));
            }

            int current = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                current += pool[i].weight;
                if (roll < current) return i;
            }

            throw new InvalidOperationException("Product pool weights are invalid.");
        }

        private ApplianceSource GetPrefab(string type)
        {
            type = type.ToLower();
            if (type.Contains("tv")) return tvPrefab;
            if (type.Contains("fan")) return fanPrefab;
            if (type.Contains("heater")) return heaterPrefab;
            if (type.Contains("induction")) return inductionPrefab;
            if (type.Contains("air")) return airConditionerPrefab;
            return null;
        }

        private bool IsProductPowerFeasible(string type, float housePower)
        {
            var prefab = GetPrefab(type);
            if (prefab == null) return false;
            return prefab.PowerConsumptionWatts <= housePower;
        }

        private bool TryPlaceProduct(
            ApplianceSource prefab,
            RoomPlacement room,
            CableRoutingGrid grid,
            ICollection<GameObject> generatedObjects,
            out string failure)
        {
            var productParent = GameObject.Find("Products")?.transform;
            var instance = RuntimeEquipmentFactory.Stage(
                prefab.gameObject,
                Vector2.zero,
                Quaternion.identity,
                productParent);
            var footprint = instance != null
                ? instance.GetComponent<PlacementFootprint>()
                : null;
            var product = instance != null
                ? instance.GetComponent<ApplianceSource>()
                : null;
            var candidateFailure = "no placement candidate was accepted";

            bool found = RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                footprint,
                socketCount: 0,
                (candidate, bounds) =>
                {
                    instance.transform.position = candidate;
                    Physics2D.SyncTransforms();
                    if (OverlapsPlacementObject(footprint, bounds))
                    {
                        candidateFailure =
                            "all otherwise valid positions overlap a Product or PowerStrip";
                        return false;
                    }

                    if (!CanReachWallOutlet(product, room, grid))
                    {
                        candidateFailure =
                            "no usable room Wall Outlet socket is reachable within the Product's initial CableLength";
                        return false;
                    }

                    return true;
                },
                out var spawnPosition,
                out failure);

            if (!found)
            {
                if (!string.IsNullOrEmpty(candidateFailure))
                {
                    failure = candidateFailure;
                }

                RuntimeEquipmentFactory.Abort(instance);
                return false;
            }

            instance.transform.position = spawnPosition;
            if (!RuntimeEquipmentFactory.TryFinalizeProduct(
                    instance,
                    room.Id,
                    grid,
                    out _,
                    out failure))
            {
                return false;
            }

            generatedObjects?.Add(instance);
            return true;
        }

        public bool GenerateRoomContent(RoomPlacement room)
        {
            const int requiredProductCount = 1;
            var roomCount = roomGeneration.State.UnlockedLayout.Rooms.Count;
            int wallOutletSocketMin = 1;
            int wallOutletSocketMax = 2;
            string configId;
            ApplianceSource starterProduct = null;
            bool isStarterRoom = roomCount <= 2;

            if (isStarterRoom)
            {
                var starter = Octoplug.RoomGeneration.RoomContentBalanceMapper
                    .GetStarterConfigs()
                    .FirstOrDefault(s =>
                        s.StarterRoomIndex == roomCount && s.Enabled);
                if (starter == null)
                {
                    Debug.LogWarning(
                        $"[RoomContent] No enabled StarterConfig found for Room {roomCount}.",
                        this);
                    return false;
                }

                starterProduct = GetStarterProductPrefab(starter);
                wallOutletSocketMin = starter.WallOutletSocketMin;
                wallOutletSocketMax = starter.WallOutletSocketMax;
                configId = "starter-" + starter.StarterRoomIndex;
            }
            else
            {
                var configs = Octoplug.RoomGeneration.RoomContentBalanceMapper
                    .GetDefaultConfigs()
                    .Where(c => c.Enabled && c.MinRoomCount <= roomCount)
                    .ToList();
                if (configs.Count == 0)
                {
                    Debug.LogWarning(
                        $"[RoomContent] No eligible RoomConfig found for Room {room.Id} "
                        + $"(RoomCount: {roomCount}).",
                        this);
                    return false;
                }

                int totalWeight = configs.Sum(c => c.Weight);
                int roll = UnityEngine.Random.Range(0, totalWeight);
                RoomConfigRecord selectedConfig = configs[0];
                int currentWeight = 0;
                foreach (var config in configs)
                {
                    currentWeight += config.Weight;
                    if (roll < currentWeight)
                    {
                        selectedConfig = config;
                        break;
                    }
                }

                wallOutletSocketMin = selectedConfig.WallOutletSocketMin;
                wallOutletSocketMax = selectedConfig.WallOutletSocketMax;
                configId = selectedConfig.Id;
            }

            var gridService = CableRoutingGridService.Instance;
            if (gridService == null)
            {
                return false;
            }

            var grid = gridService.Grid;
            generatedProductCount = 0;
            generatedWallOutletCount = 0;
            generatedWallOutletWalls = string.Empty;
            wallOutletPlacementWarning = string.Empty;
            lastSelectedRoomConfigId = configId;

            var generatedObjects = new List<GameObject>();
            generatedWallOutletCount = SpawnWallOutlets(
                RequiredWallOutletCount,
                wallOutletSocketMin,
                wallOutletSocketMax,
                room,
                generatedObjects);
            if (generatedWallOutletCount != RequiredWallOutletCount)
            {
                if (string.IsNullOrEmpty(wallOutletPlacementWarning))
                {
                    wallOutletPlacementWarning =
                        "required Wall Outlet was not finalized";
                }

                Debug.LogWarning(
                    $"[RoomContentRequiredInfrastructure]\nRoom: {room.Id}\nConfig: {configId}"
                    + $"\nRequested: {RequiredWallOutletCount}\nPlaced: {generatedWallOutletCount}"
                    + $"\nReason: {wallOutletPlacementWarning}",
                    this);
                RollBackRoomContent(generatedObjects);
                return false;
            }

            string productFailure = "required Product was not finalized";
            if (isStarterRoom && starterProduct == null)
            {
                productFailure =
                    "StarterConfig must define exactly one supported Product";
            }
            else if (starterProduct != null)
            {
                if (starterProduct.PowerConsumptionWatts <= GetHouseAllowedPower()
                    && TryPlaceProduct(
                        starterProduct,
                        room,
                        grid,
                        generatedObjects,
                        out productFailure))
                {
                    generatedProductCount = 1;
                }
                else if (starterProduct.PowerConsumptionWatts > GetHouseAllowedPower())
                {
                    productFailure =
                        "Starter Product exceeds the current House power budget";
                }
            }
            else
            {
                generatedProductCount = TryPlaceBaseProductFromPool(
                    room,
                    roomCount,
                    grid,
                    generatedObjects,
                    out productFailure)
                    ? 1
                    : 0;
            }

            if (generatedProductCount != requiredProductCount)
            {
                Debug.LogWarning(
                    $"[RoomContentProductShortfall]\nRoom: {room.Id}\nConfig: {configId}"
                    + $"\nRequested: {requiredProductCount}\nPlaced: {generatedProductCount}"
                    + $"\nReason: {productFailure}",
                    this);
                RollBackRoomContent(generatedObjects);
                return false;
            }

            Debug.Log(
                $"[RoomContent]\nRoom: {room.Id}\nConfig: {configId}"
                + $"\nProducts Generated: {generatedProductCount}"
                + $"\nWallOutlets Generated: {generatedWallOutletCount}"
                + $"\nOutlet Walls: {(string.IsNullOrEmpty(generatedWallOutletWalls) ? "None" : generatedWallOutletWalls)}",
                this);
            return true;
        }

        private ApplianceSource GetStarterProductPrefab(
            Octoplug.RoomGeneration.StarterRoomConfigRecord starter)
        {
            var products = new List<ApplianceSource>(5);
            AddStarterProduct(products, tvPrefab, starter.TvCount);
            AddStarterProduct(products, fanPrefab, starter.FanCount);
            AddStarterProduct(products, heaterPrefab, starter.HeaterCount);
            AddStarterProduct(products, inductionPrefab, starter.InductionCount);
            AddStarterProduct(
                products,
                airConditionerPrefab,
                starter.AirConditionerCount);
            return products.Count == 1 ? products[0] : null;
        }

        private static void AddStarterProduct(
            ICollection<ApplianceSource> products,
            ApplianceSource prefab,
            int count)
        {
            if (count == 1 && prefab != null)
            {
                products.Add(prefab);
            }
            else if (count != 0)
            {
                products.Add(null);
                products.Add(null);
            }
        }

        private bool TryPlaceBaseProductFromPool(
            RoomPlacement room,
            int roomCount,
            CableRoutingGrid grid,
            ICollection<GameObject> generatedObjects,
            out string failure)
        {
            var archive = BalanceRegistry.Instance;
            if (archive == null)
            {
                failure = "GameBalanceArchive is unavailable";
                return false;
            }

            float housePower = GetHouseAllowedPower();
            var pool = archive.ProductSpawnPoolRows
                .Where(p =>
                    p.enabled
                    && p.weight > 0
                    && p.minRoomCount <= roomCount
                    && IsProductPowerFeasible(p.productType, housePower))
                .ToList();
            if (pool.Count == 0)
            {
                failure = "no eligible Product exists in the Product Spawn Pool";
                return false;
            }

            failure = "no eligible Product could be placed";
            while (pool.Count > 0)
            {
                int selectedIndex = SelectProductIndexFromPool(
                    pool,
                    UnityEngine.Random.Range(0, pool.Sum(p => p.weight)));
                var selected = pool[selectedIndex];
                var prefab = GetPrefab(selected.productType);
                if (prefab != null
                    && TryPlaceProduct(
                        prefab,
                        room,
                        grid,
                        generatedObjects,
                        out failure))
                {
                    return true;
                }

                pool.RemoveAt(selectedIndex);
            }

            return false;
        }

        private int SpawnWallOutlets(int count, int minSockets, int maxSockets, RoomPlacement room, List<GameObject> generatedObjects)
        {
            if (wallOutletPrefab == null || count <= 0) return 0;
            if (!roomGeneration.TryGetRoomBinder(room.Id, out var roomBinder)) { wallOutletPlacementWarning = "room binder is unavailable"; return 0; }
            var wallParent = GameObject.Find("Wall_Outlets")?.transform;
            if (wallParent == null) { wallOutletPlacementWarning = "Wall_Outlets parent is unavailable"; return 0; }

            var outletBounds = CalculatePrefabBounds(wallOutletPrefab.gameObject);
            var outletGeometry = new WallOutletGeometry(wallOutletPrefab.transform.InverseTransformPoint(outletBounds.center), outletBounds.size);
            var placements = WallOutletPlacementPlanner.Plan(room, roomGeneration.State.UnlockedLayout.Doors, outletGeometry, roomBinder.SafetyMargin, count);
            var placedWalls = new List<string>(placements.Count);
            var socketSelector = new WeightedSocketCountSelector(Octoplug.RoomGeneration.RoomContentBalanceMapper.CreateWallOutletCatalog());
            var socketWeight = socketSelector.GetTotalWeight(minSockets, maxSockets);

            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var go = RuntimeEquipmentFactory.Stage(wallOutletPrefab.gameObject, placement.Position, Quaternion.Euler(0f, 0f, placement.RotationDegrees), wallParent);
                var socketCount = socketSelector.Select(minSockets, maxSockets, UnityEngine.Random.Range(0, socketWeight));
                if (!RuntimeEquipmentFactory.TryFinalizeWallOutlet(go, room.Id, socketCount, out var outlet, out var failure)) continue;
                generatedObjects.Add(go);
                placedWalls.Add(placement.Side.ToString());
            }

            generatedWallOutletWalls = string.Join(", ", placedWalls);
            if (placedWalls.Count < count) wallOutletPlacementWarning = "no remaining valid wall";
            return placedWalls.Count;
        }

        private static bool CanReachWallOutlet(ApplianceSource product, RoomPlacement room, CableRoutingGrid grid)
        {
            if (product == null || product.Cable == null || product.Cable.Origin == null) return false;
            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
            {
                if (outlet == null || !RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner) || owner != room.Id) continue;
                foreach (var socket in outlet.ActiveSockets)
                {
                    if (socket != null && !socket.IsConnected && CableRouteReachability.IsWithinLength(grid, product.Cable.Origin.position, socket, product.Cable.CableLength)) return true;
                }
            }
            return false;
        }

        private static bool OverlapsPlacementObject(PlacementFootprint candidate, Bounds candidateBounds)
        {
            const float tolerance = 0.0001f;
            foreach (var product in RuntimeWorldRegistry.GetProducts())
            {
                var other = product != null ? product.GetComponent<PlacementFootprint>() : null;
                if (other != null && other != candidate
                    && HasPositiveAreaOverlap(candidateBounds, other.WorldBounds, tolerance))
                {
                    return true;
                }
            }

            foreach (var powerStrip in RuntimeWorldRegistry.GetPowerStrips())
            {
                var other = powerStrip != null ? powerStrip.GetComponent<PlacementFootprint>() : null;
                if (other != null && other != candidate
                    && HasPositiveAreaOverlap(candidateBounds, other.WorldBounds, tolerance))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPositiveAreaOverlap(Bounds first, Bounds second, float tolerance)
        {
            var overlapX = Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x);
            var overlapY = Mathf.Min(first.max.y, second.max.y) - Mathf.Max(first.min.y, second.min.y);
            return overlapX > tolerance && overlapY > tolerance;
        }

        private void RollBackRoomContent(
            IReadOnlyList<GameObject> generatedObjects)
        {
            RollBackGeneratedObjects(generatedObjects);
            generatedProductCount = 0;
            generatedWallOutletCount = 0;
            generatedWallOutletWalls = string.Empty;
        }

        private static void RollBackGeneratedObjects(
            IReadOnlyList<GameObject> generatedObjects)
        {
            for (var i = generatedObjects.Count - 1; i >= 0; i--)
            {
                RuntimeEquipmentFactory.Abort(generatedObjects[i]);
            }
        }

        private static Bounds CalculatePrefabBounds(GameObject prefab)
        {
            var colliders = prefab.GetComponentsInChildren<Collider2D>(true);
            if (colliders.Length == 0) return new Bounds(Vector3.zero, Vector3.one);
            var bounds = colliders[0].bounds;
            for (var i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
            return bounds;
        }
    }
}
