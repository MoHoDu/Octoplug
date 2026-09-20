using System;
using Octoplug.RoomGeneration;

namespace Octoplug.CameraFraming
{
    /// <summary>Everything needed to decide whether a hint room needs the camera to zoom out.</summary>
    public readonly struct HintFramingRequest
    {
        public HintFramingRequest(
            Point2D cameraCenter,
            float currentOrthographicSize,
            float aspect,
            RoomBounds2D hintBounds,
            float margin)
        {
            RequirePositive(currentOrthographicSize, nameof(currentOrthographicSize));
            RequirePositive(aspect, nameof(aspect));
            if (!hintBounds.IsValid)
            {
                throw new ArgumentException("Hint bounds must be valid.", nameof(hintBounds));
            }

            RequireNonNegative(margin, nameof(margin));

            CameraCenter = cameraCenter;
            CurrentOrthographicSize = currentOrthographicSize;
            Aspect = aspect;
            HintBounds = hintBounds;
            Margin = margin;
        }

        public Point2D CameraCenter { get; }
        public float CurrentOrthographicSize { get; }
        public float Aspect { get; }
        public RoomBounds2D HintBounds { get; }
        public float Margin { get; }

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
