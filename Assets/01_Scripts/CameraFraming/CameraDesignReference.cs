namespace Octoplug.CameraFraming
{
    /// <summary>
    /// The framing design reference: 1920x1080 at 16:9. Every framing calculation
    /// (<see cref="HintFramingCalculator"/>, <see cref="DynamicZoomLimit"/>) uses the actual
    /// runtime camera aspect, never a hardcoded pixel resolution, so the result is always correct
    /// at any device aspect. This constant exists only to document the aspect the UX was designed
    /// and tuned against, and to let tests pin behavior at that specific aspect.
    /// </summary>
    public static class CameraDesignReference
    {
        public const int DesignWidthPixels = 1920;
        public const int DesignHeightPixels = 1080;
        public const float DesignAspect = DesignWidthPixels / (float)DesignHeightPixels;
    }
}
