namespace Octoplug.RoomGeneration
{
    /// <summary>Explicit authoring inputs needed to validate a door span.</summary>
    public readonly struct DoorPlanningOptions
    {
        public DoorPlanningOptions(float doorWidth, float safetyMargin)
        {
            GeometryValidation.RequirePositive(doorWidth, nameof(doorWidth));
            GeometryValidation.RequirePositive(safetyMargin, nameof(safetyMargin));
            DoorWidth = doorWidth;
            SafetyMargin = safetyMargin;
        }

        public float DoorWidth { get; }
        public float SafetyMargin { get; }

        internal void Validate()
        {
            GeometryValidation.RequirePositive(DoorWidth, nameof(DoorWidth));
            GeometryValidation.RequirePositive(SafetyMargin, nameof(SafetyMargin));
        }
    }
}
