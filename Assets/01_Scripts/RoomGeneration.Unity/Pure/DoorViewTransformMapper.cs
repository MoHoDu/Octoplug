using System;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Unity
{
    /// <summary>Maps a Door plan to the canonical authored hinge transform without Unity engine types.</summary>
    public static class DoorViewTransformMapper
    {
        public static DoorViewTransform Map(DoorPlan doorPlan)
        {
            if (doorPlan == null)
            {
                throw new ArgumentNullException(nameof(doorPlan));
            }

            var ownerWall = SelectOwnerWall(doorPlan.SharedWall);
            var rotation = RotationFor(ownerWall.Side);
            var halfWidth = doorPlan.Span.Length * 0.5f;
            var openingOffset = RotatedLocalUp(rotation, halfWidth);
            var hingePosition = new Point2D(
                doorPlan.Position.X - openingOffset.X,
                doorPlan.Position.Y - openingOffset.Y);
            return new DoorViewTransform(ownerWall, hingePosition, rotation);
        }

        private static RoomWallId SelectOwnerWall(SharedWall sharedWall)
        {
            if (sharedWall.CandidateWall.Side == WallSide.Top)
            {
                if (sharedWall.AdjacentWall.Side != WallSide.Bottom)
                {
                    throw new ArgumentException("A Top candidate wall must connect to an adjacent Bottom wall.", nameof(sharedWall));
                }

                return sharedWall.AdjacentWall;
            }

            return sharedWall.CandidateWall;
        }

        private static float RotationFor(WallSide side)
        {
            return side switch
            {
                WallSide.Left => 0f,
                WallSide.Right => 180f,
                WallSide.Bottom => 90f,
                _ => throw new ArgumentOutOfRangeException(nameof(side), side, "Top walls must be canonicalized before rotation mapping.")
            };
        }

        private static Point2D RotatedLocalUp(float rotation, float magnitude)
        {
            return rotation switch
            {
                0f => new Point2D(0f, magnitude),
                90f => new Point2D(-magnitude, 0f),
                180f => new Point2D(0f, -magnitude),
                _ => throw new ArgumentOutOfRangeException(nameof(rotation))
            };
        }
    }
}
