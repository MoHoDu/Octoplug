using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    public static class DefaultRoomContentBalance
    {
        public static IReadOnlyList<StarterRoomConfigRecord> GetStarterConfigs()
        {
            return new[]
            {
                new StarterRoomConfigRecord(
                    starterRoomIndex: 1,
                    enabled: true,
                    tvCount: 0,
                    fanCount: 1,
                    heaterCount: 0,
                    inductionCount: 0,
                    airConditionerCount: 0,
                    wallOutletCount: 1,
                    wallOutletSocketMin: 1,
                    wallOutletSocketMax: 2,
                    productPowerMin: 2,
                    productPowerMax: 3),
                new StarterRoomConfigRecord(
                    starterRoomIndex: 2,
                    enabled: true,
                    tvCount: 1,
                    fanCount: 0,
                    heaterCount: 0,
                    inductionCount: 0,
                    airConditionerCount: 0,
                    wallOutletCount: 1,
                    wallOutletSocketMin: 1,
                    wallOutletSocketMax: 2,
                    productPowerMin: 2,
                    productPowerMax: 3)
            };
        }

        public static IReadOnlyList<RoomConfigRecord> GetDefaultConfigs()
        {
            return new[]
            {
                new RoomConfigRecord("RC001", true, 1, 10, 5, 5, false, 1, 0, 0, 0, 0, 2, 2),
                new RoomConfigRecord("RC002", true, 2, 10, 6, 5, false, 0, 1, 0, 0, 0, 2, 3),
                new RoomConfigRecord("RC003", true, 3, 10, 5, 6, false, 0, 0, 1, 0, 0, 2, 3),
                new RoomConfigRecord("RC004", true, 4, 10, 6, 6, false, 1, 1, 0, 0, 0, 2, 4),
                new RoomConfigRecord("RC005", true, 5, 10, 7, 6, false, 0, 0, 0, 1, 0, 3, 4),
                new RoomConfigRecord("RC006", true, 6, 10, 6, 7, false, 0, 0, 0, 0, 1, 3, 5),
                new RoomConfigRecord("RC007", true, 7, 5,  8, 8, false, 1, 0, 1, 1, 0, 3, 5)
            };
        }
    }
}
