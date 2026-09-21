using System;

namespace Octoplug.RoomGeneration
{
    public sealed class StarterRoomConfigRecord
    {
        public StarterRoomConfigRecord(
            int starterRoomIndex,
            bool enabled,
            int tvCount,
            int fanCount,
            int heaterCount,
            int inductionCount,
            int airConditionerCount,
            int wallOutletCount,
            int wallOutletSocketMin,
            int wallOutletSocketMax,
            int productPowerMin,
            int productPowerMax)
        {
            StarterRoomIndex = starterRoomIndex;
            Enabled = enabled;
            TvCount = tvCount;
            FanCount = fanCount;
            HeaterCount = heaterCount;
            InductionCount = inductionCount;
            AirConditionerCount = airConditionerCount;
            WallOutletCount = wallOutletCount;
            WallOutletSocketMin = wallOutletSocketMin;
            WallOutletSocketMax = wallOutletSocketMax;
            ProductPowerMin = productPowerMin;
            ProductPowerMax = productPowerMax;
        }

        public int StarterRoomIndex { get; }
        public bool Enabled { get; }
        public int TvCount { get; }
        public int FanCount { get; }
        public int HeaterCount { get; }
        public int InductionCount { get; }
        public int AirConditionerCount { get; }
        public int WallOutletCount { get; }
        public int WallOutletSocketMin { get; }
        public int WallOutletSocketMax { get; }
        public int ProductPowerMin { get; }
        public int ProductPowerMax { get; }
    }
}
