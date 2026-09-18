using System;
using System.Collections.Generic;
using Octoplug.RoomGeneration;

namespace Octoplug.CameraFraming
{
    /// <summary>
    /// Derives the maximum orthographic size needed to see the entire generated house, from the
    /// actual generated Room bounds rather than a hardcoded room count or size.
    /// </summary>
    public static class DynamicZoomLimit
    {
        public static RoomBounds2D CombineBounds(IReadOnlyList<RoomBounds2D> roomBounds)
        {
            if (roomBounds == null)
            {
                throw new ArgumentNullException(nameof(roomBounds));
            }

            if (roomBounds.Count == 0)
            {
                throw new ArgumentException("At least one room bounds is required to compute house bounds.", nameof(roomBounds));
            }

            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;
            for (var i = 0; i < roomBounds.Count; i++)
            {
                var bounds = roomBounds[i];
                if (bounds.MinX < minX)
                {
                    minX = bounds.MinX;
                }

                if (bounds.MinY < minY)
                {
                    minY = bounds.MinY;
                }

                if (bounds.MaxX > maxX)
                {
                    maxX = bounds.MaxX;
                }

                if (bounds.MaxY > maxY)
                {
                    maxY = bounds.MaxY;
                }
            }

            return new RoomBounds2D(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// The orthographic size at which the whole house bounds (plus margin) fits the viewport,
        /// never below <paramref name="minOrthographicSize"/>.
        /// </summary>
        public static float ComputeMaxOrthographicSize(RoomBounds2D houseBounds, float aspect, float margin, float minOrthographicSize)
        {
            if (!houseBounds.IsValid)
            {
                throw new ArgumentException("House bounds must be valid.", nameof(houseBounds));
            }

            RequirePositive(aspect, nameof(aspect));
            RequireNonNegative(margin, nameof(margin));
            RequirePositive(minOrthographicSize, nameof(minOrthographicSize));

            var halfHeightNeeded = houseBounds.Height * 0.5f + margin;
            var halfWidthNeeded = houseBounds.Width * 0.5f + margin;
            var requiredForWidth = halfWidthNeeded / aspect;
            var required = Math.Max(halfHeightNeeded, requiredForWidth);
            return Math.Max(minOrthographicSize, required);
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
