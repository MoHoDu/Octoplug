namespace Octoplug.Power
{
    /// <summary>
    /// Machine-readable reason an Allowed Power upgrade was rejected.
    /// </summary>
    public enum PowerStripUpgradeFailure
    {
        None,
        InvalidState,
        MaximumReached,
    }
}
