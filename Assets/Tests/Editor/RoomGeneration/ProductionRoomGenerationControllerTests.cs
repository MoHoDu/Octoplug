using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEditor;
using UnityEngine;

namespace Octoplug.Tests.Editor.RoomGeneration
{
    public sealed class ProductionRoomGenerationControllerTests
    {
        private const string RoomPrefabPath = "Assets/03_Prefabs/Rooms/Room.prefab";

        private readonly List<GameObject> createdObjects = new();
        private ProductionRoomGenerationController controller;
        private CableRoutingGridService gridService;
        private Transform roomsRoot;

        [SetUp]
        public void SetUp()
        {
            var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
            Assert.That(roomPrefab, Is.Not.Null);

            var roomsObject = new GameObject("Rooms");
            createdObjects.Add(roomsObject);
            roomsRoot = roomsObject.transform;

            var seedObject = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab, roomsRoot);
            createdObjects.Add(seedObject);
            var seedBinder = seedObject.GetComponent<RoomGenerationRoomBinder>();
            var prefabBinder = roomPrefab.GetComponent<RoomGenerationRoomBinder>();
            Assert.That(seedBinder, Is.Not.Null);
            Assert.That(prefabBinder, Is.Not.Null);

            var gridObject = new GameObject("Routing Grid");
            createdObjects.Add(gridObject);
            gridService = gridObject.AddComponent<CableRoutingGridService>();
            gridService.InitializeForVerification();

            var controllerObject = new GameObject("Room Generator");
            createdObjects.Add(controllerObject);
            controller = controllerObject.AddComponent<ProductionRoomGenerationController>();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("seedRoom").objectReferenceValue = seedBinder;
            serializedController.FindProperty("roomPrefab").objectReferenceValue = prefabBinder;
            serializedController.FindProperty("roomsRoot").objectReferenceValue = roomsRoot;
            serializedController.FindProperty("routingGrid").objectReferenceValue = gridService;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

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
        public void Initialize_CreatesLockedHintExcludedFromGrid()
        {
            controller.Initialize();

            Assert.That(controller.State.HasNextRoomPlan, Is.True);
            var hint = FindBinder(controller.State.NextRoomPlan.Room.Id);
            var roomArea = hint.GetComponent<RoomArea>();
            Assert.That(roomArea.IsGameplayEnabled, Is.False);
            Assert.That(roomArea.FloorArea.enabled, Is.False);

            var center = new Vector2(hint.Bounds.Center.X, hint.Bounds.Center.Y);
            Assert.That(
                gridService.Grid.GetState(gridService.Grid.WorldToCell(center)),
                Is.EqualTo(GridCellState.Unknown));
        }

        [Test]
        public void PromoteCurrentHint_PromotesStoredPlanThenCreatesFollowingHint()
        {
            var eventOrder = new List<string>();
            controller.RoomUnlocked += _ => eventOrder.Add("unlocked");
            controller.RoomContentReady += _ => eventOrder.Add("content-ready");
            controller.NextHintCreated += _ => eventOrder.Add("next-hint");
            controller.Initialize();
            eventOrder.Clear();

            var storedHint = controller.State.NextRoomPlan.Room;
            var promoted = controller.PromoteCurrentHint();

            Assert.That(promoted, Is.True);
            Assert.That(controller.State.UnlockedLayout.Rooms, Has.Count.EqualTo(2));
            Assert.That(controller.State.UnlockedLayout.Doors, Has.Count.EqualTo(1));
            Assert.That(controller.State.UnlockedLayout.Rooms[1], Is.EqualTo(storedHint));
            Assert.That(eventOrder, Is.EqualTo(new[] { "unlocked", "content-ready", "next-hint" }));

            var promotedBinder = FindBinder(storedHint.Id);
            var roomArea = promotedBinder.GetComponent<RoomArea>();
            Assert.That(roomArea.IsGameplayEnabled, Is.True);
            Assert.That(roomArea.FloorArea.enabled, Is.True);

            var center = new Vector2(promotedBinder.Bounds.Center.X, promotedBinder.Bounds.Center.Y);
            Assert.That(
                gridService.Grid.GetState(gridService.Grid.WorldToCell(center)),
                Is.EqualTo(GridCellState.Walkable));
            Assert.That(controller.State.HasNextRoomPlan, Is.True);
            Assert.That(controller.State.NextRoomPlan.Room.Id, Is.Not.EqualTo(storedHint.Id));
        }

        private RoomGenerationRoomBinder FindBinder(RoomId roomId)
        {
            var binders = roomsRoot.GetComponentsInChildren<RoomGenerationRoomBinder>(true);
            for (var i = 0; i < binders.Length; i++)
            {
                if (binders[i].RoomId == roomId)
                {
                    return binders[i];
                }
            }

            Assert.Fail($"No room binder was found for {roomId}.");
            return null;
        }
    }
}
