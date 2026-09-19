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
        [Tooltip("Preferred source of the placement bounds.")]
        private Collider2D boundsCollider;

        [SerializeField]
        [Tooltip("Optional bounds source used when no Collider2D is assigned.")]
        private Renderer boundsRenderer;

        [SerializeField]
        [Tooltip("Local-space fallback size used when neither source is assigned.")]
        private Vector2 fallbackSize = Vector2.one;

        private readonly List<GridCoord> reservedCells = new();
        private readonly List<GridCoord> candidateCells = new();
        private CableRoutingGrid reservedGrid;

        public Bounds WorldBounds
        {
            get
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
        }

        private void OnEnable()
        {
            TryInitializeReservation();
        }

        private void Start()
        {
            // CableRoutingGridService runs first and performs its authoritative
            // scene rebuild in Start. Retry once here in case this footprint's
            // OnEnable ran before its initial bounds were reservable; never poll
            // or churn placement ownership every frame.
            if (reservedGrid == null)
            {
                TryInitializeReservation();
            }
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
            results.Clear();
            if (grid == null)
            {
                return;
            }

            var bounds = WorldBounds;
            var rootDelta = candidateRootPosition - (Vector2)transform.position;
            bounds.center += (Vector3)rootDelta;
            grid.GetCellsCoveredByBounds(bounds, results);
        }

        public bool CanReserveAt(CableRoutingGrid grid, Vector2 rootPosition)
        {
            GetCoveredCells(grid, rootPosition, candidateCells);
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

        /// <summary>
        /// Atomically replaces this owner's reservation when every candidate
        /// cell is valid. Invalid placement leaves the prior reservation intact.
        /// </summary>
        public bool TryReserveAt(CableRoutingGrid grid, Vector2 rootPosition)
        {
            if (grid == null || !CanReserveAt(grid, rootPosition))
            {
                return false;
            }

            ReleaseReservation();
            reservedGrid = grid;
            for (var i = 0; i < candidateCells.Count; i++)
            {
                grid.SetObjectOccupied(candidateCells[i], this, true);
                reservedCells.Add(candidateCells[i]);
            }

            return reservedCells.Count > 0;
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
