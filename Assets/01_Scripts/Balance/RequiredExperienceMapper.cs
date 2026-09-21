using System.Collections.Generic;
using Octoplug.Balance;

namespace Octoplug.ResidentDemand
{
    public static class RequiredExperienceMapper
    {
        public static RequiredExperienceTable Create()
        {
            var rows = new List<RequiredExperienceAuthoringRow>();
            var archive = BalanceRegistry.Instance;
            if (archive != null)
            {
                foreach (var row in archive.ProgressionRows)
                {
                    rows.Add(new RequiredExperienceAuthoringRow(row.roomCount, row.requiredEXP));
                }
            }
            return RequiredExperienceImporter.FromAuthoringRows(rows);
        }
    }
}
