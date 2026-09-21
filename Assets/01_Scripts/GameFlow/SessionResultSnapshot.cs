using System;

namespace Octoplug.GameFlow
{
    public readonly struct SessionResultSnapshot : IEquatable<SessionResultSnapshot>
    {
        public SessionResultSnapshot(
            int finalRoomCount,
            int solvedDemandCount,
            int failedDemandCount)
        {
            if (finalRoomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(finalRoomCount));
            }

            if (solvedDemandCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(solvedDemandCount));
            }

            if (failedDemandCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failedDemandCount));
            }

            FinalRoomCount = finalRoomCount;
            SolvedDemandCount = solvedDemandCount;
            FailedDemandCount = failedDemandCount;
        }

        public int FinalRoomCount { get; }
        public int SolvedDemandCount { get; }
        public int FailedDemandCount { get; }

        public bool Equals(SessionResultSnapshot other)
        {
            return FinalRoomCount == other.FinalRoomCount &&
                   SolvedDemandCount == other.SolvedDemandCount &&
                   FailedDemandCount == other.FailedDemandCount;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionResultSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                FinalRoomCount,
                SolvedDemandCount,
                FailedDemandCount);
        }
    }
}
