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
    public class CableRoutingGrid
    {
        private readonly float cellSize;
        private readonly Dictionary<GridCoord, GridCellState> cells = new();

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

        /// <summary>Drops all stored cell data so the grid can be rebuilt from scratch.</summary>
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
