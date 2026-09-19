using System;
using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Attaches to a Multitap prefab root. A power strip is movable, has
    /// its own outgoing Cable (to plug into a wall outlet or another
    /// strip), an allowed-power budget for what it can host, and the set
    /// of sockets it offers. Movement constraints (same room, no wall
    /// overlap) are implemented in a later stage.
    /// </summary>
    public class PowerStrip : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Current power this strip can host across its own sockets, in watts.")]
        private float allowedPowerWatts;

        [SerializeField]
        [Tooltip("Finalized maximum allowance for this strip, independent of socket count and House power.")]
        private float maxAllowedPowerWatts = 10f;

        [SerializeField]
        [Tooltip("This strip's own nested Cable.prefab instance (plugs into a wall outlet or another strip).")]
        private CableInfo cable;

        [SerializeField]
        [Tooltip("The existing 'Socket'/'Socket (N)' children this strip offers.")]
        private List<SocketConnector> sockets = new();

        public static event Action<PowerStrip> AllowanceChanged;

        public float AllowedPowerWatts => allowedPowerWatts;
        public float MaxAllowedPowerWatts => maxAllowedPowerWatts;
        public CableInfo Cable => cable;
        public IReadOnlyList<SocketConnector> Sockets => sockets;

        /// <summary>
        /// Connected (this strip's own Plug is paired to an upstream
        /// Socket) AND that upstream source is itself live. Only
        /// <see cref="SetPowered"/> — called from
        /// <see cref="Octoplug.Power.Cable.CableRoutingController"/> — may
        /// change this; downstream code must only read it.
        /// </summary>
        public bool IsPowered { get; private set; }

        public void SetAllowedPowerWatts(float value)
        {
            if (Mathf.Approximately(allowedPowerWatts, value))
            {
                return;
            }

            allowedPowerWatts = value;
            AllowanceChanged?.Invoke(this);
        }

        /// <summary>Set by the power-validation flow only; see <see cref="IsPowered"/>.</summary>
        public void SetPowered(bool powered)
        {
            IsPowered = powered;
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
