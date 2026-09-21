using System;
using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public static class DemandAuthoringMapper
    {
        public static DemandBalanceRecord Map(DemandAuthoringRow row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            var needs = new List<ResidentNeedType>(2)
            {
                MapNeed(row.FirstNeed),
            };

            if (!string.IsNullOrWhiteSpace(row.SecondNeed))
            {
                needs.Add(MapNeed(row.SecondNeed));
            }

            return new DemandBalanceRecord(
                row.Id,
                row.Enabled,
                row.RequiredRoomCount,
                row.Weight,
                needs,
                row.SatisfactionFillSeconds,
                row.PatienceFillSeconds,
                row.ExperienceReward,
                row.GlobalSatisfactionOnSuccess,
                row.GlobalSatisfactionOnFailure,
                row.CooldownSeconds);
        }

        public static ResidentNeedType MapNeed(string authoringValue)
        {
            switch (authoringValue)
            {
                case "Cold":
                    return ResidentNeedType.Cooling;
                case "Hot":
                    return ResidentNeedType.Heating;
                case "Food":
                    return ResidentNeedType.Meal;
                case "Fun":
                    return ResidentNeedType.Fun;
                default:
                    throw new ArgumentException(
                        $"Unsupported resident need authoring value '{authoringValue}'.",
                        nameof(authoringValue));
            }
        }
    }
}