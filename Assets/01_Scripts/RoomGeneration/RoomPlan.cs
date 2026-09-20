using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Validated candidate, adjacency, and door data ready for later integration.</summary>
    public sealed class RoomPlan
    {
        internal RoomPlan(
            RoomPlacement room,
            IReadOnlyList<SharedWall> sharedWalls,
            IReadOnlyList<DoorPlan> doorPlans,
            object sourceLayoutToken)
        {
            Room = room;
            SharedWalls = sharedWalls ?? throw new ArgumentNullException(nameof(sharedWalls));
            DoorPlans = doorPlans ?? throw new ArgumentNullException(nameof(doorPlans));
            SourceLayoutToken = sourceLayoutToken ?? throw new ArgumentNullException(nameof(sourceLayoutToken));
            State = RoomLifecycleState.HintLocked;
        }

        public RoomPlacement Room { get; }
        public Point2D CandidatePosition => Room.Bounds.Center;
        /// <summary>A RoomPlan is always a prospective hint; committed state is owned by RoomGenerationState.</summary>
        public RoomLifecycleState State { get; }
        public IReadOnlyList<SharedWall> SharedWalls { get; }
        public IReadOnlyList<DoorPlan> DoorPlans { get; }
        internal object SourceLayoutToken { get; }
    }
}
