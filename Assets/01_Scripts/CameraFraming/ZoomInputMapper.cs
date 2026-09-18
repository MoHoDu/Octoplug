using System;

namespace Octoplug.CameraFraming
{
    /// <summary>
    /// Pure conversion of raw pointer/scroll/pinch deltas into an orthographic-size delta. Reading
    /// the actual device input and applying the resulting delta to a camera are separate concerns.
    /// </summary>
    public static class ZoomInputMapper
    {
        /// <summary>Positive scroll (wheel up / two-finger scroll up) zooms in, so the size delta is negative.</summary>
        public static float MapScrollDelta(float scrollY, float sensitivity)
        {
            RequireFiniteSensitivity(sensitivity);
            RequireFinite(scrollY, nameof(scrollY));
            return -scrollY * sensitivity;
        }

        /// <summary>Fingers spreading apart (distance increasing) zooms in, so the size delta is negative.</summary>
        public static float MapPinchDelta(float previousDistance, float currentDistance, float sensitivity)
        {
            RequireFiniteSensitivity(sensitivity);
            RequireNonNegative(previousDistance, nameof(previousDistance));
            RequireNonNegative(currentDistance, nameof(currentDistance));
            return -(currentDistance - previousDistance) * sensitivity;
        }

        private static void RequireFiniteSensitivity(float sensitivity)
        {
            if (float.IsNaN(sensitivity) || float.IsInfinity(sensitivity) || sensitivity < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sensitivity), "Sensitivity must be a non-negative finite number.");
            }
        }

        private static void RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            }
        }

        private static void RequireNonNegative(float value, string parameterName)
        {
            RequireFinite(value, parameterName);
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Distance must not be negative.");
            }
        }
    }
}
