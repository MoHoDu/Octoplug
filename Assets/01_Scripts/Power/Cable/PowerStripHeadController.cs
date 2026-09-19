using UnityEngine;
using Octoplug.Power.Grid;

namespace Octoplug.Power.Cable
{
    /// <summary>
    /// Moves a PowerStrip whose Sockets have no downstream connections.
    /// The strip's own Plug may still be connected upstream — in that case
    /// the Plug stays fixed at its Socket and only the Head (plus Cable
    /// route) moves. Deliberately mirrors
    /// <see cref="Octoplug.Power.Input.PlugDragInput"/>'s own drag feel: once
    /// captured (a decision made exactly once, at Pointer Down — see
    /// <see cref="Octoplug.Power.Input.HeadDragInput"/>), the root follows
    /// the pointer directly every frame with only a cheap, O(1) Room-bounds
    /// clamp — no per-frame Grid/placement/collider re-evaluation. Full
    /// footprint legality (Room/Wall containment, Product/PowerStrip
    /// overlap) is checked exactly once, at Pointer Up: a valid drop commits
    /// in place, an invalid drop snaps to the nearest valid Grid anchor or
    /// restores the exact drag-start position.
    /// </summary>
    public class PowerStripHeadController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The 'Head' child's drag input.")]
        private Octoplug.Power.Input.HeadDragInput headDragInput;

        [SerializeField]
        [Tooltip("This strip's own Cable child routing controller.")]
        private CableRoutingController ownCableController;

        [SerializeField]
        [Tooltip("The root's multi-cell placement bounds and reservation owner.")]
        private PlacementFootprint placementFootprint;

        [SerializeField]
        [Tooltip("Maximum Grid-cell radius searched for the nearest valid placement when the Pointer-Up drop position is invalid.")]
        [Min(0)]
        private int nearestPlacementSearchRadius = 24;

        private PowerStrip powerStrip;
        private Vector3 dragStartPosition;
        private Vector2 grabOffset;
        private bool dragAllowed;

        private bool hasRoomClamp;
        private Vector2 roomClampMin;
        private Vector2 roomClampMax;

        private void Awake()
        {
            powerStrip = GetComponent<PowerStrip>();
            if (placementFootprint == null)
            {
                placementFootprint = GetComponent<PlacementFootprint>();
            }

            dragStartPosition = transform.position;
        }

        private void OnEnable()
        {
            if (headDragInput != null)
            {
                headDragInput.DragStarted += OnDragStarted;
                headDragInput.Dragged += OnDragged;
                headDragInput.DragEnded += OnDragEnded;
            }
        }

        private void OnDisable()
        {
            if (headDragInput != null)
            {
                headDragInput.DragStarted -= OnDragStarted;
                headDragInput.Dragged -= OnDragged;
                headDragInput.DragEnded -= OnDragEnded;
            }

            dragAllowed = false;
        }

        /// <summary>
        /// Eligibility (\"can this be grabbed at all?\") and the grab offset
        /// are both decided exactly once, here — never re-derived from live
        /// state for the remainder of the drag. This is the one place
        /// <see cref="HasNoDownstreamConnections"/> is consulted; once
        /// captured, no per-frame re-check occurs, matching
        /// <see cref="Octoplug.Power.Input.HeadDragInput"/>'s own
        /// once-at-pointer-down capture contract.
        /// </summary>
        private void OnDragStarted(Vector2 pointerDownWorldPos)
        {
            dragAllowed = HasNoDownstreamConnections();
            if (!dragAllowed)
            {
                return;
            }

            dragStartPosition = transform.position;
            grabOffset = (Vector2)transform.position - pointerDownWorldPos;
            hasRoomClamp = TryComputeRoomClampBounds(out roomClampMin, out roomClampMax);
        }

        /// <summary>
        /// Follows the pointer directly (grab offset only) every frame —
        /// the same cost profile as a Plug drag. The only per-frame check is
        /// a cheap O(1) clamp to the strip's current Room's floor bounds
        /// (inset by the footprint's own half-size) so the visual cannot
        /// wander outside the room while dragging; it is not a
        /// Product/PowerStrip-overlap or full-footprint check — those are
        /// deliberately deferred to <see cref="OnDragEnded"/> so dragging
        /// itself never blocks or slows down movement.
        /// </summary>
        private void OnDragged(Vector2 pointerWorldPos)
        {
            if (!dragAllowed)
            {
                return;
            }

            var desired = pointerWorldPos + grabOffset;
            if (hasRoomClamp)
            {
                desired.x = Mathf.Clamp(desired.x, roomClampMin.x, roomClampMax.x);
                desired.y = Mathf.Clamp(desired.y, roomClampMin.y, roomClampMax.y);
            }

            MovePreservingPlug(new Vector3(desired.x, desired.y, dragStartPosition.z));
            ownCableController?.RecomputePathFromCurrentOrigin();
        }

        /// <summary>
        /// The only point full placement legality (Room/Wall containment,
        /// Product/PowerStrip overlap, the complete multi-cell footprint) is
        /// evaluated: exactly once, on release. A valid drop commits in
        /// place; an invalid one snaps to the deterministic nearest valid
        /// Grid anchor, or — if none exists — restores the exact drag-start
        /// transform. Neither this method nor <see cref="OnDragged"/>
        /// re-checks <see cref="HasNoDownstreamConnections"/>: eligibility was
        /// already decided once, at <see cref="OnDragStarted"/>.
        /// </summary>
        private void OnDragEnded()
        {
            if (!dragAllowed)
            {
                return;
            }

            dragAllowed = false;
            var grid = ResolveGrid();
            if (grid == null || placementFootprint == null)
            {
                RestoreDragStart();
                return;
            }

            // Physics2D.autoSyncTransforms is disabled project-wide
            // (ProjectSettings/Physics2DSettings.asset), so the footprint's
            // own Collider2D.bounds can still reflect a stale pre-drag
            // position immediately after OnDragged's plain transform.position
            // assignments — CanReserveAt/TryReserveAt below would silently
            // validate the wrong (start, not current) location without this.
            Physics2D.SyncTransforms();

            if (TryCommitPlacement(grid, transform.position))
            {
                return;
            }

            var desiredCell = grid.WorldToCell(transform.position);
            if (TryFindNearestValidPlacement(grid, desiredCell, transform.position, out var nearestPosition)
                && TryCommitPlacement(grid, nearestPosition))
            {
                return;
            }

            RestoreDragStart();
        }

        private bool TryCommitPlacement(CableRoutingGrid grid, Vector3 position)
        {
            if (!placementFootprint.CanReserveAt(grid, position)
                || !placementFootprint.TryReserveAt(grid, position))
            {
                return false;
            }

            MovePreservingPlug(position);
            ownCableController?.RecomputePathFromCurrentOrigin();
            return true;
        }

        private bool TryFindNearestValidPlacement(
            CableRoutingGrid grid,
            GridCoord desiredCell,
            Vector2 desiredWorldPosition,
            out Vector3 result)
        {
            result = default;
            var found = false;
            var bestSqrDistance = float.PositiveInfinity;
            var bestCell = default(GridCoord);
            var radius = Mathf.Max(0, nearestPlacementSearchRadius);

            for (var x = -radius; x <= radius; x++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    var cell = new GridCoord(desiredCell.X + x, desiredCell.Y + y);
                    var anchor = grid.CellToWorld(cell);
                    var position = new Vector3(anchor.x, anchor.y, dragStartPosition.z);
                    if (!placementFootprint.CanReserveAt(grid, position))
                    {
                        continue;
                    }

                    var sqrDistance = (anchor - desiredWorldPosition).sqrMagnitude;
                    if (sqrDistance < bestSqrDistance
                        || Mathf.Approximately(sqrDistance, bestSqrDistance)
                        && (cell.X < bestCell.X || cell.X == bestCell.X && cell.Y < bestCell.Y))
                    {
                        bestSqrDistance = sqrDistance;
                        bestCell = cell;
                        result = position;
                        found = true;
                    }
                }
            }

            return found;
        }

        private void RestoreDragStart()
        {
            MovePreservingPlug(dragStartPosition);
            ownCableController?.RecomputePathFromCurrentOrigin();
        }

        /// <summary>
        /// Finds the <see cref="RoomArea"/> currently containing this
        /// strip's root (a cheap, once-per-drag lookup — never repeated per
        /// frame) and returns clamp bounds inset by the footprint's own
        /// half-size, so the clamped root position keeps the whole footprint
        /// inside the room's floor rectangle. Returns false (no clamp
        /// applied) if no containing Room is found, e.g. a strip whose
        /// authored position is already outside any Room — the existing
        /// "outside Grid" condition documented elsewhere for this project.
        /// </summary>
        private bool TryComputeRoomClampBounds(out Vector2 min, out Vector2 max)
        {
            min = default;
            max = default;
#if UNITY_2023_1_OR_NEWER
            var rooms = FindObjectsByType<Octoplug.Power.RoomArea>(FindObjectsSortMode.None);
#else
            var rooms = FindObjectsOfType<Octoplug.Power.RoomArea>();
#endif
            var position = (Vector2)transform.position;
            foreach (var room in rooms)
            {
                var floor = room != null ? room.FloorArea : null;
                if (floor == null || !floor.bounds.Contains(position))
                {
                    continue;
                }

                var half = placementFootprint != null
                    ? (Vector2)placementFootprint.WorldBounds.extents
                    : Vector2.zero;
                var roomBounds = floor.bounds;
                min = new Vector2(roomBounds.min.x + half.x, roomBounds.min.y + half.y);
                max = new Vector2(roomBounds.max.x - half.x, roomBounds.max.y - half.y);

                // A footprint wider/taller than the room itself would invert
                // min/max; skip clamping rather than pin the strip to a
                // degenerate point.
                if (min.x > max.x || min.y > max.y)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Head is movable iff none of this strip's Sockets have a
        /// downstream Plug connected. The strip's own Plug being connected
        /// upstream does NOT block Head movement — the Plug stays fixed at
        /// its Socket, the Head (and Cable route) moves independently.
        /// </summary>
        private bool HasNoDownstreamConnections()
        {
            if (powerStrip == null)
            {
                return false;
            }

            foreach (var socket in powerStrip.ActiveSockets)
            {
                if (socket.IsConnected)
                {
                    return false;
                }
            }

            return true;
        }

        private void MovePreservingPlug(Vector3 rootPosition)
        {
            var info = ownCableController != null
                ? ownCableController.CableInfo
                : null;
            var plugTransform = info != null && info.Plug != null
                ? info.Plug.transform
                : null;
            var plugWorldPosition = plugTransform != null
                ? plugTransform.position
                : (Vector3?)null;

            transform.position = rootPosition;

            if (plugTransform != null && plugWorldPosition.HasValue)
            {
                plugTransform.position = plugWorldPosition.Value;
            }
        }

        private static CableRoutingGrid ResolveGrid()
        {
            var service = CableRoutingGridService.Instance;
            return service != null ? service.Grid : null;
        }
    }
}
