using System;

namespace Octoplug.RoomGeneration
{
    internal static class GeometryValidation
    {
        public static void RequireFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
            }
        }

        public static void RequirePositive(float value, string parameterName)
        {
            RequireFinite(value, parameterName);
            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than zero.");
            }
        }

        public static void RequireNonNegative(float value, string parameterName)
        {
            RequireFinite(value, parameterName);
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must not be negative.");
            }
        }
    }
}
