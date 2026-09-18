using NUnit.Framework;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;

namespace Octoplug.RoomGeneration.Tests
{
    public sealed class DoorViewTransformMapperTests
    {
        [TestCase(WallSide.Left, WallSide.Right, 0f, "Candidate")]
        [TestCase(WallSide.Right, WallSide.Left, 180f, "Candidate")]
        [TestCase(WallSide.Bottom, WallSide.Top, 90f, "Candidate")]
        [TestCase(WallSide.Top, WallSide.Bottom, 90f, "Adjacent")]
        public void MapsCanonicalOwnerAndAllowedRotation(
            WallSide candidateSide,
            WallSide adjacentSide,
            float expectedRotation,
            string expectedOwnerId)
        {
            var plan = Door(candidateSide, adjacentSide, 6f, 2f);

            var transform = DoorViewTransformMapper.Map(plan);

            Assert.That(transform.OwnerWall.RoomId, Is.EqualTo(new RoomId(expectedOwnerId)));
            Assert.That(transform.OwnerWall.Side, Is.Not.EqualTo(WallSide.Top));
            Assert.That(transform.ZRotationDegrees, Is.EqualTo(expectedRotation));
            Assert.That(transform.ZRotationDegrees, Is.AnyOf(0f, 90f, 180f));
        }

        [TestCase(WallSide.Left, WallSide.Right)]
        [TestCase(WallSide.Right, WallSide.Left)]
        [TestCase(WallSide.Bottom, WallSide.Top)]
        [TestCase(WallSide.Top, WallSide.Bottom)]
        public void HingePlusRotatedOpeningAxisReconstructsDoorCenter(
            WallSide candidateSide,
            WallSide adjacentSide)
        {
            var plan = Door(candidateSide, adjacentSide, 6f, 2f);

            var transform = DoorViewTransformMapper.Map(plan);
            var openingAxis = RotatedLocalUp(transform.ZRotationDegrees, plan.Span.Length * 0.5f);
            var reconstructedCenter = new Point2D(
                transform.HingePosition.X + openingAxis.X,
                transform.HingePosition.Y + openingAxis.Y);

            Assert.That(reconstructedCenter, Is.EqualTo(plan.Position));
        }

        [TestCase(WallSide.Left, WallSide.Right, 1f, 0f)]
        [TestCase(WallSide.Right, WallSide.Left, -1f, 0f)]
        [TestCase(WallSide.Bottom, WallSide.Top, 0f, 1f)]
        [TestCase(WallSide.Top, WallSide.Bottom, 0f, 1f)]
        public void LocalPositiveXLeafPointsIntoOwningRoom(
            WallSide candidateSide,
            WallSide adjacentSide,
            float expectedX,
            float expectedY)
        {
            var transform = DoorViewTransformMapper.Map(Door(candidateSide, adjacentSide, 6f, 2f));

            var leafDirection = RotatedLocalRight(transform.ZRotationDegrees);

            Assert.That(leafDirection, Is.EqualTo(new Point2D(expectedX, expectedY)));
        }

        private static DoorPlan Door(
            WallSide candidateSide,
            WallSide adjacentSide,
            float centerCoordinate,
            float width)
        {
            var orientation = candidateSide is WallSide.Left or WallSide.Right
                ? WallOrientation.Vertical
                : WallOrientation.Horizontal;
            var sharedWall = new SharedWall(
                new RoomWallId(new RoomId("Candidate"), candidateSide),
                new RoomWallId(new RoomId("Adjacent"), adjacentSide),
                new WallSpan(orientation, 10f, 0f, 12f));
            return new DoorPlan(sharedWall, centerCoordinate, width);
        }

        private static Point2D RotatedLocalUp(float rotation, float magnitude)
        {
            return rotation switch
            {
                0f => new Point2D(0f, magnitude),
                90f => new Point2D(-magnitude, 0f),
                _ => new Point2D(0f, -magnitude)
            };
        }

        private static Point2D RotatedLocalRight(float rotation)
        {
            return rotation switch
            {
                0f => new Point2D(1f, 0f),
                90f => new Point2D(0f, 1f),
                _ => new Point2D(-1f, 0f)
            };
        }
    }
}
