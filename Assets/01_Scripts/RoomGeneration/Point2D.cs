using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Engine-independent 2D point used by room and door plans.</summary>
    public readonly struct Point2D : IEquatable<Point2D>
    {
        public Point2D(float x, float y)
        {
            GeometryValidation.RequireFinite(x, nameof(x));
            GeometryValidation.RequireFinite(y, nameof(y));
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }

        public bool Equals(Point2D other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Point2D other && Equals(other);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(Point2D left, Point2D right) => left.Equals(right);
        public static bool operator !=(Point2D left, Point2D right) => !left.Equals(right);
    }
}
