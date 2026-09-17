using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Marks a single socket slot on a WallOutlet or PowerStrip (the
    /// existing "Socket" child transforms). Holds only connection state;
    /// drag/drop, validation, and power checks are implemented in a later
    /// stage (P0-1C+).
    /// </summary>
    public class SocketConnector : MonoBehaviour, IPowerConnector
    {
        [SerializeField]
        private PlugConnector connectedPlug;

        public bool IsConnected => connectedPlug != null;

        public Transform ConnectorTransform => transform;

        public PlugConnector ConnectedPlug => connectedPlug;

        /// <summary>Raw state assignment. No capacity/length/room validation.</summary>
        public void AssignPlug(PlugConnector plug)
        {
            connectedPlug = plug;
        }

        /// <summary>Raw state clear. No side effects on the plug's own state.</summary>
        public void ClearPlug()
        {
            connectedPlug = null;
        }
    }
}
