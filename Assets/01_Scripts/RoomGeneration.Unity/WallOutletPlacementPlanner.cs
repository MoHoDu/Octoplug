using System;
using System.Collections.Generic;
using Octoplug.RoomGeneration;
using UnityEngine;

namespace Octoplug.RoomGeneration.Unity
{
    public readonly struct WallOutletGeometry
    {
        public WallOutletGeometry(Vector2 localCenter, Vector2 size)
        {
            LocalCenter = localCenter;
            Size = size;
        }

        public Vector2 LocalCenter { get; }
        public Vector2 Size { get; }
    }

    public readonly struct WallOutletPlacement
    {
        public WallOutletPlacement(WallSide side, Vector2 position, float rotationDegrees)
        {
            Side = side;
            Position = position;
            RotationDegrees = rotationDegrees;
        }

        public WallSide Side { get; }
        public Vector2 Position { get; }
        public float RotationDegrees { get; }
    }

    public enum ClickedWallPlacementStatus
    {
        Success,
        InvalidInput,
        TooFarFromWall,
        WallOccupied,
        NoValidCoordinateNearClick
    }

    public readonly struct ClickedWallPlacementResult
    {
        public ClickedWallPlacementResult(
            ClickedWallPlacementStatus status,
            WallOutletPlacement placement)
        {
            Status = status;
            Placement = placement;
        }

        public ClickedWallPlacementStatus Status { get; }
        public WallOutletPlacement Placement { get; }
        public bool Success => Status == ClickedWallPlacementStatus.Success;
    }

    public static class WallOutletPlacementPlanner
    {
        private static readonly WallSide[] OrderedSides =
        {
            WallSide.Top,
            WallSide.Right,
            WallSide.Bottom,
            WallSide.Left
        };

        public static IReadOnlyList<WallOutletPlacement> Plan(
            RoomPlacement room,
            IReadOnlyList<DoorPlan> doors,
            WallOutletGeometry geometry,
            float safetyMargin,
            int requestedCount)
        {
            if (!room.IsValid || requestedCount <= 0)
            {
                return Array.Empty<WallOutletPlacement>();
            }

            var placements = new List<WallOutletPlacement>(Mathf.Min(requestedCount, OrderedSides.Length));
            for (var i = 0; i < OrderedSides.Length && placements.Count < requestedCount; i++)
            {
                var side = OrderedSides[i];
                if (TryPlanOnWall(room, side, doors, geometry, safetyMargin, out var placement))
                {
                    placements.Add(placement);
                }
            }

            return placements;
        }

        public static ClickedWallPlacementResult PlanClicked(
            RoomPlacement room,
            IReadOnlyList<DoorPlan> doors,
            WallOutletGeometry geometry,
            float safetyMargin,
            Vector2 clickedWorldPoint,
            IReadOnlyCollection<WallSide> occupiedWallSides)
        {
            if (!room.IsValid
                || !IsFinitePositive(geometry.Size.x)
                || !IsFinitePositive(geometry.Size.y)
                || !IsFiniteNonNegative(safetyMargin)
                || !IsFinite(clickedWorldPoint.x)
                || !IsFinite(clickedWorldPoint.y))
            {
                return Failure(ClickedWallPlacementStatus.InvalidInput);
            }

            var side = FindNearestWall(room.Bounds, clickedWorldPoint, out var wallDistance);
            var selectionDistance = geometry.Size.y * 0.5f + safetyMargin;
            if (wallDistance > selectionDistance)
            {
                return Failure(ClickedWallPlacementStatus.TooFarFromWall);
            }

            if (IsOccupied(side, occupiedWallSides))
            {
                return Failure(ClickedWallPlacementStatus.WallOccupied);
            }

            var vertical = side is WallSide.Left or WallSide.Right;
            var wallStart = vertical ? room.Bounds.MinY : room.Bounds.MinX;
            var wallEnd = vertical ? room.Bounds.MaxY : room.Bounds.MaxX;
            var clickedCoordinate = vertical ? clickedWorldPoint.y : clickedWorldPoint.x;
            var halfFootprint = geometry.Size.x * 0.5f;
            var validStart = wallStart + halfFootprint + safetyMargin;
            var validEnd = wallEnd - halfFootprint - safetyMargin;
            if (validEnd < validStart)
            {
                return Failure(ClickedWallPlacementStatus.NoValidCoordinateNearClick);
            }

            var blocked = CollectBlockedIntervals(room.Id, side, doors, halfFootprint + safetyMargin);
            var coordinate = FindClosestFreeCoordinate(clickedCoordinate, validStart, validEnd, blocked);
            if (!coordinate.HasValue || Mathf.Abs(coordinate.Value - clickedCoordinate) > selectionDistance)
            {
                return Failure(ClickedWallPlacementStatus.NoValidCoordinateNearClick);
            }

            return new ClickedWallPlacementResult(
                ClickedWallPlacementStatus.Success,
                CreatePlacement(room, side, coordinate.Value, geometry));
        }

        public static ClickedWallPlacementResult PlanClickedWithExistingPlacements(
            RoomPlacement room,
            IReadOnlyList<DoorPlan> doors,
            WallOutletGeometry geometry,
            float safetyMargin,
            Vector2 clickedWorldPoint,
            IReadOnlyList<WallOutletPlacement> existingPlacements)
        {
            HashSet<WallSide> occupiedWallSides = null;
            if (existingPlacements != null && existingPlacements.Count > 0)
            {
                occupiedWallSides = new HashSet<WallSide>();
                for (var i = 0; i < existingPlacements.Count; i++)
                {
                    occupiedWallSides.Add(existingPlacements[i].Side);
                }
            }

            return PlanClicked(
                room,
                doors,
                geometry,
                safetyMargin,
                clickedWorldPoint,
                occupiedWallSides);
        }

        private static bool TryPlanOnWall(
            RoomPlacement room,
            WallSide side,
            IReadOnlyList<DoorPlan> doors,
            WallOutletGeometry geometry,
            float safetyMargin,
            out WallOutletPlacement placement)
        {
            var vertical = side is WallSide.Left or WallSide.Right;
            var wallStart = vertical ? room.Bounds.MinY : room.Bounds.MinX;
            var wallEnd = vertical ? room.Bounds.MaxY : room.Bounds.MaxX;
            var halfFootprint = geometry.Size.x * 0.5f;
            var validStart = wallStart + halfFootprint + safetyMargin;
            var validEnd = wallEnd - halfFootprint - safetyMargin;
            if (validEnd < validStart)
            {
                placement = default;
                return false;
            }

            var blocked = CollectBlockedIntervals(room.Id, side, doors, halfFootprint + safetyMargin);
            var coordinate = FindWidestFreeIntervalMidpoint(validStart, validEnd, blocked);
            if (!coordinate.HasValue)
            {
                placement = default;
                return false;
            }

            placement = CreatePlacement(room, side, coordinate.Value, geometry);
            return true;
        }

        private static WallOutletPlacement CreatePlacement(
            RoomPlacement room,
            WallSide side,
            float coordinate,
            WallOutletGeometry geometry)
        {
            var rotation = GetRotationDegrees(side);
            var wallPoint = side switch
            {
                WallSide.Top => new Vector2(coordinate, room.Bounds.MaxY),
                WallSide.Right => new Vector2(room.Bounds.MaxX, coordinate),
                WallSide.Bottom => new Vector2(coordinate, room.Bounds.MinY),
                WallSide.Left => new Vector2(room.Bounds.MinX, coordinate),
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Wall side must be defined.")
            };
            var inwardNormal = side switch
            {
                WallSide.Top => Vector2.down,
                WallSide.Right => Vector2.left,
                WallSide.Bottom => Vector2.up,
                WallSide.Left => Vector2.right,
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Wall side must be defined.")
            };
            var rotatedCenter = (Vector2)(Quaternion.Euler(0f, 0f, rotation)
                * geometry.LocalCenter);
            var centerDistance = Vector2.Dot(rotatedCenter, inwardNormal);
            var position = wallPoint
                + inwardNormal * (geometry.Size.y * 0.5f - centerDistance);

            return new WallOutletPlacement(side, position, rotation);
        }

        private static List<Vector2> CollectBlockedIntervals(
            RoomId roomId,
            WallSide side,
            IReadOnlyList<DoorPlan> doors,
            float clearance)
        {
            var blocked = new List<Vector2>();
            if (doors == null)
            {
                return blocked;
            }

            for (var i = 0; i < doors.Count; i++)
            {
                var door = doors[i];
                if (!TryGetDoorWall(door, roomId, out var wall) || wall.Side != side)
                {
                    continue;
                }

                blocked.Add(new Vector2(door.Span.Start - clearance, door.Span.End + clearance));
            }

            blocked.Sort((a, b) => a.x.CompareTo(b.x));
            return blocked;
        }

        private static float? FindWidestFreeIntervalMidpoint(
            float validStart,
            float validEnd,
            IReadOnlyList<Vector2> blocked)
        {
            var cursor = validStart;
            var bestStart = 0f;
            var bestEnd = 0f;
            var bestLength = -1f;

            for (var i = 0; i < blocked.Count; i++)
            {
                var blockStart = Mathf.Clamp(blocked[i].x, validStart, validEnd);
                var blockEnd = Mathf.Clamp(blocked[i].y, validStart, validEnd);
                if (blockEnd <= cursor)
                {
                    continue;
                }

                if (blockStart > cursor && blockStart - cursor > bestLength)
                {
                    bestStart = cursor;
                    bestEnd = blockStart;
                    bestLength = blockStart - cursor;
                }

                cursor = Mathf.Max(cursor, blockEnd);
                if (cursor >= validEnd)
                {
                    break;
                }
            }

            if (validEnd - cursor > bestLength)
            {
                bestStart = cursor;
                bestEnd = validEnd;
                bestLength = validEnd - cursor;
            }

            return bestLength >= 0f ? (bestStart + bestEnd) * 0.5f : null;
        }

        private static float? FindClosestFreeCoordinate(
            float clickedCoordinate,
            float validStart,
            float validEnd,
            IReadOnlyList<Vector2> blocked)
        {
            var clampedClick = Mathf.Clamp(clickedCoordinate, validStart, validEnd);
            if (!IsBlocked(clampedClick, blocked))
            {
                return clampedClick;
            }

            float? closest = null;
            var closestDistance = float.PositiveInfinity;
            ConsiderCandidate(validStart);
            ConsiderCandidate(validEnd);
            for (var i = 0; i < blocked.Count; i++)
            {
                ConsiderCandidate(Mathf.Clamp(blocked[i].x, validStart, validEnd));
                ConsiderCandidate(Mathf.Clamp(blocked[i].y, validStart, validEnd));
            }

            return closest;

            void ConsiderCandidate(float candidate)
            {
                if (IsBlocked(candidate, blocked))
                {
                    return;
                }

                var distance = Mathf.Abs(candidate - clickedCoordinate);
                if (distance < closestDistance)
                {
                    closest = candidate;
                    closestDistance = distance;
                }
            }
        }

        private static bool IsBlocked(float coordinate, IReadOnlyList<Vector2> blocked)
        {
            for (var i = 0; i < blocked.Count; i++)
            {
                if (coordinate > blocked[i].x && coordinate < blocked[i].y)
                {
                    return true;
                }
            }

            return false;
        }

        private static WallSide FindNearestWall(
            RoomBounds2D bounds,
            Vector2 point,
            out float distance)
        {
            var nearestSide = OrderedSides[0];
            distance = DistanceToWall(bounds, nearestSide, point);
            for (var i = 1; i < OrderedSides.Length; i++)
            {
                var candidateSide = OrderedSides[i];
                var candidateDistance = DistanceToWall(bounds, candidateSide, point);
                if (candidateDistance < distance)
                {
                    nearestSide = candidateSide;
                    distance = candidateDistance;
                }
            }

            return nearestSide;
        }

        private static float DistanceToWall(RoomBounds2D bounds, WallSide side, Vector2 point)
        {
            Vector2 closestPoint;
            switch (side)
            {
                case WallSide.Top:
                    closestPoint = new Vector2(Mathf.Clamp(point.x, bounds.MinX, bounds.MaxX), bounds.MaxY);
                    break;
                case WallSide.Right:
                    closestPoint = new Vector2(bounds.MaxX, Mathf.Clamp(point.y, bounds.MinY, bounds.MaxY));
                    break;
                case WallSide.Bottom:
                    closestPoint = new Vector2(Mathf.Clamp(point.x, bounds.MinX, bounds.MaxX), bounds.MinY);
                    break;
                case WallSide.Left:
                    closestPoint = new Vector2(bounds.MinX, Mathf.Clamp(point.y, bounds.MinY, bounds.MaxY));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, "Wall side must be defined.");
            }

            return Vector2.Distance(point, closestPoint);
        }

        private static bool IsOccupied(
            WallSide side,
            IReadOnlyCollection<WallSide> occupiedWallSides)
        {
            if (occupiedWallSides == null)
            {
                return false;
            }

            foreach (var occupiedSide in occupiedWallSides)
            {
                if (occupiedSide == side)
                {
                    return true;
                }
            }

            return false;
        }

        private static ClickedWallPlacementResult Failure(ClickedWallPlacementStatus status)
        {
            return new ClickedWallPlacementResult(status, default);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinitePositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }

        private static bool IsFiniteNonNegative(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        private static bool TryGetDoorWall(DoorPlan door, RoomId roomId, out RoomWallId wall)
        {
            if (door.WallA.RoomId == roomId)
            {
                wall = door.WallA;
                return true;
            }

            if (door.WallB.RoomId == roomId)
            {
                wall = door.WallB;
                return true;
            }

            wall = default;
            return false;
        }

        private static float GetRotationDegrees(WallSide side)
        {
            return side switch
            {
                WallSide.Top => 180f,
                WallSide.Right => 90f,
                WallSide.Bottom => 0f,
                WallSide.Left => -90f,
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Wall side must be defined.")
            };
        }
    }
}
