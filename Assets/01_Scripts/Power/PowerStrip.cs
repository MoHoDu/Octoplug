using System;
using System.Collections.Generic;
using Octoplug.Power.Connection;
using Octoplug.Power.Grid;
using UnityEngine;
using UnityEngine.Serialization;

namespace Octoplug.Power
{
    /// <summary>
    /// Persistent PowerStrip runtime state. Socket capacity and electrical
    /// allowance are independent: changing one never initializes or mutates
    /// the other, and authored Socket components are never replaced.
    /// </summary>
    public class PowerStrip : MonoBehaviour
    {
        [FormerlySerializedAs("allowedPowerWatts")]
        [SerializeField]
        [Tooltip("Initial power this strip can host across its own sockets, in watts.")]
        private float initialAllowedPowerWatts;

        [SerializeField]
        [Tooltip("Initial number of usable authored sockets. Independent of Allowed Power.")]
        [Range(1, 5)]
        private int initialSocketCount = 1;

        [SerializeField]
        [Tooltip("Finalized maximum allowance for this strip, independent of socket count and House power.")]
        private float maxAllowedPowerWatts = 10f;

        [SerializeField]
        [Tooltip("This strip's own nested Cable.prefab instance (plugs into a wall outlet or another strip).")]
        private CableInfo cable;

        [SerializeField]
        [Tooltip("All persistent authored sockets in Socket01 through Socket05 order.")]
        private List<SocketConnector> sockets = new();

        [SerializeField]
        private PowerStripSocketLayout socketLayout;

        [SerializeField]
        private PlacementFootprint placementFootprint;

        private float allowedPowerWatts;
        private int activeSocketCount;
        private bool initialized;

        public static event Action<PowerStrip> AllowanceChanged;
        public static event Action<PowerStrip, int, int> ActiveSocketCountChanged;

        public float InitialAllowedPowerWatts => initialAllowedPowerWatts;
        public float AllowedPowerWatts => allowedPowerWatts;
        public float MaxAllowedPowerWatts => maxAllowedPowerWatts;
        public int InitialSocketCount => initialSocketCount;
        public int ActiveSocketCount => activeSocketCount;
        public CableInfo Cable => cable;
        public IReadOnlyList<SocketConnector> Sockets => sockets;

        public IEnumerable<SocketConnector> ActiveSockets
        {
            get
            {
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

        /// <summary>
        /// Connected (this strip's own Plug is paired to an upstream Socket)
        /// AND that upstream source is itself live.
        /// </summary>
        public bool IsPowered { get; private set; }

        public bool IsSocketActive(SocketConnector socket)
        {
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
            return index >= 0 && index < activeSocketCount && index < sockets.Count;
        }

        public bool TrySetActiveSocketCount(
            int requestedCount,
            out PowerStripSocketCountFailure failure)
        {
            failure = PowerStripSocketCountFailure.None;
            if (requestedCount < 1 || requestedCount > 5)
            {
                failure = PowerStripSocketCountFailure.OutOfRange;
                return false;
            }

            if (requestedCount == activeSocketCount)
            {
                return true;
            }

            if (requestedCount < activeSocketCount)
            {
                failure = PowerStripSocketCountFailure.SocketCountDecreaseUnsupported;
                return false;
            }

            if (socketLayout == null
                || placementFootprint == null
                || sockets.Count < requestedCount
                || !socketLayout.TryValidate(requestedCount))
            {
                failure = PowerStripSocketCountFailure.MissingAuthoredConfiguration;
                return false;
            }

            var service = CableRoutingGridService.Instance;
            var grid = service != null ? service.Grid : null;
            if (grid == null)
            {
                failure = PowerStripSocketCountFailure.PlacementServiceUnavailable;
                return false;
            }

            if (!placementFootprint.TryReserveAt(grid, transform.position, requestedCount))
            {
                failure = PowerStripSocketCountFailure.PlacementUnavailable;
                return false;
            }

            var oldCount = activeSocketCount;
            activeSocketCount = requestedCount;
            socketLayout.Apply(requestedCount);
            ActiveSocketCountChanged?.Invoke(this, oldCount, requestedCount);
            PlugSocketConnection.NotifyTopologyChanged();
            return true;
        }

        public void SetAllowedPowerWatts(float value)
        {
            EnsureInitialized();
            if (Mathf.Approximately(allowedPowerWatts, value))
            {
                return;
            }

            allowedPowerWatts = value;
            AllowanceChanged?.Invoke(this);
        }

        public void SetPowered(bool powered)
        {
            IsPowered = powered;
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
            allowedPowerWatts = initialAllowedPowerWatts;
            activeSocketCount = Mathf.Clamp(initialSocketCount, 1, Mathf.Max(1, sockets.Count));
            if (socketLayout != null && socketLayout.TryValidate(activeSocketCount))
            {
                socketLayout.Apply(activeSocketCount);
            }
        }

        private void OnValidate()
        {
            initialSocketCount = Mathf.Clamp(initialSocketCount, 1, 5);
            if (Application.isPlaying)
            {
                AllowanceChanged?.Invoke(this);
            }
        }
    }
}
