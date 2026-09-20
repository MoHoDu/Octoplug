using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Immutable unlocked layout plus an optional separately stored next hint plan.</summary>
    public sealed class RoomGenerationState
    {
        public RoomGenerationState(RoomLayout unlockedLayout, RoomPlan nextRoomPlan = null)
        {
            UnlockedLayout = unlockedLayout ?? throw new ArgumentNullException(nameof(unlockedLayout));
            if (nextRoomPlan != null && !ReferenceEquals(nextRoomPlan.SourceLayoutToken, unlockedLayout.ValidationToken))
            {
                throw new ArgumentException("Next room plan was not validated against this layout.", nameof(nextRoomPlan));
            }

            NextRoomPlan = nextRoomPlan;
        }

        public RoomLayout UnlockedLayout { get; }
        public RoomPlan NextRoomPlan { get; }
        public bool HasNextRoomPlan => NextRoomPlan != null;

        public RoomGenerationState StoreNextPlan(RoomPlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (NextRoomPlan != null) throw new InvalidOperationException("A next room plan is already stored.");
            return new RoomGenerationState(UnlockedLayout, plan);
        }

        public RoomGenerationState UnlockNext()
        {
            if (NextRoomPlan == null) throw new InvalidOperationException("No next room plan is stored.");
            return new RoomGenerationState(UnlockedLayout.Apply(NextRoomPlan));
        }

        public RoomPlanResult PlanFollowing(
            System.Collections.Generic.IReadOnlyList<RoomCandidate> orderedCandidates,
            DoorPlanningOptions options,
            IDoorPlacementPolicy doorPolicy)
        {
            if (NextRoomPlan != null)
            {
                throw new InvalidOperationException("Unlock the stored next room before planning another hint.");
            }

            return RoomPlanner.PlanNext(UnlockedLayout, orderedCandidates, options, doorPolicy);
        }

        public RoomLifecycleState GetRoomState(RoomId roomId)
        {
            if (NextRoomPlan != null && NextRoomPlan.Room.Id == roomId)
            {
                return RoomLifecycleState.HintLocked;
            }

            if (UnlockedLayout.Contains(roomId))
            {
                return RoomLifecycleState.UnlockedGenerated;
            }

            throw new ArgumentException("Room is not present in this generation state.", nameof(roomId));
        }

        public RoomVisualIntent GetVisualIntent(RoomId roomId) => RoomVisualIntent.For(GetRoomState(roomId));
    }
}
