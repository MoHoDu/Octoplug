using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Octoplug.Power.Connection;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Binds one PowerStrip's own "PowerInfo" icon pool to its real
    /// downstream load, using the exact same rule (and the same
    /// <see cref="PowerMeterPresenter"/>) as the House meter in
    /// <see cref="PowerUiCoordinator"/>: exactly <c>AllowedPowerWatts</c>
    /// icons active, the first <c>Usage</c> active icons Orange, the rest
    /// Grey, everything beyond Allowed inactive.
    ///
    /// Self-contained per Prefab (like <see cref="Octoplug.Power.Grid.PlacementFootprint"/>
    /// or <see cref="Octoplug.Power.Cable.PowerStripHeadController"/>) —
    /// works for any number of placed PowerStrip instances with no central
    /// scene-level wiring. The unified PowerInfo source authors one icon per
    /// supported watt; a structural icon-pool shortage is reported instead of
    /// clamping, cloning, or partially showing the authoritative allowance.
    /// </summary>
    public class PowerStripPowerInfoBinding : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Defaults to the PowerStrip found on a parent (the Multitap root).")]
        private PowerStrip strip;

        [SerializeField]
        [Tooltip("This strip's own authored PowerInfo icons, in order.")]
        private List<Image> icons = new();

        [SerializeField]
        [Tooltip("Existing authored Orange icon (this strip's own PowerInfo) used only as a color reference.")]
        private Image usedColorSource;

        [SerializeField]
        [Tooltip("Existing authored Grey icon (this strip's own PowerInfo) used only as a color reference.")]
        private Image unusedColorSource;

        private Color? cachedUsedColor;
        private Color? cachedUnusedColor;
        private bool refreshQueued;
        private string lastLoggedShortageError;

        private void Awake()
        {
            if (strip == null)
            {
                strip = GetComponentInParent<PowerStrip>();
            }
        }

        private void OnEnable()
        {
            PlugSocketConnection.GraphChanged += OnGraphChanged;
            PowerStrip.AllowanceChanged += OnStripAllowanceChanged;
            PowerStrip.ActiveSocketCountChanged += OnActiveSocketCountChanged;
            CacheColorsOnce();
            QueueRefresh();
        }

        private void OnDisable()
        {
            PlugSocketConnection.GraphChanged -= OnGraphChanged;
            PowerStrip.AllowanceChanged -= OnStripAllowanceChanged;
            PowerStrip.ActiveSocketCountChanged -= OnActiveSocketCountChanged;
            refreshQueued = false;
        }

        private void CacheColorsOnce()
        {
            if (cachedUsedColor.HasValue || cachedUnusedColor.HasValue)
            {
                return;
            }

            if (usedColorSource != null)
            {
                cachedUsedColor = usedColorSource.color;
            }

            if (unusedColorSource != null)
            {
                cachedUnusedColor = unusedColorSource.color;
            }
        }

        private void OnGraphChanged()
        {
            QueueRefresh();
        }

        private void OnStripAllowanceChanged(PowerStrip changedStrip)
        {
            if (changedStrip == strip)
            {
                QueueRefresh();
            }
        }

        private void OnActiveSocketCountChanged(
            PowerStrip changedStrip,
            int oldCount,
            int newCount)
        {
            if (changedStrip == strip)
            {
                QueueRefresh();
            }
        }

        private void QueueRefresh()
        {
            if (!isActiveAndEnabled || refreshQueued)
            {
                return;
            }

            refreshQueued = true;
            StartCoroutine(RefreshAtEndOfFrame());
        }

        private IEnumerator RefreshAtEndOfFrame()
        {
            yield return null;
            refreshQueued = false;
            Refresh();
        }

        private void Refresh()
        {
            CacheColorsOnce();

            if (strip == null
                || !cachedUsedColor.HasValue
                || !cachedUnusedColor.HasValue)
            {
                return;
            }

            var usage = Connection.PowerValidationService.GetStripUsage(strip);
            var allowed = strip.AllowedPowerWatts;
            if (!PowerMeterPresenter.TryApply(usage, allowed, icons, cachedUsedColor.Value, cachedUnusedColor.Value, out var error))
            {
                // The icon-pool-too-small case (by far the common one) is a
                // structural mismatch between icons.Count and Allowed, not a
                // per-Usage condition — log it once per distinct (poolSize,
                // Allowed) pair rather than on every graph change anywhere in
                // the house, or every Connect/Disconnect floods the console
                // with an identical, already-reported message.
                var shortageKey = $"{icons.Count}:{allowed}";
                if (lastLoggedShortageError != shortageKey)
                {
                    lastLoggedShortageError = shortageKey;
                    Debug.LogError(
                        $"{name}: PowerStrip power meter {error}. Human Setup Required: this PowerInfo's icon pool must be resized to at least {Mathf.CeilToInt(allowed)} icons to display {strip.name}'s allowance correctly — not clamped or partially shown.",
                        this);
                }
            }
            else
            {
                lastLoggedShortageError = null;
            }
        }
    }
}
