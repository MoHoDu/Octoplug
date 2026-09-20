using System;

namespace Octoplug.ResidentDemand
{
    [Flags]
    public enum ResidentNeedType
    {
        None = 0,
        Cooling = 1 << 0,
        Heating = 1 << 1,
        Meal = 1 << 2,
        Fun = 1 << 3,
    }
}
