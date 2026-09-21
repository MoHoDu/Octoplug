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

        private void OnEnable()
        {
            if (roomGeneration != null)
            {
                roomGeneration.RoomUnlocked += HandleRoomUnlocked;
            }
        }

        private void OnDisable()
        {
            if (roomGeneration != null)
            {
                roomGeneration.RoomUnlocked -= HandleRoomUnlocked;
            }
        }

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

        private bool IsConfigPowerFeasible(RoomConfigRecord config, float housePower)
        {
            if (config.TvCount > 0 && tvPrefab != null && tvPrefab.PowerConsumptionWatts > housePower) return false;
            if (config.FanCount > 0 && fanPrefab != null && fanPrefab.PowerConsumptionWatts > housePower) return false;
            if (config.HeaterCount > 0 && heaterPrefab != null && heaterPrefab.PowerConsumptionWatts > housePower) return false;
            if (config.InductionCount > 0 && inductionPrefab != null && inductionPrefab.PowerConsumptionWatts > housePower) return false;
            if (config.AirConditionerCount > 0 && airConditionerPrefab != null && airConditionerPrefab.PowerConsumptionWatts > housePower) return false;
            return true;
        }

        private void HandleRoomUnlocked(RoomPlacement newRoom)
        {
            var archive = BalanceRegistry.Instance;
            if (archive == null) return;

            int roomCount = roomGeneration.State.UnlockedLayout.Rooms.Count;
            var rule = archive.ProductRedistributionRows.FirstOrDefault(r => roomCount >= r.minRoomCount && roomCount <= r.maxRoomCount);

            if (rule.minRoomCount == 0) return; // Not found

            int extraProducts = UnityEngine.Random.Range(rule.minExtraProducts, rule.maxExtraProducts + 1);
            if (extraProducts <= 0) return;

            float housePower = GetHouseAllowedPower();
            var validPool = archive.ProductSpawnPoolRows
                .Where(p => p.enabled && p.minRoomCount <= roomCount && IsProductPowerFeasible(p.productType, housePower))
                .ToList();

            if (validPool.Count == 0) return;

            var gridService = CableRoutingGridService.Instance;
            if (gridService == null) return;
            var grid = gridService.Grid;

            var usedRooms = new HashSet<RoomId>();
            int placed = 0;

            for (int i = 0; i < extraProducts; i++)
            {
                var targetRoom = SelectTargetRoom(rule.distinctRoomPerProduct, usedRooms);
                if (!targetRoom.IsValid) break;

                var productType = SelectProductFromPool(validPool);
                var prefab = GetPrefab(productType);

                if (prefab != null)
                {
                    if (TryPlaceExtraProduct(prefab, targetRoom, grid))
                    {
                        placed++;
                        usedRooms.Add(targetRoom.Id);
                    }
                    else
                    {
                        Debug.LogWarning($"[Redistribution] Failed to place {productType} in Room {targetRoom.Id}.");
                    }
                }
            }

            if (placed < extraProducts)
            {
                Debug.LogWarning($"[Redistribution] Requested {extraProducts}, Placed {placed}.");
            }
        }

        private RoomPlacement SelectTargetRoom(bool distinct, HashSet<RoomId> usedRooms)
        {
            var allRooms = roomGeneration.State.UnlockedLayout.Rooms;
            var candidates = new List<RoomPlacement>();
            var weights = new List<float>();
            float totalWeight = 0f;

            foreach (var room in allRooms)
            {
                if (distinct && usedRooms.Contains(room.Id)) continue;

                float area = (room.Bounds.MaxX - room.Bounds.MinX) * (room.Bounds.MaxY - room.Bounds.MinY);
                int currentProducts = CountActiveProductsInRoom(room.Id);
                float weight = area / (currentProducts + 1);

                candidates.Add(room);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (candidates.Count == 0) return default;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                currentWeight += weights[i];
                if (roll <= currentWeight) return candidates[i];
            }
            return candidates[0];
        }

        private int CountActiveProductsInRoom(RoomId roomId)
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

        private string SelectProductFromPool(List<ProductSpawnPoolSheetRow> pool)
        {
            int totalWeight = pool.Sum(p => p.weight);
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;
            foreach (var p in pool)
            {
                current += p.weight;
                if (roll <= current) return p.productType;
            }
            return pool[0].productType;
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

        private bool TryPlaceExtraProduct(ApplianceSource prefab, RoomPlacement room, CableRoutingGrid grid)
        {
            var productParent = GameObject.Find("Products")?.transform;
            var instance = RuntimeEquipmentFactory.Stage(prefab.gameObject, Vector2.zero, Quaternion.identity, productParent);
            var footprint = instance != null ? instance.GetComponent<PlacementFootprint>() : null;
            var product = instance != null ? instance.GetComponent<ApplianceSource>() : null;

            bool found = RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                footprint,
                socketCount: 0,
                (candidate, bounds) =>
                {
                    instance.transform.position = candidate;
                    Physics2D.SyncTransforms();
                    if (OverlapsPlacementObject(footprint, bounds)) return false;
                    if (!CanReachWallOutlet(product, room, grid)) return false;
                    return true;
                },
                out var spawnPosition,
                out var failure);

            if (!found)
            {
                RuntimeEquipmentFactory.Abort(instance);
                return false;
            }

            instance.transform.position = spawnPosition;
            ResolveOverlap(instance, room);
            return RuntimeEquipmentFactory.TryFinalizeProduct(instance, room.Id, grid, out _, out _);
        }

        public bool GenerateRoomContent(RoomPlacement room)
        {
            var roomCount = roomGeneration.State.UnlockedLayout.Rooms.Count;
            int? tvCount = 0;
            int? fanCount = 0;
            int? heaterCount = 0;
            int? inductionCount = 0;
            int? airConditionerCount = 0;
            int wallOutletSocketMin = 1;
            int wallOutletSocketMax = 2;
            string configId = string.Empty;

            if (roomCount <= 2)
            {
                var starters = Octoplug.RoomGeneration.RoomContentBalanceMapper.GetStarterConfigs();
                var starter = starters.FirstOrDefault(s => s.StarterRoomIndex == roomCount && s.Enabled);
                if (starter != null)
                {
                    tvCount = starter.TvCount;
                    fanCount = starter.FanCount;
                    heaterCount = starter.HeaterCount;
                    inductionCount = starter.InductionCount;
                    airConditionerCount = starter.AirConditionerCount;
                    wallOutletSocketMin = starter.WallOutletSocketMin;
                    wallOutletSocketMax = starter.WallOutletSocketMax;
                    configId = "starter-" + starter.StarterRoomIndex;
                }
            }

            if (string.IsNullOrEmpty(configId))
            {
                var housePower = GetHouseAllowedPower();
                var configs = Octoplug.RoomGeneration.RoomContentBalanceMapper.GetDefaultConfigs()
                    .Where(c => c.Enabled && c.MinRoomCount <= roomCount)
                    .Where(c => IsConfigPowerFeasible(c, housePower))
                    .ToList();

                if (configs.Count == 0)
                {
                    Debug.LogWarning($"[RoomContent] No eligible RoomConfig found for room {room.Id} (RoomCount: {roomCount}, Power: {housePower}).", this);
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

                tvCount = selectedConfig.TvCount;
                fanCount = selectedConfig.FanCount;
                heaterCount = selectedConfig.HeaterCount;
                inductionCount = selectedConfig.InductionCount;
                airConditionerCount = selectedConfig.AirConditionerCount;
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
            generatedWallOutletCount = SpawnWallOutlets(RequiredWallOutletCount, wallOutletSocketMin, wallOutletSocketMax, room, generatedObjects);
            if (generatedWallOutletCount != RequiredWallOutletCount)
            {
                if (string.IsNullOrEmpty(wallOutletPlacementWarning)) wallOutletPlacementWarning = "required Wall Outlet was not finalized";
                Debug.LogWarning($"[RoomContentRequiredInfrastructure]\nRoom: {room.Id}\nConfig: {configId}\nRequested: {RequiredWallOutletCount}\nPlaced: {generatedWallOutletCount}\nReason: {wallOutletPlacementWarning}", this);
                return false;
            }

            generatedProductCount += SpawnProduct(tvPrefab, tvCount.Value, room, grid, generatedObjects);
            generatedProductCount += SpawnProduct(fanPrefab, fanCount.Value, room, grid, generatedObjects);
            generatedProductCount += SpawnProduct(heaterPrefab, heaterCount.Value, room, grid, generatedObjects);
            generatedProductCount += SpawnProduct(inductionPrefab, inductionCount.Value, room, grid, generatedObjects);
            generatedProductCount += SpawnProduct(airConditionerPrefab, airConditionerCount.Value, room, grid, generatedObjects);

            var requestedProductCount = tvCount.Value + fanCount.Value + heaterCount.Value + inductionCount.Value + airConditionerCount.Value;
            if (generatedProductCount != requestedProductCount)
            {
                Debug.LogWarning($"[RoomContentProductShortfall]\nRoom: {room.Id}\nConfig: {configId}\nRequested: {requestedProductCount}\nPlaced: {generatedProductCount}\nReason: selected RoomConfig could not be placed exactly", this);
                RollBackGeneratedObjects(generatedObjects);
                generatedProductCount = 0;
                generatedWallOutletCount = 0;
                generatedWallOutletWalls = string.Empty;
                return false;
            }

            Debug.Log($"[RoomContent]\nRoom: {room.Id}\nConfig: {configId}\nProducts Generated: {generatedProductCount}\nWallOutlets Generated: {generatedWallOutletCount}\nOutlet Walls: {(string.IsNullOrEmpty(generatedWallOutletWalls) ? "None" : generatedWallOutletWalls)}", this);

            return true;
        }

        private int SpawnProduct(ApplianceSource prefab, int count, RoomPlacement room, CableRoutingGrid grid, List<GameObject> generatedObjects)
        {
            if (count <= 0) return 0;
            if (prefab == null) return 0;

            var productParent = GameObject.Find("Products")?.transform;
            var spawned = 0;
            var failure = "no placement attempt was made";
            for (var i = 0; i < count; i++)
            {
                var instance = RuntimeEquipmentFactory.Stage(prefab.gameObject, Vector2.zero, Quaternion.identity, productParent);
                var footprint = instance != null ? instance.GetComponent<PlacementFootprint>() : null;
                var product = instance != null ? instance.GetComponent<ApplianceSource>() : null;
                var candidateFailure = failure;
                if (!RoomObjectPlacementPlanner.TryFindPosition(room, grid, footprint, 0, (candidate, bounds) =>
                {
                    instance.transform.position = candidate;
                    Physics2D.SyncTransforms();
                    if (OverlapsPlacementObject(footprint, bounds)) { candidateFailure = "all otherwise valid positions overlap a Product or PowerStrip"; return false; }
                    if (!CanReachWallOutlet(product, room, grid)) { candidateFailure = "no usable room Wall Outlet socket is reachable within the Product's initial CableLength"; return false; }
                    return true;
                }, out var spawnPosition, out failure))
                {
                    if (!string.IsNullOrEmpty(candidateFailure)) failure = candidateFailure;
                    RuntimeEquipmentFactory.Abort(instance);
                    continue;
                }

                instance.transform.position = spawnPosition;
                ResolveOverlap(instance, room);
                if (!RuntimeEquipmentFactory.TryFinalizeProduct(instance, room.Id, grid, out _, out failure)) continue;

                generatedObjects.Add(instance);
                spawned++;
            }

            if (spawned < count) Debug.LogWarning($"[RoomContentProductPlacement]\nRoom: {room.Id}\nProduct: {prefab.name}\nRequested: {count}\nPlaced: {spawned}\nReason: {failure}", this);
            return spawned;
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

        private void ResolveOverlap(GameObject instance, RoomPlacement room)
        {
            var colliders = instance.GetComponentsInChildren<Collider2D>();
            if (colliders.Length == 0) return;

            for (int iteration = 0; iteration < 15; iteration++)
            {
                bool overlapped = false;
                Physics2D.SyncTransforms();
                foreach (var col in colliders)
                {
                    if (col.isTrigger) continue;
                    var hits = new List<Collider2D>();
                    var filter = new ContactFilter2D { useTriggers = false };
                    Physics2D.OverlapCollider(col, filter, hits);

                    foreach (var hit in hits)
                    {
                        if (hit.transform.IsChildOf(instance.transform) || hit.isTrigger) continue;
                        var dist = Physics2D.Distance(col, hit);
                        if (dist.isOverlapped)
                        {
                            overlapped = true;
                            var pushDir = dist.normal;
                            if (pushDir.sqrMagnitude < 0.001f) pushDir = UnityEngine.Random.insideUnitCircle.normalized;
                            instance.transform.position += (Vector3)(pushDir * (-dist.distance + 0.01f));
                        }
                    }
                }
                if (!overlapped) break;
            }

            var pos = instance.transform.position;
            pos.x = Mathf.Clamp(pos.x, room.Bounds.MinX + 0.5f, room.Bounds.MaxX - 0.5f);
            pos.y = Mathf.Clamp(pos.y, room.Bounds.MinY + 0.5f, room.Bounds.MaxY - 0.5f);
            instance.transform.position = pos;

            var footprint = instance.GetComponent<PlacementFootprint>();
            if (footprint != null)
            {
                var service = CableRoutingGridService.Instance;
                if (service != null && service.Grid != null) footprint.TryReserveAt(service.Grid, pos);
            }
        }

        private static bool OverlapsPlacementObject(PlacementFootprint candidate, Bounds candidateBounds)
        {
            const float inset = 0.0001f;
            var halfWidth = candidateBounds.extents.x - inset;
            var halfHeight = candidateBounds.extents.y - inset;
            if (halfWidth <= 0f || halfHeight <= 0f) return false;
            var hits = Physics2D.OverlapAreaAll(
                new Vector2(candidateBounds.center.x - halfWidth, candidateBounds.center.y - halfHeight),
                new Vector2(candidateBounds.center.x + halfWidth, candidateBounds.center.y + halfHeight));
            for (var i = 0; i < hits.Length; i++)
            {
                var other = hits[i] != null ? hits[i].GetComponentInParent<PlacementFootprint>() : null;
                if (other == null || other == candidate || (other.GetComponent<ApplianceSource>() == null && other.GetComponent<PowerStrip>() == null)) continue;
                if (HasPositiveAreaOverlap(candidateBounds, other.WorldBounds, inset)) return true;
            }
            return false;
        }

        private static bool HasPositiveAreaOverlap(Bounds first, Bounds second, float tolerance)
        {
            var overlapX = Mathf.Min(first.max.x, second.max.x) - Mathf.Max(first.min.x, second.min.x);
            var overlapY = Mathf.Min(first.max.y, second.max.y) - Mathf.Max(first.min.y, second.min.y);
            return overlapX > tolerance && overlapY > tolerance;
        }

        private static void RollBackGeneratedObjects(IReadOnlyList<GameObject> generatedObjects)
        {
            for (var i = generatedObjects.Count - 1; i >= 0; i--) RuntimeEquipmentFactory.Abort(generatedObjects[i]);
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
