using System;
using System.Collections.Generic;
using Octoplug.Power.Connection;
using UnityEngine;

namespace Octoplug.Power
{
    /// <summary>
    /// Fixed House-backed Socket source. Socket capacity is independent of House
    /// allowance, and runtime increases reveal only persistent authored modules.
    /// </summary>
    public class WallOutlet : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Initial number of usable authored sockets.")]
        [Range(1, 5)]
        private int initialSocketCount = 1;

        [SerializeField]
        [Tooltip("All persistent authored sockets in Socket01 through Socket05 order.")]
        private List<SocketConnector> sockets = new();

        [SerializeField]
        private SocketModuleLayout socketLayout;

        private int activeSocketCount;
        private bool initialized;

        public static event Action<WallOutlet, int, int> ActiveSocketCountChanged;

        public int InitialSocketCount => initialSocketCount;
        public int ActiveSocketCount
        {
            get
            {
                EnsureInitialized();
                return activeSocketCount;
            }
        }
        public IReadOnlyList<SocketConnector> Sockets => sockets;

        public IEnumerable<SocketConnector> ActiveSockets
        {
            get
            {
                EnsureInitialized();
                var count = Mathf.Min(activeSocketCount, sockets.Count);
                for (var i = 0; i < count; i++)
                {
                    if (sockets[i] != null)
                    {
                        yield return sockets[i];
                    }
                }
            }
        }

        public bool IsSocketActive(SocketConnector socket)
        {
            EnsureInitialized();
            if (socket == null)
            {
                return false;
            }

            var count = Mathf.Min(activeSocketCount, sockets.Count);
            for (var i = 0; i < count; i++)
            {
                if (sockets[i] == socket)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsSocketIndexActive(int index)
        {
            EnsureInitialized();
            return index >= 0 && index < activeSocketCount && index < sockets.Count;
        }

        public bool TryUpgradeActiveSocketCount(
            out WallOutletSocketCountFailure failure)
        {
            EnsureInitialized();
            if (activeSocketCount >= 5)
            {
                failure = WallOutletSocketCountFailure.MaximumReached;
                return false;
            }

            return TrySetActiveSocketCount(activeSocketCount + 1, out failure);
        }

        private bool TrySetActiveSocketCount(
            int requestedCount,
            out WallOutletSocketCountFailure failure)
        {
            failure = WallOutletSocketCountFailure.None;
            if (requestedCount < 1 || requestedCount > 5)
            {
                failure = WallOutletSocketCountFailure.OutOfRange;
                return false;
            }

            if (requestedCount == activeSocketCount)
            {
                return true;
            }

            if (requestedCount < activeSocketCount)
            {
                failure = WallOutletSocketCountFailure.SocketCountDecreaseUnsupported;
                return false;
            }

            if (socketLayout == null
                || sockets.Count < requestedCount
                || !socketLayout.TryValidate(requestedCount))
            {
                failure = WallOutletSocketCountFailure.MissingAuthoredConfiguration;
                return false;
            }

            for (var i = 0; i < sockets.Count; i++)
            {
                if (sockets[i] == null || socketLayout.GetSocket(i) != sockets[i])
                {
                    failure = WallOutletSocketCountFailure.MissingAuthoredConfiguration;
                    return false;
                }
            }

            var oldCount = activeSocketCount;
            activeSocketCount = requestedCount;
            socketLayout.Apply(requestedCount);
            ActiveSocketCountChanged?.Invoke(this, oldCount, requestedCount);
            PlugSocketConnection.NotifyTopologyChanged();
            return true;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            activeSocketCount = Mathf.Clamp(
                initialSocketCount,
                1,
                Mathf.Max(1, sockets.Count));
            if (socketLayout != null
                && socketLayout.TryValidate(activeSocketCount))
            {
                socketLayout.Apply(activeSocketCount);
            }
        }

        private void OnValidate()
        {
            initialSocketCount = Mathf.Clamp(initialSocketCount, 1, 5);
        }
    }
}
