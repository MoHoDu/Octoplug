using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power.Grid
{
    /// <summary>
    /// Minimal foundation for the 4-directional cable-routing grid.
    ///
    /// This stage only stores which cells are walkable, blocked, or a
    /// door, and can be rebuilt at any time from current Room/Wall/Door
    /// data (including after procedural room generation adds new rooms).
    /// The actual shortest-path search (A* or equivalent), max-length
    /// truncation, and nearest-valid-drop lookup are implemented in a
    /// later stage that consumes this grid — they are intentionally not
    /// implemented here yet.
    /// </summary>
    /// <summary>One side of a cell — for placing a Wall-mounted object (e.g. a Wall Outlet) relative to the cell it is mounted against, without needing its own coordinate system.</summary>
    public enum CellEdge
    {
        Left,
        Right,
        Top,
        Bottom
    }

    public class CableRoutingGrid
    {
        private readonly float cellSize;
        private readonly object legacyPlacementOwner = new();
        private readonly Dictionary<GridCoord, GridCellState> cells = new();

        /// <summary>
        /// Placement reservations keyed by cell and owner, tracked independently
        /// of <see cref="GridCellState"/>. A placed Product or PowerStrip does
        /// not block Cable traversal; only placement queries consult this map.
        /// </summary>
        private readonly Dictionary<GridCoord, HashSet<object>> placementOwners = new();

        public CableRoutingGrid(float cellSize)
        {
            this.cellSize = Mathf.Max(0.01f, cellSize);
        }

        public float CellSize => cellSize;

        public GridCoord WorldToCell(Vector2 worldPosition)
        {
            return new GridCoord(
                Mathf.FloorToInt(worldPosition.x / cellSize),
                Mathf.FloorToInt(worldPosition.y / cellSize));
        }

        public Vector2 CellToWorld(GridCoord coord)
        {
            return new Vector2(
                (coord.X + 0.5f) * cellSize,
                (coord.Y + 0.5f) * cellSize);
        }

        /// <summary>World-space position of one side of a cell — e.g. the wall-facing edge a Wall-mounted object anchors against.</summary>
        public Vector2 CellEdgeWorld(GridCoord coord, CellEdge edge)
        {
            var center = CellToWorld(coord);
            var half = cellSize * 0.5f;
            return edge switch
            {
                CellEdge.Left => center + new Vector2(-half, 0f),
                CellEdge.Right => center + new Vector2(half, 0f),
                CellEdge.Top => center + new Vector2(0f, half),
                CellEdge.Bottom => center + new Vector2(0f, -half),
                _ => center,
            };
        }

        /// <summary>
        /// Drops traversal cell state so room geometry can be rebuilt. Live
        /// placement reservations survive because traversal rebuilds and object
        /// lifetimes are independent.
        /// </summary>
        public void Clear()
        {
            cells.Clear();
        }

        public GridCellState GetState(GridCoord coord)
        {
            return cells.TryGetValue(coord, out var state) ? state : GridCellState.Unknown;
        }

        public bool IsWalkable(GridCoord coord)
        {
            var state = GetState(coord);
            return state == GridCellState.Walkable || state == GridCellState.Door;
        }

        /// <summary>
        /// Marks (or clears) one cell for an owner. Multiple owners are retained
        /// independently so one object's release cannot erase another object's
        /// reservation. Never affects <see cref="IsWalkable"/>.
        /// </summary>
        public void SetObjectOccupied(GridCoord coord, bool occupied) =>
            SetObjectOccupied(coord, legacyPlacementOwner, occupied);

        public void SetObjectOccupied(GridCoord coord, object owner, bool occupied)
        {
            if (owner == null)
            {
                return;
            }

            if (occupied)
            {
                if (!placementOwners.TryGetValue(coord, out var owners))
                {
                    owners = new HashSet<object>();
                    placementOwners.Add(coord, owners);
                }

                owners.Add(owner);
                return;
            }

            if (!placementOwners.TryGetValue(coord, out var existingOwners))
            {
                return;
            }

            existingOwners.Remove(owner);
            if (existingOwners.Count == 0)
            {
                placementOwners.Remove(coord);
            }
        }

        public bool IsObjectOccupied(GridCoord coord) =>
            placementOwners.TryGetValue(coord, out var owners) && owners.Count > 0;

        public bool IsObjectOccupiedByOther(GridCoord coord, object owner)
        {
            if (!placementOwners.TryGetValue(coord, out var owners))
            {
                return false;
            }

            foreach (var existingOwner in owners)
            {
                if (!ReferenceEquals(existingOwner, owner))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether an owner may reserve this cell: walkable and not reserved by
        /// another owner. Cable traversal must keep using <see cref="IsWalkable"/>.
        /// </summary>
        public bool IsFreeForPlacement(GridCoord coord) =>
            IsWalkable(coord) && !IsObjectOccupied(coord);

        public bool IsFreeForPlacement(GridCoord coord, object owner) =>
            IsWalkable(coord) && !IsObjectOccupiedByOther(coord, owner);

        /// <summary>Appends every grid cell whose area intersects the supplied world bounds.</summary>
        public void GetCellsCoveredByBounds(Bounds bounds, List<GridCoord> results)
        {
            var inset = Mathf.Min(
                cellSize * 0.001f,
                bounds.extents.x * 0.5f,
                bounds.extents.y * 0.5f,
                0.0001f);
            var min = WorldToCell((Vector2)bounds.min + Vector2.one * inset);
            var max = WorldToCell((Vector2)bounds.max - Vector2.one * inset);

            for (var x = min.X; x <= max.X; x++)
            {
                for (var y = min.Y; y <= max.Y; y++)
                {
                    results.Add(new GridCoord(x, y));
                }
            }
        }

        /// <summary>
        /// Marks every cell covered by <paramref name="bounds"/> with
        /// <paramref name="state"/>. Blocked and Door never downgrade back
        /// to Walkable from a later, larger-area call — callers should
        /// mark room floor areas first, then overlay walls and doors.
        /// </summary>
        public void MarkArea(Bounds bounds, GridCellState state)
        {
            var min = WorldToCell(bounds.min);
            var max = WorldToCell(bounds.max);

            for (var x = min.X; x <= max.X; x++)
            {
                for (var y = min.Y; y <= max.Y; y++)
                {
                    var coord = new GridCoord(x, y);
                    if (state == GridCellState.Walkable && GetState(coord) is GridCellState.Blocked or GridCellState.Door)
                    {
                        continue;
                    }

                    cells[coord] = state;
                }
            }
        }
    }
}
