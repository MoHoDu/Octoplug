using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Attaches to a Wall_Outlet prefab root. A wall outlet is fixed, has
    /// no cable of its own, and no allowed-power limit of its own — only
    /// the sockets it offers.
    /// </summary>
    public class WallOutlet : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The existing 'Socket'/'Socket (N)' children this outlet offers.")]
        private List<SocketConnector> sockets = new();

        public IReadOnlyList<SocketConnector> Sockets => sockets;
    }
}
