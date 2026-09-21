using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.Tests.Editor.RoomGeneration
{
    public sealed class WallOutletPlacementPlannerTests
    {
        [Test]
        public void Plan_AssignsAtMostOneOutletToEachActualWall()
        {
            var room = new RoomPlacement(new RoomId("room-test"), new RoomBounds2D(2f, 3f, 8f, 6f));

            var placements = WallOutletPlacementPlanner.Plan(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.25f,
                6);

            Assert.That(placements, Has.Count.EqualTo(4));
            Assert.That(new HashSet<WallSide>
            {
                placements[0].Side,
                placements[1].Side,
                placements[2].Side,
                placements[3].Side
            }, Has.Count.EqualTo(4));

            foreach (var placement in placements)
            {
                switch (placement.Side)
                {
                    case WallSide.Top:
                        Assert.That(placement.Position.y, Is.EqualTo(room.Bounds.MaxY - 0.5f));
                        Assert.That(placement.RotationDegrees, Is.EqualTo(180f));
                        break;
                    case WallSide.Right:
                        Assert.That(placement.Position.x, Is.EqualTo(room.Bounds.MaxX - 0.5f));
                        Assert.That(placement.RotationDegrees, Is.EqualTo(90f));
                        break;
                    case WallSide.Bottom:
                        Assert.That(placement.Position.y, Is.EqualTo(room.Bounds.MinY + 0.5f));
                        Assert.That(placement.RotationDegrees, Is.EqualTo(0f));
                        break;
                    case WallSide.Left:
                        Assert.That(placement.Position.x, Is.EqualTo(room.Bounds.MinX + 0.5f));
                        Assert.That(placement.RotationDegrees, Is.EqualTo(-90f));
                        break;
                }
            }
        }

        [Test]
        public void Plan_UsesAuthoredPivotOffsetForEveryWallOrientation()
        {
            var room = new RoomPlacement(
                new RoomId("room-pivot"),
                new RoomBounds2D(0f, 0f, 8f, 6f));
            var placements = WallOutletPlacementPlanner.Plan(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(new Vector2(0f, 0.25f), new Vector2(2f, 1f)),
                0f,
                4);

            foreach (var placement in placements)
            {
                switch (placement.Side)
                {
                    case WallSide.Top:
                        Assert.That(placement.Position.y, Is.EqualTo(5.75f));
                        break;
                    case WallSide.Right:
                        Assert.That(placement.Position.x, Is.EqualTo(7.75f));
                        break;
                    case WallSide.Bottom:
                        Assert.That(placement.Position.y, Is.EqualTo(0.25f));
                        break;
                    case WallSide.Left:
                        Assert.That(placement.Position.x, Is.EqualTo(0.25f));
                        break;
                }
            }
        }

        [Test]
        public void Plan_ExcludesDoorOpeningAndSafetyClearance()
        {
            var adjacent = new RoomPlacement(new RoomId("room-adjacent"), new RoomBounds2D(0f, 0f, 8f, 6f));
            var room = new RoomPlacement(new RoomId("room-door"), new RoomBounds2D(0f, 6f, 8f, 6f));
            var layout = RoomLayout.Create(new[] { adjacent });
            var result = RoomPlanner.PlanNext(
                layout,
                new[] { new RoomCandidate(room.Id, room.Bounds) },
                new DoorPlanningOptions(2f, 0.5f),
                new WidestSafeIntervalMidpointPolicy());
            Assert.That(result.Success, Is.True);
            var door = result.Plan.DoorPlans[0];

            var placements = WallOutletPlacementPlanner.Plan(
                room,
                result.Plan.DoorPlans,
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.5f,
                4);

            var bottom = default(WallOutletPlacement);
            for (var i = 0; i < placements.Count; i++)
            {
                if (placements[i].Side == WallSide.Bottom)
                {
                    bottom = placements[i];
                    break;
                }
            }

            Assert.That(bottom.Side, Is.EqualTo(WallSide.Bottom));
            var halfOutletWidth = 1f;
            var outletStart = bottom.Position.x - halfOutletWidth - 0.5f;
            var outletEnd = bottom.Position.x + halfOutletWidth + 0.5f;
            Assert.That(outletEnd <= door.Span.Start || outletStart >= door.Span.End, Is.True);
        }

        [Test]
        public void Plan_UsesOnlyAvailableWallSpaceWhenOutletClearanceConsumesWall()
        {
            var room = new RoomPlacement(new RoomId("room-small"), new RoomBounds2D(0f, 0f, 3f, 8f));

            var placements = WallOutletPlacementPlanner.Plan(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(Vector2.zero, new Vector2(4f, 4f)),
0f,
                4);

            Assert.That(placements, Has.Count.EqualTo(2));
            Assert.That(placements[0].Side, Is.EqualTo(WallSide.Right));
            Assert.That(placements[1].Side, Is.EqualTo(WallSide.Left));
        }

        [Test]
        public void PlanClicked_SnapsToNearestWallAndPreservesClickedCoordinate()
        {
            var room = new RoomPlacement(new RoomId("room-click"), new RoomBounds2D(2f, 3f, 8f, 6f));

            var result = WallOutletPlacementPlanner.PlanClicked(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.25f,
                new Vector2(5.5f, 8.6f),
                new HashSet<WallSide>());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(ClickedWallPlacementStatus.Success));
            Assert.That(result.Placement.Side, Is.EqualTo(WallSide.Top));
            Assert.That(result.Placement.Position, Is.EqualTo(new Vector2(5.5f, 8.5f)));
            Assert.That(result.Placement.RotationDegrees, Is.EqualTo(180f));
        }

        [Test]
        public void PlanClicked_RejectsClickBeyondFootprintDerivedSelectionDistance()
        {
            var room = new RoomPlacement(new RoomId("room-far"), new RoomBounds2D(0f, 0f, 8f, 6f));

            var result = WallOutletPlacementPlanner.PlanClicked(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.25f,
                new Vector2(4f, 3f),
                new HashSet<WallSide>());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(ClickedWallPlacementStatus.TooFarFromWall));
        }

        [Test]
        public void PlanClicked_RejectsNearestWallWhenOccupiedWithoutChoosingAnotherWall()
        {
            var room = new RoomPlacement(new RoomId("room-occupied"), new RoomBounds2D(0f, 0f, 8f, 6f));

            var result = WallOutletPlacementPlanner.PlanClicked(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.25f,
                new Vector2(7.8f, 3f),
                new HashSet<WallSide> { WallSide.Right });

            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(ClickedWallPlacementStatus.WallOccupied));
        }

        [Test]
        public void PlanClicked_ClampsToNearbyEndClearance()
        {
            var room = new RoomPlacement(new RoomId("room-end"), new RoomBounds2D(0f, 0f, 8f, 6f));

            var result = WallOutletPlacementPlanner.PlanClicked(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.25f,
                new Vector2(0.8f, 6.1f),
                new HashSet<WallSide>());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Placement.Side, Is.EqualTo(WallSide.Top));
            Assert.That(result.Placement.Position, Is.EqualTo(new Vector2(1.25f, 5.5f)));
        }

        [Test]
        public void PlanClicked_RejectsWhenDoorClearanceHasNoValidCoordinateNearClick()
        {
            var adjacent = new RoomPlacement(new RoomId("room-door-adjacent"), new RoomBounds2D(0f, 0f, 8f, 6f));
            var room = new RoomPlacement(new RoomId("room-door-click"), new RoomBounds2D(0f, 6f, 8f, 6f));
            var layout = RoomLayout.Create(new[] { adjacent });
            var plan = RoomPlanner.PlanNext(
                layout,
                new[] { new RoomCandidate(room.Id, room.Bounds) },
                new DoorPlanningOptions(2f, 0.5f),
                new WidestSafeIntervalMidpointPolicy()).Plan;

            var result = WallOutletPlacementPlanner.PlanClicked(
                room,
                plan.DoorPlans,
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.25f,
                new Vector2(4f, 6.1f),
                new HashSet<WallSide>());

            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(ClickedWallPlacementStatus.NoValidCoordinateNearClick));
        }

        [Test]
        public void PlanClickedWithExistingPlacements_UsesPlacementSidesAsOccupiedWalls()
        {
            var room = new RoomPlacement(new RoomId("room-existing"), new RoomBounds2D(0f, 0f, 8f, 6f));
            var existing = new[]
            {
                new WallOutletPlacement(WallSide.Left, new Vector2(0f, 3f), -90f)
            };

            var result = WallOutletPlacementPlanner.PlanClickedWithExistingPlacements(
                room,
                new List<DoorPlan>(),
                new WallOutletGeometry(Vector2.zero, new Vector2(2f, 1f)),
0.25f,
                new Vector2(0.1f, 3f),
                existing);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(ClickedWallPlacementStatus.WallOccupied));
        }
    }
}
