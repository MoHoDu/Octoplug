using System;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Whole-house allowed power budget, checked by future connection
    /// validation. One instance is expected per playable session (placed
    /// on the scene's ConnectionDirector).
    /// </summary>
    public class HousePowerBudget : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Current power the house can supply across all active connections, in watts.")]
        private float allowedPowerWatts;

        [SerializeField]
        [Tooltip("Finalized maximum house power allowance, independent of every PowerStrip.")]
        private float maxAllowedPowerWatts = 22f;

        public static event Action<HousePowerBudget> AllowanceChanged;

        public float AllowedPowerWatts => allowedPowerWatts;
        public float MaxAllowedPowerWatts => maxAllowedPowerWatts;

        public void SetAllowedPowerWatts(float value)
        {
            if (Mathf.Approximately(allowedPowerWatts, value))
            {
                return;
            }

            allowedPowerWatts = value;
            AllowanceChanged?.Invoke(this);
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                AllowanceChanged?.Invoke(this);
            }
        }
    }
}
