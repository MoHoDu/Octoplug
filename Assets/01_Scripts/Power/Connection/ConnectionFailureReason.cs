namespace Octoplug.Power.Connection
{
    /// <summary>
    /// Machine-readable reason a Plug-to-Socket connection was rejected.
    /// Presentation code may map these values to UI later; gameplay code must
    /// not depend on player-facing wording.
    /// </summary>
    public enum ConnectionFailureReason
    {
        None,
        HousePowerExceeded,
        PowerStripPowerExceeded,
        SelfConnection,
        CircularConnection,
        SocketInactive,
    }
}
