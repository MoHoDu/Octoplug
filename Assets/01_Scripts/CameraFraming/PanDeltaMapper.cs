using System;
using Octoplug.RoomGeneration;

namespace Octoplug.CameraFraming
{
    /// <summary>Maps pointer movement in screen pixels to map-style camera movement in world units.</summary>
    public static class PanDeltaMapper
    {
        public static Point2D Map(
            Point2D screenDelta,
            float orthographicSize,
            float aspect,
            float viewportWidthPixels,
            float viewportHeightPixels,
            float sensitivity)
        {
            RequirePositive(orthographicSize, nameof(orthographicSize));
            RequirePositive(aspect, nameof(aspect));
            RequirePositive(viewportWidthPixels, nameof(viewportWidthPixels));
            RequirePositive(viewportHeightPixels, nameof(viewportHeightPixels));
            RequireNonNegative(sensitivity, nameof(sensitivity));

            var worldWidth = orthographicSize * 2f * aspect;
            var worldHeight = orthographicSize * 2f;
            return new Point2D(
                -screenDelta.X / viewportWidthPixels * worldWidth * sensitivity,
                -screenDelta.Y / viewportHeightPixels * worldHeight * sensitivity);
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
