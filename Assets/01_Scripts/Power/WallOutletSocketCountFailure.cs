namespace Octoplug.Power
{
    /// <summary>
    /// Machine-readable reason a WallOutlet ActiveSocketCount increase was rejected.
    /// Player-facing reward and feedback behavior remain outside this state layer.
    /// </summary>
    public enum WallOutletSocketCountFailure
    {
        None,
        OutOfRange,
        MaximumReached,
        MissingAuthoredConfiguration,
        SocketCountDecreaseUnsupported,
    }
}
