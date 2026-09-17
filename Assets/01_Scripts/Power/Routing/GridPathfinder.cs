using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power.Routing
{
    /// <summary>
    /// Pure, MonoBehaviour-free 4-directional shortest-path search over a
    /// <see cref="Octoplug.Power.Grid.CableRoutingGrid"/>. Every cell step
    /// has equal cost, so a breadth-first search already returns the
    /// shortest orthogonal path — no heuristic/A* bookkeeping is needed
    /// for this grid, and none is added.
    ///
    /// This is the minimum implementation Cable routing needs; it makes
    /// no assumption about room/door count or layout and simply reads
    /// whatever the grid currently reports.
    /// </summary>
    public static class GridPathfinder
    {
        private static readonly Octoplug.Power.Grid.GridCoord[] Directions =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
        };

        /// <summary>
        /// Finds the shortest walkable/door cell path from <paramref name="start"/>
        /// to <paramref name="goal"/>. Returns false if no path exists (including
        /// when either endpoint itself is not walkable).
        /// </summary>
        public static bool TryFindPath(
            Octoplug.Power.Grid.CableRoutingGrid grid,
            Octoplug.Power.Grid.GridCoord start,
            Octoplug.Power.Grid.GridCoord goal,
            out List<Octoplug.Power.Grid.GridCoord> path)
        {
            path = null;
            if (grid == null || !grid.IsWalkable(start) || !grid.IsWalkable(goal))
            {
                return false;
            }

            if (start == goal)
            {
                path = new List<Octoplug.Power.Grid.GridCoord> { start };
                return true;
            }

            var frontier = new Queue<Octoplug.Power.Grid.GridCoord>();
            var cameFrom = new Dictionary<Octoplug.Power.Grid.GridCoord, Octoplug.Power.Grid.GridCoord>();
            frontier.Enqueue(start);
            cameFrom[start] = start;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == goal)
                {
                    path = ReconstructPath(cameFrom, start, goal);
                    return true;
                }

                foreach (var dir in Directions)
                {
                    var next = new Octoplug.Power.Grid.GridCoord(current.X + dir.X, current.Y + dir.Y);
                    if (cameFrom.ContainsKey(next) || !grid.IsWalkable(next))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the nearest walkable/door cell to <paramref name="from"/> (which
        /// may itself be blocked/unknown), searching outward up to
        /// <paramref name="maxRadius"/> cells. Used to resolve a drag pointer or
        /// release position that has landed outside any valid space.
        /// </summary>
        public static bool TryFindNearestWalkable(
            Octoplug.Power.Grid.CableRoutingGrid grid,
            Octoplug.Power.Grid.GridCoord from,
            int maxRadius,
            out Octoplug.Power.Grid.GridCoord result)
        {
            result = from;
            if (grid == null)
            {
                return false;
            }

            if (grid.IsWalkable(from))
            {
                result = from;
                return true;
            }

            var visited = new HashSet<Octoplug.Power.Grid.GridCoord> { from };
            var frontier = new Queue<Octoplug.Power.Grid.GridCoord>();
            frontier.Enqueue(from);
            var steps = 0;

            while (frontier.Count > 0 && steps < maxRadius * maxRadius * 4)
            {
                var current = frontier.Dequeue();
                steps++;

                foreach (var dir in Directions)
                {
                    var next = new Octoplug.Power.Grid.GridCoord(current.X + dir.X, current.Y + dir.Y);
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    if (Mathf.Abs(next.X - from.X) > maxRadius || Mathf.Abs(next.Y - from.Y) > maxRadius)
                    {
                        continue;
                    }

                    if (grid.IsWalkable(next))
                    {
                        result = next;
                        return true;
                    }

                    frontier.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>
        /// Searches outward from <paramref name="releaseCell"/> (nearest cells
        /// first) for a walkable cell that is actually reachable from
        /// <paramref name="originCell"/> within <paramref name="maxCableLength"/>
        /// (measured in whole cell steps from <paramref name="originCell"/>,
        /// a safe/conservative proxy for the continuous cable length used
        /// during live dragging). Used only as the Drop safety net.
        /// </summary>
        public static bool TryFindNearestValidDropCell(
            Octoplug.Power.Grid.CableRoutingGrid grid,
            Octoplug.Power.Grid.GridCoord originCell,
            Octoplug.Power.Grid.GridCoord releaseCell,
            float maxCableLength,
            int maxRadius,
            out Octoplug.Power.Grid.GridCoord result)
        {
            result = releaseCell;
            if (grid == null)
            {
                return false;
            }

            var maxSteps = Mathf.Max(1, Mathf.FloorToInt(maxCableLength / grid.CellSize) + 1);

            bool IsValid(Octoplug.Power.Grid.GridCoord cell)
            {
                return grid.IsWalkable(cell)
                       && TryFindPath(grid, originCell, cell, out var path)
                       && path.Count - 1 <= maxSteps;
            }

            if (IsValid(releaseCell))
            {
                result = releaseCell;
                return true;
            }

            var visited = new HashSet<Octoplug.Power.Grid.GridCoord> { releaseCell };
            var frontier = new Queue<Octoplug.Power.Grid.GridCoord>();
            frontier.Enqueue(releaseCell);
            var steps = 0;

            while (frontier.Count > 0 && steps < maxRadius * maxRadius * 4)
            {
                var current = frontier.Dequeue();
                steps++;

                foreach (var dir in Directions)
                {
                    var next = new Octoplug.Power.Grid.GridCoord(current.X + dir.X, current.Y + dir.Y);
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    if (Mathf.Abs(next.X - releaseCell.X) > maxRadius || Mathf.Abs(next.Y - releaseCell.Y) > maxRadius)
                    {
                        continue;
                    }

                    if (IsValid(next))
                    {
                        result = next;
                        return true;
                    }

                    frontier.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>Total Euclidean length of a connected world-space polyline.</summary>
        public static float PathLength(IReadOnlyList<Vector2> worldPoints)
        {
            var total = 0f;
            for (var i = 1; i < worldPoints.Count; i++)
            {
                total += Vector2.Distance(worldPoints[i - 1], worldPoints[i]);
            }

            return total;
        }

        /// <summary>
        /// Walks <paramref name="worldPoints"/> and returns the prefix of the
        /// polyline that is at most <paramref name="maxLength"/> long, ending
        /// exactly at that length (interpolated inside the segment it falls in).
        /// Returns the full list unchanged if it is already within budget.
        /// </summary>
        public static List<Vector2> TruncateToLength(IReadOnlyList<Vector2> worldPoints, float maxLength)
        {
            var result = new List<Vector2> { worldPoints[0] };
            var remaining = maxLength;

            for (var i = 1; i < worldPoints.Count; i++)
            {
                var segment = worldPoints[i] - worldPoints[i - 1];
                var segmentLength = segment.magnitude;

                if (segmentLength <= remaining)
                {
                    result.Add(worldPoints[i]);
                    remaining -= segmentLength;
                    continue;
                }

                if (segmentLength > 0.0001f)
                {
                    var t = Mathf.Clamp01(remaining / segmentLength);
                    result.Add(worldPoints[i - 1] + segment * t);
                }

                return result;
            }

            return result;
        }

        private static List<Octoplug.Power.Grid.GridCoord> ReconstructPath(
            Dictionary<Octoplug.Power.Grid.GridCoord, Octoplug.Power.Grid.GridCoord> cameFrom,
            Octoplug.Power.Grid.GridCoord start,
            Octoplug.Power.Grid.GridCoord goal)
        {
            var path = new List<Octoplug.Power.Grid.GridCoord> { goal };
            var current = goal;
            while (current != start)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}
