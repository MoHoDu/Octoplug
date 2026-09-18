using Octoplug.Power;

namespace Octoplug.Power.Connection
{
    /// <summary>
    /// The single place that mutates <see cref="PlugConnector"/>/
    /// <see cref="SocketConnector"/> pairing state, so both sides always
    /// change together. Callers (drag/drop, later power validation) must
    /// go through this instead of calling AssignSocket/AssignPlug/
    /// ClearSocket/ClearPlug directly, or a one-sided state is possible.
    /// </summary>
    public static class PlugSocketConnection
    {
        /// <summary>
        /// Connects <paramref name="plug"/> to <paramref name="socket"/> if
        /// the socket is free. Disconnects any existing pairing on
        /// <paramref name="plug"/> first, so the plug never ends up paired
        /// to two sockets at once. Returns false (no state change) if the
        /// socket is already occupied by a different plug.
        /// </summary>
        public static bool Connect(PlugConnector plug, SocketConnector socket)
        {
            if (plug == null || socket == null)
            {
                return false;
            }

            if (socket.IsConnected && socket.ConnectedPlug != plug)
            {
                return false;
            }

            if (plug.ConnectedSocket != socket)
            {
                Disconnect(plug);
            }

            plug.AssignSocket(socket);
            socket.AssignPlug(plug);
            return true;
        }

        /// <summary>
        /// Clears <paramref name="plug"/>'s pairing and, symmetrically, the
        /// socket's reference back to it. No-op if the plug is not
        /// currently connected.
        /// </summary>
        public static void Disconnect(PlugConnector plug)
        {
            if (plug == null || !plug.IsConnected)
            {
                return;
            }

            var socket = plug.ConnectedSocket;
            plug.ClearSocket();

            if (socket != null && socket.ConnectedPlug == plug)
            {
                socket.ClearPlug();
            }
        }
    }
}
