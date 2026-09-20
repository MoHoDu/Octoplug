using System;
using Octoplug.RoomGeneration;

namespace Octoplug.CameraFraming
{
    public readonly struct PanAxisRange
    {
        public PanAxisRange(float min, float max, bool isCollapsed)
        {
            Min = min;
            Max = max;
            IsCollapsed = isCollapsed;
        }

        public float Min { get; }
        public float Max { get; }
        public bool IsCollapsed { get; }

        public float Clamp(float current, float proposed)
        {
            if (IsCollapsed)
            {
                return current;
            }

            return Math.Max(Min, Math.Min(Max, proposed));
        }
    }

    public readonly struct PanCenterRange
    {
        public PanCenterRange(PanAxisRange x, PanAxisRange y)
        {
            X = x;
            Y = y;
        }

        public PanAxisRange X { get; }
        public PanAxisRange Y { get; }

        public Point2D Clamp(Point2D current, Point2D proposed)
        {
            return new Point2D(
                X.Clamp(current.X, proposed.X),
                Y.Clamp(current.Y, proposed.Y));
        }
    }

    /// <summary>Derives legal camera-center ranges from actual House and viewport bounds.</summary>
    public static class PanBoundaryCalculator
    {
        public static PanCenterRange Compute(
            RoomBounds2D houseBounds,
            float orthographicSize,
            float aspect,
            float margin)
        {
            if (!houseBounds.IsValid)
            {
                throw new ArgumentException("House bounds must be valid.", nameof(houseBounds));
            }

            RequirePositive(orthographicSize, nameof(orthographicSize));
            RequirePositive(aspect, nameof(aspect));
            RequireNonNegative(margin, nameof(margin));

            var halfHeight = orthographicSize;
            var halfWidth = orthographicSize * aspect;
            return new PanCenterRange(
                ComputeAxis(houseBounds.MinX - margin, houseBounds.MaxX + margin, halfWidth),
                ComputeAxis(houseBounds.MinY - margin, houseBounds.MaxY + margin, halfHeight));
        }

        private static PanAxisRange ComputeAxis(float contentMin, float contentMax, float viewportHalfExtent)
        {
            var min = contentMin + viewportHalfExtent;
            var max = contentMax - viewportHalfExtent;
            return min <= max
                ? new PanAxisRange(min, max, isCollapsed: false)
                : new PanAxisRange(0f, 0f, isCollapsed: true);
        }

        private static void RequirePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be a positive finite number.");
            }
        }

        private static void RequireNonNegative(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be a non-negative finite number.");
            }
        }
    }
}
