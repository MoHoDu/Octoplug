using System;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Tests
{
    public sealed class BalancedFrontierCandidateGeneratorTests
    {
        [Test]
        public void GeneratesVariableSizeCandidatesWithExactIntegerGeometry()
        {
            var layout = RoomLayout.Create(new[] { Room("Origin", 0f, 0f, 2f, 2f) });
            var candidates = BalancedFrontierCandidateGenerator.Generate(
                layout,
                new[] { new IntegerRoomSize(1, 1), new IntegerRoomSize(3, 2) },
                "Candidate");

            Assert.That(candidates, Has.Some.Matches<RoomCandidate>(candidate =>
                candidate.Bounds.Equals(new RoomBounds2D(-3f, 0f, 3f, 2f))));
            Assert.That(candidates, Has.Some.Matches<RoomCandidate>(candidate =>
                candidate.Bounds.Equals(new RoomBounds2D(2f, 1f, 1f, 1f))));

            for (var i = 0; i < candidates.Count; i++)
            {
                Assert.That(candidates[i].Bounds.MinX, Is.EqualTo(MathF.Truncate(candidates[i].Bounds.MinX)));
                Assert.That(candidates[i].Bounds.MinY, Is.EqualTo(MathF.Truncate(candidates[i].Bounds.MinY)));
                Assert.That(candidates[i].Bounds.Overlaps(layout.Rooms[0].Bounds), Is.False);
                Assert.That(RoomGeometry.FindSharedWalls(candidates[i].ToPlacement(), layout.Rooms), Is.Not.Empty);
            }
        }

        [Test]
        public void IdenticalInputsProduceIdenticalOrderedCandidates()
        {
            var layout = RoomLayout.Create(new[] { Room("Origin", -1f, -1f, 2f, 2f) });
            var sizes = new[] { new IntegerRoomSize(1, 2), new IntegerRoomSize(2, 1) };

            var first = BalancedFrontierCandidateGenerator.Generate(layout, sizes, "Next");
            var second = BalancedFrontierCandidateGenerator.Generate(layout, sizes, "Next");

            Assert.That(second, Has.Count.EqualTo(first.Count));
            for (var i = 0; i < first.Count; i++)
            {
                Assert.That(second[i].Id, Is.EqualTo(first[i].Id));
                Assert.That(second[i].Bounds, Is.EqualTo(first[i].Bounds));
            }
        }

        [Test]
        public void RepeatedFrontierSelectionBalancesFourCardinalDirections()
        {
            var rooms = new List<RoomPlacement> { Room("Origin", 0f, 0f, 1f, 1f) };
            var selected = new List<RoomBounds2D>();
            for (var step = 0; step < 4; step++)
            {
                var layout = RoomLayout.Create(rooms);
                var candidates = BalancedFrontierCandidateGenerator.Generate(
                    layout,
                    new[] { new IntegerRoomSize(1, 1) },
                    $"Step{step}");
                var candidate = candidates[0];
                selected.Add(candidate.Bounds);
                rooms.Add(candidate.ToPlacement());
            }

            Assert.That(selected, Is.EqualTo(new[]
            {
                new RoomBounds2D(1f, 0f, 1f, 1f),
                new RoomBounds2D(0f, 1f, 1f, 1f),
                new RoomBounds2D(-1f, 0f, 1f, 1f),
                new RoomBounds2D(0f, -1f, 1f, 1f)
            }));
        }

        [Test]
        public void AllAlternativesShareOneLogicalNextRoomId()
        {
            var candidates = BalancedFrontierCandidateGenerator.Generate(
                RoomLayout.Create(new[] { Room("Origin", 0f, 0f, 2f, 2f) }),
                new[] { new IntegerRoomSize(1, 1), new IntegerRoomSize(2, 1) },
                "Next");

            Assert.That(candidates, Is.Not.Empty);
            for (var i = 1; i < candidates.Count; i++)
            {
                Assert.That(candidates[i].Id, Is.EqualTo(candidates[0].Id));
            }
        }

        [Test]
        public void RotatesFootprintPriorityWithExpansionOrdinal()
        {
            var rooms = new[]
            {
                Room("Origin", 0f, 0f, 1f, 1f),
                Room("Existing", 1f, 0f, 1f, 1f)
            };

            var candidates = BalancedFrontierCandidateGenerator.Generate(
                RoomLayout.Create(rooms),
                new[] { new IntegerRoomSize(2, 1), new IntegerRoomSize(1, 2) },
                "Next");

            Assert.That(candidates[0].Bounds.Width, Is.EqualTo(1f));
            Assert.That(candidates[0].Bounds.Height, Is.EqualTo(2f));
        }

        [Test]
        public void CentersCandidateAlongSharedEdgeBeforeOffsetAlternatives()
        {
            var candidates = BalancedFrontierCandidateGenerator.Generate(
                RoomLayout.Create(new[] { Room("Origin", 0f, 0f, 4f, 4f) }),
                new[] { new IntegerRoomSize(2, 2) },
                "Next");

            Assert.That(candidates[0].Bounds, Is.EqualTo(new RoomBounds2D(4f, 1f, 2f, 2f)));
        }

        [Test]
        public void SeedBfsDepthPrecedesOuterAnchorExtent()
        {
            var layout = RoomLayout.Create(new[]
            {
                Room("Origin", 0f, 0f, 1f, 1f),
                Room("Right1", 1f, 0f, 1f, 1f),
                Room("Right2", 2f, 0f, 1f, 1f)
            });

            var candidates = BalancedFrontierCandidateGenerator.Generate(
                layout,
                new[] { new IntegerRoomSize(1, 1) },
                "Next");

            Assert.That(candidates[0].Bounds, Is.EqualTo(new RoomBounds2D(-1f, 0f, 1f, 1f)));
        }

        [Test]
        public void RejectsLayoutDisconnectedFromSeed()
        {
            var layout = RoomLayout.Create(new[]
            {
                Room("Origin", 0f, 0f, 1f, 1f),
                Room("Island", 5f, 5f, 1f, 1f)
            });

            Assert.Throws<ArgumentException>(() => BalancedFrontierCandidateGenerator.Generate(
                layout,
                new[] { new IntegerRoomSize(1, 1) },
                "Next"));
        }

        [Test]
        public void SkipsGeneratedIdsAlreadyPresentInLayout()
        {
            var layout = RoomLayout.Create(new[] { Room("Next-0000", 0f, 0f, 1f, 1f) });

            var candidates = BalancedFrontierCandidateGenerator.Generate(
                layout,
                new[] { new IntegerRoomSize(1, 1) },
                "Next");

            Assert.That(candidates[0].Id, Is.EqualTo(new RoomId("Next-0001")));
        }

        [Test]
        public void RejectsNonIntegerLayoutInsteadOfSnappingIt()
        {
            var layout = RoomLayout.Create(new[] { Room("Origin", 0.25f, 0f, 2f, 2f) });

            Assert.Throws<ArgumentException>(() => BalancedFrontierCandidateGenerator.Generate(
                layout,
                new[] { new IntegerRoomSize(1, 1) },
                "Candidate"));
        }

        [Test]
        public void RejectsFrontierCoordinatesOutsideExactFloatIntegerRange()
        {
            var layout = RoomLayout.Create(new[] { Room("Edge", 16777215f, 0f, 1f, 1f) });

            Assert.Throws<ArgumentException>(() => BalancedFrontierCandidateGenerator.Generate(
                layout,
                new[] { new IntegerRoomSize(1, 1) },
                "Next"));
        }

        [Test]
        public void RejectsInvalidSizesAndInputs()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntegerRoomSize(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IntegerRoomSize(1, -1));
            Assert.Throws<ArgumentNullException>(() => BalancedFrontierCandidateGenerator.Generate(
                null,
                new[] { new IntegerRoomSize(1, 1) },
                "Candidate"));
            Assert.Throws<ArgumentException>(() => BalancedFrontierCandidateGenerator.Generate(
                RoomLayout.Create(Array.Empty<RoomPlacement>()),
                Array.Empty<IntegerRoomSize>(),
                "Candidate"));
        }

        private static RoomPlacement Room(string id, float x, float y, float width, float height)
            => new(new RoomId(id), new RoomBounds2D(x, y, width, height));
    }
}
