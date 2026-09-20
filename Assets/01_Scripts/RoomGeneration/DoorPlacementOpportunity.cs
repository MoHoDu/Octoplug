using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Validated center intervals where one door can fit on a shared wall.</summary>
    public sealed class DoorPlacementOpportunity
    {
        internal DoorPlacementOpportunity(SharedWall sharedWall, IReadOnlyList<CoordinateInterval> safeCenterIntervals)
        {
            SharedWall = sharedWall;
            SafeCenterIntervals = safeCenterIntervals ?? throw new ArgumentNullException(nameof(safeCenterIntervals));
        }

        public SharedWall SharedWall { get; }
        public IReadOnlyList<CoordinateInterval> SafeCenterIntervals { get; }
    }
}
