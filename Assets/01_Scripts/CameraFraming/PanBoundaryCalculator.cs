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
            RequireFinite(current, nameof(current));
            RequireFinite(proposed, nameof(proposed));
            return ClampProposed(current, proposed);
        }

        /// <summary>입력 변위를 제한한 최종 좌표를 반환한다. 반환값은 변위가 아니다.</summary>
        public float ClampDelta(float current, float delta)
        {
            RequireFinite(current, nameof(current));
            RequireFinite(delta, nameof(delta));
            return ClampProposed(current, (double)current + delta);
        }

        public float ClosestSafeCoordinate(float current)
        {
            RequireFinite(current, nameof(current));
            return Math.Max(Min, Math.Min(Max, current));
        }

        private float ClampProposed(float current, double proposed)
        {
            // current + Clamp(delta, Min(0, Min-current), Max(0, Max-current))와 동치이며,
            // 큰 좌표에서 경계를 빼고 다시 더할 때 발생하는 상쇄 오차를 피한다.
            var lower = Math.Min(current, Min);
            var upper = Math.Max(current, Max);
            if (IsCollapsed)
            {
                // 전체 콘텐츠가 보이면 고정하고, 가려진 경우 가장 가까운 안전 좌표까지만 복구한다.
                var safe = ClosestSafeCoordinate(current);
                lower = Math.Min(current, safe);
                upper = Math.Max(current, safe);
            }

            return (float)Math.Max(lower, Math.Min(upper, proposed));
        }

        private static void RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "좌표와 입력은 유한해야 합니다.");
            }
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

        /// <summary>입력 변위를 축별로 제한한 최종 카메라 좌표를 반환한다. 반환값은 변위가 아니다.</summary>
        public Point2D ClampDelta(Point2D current, Point2D delta)
        {
            return new Point2D(
                X.ClampDelta(current.X, delta.X),
                Y.ClampDelta(current.Y, delta.Y));
        }

        public Point2D ClosestSafeCoordinate(Point2D current)
        {
            return new Point2D(X.ClosestSafeCoordinate(current.X), Y.ClosestSafeCoordinate(current.Y));
        }
    }

    /// <summary>실제 집과 상하단 가림을 제외한 화면으로 카메라 중심의 이동·안전 구간을 계산한다.</summary>
    public static class PanBoundaryCalculator
    {
        public static PanCenterRange Compute(
            RoomBounds2D houseBounds,
            float orthographicSize,
            float aspect,
            float margin)
        {
            return Compute(houseBounds, orthographicSize, aspect, margin, 0f, 0f);
        }

        public static PanCenterRange Compute(
            RoomBounds2D houseBounds,
            float orthographicSize,
            float aspect,
            float margin,
            float topInsetFraction)
        {
            return Compute(houseBounds, orthographicSize, aspect, margin, topInsetFraction, 0f);
        }

        public static PanCenterRange Compute(
            RoomBounds2D houseBounds,
            float orthographicSize,
            float aspect,
            float margin,
            float topInsetFraction,
            float bottomInsetFraction)
        {
            if (!houseBounds.IsValid)
            {
                throw new ArgumentException("House bounds must be valid.", nameof(houseBounds));
            }

            RequirePositive(orthographicSize, nameof(orthographicSize));
            RequirePositive(aspect, nameof(aspect));
            RequireNonNegative(margin, nameof(margin));
            RequireInset(topInsetFraction, nameof(topInsetFraction));
            RequireInset(bottomInsetFraction, nameof(bottomInsetFraction));
            if (!new UsableViewport(topInsetFraction, bottomInsetFraction).IsValid)
            {
                throw new ArgumentOutOfRangeException(nameof(bottomInsetFraction), "상하단 가림 비율의 합은 1보다 작아야 합니다.");
            }

            var halfHeight = (double)orthographicSize;
            var halfWidth = halfHeight * aspect;
            var usableBottom = halfHeight * (2d * bottomInsetFraction - 1d);
            var usableTop = halfHeight * (1d - 2d * topInsetFraction);
            return new PanCenterRange(
                ComputeAxis((double)houseBounds.MinX - margin, (double)houseBounds.MaxX + margin, -halfWidth, halfWidth),
                ComputeAxis((double)houseBounds.MinY - margin, (double)houseBounds.MaxY + margin, usableBottom, usableTop));
        }

        private static PanAxisRange ComputeAxis(double contentMin, double contentMax, double viewportMin, double viewportMax)
        {
            var min = contentMin - viewportMin;
            var max = contentMax - viewportMax;
            if (Math.Abs(min) > float.MaxValue || Math.Abs(max) > float.MaxValue)
            {
                throw new ArgumentOutOfRangeException("orthographicSize", "계산한 카메라 구간은 유한한 float 범위여야 합니다.");
            }

            // 화면이 더 넓으면 양 끝을 뒤집어 콘텐츠 전체가 들어오는 containment 구간을 보존한다.
            return min <= max
                ? new PanAxisRange((float)min, (float)max, isCollapsed: false)
                : new PanAxisRange((float)max, (float)min, isCollapsed: true);
        }

        private static void RequireInset(float value, string parameterName)
        {
            RequireNonNegative(value, parameterName);
            if (value >= 1f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "가림 비율은 1보다 작아야 합니다.");
            }
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
