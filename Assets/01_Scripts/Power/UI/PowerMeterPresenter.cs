using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// The single icon-meter rule shared by every power display in this
    /// project (House, and each PowerStrip's own PowerInfo): exactly
    /// <c>Allowed</c> icons active, the first <c>Usage</c> active icons in
    /// the "used" color, the rest of the active range in the "unused"
    /// color, everything beyond <c>Allowed</c> inactive. Never invents,
    /// clamps, or partially represents a pool too small for
    /// <c>Allowed</c> — that is reported as a Human Setup problem by the
    /// caller, not silently worked around here.
    /// </summary>
    internal static class PowerMeterPresenter
    {
        /// <summary>
        /// Applies the meter rule to <paramref name="icons"/>. Returns false
        /// (no icon touched) with a human-readable <paramref name="error"/>
        /// if the pool cannot faithfully represent the given values —
        /// non-integer Usage/Allowed, a missing icon, or (most commonly) an
        /// icon pool smaller than <c>Allowed</c> requires.
        /// </summary>
        public static bool TryApply(
            float usage,
            float allowed,
            IReadOnlyList<Image> icons,
            Color usedColor,
            Color unusedColor,
            out string error)
        {
            if (icons == null || icons.Count == 0)
            {
                error = "no icons are assigned";
                return false;
            }

            if (!TryGetWholeIconCount(usage, out var usageCount)
                || !TryGetWholeIconCount(allowed, out var allowedCount)
                || allowedCount > icons.Count)
            {
                error = $"cannot represent Usage={usage} and Allowed={allowed} with {icons.Count} icons";
                return false;
            }

            for (var i = 0; i < icons.Count; i++)
            {
                var icon = icons[i];
                if (icon == null)
                {
                    error = $"icon {i} is not assigned";
                    return false;
                }

                var active = i < allowedCount;
                icon.gameObject.SetActive(active);
                if (active)
                {
                    icon.color = i < usageCount ? usedColor : unusedColor;
                }
            }

            error = null;
            return true;
        }

        private static bool TryGetWholeIconCount(float value, out int count)
        {
            count = Mathf.RoundToInt(value);
            return value >= 0f && Mathf.Approximately(value, count);
        }
    }
}
