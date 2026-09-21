using System.Collections.Generic;
using Octoplug.Power.Grid;
using UnityEngine;

namespace Octoplug.Power.Routing
{
    /// <summary>
    /// Non-mutating cable reachability using the same grid route, terminal
    /// approach-side rule, and continuous endpoint corrections as gameplay.
    /// </summary>
    public static class CableRouteReachability
    {
        public static bool TryGetLength(
            CableRoutingGrid grid,
            Vector2 originPosition,
            SocketConnector socket,
            int approachSearchRadius,
            out float length)
        {
            length = 0f;
            if (!TryBuildPath(
                    grid,
                    originPosition,
                    socket,
                    approachSearchRadius,
                    minRenderSegmentLength: 0.15f,
                    out var points))
            {
                return false;
            }

            length = GridPathfinder.PathLength(points);
            return true;
        }

        public static bool TryBuildPath(
            CableRoutingGrid grid,
            Vector2 originPosition,
            SocketConnector socket,
            int approachSearchRadius,
            float minRenderSegmentLength,
            out List<Vector2> worldPath)
        {
            worldPath = null;
            if (grid == null
                || socket == null
                || approachSearchRadius < 0
                || !float.IsFinite(minRenderSegmentLength)
                || minRenderSegmentLength < 0f)
            {
                return false;
            }

            var originCell = grid.WorldToCell(originPosition);
            if (!grid.IsWalkable(originCell)
                || !TryGetSocketApproachCell(
                    grid,
                    socket,
                    approachSearchRadius,
                    out var approachCell)
                || !GridPathfinder.TryFindPath(
                    grid,
                    originCell,
                    approachCell,
                    out var cellPath))
            {
                return false;
            }

            worldPath = BuildWorldPath(
                grid,
                originPosition,
                cellPath,
                socket.ConnectorTransform.position,
                minRenderSegmentLength);
            return true;
        }

        public static bool IsWithinLength(
            CableRoutingGrid grid,
            Vector2 originPosition,
            SocketConnector socket,
            float cableLength,
            int approachSearchRadius = 3)
        {
            return float.IsFinite(cableLength)
                && cableLength >= 0f
                && TryGetLength(
                    grid,
                    originPosition,
                    socket,
                    approachSearchRadius,
                    out var routedLength)
                && routedLength <= cableLength + 0.001f;
        }

        private static bool TryGetSocketApproachCell(
            CableRoutingGrid grid,
            SocketConnector socket,
            int searchRadius,
            out GridCoord approachCell)
        {
            var socketPosition = (Vector2)socket.ConnectorTransform.position;
            var socketCell = grid.WorldToCell(socketPosition);
            approachCell = socketCell;
            if (grid.IsWalkable(socketCell))
            {
                return true;
            }

            var found = false;
            var bestSqrDistance = float.PositiveInfinity;
            for (var x = -searchRadius; x <= searchRadius; x++)
            {
                for (var y = -searchRadius; y <= searchRadius; y++)
                {
                    var candidate = new GridCoord(socketCell.X + x, socketCell.Y + y);
                    if (!grid.IsWalkable(candidate))
                    {
                        continue;
                    }

                    var candidatePosition = grid.CellToWorld(candidate);
                    if (socket.IsTerminalEndpoint)
                    {
                        var dot = Vector2.Dot(candidatePosition - socketPosition, socket.ApproachDirection);
                        // Allow front (dot > 0) and back (dot < 0) for shared walls.
                        // Reject only if it's perfectly orthogonal (approaching from inside the wall).
                        if (Mathf.Abs(dot) < 0.001f)
                        {
                            continue;
                        }
                    }

                    var sqrDistance = (candidatePosition - socketPosition).sqrMagnitude;
                    if (sqrDistance < bestSqrDistance)
                    {
                        bestSqrDistance = sqrDistance;
                        approachCell = candidate;
                        found = true;
                    }
                }
            }

            return found;
        }

        private static List<Vector2> BuildWorldPath(
            CableRoutingGrid grid,
            Vector2 originPosition,
            IReadOnlyList<GridCoord> cellPath,
            Vector2 socketPosition,
            float minRenderSegmentLength)
        {
            var points = new List<Vector2>(cellPath.Count + 3)
            {
                originPosition
            };
            var firstCellIndex = 0;
            var firstCellPosition = grid.CellToWorld(cellPath[0]);
            if (cellPath.Count > 1
                && Vector2.Distance(originPosition, firstCellPosition)
                    < minRenderSegmentLength)
            {
                firstCellIndex = 1;
            }
            else
            {
                AppendOrthogonalJog(
                    points,
                    firstCellPosition,
                    minRenderSegmentLength);
                firstCellIndex = 1;
            }

            for (var i = firstCellIndex; i < cellPath.Count; i++)
            {
                points.Add(grid.CellToWorld(cellPath[i]));
            }

            if (points.Count > 2
                && Vector2.Distance(points[^1], socketPosition)
                    < minRenderSegmentLength)
            {
                points.RemoveAt(points.Count - 1);
            }

            AppendOrthogonalJog(points, socketPosition, minRenderSegmentLength);
            return points;
        }

        private static void AppendOrthogonalJog(
            List<Vector2> path,
            Vector2 target,
            float minRenderSegmentLength)
        {
            var from = path[^1];
            var dx = Mathf.Abs(from.x - target.x);
            var dy = Mathf.Abs(from.y - target.y);
            if (dx > minRenderSegmentLength && dy > minRenderSegmentLength)
            {
                var incoming = path.Count > 1
                    ? from - path[^2]
                    : Vector2.zero;
                var continueHorizontal = Mathf.Abs(incoming.x)
                    >= Mathf.Abs(incoming.y);
                path.Add(
                    continueHorizontal
                        ? new Vector2(target.x, from.y)
                        : new Vector2(from.x, target.y));
            }

            if (Vector2.Distance(path[^1], target) > 0.0001f)
            {
                path.Add(target);
            }
        }
    }
}
