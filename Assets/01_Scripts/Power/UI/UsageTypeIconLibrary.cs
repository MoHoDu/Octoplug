using System;
using UnityEngine;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Static UsageType-to-icon lookup table (a design/art table, not
    /// per-product data — this is why it is a ScriptableObject while
    /// <see cref="Octoplug.Power.ApplianceSource"/> deliberately is not).
    /// One asset instance is expected in the project; UI code never
    /// hardcodes a Product-name-to-icon mapping.
    /// </summary>
    [CreateAssetMenu(fileName = "UsageTypeIconLibrary", menuName = "Octoplug/Power/Usage Type Icon Library")]
    public class UsageTypeIconLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public UsageType usageType;
            public Sprite icon;
        }

        [SerializeField]
        private Entry[] entries = Array.Empty<Entry>();

        /// <summary>Looks up the icon for a single flag. Pass one flag at a time (see <see cref="UsageTypeUtility.EnumerateFlags"/> for multi-tag products).</summary>
        public Sprite GetIcon(UsageType singleFlag)
        {
            foreach (var entry in entries)
            {
                if (entry.usageType == singleFlag)
                {
                    return entry.icon;
                }
            }

            return null;
        }
    }
}
