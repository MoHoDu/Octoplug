namespace Octoplug.GameFlow
{
    public static class GameplayInputLock
    {
        public static bool IsLocked { get; set; }
        public static bool AllowCameraMovementDuringLock { get; set; }
        public static bool SuppressUntilPointerRelease { get; set; }
    }
}
