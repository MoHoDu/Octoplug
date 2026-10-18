using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Audio;
using Octoplug.Power;
using Octoplug.Power.Grid;
using Octoplug.Power.Routing;
using UnityEngine;

namespace Octoplug.Tests.Editor
{
    public class SharedWallOutletRoutingTests : UnityVerificationFixture
    {
        private const int ApproachSearchRadius = 3;
        private const int WallHalfLength = 8;
        private readonly List<ApplianceSource> products = new();
        private System.IDisposable playbackOverride;
        private GridCoord normal;
        private GridCoord tangent;

        [SetUp]
        public void SetUp()
        {
            // 연결 트랜잭션은 그대로 검증하고 PlayMode 전용 오디오 출력만 격리한다.
            playbackOverride = GameplaySfxPlayer.OverridePlaybackForVerification(_ => { });
            try
            {
                SetUpVerificationFixture();
            }
            catch
            {
                TearDown();
                throw;
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                foreach (var product in products)
                {
                    if (product != null)
                    {
                        Disconnect(product.Cable.Plug);
                        product.SetPowered(false);
                    }
                }
            }
            finally
            {
                // 연결 해제 실패가 다음 케이스의 전역 소켓 탐색을 오염시키지 않게 한다.
                products.Clear();
                try
                {
                    TearDownVerificationFixture();
                }
                finally
                {
                    playbackOverride?.Dispose();
                    playbackOverride = null;
                }
            }
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(180f)]
        [TestCase(270f)]
        public void ContinuousSharedWallAllowsBothRoomDropsAndReconnectWithoutOpeningPortal(
            float rotation)
        {
            var socket = CreateSharedWall(rotation);
            var product = CreateRoutedProduct(4);
            var controller = GetRoutingController(product);
            AssertNoCrossing();

            // 실제 프리팹의 앞/뒤에서 후보 탐색과 최종 드롭을 모두 거친다.
            foreach (var side in new[] { 1, -1 })
            {
                PlaceOrigin(product, Cell(side * 4));
                var pointer = PointerOnSide(socket, side);
                Assert.That(
                    controller.TryResolveSocketCandidate(pointer, out var candidate, out var path),
                    Is.True);
                Assert.That(candidate, Is.SameAs(socket));
                AssertTerminalPathOnSide(path, socket, side);
                Assert.That(product.Cable.Plug.IsConnected, Is.False);
                Assert.That(socket.IsConnected, Is.False);
                Assert.That(controller.TryCommitSocketDrop(pointer), Is.True);
                Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
                Assert.That(socket.ConnectedPlug, Is.SameAs(product.Cable.Plug));
                Assert.That(product.IsPowered, Is.True);
                AssertSharedWallUnchanged();
                AssertNoCrossing();
                Disconnect(product.Cable.Plug);
                product.SetPowered(false);
            }
        }

        [Test]
        public void CloserBackApproachCannotHideReachableFrontAndFrontOnlyRemainsRestricted()
        {
            var socket = CreateSharedWall();
            var socketPosition = (Vector2)socket.ConnectorTransform.position;
            Assert.That(
                Vector2.Distance(Grid.CellToWorld(Cell(-1)), socketPosition),
                Is.LessThan(Vector2.Distance(Grid.CellToWorld(Cell(1)), socketPosition)));

            var frontOrigin = Grid.CellToWorld(Cell(4));
            Assert.That(TryBuildPath(frontOrigin, socket, out var frontPath), Is.True);
            AssertTerminalPathOnSide(frontPath, socket, 1);
            Assert.That(
                TryBuildPath(frontOrigin, socket, out var frontOnlyPath, terminalFrontOnly: true),
                Is.True);
            Assert.That(frontOnlyPath, Is.EqualTo(frontPath));

            var backOrigin = Grid.CellToWorld(Cell(-4));
            Assert.That(TryBuildPath(backOrigin, socket, out var backPath), Is.True);
            AssertTerminalPathOnSide(backPath, socket, -1);
            Assert.That(
                TryBuildPath(backOrigin, socket, out _, terminalFrontOnly: true),
                Is.False);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void DistantDoorDoesNotReplaceShortDirectTerminalRoute(int side)
        {
            var socket = CreateSharedWall();
            var door = Cell(0, 7);
            MarkCell(door, GridCellState.Door);
            var origin = Grid.CellToWorld(Cell(side * 4));
            Assert.That(TryBuildPath(origin, socket, out var path), Is.True);
            AssertTerminalPathOnSide(path, socket, side);
            Assert.That(
                GridPathfinder.PathLength(path),
                Is.EqualTo(Vector2.Distance(origin, socket.ConnectorTransform.position))
                    .Within(0.0001f));

            Assert.That(
                TryBuildPath(origin, socket, out var frontOnlyPath, terminalFrontOnly: true),
                Is.True);
            if (side < 0)
            {
                Assert.That(
                    GridPathfinder.PathLength(path),
                    Is.LessThan(GridPathfinder.PathLength(frontOnlyPath)));
            }

            // 일반 방간 경로에는 소켓이 있어도 유일한 문을 반드시 통과해야 한다.
            Assert.That(
                GridPathfinder.TryFindPath(Grid, Cell(4), Cell(-4), out var crossing),
                Is.True);
            Assert.That(PathContainsCell(crossing, door), Is.True);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void ExteriorUnknownSideIsRejectedButExactEndpointAcceptsExistingSide(int floorSide)
        {
            var socket = CreateSharedWall();
            for (var depth = 1; depth <= 6; depth++)
            {
                for (var along = -WallHalfLength; along <= WallHalfLength; along++)
                {
                    MarkCell(Cell(-floorSide * depth, along), GridCellState.Unknown);
                }
            }

            var product = CreateRoutedProduct(floorSide * 4);
            var controller = GetRoutingController(product);
            var outsidePointer = PointerOnSide(socket, -floorSide);
            Assert.That(Grid.GetState(Cell(-floorSide)), Is.EqualTo(GridCellState.Unknown));
            Assert.That(
                CableRouteReachability.IsPointerOnApproachSide(
                    Grid, socket, outsidePointer, ApproachSearchRadius),
                Is.False);
            Assert.That(controller.TryResolveSocketCandidate(outsidePointer, out _, out _), Is.False);
            Assert.That(controller.TryCommitSocketDrop(outsidePointer), Is.False);
            Assert.That(product.Cable.Plug.IsConnected, Is.False);
            Assert.That(socket.IsConnected, Is.False);

            var endpoint = (Vector2)socket.ConnectorTransform.position;
            Assert.That(
                CableRouteReachability.IsPointerOnApproachSide(
                    Grid, socket, endpoint, ApproachSearchRadius),
                Is.True);
            Assert.That(controller.TryResolveSocketCandidate(endpoint, out var candidate, out _), Is.True);
            Assert.That(candidate, Is.SameAs(socket));
            Assert.That(controller.TryCommitSocketDrop(endpoint), Is.True);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
        }

        [Test]
        public void OppositeRoomCannotStealOccupiedSocketAndCanReconnectAfterRelease()
        {
            var socket = CreateSharedWall();
            var first = CreateRoutedProduct(4);
            var second = CreateRoutedProduct(-4);
            var firstController = GetRoutingController(first);
            var secondController = GetRoutingController(second);
            Assert.That(firstController.TryCommitSocketDrop(PointerOnSide(socket, 1)), Is.True);
            var backPointer = PointerOnSide(socket, -1);
            Assert.That(secondController.TryResolveSocketCandidate(backPointer, out _, out _), Is.False);
            Assert.That(secondController.TryCommitSocketDrop(backPointer), Is.False);
            Assert.That(second.Cable.Plug.IsConnected, Is.False);
            Assert.That(socket.ConnectedPlug, Is.SameAs(first.Cable.Plug));

            Disconnect(first.Cable.Plug);
            first.SetPowered(false);
            Assert.That(secondController.TryResolveSocketCandidate(backPointer, out var candidate, out _), Is.True);
            Assert.That(candidate, Is.SameAs(socket));
            Assert.That(secondController.TryCommitSocketDrop(backPointer), Is.True);
            Assert.That(socket.ConnectedPlug, Is.SameAs(second.Cable.Plug));
        }

        [Test]
        public void BackSideDropStillRequiresEnoughRenderedCableLength()
        {
            var socket = CreateSharedWall();
            var product = CreateRoutedProduct(-4);
            var depth = Mathf.CeilToInt(product.Cable.CableLength / Grid.CellSize) + 2;
            for (var distance = 1; distance <= depth; distance++)
            {
                MarkCell(Cell(-distance), GridCellState.Walkable);
            }

            PlaceOrigin(product, Cell(-depth));
            var origin = (Vector2)product.Cable.Origin.position;
            Assert.That(TryBuildPath(origin, socket, out var path), Is.True);
            var length = GridPathfinder.PathLength(path);
            Assert.That(length, Is.GreaterThan(product.Cable.CableLength));
            Assert.That(
                CableRouteReachability.IsWithinLength(Grid, origin, socket, length),
                Is.True);
            Assert.That(
                CableRouteReachability.IsWithinLength(Grid, origin, socket, length - 0.01f),
                Is.False);
            var pointer = PointerOnSide(socket, -1);
            var controller = GetRoutingController(product);
            Assert.That(controller.TryResolveSocketCandidate(pointer, out _, out _), Is.False);
            Assert.That(controller.TryCommitSocketDrop(pointer), Is.False);
            Assert.That(product.Cable.Plug.IsConnected, Is.False);

            UpgradeCableLengthTo(product.Cable, length);
            Assert.That(controller.TryResolveSocketCandidate(pointer, out var candidate, out _), Is.True);
            Assert.That(candidate, Is.SameAs(socket));
            Assert.That(controller.TryCommitSocketDrop(pointer), Is.True);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
        }

        private SocketConnector CreateSharedWall(float rotation = 0f)
        {
            var outlet = CreateWallOutlet();
            outlet.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
            var socket = outlet.Sockets[0];
            var direction = socket.ApproachDirection;
            normal = new GridCoord(Mathf.RoundToInt(direction.x), Mathf.RoundToInt(direction.y));
            tangent = new GridCoord(-normal.Y, normal.X);

            // 후면 셀이 더 가까운 sub-cell 오프셋으로 최근접 한쪽 선택 버그를 재현한다.
            var endpoint = Grid.CellToWorld(new GridCoord(0, 0))
                - direction * (Grid.CellSize * 0.2f);
            outlet.transform.position += (Vector3)(endpoint - (Vector2)socket.ConnectorTransform.position);
            for (var depth = -6; depth <= 6; depth++)
            {
                for (var along = -WallHalfLength; along <= WallHalfLength; along++)
                {
                    MarkCell(Cell(depth, along), depth == 0 ? GridCellState.Blocked : GridCellState.Walkable);
                }
            }

            Assert.That(socket.IsTerminalEndpoint, Is.True);
            return socket;
        }

        private ApplianceSource CreateRoutedProduct(int depth)
        {
            var product = CreateProduct("TV");
            products.Add(product);
            PlaceOrigin(product, Cell(depth));
            return product;
        }

        private void PlaceOrigin(ApplianceSource product, GridCoord cell)
        {
            product.transform.position += (Vector3)(Grid.CellToWorld(cell)
                - (Vector2)product.Cable.Origin.position);
        }

        private GridCoord Cell(int depth, int along = 0)
        {
            return new GridCoord(normal.X * depth + tangent.X * along,
                normal.Y * depth + tangent.Y * along);
        }

        private Vector2 PointerOnSide(SocketConnector socket, int side)
        {
            return (Vector2)socket.ConnectorTransform.position
                + socket.ApproachDirection * (side * Grid.CellSize);
        }

        private bool TryBuildPath(Vector2 origin, SocketConnector socket,
            out List<Vector2> path, bool terminalFrontOnly = false)
        {
            return CableRouteReachability.TryBuildPath(Grid, origin, socket,
                ApproachSearchRadius, 0.15f, out path, terminalFrontOnly);
        }

        private void AssertTerminalPathOnSide(IReadOnlyList<Vector2> path,
            SocketConnector socket, int side)
        {
            Assert.That(path, Is.Not.Null.And.Count.GreaterThanOrEqualTo(2));
            var endpoint = (Vector2)socket.ConnectorTransform.position;
            Assert.That(Vector2.Distance(path[^1], endpoint), Is.LessThan(0.0001f));
            for (var i = 0; i < path.Count - 1; i++)
            {
                Assert.That(Grid.IsWalkable(Grid.WorldToCell(path[i])), Is.True);
                Assert.That(Vector2.Dot(path[i] - endpoint, socket.ApproachDirection) * side,
                    Is.GreaterThan(0f));
            }
        }

        private void AssertNoCrossing()
        {
            Assert.That(GridPathfinder.TryFindPath(Grid, Cell(4), Cell(-4), out _), Is.False);
        }

        private void AssertSharedWallUnchanged()
        {
            for (var depth = -6; depth <= 6; depth++)
            {
                for (var along = -WallHalfLength; along <= WallHalfLength; along++)
                {
                    Assert.That(Grid.GetState(Cell(depth, along)),
                        Is.EqualTo(depth == 0 ? GridCellState.Blocked : GridCellState.Walkable));
                }
            }
        }
    }
}
