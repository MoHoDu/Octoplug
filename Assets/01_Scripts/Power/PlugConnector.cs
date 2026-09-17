using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// The draggable plug endpoint (the existing "Plug" child of
    /// Cable.prefab). Holds only connection state; drag/drop, snapping,
    /// and validation are implemented in a later stage (P0-1C+).
    /// </summary>
    public class PlugConnector : MonoBehaviour, IPowerConnector
    {
        [SerializeField]
        private SocketConnector connectedSocket;

        public bool IsConnected => connectedSocket != null;

        public Transform ConnectorTransform => transform;

        public SocketConnector ConnectedSocket => connectedSocket;

        /// <summary>Raw state assignment. No capacity/length/room validation.</summary>
        public void AssignSocket(SocketConnector socket)
        {
            connectedSocket = socket;
        }

        /// <summary>Raw state clear. No side effects on the socket's own state.</summary>
        public void ClearSocket()
        {
            connectedSocket = null;
        }
    }
}
