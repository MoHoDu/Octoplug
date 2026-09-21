using System;

namespace Octoplug.RoomGeneration
{
    public sealed class SocketCountWeightRecord
    {
        public const int MinimumSocketCount = 1;
        public const int MaximumSocketCount = 5;

        public SocketCountWeightRecord(int socketCount, int weight)
        {
            if (socketCount < MinimumSocketCount || socketCount > MaximumSocketCount)
            {
                throw new ArgumentOutOfRangeException(nameof(socketCount));
            }

            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            SocketCount = socketCount;
            Weight = weight;
        }

        public int SocketCount { get; }
        public int Weight { get; }
    }
}
