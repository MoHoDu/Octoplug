using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
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

        private static RoomPlacement CreateRoom(float x, float y, float width, float height)
        {
            return new RoomPlacement(
                new RoomId("placement-test"),
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
