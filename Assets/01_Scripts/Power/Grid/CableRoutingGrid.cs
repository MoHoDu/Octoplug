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
        private readonly Dictionary<GridCoord, GridCellState> cells = new();

        /// <summary>
        /// Cells currently occupied by a placed object (Product, PowerStrip,
        /// ...) — tracked independently of <see cref="GridCellState"/>
        /// because the two questions are not the same: a cell a Product
        /// sits on is not, by itself, blocked for Cable traversal (a cable
        /// may pass visually behind it), while a Wall cell is blocked for
        /// both. Nothing currently populates this — it exists so a future
        /// placement system has one shared place to register/query it
        /// instead of inventing its own per-system occupancy tracking.
        /// </summary>
        private readonly HashSet<GridCoord> objectOccupied = new();

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

        /// <summary>Drops all stored cell data so the grid can be rebuilt from scratch.</summary>
        public void Clear()
        {
            cells.Clear();
            objectOccupied.Clear();
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

        /// <summary>Marks (or clears) a cell as occupied by a placed object. Never affects <see cref="IsWalkable"/> — Cable traversal and object placement are independent questions (see <see cref="objectOccupied"/>).</summary>
        public void SetObjectOccupied(GridCoord coord, bool occupied)
        {
            if (occupied)
            {
                objectOccupied.Add(coord);
            }
            else
            {
                objectOccupied.Remove(coord);
            }
        }

        public bool IsObjectOccupied(GridCoord coord) => objectOccupied.Contains(coord);

        /// <summary>Whether a future placement system may place an object on this cell: walkable and not already occupied by another object. Cable traversal must keep using <see cref="IsWalkable"/>, not this.</summary>
        public bool IsFreeForPlacement(GridCoord coord) => IsWalkable(coord) && !objectOccupied.Contains(coord);

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
