using System.Collections.Generic;
using System.Linq;
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
                housePowerBudget = Object.FindAnyObjectByType<HousePowerBudget>();
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
/// <summary>
        /// Creates the promoted room's production content synchronously. The room
        /// lifecycle owner calls this before its final routing-grid refresh and
        /// before publishing RoomContentReady, so consumers can rely on that event
        /// meaning the generated sockets are already discoverable and routable.
        /// </summary>
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
                var starters = DefaultRoomContentBalance.GetStarterConfigs();
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
                    configId = $"starter-{starter.StarterRoomIndex}";
                }
            }

            if (string.IsNullOrEmpty(configId))
            {
                var housePower = GetHouseAllowedPower();
                var configs = DefaultRoomContentBalance.GetDefaultConfigs()
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

            // Infrastructure is finalized first so every Product candidate can
            // prove a production-routed connection using its initial CableLength.
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
                    wallOutletPlacementWarning = "required Wall Outlet was not finalized";
                }

                Debug.LogWarning(
                    $"[RoomContentRequiredInfrastructure]\n" +
                    $"Room: {room.Id}\n" +
                    $"Config: {configId}\n" +
                    $"Requested: {RequiredWallOutletCount}\n" +
                    $"Placed: {generatedWallOutletCount}\n" +
                    $"Reason: {wallOutletPlacementWarning}",
                    this);
                return false;
            }

            // Each accepted footprint is reserved before the next Product is
            // considered, so same-batch placement never relies on physics lag.
            generatedProductCount += SpawnProduct(
                tvPrefab,
                tvCount.Value,
                room,
                grid,
                generatedObjects);
            generatedProductCount += SpawnProduct(
                fanPrefab,
                fanCount.Value,
                room,
                grid,
                generatedObjects);
            generatedProductCount += SpawnProduct(
                heaterPrefab,
                heaterCount.Value,
                room,
                grid,
                generatedObjects);
            generatedProductCount += SpawnProduct(
                inductionPrefab,
                inductionCount.Value,
                room,
                grid,
                generatedObjects);
            generatedProductCount += SpawnProduct(
                airConditionerPrefab,
                airConditionerCount.Value,
                room,
                grid,
                generatedObjects);

            var requestedProductCount = tvCount.Value
                + fanCount.Value
                + heaterCount.Value
                + inductionCount.Value
                + airConditionerCount.Value;
            if (generatedProductCount != requestedProductCount)
            {
                Debug.LogWarning(
                    $"[RoomContentProductShortfall]\n" +
                    $"Room: {room.Id}\n" +
                    $"Config: {configId}\n" +
                    $"Requested: {requestedProductCount}\n" +
                    $"Placed: {generatedProductCount}\n" +
                    "Reason: selected RoomConfig could not be placed exactly",
                    this);
                RollBackGeneratedObjects(generatedObjects);
                generatedProductCount = 0;
                generatedWallOutletCount = 0;
                generatedWallOutletWalls = string.Empty;
                return false;
            }

            Debug.Log(
                $"[RoomContent]\n" +
                $"Room: {room.Id}\n" +
                $"Config: {configId}\n" +
                $"Products Generated: {generatedProductCount}\n" +
                $"WallOutlets Generated: {generatedWallOutletCount}\n" +
                $"Outlet Walls: {(string.IsNullOrEmpty(generatedWallOutletWalls) ? "None" : generatedWallOutletWalls)}",
                this);

            return true;
        }

        private int SpawnProduct(
            ApplianceSource prefab,
            int count,
            RoomPlacement room,
            CableRoutingGrid grid,
            List<GameObject> generatedObjects)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[RoomContentProductPlacement]\n" +
                    $"Room: {room.Id}\n" +
                    "Product: Missing prefab\n" +
                    $"Requested: {count}\n" +
                    "Placed: 0\n" +
                    "Reason: configured Product prefab is unavailable",
                    this);
                return 0;
            }

            var productParent = GameObject.Find("Products")?.transform;
            var spawned = 0;
            var failure = "no placement attempt was made";
            for (var i = 0; i < count; i++)
            {
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
                var candidateFailure = failure;
                if (!RoomObjectPlacementPlanner.TryFindPosition(
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
                        out failure))
                {
                    if (!string.IsNullOrEmpty(candidateFailure))
                    {
                        failure = candidateFailure;
                    }

                    RuntimeEquipmentFactory.Abort(instance);
                    continue;
                }

                instance.transform.position = spawnPosition;
                ResolveOverlap(instance, room);
                if (!RuntimeEquipmentFactory.TryFinalizeProduct(
                        instance,
                        room.Id,
                        grid,
                        out _,
                        out failure))
                {
                    continue;
                }

                generatedObjects.Add(instance);
                spawned++;
            }

            if (spawned < count)
            {
                Debug.LogWarning(
                    $"[RoomContentProductPlacement]\n" +
                    $"Room: {room.Id}\n" +
                    $"Product: {prefab.name}\n" +
                    $"Requested: {count}\n" +
                    $"Placed: {spawned}\n" +
                    $"Reason: {failure}",
                    this);
            }

            return spawned;
        }

        private int SpawnWallOutlets(
            int count,
            int minSockets,
            int maxSockets,
            RoomPlacement room,
            List<GameObject> generatedObjects)
        {
            if (wallOutletPrefab == null || count <= 0)
            {
                return 0;
            }

            if (!roomGeneration.TryGetRoomBinder(room.Id, out var roomBinder))
            {
                wallOutletPlacementWarning = "room binder is unavailable";
                return 0;
            }

            var wallParent = GameObject.Find("Wall_Outlets")?.transform;
            if (wallParent == null)
            {
                wallOutletPlacementWarning = "Wall_Outlets parent is unavailable";
                return 0;
            }

            var outletBounds = CalculatePrefabBounds(wallOutletPrefab.gameObject);
            var outletGeometry = new WallOutletGeometry(
                wallOutletPrefab.transform.InverseTransformPoint(outletBounds.center),
                outletBounds.size);
            var placements = WallOutletPlacementPlanner.Plan(
                room,
                roomGeneration.State.UnlockedLayout.Doors,
                outletGeometry,
                roomBinder.SafetyMargin,
                count);
            var placedWalls = new List<string>(placements.Count);
            var socketSelector = new WeightedSocketCountSelector(
                DefaultSocketCountBalance.CreateWallOutletCatalog());
            var socketWeight = socketSelector.GetTotalWeight(minSockets, maxSockets);

            for (var i = 0; i < placements.Count; i++)
            {
                var placement = placements[i];
                var go = RuntimeEquipmentFactory.Stage(
                    wallOutletPrefab.gameObject,
                    placement.Position,
                    Quaternion.Euler(0f, 0f, placement.RotationDegrees),
                    wallParent);
                var socketCount = socketSelector.Select(
                    minSockets,
                    maxSockets,
                    UnityEngine.Random.Range(0, socketWeight));
                if (!RuntimeEquipmentFactory.TryFinalizeWallOutlet(
                        go,
                        room.Id,
                        socketCount,
                        out var outlet,
                        out var failure))
                {
                    Debug.LogWarning(
                        $"[RoomContent] Failed to initialize Wall Outlet on {placement.Side}: {failure}.",
                        this);
                    continue;
                }

                var activeSockets = outlet.ActiveSockets.ToArray();
                var activeColliderCount = 0;
                for (var socketIndex = 0;
                     socketIndex < activeSockets.Length;
                     socketIndex++)
                {
                    if (activeSockets[socketIndex] != null
                        && activeSockets[socketIndex].isActiveAndEnabled
                        && activeSockets[socketIndex]
                            .GetComponentsInChildren<Collider2D>(true)
                            .Any(collider => collider.enabled
                                && collider.gameObject.activeInHierarchy))
                    {
                        activeColliderCount++;
                    }
                }

                Debug.Log(
                    $"[RuntimeWallOutlet]\n" +
                    $"Room: {room.Id}\n" +
                    $"ActiveSocketCount: {outlet.ActiveSocketCount}\n" +
                    $"RegisteredSockets: {activeSockets.Length}\n" +
                    $"Colliders: {activeColliderCount}\n" +
                    $"ConnectionRegistry: OK",
                    outlet);
                generatedObjects.Add(go);
                placedWalls.Add(placement.Side.ToString());
            }

            generatedWallOutletWalls = string.Join(", ", placedWalls);
            if (placedWalls.Count < count)
            {
                wallOutletPlacementWarning = "no remaining valid wall";
            }

            return placedWalls.Count;
        }

        private static bool CanReachWallOutlet(
            ApplianceSource product,
            RoomPlacement room,
            CableRoutingGrid grid)
        {
            if (product == null
                || product.Cable == null
                || product.Cable.Origin == null)
            {
                return false;
            }

            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
            {
                if (outlet == null
                    || !RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner)
                    || owner != room.Id)
                {
                    continue;
                }

                foreach (var socket in outlet.ActiveSockets)
                {
                    if (socket != null
                        && !socket.IsConnected
                        && CableRouteReachability.IsWithinLength(
                            grid,
                            product.Cable.Origin.position,
                            socket,
                            product.Cable.CableLength))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool OverlapsPlacementObject(
            PlacementFootprint candidate,
            Bounds candidateBounds)
        {
            const float inset = 0.0001f;
            var halfWidth = candidateBounds.extents.x - inset;
            var halfHeight = candidateBounds.extents.y - inset;
            if (halfWidth <= 0f || halfHeight <= 0f)
            {
                return false;
            }

            var hits = Physics2D.OverlapAreaAll(
                new Vector2(
                    candidateBounds.center.x - halfWidth,
                    candidateBounds.center.y - halfHeight),
                new Vector2(
                    candidateBounds.center.x + halfWidth,
                    candidateBounds.center.y + halfHeight));
            for (var i = 0; i < hits.Length; i++)
            {
                var other = hits[i] != null
                    ? hits[i].GetComponentInParent<PlacementFootprint>()
                    : null;
                if (other == null
                    || other == candidate
                    || (other.GetComponent<ApplianceSource>() == null
                        && other.GetComponent<PowerStrip>() == null))
                {
                    continue;
                }

                if (HasPositiveAreaOverlap(candidateBounds, other.WorldBounds, inset))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasPositiveAreaOverlap(
            Bounds first,
            Bounds second,
            float tolerance)
        {
            var overlapX = Mathf.Min(first.max.x, second.max.x)
                - Mathf.Max(first.min.x, second.min.x);
            var overlapY = Mathf.Min(first.max.y, second.max.y)
                - Mathf.Max(first.min.y, second.min.y);
            return overlapX > tolerance && overlapY > tolerance;
        }

                private void ResolveOverlap(GameObject instance, RoomPlacement room)
        {
            var colliders = instance.GetComponentsInChildren<Collider2D>();
            if (colliders.Length == 0) return;

            // Try adjusting position up to 15 times
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
                            // normal points from hit to col. distance is negative.
                            // Move col out of hit.
                            var pushDir = dist.normal;
                            if (pushDir.sqrMagnitude < 0.001f)
                            {
                                pushDir = UnityEngine.Random.insideUnitCircle.normalized;
                            }
                            instance.transform.position += (Vector3)(pushDir * (-dist.distance + 0.01f));
                        }
                    }
                }

                if (!overlapped) break;
            }

            // Constrain inside room bounds just in case it got pushed out
            var pos = instance.transform.position;
            pos.x = Mathf.Clamp(pos.x, room.Bounds.MinX + 0.5f, room.Bounds.MaxX - 0.5f);
            pos.y = Mathf.Clamp(pos.y, room.Bounds.MinY + 0.5f, room.Bounds.MaxY - 0.5f);
            instance.transform.position = pos;

            // Re-reserve grid cells if it moved
            var footprint = instance.GetComponent<PlacementFootprint>();
            if (footprint != null)
            {
                var service = CableRoutingGridService.Instance;
                if (service != null && service.Grid != null)
                {
                    footprint.TryReserveAt(service.Grid, pos);
                }
            }
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
            if (colliders.Length == 0)
            {
                return new Bounds(Vector3.zero, Vector3.one);
            }

            var bounds = colliders[0].bounds;
            for (var i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return bounds;
        }

    }
}
