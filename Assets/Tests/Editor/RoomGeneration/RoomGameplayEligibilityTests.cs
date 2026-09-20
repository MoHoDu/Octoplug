using NUnit.Framework;
using Octoplug.Power;
using Octoplug.Power.Grid;
using UnityEngine;

namespace Octoplug.Tests.Editor.RoomGeneration
{
    public sealed class RoomGameplayEligibilityTests
    {
        private GameObject context;
        private GameObject roomObject;
        private CableRoutingGridService gridService;
        private RoomArea roomArea;

        [SetUp]
        public void SetUp()
        {
            context = new GameObject("Room Gameplay Eligibility Context");
            gridService = context.AddComponent<CableRoutingGridService>();
            gridService.InitializeForVerification();

            roomObject = new GameObject("Room Area");
            var floor = roomObject.AddComponent<BoxCollider2D>();
            floor.size = new Vector2(4f, 4f);
            roomArea = roomObject.AddComponent<RoomArea>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(roomObject);
            Object.DestroyImmediate(context);
        }

        [Test]
        public void DisabledHintRoom_IsExcludedFromAuthoritativeGrid()
        {
            roomArea.SetGameplayEnabled(false);

            gridService.RebuildFromScene();

            Assert.That(roomArea.IsGameplayEnabled, Is.False);
            Assert.That(roomArea.FloorArea.enabled, Is.False);
            Assert.That(
                gridService.Grid.GetState(gridService.Grid.WorldToCell(Vector2.zero)),
                Is.EqualTo(GridCellState.Unknown));
        }

        [Test]
        public void PromotedRoom_BecomesWalkableAfterAuthoritativeGridRebuild()
        {
            roomArea.SetGameplayEnabled(false);
            gridService.RebuildFromScene();

            roomArea.SetGameplayEnabled(true);
            gridService.RebuildFromScene();

            Assert.That(roomArea.IsGameplayEnabled, Is.True);
            Assert.That(roomArea.FloorArea.enabled, Is.True);
            Assert.That(
                gridService.Grid.GetState(gridService.Grid.WorldToCell(Vector2.zero)),
                Is.EqualTo(GridCellState.Walkable));
        }
    }
}
