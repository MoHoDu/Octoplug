using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Tests
{
    public sealed class RoomPlannerTests
    {
        private static readonly DoorPlanningOptions Options = new(2f, 1f);

        [Test]
        public void RejectsOverlapThenSelectsNextValidCandidateInCallerOrder()
        {
            var layout = Layout(Room("A", 0f, 0f, 10f, 10f));
            var candidates = new[]
            {
                Candidate("Overlap", 5f, 0f, 10f, 10f),
                Candidate("Right", 10f, 0f, 10f, 10f)
            };

            var result = RoomPlanner.PlanNext(layout, candidates, Options, new SelectFirstMidpointPolicy());

            Assert.That(result.Success, Is.True);
            Assert.That(result.SelectedCandidateIndex, Is.EqualTo(1));
            Assert.That(result.Plan.Room.Id, Is.EqualTo(new RoomId("Right")));
            Assert.That(result.Rejections[0].Reason, Is.EqualTo(RoomCandidateRejection.OverlapsExistingRoom));
        }

        [Test]
        public void ReorderingValidCandidatesControlsSelection()
        {
            var layout = Layout(Room("A", 0f, 0f, 10f, 10f));
            var right = Candidate("Right", 10f, 0f, 10f, 10f);
            var top = Candidate("Top", 0f, 10f, 10f, 10f);

            var first = RoomPlanner.PlanNext(layout, new[] { right, top }, Options, new SelectFirstMidpointPolicy());
            var second = RoomPlanner.PlanNext(layout, new[] { top, right }, Options, new SelectFirstMidpointPolicy());

            Assert.That(first.Plan.Room.Id, Is.EqualTo(right.Id));
            Assert.That(second.Plan.Room.Id, Is.EqualTo(top.Id));
        }

        [Test]
        public void RequiresAtLeastOneDoorPlan()
        {
            var result = RoomPlanner.PlanNext(
                Layout(Room("A", 0f, 0f, 10f, 10f)),
                new[] { Candidate("B", 10f, 0f, 10f, 10f) },
                Options,
                new NoDoorPolicy());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Rejections[0].Reason, Is.EqualTo(RoomCandidateRejection.NoDoorSelected));
        }

        [Test]
        public void SharedWallTooShortForWidthAndMarginsIsRejected()
        {
            var result = RoomPlanner.PlanNext(
                Layout(Room("A", 0f, 0f, 10f, 3f)),
                new[] { Candidate("B", 10f, 0f, 10f, 3f) },
                Options,
                new SelectFirstMidpointPolicy());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Rejections[0].Reason, Is.EqualTo(RoomCandidateRejection.NoUsableSharedWall));
        }

        [Test]
        public void DoorClearsCornersAndMatchesVerticalWall()
        {
            var result = RoomPlanner.PlanNext(
                Layout(Room("A", 0f, 0f, 10f, 10f)),
                new[] { Candidate("B", 10f, 0f, 10f, 10f) },
                Options,
                new SelectFirstMidpointPolicy());

            var door = result.Plan.DoorPlans[0];
            Assert.That(door.Orientation, Is.EqualTo(DoorOrientation.VerticalWall));
            Assert.That(door.Position.X, Is.EqualTo(10f));
            Assert.That(door.Span.Start, Is.GreaterThanOrEqualTo(2f));
            Assert.That(door.Span.End, Is.LessThanOrEqualTo(8f));
        }

        [Test]
        public void SafetyMarginMustBePositive()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new DoorPlanningOptions(2f, 0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new DoorPlanningOptions(2f, -1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => RoomPlanner.PlanNext(
                Layout(Room("A", 0f, 0f, 10f, 10f)),
                new[] { Candidate("B", 10f, 0f, 10f, 10f) },
                default,
                new SelectFirstMidpointPolicy()));
        }

        [Test]
        public void MinimumSafeCenterMaintainsExplicitCornerClearance()
        {
            var result = RoomPlanner.PlanNext(
                Layout(Room("A", 0f, 0f, 10f, 10f)),
                new[] { Candidate("B", 10f, 0f, 10f, 10f) },
                new DoorPlanningOptions(2f, 0.25f),
                new SelectFirstIntervalMinimumPolicy());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Plan.DoorPlans[0].Span.Start, Is.EqualTo(0.25f));
        }

        [Test]
        public void MinimumSafeCenterMaintainsExplicitIntersectionClearance()
        {
            var result = RoomPlanner.PlanNext(
                Layout(
                    Room("Lower", 0f, 0f, 10f, 5f),
                    Room("Upper", 0f, 5f, 10f, 5f)),
                new[] { Candidate("New", 10f, 0f, 10f, 10f) },
                new DoorPlanningOptions(2f, 0.25f),
                new SelectFirstIntervalMinimumPolicy());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Plan.DoorPlans[0].Span.End, Is.LessThan(5f));
        }

        [Test]
        public void DoorMatchesHorizontalWall()
        {
            var result = RoomPlanner.PlanNext(
                Layout(Room("A", 0f, 0f, 10f, 10f)),
                new[] { Candidate("B", 0f, 10f, 10f, 10f) },
                Options,
                new SelectFirstMidpointPolicy());

            Assert.That(result.Plan.DoorPlans[0].Orientation, Is.EqualTo(DoorOrientation.HorizontalWall));
            Assert.That(result.Plan.DoorPlans[0].Position.Y, Is.EqualTo(10f));
        }

        [Test]
        public void InvalidPolicyCenterRejectsCandidate()
        {
            var result = RoomPlanner.PlanNext(
                Layout(Room("A", 0f, 0f, 10f, 10f)),
                new[] { Candidate("B", 10f, 0f, 10f, 10f) },
                Options,
                new InvalidCenterPolicy());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Rejections[0].Reason, Is.EqualTo(RoomCandidateRejection.InvalidDoorProposal));
        }

        [Test]
        public void MultipleNeighborsOnDistinctWallsProduceMultipleDoors()
        {
            var layout = Layout(
                Room("Left", 0f, 0f, 10f, 10f),
                Room("Bottom", 10f, -10f, 10f, 10f));

            var result = RoomPlanner.PlanNext(
                layout,
                new[] { Candidate("New", 10f, 0f, 10f, 10f) },
                Options,
                new SelectAllMidpointsPolicy());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Plan.SharedWalls, Has.Count.EqualTo(2));
            Assert.That(result.Plan.DoorPlans, Has.Count.EqualTo(2));
            Assert.That(result.Plan.DoorPlans[0].WallA, Is.Not.EqualTo(result.Plan.DoorPlans[1].WallA));
        }

        [Test]
        public void MultipleNeighborsOnSameCandidateWallCannotCreateTwoDoors()
        {
            var layout = Layout(
                Room("Lower", 0f, 0f, 10f, 5f),
                Room("Upper", 0f, 5f, 10f, 5f));

            var result = RoomPlanner.PlanNext(
                layout,
                new[] { Candidate("New", 10f, 0f, 10f, 10f) },
                new DoorPlanningOptions(1f, 0.5f),
                new SelectAllMidpointsPolicy());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Rejections[0].Reason, Is.EqualTo(RoomCandidateRejection.InvalidDoorProposal));
        }

        [Test]
        public void PreviouslyOccupiedAdjacentWallCannotReceiveAnotherDoor()
        {
            var occupied = new RoomWallId(new RoomId("A"), WallSide.Right);
            var layout = RoomLayout.Create(new[] { Room("A", 0f, 0f, 10f, 10f) }, new[] { occupied });

            var result = RoomPlanner.PlanNext(
                layout,
                new[] { Candidate("B", 10f, 0f, 10f, 10f) },
                Options,
                new SelectFirstMidpointPolicy());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Rejections[0].Reason, Is.EqualTo(RoomCandidateRejection.NoUsableSharedWall));
        }

        [Test]
        public void IdenticalInputsProduceIdenticalPlanGeometry()
        {
            var layout = Layout(Room("A", 0f, 0f, 10f, 10f));
            var candidates = new[] { Candidate("B", 10f, 0f, 10f, 10f) };

            var first = RoomPlanner.PlanNext(layout, candidates, Options, new SelectFirstMidpointPolicy()).Plan;
            var second = RoomPlanner.PlanNext(layout, candidates, Options, new SelectFirstMidpointPolicy()).Plan;

            Assert.That(second.Room, Is.EqualTo(first.Room));
            Assert.That(second.SharedWalls[0], Is.EqualTo(first.SharedWalls[0]));
            Assert.That(second.DoorPlans[0].Position, Is.EqualTo(first.DoorPlans[0].Position));
            Assert.That(second.DoorPlans[0].Span, Is.EqualTo(first.DoorPlans[0].Span));
        }

        private static RoomLayout Layout(params RoomPlacement[] rooms) => RoomLayout.Create(rooms);
        private static RoomPlacement Room(string id, float x, float y, float width, float height)
            => new(new RoomId(id), new RoomBounds2D(x, y, width, height));
        private static RoomCandidate Candidate(string id, float x, float y, float width, float height)
            => new(new RoomId(id), new RoomBounds2D(x, y, width, height));
    }
}
