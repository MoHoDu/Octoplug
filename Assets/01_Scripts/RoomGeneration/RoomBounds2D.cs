using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Immutable axis-aligned room rectangle with exact edge semantics.</summary>
    public readonly struct RoomBounds2D : IEquatable<RoomBounds2D>
    {
        public RoomBounds2D(float minX, float minY, float width, float height)
        {
            GeometryValidation.RequireFinite(minX, nameof(minX));
            GeometryValidation.RequireFinite(minY, nameof(minY));
            GeometryValidation.RequirePositive(width, nameof(width));
            GeometryValidation.RequirePositive(height, nameof(height));

            MinX = minX;
            MinY = minY;
            MaxX = minX + width;
            MaxY = minY + height;
            GeometryValidation.RequireFinite(MaxX, nameof(width));
            GeometryValidation.RequireFinite(MaxY, nameof(height));
            if (MaxX <= MinX)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Width must remain positive at the supplied coordinate precision.");
            }

            if (MaxY <= MinY)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Height must remain positive at the supplied coordinate precision.");
            }
        }

        public float MinX { get; }
        public float MinY { get; }
        public float MaxX { get; }
        public float MaxY { get; }
        public float Width => MaxX - MinX;
        public float Height => MaxY - MinY;
        public bool IsValid => IsFinite(MinX) && IsFinite(MinY) && IsFinite(MaxX) && IsFinite(MaxY)
            && MaxX > MinX && MaxY > MinY;
        public Point2D Center => new(MinX + Width * 0.5f, MinY + Height * 0.5f);

        public bool Overlaps(RoomBounds2D other)
        {
            return MinX < other.MaxX && MaxX > other.MinX
                && MinY < other.MaxY && MaxY > other.MinY;
        }

        public bool Equals(RoomBounds2D other)
        {
            return MinX.Equals(other.MinX) && MinY.Equals(other.MinY)
                && MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY);
        }

        public override bool Equals(object obj) => obj is RoomBounds2D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = MinX.GetHashCode();
                hash = (hash * 397) ^ MinY.GetHashCode();
                hash = (hash * 397) ^ MaxX.GetHashCode();
                return (hash * 397) ^ MaxY.GetHashCode();
            }
        }

        public override string ToString() => $"[{MinX}, {MinY}]–[{MaxX}, {MaxY}]";

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
