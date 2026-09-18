using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Selects the midpoint of the globally widest safe Door interval.</summary>
    public sealed class WidestSafeIntervalMidpointPolicy : IDoorPlacementPolicy
    {
        public IReadOnlyList<DoorPlacementProposal> SelectDoors(IReadOnlyList<DoorPlacementOpportunity> opportunities)
        {
            if (opportunities == null)
            {
                throw new ArgumentNullException(nameof(opportunities));
            }

            var selectedOpportunity = -1;
            var selectedInterval = -1;
            var selectedWidth = float.NegativeInfinity;
            for (var opportunityIndex = 0; opportunityIndex < opportunities.Count; opportunityIndex++)
            {
                var opportunity = opportunities[opportunityIndex]
                    ?? throw new ArgumentException("Door opportunities may not contain null.", nameof(opportunities));
                for (var intervalIndex = 0; intervalIndex < opportunity.SafeCenterIntervals.Count; intervalIndex++)
                {
                    var interval = opportunity.SafeCenterIntervals[intervalIndex];
                    var width = interval.Max - interval.Min;
                    if (width > selectedWidth)
                    {
                        selectedOpportunity = opportunityIndex;
                        selectedInterval = intervalIndex;
                        selectedWidth = width;
                    }
                }
            }

            if (selectedOpportunity < 0)
            {
                return Array.Empty<DoorPlacementProposal>();
            }

            var selected = opportunities[selectedOpportunity].SafeCenterIntervals[selectedInterval];
            var center = selected.Min + (selected.Max - selected.Min) * 0.5f;
            return new[] { new DoorPlacementProposal(selectedOpportunity, center) };
        }
    }
}
