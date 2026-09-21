using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    public sealed class WeightedSocketCountSelector
    {
        private readonly IReadOnlyList<SocketCountWeightRecord> _records;

        public WeightedSocketCountSelector(SocketCountWeightCatalog catalog)
        {
            _records = catalog?.Records ?? throw new ArgumentNullException(nameof(catalog));
        }

        public int GetTotalWeight(int minimumSocketCount, int maximumSocketCount)
        {
            ValidateRange(minimumSocketCount, maximumSocketCount);

            var totalWeight = 0;
            foreach (var record in _records)
            {
                if (!IsInRange(record, minimumSocketCount, maximumSocketCount))
                {
                    continue;
                }

                checked
                {
                    totalWeight += record.Weight;
                }
            }

            if (totalWeight == 0)
            {
                throw new InvalidOperationException("No socket-count record is eligible for the requested range.");
            }

            return totalWeight;
        }

        public int Select(int minimumSocketCount, int maximumSocketCount, int roll)
        {
            var totalWeight = GetTotalWeight(minimumSocketCount, maximumSocketCount);
            if (roll < 0 || roll >= totalWeight)
            {
                throw new ArgumentOutOfRangeException(nameof(roll));
            }

            foreach (var record in _records)
            {
                if (!IsInRange(record, minimumSocketCount, maximumSocketCount))
                {
                    continue;
                }

                if (roll < record.Weight)
                {
                    return record.SocketCount;
                }

                roll -= record.Weight;
            }

            throw new InvalidOperationException("Weighted socket-count selection failed.");
        }

        public IReadOnlyList<SocketCountWeightRecord> GetEligibleRecords(
            int minimumSocketCount,
            int maximumSocketCount)
        {
            ValidateRange(minimumSocketCount, maximumSocketCount);

            var eligible = new List<SocketCountWeightRecord>();
            foreach (var record in _records)
            {
                if (IsInRange(record, minimumSocketCount, maximumSocketCount))
                {
                    eligible.Add(record);
                }
            }

            if (eligible.Count == 0)
            {
                throw new InvalidOperationException("No socket-count record is eligible for the requested range.");
            }

            return eligible;
        }

        private static bool IsInRange(
            SocketCountWeightRecord record,
            int minimumSocketCount,
            int maximumSocketCount)
        {
            return record.SocketCount >= minimumSocketCount
                && record.SocketCount <= maximumSocketCount;
        }

        private static void ValidateRange(int minimumSocketCount, int maximumSocketCount)
        {
            if (minimumSocketCount < SocketCountWeightRecord.MinimumSocketCount
                || minimumSocketCount > SocketCountWeightRecord.MaximumSocketCount)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumSocketCount));
            }

            if (maximumSocketCount < SocketCountWeightRecord.MinimumSocketCount
                || maximumSocketCount > SocketCountWeightRecord.MaximumSocketCount)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSocketCount));
            }

            if (minimumSocketCount > maximumSocketCount)
            {
                throw new ArgumentException("Minimum socket count cannot exceed maximum socket count.");
            }
        }
    }
}
