using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Closed scalar interval used for valid door-center choices.</summary>
    public readonly struct CoordinateInterval : IEquatable<CoordinateInterval>
    {
        public CoordinateInterval(float min, float max)
        {
            GeometryValidation.RequireFinite(min, nameof(min));
            GeometryValidation.RequireFinite(max, nameof(max));
            if (max < min)
            {
                throw new ArgumentOutOfRangeException(nameof(max), "Interval maximum must not be less than minimum.");
            }

            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }
        public bool Contains(float value) => value >= Min && value <= Max;

        public bool Equals(CoordinateInterval other) => Min.Equals(other.Min) && Max.Equals(other.Max);
        public override bool Equals(object obj) => obj is CoordinateInterval other && Equals(other);
        public override int GetHashCode() => (Min.GetHashCode() * 397) ^ Max.GetHashCode();
    }
}
