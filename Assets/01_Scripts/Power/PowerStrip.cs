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
        [Tooltip("Total power this strip can host across its own sockets, in watts. Demo placeholder — needs real balance data.")]
        private float allowedPowerWatts;

        [SerializeField]
        [Tooltip("This strip's own nested Cable.prefab instance (plugs into a wall outlet or another strip).")]
        private CableInfo cable;

        [SerializeField]
        [Tooltip("The existing 'Socket'/'Socket (N)' children this strip offers.")]
        private List<SocketConnector> sockets = new();

        public float AllowedPowerWatts => allowedPowerWatts;
        public CableInfo Cable => cable;
        public IReadOnlyList<SocketConnector> Sockets => sockets;
    }
}
