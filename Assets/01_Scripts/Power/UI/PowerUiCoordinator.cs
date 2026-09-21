using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Octoplug.Power.Cable;
using Octoplug.Power.Connection;

namespace Octoplug.Power.UI
{
    /// <summary>
    /// Presents power-system events through the existing scene UI. This
    /// component owns no gameplay state and never creates UI objects.
    /// </summary>
    public class PowerUiCoordinator : MonoBehaviour
    {
        [SerializeField]
        private GameObject alertRoot;

        [SerializeField]
        private TMP_Text alertText;

        [SerializeField]
        [Min(0f)]
        private float alertDisplayDurationSeconds = 2f;

        [Header("House Power Meter")]
        [SerializeField]
        private HousePowerBudget houseBudget;

        [SerializeField]
        private List<Image> housePowerIcons = new();

        [SerializeField]
        [Tooltip("Existing authored Orange icon used only as a color reference.")]
        private Image houseUsedColorSource;

        [SerializeField]
        [Tooltip("Existing authored Grey icon used only as a color reference.")]
        private Image houseUnusedColorSource;

        private Coroutine hideAlertRoutine;
        private bool refreshQueued;
        private string persistentAlert;

        public static PowerUiCoordinator Instance { get; private set; }

        /// <summary>
        /// The authored Orange/Grey colors, captured once from
        /// <see cref="houseUsedColorSource"/>/<see cref="houseUnusedColorSource"/>
        /// before the very first refresh. Those two references are the same
        /// <see cref="Image"/> objects as <see cref="housePowerIcons"/>[0]/[1]
        /// — reading their live `.color` on every refresh (as the original
        /// implementation did) meant the first refresh overwrote the very
        /// source the loop reads its "authored" colors from, permanently
        /// losing Orange after refresh #1. Caching once fixes this without
        /// any new serialized reference or visual change.
        /// </summary>
        private Color? cachedUsedColor;
        private Color? cachedUnusedColor;

        private void OnEnable()
        {
            Instance = this;
            CableRoutingController.AnyConnectionRejected +=
                OnConnectionRejected;
            PlugSocketConnection.GraphChanged += OnGraphChanged;
            HousePowerBudget.AllowanceChanged += OnHouseAllowanceChanged;
            PowerStrip.AllowanceChanged += OnStripAllowanceChanged;
            CacheHouseColorsOnce();
            QueueRefresh();
        }

        private void CacheHouseColorsOnce()
        {
            if (cachedUsedColor.HasValue || cachedUnusedColor.HasValue)
            {
                return;
            }

            if (houseUsedColorSource != null)
            {
                cachedUsedColor = houseUsedColorSource.color;
            }

            if (houseUnusedColorSource != null)
            {
                cachedUnusedColor = houseUnusedColorSource.color;
            }
        }

        private void OnDisable()
        {
            CableRoutingController.AnyConnectionRejected -=
                OnConnectionRejected;
            PlugSocketConnection.GraphChanged -= OnGraphChanged;
            HousePowerBudget.AllowanceChanged -= OnHouseAllowanceChanged;
            PowerStrip.AllowanceChanged -= OnStripAllowanceChanged;
            refreshQueued = false;
            if (Instance == this)
            {
                Instance = null;
            }

            if (hideAlertRoutine != null)
            {
                StopCoroutine(hideAlertRoutine);
                hideAlertRoutine = null;
            }
        }

        public void ShowPersistent(string message)
        {
            persistentAlert = message ?? string.Empty;
            ShowAlert(persistentAlert);
        }

        public void ClearPersistent()
        {
            persistentAlert = string.Empty;
            if (hideAlertRoutine == null)
            {
                HideAlert();
            }
        }

        private void OnConnectionRejected(ConnectionFailureReason reason)
        {
            QueueRefresh();
            if (!TryGetAlertMessage(reason, out var message))
            {
                Debug.LogWarning($"{name}: no alert message is defined for connection failure reason {reason}.", this);
                return;
            }

            ShowAlert(message);
            if (hideAlertRoutine != null)
            {
                StopCoroutine(hideAlertRoutine);
            }

            hideAlertRoutine = StartCoroutine(HideAlertAfterDelay());
        }

        private void ShowAlert(string message)
        {
            if (alertRoot == null || alertText == null)
            {
                Debug.LogWarning($"{name}: PowerUiCoordinator is missing the existing Alert root or text reference.", this);
                return;
            }

            alertText.text = message;
            alertRoot.SetActive(!string.IsNullOrEmpty(message));
        }

        private void HideAlert()
        {
            if (alertText != null)
            {
                alertText.text = string.Empty;
            }

            if (alertRoot != null)
            {
                alertRoot.SetActive(false);
            }
        }

        private void OnGraphChanged()
        {
            QueueRefresh();
        }

        private void OnHouseAllowanceChanged(HousePowerBudget changedBudget)
        {
            if (changedBudget == houseBudget)
            {
                QueueRefresh();
            }
        }

        private void OnStripAllowanceChanged(PowerStrip _)
        {
            QueueRefresh();
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
            RefreshHousePowerMeter();
        }

        private void RefreshHousePowerMeter()
        {
            CacheHouseColorsOnce();

            if (houseBudget == null
                || !cachedUsedColor.HasValue
                || !cachedUnusedColor.HasValue
                || housePowerIcons == null
                || housePowerIcons.Count == 0)
            {
                return;
            }

            var usage = PowerValidationService.GetHouseUsage();
            var allowed = houseBudget.AllowedPowerWatts;
            if (!PowerMeterPresenter.TryApply(usage, allowed, housePowerIcons, cachedUsedColor.Value, cachedUnusedColor.Value, out var error))
            {
                Debug.LogError($"{name}: House power meter {error}.", this);
            }
        }

        private IEnumerator HideAlertAfterDelay()
        {
            if (alertDisplayDurationSeconds > 0f)
            {
                yield return new WaitForSeconds(alertDisplayDurationSeconds);
            }

            hideAlertRoutine = null;
            if (!string.IsNullOrEmpty(persistentAlert))
            {
                ShowAlert(persistentAlert);
            }
            else
            {
                HideAlert();
            }
        }

        private static bool TryGetAlertMessage(
            ConnectionFailureReason reason,
            out string message)
        {
            switch (reason)
            {
                case ConnectionFailureReason.HousePowerExceeded:
                    message = "집의 허용 전력을 초과했습니다.";
                    return true;
                case ConnectionFailureReason.PowerStripPowerExceeded:
                    message = "멀티탭의 허용 전력을 초과했습니다.";
                    return true;
                case ConnectionFailureReason.SelfConnection:
                    message = "같은 멀티탭에는 연결할 수 없습니다.";
                    return true;
                case ConnectionFailureReason.CircularConnection:
                    message = "순환 연결은 허용되지 않습니다.";
                    return true;
                default:
                    message = null;
                    return false;
            }
        }
    }
}
