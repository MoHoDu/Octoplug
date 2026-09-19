using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power.Grid
{
    /// <summary>
    /// Describes and owns the placement-only grid reservation for a Product or
    /// PowerStrip. Cable traversal walkability remains independent.
    /// </summary>
    public class PlacementFootprint : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Preferred source of the placement bounds for fixed-size objects.")]
        private Collider2D boundsCollider;

        [SerializeField]
        [Tooltip("Optional bounds source used by fixed-size objects when no Collider2D is assigned.")]
        private Renderer boundsRenderer;

        [SerializeField]
        [Tooltip("Local-space fallback size used by fixed-size objects when neither source is assigned.")]
        private Vector2 fallbackSize = Vector2.one;

        [SerializeField]
        [Tooltip("Optional authored variable-size geometry for a PowerStrip.")]
        private SocketModuleLayout powerStripLayout;

        private readonly List<GridCoord> reservedCells = new();
        private readonly List<GridCoord> candidateCells = new();
        private readonly List<GridCoord> colliderCells = new();
        private readonly List<BoxCollider2D> activeColliders = new();
        private readonly HashSet<GridCoord> uniqueCells = new();
        private CableRoutingGrid reservedGrid;
        private PowerStrip powerStrip;

        public Bounds WorldBounds
        {
            get
            {
                if (powerStripLayout != null && powerStrip != null)
                {
                    return GetWorldBounds(powerStrip.ActiveSocketCount, transform.position);
                }

                return GetFixedWorldBounds();
            }
        }

        private void Awake()
        {
            powerStrip = GetComponent<PowerStrip>();
        }

        private void OnEnable()
        {
            TryInitializeReservation();
        }

        private void Start()
        {
            var service = CableRoutingGridService.Instance;
            var grid = service != null ? service.Grid : null;
            if (grid == null)
            {
                TryInitializeReservation();
                return;
            }

            // Component Awake order on one GameObject is not authoritative.
            // Refresh once after every Awake so a PowerStrip reservation uses its
            // initialized ActiveSocketCount rather than the fixed-size fallback.
            TryReserveAt(grid, transform.position);
        }

        private void OnDisable()
        {
            ReleaseReservation();
        }

        public void GetCoveredCells(
            CableRoutingGrid grid,
            Vector2 candidateRootPosition,
            List<GridCoord> results)
        {
            var socketCount = powerStrip != null ? powerStrip.ActiveSocketCount : 0;
            GetCoveredCells(grid, candidateRootPosition, socketCount, results);
        }

        public void GetCoveredCells(
            CableRoutingGrid grid,
            Vector2 candidateRootPosition,
            int socketCount,
            List<GridCoord> results)
        {
            results.Clear();
            if (grid == null)
            {
                return;
            }

            if (powerStripLayout == null || socketCount <= 0)
            {
                var bounds = GetFixedWorldBounds();
                bounds.center += (Vector3)(candidateRootPosition - (Vector2)transform.position);
                grid.GetCellsCoveredByBounds(bounds, results);
                return;
            }

            uniqueCells.Clear();
            powerStripLayout.GetColliders(socketCount, activeColliders);
            var rootDelta = candidateRootPosition - (Vector2)transform.position;
            for (var i = 0; i < activeColliders.Count; i++)
            {
                var bounds = GetAuthoredWorldBounds(activeColliders[i]);
                bounds.center += (Vector3)rootDelta;
                colliderCells.Clear();
                grid.GetCellsCoveredByBounds(bounds, colliderCells);
                for (var cellIndex = 0; cellIndex < colliderCells.Count; cellIndex++)
                {
                    if (uniqueCells.Add(colliderCells[cellIndex]))
                    {
                        results.Add(colliderCells[cellIndex]);
                    }
                }
            }
        }

        public bool CanReserveAt(CableRoutingGrid grid, Vector2 rootPosition)
        {
            var socketCount = powerStrip != null ? powerStrip.ActiveSocketCount : 0;
            return CanReserveAt(grid, rootPosition, socketCount);
        }

        public bool CanReserveAt(CableRoutingGrid grid, Vector2 rootPosition, int socketCount)
        {
            GetCoveredCells(grid, rootPosition, socketCount, candidateCells);
            if (candidateCells.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < candidateCells.Count; i++)
            {
                if (!grid.IsFreeForPlacement(candidateCells[i], this))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryReserveAt(CableRoutingGrid grid, Vector2 rootPosition)
        {
            var socketCount = powerStrip != null ? powerStrip.ActiveSocketCount : 0;
            return TryReserveAt(grid, rootPosition, socketCount);
        }

        /// <summary>
        /// Atomically replaces this owner's reservation after every prospective
        /// authored cell has been validated. A failure leaves the old cells owned.
        /// </summary>
        public bool TryReserveAt(CableRoutingGrid grid, Vector2 rootPosition, int socketCount)
        {
            if (grid == null || !CanReserveAt(grid, rootPosition, socketCount))
            {
                return false;
            }

            var previousGrid = reservedGrid;
            if (previousGrid != null)
            {
                for (var i = 0; i < reservedCells.Count; i++)
                {
                    previousGrid.SetObjectOccupied(reservedCells[i], this, false);
                }
            }

            reservedCells.Clear();
            reservedGrid = grid;
            for (var i = 0; i < candidateCells.Count; i++)
            {
                grid.SetObjectOccupied(candidateCells[i], this, true);
                reservedCells.Add(candidateCells[i]);
            }

            return reservedCells.Count > 0;
        }

        public Bounds GetWorldBounds(int socketCount, Vector2 candidateRootPosition)
        {
            if (powerStripLayout == null || socketCount <= 0)
            {
                var fixedBounds = GetFixedWorldBounds();
                fixedBounds.center += (Vector3)(candidateRootPosition - (Vector2)transform.position);
                return fixedBounds;
            }

            powerStripLayout.GetColliders(socketCount, activeColliders);
            if (activeColliders.Count == 0)
            {
                return new Bounds(candidateRootPosition, Vector3.zero);
            }

            var aggregate = GetAuthoredWorldBounds(activeColliders[0]);
            for (var i = 1; i < activeColliders.Count; i++)
            {
                aggregate.Encapsulate(GetAuthoredWorldBounds(activeColliders[i]));
            }

            aggregate.center += (Vector3)(candidateRootPosition - (Vector2)transform.position);
            return aggregate;
        }

        public void ReleaseReservation()
        {
            if (reservedGrid != null)
            {
                for (var i = 0; i < reservedCells.Count; i++)
                {
                    reservedGrid.SetObjectOccupied(reservedCells[i], this, false);
                }
            }

            reservedCells.Clear();
            reservedGrid = null;
        }

        private Bounds GetFixedWorldBounds()
        {
            if (boundsCollider != null)
            {
                return boundsCollider.bounds;
            }

            if (boundsRenderer != null)
            {
                return boundsRenderer.bounds;
            }

            var worldSize = Vector3.Scale(
                new Vector3(fallbackSize.x, fallbackSize.y, 0f),
                transform.lossyScale);
            return new Bounds(transform.position, worldSize);
        }

        private static Bounds GetAuthoredWorldBounds(BoxCollider2D collider)
        {
            var half = collider.size * 0.5f;
            var offset = collider.offset;
            var first = collider.transform.TransformPoint(offset + new Vector2(-half.x, -half.y));
            var bounds = new Bounds(first, Vector3.zero);
            bounds.Encapsulate(collider.transform.TransformPoint(offset + new Vector2(-half.x, half.y)));
            bounds.Encapsulate(collider.transform.TransformPoint(offset + new Vector2(half.x, -half.y)));
            bounds.Encapsulate(collider.transform.TransformPoint(offset + new Vector2(half.x, half.y)));
            return bounds;
        }

        private void TryInitializeReservation()
        {
            var service = CableRoutingGridService.Instance;
            var grid = service != null ? service.Grid : null;
            if (grid == null)
            {
                return;
            }

            if (reservedGrid != null && !ReferenceEquals(reservedGrid, grid))
            {
                ReleaseReservation();
            }

            if (reservedGrid == null)
            {
                TryReserveAt(grid, transform.position);
            }
        }
    }
}
