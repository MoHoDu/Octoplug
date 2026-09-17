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
        [Tooltip("Total power the house can supply across all active connections, in watts. Demo placeholder — needs real balance data.")]
        private float allowedPowerWatts;

        public float AllowedPowerWatts => allowedPowerWatts;
    }
}
