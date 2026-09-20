using System;
using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public sealed class WeightedDemandSelector
    {
        private const ResidentNeedType AllNeeds =
            ResidentNeedType.Cooling |
            ResidentNeedType.Heating |
            ResidentNeedType.Meal |
            ResidentNeedType.Fun;

        private readonly IReadOnlyList<DemandBalanceRecord> _records;
        private readonly IRandomSource _randomSource;

        public WeightedDemandSelector(DemandBalanceCatalog catalog, IRandomSource randomSource)
        {
            _records = catalog?.Records ?? throw new ArgumentNullException(nameof(catalog));
            _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        public DemandBalanceRecord Select(int unlockedRoomCount)
        {
            if (TrySelect(unlockedRoomCount, AllNeeds, out var selected))
            {
                return selected;
            }

            throw new InvalidOperationException("No demand is eligible for the current room count and available needs.");
        }

        public bool TrySelect(
            int unlockedRoomCount,
            ResidentNeedType availableNeeds,
            out DemandBalanceRecord selected)
        {
            if (unlockedRoomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(unlockedRoomCount));
            }

            var totalWeight = 0;
            foreach (var record in _records)
            {
                if (IsEligible(record, unlockedRoomCount, availableNeeds))
                {
                    checked
                    {
                        totalWeight += record.Weight;
                    }
                }
            }

            if (totalWeight == 0)
            {
                selected = null;
                return false;
            }

            var selection = _randomSource.Next(totalWeight);
            if (selection < 0 || selection >= totalWeight)
            {
                throw new InvalidOperationException("Random source returned a value outside the requested range.");
            }

            foreach (var record in _records)
            {
                if (!IsEligible(record, unlockedRoomCount, availableNeeds))
                {
                    continue;
                }

                if (selection < record.Weight)
                {
                    selected = record;
                    return true;
                }

                selection -= record.Weight;
            }

            throw new InvalidOperationException("Weighted demand selection failed.");
        }

        private static bool IsEligible(
            DemandBalanceRecord record,
            int unlockedRoomCount,
            ResidentNeedType availableNeeds)
        {
            if (!record.Enabled || record.RequiredRoomCount > unlockedRoomCount)
            {
                return false;
            }

            foreach (var need in record.Needs)
            {
                if ((availableNeeds & need) != need)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
