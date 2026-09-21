using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Balance;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEditor;
using UnityEngine;

namespace Octoplug.Tests.Editor.RoomGeneration
{
    public sealed class RoomObjectPlacementPlannerTests
    {
        private readonly List<GameObject> createdObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void TryFindPosition_KeepsFullFootprintInsideRoom()
        {
            var room = CreateRoom(0f, 0f, 3f, 3f);
            var grid = CreateGrid(room);
            var footprint = CreateFootprint(new Vector2(2f, 2f));

            var found = RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                footprint,
                socketCount: 0,
                out var position,
                out var failure);

            Assert.That(found, Is.True, failure);
            var bounds = footprint.GetWorldBounds(0, position);
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(room.Bounds.MinX));
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(room.Bounds.MaxX));
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(room.Bounds.MinY));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(room.Bounds.MaxY));
        }

        [Test]
        public void TryFindPosition_RejectsDoorAndReservedCells()
        {
            var room = CreateRoom(0f, 0f, 2f, 1f);
            var grid = CreateGrid(room);
            var footprint = CreateFootprint(Vector2.one);
            var first = new GridCoord(0, 0);
            var second = new GridCoord(1, 0);
            grid.MarkArea(new Bounds(grid.CellToWorld(first), Vector3.one), GridCellState.Door);
            grid.SetObjectOccupied(second, new object(), true);

            var found = RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                footprint,
                socketCount: 0,
                out _,
                out var failure);

            Assert.That(found, Is.False);
            Assert.That(failure, Does.Contain("no full-footprint position"));
        }

        [Test]
        public void TryFindPosition_ContinuesAfterCandidateConstraintRejectsFirstCell()
        {
            var room = CreateRoom(0f, 0f, 2f, 1f);
            var grid = CreateGrid(room);
            var footprint = CreateFootprint(Vector2.one);
            var visited = new List<Vector2>();

            var found = RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                footprint,
                socketCount: 0,
                (candidate, _) =>
                {
                    visited.Add(candidate);
                    return candidate.x > 1f;
                },
                out var position,
                out var failure);

            Assert.That(found, Is.True, failure);
            Assert.That(visited.Count, Is.EqualTo(2));
            Assert.That(position, Is.EqualTo(grid.CellToWorld(new GridCoord(1, 0))));
        }

        [Test]
        public void TryFindPosition_ReportsConstraintExhaustionAfterVisitingEveryCell()
        {
            var room = CreateRoom(0f, 0f, 2f, 1f);
            var grid = CreateGrid(room);
            var footprint = CreateFootprint(Vector2.one);
            var visited = 0;

            var found = RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                footprint,
                socketCount: 0,
                (_, _) =>
                {
                    visited++;
                    return false;
                },
                out _,
                out var failure);

            Assert.That(found, Is.False);
            Assert.That(visited, Is.EqualTo(2));
            Assert.That(failure, Does.Contain("additional placement constraints"));
        }

        [Test]
        public void AcceptedReservationPreventsLaterPlacementOverlap()
        {
            var room = CreateRoom(0f, 0f, 2f, 1f);
            var grid = CreateGrid(room);
            var first = CreateFootprint(Vector2.one);
            var second = CreateFootprint(Vector2.one);

            Assert.That(RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                first,
                0,
                out var firstPosition,
                out var firstFailure), Is.True, firstFailure);
            Assert.That(first.TryReserveAt(grid, firstPosition, 0), Is.True);
            Assert.That(RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                second,
                0,
                out var secondPosition,
                out var secondFailure), Is.True, secondFailure);

            Assert.That(secondPosition, Is.Not.EqualTo(firstPosition));
            Assert.That(second.TryReserveAt(grid, secondPosition, 0), Is.True);
            Assert.That(RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                CreateFootprint(Vector2.one),
                0,
                out _,
                out var finalFailure), Is.False);
            Assert.That(finalFailure, Does.Contain("reservations"));
        }

        [TestCase("Assets/03_Prefabs/Products/Fan.prefab")]
        [TestCase("Assets/03_Prefabs/Products/Heater.prefab")]
        [TestCase("Assets/03_Prefabs/Products/Air_Conditioner.prefab")]
        public void InactiveProductionPrefabKeepsAuthoredBoundsAtCandidatePosition(
            string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null);
            var gameObject = Object.Instantiate(prefab);
            createdObjects.Add(gameObject);
            var footprint = gameObject.GetComponent<PlacementFootprint>();
            Assert.That(footprint, Is.Not.Null);
            gameObject.SetActive(false);

            var sourceBounds = footprint.GetWorldBounds(0, Vector2.zero);
            var bounds = footprint.GetWorldBounds(0, new Vector2(4f, 3f));

            Assert.That(bounds.size.x, Is.GreaterThan(0.9f));
            Assert.That(bounds.size.y, Is.GreaterThan(0.9f));
            Assert.That(bounds.size, Is.EqualTo(sourceBounds.size));
            Assert.That(
                (Vector2)(bounds.center - sourceBounds.center),
                Is.EqualTo(new Vector2(4f, 3f)));
        }

        [Test]
        public void InactiveLargeFootprintsReserveDisjointCells()
        {
            var room = CreateRoom(0f, 0f, 3f, 2f);
            var grid = new CableRoutingGrid(0.25f);
            grid.MarkArea(
                new Bounds(new Vector3(1.5f, 1f), new Vector3(3f, 2f, 0f)),
                GridCellState.Walkable);
            var first = CreateFootprint(new Vector2(0.96f, 0.96f));
            var second = CreateFootprint(new Vector2(0.96f, 0.96f));
            first.gameObject.SetActive(false);
            second.gameObject.SetActive(false);

            Assert.That(RoomObjectPlacementPlanner.TryFindPosition(
                room, grid, first, 0, out var firstPosition, out var firstFailure),
                Is.True,
                firstFailure);
            Assert.That(first.TryReserveAt(grid, firstPosition, 0), Is.True);
            Assert.That(RoomObjectPlacementPlanner.TryFindPosition(
                room, grid, second, 0, out var secondPosition, out var secondFailure),
                Is.True,
                secondFailure);
            Assert.That(second.TryReserveAt(grid, secondPosition, 0), Is.True);

            var firstBounds = first.GetWorldBounds(0, firstPosition);
            var secondBounds = second.GetWorldBounds(0, secondPosition);
            var overlapX = Mathf.Min(firstBounds.max.x, secondBounds.max.x)
                - Mathf.Max(firstBounds.min.x, secondBounds.min.x);
            var overlapY = Mathf.Min(firstBounds.max.y, secondBounds.max.y)
                - Mathf.Max(firstBounds.min.y, secondBounds.min.y);
            Assert.That(overlapX > 0f && overlapY > 0f, Is.False);
        }

        [Test]
        public void RedistributionTargetsPreferFewerProductsThenLargerArea()
        {
            var smallEmpty = CreateRoom(0f, 0f, 2f, 2f, "small-empty");
            var largeEmpty = CreateRoom(3f, 0f, 4f, 3f, "large-empty");
            var occupied = CreateRoom(8f, 0f, 6f, 4f, "occupied");
            var counts = new Dictionary<RoomId, int>
            {
                [smallEmpty.Id] = 0,
                [largeEmpty.Id] = 0,
                [occupied.Id] = 1
            };

            var selected = RoomContentGenerationController.SelectTargetRooms(
                new[] { smallEmpty, occupied, largeEmpty },
                2,
                roomId => counts[roomId]);

            Assert.That(selected, Has.Count.EqualTo(2));
            Assert.That(selected[0].Id, Is.EqualTo(largeEmpty.Id));
            Assert.That(selected[1].Id, Is.EqualTo(smallEmpty.Id));
            Assert.That(selected[0].Id, Is.Not.EqualTo(selected[1].Id));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        public void ProductPoolSelectionUsesHalfOpenCumulativeIntervals(int roll, int expectedIndex)
        {
            var pool = new[]
            {
                new ProductSpawnPoolSheetRow { productType = "Fan", weight = 2 },
                new ProductSpawnPoolSheetRow { productType = "TV", weight = 3 },
                new ProductSpawnPoolSheetRow { productType = "Heater", weight = 1 }
            };

            Assert.That(
                RoomContentGenerationController.SelectProductIndexFromPool(pool, roll),
                Is.EqualTo(expectedIndex));
        }

        [Test]
        public void AcceptedReservationsKeepLargeFootprintsDisjoint()
        {
            var room = CreateRoom(0f, 0f, 6f, 3f);
            var grid = CreateGrid(room);
            var first = CreateFootprint(new Vector2(2f, 2f));
            var second = CreateFootprint(new Vector2(2f, 2f));

            Assert.That(RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                first,
                0,
                out var firstPosition,
                out var firstFailure), Is.True, firstFailure);
            Assert.That(first.TryReserveAt(grid, firstPosition, 0), Is.True);
            Assert.That(RoomObjectPlacementPlanner.TryFindPosition(
                room,
                grid,
                second,
                0,
                out var secondPosition,
                out var secondFailure), Is.True, secondFailure);
            Assert.That(second.TryReserveAt(grid, secondPosition, 0), Is.True);

            var firstBounds = first.GetWorldBounds(0, firstPosition);
            var secondBounds = second.GetWorldBounds(0, secondPosition);
            var overlapX = Mathf.Min(firstBounds.max.x, secondBounds.max.x)
                - Mathf.Max(firstBounds.min.x, secondBounds.min.x);
            var overlapY = Mathf.Min(firstBounds.max.y, secondBounds.max.y)
                - Mathf.Max(firstBounds.min.y, secondBounds.min.y);
            Assert.That(overlapX > 0f && overlapY > 0f, Is.False);
        }

        private static RoomPlacement CreateRoom(
            float x,
            float y,
            float width,
            float height,
            string id = "placement-test")
        {
            return new RoomPlacement(
                new RoomId(id),
                new RoomBounds2D(x, y, width, height));
        }

        private static CableRoutingGrid CreateGrid(RoomPlacement room)
        {
            var grid = new CableRoutingGrid(1f);
            var center = new Vector2(
                room.Bounds.MinX + room.Bounds.Width * 0.5f,
                room.Bounds.MinY + room.Bounds.Height * 0.5f);
            grid.MarkArea(
                new Bounds(center, new Vector3(room.Bounds.Width, room.Bounds.Height, 0f)),
                GridCellState.Walkable);
            return grid;
        }

        private PlacementFootprint CreateFootprint(Vector2 size)
        {
            var gameObject = new GameObject("Footprint");
            createdObjects.Add(gameObject);
            var collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = size;
            return gameObject.AddComponent<PlacementFootprint>();
        }
    }
}
