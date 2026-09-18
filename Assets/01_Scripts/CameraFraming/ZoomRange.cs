using System;

namespace Octoplug.CameraFraming
{
    /// <summary>Engine-independent inclusive orthographic-size range; the minimum is never below zero and the maximum is never below the minimum.</summary>
    public readonly struct ZoomRange : IEquatable<ZoomRange>
    {
        public ZoomRange(float min, float max)
        {
            RequireFinite(min, nameof(min));
            RequireFinite(max, nameof(max));
            if (min <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(min), "Minimum orthographic size must be positive.");
            }

            if (max < min)
            {
                throw new ArgumentOutOfRangeException(nameof(max), "Maximum orthographic size must be at least the minimum.");
            }

            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }

        public float Clamp(float value)
        {
            if (value < Min)
            {
                return Min;
            }

            return value > Max ? Max : value;
        }

        public bool Equals(ZoomRange other) => Min.Equals(other.Min) && Max.Equals(other.Max);
        public override bool Equals(object obj) => obj is ZoomRange other && Equals(other);
        public override int GetHashCode() => (Min.GetHashCode() * 397) ^ Max.GetHashCode();
        public override string ToString() => $"[{Min}, {Max}]";

        private static void RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            }
        }
    }
}
