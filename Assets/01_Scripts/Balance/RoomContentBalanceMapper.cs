using System.Collections.Generic;
using Octoplug.Balance;

namespace Octoplug.RoomGeneration
{
    public static class RoomContentBalanceMapper
    {
        public static IReadOnlyList<RoomConfigRecord> GetDefaultConfigs()
        {
            var list = new List<RoomConfigRecord>();
            var archive = BalanceRegistry.Instance;
            if (archive != null)
            {
                foreach (var row in archive.RoomConfigRows)
                {
                    list.Add(new RoomConfigRecord(
                        row.roomConfigId,
                        row.enabled,
                        row.minRoomCount,
                        row.weight,
                        row.widthWorld,
                        row.heightWorld,
                        row.allowRotation,
                        row.tvCount,
                        row.fanCount,
                        row.heaterCount,
                        row.inductionCount,
                        row.airConditionerCount,
                        row.wallOutletSocketMin,
                        row.wallOutletSocketMax
                    ));
                }
            }
            return list;
        }

        public static IReadOnlyList<StarterRoomConfigRecord> GetStarterConfigs()
        {
            var list = new List<StarterRoomConfigRecord>();
            var archive = BalanceRegistry.Instance;
            if (archive != null)
            {
                foreach (var row in archive.StarterConfigRows)
                {
                    int tv = 0, fan = 0, heater = 0, induction = 0, air = 0;
                    string type = row.productType?.ToLower() ?? "";
                    if (type.Contains("tv")) tv = row.productCount;
                    else if (type.Contains("fan")) fan = row.productCount;
                    else if (type.Contains("heater")) heater = row.productCount;
                    else if (type.Contains("induction")) induction = row.productCount;
                    else if (type.Contains("air")) air = row.productCount;

                    list.Add(new StarterRoomConfigRecord(
                        row.starterRoomIndex,
                        row.enabled,
                        tv,
                        fan,
                        heater,
                        induction,
                        air,
                        row.wallOutletCount,
                        row.wallOutletSocketMin,
                        row.wallOutletSocketMax,
                        row.productPowerMin,
                        row.productPowerMax
                    ));
                }
            }
            return list;
        }

        public static SocketCountWeightCatalog CreateWallOutletCatalog() { var list = new List<SocketCountWeightAuthoringRow>(); var archive = BalanceRegistry.Instance; if (archive != null) { foreach (var row in archive.WallOutletSpawnRows) { list.Add(new SocketCountWeightAuthoringRow(row.socketCount, row.weight)); } } return SocketCountWeightCatalog.FromAuthoringRows(list); }

        public static SocketCountWeightCatalog CreatePowerStripCatalog() { var list = new List<SocketCountWeightAuthoringRow>(); var archive = BalanceRegistry.Instance; if (archive != null) { foreach (var row in archive.PowerStripSpawnRows) { list.Add(new SocketCountWeightAuthoringRow(row.socketCount, row.weight)); } } return SocketCountWeightCatalog.FromAuthoringRows(list); }
    }
}
