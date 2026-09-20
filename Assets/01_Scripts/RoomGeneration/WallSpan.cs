using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Absolute axis-aligned wall segment.</summary>
    public readonly struct WallSpan : IEquatable<WallSpan>
    {
        public WallSpan(WallOrientation orientation, float fixedCoordinate, float start, float end)
        {
            if (!Enum.IsDefined(typeof(WallOrientation), orientation))
            {
                throw new ArgumentOutOfRangeException(nameof(orientation), orientation, "Wall orientation must be defined.");
            }

            GeometryValidation.RequireFinite(fixedCoordinate, nameof(fixedCoordinate));
            GeometryValidation.RequireFinite(start, nameof(start));
            GeometryValidation.RequireFinite(end, nameof(end));
            if (end <= start)
            {
                throw new ArgumentOutOfRangeException(nameof(end), "Wall span end must be greater than start.");
            }

            Orientation = orientation;
            FixedCoordinate = fixedCoordinate;
            Start = start;
            End = end;
        }

        public WallOrientation Orientation { get; }
        public float FixedCoordinate { get; }
        public float Start { get; }
        public float End { get; }
        public float Length => End - Start;
        public bool IsValid => Enum.IsDefined(typeof(WallOrientation), Orientation)
            && IsFinite(FixedCoordinate) && IsFinite(Start) && IsFinite(End) && End > Start;

        public Point2D PointAt(float coordinate)
        {
            GeometryValidation.RequireFinite(coordinate, nameof(coordinate));
            return Orientation == WallOrientation.Horizontal
                ? new Point2D(coordinate, FixedCoordinate)
                : new Point2D(FixedCoordinate, coordinate);
        }

        public bool Contains(float start, float end) => start >= Start && end <= End && end > start;

        public bool Equals(WallSpan other)
        {
            return Orientation == other.Orientation
                && FixedCoordinate.Equals(other.FixedCoordinate)
                && Start.Equals(other.Start)
                && End.Equals(other.End);
        }

        public override bool Equals(object obj) => obj is WallSpan other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Orientation;
                hash = (hash * 397) ^ FixedCoordinate.GetHashCode();
                hash = (hash * 397) ^ Start.GetHashCode();
                return (hash * 397) ^ End.GetHashCode();
            }
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
