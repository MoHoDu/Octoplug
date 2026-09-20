using NUnit.Framework;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Tests
{
    public sealed class WidestSafeIntervalMidpointPolicyTests
    {
        [Test]
        public void SelectsMidpointOfWidestSafeInterval()
        {
            var opportunity = Opportunity(
                "Candidate",
                WallSide.Right,
                "Adjacent",
                WallSide.Left,
                new CoordinateInterval(1f, 3f),
                new CoordinateInterval(10f, 16f),
                new CoordinateInterval(20f, 21f));

            var proposals = new WidestSafeIntervalMidpointPolicy().SelectDoors(new[] { opportunity });

            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(proposals[0].OpportunityIndex, Is.EqualTo(0));
            Assert.That(proposals[0].CenterCoordinate, Is.EqualTo(13f));
        }

        [Test]
        public void EqualWidthsChooseEarlierIntervalDeterministically()
        {
            var opportunity = Opportunity(
                "Candidate",
                WallSide.Right,
                "Adjacent",
                WallSide.Left,
                new CoordinateInterval(8f, 12f),
                new CoordinateInterval(0f, 4f));

            var proposals = new WidestSafeIntervalMidpointPolicy().SelectDoors(new[] { opportunity });

            Assert.That(proposals[0].CenterCoordinate, Is.EqualTo(10f));
        }

        [Test]
        public void SharedCompleteWallReceivesOnlyWidestDoor()
        {
            var first = Opportunity(
                "Candidate",
                WallSide.Right,
                "Lower",
                WallSide.Left,
                new CoordinateInterval(0f, 2f));
            var second = Opportunity(
                "Candidate",
                WallSide.Right,
                "Upper",
                WallSide.Left,
                new CoordinateInterval(5f, 11f));

            var proposals = new WidestSafeIntervalMidpointPolicy().SelectDoors(new[] { first, second });

            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(proposals[0].OpportunityIndex, Is.EqualTo(1));
            Assert.That(proposals[0].CenterCoordinate, Is.EqualTo(8f));
        }

        [Test]
        public void DistinctCandidateWallsStillProduceOnlyGloballyWidestDoor()
        {
            var right = Opportunity(
                "Candidate",
                WallSide.Right,
                "Right",
                WallSide.Left,
                new CoordinateInterval(2f, 8f));
            var top = Opportunity(
                "Candidate",
                WallSide.Top,
                "Top",
                WallSide.Bottom,
                new CoordinateInterval(1f, 5f));

            var proposals = new WidestSafeIntervalMidpointPolicy().SelectDoors(new[] { right, top });

            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(proposals[0].OpportunityIndex, Is.EqualTo(0));
            Assert.That(proposals[0].CenterCoordinate, Is.EqualTo(5f));
        }

        private static DoorPlacementOpportunity Opportunity(
            string candidateId,
            WallSide candidateSide,
            string adjacentId,
            WallSide adjacentSide,
            params CoordinateInterval[] intervals)
        {
            var orientation = candidateSide is WallSide.Left or WallSide.Right
                ? WallOrientation.Vertical
                : WallOrientation.Horizontal;
            var wall = new SharedWall(
                new RoomWallId(new RoomId(candidateId), candidateSide),
                new RoomWallId(new RoomId(adjacentId), adjacentSide),
                new WallSpan(orientation, 0f, 0f, 20f));
            return new DoorPlacementOpportunity(wall, intervals);
        }
    }
}
