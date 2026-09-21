using System;
using Octoplug.Audio;
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
        public static event Action GraphChanged;

        /// <summary>
        /// Connects <paramref name="plug"/> to <paramref name="socket"/> if
        /// the socket is free. Disconnects any existing pairing on
        /// <paramref name="plug"/> first, so the plug never ends up paired
        /// to two sockets at once. Returns false (no state change) if the
        /// socket is already occupied by a different plug.
        /// </summary>
        public static bool Connect(PlugConnector plug, SocketConnector socket)
        {
            if (plug == null || socket == null || !socket.IsActiveSocket)
            {
                return false;
            }

            if (socket.IsConnected && socket.ConnectedPlug != plug)
            {
                return false;
            }

            if (plug.ConnectedSocket == socket)
            {
                return true;
            }

            DisconnectInternal(plug, false);
            plug.AssignSocket(socket);
            socket.AssignPlug(plug);
            GraphChanged?.Invoke();
            GameplaySfxPlayer.Play(GameplaySfxCue.PlugConnect);
            return true;
        }

        /// <summary>
        /// Clears <paramref name="plug"/>'s pairing and, symmetrically, the
        /// socket's reference back to it. No-op if the plug is not
        /// currently connected.
        /// </summary>
        public static void Disconnect(PlugConnector plug)
        {
            if (DisconnectInternal(plug, true))
            {
                GameplaySfxPlayer.Play(GameplaySfxCue.PlugDisconnect);
            }
        }

        /// <summary>
        /// Notifies graph-derived presenters and powered-state refresh paths that
        /// effective topology changed without replacing a Plug/Socket pairing.
        /// </summary>
        public static void NotifyTopologyChanged()
        {
            GraphChanged?.Invoke();
        }

        private static bool DisconnectInternal(
            PlugConnector plug,
            bool notify)
        {
            if (plug == null || !plug.IsConnected)
            {
                return false;
            }

            var socket = plug.ConnectedSocket;
            plug.ClearSocket();

            if (socket != null && socket.ConnectedPlug == plug)
            {
                socket.ClearPlug();
            }

            if (notify)
            {
                GraphChanged?.Invoke();
            }

            return true;
        }
    }
}
