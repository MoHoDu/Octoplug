using System.Collections.Generic;
using Octoplug.Balance;
using Octoplug.ResidentDemand;

namespace Octoplug.Balance
{
    public static class DemandBalanceRegistryMapper
    {
        public static DemandBalanceCatalog MapAll()
        {
            var rows = new List<DemandAuthoringRow>();
            var archive = BalanceRegistry.Instance;
            if (archive != null)
            {
                foreach (var row in archive.DemandRows)
                {
                    rows.Add(new DemandAuthoringRow(
                        row.id,
                        row.enabled,
                        row.requiredRoomCount,
                        row.weight,
                        row.firstNeed,
                        row.secondNeed,
                        row.satisfactionFillSeconds,
                        row.patienceFillSeconds,
                        row.experienceReward,
                        row.globalSatisfactionOnSuccess,
                        row.globalSatisfactionOnFailure,
                        row.cooldownSeconds
                    ));
                }
            }
            return DemandBalanceCatalog.FromAuthoringRows(rows);
        }
    }
}