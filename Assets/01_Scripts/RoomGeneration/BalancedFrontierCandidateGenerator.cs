using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Generates exact integer-lattice frontier candidates in balanced breadth-first order.</summary>
    public static class BalancedFrontierCandidateGenerator
    {
        private const int MinExactInteger = -16777216;
        private const int MaxExactInteger = 16777216;

        private static readonly WallSide[] CardinalOrder =
        {
            WallSide.Right,
            WallSide.Top,
            WallSide.Left,
            WallSide.Bottom
        };

        public static IReadOnlyList<RoomCandidate> Generate(
            RoomLayout layout,
            IReadOnlyList<IntegerRoomSize> roomSizes,
            string candidateIdPrefix)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (roomSizes == null)
            {
                throw new ArgumentNullException(nameof(roomSizes));
            }

            if (roomSizes.Count == 0)
            {
                throw new ArgumentException("At least one room size is required.", nameof(roomSizes));
            }

            if (string.IsNullOrWhiteSpace(candidateIdPrefix))
            {
                throw new ArgumentException("Candidate id prefix must not be empty.", nameof(candidateIdPrefix));
            }

            if (layout.Rooms.Count == 0)
            {
                throw new ArgumentException("A frontier requires at least one existing room.", nameof(layout));
            }

            ValidateIntegerLattice(layout);
            ValidateRoomSizes(roomSizes);
            var originBounds = layout.Rooms[0].Bounds;
            var roomDepths = CalculateRoomDepths(layout.Rooms);
            var directionExtents = CalculateDirectionExtents(layout.Rooms, originBounds);
            var expansionIndex = layout.Rooms.Count - 1;
            var directionStart = expansionIndex % CardinalOrder.Length;
            var footprintStart = expansionIndex % roomSizes.Count;
            var candidatesByBounds = new Dictionary<RoomBounds2D, CandidateBounds>();

            for (var roomIndex = 0; roomIndex < layout.Rooms.Count; roomIndex++)
            {
                var anchor = layout.Rooms[roomIndex].Bounds;
                for (var directionOffset = 0; directionOffset < CardinalOrder.Length; directionOffset++)
                {
                    var directionOrder = (directionStart + directionOffset) % CardinalOrder.Length;
                    var side = CardinalOrder[directionOrder];
                    for (var footprintOffset = 0; footprintOffset < roomSizes.Count; footprintOffset++)
                    {
                        var sizeIndex = (footprintStart + footprintOffset) % roomSizes.Count;
                        AddAlongWall(
                            layout,
                            anchor,
                            side,
                            roomSizes[sizeIndex],
                            roomDepths[roomIndex] + 1,
                            directionExtents,
                            directionOrder,
                            directionOffset,
                            sizeIndex,
                            footprintOffset,
                            candidatesByBounds);
                    }
                }
            }

            var candidates = new List<CandidateBounds>(candidatesByBounds.Values);
            candidates.Sort(CandidateBoundsComparer.Instance);
            var candidateId = NextCandidateId(layout.Rooms, candidateIdPrefix);
            var result = new List<RoomCandidate>(candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                result.Add(new RoomCandidate(candidateId, candidates[i].Bounds));
            }

            return result.AsReadOnly();
        }

        private static void AddAlongWall(
            RoomLayout layout,
            RoomBounds2D anchor,
            WallSide side,
            IntegerRoomSize size,
            int breadthFirstDepth,
            int[] directionExtents,
            int directionOrder,
            int directionPriority,
            int sizeIndex,
            int footprintPriority,
            Dictionary<RoomBounds2D, CandidateBounds> candidatesByBounds)
        {
            var width = size.Width;
            var height = size.Height;
            if (side is WallSide.Left or WallSide.Right)
            {
                var minX = side == WallSide.Left
                    ? CheckedCoordinate((long)ToInteger(anchor.MinX) - width)
                    : ToInteger(anchor.MaxX);
                RequireGeneratedEnd((long)minX + width);
                var minimumY = CheckedCoordinate((long)ToInteger(anchor.MinY) - height + 1L);
                var maximumY = CheckedCoordinate((long)ToInteger(anchor.MaxY) - 1L);
                for (var minY = minimumY; minY <= maximumY; minY++)
                {
                    RequireGeneratedEnd((long)minY + height);
                    var bounds = new RoomBounds2D(minX, minY, width, height);
                    AddIfFrontier(
                        layout,
                        bounds,
                        breadthFirstDepth,
                        directionExtents,
                        directionOrder,
                        directionPriority,
                        sizeIndex,
                        footprintPriority,
                        CenterScore(anchor.MinY, anchor.MaxY, bounds.MinY, bounds.MaxY),
                        minY - minimumY,
                        candidatesByBounds);
                }

                return;
            }

            var minYForSide = side == WallSide.Bottom
                ? CheckedCoordinate((long)ToInteger(anchor.MinY) - height)
                : ToInteger(anchor.MaxY);
            RequireGeneratedEnd((long)minYForSide + height);
            var minimumX = CheckedCoordinate((long)ToInteger(anchor.MinX) - width + 1L);
            var maximumX = CheckedCoordinate((long)ToInteger(anchor.MaxX) - 1L);
            for (var minXForSide = minimumX; minXForSide <= maximumX; minXForSide++)
            {
                RequireGeneratedEnd((long)minXForSide + width);
                var bounds = new RoomBounds2D(minXForSide, minYForSide, width, height);
                AddIfFrontier(
                    layout,
                    bounds,
                    breadthFirstDepth,
                    directionExtents,
                    directionOrder,
                    directionPriority,
                    sizeIndex,
                    footprintPriority,
                    CenterScore(anchor.MinX, anchor.MaxX, bounds.MinX, bounds.MaxX),
                    minXForSide - minimumX,
                    candidatesByBounds);
            }
        }

        private static void AddIfFrontier(
            RoomLayout layout,
            RoomBounds2D bounds,
            int breadthFirstDepth,
            int[] directionExtents,
            int directionOrder,
            int directionPriority,
            int sizeIndex,
            int footprintPriority,
            float centerScore,
            int alignmentOrder,
            Dictionary<RoomBounds2D, CandidateBounds> candidatesByBounds)
        {
            if (OverlapsAny(bounds, layout.Rooms))
            {
                return;
            }

            var placement = new RoomPlacement(new RoomId("__frontier__"), bounds);
            if (RoomGeometry.FindSharedWalls(placement, layout.Rooms).Count == 0)
            {
                return;
            }

            var candidate = new CandidateBounds(
                bounds,
                breadthFirstDepth,
                directionExtents[directionOrder],
                directionPriority,
                footprintPriority,
                centerScore,
                alignmentOrder,
                directionOrder,
                sizeIndex);
            if (!candidatesByBounds.TryGetValue(bounds, out var existing)
                || CandidateBoundsComparer.Instance.Compare(candidate, existing) < 0)
            {
                candidatesByBounds[bounds] = candidate;
            }
        }

        private static int[] CalculateRoomDepths(IReadOnlyList<RoomPlacement> rooms)
        {
            var depths = new int[rooms.Count];
            for (var i = 1; i < depths.Length; i++)
            {
                depths[i] = int.MaxValue;
            }

            var queue = new Queue<int>();
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                for (var neighbor = 0; neighbor < rooms.Count; neighbor++)
                {
                    if (depths[neighbor] != int.MaxValue
                        || !RoomGeometry.TryGetSharedWall(rooms[current], rooms[neighbor], out _))
                    {
                        continue;
                    }

                    depths[neighbor] = depths[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }

            for (var i = 0; i < depths.Length; i++)
            {
                if (depths[i] == int.MaxValue)
                {
                    throw new ArgumentException("Room layout must be connected to its first seed room.", nameof(rooms));
                }
            }

            return depths;
        }

        private static int[] CalculateDirectionExtents(
            IReadOnlyList<RoomPlacement> rooms,
            RoomBounds2D originBounds)
        {
            var minX = originBounds.MinX;
            var minY = originBounds.MinY;
            var maxX = originBounds.MaxX;
            var maxY = originBounds.MaxY;
            for (var i = 1; i < rooms.Count; i++)
            {
                minX = Math.Min(minX, rooms[i].Bounds.MinX);
                minY = Math.Min(minY, rooms[i].Bounds.MinY);
                maxX = Math.Max(maxX, rooms[i].Bounds.MaxX);
                maxY = Math.Max(maxY, rooms[i].Bounds.MaxY);
            }

            return new[]
            {
                checked(ToInteger(maxX) - ToInteger(originBounds.MaxX)),
                checked(ToInteger(maxY) - ToInteger(originBounds.MaxY)),
                checked(ToInteger(originBounds.MinX) - ToInteger(minX)),
                checked(ToInteger(originBounds.MinY) - ToInteger(minY))
            };
        }

        private static float CenterScore(float anchorMin, float anchorMax, float candidateMin, float candidateMax)
            => Math.Abs((candidateMin + candidateMax) - (anchorMin + anchorMax));

        private static RoomId NextCandidateId(IReadOnlyList<RoomPlacement> rooms, string prefix)
        {
            var existingIds = new HashSet<RoomId>();
            for (var i = 0; i < rooms.Count; i++)
            {
                existingIds.Add(rooms[i].Id);
            }

            for (var ordinal = 0; ; ordinal++)
            {
                var candidateId = new RoomId($"{prefix}-{ordinal:D4}");
                if (!existingIds.Contains(candidateId))
                {
                    return candidateId;
                }
            }
        }

        private static bool OverlapsAny(RoomBounds2D bounds, IReadOnlyList<RoomPlacement> rooms)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                if (bounds.Overlaps(rooms[i].Bounds))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateIntegerLattice(RoomLayout layout)
        {
            for (var i = 0; i < layout.Rooms.Count; i++)
            {
                var bounds = layout.Rooms[i].Bounds;
                RequireInteger(bounds.MinX, nameof(layout));
                RequireInteger(bounds.MinY, nameof(layout));
                RequireInteger(bounds.MaxX, nameof(layout));
                RequireInteger(bounds.MaxY, nameof(layout));
            }
        }

        private static void ValidateRoomSizes(IReadOnlyList<IntegerRoomSize> roomSizes)
        {
            for (var i = 0; i < roomSizes.Count; i++)
            {
                if (roomSizes[i].Width > MaxExactInteger || roomSizes[i].Height > MaxExactInteger)
                {
                    throw new ArgumentException("Room sizes must fit the exact integer range supported by float geometry.", nameof(roomSizes));
                }
            }
        }

        private static int CheckedCoordinate(long value)
        {
            if (value < MinExactInteger || value > MaxExactInteger)
            {
                throw new ArgumentException("Generated room coordinates exceed the exact integer range supported by float geometry.");
            }

            return (int)value;
        }

        private static void RequireGeneratedEnd(long value) => CheckedCoordinate(value);

        private static int ToInteger(float value)
        {
            RequireInteger(value, nameof(value));
            return (int)value;
        }

        private static void RequireInteger(float value, string parameterName)
        {
            if (value != Math.Truncate(value) || value < MinExactInteger || value > MaxExactInteger)
            {
                throw new ArgumentException("Room layout bounds must use exactly representable integer lattice coordinates.", parameterName);
            }
        }

        private readonly struct CandidateBounds
        {
            public CandidateBounds(
                RoomBounds2D bounds,
                int breadthFirstDepth,
                int directionExtent,
                int directionPriority,
                int footprintPriority,
                float centerScore,
                int alignmentOrder,
                int directionOrder,
                int sizeIndex)
            {
                Bounds = bounds;
                BreadthFirstDepth = breadthFirstDepth;
                DirectionExtent = directionExtent;
                DirectionPriority = directionPriority;
                FootprintPriority = footprintPriority;
                CenterScore = centerScore;
                AlignmentOrder = alignmentOrder;
                DirectionOrder = directionOrder;
                SizeIndex = sizeIndex;
            }

            public RoomBounds2D Bounds { get; }
            public int BreadthFirstDepth { get; }
            public int DirectionExtent { get; }
            public int DirectionPriority { get; }
            public int FootprintPriority { get; }
            public float CenterScore { get; }
            public int AlignmentOrder { get; }
            public int DirectionOrder { get; }
            public int SizeIndex { get; }
        }

        private sealed class CandidateBoundsComparer : IComparer<CandidateBounds>
        {
            public static readonly CandidateBoundsComparer Instance = new();

            public int Compare(CandidateBounds first, CandidateBounds second)
            {
                var depth = first.BreadthFirstDepth.CompareTo(second.BreadthFirstDepth);
                if (depth != 0) return depth;
                var extent = first.DirectionExtent.CompareTo(second.DirectionExtent);
                if (extent != 0) return extent;
                var directionPriority = first.DirectionPriority.CompareTo(second.DirectionPriority);
                if (directionPriority != 0) return directionPriority;
                var footprintPriority = first.FootprintPriority.CompareTo(second.FootprintPriority);
                if (footprintPriority != 0) return footprintPriority;
                var centerScore = first.CenterScore.CompareTo(second.CenterScore);
                if (centerScore != 0) return centerScore;
                var alignmentOrder = first.AlignmentOrder.CompareTo(second.AlignmentOrder);
                if (alignmentOrder != 0) return alignmentOrder;
                var direction = first.DirectionOrder.CompareTo(second.DirectionOrder);
                if (direction != 0) return direction;
                var size = first.SizeIndex.CompareTo(second.SizeIndex);
                if (size != 0) return size;
                var minX = first.Bounds.MinX.CompareTo(second.Bounds.MinX);
                if (minX != 0) return minX;
                var minY = first.Bounds.MinY.CompareTo(second.Bounds.MinY);
                if (minY != 0) return minY;
                var width = first.Bounds.Width.CompareTo(second.Bounds.Width);
                return width != 0 ? width : first.Bounds.Height.CompareTo(second.Bounds.Height);
            }
        }
    }
}
