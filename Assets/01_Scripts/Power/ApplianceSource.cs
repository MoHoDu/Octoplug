using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Attaches to a Product prefab root (TV, Fan, Heater, Induction,
    /// Air_Conditioner). Exposes the product's power consumption and a
    /// reference to its nested Cable so future connection-validation code
    /// never needs to search the hierarchy by name.
    ///
    /// Not a ScriptableObject on purpose: real product data is expected to
    /// come from an external sheet later, and a plain per-instance
    /// MonoBehaviour keeps that migration from being blocked by asset
    /// identity/sharing concerns an SO would introduce.
    /// </summary>
    public class ApplianceSource : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Power drawn while operating, in watts. Demo placeholder — needs real balance data.")]
        private float powerConsumptionWatts;

        [SerializeField]
        [Tooltip("This product's nested Cable.prefab instance.")]
        private CableInfo cable;

        [SerializeField]
        [Tooltip("Resident-need tag(s) this product serves. Multi-select is supported structurally even though current products only set one.")]
        private UsageType usageTypes;

        [SerializeField]
        [Tooltip("How many residents can use this product at once. Demo placeholder — needs real balance data.")]
        private int capacity = 1;

        [SerializeField]
        [Tooltip("Seconds of use until a resident's need is satisfied. Demo placeholder — needs real balance data.")]
        private float satisfactionDurationSeconds;

        /// <summary>
        /// Connected (physical Plug/Socket pairing) AND Power Validation
        /// passed. Only <see cref="SetPowered"/> — called from the
        /// connect/disconnect flow in
        /// <see cref="Octoplug.Power.Cable.CableRoutingController"/> — may
        /// change this; rendering code (e.g. <see cref="Octoplug.Power.Cable.CablePowerFlowEffect"/>)
        /// must only read it.
        /// </summary>
        public bool IsPowered { get; private set; }

        public float PowerConsumptionWatts => powerConsumptionWatts;
        public CableInfo Cable => cable;
        public UsageType UsageTypes => usageTypes;
        public int Capacity => capacity;
        public float SatisfactionDurationSeconds => satisfactionDurationSeconds;
        public bool IsConnected => cable != null && cable.Plug != null && cable.Plug.IsConnected;

        public event System.Action<ApplianceSource, bool> PoweredChanged;

        /// <summary>Set by the power-validation flow only; see <see cref="IsPowered"/>.</summary>
        public void SetPowered(bool powered)
        {
            if (IsPowered == powered)
            {
                return;
            }

            IsPowered = powered;
            PoweredChanged?.Invoke(this, powered);
        }
    }
}
