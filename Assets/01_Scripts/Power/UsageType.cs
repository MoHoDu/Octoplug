using System;
using System.Collections.Generic;

namespace Octoplug.Power
{
    /// <summary>
    /// Resident-need tags a product can serve. Flags-based so a product can
    /// carry more than one tag structurally, even though every current
    /// product prefab only sets a single flag.
    /// </summary>
    [Flags]
    public enum UsageType
    {
        None = 0,
        Cooling = 1 << 0,
        Heating = 1 << 1,
        Meal = 1 << 2,
        Fun = 1 << 3,
    }

    public static class UsageTypeUtility
    {
        private static readonly UsageType[] AllFlags =
        {
            UsageType.Cooling,
            UsageType.Heating,
            UsageType.Meal,
            UsageType.Fun,
        };

        /// <summary>Yields each single flag set within <paramref name="value"/>, in a stable order.</summary>
        public static IEnumerable<UsageType> EnumerateFlags(UsageType value)
        {
            foreach (var flag in AllFlags)
            {
                if ((value & flag) != 0)
                {
                    yield return flag;
                }
            }
        }
    }
}
