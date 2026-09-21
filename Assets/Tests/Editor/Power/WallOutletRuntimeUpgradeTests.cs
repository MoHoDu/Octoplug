using System;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.Power.Grid;
using Octoplug.Power.Routing;
using UnityEngine;

namespace Octoplug.Tests.Editor
{
    public class WallOutletRuntimeUpgradeTests : UnityVerificationFixture
    {
        [SetUp]
        public void SetUp()
        {
            SetUpVerificationFixture();
        }

        [TearDown]
        public void TearDown()
        {
            TearDownVerificationFixture();
        }

        [Test]
        public void CountsOneThroughFiveUsePersistentContiguousModules()
        {
            var outlet = CreateWallOutlet();
            var layout = RequireComponent<SocketModuleLayout>(outlet.gameObject);
            var socketIds = GetInstanceIds(outlet.Sockets);
            var wholes = GetWholes(outlet.transform);
            var ends = GetEnds(wholes);
            var first = outlet.transform.Find("Head/Whole01/Edges/First");
            var colliders = new List<BoxCollider2D>();

            Assert.That(outlet.InitialSocketCount, Is.EqualTo(1));
            Assert.That(outlet.ActiveSocketCount, Is.EqualTo(1));
            Assert.That(outlet.Sockets.Count, Is.EqualTo(5));
            Assert.That(layout.Capacity, Is.EqualTo(5));

            for (var count = 1; count <= 5; count++)
            {
                UpgradeSocketCountTo(outlet, count);
                Assert.That(outlet.ActiveSocketCount, Is.EqualTo(count));
                Assert.That(first.gameObject.activeSelf, Is.True);

                layout.GetColliders(count, colliders);
                Assert.That(colliders.Count, Is.EqualTo(count + 2));
                Assert.That(colliders[0], Is.SameAs(first.GetComponent<BoxCollider2D>()));
                Assert.That(
                    colliders[^1],
                    Is.SameAs(ends[count - 1].GetComponent<BoxCollider2D>()));

                for (var i = 0; i < outlet.Sockets.Count; i++)
                {
                    var active = i < count;
                    Assert.That(wholes[i].gameObject.activeSelf, Is.EqualTo(active));
                    Assert.That(outlet.Sockets[i].gameObject.activeSelf, Is.EqualTo(active));
                    Assert.That(outlet.Sockets[i].IsActiveSocket, Is.EqualTo(active));
                    Assert.That(ends[i].gameObject.activeSelf, Is.EqualTo(active && i == count - 1));
                    Assert.That(outlet.Sockets[i].GetInstanceID(), Is.EqualTo(socketIds[i]));
                }
            }

            Assert.That(
                outlet.TryUpgradeActiveSocketCount(out var failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(WallOutletSocketCountFailure.MaximumReached));
        }

        [Test]
        public void RuntimeInitializationRegistersExactlyRequestedActiveSockets()
        {
            var outlet = CreateWallOutlet();

            Assert.That(
                outlet.TryInitializeActiveSocketCount(
                    3,
                    out var failure),
                Is.True);
            Assert.That(
                failure,
                Is.EqualTo(WallOutletSocketCountFailure.None));
            Assert.That(outlet.ActiveSocketCount, Is.EqualTo(3));

            var registered = UnityEngine.Object.FindObjectsByType<
                SocketConnector>(FindObjectsSortMode.None);
            var registeredActive = 0;
            for (var i = 0; i < registered.Length; i++)
            {
                if (registered[i].GetComponentInParent<WallOutlet>() == outlet
                    && registered[i].isActiveAndEnabled
                    && registered[i].IsActiveSocket)
                {
                    registeredActive++;
                }
            }

            Assert.That(registeredActive, Is.EqualTo(3));
            for (var i = 0; i < outlet.Sockets.Count; i++)
            {
                Assert.That(
                    outlet.Sockets[i].isActiveAndEnabled,
                    Is.EqualTo(i < 3));
            }
        }

        [Test]
        public void RuntimeInitializedSocketIsCandidateAndReconnects()
        {
            var outlet = CreateWallOutlet(Vector2.zero);
            Assert.That(
                outlet.TryInitializeActiveSocketCount(
                    2,
                    out var failure),
                Is.True);
            Assert.That(
                failure,
                Is.EqualTo(WallOutletSocketCountFailure.None));
            var socket = outlet.Sockets[1];
            var product = CreateProduct("TV", Vector2.zero);
            PlaceProductNearSocket(product, socket);
            MarkWalkableForCableAndApproach(product, socket);
            var controller = GetRoutingController(product);
            var pointer = (Vector2)socket.ConnectorTransform.position
                + socket.ApproachDirection * Grid.CellSize;

            Assert.That(
                controller.TryResolveSocketCandidate(
                    pointer,
                    out var candidate,
                    out var path),
                Is.True);
            Assert.That(candidate, Is.SameAs(socket));
            Assert.That(path, Is.Not.Null.And.Count.GreaterThanOrEqualTo(2));
            Assert.That(Connect(product.Cable.Plug, candidate), Is.True);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));

            Disconnect(product.Cable.Plug);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.Null);
            Assert.That(socket.ConnectedPlug, Is.Null);
            Assert.That(Connect(product.Cable.Plug, socket), Is.True);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
        }

        [Test]
        public void ConnectedTwoToThreeUpgradePreservesIdentitiesAndHouseUsage()
        {
            var outlet = CreateWallOutlet();
            UpgradeSocketCountTo(outlet, 2);
            var productA = CreateProduct("TV", new Vector2(2f, 0f));
            var productB = CreateProduct("Fan", new Vector2(3f, 0f));
            var socket01 = outlet.Sockets[0];
            var socket02 = outlet.Sockets[1];
            var socket03 = outlet.Sockets[2];
            var outletId = outlet.GetInstanceID();
            var socket01Id = socket01.GetInstanceID();
            var socket02Id = socket02.GetInstanceID();

            Assert.That(Connect(productA.Cable.Plug, socket01), Is.True);
            Assert.That(Connect(productB.Cable.Plug, socket02), Is.True);
            var houseUsage = PowerValidationService.GetHouseUsage();

            Assert.That(
                outlet.TryUpgradeActiveSocketCount(out var failure),
                Is.True);
            Assert.That(failure, Is.EqualTo(WallOutletSocketCountFailure.None));
            Assert.That(outlet.GetInstanceID(), Is.EqualTo(outletId));
            Assert.That(outlet.Sockets[0].GetInstanceID(), Is.EqualTo(socket01Id));
            Assert.That(outlet.Sockets[1].GetInstanceID(), Is.EqualTo(socket02Id));
            Assert.That(productA.Cable.Plug.ConnectedSocket, Is.SameAs(socket01));
            Assert.That(productB.Cable.Plug.ConnectedSocket, Is.SameAs(socket02));
            Assert.That(socket03.IsActiveSocket, Is.True);
            Assert.That(PowerValidationService.GetHouseUsage(), Is.EqualTo(houseUsage));
        }

        [Test]
        public void InactiveSocketIsExcludedFromInteractionConnectionAndPower()
        {
            var outlet = CreateWallOutlet();
            var product = CreateProduct("TV", new Vector2(2f, 0f));
            var inactiveSocket = outlet.Sockets[1];

            Assert.That(inactiveSocket.IsActiveSocket, Is.False);
            Assert.That(
                inactiveSocket.IsPointerInInteractionArea(inactiveSocket.transform.position),
                Is.False);
            Assert.That(PowerValidationService.IsSocketSourceLive(inactiveSocket), Is.False);
            Assert.That(
                PowerValidationService.TryValidate(
                    product,
                    inactiveSocket,
                    null,
                    out var failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(ConnectionFailureReason.SocketInactive));
            Assert.That(Connect(product.Cable.Plug, inactiveSocket), Is.False);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.Null);
            Assert.That(inactiveSocket.ConnectedPlug, Is.Null);
            Assert.That(PowerValidationService.GetHouseUsage(), Is.Zero);

            var activeSocket = outlet.Sockets[0];
            Assert.That(PowerValidationService.IsSocketSourceLive(activeSocket), Is.True);
            Assert.That(Connect(product.Cable.Plug, activeSocket), Is.True);
            Assert.That(
                PowerValidationService.GetHouseUsage(),
                Is.EqualTo(product.PowerConsumptionWatts));
        }

        [Test]
        public void SocketCountChangesDoNotMutateHouseAllowance()
        {
            var budgetObject = new GameObject("HousePowerBudget");
            var budget = budgetObject.AddComponent<HousePowerBudget>();
            budget.SetAllowedPowerWatts(8f);
            var outlet = CreateWallOutlet();
            var initialAllowed = budget.AllowedPowerWatts;
            var maximumAllowed = budget.MaxAllowedPowerWatts;

            UpgradeSocketCountTo(outlet, 5);

            Assert.That(initialAllowed, Is.EqualTo(8f));
            Assert.That(maximumAllowed, Is.EqualTo(22f));
            Assert.That(budget.AllowedPowerWatts, Is.EqualTo(initialAllowed));
            Assert.That(budget.MaxAllowedPowerWatts, Is.EqualTo(maximumAllowed));
            UnityEngine.Object.DestroyImmediate(budgetObject);
        }

        [Test]
        public void TerminalCandidateUsesRoomSideAndExactSocketEndpoint()
        {
            var outlet = CreateWallOutlet(Vector2.zero);
            var socket = outlet.Sockets[0];
            var product = CreateProduct("TV", Vector2.zero);
            PlaceProductNearSocket(product, socket);
            MarkWalkableForCableAndApproach(product, socket);
            var controller = GetRoutingController(product);
            var socketPosition =
                (Vector2)socket.ConnectorTransform.position;
            var roomSidePointer = socketPosition
                + socket.ApproachDirection * Grid.CellSize;
            var oppositeSidePointer = socketPosition
                - socket.ApproachDirection * Grid.CellSize;

            Assert.That(socket.IsTerminalEndpoint, Is.True);
            Assert.That(
                socket.IsPointerOnApproachSide(roomSidePointer),
                Is.True);
            Assert.That(
                socket.IsPointerOnApproachSide(oppositeSidePointer),
                Is.False);
            Assert.That(
                controller.TryResolveSocketCandidate(
                    roomSidePointer,
                    out var resolvedSocket,
                    out var worldPath),
                Is.True);
            Assert.That(resolvedSocket, Is.SameAs(socket));
            Assert.That(
                worldPath,
                Is.Not.Null.And.Count.GreaterThanOrEqualTo(2));
            Assert.That(
                Vector2.Distance(worldPath[^1], socketPosition),
                Is.LessThan(0.0001f));
            Assert.That(
                controller.TryResolveSocketCandidate(
                    oppositeSidePointer,
                    out _,
                    out _),
                Is.False);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.Null);
            Assert.That(socket.ConnectedPlug, Is.Null);
        }

        [Test]
        public void InactiveSocketIsNotAMagneticCandidate()
        {
            var outlet = CreateWallOutlet(Vector2.zero);
            var inactiveSocket = outlet.Sockets[1];
            var product = CreateProduct("TV", Vector2.zero);
            PlaceProductNearSocket(product, inactiveSocket);
            MarkWalkableForCableAndApproach(product, inactiveSocket);
            var controller = GetRoutingController(product);
            var activeSocket = outlet.Sockets[0];
            var inactiveSocketPosition =
                (Vector2)inactiveSocket.ConnectorTransform.position;
            var pointer = inactiveSocketPosition
                + inactiveSocket.ApproachDirection * Grid.CellSize;

            Assert.That(inactiveSocket.IsActiveSocket, Is.False);
            Assert.That(
                controller.TryResolveSocketCandidate(
                    pointer,
                    out var resolvedSocket,
                    out _),
                Is.True);
            Assert.That(resolvedSocket, Is.SameAs(activeSocket));
            Assert.That(resolvedSocket, Is.Not.SameAs(inactiveSocket));
            Assert.That(
                Vector2.Distance(
                    pointer,
                    inactiveSocketPosition),
                Is.LessThan(Vector2.Distance(
                    pointer,
                    activeSocket.ConnectorTransform.position)));
        }

        [Test]
        public void BlockedWallIsNotAPortalAndDoorIsTheOnlyCrossing()
        {
            var grid = new CableRoutingGrid(1f);
            for (var x = -2; x <= 2; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    grid.MarkArea(
                        new Bounds(
                            grid.CellToWorld(new GridCoord(x, y)),
                            Vector3.one * 0.9f),
                        y == 0
                            ? GridCellState.Blocked
                            : GridCellState.Walkable);
                }
            }

            var start = new GridCoord(-2, -1);
            var goal = new GridCoord(2, 1);
            var wallCell = new GridCoord(0, 0);
            Assert.That(
                GridPathfinder.TryFindPath(
                    grid,
                    start,
                    goal,
                    out _),
                Is.False);

            grid.MarkArea(
                new Bounds(
                    grid.CellToWorld(wallCell),
                    Vector3.one * 0.9f),
                GridCellState.Door);

            Assert.That(
                GridPathfinder.TryFindPath(
                    grid,
                    start,
                    goal,
                    out var doorPath),
                Is.True);
            Assert.That(PathContainsCell(doorPath, wallCell), Is.True);
            for (var x = -2; x <= 2; x++)
            {
                var boundary = new GridCoord(x, 0);
                if (boundary != wallCell)
                {
                    Assert.That(
                        PathContainsCell(doorPath, boundary),
                        Is.False);
                }
            }
        }

        private static void PlaceProductNearSocket(
            ApplianceSource product,
            SocketConnector socket)
        {
            var desiredOrigin =
                (Vector2)socket.ConnectorTransform.position
                + socket.ApproachDirection * 1.5f;
            var currentOrigin =
                (Vector2)product.Cable.Origin.position;
            product.transform.position +=
                (Vector3)(desiredOrigin - currentOrigin);
        }

        private void MarkWalkableForCableAndApproach(
            ApplianceSource product,
            SocketConnector socket)
        {
            var originCell = Grid.WorldToCell(
                product.Cable.Origin.position);
            var socketCell = Grid.WorldToCell(
                socket.ConnectorTransform.position);
            var direction = socket.ApproachDirection;
            var approachStep = new GridCoord(
                Mathf.RoundToInt(direction.x),
                Mathf.RoundToInt(direction.y));
            var approachCell = new GridCoord(
                socketCell.X + approachStep.X,
                socketCell.Y + approachStep.Y);
            var minX = Mathf.Min(originCell.X, approachCell.X) - 1;
            var maxX = Mathf.Max(originCell.X, approachCell.X) + 1;
            var minY = Mathf.Min(originCell.Y, approachCell.Y) - 1;
            var maxY = Mathf.Max(originCell.Y, approachCell.Y) + 1;

            MarkWalkableRectangle(minX, maxX, minY, maxY);
            MarkCell(socketCell, GridCellState.Blocked);
        }

        private static int[] GetInstanceIds(IReadOnlyList<SocketConnector> sockets)
        {
            var ids = new int[sockets.Count];
            for (var i = 0; i < sockets.Count; i++)
            {
                ids[i] = sockets[i].GetInstanceID();
            }

            return ids;
        }

        private static Transform[] GetWholes(Transform root)
        {
            var wholes = new Transform[5];
            for (var i = 0; i < wholes.Length; i++)
            {
                wholes[i] = root.Find($"Head/Whole0{i + 1}");
                Assert.That(wholes[i], Is.Not.Null);
            }

            return wholes;
        }

        private static Transform[] GetEnds(IReadOnlyList<Transform> wholes)
        {
            var ends = new Transform[wholes.Count];
            for (var i = 0; i < wholes.Count; i++)
            {
                ends[i] = wholes[i].Find("Edges/End");
                Assert.That(ends[i], Is.Not.Null);
            }

            return ends;
        }
    }
}
