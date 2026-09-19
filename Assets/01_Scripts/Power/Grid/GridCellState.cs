namespace Octoplug.Power.Grid
{
    /// <summary>State of one cell in the cable-routing grid.</summary>
    public enum GridCellState
    {
        /// <summary>Not inside any registered room floor area.</summary>
        Unknown,

        /// <summary>Inside a room floor area and free of walls.</summary>
        Walkable,

        /// <summary>Blocked by a Wall-layer collider; cables cannot cross it.</summary>
        Blocked,

        /// <summary>Inside a Door-layer collider; the only way between two rooms.</summary>
        Door
    }
}
