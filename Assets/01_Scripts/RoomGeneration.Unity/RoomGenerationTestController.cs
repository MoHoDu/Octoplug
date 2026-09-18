using System;
using System.Collections.Generic;
using Octoplug.RoomGeneration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>
    /// Test-scene orchestration for deterministic room hints and exact stored-plan promotion on Space.
    /// </summary>
    public sealed class RoomGenerationTestController : MonoBehaviour
    {
        private const float SeedLatticeTolerance = 0.01f;

        [Header("Typed scene references")]
        [SerializeField]
        private RoomGenerationRoomBinder seedRoom;

        [SerializeField]
        private RoomGenerationRoomBinder roomPrefab;

        [SerializeField]
        private Transform roomsRoot;

        [Header("Deterministic test sequence")]
        [SerializeField]
        private string seedRoomId = "room-seed";

        [SerializeField]
        private string candidateIdPrefix = "room";

        private readonly Dictionary<RoomId, RoomGenerationRoomBinder> bindersById = new();
        private readonly WidestSafeIntervalMidpointPolicy doorPolicy = new();
        private RoomGenerationState state;
        private DoorPlanningOptions doorOptions;
        private IntegerRoomSize[] coreRoomSizes;
        private bool initialized;

        public RoomGenerationState State => state;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (initialized && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                PromoteStoredPlanAndPlanFollowing();
            }
        }

        private void OnValidate()
        {
            if (seedRoom == null || roomPrefab == null || roomsRoot == null)
            {
                Debug.LogError($"{nameof(RoomGenerationTestController)} requires seed room, room prefab, and rooms root references.", this);
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
            var layout = RoomLayout.Create(new[] { seedPlacement }, initiallyOccupiedWalls);
            state = new RoomGenerationState(layout);
            doorOptions = new DoorPlanningOptions(seedRoom.DoorWidth, seedRoom.SafetyMargin);

            bindersById.Add(seedPlacement.Id, seedRoom);
            seedRoom.ApplyPlacement(seedPlacement, RoomVisualIntent.For(RoomLifecycleState.UnlockedGenerated));
            seedRoom.SetAuthoredDoorTemplateVisible(true);
            initialized = true;

            PlanAndStoreFollowing();
            RenderState();
        }

        public void PromoteStoredPlanAndPlanFollowing()
        {
            if (!initialized)
            {
                throw new InvalidOperationException("Controller must be initialized before promotion.");
            }

            if (!state.HasNextRoomPlan)
            {
                Debug.LogWarning("No stored room hint is available to promote.", this);
                return;
            }

            // UnlockNext applies the exact validated RoomPlan object stored in state; no candidate is recalculated here.
            state = state.UnlockNext();
            PlanAndStoreFollowing();
            RenderState();
        }

        private void PlanAndStoreFollowing()
        {
            var candidates = BuildFollowingCandidates(state.UnlockedLayout);
            var result = state.PlanFollowing(candidates, doorOptions, doorPolicy);
            if (!result.Success)
            {
                Debug.LogWarning($"No valid following room candidate was found after {result.Rejections.Count} rejection(s).", this);
                return;
            }

            state = state.StoreNextPlan(result.Plan);
        }

        private IReadOnlyList<RoomCandidate> BuildFollowingCandidates(RoomLayout layout)
        {
            return BalancedFrontierCandidateGenerator.Generate(layout, coreRoomSizes, candidateIdPrefix);
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
                var hintPlacement = state.NextRoomPlan.Room;
                bindersById[hintPlacement.Id].ApplyPlacement(hintPlacement, state.GetVisualIntent(hintPlacement.Id));
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
            return binder;
        }

        private void RenderDoor(DoorPlan doorPlan)
        {
            if (!bindersById.TryGetValue(doorPlan.WallA.RoomId, out var roomA)
                || !bindersById.TryGetValue(doorPlan.WallB.RoomId, out var roomB))
            {
                throw new InvalidOperationException($"Both connected room binders are required before rendering door {doorPlan.WallA} / {doorPlan.WallB}.");
            }

            roomA.OpenDoor(doorPlan.WallA, doorPlan.Span);
            roomB.OpenDoor(doorPlan.WallB, doorPlan.Span);

            var mapped = DoorViewTransformMapper.Map(doorPlan);
            if (!bindersById.TryGetValue(mapped.OwnerWall.RoomId, out var owner))
            {
                throw new InvalidOperationException($"Mapped door owner {mapped.OwnerWall.RoomId} has no room binder.");
            }

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

        private static int RequireIntegralDimension(float value, string label)
        {
            var nearestInteger = Mathf.RoundToInt(value);
            if (Mathf.Abs(value - nearestInteger) > SeedLatticeTolerance)
            {
                throw new InvalidOperationException(
                    $"The {label} ({value}) must resolve to an integer lattice dimension. "
                    + "Correct the copied prefab geometry; runtime snapping is intentionally forbidden.");
            }

            if (value != nearestInteger)
            {
                throw new InvalidOperationException(
                    $"The {label} ({value}) is within tolerance but not exact. Set it to {nearestInteger}; runtime snapping is intentionally forbidden.");
            }

            return nearestInteger;
        }

        private void ValidateControllerReferences()
        {
            if (seedRoom == null || roomPrefab == null || roomsRoot == null)
            {
                throw new InvalidOperationException("Seed room, room prefab, and rooms root references are required.");
            }

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

        private static void ValidateSeedLattice(RoomBounds2D bounds)
        {
            ValidateCoordinate(bounds.MinX, nameof(bounds.MinX));
            ValidateCoordinate(bounds.MinY, nameof(bounds.MinY));
            ValidateCoordinate(bounds.MaxX, nameof(bounds.MaxX));
            ValidateCoordinate(bounds.MaxY, nameof(bounds.MaxY));

            static void ValidateCoordinate(float value, string coordinateName)
            {
                var nearestInteger = Mathf.Round(value);
                if (Mathf.Abs(value - nearestInteger) > SeedLatticeTolerance)
                {
                    throw new InvalidOperationException(
                        $"Seed room {coordinateName}={value} is more than {SeedLatticeTolerance} from the integer lattice. "
                        + "Align the copied test room in the Editor; runtime snapping is intentionally forbidden.");
                }

                if (value != nearestInteger)
                {
                    throw new InvalidOperationException(
                        $"Seed room {coordinateName}={value} is within validation tolerance but is not exact. "
                        + $"Set it to {nearestInteger} in the copied test scene; runtime snapping is intentionally forbidden.");
                }
            }
        }
    }
}
