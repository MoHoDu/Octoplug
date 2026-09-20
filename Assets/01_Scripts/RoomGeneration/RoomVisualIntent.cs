namespace Octoplug.RoomGeneration
{
    /// <summary>Data-only visual and gameplay availability projection for a room state.</summary>
    public readonly struct RoomVisualIntent
    {
        private static readonly Rgb24 LockedColor = new(0x15, 0x78, 0x6B);
        private static readonly Rgb24 UnlockedColor = new(0x87, 0x96, 0x8E);

        private RoomVisualIntent(
            RoomLifecycleState state,
            RoomWallStyle wallStyle,
            Rgb24 wallColor,
            bool showLockIcon,
            bool showHatching,
            bool gameplayContentEnabled)
        {
            State = state;
            WallStyle = wallStyle;
            WallColor = wallColor;
            ShowLockIcon = showLockIcon;
            ShowHatching = showHatching;
            GameplayContentEnabled = gameplayContentEnabled;
        }

        public RoomLifecycleState State { get; }
        public RoomWallStyle WallStyle { get; }
        public Rgb24 WallColor { get; }
        public bool ShowLockIcon { get; }
        public bool ShowHatching { get; }
        public bool GameplayContentEnabled { get; }

        public static RoomVisualIntent For(RoomLifecycleState state)
        {
            return state switch
            {
                RoomLifecycleState.HintLocked =>
                    new RoomVisualIntent(state, RoomWallStyle.Dashed, LockedColor, true, true, false),
                RoomLifecycleState.UnlockedGenerated =>
                    new RoomVisualIntent(state, RoomWallStyle.Solid, UnlockedColor, false, false, true),
                _ => throw new System.ArgumentOutOfRangeException(nameof(state), state, "Room lifecycle state must be defined.")
            };
        }
    }
}
