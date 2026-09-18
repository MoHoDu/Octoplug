using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Identity and world-independent bounds for a room in a layout.</summary>
    public readonly struct RoomPlacement : IEquatable<RoomPlacement>
    {
        public RoomPlacement(RoomId id, RoomBounds2D bounds)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Room id must be valid.", nameof(id));
            }

            if (!bounds.IsValid)
            {
                throw new ArgumentException("Room bounds must be valid.", nameof(bounds));
            }

            Id = id;
            Bounds = bounds;
        }

        public RoomId Id { get; }
        public RoomBounds2D Bounds { get; }
        public bool IsValid => Id.IsValid && Bounds.IsValid;

        public bool Equals(RoomPlacement other) => Id.Equals(other.Id) && Bounds.Equals(other.Bounds);
        public override bool Equals(object obj) => obj is RoomPlacement other && Equals(other);
        public override int GetHashCode() => (Id.GetHashCode() * 397) ^ Bounds.GetHashCode();
    }
}
