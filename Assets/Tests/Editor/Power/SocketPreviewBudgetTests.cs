using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Octoplug.Audio;
using Octoplug.Power;
using Octoplug.Power.Cable;
using Octoplug.Power.Connection;
using Octoplug.Power.Grid;
using Octoplug.Power.Routing;
using UnityEngine;
using UnityEngine.TestTools;

namespace Octoplug.Tests.Editor
{
    public sealed class SocketPreviewBudgetTests : UnityVerificationFixture
    {
        private readonly List<CableInfo> cables = new();
        private readonly List<GameplaySfxCue> played = new();
        private IDisposable playbackOverride;
        private GameObject houseObject;
        private HousePowerBudget house;

        [SetUp]
        public void SetUp()
        {
            // 실제 연결 트랜잭션은 유지하고 EditMode에서 실행할 수 없는 오디오 출력만 격리한다.
            playbackOverride = GameplaySfxPlayer.OverridePlaybackForVerification(played.Add);
            try
            {
                SetUpVerificationFixture();
                MarkWalkableRectangle(-32, 32, -16, 16);
                houseObject = new GameObject("Preview budget test house");
                house = houseObject.AddComponent<HousePowerBudget>();
                house.SetAllowedPowerWatts(22f);
                played.Clear();
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
                foreach (var cable in cables)
                {
                    if (cable != null) Disconnect(cable.Plug);
                }
            }
            finally
            {
                cables.Clear();
                try
                {
                    TearDownVerificationFixture();
                    if (houseObject != null) UnityEngine.Object.DestroyImmediate(houseObject);
                    houseObject = null;
                    house = null;
                }
                finally
                {
                    playbackOverride?.Dispose();
                    playbackOverride = null;
                    played.Clear();
                }
            }
        }

        [Test]
        public void HouseOverBudget_AllowsPureCandidateAndVisualPreview_ButRejectsDrop()
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = ProductNear(socket);
            var routing = Routing(product.Cable);
            house.SetAllowedPowerWatts(product.PowerConsumptionWatts - 1f);
            var plugPosition = product.Cable.Plug.transform.position;
            var path = CopyRenderedPath(product.Cable);
            var houseUsage = PowerValidationService.GetHouseUsage();
            var graphEvents = 0;
            var rejectedEvents = 0;
            Action graphHandler = () => graphEvents++;
            Action<ConnectionFailureReason> rejectedHandler = _ => rejectedEvents++;
            PlugSocketConnection.GraphChanged += graphHandler;
            routing.ConnectionRejected += rejectedHandler;
            try
            {
                for (var i = 0; i < 3; i++) AssertCandidate(routing, socket);
                Assert.That(product.Cable.Plug.transform.position, Is.EqualTo(plugPosition));
                Assert.That(CopyRenderedPath(product.Cable), Is.EqualTo(path));

                // Reflection은 실제 입력 검증이 아니라 전체 production drag 처리의 Logic Test이다.
                Invoke(routing, "OnDragStarted");
                Invoke(routing, "OnDragged", Position(socket));
                Assert.That((Vector2)product.Cable.Plug.transform.position, Is.EqualTo(Position(socket)));
                AssertRenderedEndpoint(product.Cable, Position(socket));
                Assert.That(product.Cable.Plug.IsConnected, Is.False);
                Assert.That(socket.IsConnected, Is.False);
                Assert.That(product.IsPowered, Is.False);
                Assert.That(PowerValidationService.GetHouseUsage(), Is.EqualTo(houseUsage));
                Assert.That(graphEvents, Is.Zero);
                Assert.That(rejectedEvents, Is.Zero);
                Assert.That(routing.LastFailureReason, Is.EqualTo(ConnectionFailureReason.None));
                Assert.That(played, Is.Empty);

                ExpectRejection(ConnectionFailureReason.HousePowerExceeded);
                Invoke(routing, "OnDragEnded");
                Assert.That(rejectedEvents, Is.EqualTo(1));
                Assert.That(routing.LastFailureReason, Is.EqualTo(ConnectionFailureReason.HousePowerExceeded));
                Assert.That(graphEvents, Is.Zero);
                Assert.That(product.Cable.Plug.IsConnected, Is.False);
                Assert.That(socket.IsConnected, Is.False);
                Assert.That(product.IsPowered, Is.False);
                Assert.That(played, Is.Empty);
            }
            finally
            {
                PlugSocketConnection.GraphChanged -= graphHandler;
                routing.ConnectionRejected -= rejectedHandler;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProductOverDirectOrAncestorStripBudget_PreviewsThenRejects(bool nested)
        {
            var upstream = CreatePowerStrip(new Vector2(4f, 0f));
            var targetStrip = upstream;
            if (nested)
            {
                targetStrip = CreatePowerStrip();
                UpgradeAllowedPowerTo(targetStrip, 10f);
                PrepareCable(targetStrip.Cable, Position(upstream.Sockets[0]) + Vector2.left);
                Commit(targetStrip.Cable, upstream.Sockets[0]);
            }

            var socket = targetStrip.Sockets[0];
            var product = ProductNear(socket);
            Assert.That(product.PowerConsumptionWatts, Is.GreaterThan(upstream.AllowedPowerWatts));
            if (nested) Assert.That(product.PowerConsumptionWatts, Is.LessThanOrEqualTo(targetStrip.AllowedPowerWatts));
            var routing = Routing(product.Cable);
            played.Clear();
            var beforeUsage = PowerValidationService.GetStripUsage(upstream);
            AssertCandidate(routing, socket);
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(socket));
            Assert.That((Vector2)product.Cable.Plug.transform.position, Is.EqualTo(Position(socket)));
            Assert.That(PowerValidationService.GetStripUsage(upstream), Is.EqualTo(beforeUsage));
            Assert.That(product.Cable.Plug.IsConnected, Is.False);
            Assert.That(product.IsPowered, Is.False);
            Assert.That(played, Is.Empty);
            AssertCommitRejected(product.Cable, socket, ConnectionFailureReason.PowerStripPowerExceeded);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LoadedUnpoweredStrip_PreviewsBeforeHouseOrUpstreamBudgetRejects(bool upstreamLimited)
        {
            var branch = CreatePowerStrip();
            UpgradeAllowedPowerTo(branch, 5f);
            var product = ProductNear(branch.Sockets[0]);
            Commit(product.Cable, branch.Sockets[0]);
            Assert.That(product.IsPowered, Is.False);
            Assert.That(branch.IsPowered, Is.False);
            Assert.That(PowerValidationService.GetStripUsage(branch), Is.EqualTo(product.PowerConsumptionWatts));
            Assert.That(PowerValidationService.GetHouseUsage(), Is.Zero);

            SocketConnector target;
            if (upstreamLimited)
            {
                var upstream = CreatePowerStrip(new Vector2(4f, 0f));
                target = upstream.Sockets[0];
                Assert.That(product.PowerConsumptionWatts, Is.GreaterThan(upstream.AllowedPowerWatts));
            }
            else
            {
                target = WallSocketAt(Cell(16, 0));
                house.SetAllowedPowerWatts(product.PowerConsumptionWatts - 1f);
            }

            PrepareCable(branch.Cable, Position(target) + Vector2.left);
            var routing = Routing(branch.Cable);
            played.Clear();
            AssertCandidate(routing, target);
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(target));
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(branch.Sockets[0]));
            Assert.That(branch.Cable.Plug.IsConnected, Is.False);
            Assert.That(branch.IsPowered, Is.False);
            Assert.That(product.IsPowered, Is.False);
            Assert.That(PowerValidationService.GetHouseUsage(), Is.Zero);
            Assert.That(PowerValidationService.GetStripUsage(branch), Is.EqualTo(product.PowerConsumptionWatts));
            Assert.That(played, Is.Empty);
            AssertCommitRejected(branch.Cable, target, upstreamLimited
                ? ConnectionFailureReason.PowerStripPowerExceeded
                : ConnectionFailureReason.HousePowerExceeded);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(branch.Sockets[0]));
            Assert.That(branch.IsPowered, Is.False);
            Assert.That(product.IsPowered, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DropUsesLatestHouseAllowance_NotPreviewAllowance(bool becomesAffordable)
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = ProductNear(socket);
            var routing = Routing(product.Cable);
            var required = product.PowerConsumptionWatts;
            house.SetAllowedPowerWatts(becomesAffordable ? required - 1f : required);
            AssertCandidate(routing, socket);
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(socket));
            house.SetAllowedPowerWatts(becomesAffordable ? required : required - 1f);
            played.Clear();
            if (!becomesAffordable) ExpectRejection(ConnectionFailureReason.HousePowerExceeded);
            Invoke(routing, "OnDragEnded");
            Assert.That(product.Cable.Plug.IsConnected, Is.EqualTo(becomesAffordable));
            Assert.That(product.IsPowered, Is.EqualTo(becomesAffordable));
            Assert.That(socket.ConnectedPlug, becomesAffordable
                ? Is.SameAs(product.Cable.Plug) : Is.Null);
            Assert.That(routing.LastFailureReason, Is.EqualTo(becomesAffordable
                ? ConnectionFailureReason.None : ConnectionFailureReason.HousePowerExceeded));
            Assert.That(played, Is.EqualTo(becomesAffordable
                ? new[] { GameplaySfxCue.PlugConnect } : Array.Empty<GameplaySfxCue>()));
        }

        [Test]
        public void DropUsesLatestStripAllowance_AfterPreviewUpgrade()
        {
            var strip = CreatePowerStrip();
            var product = ProductNear(strip.Sockets[0]);
            var routing = Routing(product.Cable);
            Assert.That(product.PowerConsumptionWatts, Is.GreaterThan(strip.AllowedPowerWatts));
            AssertCandidate(routing, strip.Sockets[0]);
            UpgradeAllowedPowerTo(strip, product.PowerConsumptionWatts);
            Commit(product.Cable, strip.Sockets[0]);
            Assert.That(product.IsPowered, Is.False, "미급전 멀티탭에는 물리적으로 연결되어도 Powered가 되지 않는다.");
            Assert.That(PowerValidationService.GetStripUsage(strip), Is.EqualTo(product.PowerConsumptionWatts));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DropRechecksOccupiedOrInactiveSocket_AfterVisualPreview(bool inactive)
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = ProductNear(socket);
            var routing = Routing(product.Cable);
            AssertCandidate(routing, socket);
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(socket));
            ApplianceSource occupant = null;
            if (inactive)
            {
                socket.gameObject.SetActive(false);
            }
            else
            {
                occupant = ProductNear(socket, "Fan");
                Commit(occupant.Cable, socket);
            }

            played.Clear();
            Assert.That(routing.TryResolveSocketCandidate(Position(socket), out _, out _), Is.False);
            Assert.That(routing.TryCommitSocketDrop(Position(socket)), Is.False);
            Invoke(routing, "OnDragEnded");
            Assert.That(product.Cable.Plug.IsConnected, Is.False);
            Assert.That(product.IsPowered, Is.False);
            Assert.That(socket.ConnectedPlug, inactive ? Is.Null : Is.SameAs(occupant.Cable.Plug));
            if (occupant != null) Assert.That(occupant.IsPowered, Is.True);
            Assert.That(played, Is.Empty);
        }

        [Test]
        public void NearestOverloadedStrip_IsPreviewedAndRejected_NotReplacedByFartherAffordableWall()
        {
            var strip = CreatePowerStrip();
            var nearest = strip.Sockets[0];
            var farther = WallSocketAt(Position(nearest) + Vector2.right * 0.2f);
            var product = ProductNear(nearest);
            var routing = Routing(product.Cable);
            Assert.That(PowerValidationService.TryValidate(product, farther, house, out _), Is.True);
            Assert.That(PowerValidationService.TryValidate(product, nearest, house, out var reason), Is.False);
            Assert.That(reason, Is.EqualTo(ConnectionFailureReason.PowerStripPowerExceeded));
            AssertCandidate(routing, nearest);
            AssertCommitRejected(product.Cable, nearest, reason);
            Assert.That(farther.IsConnected, Is.False);
        }

        [Test]
        public void FullDragEnd_PreservesAppliedPowerRejectionPositionAndRenderedPath()
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = ProductNear(socket);
            var routing = Routing(product.Cable);
            house.SetAllowedPowerWatts(product.PowerConsumptionWatts - 1f);
            MarkCell(Grid.WorldToCell(Position(socket)), GridCellState.Blocked);
            AssertCandidate(routing, socket);
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(socket));
            Assert.That((Vector2)product.Cable.Plug.transform.position, Is.EqualTo(Position(socket)));
            ExpectRejection(ConnectionFailureReason.HousePowerExceeded);
            Invoke(routing, "OnDragEnded");
            var rejectedPosition = (Vector2)product.Cable.Plug.transform.position;
            var rejectDistance = ReadField<float>(routing, "powerRejectFallbackDistance");
            Assert.That(Vector2.Distance(rejectedPosition, Position(socket)), Is.GreaterThanOrEqualTo(rejectDistance - 0.001f));
            Assert.That(Grid.IsWalkable(Grid.WorldToCell(rejectedPosition)), Is.True);
            Assert.That(ReadField<Vector2>(routing, "lastValidPlugPosition"), Is.EqualTo(rejectedPosition));
            AssertRenderedEndpoint(product.Cable, rejectedPosition);
            Assert.That(GridPathfinder.PathLength(CopyRenderedPath(product.Cable)), Is.LessThanOrEqualTo(product.Cable.CableLength + 0.001f));
            Assert.That(product.Cable.Plug.IsConnected, Is.False);
            Assert.That(product.IsPowered, Is.False);
        }

        [Test]
        public void FullDragEnd_WhenPowerPushbackHasNoCell_StillAppliesOrdinaryDropFallback()
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = CreateProduct("TV");
            PrepareCable(product.Cable, Cell(-2, 0));
            var routing = Routing(product.Cable);
            house.SetAllowedPowerWatts(product.PowerConsumptionWatts - 1f);
            // 남은 두 바닥 셀은 소켓에서 0.6보다 가까워 전력 거절 전용 위치가 없다.
            Grid.Clear();
            MarkCell(new GridCoord(-2, 0), GridCellState.Walkable);
            MarkCell(new GridCoord(-1, 0), GridCellState.Walkable);
            MarkCell(new GridCoord(0, 0), GridCellState.Blocked);
            AssertCandidate(routing, socket);
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(socket));
            ExpectRejection(ConnectionFailureReason.HousePowerExceeded);
            Invoke(routing, "OnDragEnded");
            var fallback = (Vector2)product.Cable.Plug.transform.position;
            Assert.That(fallback, Is.EqualTo(Cell(-1, 0)));
            Assert.That(ReadField<Vector2>(routing, "lastValidPlugPosition"), Is.EqualTo(fallback));
            AssertRenderedEndpoint(product.Cable, fallback);
            Assert.That(product.Cable.Plug.IsConnected, Is.False);
            Assert.That(routing.LastFailureReason, Is.EqualTo(ConnectionFailureReason.HousePowerExceeded));
        }

        [Test]
        public void ConnectedNoDrift_ReleaseRetainsPairingPowerAndSoundSilence_DespiteLowerBudget()
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = ProductNear(socket);
            var routing = Routing(product.Cable);
            Commit(product.Cable, socket);
            house.SetAllowedPowerWatts(product.PowerConsumptionWatts - 1f);
            played.Clear();
            var rejected = 0;
            Action<ConnectionFailureReason> handler = _ => rejected++;
            routing.ConnectionRejected += handler;
            try
            {
                Invoke(routing, "OnDragStarted");
                Invoke(routing, "OnDragged", Position(socket) + Vector2.up * 0.05f);
                Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
                Assert.That(product.IsPowered, Is.True);
                Invoke(routing, "OnDragEnded");
                Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
                Assert.That(socket.ConnectedPlug, Is.SameAs(product.Cable.Plug));
                Assert.That(product.IsPowered, Is.True);
                Assert.That((Vector2)product.Cable.Plug.transform.position, Is.EqualTo(Position(socket)));
                Assert.That(rejected, Is.Zero);
                Assert.That(played, Is.Empty);
            }
            finally
            {
                routing.ConnectionRejected -= handler;
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConnectedDriftAndReturn_MutatesOnlyOnRelease_AndRechecksBudget(bool affordable)
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = ProductNear(socket);
            var routing = Routing(product.Cable);
            Commit(product.Cable, socket);
            house.SetAllowedPowerWatts(product.PowerConsumptionWatts - (affordable ? 0f : 1f));
            played.Clear();
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(socket) + Vector2.up * 0.8f);
            Invoke(routing, "OnDragged", Position(socket));
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
            Assert.That(product.IsPowered, Is.True);
            Assert.That(PowerValidationService.GetHouseUsage(), Is.EqualTo(product.PowerConsumptionWatts));
            Assert.That(played, Is.Empty);
            if (!affordable) ExpectRejection(ConnectionFailureReason.HousePowerExceeded);
            Invoke(routing, "OnDragEnded");
            Assert.That(product.Cable.Plug.IsConnected, Is.EqualTo(affordable));
            Assert.That(product.IsPowered, Is.EqualTo(affordable));
            Assert.That(socket.ConnectedPlug, affordable ? Is.SameAs(product.Cable.Plug) : Is.Null);
            Assert.That(PowerValidationService.GetHouseUsage(), Is.EqualTo(affordable ? product.PowerConsumptionWatts : 0f));
            Assert.That(played, Is.EqualTo(affordable
                ? new[] { GameplaySfxCue.PlugDisconnect, GameplaySfxCue.PlugConnect }
                : new[] { GameplaySfxCue.PlugDisconnect }));
            Assert.That(routing.LastFailureReason, Is.EqualTo(affordable
                ? ConnectionFailureReason.None : ConnectionFailureReason.HousePowerExceeded));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DriftedConnectedSocket_BecomingInactiveCannotReconnectThroughRecentSocketFallback(bool disableComponent)
        {
            var socket = WallSocketAt(Cell(0, 0));
            var product = ProductNear(socket);
            var routing = Routing(product.Cable);
            Commit(product.Cable, socket);
            played.Clear();
            Invoke(routing, "OnDragStarted");
            Invoke(routing, "OnDragged", Position(socket) + Vector2.up * 0.8f);
            Invoke(routing, "OnDragged", Position(socket));
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.SameAs(socket));
            if (disableComponent) socket.enabled = false;
            else socket.gameObject.SetActive(false);

            // 일반 후보 탐색뿐 아니라 최근 분리 소켓의 재연결 경로도 활성 상태를 다시 확인해야 한다.
            Invoke(routing, "OnDragEnded");
            Assert.That(product.Cable.Plug.IsConnected, Is.False);
            Assert.That(socket.IsConnected, Is.False);
            Assert.That(product.IsPowered, Is.False);
            Assert.That(played, Is.EqualTo(new[] { GameplaySfxCue.PlugDisconnect }));
        }

        private Vector2 Cell(int x, int y) => Grid.CellToWorld(new GridCoord(x, y));

        private static Vector2 Position(SocketConnector socket) => socket.ConnectorTransform.position;

        private SocketConnector WallSocketAt(Vector2 position)
        {
            var outlet = CreateWallOutlet();
            var socket = outlet.Sockets[0];
            // 좁은 fallback 검증의 바닥 두 셀이 authored 전면에 놓이도록 벽 방향을 정한다.
            outlet.transform.rotation = Quaternion.Euler(0f, 0f,
                Vector2.SignedAngle(socket.ApproachDirection, Vector2.left));
            outlet.transform.position += (Vector3)(position - Position(socket));
            Physics2D.SyncTransforms();
            return socket;
        }

        private ApplianceSource ProductNear(SocketConnector socket, string prefab = "TV")
        {
            var product = CreateProduct(prefab);
            PrepareCable(product.Cable, Position(socket) + Vector2.left);
            return product;
        }

        private void PrepareCable(CableInfo cable, Vector2 origin)
        {
            // 테스트 인스턴스의 연속 좌표만 구성하며 원본 프리팹과 케이블 길이는 바꾸지 않는다.
            cable.Origin.position = origin;
            cable.Plug.transform.position = origin + Vector2.up * Grid.CellSize;
            var routing = Routing(cable);
            SetField(routing, "houseBudget", house);
            Physics2D.SyncTransforms();
            Invoke(routing, "Start");
            cables.Add(cable);
        }

        private static CableRoutingController Routing(CableInfo cable) => cable.GetComponent<CableRoutingController>();

        private static void AssertCandidate(CableRoutingController routing, SocketConnector socket)
        {
            Assert.That(routing.TryResolveSocketCandidate(Position(socket), out var candidate, out var path), Is.True);
            Assert.That(candidate, Is.SameAs(socket));
            Assert.That(path, Is.Not.Null.And.Not.Empty);
            Assert.That(path[^1], Is.EqualTo(Position(socket)));
        }

        private static void Commit(CableInfo cable, SocketConnector socket)
        {
            Assert.That(Routing(cable).TryCommitSocketDrop(Position(socket)), Is.True);
            Assert.That(cable.Plug.ConnectedSocket, Is.SameAs(socket));
            Assert.That(socket.ConnectedPlug, Is.SameAs(cable.Plug));
        }

        private void AssertCommitRejected(CableInfo cable, SocketConnector socket, ConnectionFailureReason reason)
        {
            var routing = Routing(cable);
            var rejected = new List<ConnectionFailureReason>();
            Action<ConnectionFailureReason> handler = rejected.Add;
            routing.ConnectionRejected += handler;
            played.Clear();
            try
            {
                ExpectRejection(reason);
                Assert.That(routing.TryCommitSocketDrop(Position(socket)), Is.False);
                Assert.That(routing.LastFailureReason, Is.EqualTo(reason));
                Assert.That(rejected, Is.EqualTo(new[] { reason }));
                Assert.That(cable.Plug.IsConnected, Is.False);
                Assert.That(socket.IsConnected, Is.False);
                Assert.That(played, Is.Empty);
            }
            finally
            {
                routing.ConnectionRejected -= handler;
            }
        }

        private static void ExpectRejection(ConnectionFailureReason reason) =>
            LogAssert.Expect(LogType.Warning, new Regex("connection to .* rejected — " + reason + "\\."));

        private static List<Vector2> CopyRenderedPath(CableInfo cable) =>
            new(ReadField<CablePathRenderer>(Routing(cable), "pathRenderer").LastRenderedWorldPath);

        private static void AssertRenderedEndpoint(CableInfo cable, Vector2 expected)
        {
            var path = CopyRenderedPath(cable);
            Assert.That(path.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(Vector2.Distance(path[^1], expected), Is.LessThan(0.0001f));
        }

        private static void Invoke(CableRoutingController routing, string methodName, params object[] arguments)
        {
            var method = typeof(CableRoutingController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(routing, arguments);
        }

        private static T ReadField<T>(CableRoutingController routing, string fieldName)
        {
            var field = typeof(CableRoutingController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(routing);
        }

        private static void SetField(CableRoutingController routing, string fieldName, object value)
        {
            var field = typeof(CableRoutingController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(routing, value);
        }
    }
}
