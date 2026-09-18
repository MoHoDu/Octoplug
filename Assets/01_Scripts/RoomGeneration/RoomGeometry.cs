using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Pure overlap, adjacency, and shared-wall calculations.</summary>
    public static class RoomGeometry
    {
        public static bool TryGetSharedWall(
            RoomPlacement candidate,
            RoomPlacement adjacent,
            out SharedWall sharedWall)
        {
            if (!candidate.IsValid)
            {
                throw new ArgumentException("Candidate placement must be valid.", nameof(candidate));
            }

            if (!adjacent.IsValid)
            {
                throw new ArgumentException("Adjacent placement must be valid.", nameof(adjacent));
            }

            var candidateBounds = candidate.Bounds;
            var adjacentBounds = adjacent.Bounds;

            if (candidateBounds.MinX == adjacentBounds.MaxX
                && TryOverlap(candidateBounds.MinY, candidateBounds.MaxY, adjacentBounds.MinY, adjacentBounds.MaxY, out var start, out var end))
            {
                sharedWall = Create(candidate, WallSide.Left, adjacent, WallSide.Right,
                    WallOrientation.Vertical, candidateBounds.MinX, start, end);
                return true;
            }

            if (candidateBounds.MaxX == adjacentBounds.MinX
                && TryOverlap(candidateBounds.MinY, candidateBounds.MaxY, adjacentBounds.MinY, adjacentBounds.MaxY, out start, out end))
            {
                sharedWall = Create(candidate, WallSide.Right, adjacent, WallSide.Left,
                    WallOrientation.Vertical, candidateBounds.MaxX, start, end);
                return true;
            }

            if (candidateBounds.MinY == adjacentBounds.MaxY
                && TryOverlap(candidateBounds.MinX, candidateBounds.MaxX, adjacentBounds.MinX, adjacentBounds.MaxX, out start, out end))
            {
                sharedWall = Create(candidate, WallSide.Bottom, adjacent, WallSide.Top,
                    WallOrientation.Horizontal, candidateBounds.MinY, start, end);
                return true;
            }

            if (candidateBounds.MaxY == adjacentBounds.MinY
                && TryOverlap(candidateBounds.MinX, candidateBounds.MaxX, adjacentBounds.MinX, adjacentBounds.MaxX, out start, out end))
            {
                sharedWall = Create(candidate, WallSide.Top, adjacent, WallSide.Bottom,
                    WallOrientation.Horizontal, candidateBounds.MaxY, start, end);
                return true;
            }

            sharedWall = default;
            return false;
        }

        public static IReadOnlyList<SharedWall> FindSharedWalls(RoomPlacement candidate, IReadOnlyList<RoomPlacement> rooms)
        {
            if (rooms == null)
            {
                throw new ArgumentNullException(nameof(rooms));
            }

            if (!candidate.IsValid)
            {
                throw new ArgumentException("Candidate placement must be valid.", nameof(candidate));
            }

            var result = new List<SharedWall>();
            for (var i = 0; i < rooms.Count; i++)
            {
                if (TryGetSharedWall(candidate, rooms[i], out var sharedWall))
                {
                    result.Add(sharedWall);
                }
            }

            result.Sort(SharedWallComparer.Instance);
            return result;
        }

        private static SharedWall Create(
            RoomPlacement candidate,
            WallSide candidateSide,
            RoomPlacement adjacent,
            WallSide adjacentSide,
            WallOrientation orientation,
            float fixedCoordinate,
            float start,
            float end)
        {
            return new SharedWall(
                new RoomWallId(candidate.Id, candidateSide),
                new RoomWallId(adjacent.Id, adjacentSide),
                new WallSpan(orientation, fixedCoordinate, start, end));
        }

        private static bool TryOverlap(float firstStart, float firstEnd, float secondStart, float secondEnd, out float start, out float end)
        {
            start = Math.Max(firstStart, secondStart);
            end = Math.Min(firstEnd, secondEnd);
            return end > start;
        }

        private sealed class SharedWallComparer : IComparer<SharedWall>
        {
            public static readonly SharedWallComparer Instance = new();

            public int Compare(SharedWall first, SharedWall second)
            {
                var side = first.CandidateWall.Side.CompareTo(second.CandidateWall.Side);
                if (side != 0) return side;
                var start = first.Span.Start.CompareTo(second.Span.Start);
                if (start != 0) return start;
                var end = first.Span.End.CompareTo(second.Span.End);
                if (end != 0) return end;
                return first.AdjacentWall.RoomId.CompareTo(second.AdjacentWall.RoomId);
            }
        }
    }
}
