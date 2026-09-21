using System;
using System.Collections.Generic;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>
    /// Owns the production room lifecycle while leaving progression and room-content policy to callers.
    /// </summary>
    public sealed class ProductionRoomGenerationController : MonoBehaviour
    {
        private const float SeedLatticeTolerance = 0.01f;
        private static readonly ProfilerMarker PromoteMarker =
            new("Octoplug.RoomGeneration.PromoteCurrentHint");
        private static readonly ProfilerMarker RenderPromotionMarker =
            new("Octoplug.RoomGeneration.RenderPromotion");
        private static readonly ProfilerMarker FinalizeContentMarker =
            new("Octoplug.RoomGeneration.FinalizeContent");
        private static readonly ProfilerMarker RedistributeMarker =
            new("Octoplug.RoomGeneration.Redistribute");
        private static readonly ProfilerMarker PlanHintMarker =
            new("Octoplug.RoomGeneration.PlanNextHint");

        [Header("Production references")]
        [SerializeField]
        private RoomGenerationRoomBinder seedRoom;

        [SerializeField]
        private RoomGenerationRoomBinder roomPrefab;

        [SerializeField]
        private Transform roomsRoot;

        [SerializeField]
        private CableRoutingGridService routingGrid;

        [SerializeField]
        private RoomContentGenerationController roomContentGeneration;

        [Header("Candidate sequence")]
        [SerializeField]
        private string seedRoomId = "room-seed";

        [SerializeField]
        private string candidateIdPrefix = "room";

        [Header("Debug")]
        [SerializeField]
        private bool enableDebugPromotion = true;

        private readonly Dictionary<RoomId, RoomGenerationRoomBinder> bindersById = new();
        private readonly WidestSafeIntervalMidpointPolicy doorPolicy = new();
        private RoomGenerationState state;
        private DoorPlanningOptions doorOptions;
        private IntegerRoomSize[] coreRoomSizes;
        private bool initialized;

        public event Action<RoomGenerationRoomBinder> RoomGenerated;
        public event Action<RoomPlacement> RoomUnlocked;
        public event Action<RoomPlacement> RoomContentReady;
        public event Action<RoomPlacement> NextHintCreated;

        public RoomGenerationState State => state;
        public bool IsInitialized => initialized;

        public bool TryGetRoomBinder(RoomId roomId, out RoomGenerationRoomBinder binder)
        {
            return bindersById.TryGetValue(roomId, out binder) && binder != null;
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (enableDebugPromotion && initialized && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                PromoteCurrentHint();
            }
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ValidateControllerReferences();
            coreRoomSizes = BuildRoomSizes();
            var seedPlacement = seedRoom.CreateSeedPlacement(seedRoomId);
            ValidateSeedLattice(seedPlacement.Bounds);

            var initiallyOccupiedWalls = new[]
            {
                new RoomWallId(seedPlacement.Id, WallSide.Left)
            };
            state = new RoomGenerationState(RoomLayout.Create(new[] { seedPlacement }, initiallyOccupiedWalls));
            doorOptions = new DoorPlanningOptions(seedRoom.DoorWidth, seedRoom.SafetyMargin);

            bindersById.Add(seedPlacement.Id, seedRoom);
            seedRoom.ApplyPlacement(seedPlacement, RoomVisualIntent.For(RoomLifecycleState.UnlockedGenerated));
            seedRoom.SetAuthoredDoorTemplateVisible(true);
            initialized = true;

            if (!FinalizeRoomContent(seedPlacement))
            {
                initialized = false;
                throw new InvalidOperationException(
                    $"Required seed Room content generation failed for {seedPlacement.Id}.");
            }

            RoomContentReady?.Invoke(seedPlacement);
            PlanStoreAndRenderNextHint();
            PromoteCurrentHint();
        }

        [ContextMenu("Debug/Promote Current Hint")]
        private void PromoteCurrentHintFromInspector()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Room hints can be promoted only in Play Mode.", this);
                return;
            }

            PromoteCurrentHint();
        }

        /// <summary>
        /// Promotes the exact stored hint. Progression/GameFlow decides when to call this method.
        /// </summary>
        public bool PromoteCurrentHint()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Controller must be initialized before promotion.");
            }

            if (!state.HasNextRoomPlan)
            {
                Debug.LogWarning("No stored room hint is available to promote.", this);
                return false;
            }

            using (PromoteMarker.Auto())
            {
                var previousState = state;
                var promotedPlan = state.NextRoomPlan;
                var promoted = promotedPlan.Room;
                state = state.UnlockNext();
                using (RenderPromotionMarker.Auto())
                {
                    RenderPromotion(promotedPlan);
                }

                using (FinalizeContentMarker.Auto())
                {
                    if (!FinalizeRoomContent(promoted))
                    {
                        state = previousState;
                        RenderState();
                        routingGrid.RebuildFromScene();
                        Debug.LogWarning(
                            $"Room content generation failed for {promoted.Id}; "
                            + "the locked Room hint was restored.",
                            this);
                        return false;
                    }
                }

                RoomUnlocked?.Invoke(promoted);
                using (RedistributeMarker.Auto())
                {
                    roomContentGeneration?.RedistributeProducts();
                }

                routingGrid.RebuildFromScene();
                RoomContentReady?.Invoke(promoted);
                using (PlanHintMarker.Auto())
                {
                    PlanStoreAndRenderNextHint();
                }

                return true;
            }
        }

        private bool FinalizeRoomContent(RoomPlacement room)
        {
            // Promoted geometry must be present before content placement queries it.
            // The caller performs one authoritative post-redistribution rebuild.
            routingGrid.RebuildFromScene();
            roomContentGeneration ??=
                GetComponent<RoomContentGenerationController>();
            if (roomContentGeneration != null
                && !roomContentGeneration.GenerateRoomContent(room))
            {
                return false;
            }

            return true;
        }

        private void PlanStoreAndRenderNextHint()
        {
            var candidates = BalancedFrontierCandidateGenerator.Generate(
                state.UnlockedLayout,
                coreRoomSizes,
                candidateIdPrefix);

            var extraBlocked = new Dictionary<RoomWallId, IReadOnlyList<CoordinateInterval>>();
            foreach (var outlet in Octoplug.Power.RuntimeWorldRegistry.GetWallOutlets())
            {
                if (outlet != null && Octoplug.Power.RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var ownerId))
                {
                    RoomPlacement room = default;
                    bool foundRoom = false;
                    for (int i = 0; i < state.UnlockedLayout.Rooms.Count; i++)
                    {
                        if (state.UnlockedLayout.Rooms[i].Id == ownerId)
                        {
                            room = state.UnlockedLayout.Rooms[i];
                            foundRoom = true;
                            break;
                        }
                    }
                    if (!foundRoom) continue;

                    var colliders = outlet.GetComponentsInChildren<Collider2D>(true);
                    if (colliders.Length == 0) continue;
                    var bounds = colliders[0].bounds;
                    for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);

                    var center = bounds.center;
                    var roomBounds = room.Bounds;
                    WallSide side = WallSide.Bottom;
                    float minDiff = float.MaxValue;

                    float dTop = Mathf.Abs(center.y - roomBounds.MaxY);
                    if (dTop < minDiff) { minDiff = dTop; side = WallSide.Top; }
                    float dRight = Mathf.Abs(center.x - roomBounds.MaxX);
                    if (dRight < minDiff) { minDiff = dRight; side = WallSide.Right; }
                    float dBottom = Mathf.Abs(center.y - roomBounds.MinY);
                    if (dBottom < minDiff) { minDiff = dBottom; side = WallSide.Bottom; }
                    float dLeft = Mathf.Abs(center.x - roomBounds.MinX);
                    if (dLeft < minDiff) { minDiff = dLeft; side = WallSide.Left; }

                    var wallId = new RoomWallId(ownerId, side);
                    float start = (side == WallSide.Top || side == WallSide.Bottom) ? bounds.min.x : bounds.min.y;
                    float end = (side == WallSide.Top || side == WallSide.Bottom) ? bounds.max.x : bounds.max.y;

                    if (!extraBlocked.TryGetValue(wallId, out var list))
                    {
                        list = new List<CoordinateInterval>();
                        extraBlocked[wallId] = list;
                    }
                    ((List<CoordinateInterval>)list).Add(new CoordinateInterval(start, end));
                }
            }

            var dynamicOptions = new DoorPlanningOptions(doorOptions.DoorWidth, doorOptions.SafetyMargin, extraBlocked);

            var result = state.PlanFollowing(candidates, dynamicOptions, doorPolicy);
            if (!result.Success)
            {
                Debug.LogWarning($"No valid following room candidate was found after {result.Rejections.Count} rejection(s).", this);
                RenderState();
                return;
            }

            state = state.StoreNextPlan(result.Plan);
            RenderNextHint(result.Plan.Room);
            NextHintCreated?.Invoke(state.NextRoomPlan.Room);
        }

        private void RenderPromotion(RoomPlan promotedPlan)
        {
            var binder = EnsureBinder(promotedPlan.Room);
            binder.ApplyPlacement(
                promotedPlan.Room,
                state.GetVisualIntent(promotedPlan.Room.Id));
            for (var i = 0; i < promotedPlan.DoorPlans.Count; i++)
            {
                RenderDoor(promotedPlan.DoorPlans[i]);
            }
        }

        private void RenderNextHint(RoomPlacement hint)
        {
            EnsureBinder(hint).ApplyPlacement(
                hint,
                state.GetVisualIntent(hint.Id));
        }

        private void RenderState()
        {
            EnsureRequiredBinders();

            foreach (var pair in bindersById)
            {
                pair.Value.ClearRuntimeDoors();
                pair.Value.RebuildFullWalls();
            }

            for (var i = 0; i < state.UnlockedLayout.Rooms.Count; i++)
            {
                var placement = state.UnlockedLayout.Rooms[i];
                bindersById[placement.Id].ApplyPlacement(placement, state.GetVisualIntent(placement.Id));
            }

            if (state.HasNextRoomPlan)
            {
                var hint = state.NextRoomPlan.Room;
                bindersById[hint.Id].ApplyPlacement(hint, state.GetVisualIntent(hint.Id));
            }

            for (var i = 0; i < state.UnlockedLayout.Doors.Count; i++)
            {
                RenderDoor(state.UnlockedLayout.Doors[i]);
            }
        }

        private void EnsureRequiredBinders()
        {
            for (var i = 0; i < state.UnlockedLayout.Rooms.Count; i++)
            {
                EnsureBinder(state.UnlockedLayout.Rooms[i]);
            }

            if (state.HasNextRoomPlan)
            {
                EnsureBinder(state.NextRoomPlan.Room);
            }
        }

        private RoomGenerationRoomBinder EnsureBinder(RoomPlacement placement)
        {
            if (bindersById.TryGetValue(placement.Id, out var existing))
            {
                return existing;
            }

            var binder = Instantiate(roomPrefab, roomsRoot);
            binder.name = $"Room {placement.Id.Value}";
            binder.gameObject.SetActive(false);
            binder.SetAuthoredDoorTemplateVisible(false);
            binder.gameObject.SetActive(true);
            bindersById.Add(placement.Id, binder);
            RoomGenerated?.Invoke(binder);
            return binder;
        }

        private void RenderDoor(DoorPlan doorPlan)
        {
            var roomA = bindersById[doorPlan.WallA.RoomId];
            var roomB = bindersById[doorPlan.WallB.RoomId];
            roomA.OpenDoor(doorPlan.WallA, doorPlan.Span);
            roomB.OpenDoor(doorPlan.WallB, doorPlan.Span);

            var mapped = DoorViewTransformMapper.Map(doorPlan);
            var owner = bindersById[mapped.OwnerWall.RoomId];
            owner.SpawnDoor(
                new Vector2(mapped.HingePosition.X, mapped.HingePosition.Y),
                mapped.ZRotationDegrees);
        }

        private IntegerRoomSize[] BuildRoomSizes()
        {
            var bounds = roomPrefab.AuthoredLocalBounds;
            var baseWidth = RequireIntegralDimension(bounds.Width, "authored room width");
            var baseHeight = RequireIntegralDimension(bounds.Height, "authored room height");
            var wideWidth = RequireIntegralDimension(bounds.Width * 1.5f, "1.5x room width");
            var tallHeight = RequireIntegralDimension(bounds.Height * 1.5f, "1.5x room height");
            return new[]
            {
                new IntegerRoomSize(baseWidth, baseHeight),
                new IntegerRoomSize(wideWidth, baseHeight),
                new IntegerRoomSize(baseWidth, tallHeight),
                new IntegerRoomSize(wideWidth, tallHeight)
            };
        }

        private void ValidateControllerReferences()
        {
            if (seedRoom == null || roomPrefab == null || roomsRoot == null || routingGrid == null)
            {
                throw new InvalidOperationException("Seed room, room prefab, rooms root, and routing grid references are required.");
            }

            roomContentGeneration ??=
                GetComponent<RoomContentGenerationController>();

            if (string.IsNullOrWhiteSpace(seedRoomId) || string.IsNullOrWhiteSpace(candidateIdPrefix))
            {
                throw new InvalidOperationException("Seed room id and candidate id prefix must not be empty.");
            }

            if (!seedRoom.TryValidate(out var seedError))
            {
                throw new InvalidOperationException($"Seed room binder is invalid: {seedError}");
            }

            if (!roomPrefab.TryValidate(out var prefabError))
            {
                throw new InvalidOperationException($"Room prefab binder is invalid: {prefabError}");
            }
        }

        private static int RequireIntegralDimension(float value, string label)
        {
            var nearestInteger = Mathf.RoundToInt(value);
            if (Mathf.Abs(value - nearestInteger) > SeedLatticeTolerance || value != nearestInteger)
            {
                throw new InvalidOperationException(
                    $"The {label} ({value}) must be an exact integer lattice dimension; runtime snapping is forbidden.");
            }

            return nearestInteger;
        }

        private static void ValidateSeedLattice(RoomBounds2D bounds)
        {
            ValidateCoordinate(bounds.MinX, nameof(bounds.MinX));
            ValidateCoordinate(bounds.MinY, nameof(bounds.MinY));
            ValidateCoordinate(bounds.MaxX, nameof(bounds.MaxX));
            ValidateCoordinate(bounds.MaxY, nameof(bounds.MaxY));

            static void ValidateCoordinate(float value, string coordinateName)
            {
                var nearestInteger = Mathf.Round(value);
                if (Mathf.Abs(value - nearestInteger) > SeedLatticeTolerance || value != nearestInteger)
                {
                    throw new InvalidOperationException(
                        $"Seed room {coordinateName}={value} must lie exactly on the integer lattice; runtime snapping is forbidden.");
                }
            }
        }
    }
}
