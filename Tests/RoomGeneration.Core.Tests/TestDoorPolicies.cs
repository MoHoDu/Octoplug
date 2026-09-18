using System.Collections.Generic;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Tests
{
    internal sealed class SelectAllMidpointsPolicy : IDoorPlacementPolicy
    {
        public IReadOnlyList<DoorPlacementProposal> SelectDoors(IReadOnlyList<DoorPlacementOpportunity> opportunities)
        {
            var result = new List<DoorPlacementProposal>();
            for (var i = 0; i < opportunities.Count; i++)
            {
                var interval = opportunities[i].SafeCenterIntervals[0];
                result.Add(new DoorPlacementProposal(i, (interval.Min + interval.Max) * 0.5f));
            }

            return result;
        }
    }

    internal sealed class SelectFirstMidpointPolicy : IDoorPlacementPolicy
    {
        public IReadOnlyList<DoorPlacementProposal> SelectDoors(IReadOnlyList<DoorPlacementOpportunity> opportunities)
        {
            if (opportunities.Count == 0) return new DoorPlacementProposal[0];
            var interval = opportunities[0].SafeCenterIntervals[0];
            return new[] { new DoorPlacementProposal(0, (interval.Min + interval.Max) * 0.5f) };
        }
    }

    internal sealed class NoDoorPolicy : IDoorPlacementPolicy
    {
        public IReadOnlyList<DoorPlacementProposal> SelectDoors(IReadOnlyList<DoorPlacementOpportunity> opportunities)
            => new DoorPlacementProposal[0];
    }

    internal sealed class InvalidCenterPolicy : IDoorPlacementPolicy
    {
        public IReadOnlyList<DoorPlacementProposal> SelectDoors(IReadOnlyList<DoorPlacementOpportunity> opportunities)
            => new[] { new DoorPlacementProposal(0, float.MaxValue) };
    }

    internal sealed class SelectFirstIntervalMinimumPolicy : IDoorPlacementPolicy
    {
        public IReadOnlyList<DoorPlacementProposal> SelectDoors(IReadOnlyList<DoorPlacementOpportunity> opportunities)
            => new[] { new DoorPlacementProposal(0, opportunities[0].SafeCenterIntervals[0].Min) };
    }
}
