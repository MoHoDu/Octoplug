namespace Octoplug.Power
{
    /// <summary>
    /// Minimal shared contract for a Plug or a Socket endpoint.
    /// P0-1B data foundation only: no drag, validation, or connection
    /// gameplay logic is implemented here.
    /// </summary>
    public interface IPowerConnector
    {
        /// <summary>True when this endpoint currently has a counterpart assigned.</summary>
        bool IsConnected { get; }

        /// <summary>World-space transform used for routing and distance queries.</summary>
        UnityEngine.Transform ConnectorTransform { get; }
    }
}
