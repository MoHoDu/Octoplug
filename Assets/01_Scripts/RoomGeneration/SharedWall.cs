using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Pairwise positive-length contact between opposite room walls.</summary>
    public readonly struct SharedWall : IEquatable<SharedWall>
    {
        public SharedWall(RoomWallId candidateWall, RoomWallId adjacentWall, WallSpan span)
        {
            if (!candidateWall.IsValid || !adjacentWall.IsValid || !span.IsValid)
            {
                throw new ArgumentException("Shared wall data must be valid.");
            }

            if (candidateWall.Orientation != adjacentWall.Orientation
                || candidateWall.Orientation != span.Orientation)
            {
                throw new ArgumentException("Shared wall orientations must match.");
            }

            CandidateWall = candidateWall;
            AdjacentWall = adjacentWall;
            Span = span;
        }

        public RoomWallId CandidateWall { get; }
        public RoomWallId AdjacentWall { get; }
        public WallSpan Span { get; }

        public bool Equals(SharedWall other)
        {
            return CandidateWall.Equals(other.CandidateWall)
                && AdjacentWall.Equals(other.AdjacentWall)
                && Span.Equals(other.Span);
        }

        public override bool Equals(object obj) => obj is SharedWall other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = CandidateWall.GetHashCode();
                hash = (hash * 397) ^ AdjacentWall.GetHashCode();
                return (hash * 397) ^ Span.GetHashCode();
            }
        }
    }
}
