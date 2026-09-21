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
                    list.Add(new StarterRoomConfigRecord(
                        row.starterRoomIndex,
                        row.enabled,
                        row.tvCount,
                        row.fanCount,
                        row.heaterCount,
                        row.inductionCount,
                        row.airConditionerCount,
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

