using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Caller-supplied candidate; ordering remains the caller's decision.</summary>
    public readonly struct RoomCandidate
    {
        public RoomCandidate(RoomId id, RoomBounds2D bounds)
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
        public Point2D CandidatePosition => Bounds.Center;
        public RoomPlacement ToPlacement() => new(Id, Bounds);
    }
}
