namespace Octoplug.CameraFraming
{
    public enum PanGestureOwnership
    {
        None,
        CameraPan,
        GameplayBlocked,
        MultiTouchSuppressed
    }

    /// <summary>Latches one gesture owner from pointer down until release or multitouch cancellation.</summary>
    public sealed class PanGestureTracker
    {
        public PanGestureOwnership Ownership { get; private set; }

        public bool TryBegin(bool isGameplayBlocked)
        {
            if (Ownership != PanGestureOwnership.None)
            {
                return false;
            }

            Ownership = isGameplayBlocked
                ? PanGestureOwnership.GameplayBlocked
                : PanGestureOwnership.CameraPan;
            return true;
        }

        public void SuppressForMultiTouch()
        {
            Ownership = PanGestureOwnership.MultiTouchSuppressed;
        }

        public void EndPointer()
        {
            if (Ownership != PanGestureOwnership.MultiTouchSuppressed)
            {
                Ownership = PanGestureOwnership.None;
            }
        }

        public void EndAllTouches()
        {
            Ownership = PanGestureOwnership.None;
        }
    }
}
