using System;
using Octoplug.RoomGeneration;

namespace Octoplug.CameraFraming
{
    /// <summary>상하단 가림을 제외한 직교 화면의 초기 배치와 위치 고정 fit을 계산한다.</summary>
    public readonly struct UsableViewport
    {
        public UsableViewport(float topInsetFraction, float bottomInsetFraction)
        {
            TopInsetFraction = topInsetFraction;
            BottomInsetFraction = bottomInsetFraction;
        }

        public float TopInsetFraction { get; }
        public float BottomInsetFraction { get; }
        public bool IsValid => IsNonNegativeFinite(TopInsetFraction)
            && IsNonNegativeFinite(BottomInsetFraction)
            && (double)TopInsetFraction + BottomInsetFraction < 1d;

        /// <summary>전체 bounds를 가용 화면 중앙에 맞춘다. 실패 시 size는 0, center는 기본값이다.</summary>
        public bool TryComputeInitialFit(
            RoomBounds2D bounds,
            float aspect,
            float margin,
            float minOrthographicSize,
            out float orthographicSize,
            out Point2D center)
        {
            orthographicSize = 0f;
            center = default;
            if (!CanFit(bounds, aspect, margin, minOrthographicSize))
            {
                return false;
            }

            var halfWidth = ((double)bounds.MaxX - bounds.MinX) * 0.5d + margin;
            var halfHeight = ((double)bounds.MaxY - bounds.MinY) * 0.5d + margin;
            var usableFraction = 1d - TopInsetFraction - BottomInsetFraction;
            var required = Math.Max(minOrthographicSize, Math.Max(halfWidth / aspect, halfHeight / usableFraction));
            if (!TryRoundSizeUp(required, out var size))
            {
                return false;
            }

            var centerX = ((double)bounds.MinX + bounds.MaxX) * 0.5d;
            var centerY = ((double)bounds.MinY + bounds.MaxY) * 0.5d
                - size * ((double)BottomInsetFraction - TopInsetFraction);
            if (!FitsFloat(centerX) || !FitsFloat(centerY))
            {
                return false;
            }

            orthographicSize = size;
            center = new Point2D((float)centerX, (float)centerY);
            return true;
        }

        /// <summary>중심을 유지하며 zoom-out만 허용한다. 불가능하면 false와 유효한 현재 size(그 외 0)를 반환한다.</summary>
        public bool TryComputeFixedCenterFit(
            RoomBounds2D bounds,
            Point2D cameraCenter,
            float aspect,
            float margin,
            float currentOrthographicSize,
            out float orthographicSize)
        {
            orthographicSize = IsPositiveFinite(currentOrthographicSize) ? currentOrthographicSize : 0f;
            if (!CanFit(bounds, aspect, margin, currentOrthographicSize))
            {
                return false;
            }

            var horizontal = Math.Max(
                Math.Abs((double)bounds.MinX - cameraCenter.X),
                Math.Abs((double)bounds.MaxX - cameraCenter.X)) + margin;
            var lowerSize = Math.Max(currentOrthographicSize, horizontal / aspect);
            var upperSize = double.PositiveInfinity;

            // 한쪽 가림이 절반 이상이면 해당 부등식은 최소 size가 아닌 최대 size를 제한한다.
            if (!ConstrainSize(1d - 2d * TopInsetFraction,
                    (double)bounds.MaxY + margin - cameraCenter.Y, ref lowerSize, ref upperSize)
                || !ConstrainSize(1d - 2d * BottomInsetFraction,
                    (double)cameraCenter.Y - bounds.MinY + margin, ref lowerSize, ref upperSize)
                || !TryRoundSizeUp(lowerSize, out var size)
                || size > upperSize)
            {
                return false;
            }

            orthographicSize = size;
            return true;
        }

        private bool CanFit(RoomBounds2D bounds, float aspect, float margin, float size)
        {
            return IsValid && bounds.IsValid && IsPositiveFinite(aspect)
                && IsNonNegativeFinite(margin) && IsPositiveFinite(size);
        }

        private static bool ConstrainSize(double coefficient, double distance, ref double lower, ref double upper)
        {
            // coefficient * size >= distance를 부호에 맞춰 풀며, 0 계수도 독립 판정한다.
            if (coefficient > 0d)
            {
                lower = Math.Max(lower, distance / coefficient);
            }
            else if (coefficient < 0d)
            {
                upper = Math.Min(upper, distance / coefficient);
            }
            else if (distance > 0d)
            {
                return false;
            }

            return lower <= upper;
        }

        private static bool TryRoundSizeUp(double required, out float size)
        {
            size = 0f;
            if (!FitsFloat(required) || required <= 0d)
            {
                return false;
            }

            size = (float)required;
            // float 변환이 아래로 반올림되면 콘텐츠 끝이 가림 안으로 들어갈 수 있다.
            if (size < required)
            {
                size = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(size) + 1);
            }

            return IsPositiveFinite(size);
        }

        private static bool FitsFloat(double value) => !double.IsNaN(value)
            && value >= -float.MaxValue && value <= float.MaxValue;

        private static bool IsPositiveFinite(float value) => IsNonNegativeFinite(value) && value > 0f;

        private static bool IsNonNegativeFinite(float value) => !float.IsNaN(value)
            && !float.IsInfinity(value) && value >= 0f;
    }
}
