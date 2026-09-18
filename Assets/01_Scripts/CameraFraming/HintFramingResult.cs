namespace Octoplug.CameraFraming
{
    /// <summary>Result of one hint-framing decision.</summary>
    public readonly struct HintFramingResult
    {
        public HintFramingResult(float targetOrthographicSize, bool changed)
        {
            TargetOrthographicSize = targetOrthographicSize;
            Changed = changed;
        }

        /// <summary>
        /// The orthographic size needed so the hint room's full bounds (plus margin) fit within
        /// the viewport at the current camera position. Equal to the request's current size when
        /// <see cref="Changed"/> is false. Never smaller than the request's current size — hint
        /// framing only ever zooms out, never in.
        /// </summary>
        public float TargetOrthographicSize { get; }

        /// <summary>True when the hint room's full bounds were not already fully visible and the camera must zoom out to include them.</summary>
        public bool Changed { get; }
    }
}
