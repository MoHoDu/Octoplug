using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Attaches to a Product prefab root (TV, Fan, Heater, Induction,
    /// Air_Conditioner). Exposes the product's power consumption and a
    /// reference to its nested Cable so future connection-validation code
    /// never needs to search the hierarchy by name.
    /// </summary>
    public class ApplianceSource : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Power drawn while operating, in watts. Demo placeholder — needs real balance data.")]
        private float powerConsumptionWatts;

        [SerializeField]
        [Tooltip("This product's nested Cable.prefab instance.")]
        private CableInfo cable;

        public float PowerConsumptionWatts => powerConsumptionWatts;
        public CableInfo Cable => cable;
    }
}
