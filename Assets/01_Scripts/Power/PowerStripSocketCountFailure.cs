namespace Octoplug.Power
{
    /// <summary>
    /// Machine-readable reason an ActiveSocketCount transaction was rejected.
    /// Player-facing feedback and relocation policy are intentionally outside
    /// the PowerStrip state layer.
    /// </summary>
    public enum PowerStripSocketCountFailure
    {
        None,
        OutOfRange,
        MissingAuthoredConfiguration,
        SocketCountDecreaseUnsupported,
        PlacementServiceUnavailable,
        PlacementUnavailable,
    }
}
