using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Octoplug.Audio;
using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.Power.Grid;
using Octoplug.Power.Routing;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using Octoplug.Telemetry;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Octoplug.Tests.Editor.RoomGeneration
{
    public sealed class ProductionRoomGenerationControllerTests
    {
        private const string RoomPrefabPath = "Assets/03_Prefabs/Rooms/Room.prefab";

        private readonly List<GameObject> createdObjects = new();
        private ProductionRoomGenerationController controller;
        private CableRoutingGridService gridService;
        private Transform roomsRoot;
        private string telemetryRoot;
        private System.IDisposable playbackOverride;
        private Random.State previousRandomState;

        [SetUp]
        public void SetUp()
        {
            previousRandomState = Random.state;
            Random.InitState(21);
            // EditMode에서는 연결 로직을 유지하고 PlayMode 전용 오디오 출력만 격리한다.
            playbackOverride = GameplaySfxPlayer.OverridePlaybackForVerification(_ => { });
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
            try
            {
                SessionTelemetryService.SetRecorderForVerification(null);
                for (var i = createdObjects.Count - 1; i >= 0; i--)
                {
                    if (createdObjects[i] != null)
                    {
                        Object.DestroyImmediate(createdObjects[i]);
                    }
                }

                createdObjects.Clear();
                if (!string.IsNullOrEmpty(telemetryRoot)
                    && System.IO.Directory.Exists(telemetryRoot))
                {
                    System.IO.Directory.Delete(telemetryRoot, true);
                }
            }
            finally
            {
                playbackOverride?.Dispose();
                playbackOverride = null;
                Random.state = previousRandomState;
            }
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
        public void RoomContentReady_PublishesAfterRuntimeOutletIsConnectionReady()
        {
            ConfigureProductionContent();

            controller.Initialize();
            // 이 검사는 신규 자기방 소켓의 준비 시점을 검증하므로 공유 대상은 제외한다.
            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets().ToArray())
            {
                outlet.gameObject.SetActive(false);
            }
            var promotedRoom = controller.State.NextRoomPlan.Room;
            WallOutlet readyOutlet = null;
            SocketConnector readySocket = null;
            controller.RoomContentReady += room =>
            {
                if (room.Id != promotedRoom.Id)
                {
                    return;
                }

                var roomOutlets = new List<WallOutlet>();
                foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
                {
                    if (RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner)
                        && owner == room.Id)
                    {
                        roomOutlets.Add(outlet);
                    }
                }

                Assert.That(roomOutlets, Has.Count.EqualTo(1));
                readyOutlet = roomOutlets[0];
                foreach (var socket in readyOutlet.ActiveSockets)
                {
                    readySocket = socket;
                    break;
                }
            };

            Assert.That(controller.PromoteCurrentHint(), Is.True);
            Assert.That(readyOutlet, Is.Not.Null);
            Assert.That(readySocket, Is.Not.Null);
            var roomProducts = new List<ApplianceSource>();
            foreach (var product in RuntimeWorldRegistry.GetProducts())
            {
                if (RuntimeWorldRegistry.TryGetRoomOwner(product, out var owner)
                    && owner == promotedRoom.Id)
                {
                    roomProducts.Add(product);
                }
            }

            Assert.That(roomProducts.Count, Is.EqualTo(1));
            Assert.That(readySocket.isActiveAndEnabled, Is.True);
            Assert.That(readySocket.IsActiveSocket, Is.True);
            Assert.That(readySocket.IsTerminalEndpoint, Is.True);

            var roomCenter = new Vector2(
                promotedRoom.Bounds.Center.X,
                promotedRoom.Bounds.Center.Y);
            var socketPosition =
                (Vector2)readySocket.ConnectorTransform.position;
            Assert.That(
                Vector2.Dot(
                    roomCenter - socketPosition,
                    readySocket.ApproachDirection),
                Is.GreaterThan(0f));

            var socketCell = gridService.Grid.WorldToCell(socketPosition);
            Assert.That(
                gridService.Grid.GetState(socketCell),
                Is.Not.EqualTo(GridCellState.Unknown));
            Assert.That(
                CableRouteReachability.TryBuildPath(
                    gridService.Grid,
                    roomProducts[0].Cable.Origin.position,
                    readySocket,
                    approachSearchRadius: 3,
                    minRenderSegmentLength: 0.15f,
                    out var path),
                Is.True);
            Assert.That(path, Is.Not.Null.And.Count.GreaterThanOrEqualTo(2));
            Assert.That(
                Vector2.Distance(path[^1], socketPosition),
                Is.LessThan(0.0001f));

            var routing = roomProducts[0].Cable.GetComponent<
                Octoplug.Power.Cable.CableRoutingController>();
            Assert.That(routing, Is.Not.Null);
            var pointer = socketPosition
                + readySocket.ApproachDirection * gridService.Grid.CellSize;
            Assert.That(
                routing.TryResolveSocketCandidate(
                    pointer,
                    out var resolvedSocket,
                    out var resolvedPath),
                Is.True);
            Assert.That(resolvedSocket, Is.Not.Null);
            Assert.That(
                resolvedSocket.GetComponentInParent<WallOutlet>(),
                Is.SameAs(readyOutlet));
            Assert.That(resolvedSocket.IsActiveSocket, Is.True);
            Assert.That(resolvedSocket.IsTerminalEndpoint, Is.True);
            Assert.That(
                Vector2.Distance(
                    resolvedPath[^1],
                    resolvedSocket.ConnectorTransform.position),
                Is.LessThan(0.0001f));

            Assert.That(routing.TryCommitSocketDrop(pointer), Is.True);
            Assert.That(
                roomProducts[0].Cable.Plug.ConnectedSocket,
                Is.SameAs(resolvedSocket));
            Assert.That(resolvedSocket.ConnectedPlug,
                Is.SameAs(roomProducts[0].Cable.Plug));
            Assert.That(roomProducts[0].IsPowered, Is.True);

            PlugSocketConnection.Disconnect(roomProducts[0].Cable.Plug);
            roomProducts[0].SetPowered(false);
            Assert.That(roomProducts[0].Cable.Plug.IsConnected, Is.False);
            Assert.That(resolvedSocket.IsConnected, Is.False);
            Assert.That(routing.TryCommitSocketDrop(pointer), Is.True);
            Assert.That(
                roomProducts[0].Cable.Plug.ConnectedSocket,
                Is.SameAs(resolvedSocket));
            Assert.That(roomProducts[0].IsPowered, Is.True);
        }

        [Test]
        public void PromoteCurrentHint_RefreshesGridBeforeRequiredProductReachability()
        {
            ConfigureProductionContent();
            controller.Initialize();
            var promotedRoom = controller.State.NextRoomPlan.Room;
            var initialRoomCount = controller.State.UnlockedLayout.Rooms.Count;

            Assert.That(controller.PromoteCurrentHint(), Is.True);

            Assert.That(
                controller.State.UnlockedLayout.Rooms.Count,
                Is.EqualTo(initialRoomCount + 1));
            Assert.That(CountWallOutlets(promotedRoom.Id), Is.EqualTo(1));
            Assert.That(CountProducts(promotedRoom.Id), Is.EqualTo(1));

            ApplianceSource product = null;
            foreach (var candidate in RuntimeWorldRegistry.GetProducts())
            {
                if (RuntimeWorldRegistry.TryGetRoomOwner(candidate, out var owner)
                    && owner == promotedRoom.Id)
                {
                    product = candidate;
                    break;
                }
            }

            SocketConnector socket = null;
            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
            {
                if (!RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner)
                    || owner != promotedRoom.Id)
                {
                    continue;
                }

                foreach (var candidate in outlet.ActiveSockets)
                {
                    socket = candidate;
                    break;
                }
            }

            Assert.That(product, Is.Not.Null);
            Assert.That(socket, Is.Not.Null);
        }

        [Test]
        public void ConsecutivePromotions_GenerateRequiredContentThroughRoomFour()
        {
            ConfigureProductionContent();
            controller.Initialize();

            Assert.That(controller.PromoteCurrentHint(), Is.True, "Room 3 promotion");
            var roomFour = controller.State.NextRoomPlan.Room;

            Assert.That(controller.PromoteCurrentHint(), Is.True, "Room 4 promotion");
            Assert.That(controller.State.UnlockedLayout.Rooms, Has.Count.EqualTo(4));
            Assert.That(CountWallOutlets(roomFour.Id), Is.EqualTo(1));
            Assert.That(CountProducts(roomFour.Id), Is.EqualTo(1));

            ApplianceSource roomFourProduct = null;
            foreach (var product in RuntimeWorldRegistry.GetProducts())
            {
                if (RuntimeWorldRegistry.TryGetRoomOwner(product, out var owner)
                    && owner == roomFour.Id)
                {
                    roomFourProduct = product;
                    break;
                }
            }

            SocketConnector roomFourSocket = null;
            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
            {
                if (!RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner)
                    || owner != roomFour.Id)
                {
                    continue;
                }

                foreach (var socket in outlet.ActiveSockets)
                {
                    roomFourSocket = socket;
                    break;
                }
            }

            Assert.That(roomFourProduct, Is.Not.Null);
            Assert.That(roomFourSocket, Is.Not.Null);
            // Room3+는 공유벽 우선 또는 자기방 전면 fallback 중 하나면 충분하다.
            var hasSharedRoute = GetSharedSockets(roomFour).Any(socket =>
                HasReachableRoute(roomFourProduct, socket, roomFour, terminalFrontOnly: false));
            Assert.That(hasSharedRoute || GetOwnSockets(roomFour.Id).Any(socket =>
                HasReachableRoute(roomFourProduct, socket, roomFour, terminalFrontOnly: true)), Is.True);
            AssertValidPlacement(roomFourProduct, roomFour);
        }

        [Test]
        public void RoomThreeGeneratedTv_ReachesExistingSharedSocketWithoutMovingOrLengthUpgrade()
        {
            ConfigureProductionContent();
            controller.Initialize();
            RestrictNextBaseProductToTv();
            var room = controller.State.NextRoomPlan.Room;
            Assert.That(room.Bounds.MinY,
                Is.EqualTo(controller.State.UnlockedLayout.Rooms[0].Bounds.MaxY),
                "회귀 조건은 초기 방 위쪽에 생성되는 세 번째 방이다.");
            var existingSockets = GetSharedSockets(room);
            Assert.That(existingSockets, Is.Not.Empty, "실제 초기 방에 공유벽 소켓이 있어야 한다.");
            var existingPositions = RuntimeWorldRegistry.GetProducts()
                .ToDictionary(product => product, product => product.transform.position);

            Assert.That(controller.PromoteCurrentHint(), Is.True);

            var tv = GetOnlyProduct(room.Id);
            Assert.That(tv.Cable.CableLength, Is.EqualTo(3f));
            Assert.That(tv.name, Does.StartWith("TV"));
            AssertValidPlacement(tv, room);
            foreach (var entry in existingPositions)
            {
                Assert.That(entry.Key.transform.position, Is.EqualTo(entry.Value));
            }

            var socket = existingSockets.FirstOrDefault(candidate =>
                HasReachableRoute(tv, candidate, room, terminalFrontOnly: false));
            Assert.That(socket, Is.Not.Null,
                "생성된 TV 자체가 길이 3으로 기존 공유벽에 닿아야 한다; 테스트에서 위치를 보정하지 않는다.");
            var generatedPosition = tv.transform.position;
            var generatedOrigin = tv.Cable.Origin.position;
            var pointer = (Vector2)socket.ConnectorTransform.position
                - socket.ApproachDirection * gridService.Grid.CellSize;
            var routing = tv.Cable.GetComponent<Octoplug.Power.Cable.CableRoutingController>();
            Assert.That(routing.TryResolveSocketCandidate(pointer, out var candidate, out var path), Is.True);
            Assert.That(existingSockets, Does.Contain(candidate));
            Assert.That(GridPathfinder.PathLength(path), Is.LessThanOrEqualTo(3.001f));
            AssertPointInsideRoom(path[^2], room);
            Assert.That(routing.TryCommitSocketDrop(pointer), Is.True);
            Assert.That(tv.Cable.Plug.ConnectedSocket, Is.SameAs(candidate));
            Assert.That(candidate.ConnectedPlug, Is.SameAs(tv.Cable.Plug));
            Assert.That(tv.IsPowered, Is.True);

            PlugSocketConnection.Disconnect(tv.Cable.Plug);
            tv.SetPowered(false);
            Assert.That(routing.TryCommitSocketDrop(pointer), Is.True);
            Assert.That(tv.Cable.Plug.ConnectedSocket, Is.SameAs(candidate));
            Assert.That(tv.IsPowered, Is.True);
            Assert.That(tv.transform.position, Is.EqualTo(generatedPosition));
            Assert.That(tv.Cable.Origin.position, Is.EqualTo(generatedOrigin));
            Assert.That(tv.Cable.CableLength, Is.EqualTo(3f));
        }

        [TestCase("missing")]
        [TestCase("occupied")]
        [TestCase("inactive")]
        [TestCase("opposite-wall")]
        [TestCase("no-placement")]
        public void RoomThreeWithoutUsableSharedSocket_FallsBackToOwnFront(string unavailableReason)
        {
            ConfigureProductionContent();
            controller.Initialize();
            RestrictNextBaseProductToTv();
            var room = controller.State.NextRoomPlan.Room;
            var sharedSockets = GetSharedSockets(room);
            Assert.That(sharedSockets, Is.Not.Empty);
            if (unavailableReason == "no-placement")
            {
                // 공유 소켓은 남겨두되 길이 3 안쪽의 제품 배치 공간만 예약으로 막는다.
                var grid = gridService.Grid;
                var min = grid.WorldToCell(new Vector2(room.Bounds.MinX, room.Bounds.MinY));
                var max = grid.WorldToCell(new Vector2(room.Bounds.MaxX, room.Bounds.MinY + 3.25f));
                var reservationOwner = new object();
                for (var x = min.X; x <= max.X; x++)
                {
                    for (var y = min.Y; y <= max.Y; y++)
                    {
                        grid.SetObjectOccupied(new GridCoord(x, y), reservationOwner, true);
                    }
                }
            }
            foreach (var outlet in sharedSockets.Select(socket => socket.GetComponentInParent<WallOutlet>()).Distinct())
            {
                if (unavailableReason == "no-placement") continue;
                if (unavailableReason == "missing")
                {
                    Object.DestroyImmediate(outlet.gameObject);
                }
                else if (unavailableReason == "inactive")
                {
                    outlet.gameObject.SetActive(false);
                }
                else if (unavailableReason == "opposite-wall")
                {
                    Assert.That(RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner), Is.True);
                    var ownerRoom = controller.State.UnlockedLayout.Rooms.First(value => value.Id == owner);
                    var direction = outlet.Sockets[0].ApproachDirection;
                    var width = ownerRoom.Bounds.MaxX - ownerRoom.Bounds.MinX;
                    var height = ownerRoom.Bounds.MaxY - ownerRoom.Bounds.MinY;
                    outlet.transform.position += (Vector3)(direction
                        * (Mathf.Abs(direction.x) > 0.5f ? width : height));
                }
                else
                {
                    foreach (var socket in outlet.ActiveSockets)
                    {
                        // 점유 입력만 구성한다. 성공 증거는 아래의 실제 생성/라우팅 경로다.
                        var blocker = new GameObject("Occupied shared socket");
                        createdObjects.Add(blocker);
                        socket.AssignPlug(blocker.AddComponent<PlugConnector>());
                    }
                }
            }

            Assert.That(GetSharedSockets(room).Count > 0,
                Is.EqualTo(unavailableReason == "no-placement"));
            Assert.That(controller.PromoteCurrentHint(), Is.True);
            var tv = GetOnlyProduct(room.Id);
            Assert.That(tv.Cable.CableLength, Is.EqualTo(3f));
            Assert.That(GetOwnSockets(room.Id).Any(socket =>
                HasReachableRoute(tv, socket, room, terminalFrontOnly: true)), Is.True);
            AssertValidPlacement(tv, room);
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
            var existingDoor = controller.State.UnlockedLayout.Doors[0];
            var existingDoorView = FindRuntimeDoor(existingDoor);
            Assert.That(existingDoorView, Is.Not.Null);

            var promoted = controller.PromoteCurrentHint();

            Assert.That(promoted, Is.True);
            Assert.That(controller.State.UnlockedLayout.Rooms, Has.Count.EqualTo(3));
            Assert.That(controller.State.UnlockedLayout.Doors, Has.Count.EqualTo(2));
            Assert.That(controller.State.UnlockedLayout.Rooms[2], Is.EqualTo(storedHint));
            Assert.That(eventOrder, Is.EqualTo(new[] { "unlocked", "content-ready", "next-hint" }));
            Assert.That(
                FindRuntimeDoor(existingDoor),
                Is.SameAs(existingDoorView),
                "A successful promotion must not destroy and recreate an existing Door view.");

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

        [Test]
        public void FailedPromotion_DoesNotRecordRolledBackRoomOrDoorTelemetry()
        {
            controller.Initialize();
            var failedPlan = controller.State.NextRoomPlan;
            var content = controller.gameObject.AddComponent<RoomContentGenerationController>();
            var serializedContent = new SerializedObject(content);
            serializedContent.FindProperty("roomGeneration").objectReferenceValue = controller;
            serializedContent.ApplyModifiedPropertiesWithoutUndo();
            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("roomContentGeneration").objectReferenceValue = content;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            var recorder = StartTelemetryRecorder();
            var initialRoomCount = recorder.Document.Rooms.Count;
            var initialDoorCount = recorder.Document.Doors.Count;
            LogAssert.Expect(
                LogType.Warning,
                new System.Text.RegularExpressions.Regex("\\[RoomContentRequiredInfrastructure\\]"));
            LogAssert.Expect(
                LogType.Warning,
                $"Room content generation failed for {failedPlan.Room.Id}; the locked Room hint was restored.");

            Assert.That(controller.PromoteCurrentHint(), Is.False);

            Assert.That(controller.State.NextRoomPlan.Room.Id, Is.EqualTo(failedPlan.Room.Id));
            Assert.That(recorder.Document.Rooms, Has.Count.EqualTo(initialRoomCount));
            Assert.That(recorder.Document.Doors, Has.Count.EqualTo(initialDoorCount));
            Assert.That(
                recorder.Document.Events.FindAll(value =>
                    value.EventType == "RoomUnlocked"
                    && value.EntityID == failedPlan.Room.Id.Value),
                Is.Empty);
            Assert.That(
                recorder.Document.Events.FindAll(value =>
                    value.EventType == "DoorCreated"
                    && value.Payload.Contains(failedPlan.Room.Id.Value)),
                Is.Empty);
        }

        [Test]
        public void SuccessfulPromotion_RecordsCommittedRoomAndDoorExactlyOnce()
        {
            controller.Initialize();
            var promotedPlan = controller.State.NextRoomPlan;
            var recorder = StartTelemetryRecorder();
            controller.RoomUnlocked += room =>
            {
                SessionTelemetryService.RecordRoom(room);
                for (var i = 0; i < promotedPlan.DoorPlans.Count; i++)
                {
                    SessionTelemetryService.RecordDoor(promotedPlan.DoorPlans[i]);
                }
            };

            Assert.That(controller.PromoteCurrentHint(), Is.True);

            Assert.That(
                recorder.Document.Rooms.FindAll(value =>
                    value.RoomID == promotedPlan.Room.Id.Value),
                Has.Count.EqualTo(1));
            Assert.That(
                recorder.Document.Events.FindAll(value =>
                    value.EventType == "RoomUnlocked"
                    && value.EntityID == promotedPlan.Room.Id.Value),
                Has.Count.EqualTo(1));
            Assert.That(recorder.Document.Doors, Has.Count.EqualTo(promotedPlan.DoorPlans.Count));
            Assert.That(
                recorder.Document.Events.FindAll(value => value.EventType == "DoorCreated"),
                Has.Count.EqualTo(promotedPlan.DoorPlans.Count));
        }

        [Test]
        public void Initialize_CreatesTwoStarterRoomsWithExactRequiredContent()
        {
            ConfigureProductionContent();

            controller.Initialize();

            Assert.That(controller.State.UnlockedLayout.Rooms, Has.Count.EqualTo(2));
            foreach (var room in controller.State.UnlockedLayout.Rooms)
            {
                Assert.That(CountProducts(room.Id), Is.EqualTo(1));
                Assert.That(CountWallOutlets(room.Id), Is.EqualTo(1));
                var product = GetOnlyProduct(room.Id);
                Assert.That(GetOwnSockets(room.Id).Any(socket =>
                    HasReachableRoute(product, socket, room, terminalFrontOnly: true)), Is.True);
                AssertValidPlacement(product, room);
            }
        }

        private void RestrictNextBaseProductToTv()
        {
            // 초기 구성은 실제 TV/Fan 그대로 생성한 뒤 다음 pool 선택만 TV로 제한한다.
            // 원본 prefab, 밸런스, 생성된 제품의 위치/길이는 변경하지 않는다.
            var content = new SerializedObject(controller.GetComponent<RoomContentGenerationController>());
            foreach (var field in new[] { "fanPrefab", "heaterPrefab", "inductionPrefab", "airConditionerPrefab" })
            {
                content.FindProperty(field).objectReferenceValue = null;
            }
            content.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ApplianceSource GetOnlyProduct(RoomId roomId)
        {
            var products = RuntimeWorldRegistry.GetProducts().Where(product =>
                RuntimeWorldRegistry.TryGetRoomOwner(product, out var owner) && owner == roomId).ToArray();
            Assert.That(products, Has.Length.EqualTo(1));
            return products[0];
        }

        private static List<SocketConnector> GetOwnSockets(RoomId roomId)
        {
            return RuntimeWorldRegistry.GetWallOutlets().Where(outlet =>
                RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner) && owner == roomId)
                .SelectMany(outlet => outlet.ActiveSockets)
                .Where(socket => socket.isActiveAndEnabled && !socket.IsConnected).ToList();
        }

        private List<SocketConnector> GetSharedSockets(RoomPlacement room)
        {
            var sockets = new List<SocketConnector>();
            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
            {
                if (!RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner) || owner == room.Id)
                {
                    continue;
                }

                foreach (var adjacent in controller.State.UnlockedLayout.Rooms)
                {
                    if (adjacent.Id != owner || !RoomGeometry.TryGetSharedWall(room, adjacent, out var shared))
                    {
                        continue;
                    }

                    foreach (var socket in outlet.ActiveSockets)
                    {
                        var position = (Vector2)socket.ConnectorTransform.position;
                        var horizontal = shared.Span.Orientation == WallOrientation.Horizontal;
                        var along = horizontal ? position.x : position.y;
                        var offset = (horizontal ? position.y : position.x) - shared.Span.FixedCoordinate;
                        var center = new Vector2(room.Bounds.Center.X, room.Bounds.Center.Y);
                        if (socket.isActiveAndEnabled && !socket.IsConnected
                            && along >= shared.Span.Start && along <= shared.Span.End
                            && Mathf.Abs(offset) <= gridService.Grid.CellSize * 0.5f
                            && Vector2.Dot(center - position, socket.ApproachDirection) < 0f)
                        {
                            sockets.Add(socket);
                        }
                    }
                }
            }
            return sockets;
        }

        private bool HasReachableRoute(ApplianceSource product, SocketConnector socket,
            RoomPlacement room, bool terminalFrontOnly)
        {
            if (!CableRouteReachability.TryBuildPath(gridService.Grid, product.Cable.Origin.position,
                    socket, 3, 0.15f, out var path, terminalFrontOnly)
                || path.Count < 2 || GridPathfinder.PathLength(path) > product.Cable.CableLength + 0.001f)
            {
                return false;
            }

            var approach = path[^2];
            return approach.x > room.Bounds.MinX && approach.x < room.Bounds.MaxX
                && approach.y > room.Bounds.MinY && approach.y < room.Bounds.MaxY;
        }

        private static void AssertPointInsideRoom(Vector2 point, RoomPlacement room)
        {
            Assert.That(point.x, Is.GreaterThan(room.Bounds.MinX).And.LessThan(room.Bounds.MaxX));
            Assert.That(point.y, Is.GreaterThan(room.Bounds.MinY).And.LessThan(room.Bounds.MaxY));
        }

        private void AssertValidPlacement(ApplianceSource product, RoomPlacement room)
        {
            Physics2D.SyncTransforms();
            var footprint = product.GetComponent<PlacementFootprint>();
            Assert.That(footprint, Is.Not.Null);
            var bounds = footprint.WorldBounds;
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(room.Bounds.MinX));
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(room.Bounds.MaxX));
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(room.Bounds.MinY));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(room.Bounds.MaxY));
            var cells = new List<GridCoord>();
            footprint.GetCoveredCells(gridService.Grid, product.transform.position, cells);
            Assert.That(cells, Is.Not.Empty);
            foreach (var cell in cells)
            {
                Assert.That(gridService.Grid.IsWalkable(cell), Is.True);
                Assert.That(gridService.Grid.IsObjectOccupied(cell), Is.True);
                Assert.That(gridService.Grid.IsObjectOccupiedByOther(cell, footprint), Is.False);
            }
            foreach (var other in RuntimeWorldRegistry.GetProducts())
            {
                if (other == product) continue;
                var otherBounds = other.GetComponent<PlacementFootprint>().WorldBounds;
                var overlapX = Mathf.Min(bounds.max.x, otherBounds.max.x) - Mathf.Max(bounds.min.x, otherBounds.min.x);
                var overlapY = Mathf.Min(bounds.max.y, otherBounds.max.y) - Mathf.Max(bounds.min.y, otherBounds.min.y);
                Assert.That(overlapX > 0.0001f && overlapY > 0.0001f, Is.False);
            }
        }

        private void ConfigureProductionContent()
        {
            var wallOutletPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03_Prefabs/Wall_Outlets/Wall_Outlet.prefab");
            var tvPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03_Prefabs/Products/TV.prefab");
            var fanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03_Prefabs/Products/Fan.prefab");
            var heaterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03_Prefabs/Products/Heater.prefab");
            var inductionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03_Prefabs/Products/Induction.prefab");
            var airConditionerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03_Prefabs/Products/Air_Conditioner.prefab");
            Assert.That(wallOutletPrefab, Is.Not.Null);
            Assert.That(tvPrefab, Is.Not.Null);
            Assert.That(fanPrefab, Is.Not.Null);
            Assert.That(heaterPrefab, Is.Not.Null);
            Assert.That(inductionPrefab, Is.Not.Null);
            Assert.That(airConditionerPrefab, Is.Not.Null);

            createdObjects.Add(new GameObject("Wall_Outlets"));
            createdObjects.Add(new GameObject("Products"));
            var content = controller.gameObject.AddComponent<
                RoomContentGenerationController>();
            var serializedContent = new SerializedObject(content);
            serializedContent.FindProperty("roomGeneration")
                .objectReferenceValue = controller;
            serializedContent.FindProperty("tvPrefab")
                .objectReferenceValue = tvPrefab.GetComponent<ApplianceSource>();
            serializedContent.FindProperty("fanPrefab")
                .objectReferenceValue = fanPrefab.GetComponent<ApplianceSource>();
            serializedContent.FindProperty("heaterPrefab")
                .objectReferenceValue = heaterPrefab.GetComponent<ApplianceSource>();
            serializedContent.FindProperty("inductionPrefab")
                .objectReferenceValue = inductionPrefab.GetComponent<ApplianceSource>();
            serializedContent.FindProperty("airConditionerPrefab")
                .objectReferenceValue = airConditionerPrefab.GetComponent<ApplianceSource>();
            serializedContent.FindProperty("wallOutletPrefab")
                .objectReferenceValue = wallOutletPrefab.GetComponent<WallOutlet>();
            serializedContent.ApplyModifiedPropertiesWithoutUndo();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("roomContentGeneration")
                .objectReferenceValue = content;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private SessionTelemetryRecorder StartTelemetryRecorder()
        {
            telemetryRoot = System.IO.Path.Combine(
                Application.temporaryCachePath,
                "octoplug-room-promotion-" + System.Guid.NewGuid().ToString("N"));
            var recorder = new SessionTelemetryRecorder(telemetryRoot, () => 0f);
            recorder.Start();
            SessionTelemetryService.SetRecorderForVerification(recorder);
            return recorder;
        }

        private static int CountProducts(RoomId roomId)
        {
            var count = 0;
            foreach (var product in RuntimeWorldRegistry.GetProducts())
            {
                if (RuntimeWorldRegistry.TryGetRoomOwner(product, out var owner)
                    && owner == roomId)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountWallOutlets(RoomId roomId)
        {
            var count = 0;
            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
            {
                if (RuntimeWorldRegistry.TryGetRoomOwner(outlet, out var owner)
                    && owner == roomId)
                {
                    count++;
                }
            }

            return count;
        }

        private Transform FindRuntimeDoor(DoorPlan plan)
        {
            var mapped = DoorViewTransformMapper.Map(plan);
            var owner = FindBinder(mapped.OwnerWall.RoomId);
            var doors = owner.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < doors.Length; i++)
            {
                if (doors[i].name.EndsWith("(Runtime)"))
                {
                    return doors[i];
                }
            }

            return null;
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
