using Octoplug.Power;

namespace Octoplug.ResidentDemand.Unity
{
    public static class UsageTypeMapper
    {
        public static ResidentNeedType Map(UsageType usageTypes)
        {
            var result = ResidentNeedType.None;
            if ((usageTypes & UsageType.Cooling) != 0)
            {
                result |= ResidentNeedType.Cooling;
            }

            if ((usageTypes & UsageType.Heating) != 0)
            {
                result |= ResidentNeedType.Heating;
            }

            if ((usageTypes & UsageType.Meal) != 0)
            {
                result |= ResidentNeedType.Meal;
            }

            if ((usageTypes & UsageType.Fun) != 0)
            {
                result |= ResidentNeedType.Fun;
            }

            return result;
        }

        public static UsageType MapSingle(ResidentNeedType needType)
        {
            switch (needType)
            {
                case ResidentNeedType.Cooling:
                    return UsageType.Cooling;
                case ResidentNeedType.Heating:
                    return UsageType.Heating;
                case ResidentNeedType.Meal:
                    return UsageType.Meal;
                case ResidentNeedType.Fun:
                    return UsageType.Fun;
                default:
                    throw new System.ArgumentOutOfRangeException(
                        nameof(needType),
                        needType,
                        "A Resident card requires exactly one Need type.");
            }
        }
    }
}
