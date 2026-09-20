using System;

namespace Octoplug.CameraFraming
{
    /// <summary>
    /// Computes the orthographic size needed to bring a newly generated hint room's full bounds
    /// (not just its center) fully into view without moving the camera and without ever zooming
    /// in past the viewer's current size.
    /// </summary>
    public static class HintFramingCalculator
    {
        private const float Tolerance = 0.0001f;

        public static HintFramingResult Compute(HintFramingRequest request)
        {
            var bounds = request.HintBounds;
            var center = request.CameraCenter;

            // Distance from the camera to the FARTHEST edge on each axis — using both bounds.Min
            // and bounds.Max, never only the bounds center — so the whole room, not just its
            // midpoint, ends up inside the viewport.
            var requiredVertical = Math.Max(Math.Abs(bounds.MaxY - center.Y), Math.Abs(center.Y - bounds.MinY)) + request.Margin;
            var requiredHorizontal = (Math.Max(Math.Abs(bounds.MaxX - center.X), Math.Abs(center.X - bounds.MinX)) + request.Margin) / request.Aspect;
            var required = Math.Max(requiredVertical, requiredHorizontal);

            // Hint framing only ever zooms out; it never zooms in past what the user is already
            // viewing, and it is never capped by any other zoom limit — the whole hint room must
            // always end up fully visible.
            var desired = Math.Max(request.CurrentOrthographicSize, required);
            var changed = desired > request.CurrentOrthographicSize + Tolerance;
            return new HintFramingResult(changed ? desired : request.CurrentOrthographicSize, changed);
        }
    }
}
