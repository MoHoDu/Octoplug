using System;
using System.Collections.Generic;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using UnityEngine;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>
    /// Finds grid-aligned room positions whose complete authored placement
    /// footprint is inside the room and clear of geometry and reservations.
    /// </summary>
    public static class RoomObjectPlacementPlanner
    {
        public static bool TryFindPosition(
            RoomPlacement room,
            CableRoutingGrid grid,
            PlacementFootprint footprint,
            int socketCount,
            out Vector2 position,
            out string failure) =>
            TryFindPosition(
                room,
                grid,
                footprint,
                socketCount,
                candidateConstraint: null,
                out position,
                out failure);

        public static bool TryFindPosition(
            RoomPlacement room,
            CableRoutingGrid grid,
            PlacementFootprint footprint,
            int socketCount,
            Func<Vector2, Bounds, bool> candidateConstraint,
            out Vector2 position,
            out string failure)
        {
            position = default;
            if (!room.IsValid)
            {
                failure = "room is invalid";
                return false;
            }

            if (grid == null)
            {
                failure = "routing grid is unavailable";
                return false;
            }

            if (footprint == null)
            {
                failure = "placement footprint is unavailable";
                return false;
            }

            var minCell = grid.WorldToCell(new Vector2(room.Bounds.MinX, room.Bounds.MinY));
            var maxCell = grid.WorldToCell(new Vector2(room.Bounds.MaxX, room.Bounds.MaxY));
            var cells = new List<GridCoord>();
            var foundAvailableCandidate = false;
            for (var y = minCell.Y; y <= maxCell.Y; y++)
            {
                for (var x = minCell.X; x <= maxCell.X; x++)
                {
                    var candidate = grid.CellToWorld(new GridCoord(x, y));
                    var bounds = footprint.GetWorldBounds(socketCount, candidate);
                    if (!IsInsideRoom(bounds, room.Bounds))
                    {
                        continue;
                    }

                    footprint.GetCoveredCells(grid, candidate, socketCount, cells);
                    if (cells.Count == 0 || !AreCellsAvailable(grid, footprint, cells))
                    {
                        continue;
                    }

                    foundAvailableCandidate = true;
                    if (candidateConstraint != null
                        && !candidateConstraint(candidate, bounds))
                    {
                        continue;
                    }

                    position = candidate;
                    failure = null;
                    return true;
                }
            }

            failure = foundAvailableCandidate && candidateConstraint != null
                ? "full-footprint positions exist, but none satisfy the additional placement constraints"
                : "no full-footprint position is inside the room and clear of walls, doors, products, PowerStrips, and reservations";
            return false;
        }

        public static bool TryFindNearestPosition(
            RoomPlacement room,
            CableRoutingGrid grid,
            PlacementFootprint footprint,
            int socketCount,
            Vector2 desiredPosition,
            float maximumCorrectionDistance,
            out Vector2 position,
            out string failure)
        {
            position = default;
            if (!room.IsValid || grid == null || footprint == null
                || !float.IsFinite(maximumCorrectionDistance)
                || maximumCorrectionDistance < 0f)
            {
                failure = "placement request is invalid";
                return false;
            }

            var minCell = grid.WorldToCell(new Vector2(room.Bounds.MinX, room.Bounds.MinY));
            var maxCell = grid.WorldToCell(new Vector2(room.Bounds.MaxX, room.Bounds.MaxY));
            var cells = new List<GridCoord>();
            if (IsCandidateAvailable(
                    room,
                    grid,
                    footprint,
                    socketCount,
                    desiredPosition,
                    cells))
            {
                position = desiredPosition;
                failure = null;
                return true;
            }

            var bestSqrDistance = float.PositiveInfinity;
            var maximumSqrDistance = maximumCorrectionDistance * maximumCorrectionDistance;
            for (var y = minCell.Y; y <= maxCell.Y; y++)
            {
                for (var x = minCell.X; x <= maxCell.X; x++)
                {
                    var candidate = grid.CellToWorld(new GridCoord(x, y));
                    var sqrDistance = (candidate - desiredPosition).sqrMagnitude;
                    if (sqrDistance > maximumSqrDistance || sqrDistance >= bestSqrDistance)
                    {
                        continue;
                    }

                    if (!IsCandidateAvailable(
                            room,
                            grid,
                            footprint,
                            socketCount,
                            candidate,
                            cells))
                    {
                        continue;
                    }

                    bestSqrDistance = sqrDistance;
                    position = candidate;
                }
            }

            if (float.IsPositiveInfinity(bestSqrDistance))
            {
                failure = "no valid position is available near the requested point";
                return false;
            }

            failure = null;
            return true;
        }

        public static bool CanPlaceAllSocketCounts(
            RoomPlacement room,
            CableRoutingGrid grid,
            PlacementFootprint footprint,
            IReadOnlyList<int> socketCounts)
        {
            if (socketCounts == null || socketCounts.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < socketCounts.Count; i++)
            {
                if (!TryFindPosition(
                        room,
                        grid,
                        footprint,
                        socketCounts[i],
                        out _,
                        out _))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsCandidateAvailable(
            RoomPlacement room,
            CableRoutingGrid grid,
            PlacementFootprint footprint,
            int socketCount,
            Vector2 candidate,
            List<GridCoord> cells)
        {
            var bounds = footprint.GetWorldBounds(socketCount, candidate);
            if (!IsInsideRoom(bounds, room.Bounds))
            {
                return false;
            }

            footprint.GetCoveredCells(grid, candidate, socketCount, cells);
            return cells.Count > 0 && AreCellsAvailable(grid, footprint, cells);
        }

        private static bool AreCellsAvailable(
            CableRoutingGrid grid,
            PlacementFootprint owner,
            IReadOnlyList<GridCoord> cells)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                // Door cells remain cable-walkable but are never object placement cells.
                if (grid.GetState(cells[i]) != GridCellState.Walkable
                    || !grid.IsFreeForPlacement(cells[i], owner))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsInsideRoom(Bounds bounds, RoomBounds2D room)
        {
            const float tolerance = 0.0001f;
            return bounds.min.x >= room.MinX - tolerance
                && bounds.max.x <= room.MaxX + tolerance
                && bounds.min.y >= room.MinY - tolerance
                && bounds.max.y <= room.MaxY + tolerance;
        }
    }
}
