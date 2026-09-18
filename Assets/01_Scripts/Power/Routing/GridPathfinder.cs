using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power.Routing
{
    /// <summary>
    /// Pure, MonoBehaviour-free 8-directional (orthogonal + diagonal)
    /// shortest-path search over a <see cref="Octoplug.Power.Grid.CableRoutingGrid"/>.
    /// Orthogonal steps cost 1, diagonal steps cost <see cref="Sqrt2"/> —
    /// once diagonals have unequal cost, a uniform-cost BFS can no longer
    /// guarantee the shortest path, so this is A* (octile heuristic, exact
    /// for this move set — never overestimates, so the result is always
    /// truly shortest). This stays the minimum implementation the current
    /// grid size needs; it is not a general-purpose navigation framework.
    ///
    /// A diagonal step is only taken when both orthogonal cells it would
    /// otherwise "cut past" are also walkable — a diagonal move can never
    /// pass through a wall corner. This never relaxes the existing
    /// Room/Wall/Door rules: Door is the only passable room boundary,
    /// unchanged by allowing diagonal movement elsewhere.
    /// </summary>
    public static class GridPathfinder
    {
        private const float Sqrt2 = 1.4142135f;

        private static readonly (Octoplug.Power.Grid.GridCoord dir, float cost)[] Moves =
        {
            (new(1, 0), 1f), (new(-1, 0), 1f), (new(0, 1), 1f), (new(0, -1), 1f),
            (new(1, 1), Sqrt2), (new(1, -1), Sqrt2), (new(-1, 1), Sqrt2), (new(-1, -1), Sqrt2),
        };

        /// <summary>Orthogonal-only directions, used by the simple proximity searches below (no path-cost weighting needed there).</summary>
        private static readonly Octoplug.Power.Grid.GridCoord[] AllDirections =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        /// <summary>
        /// Finds the shortest-cost walkable/door cell path from <paramref name="start"/>
        /// to <paramref name="goal"/>, allowing 8-directional movement with
        /// diagonal steps weighted <see cref="Sqrt2"/>× an orthogonal step,
        /// and never cutting a diagonal through a blocked corner. Returns
        /// false if no path exists (including when either endpoint itself
        /// is not walkable).
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

            var open = new List<Octoplug.Power.Grid.GridCoord> { start };
            var cameFrom = new Dictionary<Octoplug.Power.Grid.GridCoord, Octoplug.Power.Grid.GridCoord>();
            var gScore = new Dictionary<Octoplug.Power.Grid.GridCoord, float> { [start] = 0f };
            var closed = new HashSet<Octoplug.Power.Grid.GridCoord>();

            while (open.Count > 0)
            {
                var currentIndex = IndexOfLowestFScore(open, gScore, goal);
                var current = open[currentIndex];

                if (current == goal)
                {
                    path = ReconstructPath(cameFrom, start, goal);
                    return true;
                }

                open.RemoveAt(currentIndex);
                closed.Add(current);

                foreach (var (dir, cost) in Moves)
                {
                    var next = new Octoplug.Power.Grid.GridCoord(current.X + dir.X, current.Y + dir.Y);
                    if (closed.Contains(next) || !grid.IsWalkable(next))
                    {
                        continue;
                    }

                    if (dir.X != 0 && dir.Y != 0 && !CanCutDiagonal(grid, current, dir))
                    {
                        continue;
                    }

                    var tentativeG = gScore[current] + cost;
                    if (gScore.TryGetValue(next, out var existingG) && tentativeG >= existingG)
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    gScore[next] = tentativeG;
                    if (!open.Contains(next))
                    {
                        open.Add(next);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// A diagonal step from <paramref name="from"/> in <paramref name="dir"/>
        /// is only legal if both orthogonal neighbors it passes between —
        /// (from.X+dir.X, from.Y) and (from.X, from.Y+dir.Y) — are walkable.
        /// Otherwise it would cut through a wall corner.
        /// </summary>
        private static bool CanCutDiagonal(Octoplug.Power.Grid.CableRoutingGrid grid, Octoplug.Power.Grid.GridCoord from, Octoplug.Power.Grid.GridCoord dir)
        {
            var sideA = new Octoplug.Power.Grid.GridCoord(from.X + dir.X, from.Y);
            var sideB = new Octoplug.Power.Grid.GridCoord(from.X, from.Y + dir.Y);
            return grid.IsWalkable(sideA) && grid.IsWalkable(sideB);
        }

        /// <summary>Octile distance — the exact, never-overestimating heuristic for this move set (4-directional + diagonal at Sqrt2 cost).</summary>
        private static float Octile(Octoplug.Power.Grid.GridCoord a, Octoplug.Power.Grid.GridCoord b)
        {
            var dx = Mathf.Abs(a.X - b.X);
            var dy = Mathf.Abs(a.Y - b.Y);
            var straight = Mathf.Max(dx, dy) - Mathf.Min(dx, dy);
            var diagonal = Mathf.Min(dx, dy);
            return straight + diagonal * Sqrt2;
        }

        private static int IndexOfLowestFScore(List<Octoplug.Power.Grid.GridCoord> open, Dictionary<Octoplug.Power.Grid.GridCoord, float> gScore, Octoplug.Power.Grid.GridCoord goal)
        {
            var bestIndex = 0;
            var bestF = gScore[open[0]] + Octile(open[0], goal);
            for (var i = 1; i < open.Count; i++)
            {
                var f = gScore[open[i]] + Octile(open[i], goal);
                if (f < bestF)
                {
                    bestF = f;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Finds the *geometrically* (real-distance) nearest walkable/door
        /// cell to <paramref name="from"/> (which may itself be
        /// blocked/unknown), within <paramref name="maxRadius"/> cells.
        /// Used to resolve a drag pointer or release position that has
        /// landed outside any valid space, and — via
        /// <see cref="Octoplug.Power.Cable.CableRoutingController.TryGetSocketApproachCell"/> —
        /// to resolve a Wall Outlet's room-interior Approach Point. A
        /// proximity search, not a route, so diagonal corner-cutting does
        /// not apply here.
        ///
        /// A plain BFS-queue-order search (the previous implementation)
        /// only guarantees the fewest *hops*, which is no longer the same
        /// as nearest once diagonal neighbors are mixed into the same
        /// queue at unequal real cost — it could return a farther cell
        /// than one found a "ring" later, inflating the route that follows
        /// (this was the actual root cause of an inflated, zig-zag-prone
        /// Wall Outlet approach, not the A* search itself). This scans the
        /// whole bounding square and picks the true minimum-distance
        /// walkable cell instead — the search radius stays small (Wall
        /// Outlet approach: single-digit cells; general recovery: two
        /// digits), so an exhaustive scan is simple and fast enough
        /// without needing a general nearest-neighbor structure.
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

            var found = false;
            var bestSqrDistance = float.MaxValue;

            for (var dx = -maxRadius; dx <= maxRadius; dx++)
            {
                for (var dy = -maxRadius; dy <= maxRadius; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    var candidate = new Octoplug.Power.Grid.GridCoord(from.X + dx, from.Y + dy);
                    if (!grid.IsWalkable(candidate))
                    {
                        continue;
                    }

                    var sqrDistance = (float)(dx * dx + dy * dy);
                    if (sqrDistance < bestSqrDistance)
                    {
                        bestSqrDistance = sqrDistance;
                        result = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Searches outward from <paramref name="releaseCell"/> (nearest cells
        /// first) for a walkable cell that is actually reachable from
        /// <paramref name="originCell"/> within <paramref name="maxCableLength"/>
        /// — reachability (including the corner-cutting rule) is decided by
        /// <see cref="TryFindPath"/> itself, so this never returns a cell
        /// only reachable by cutting a corner. Used only as the Drop safety
        /// net.
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

            bool IsValid(Octoplug.Power.Grid.GridCoord cell)
            {
                return grid.IsWalkable(cell)
                       && TryFindPath(grid, originCell, cell, out var path)
                       && PathLength(BuildCellCenterPolyline(grid, path)) <= maxCableLength + 0.001f;
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

                foreach (var dir in AllDirections)
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

        private static List<Vector2> BuildCellCenterPolyline(Octoplug.Power.Grid.CableRoutingGrid grid, IReadOnlyList<Octoplug.Power.Grid.GridCoord> cellPath)
        {
            var points = new List<Vector2>(cellPath.Count);
            foreach (var cell in cellPath)
            {
                points.Add(grid.CellToWorld(cell));
            }

            return points;
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
