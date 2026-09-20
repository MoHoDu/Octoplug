using System;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>Engine-free owning wall, hinge position, and allowed Z rotation for one Door view.</summary>
    public readonly struct DoorViewTransform : IEquatable<DoorViewTransform>
    {
        public DoorViewTransform(RoomWallId ownerWall, Point2D hingePosition, float zRotationDegrees)
        {
            if (!ownerWall.IsValid)
            {
                throw new ArgumentException("Door owner wall must be valid.", nameof(ownerWall));
            }

            if (zRotationDegrees != 0f && zRotationDegrees != 90f && zRotationDegrees != 180f)
            {
                throw new ArgumentOutOfRangeException(nameof(zRotationDegrees), "Door Z rotation must be 0, 90, or 180 degrees.");
            }

            OwnerWall = ownerWall;
            HingePosition = hingePosition;
            ZRotationDegrees = zRotationDegrees;
        }

        public RoomWallId OwnerWall { get; }
        public Point2D HingePosition { get; }
        public float ZRotationDegrees { get; }

        public bool Equals(DoorViewTransform other)
        {
            return OwnerWall.Equals(other.OwnerWall)
                && HingePosition.Equals(other.HingePosition)
                && ZRotationDegrees.Equals(other.ZRotationDegrees);
        }

        public override bool Equals(object obj) => obj is DoorViewTransform other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = OwnerWall.GetHashCode();
                hash = (hash * 397) ^ HingePosition.GetHashCode();
                return (hash * 397) ^ ZRotationDegrees.GetHashCode();
            }
        }
    }
}
