using System;
using NUnit.Framework;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Tests
{
    public sealed class RoomGeometryTests
    {
        [TestCase(-5f, 0f, WallSide.Right, WallSide.Left, WallOrientation.Vertical)]
        [TestCase(5f, 0f, WallSide.Left, WallSide.Right, WallOrientation.Vertical)]
        [TestCase(0f, -5f, WallSide.Top, WallSide.Bottom, WallOrientation.Horizontal)]
        [TestCase(0f, 5f, WallSide.Bottom, WallSide.Top, WallOrientation.Horizontal)]
        public void FindsAdjacencyInAllFourDirections(
            float candidateX,
            float candidateY,
            WallSide candidateSide,
            WallSide adjacentSide,
            WallOrientation orientation)
        {
            var adjacent = Room("A", 0f, 0f, 5f, 5f);
            var candidate = Room("B", candidateX, candidateY, 5f, 5f);

            Assert.That(RoomGeometry.TryGetSharedWall(candidate, adjacent, out var wall), Is.True);
            Assert.That(wall.CandidateWall.Side, Is.EqualTo(candidateSide));
            Assert.That(wall.AdjacentWall.Side, Is.EqualTo(adjacentSide));
            Assert.That(wall.Span.Orientation, Is.EqualTo(orientation));
            Assert.That(wall.Span.Length, Is.EqualTo(5f));
        }

        [Test]
        public void SeparatedRoomsAreNotAdjacent()
        {
            Assert.That(RoomGeometry.TryGetSharedWall(
                Room("B", 6f, 0f, 5f, 5f), Room("A", 0f, 0f, 5f, 5f), out _), Is.False);
        }

        [Test]
        public void CornerTouchIsNotAdjacency()
        {
            Assert.That(RoomGeometry.TryGetSharedWall(
                Room("B", 5f, 5f, 5f, 5f), Room("A", 0f, 0f, 5f, 5f), out _), Is.False);
        }

        [Test]
        public void SharedWallUsesOnlyPositiveOverlapInterval()
        {
            Assert.That(RoomGeometry.TryGetSharedWall(
                Room("B", 5f, 2f, 5f, 5f), Room("A", 0f, 0f, 5f, 5f), out var wall), Is.True);

            Assert.That(wall.Span.FixedCoordinate, Is.EqualTo(5f));
            Assert.That(wall.Span.Start, Is.EqualTo(2f));
            Assert.That(wall.Span.End, Is.EqualTo(5f));
        }

        [Test]
        public void InteriorOverlapIsRejectedButEdgeTouchIsNotOverlap()
        {
            var origin = new RoomBounds2D(0f, 0f, 5f, 5f);
            Assert.That(origin.Overlaps(new RoomBounds2D(4f, 0f, 5f, 5f)), Is.True);
            Assert.That(origin.Overlaps(new RoomBounds2D(5f, 0f, 5f, 5f)), Is.False);
        }

        [TestCase(0f, 5f)]
        [TestCase(-1f, 0f)]
        [TestCase(float.NaN, 5f)]
        [TestCase(float.PositiveInfinity, 5f)]
        public void InvalidBoundsAreRejected(float width, float height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoomBounds2D(0f, 0f, width, height));
        }

        [Test]
        public void DefaultBoundsCannotEnterPlacementOrCandidate()
        {
            Assert.Throws<ArgumentException>(() => new RoomPlacement(new RoomId("A"), default));
            Assert.Throws<ArgumentException>(() => new RoomCandidate(new RoomId("A"), default));
        }

        [Test]
        public void BoundsRejectDimensionsThatCollapseAtCoordinatePrecision()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoomBounds2D(1e20f, 0f, 1f, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoomBounds2D(0f, 1e20f, 10f, 1f));
        }

        [Test]
        public void LayoutRejectsDefaultPlacement()
        {
            Assert.Throws<ArgumentException>(() => RoomLayout.Create(new[] { default(RoomPlacement) }));
        }

        [Test]
        public void WallIdentifiersRejectInvalidInputs()
        {
            Assert.Throws<ArgumentException>(() => new RoomWallId(default, WallSide.Left));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoomWallId(new RoomId("A"), (WallSide)999));
            Assert.Throws<ArgumentException>(() => RoomLayout.Create(
                new[] { Room("A", 0f, 0f, 5f, 5f) },
                new[] { default(RoomWallId) }));
        }

        [Test]
        public void WallSpanRejectsUnknownOrientation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WallSpan((WallOrientation)999, 0f, 0f, 1f));
        }

        [Test]
        public void PublicGeometryRejectsDefaultPlacements()
        {
            var valid = Room("A", 0f, 0f, 5f, 5f);
            Assert.Throws<ArgumentException>(() => RoomGeometry.TryGetSharedWall(default, valid, out _));
            Assert.Throws<ArgumentException>(() => RoomGeometry.TryGetSharedWall(valid, default, out _));
            Assert.Throws<ArgumentException>(() => RoomGeometry.FindSharedWalls(default, new[] { valid }));
            Assert.Throws<ArgumentException>(() => RoomGeometry.FindSharedWalls(valid, new[] { default(RoomPlacement) }));
        }

        private static RoomPlacement Room(string id, float minX, float minY, float width, float height)
            => new(new RoomId(id), new RoomBounds2D(minX, minY, width, height));
    }
}
