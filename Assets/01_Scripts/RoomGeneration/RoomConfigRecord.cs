using System;

namespace Octoplug.RoomGeneration
{
    public sealed class RoomConfigRecord
    {
        public RoomConfigRecord(
            string id,
            bool enabled,
            int minRoomCount,
            int weight,
            int widthWorld,
            int heightWorld,
            bool allowRotation,
            int tvCount,
            int fanCount,
            int heaterCount,
            int inductionCount,
            int airConditionerCount,
            int wallOutletSocketMin,
            int wallOutletSocketMax)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("RoomConfigID is required.", nameof(id));
            }

            Id = id;
            Enabled = enabled;
            MinRoomCount = minRoomCount;
            Weight = weight;
            WidthWorld = widthWorld;
            HeightWorld = heightWorld;
            AllowRotation = allowRotation;
            TvCount = tvCount;
            FanCount = fanCount;
            HeaterCount = heaterCount;
            InductionCount = inductionCount;
            AirConditionerCount = airConditionerCount;
            WallOutletSocketMin = wallOutletSocketMin;
            WallOutletSocketMax = wallOutletSocketMax;
        }

        public string Id { get; }
        public bool Enabled { get; }
        public int MinRoomCount { get; }
        public int Weight { get; }
        public int WidthWorld { get; }
        public int HeightWorld { get; }
        public bool AllowRotation { get; }
        public int TvCount { get; }
        public int FanCount { get; }
        public int HeaterCount { get; }
        public int InductionCount { get; }
        public int AirConditionerCount { get; }
        public int WallOutletSocketMin { get; }
        public int WallOutletSocketMax { get; }
    }
}
