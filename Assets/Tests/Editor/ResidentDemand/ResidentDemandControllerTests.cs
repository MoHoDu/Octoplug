using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Octoplug.Audio;
using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.ResidentDemand;
using Octoplug.ResidentDemand.Unity;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Octoplug.Tests.Editor.ResidentDemand
{
    public sealed class ResidentDemandControllerTests
    {
        private readonly List<GameObject> _roots = new();
        private readonly List<GameplaySfxCue> _playedSfx = new();
        private ResidentDemandController _controller;
        private IDisposable _playbackOverride;

        [SetUp]
        public void SetUp()
        {
            _playbackOverride = GameplaySfxPlayer.OverridePlaybackForVerification(
                cue => _playedSfx.Add(cue));
        }

        [TearDown]
        public void TearDown()
        {
            _playbackOverride?.Dispose();
            _playedSfx.Clear();
            if (_controller != null)
            {
                Object.DestroyImmediate(_controller.gameObject);
            }

            for (var index = _roots.Count - 1; index >= 0; index--)
            {
                if (_roots[index] != null)
                {
                    Object.DestroyImmediate(_roots[index]);
                }
            }

            _roots.Clear();
        }

        [Test]
        public void AddResident_NoAvailableUsage_RemainsIdleWithoutFallback()
        {
            InitializeController();

            var resident = _controller.AddResident();

            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.None));
            Assert.That(resident.NeedCount, Is.Zero);
            Assert.That(_controller.CurrentAssignments.Assignments, Is.Empty);
            Assert.That(_controller.CurrentAssignments.Waiting, Is.Empty);
            Assert.That(_playedSfx, Is.Empty);
        }

        [Test]
        public void DemandStartAndRefresh_PlaySpawnOnlyOnce()
        {
            CreateProduct("Cold Product", UsageType.Cooling, 1);
            InitializeController();

            _controller.AddResident();
            _controller.RefreshDemandAvailability();
            _controller.RecomputeAssignmentsForVerification();

            Assert.That(
                _playedSfx,
                Is.EqualTo(new[] { GameplaySfxCue.NeedSpawn }));
        }

        [Test]
        public void AddResident_UnpoweredGameplayProduct_SelectsDemandButWaits()
        {
            CreateProduct("Cold Product", UsageType.Cooling, 1);
            InitializeController();

            var resident = _controller.AddResident();

            Assert.That(_controller.AvailableNeedTypes, Is.EqualTo(ResidentNeedType.Cooling));
            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.Waiting));
            Assert.That(resident.GetNeed(0), Is.EqualTo(ResidentNeedType.Cooling));
            Assert.That(_controller.CurrentAssignments.Assignments, Is.Empty);
            AssertResidentNumbers(_controller.CurrentAssignments.Waiting, 1);
        }

        [Test]
        public void AddResident_LockedRoomProductIsExcludedUntilRefresh()
        {
            var coldProduct = CreateProduct("Cold Product", UsageType.Cooling, 1);
            coldProduct.RoomArea.SetGameplayEnabled(false);
            InitializeController();
            var resident = _controller.AddResident();

            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.None));
            Assert.That(_controller.AvailableNeedTypes, Is.EqualTo(ResidentNeedType.None));

            coldProduct.RoomArea.SetGameplayEnabled(true);
            _controller.RefreshDemandAvailability();

            Assert.That(_controller.AvailableNeedTypes, Is.EqualTo(ResidentNeedType.Cooling));
            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.Waiting));
            Assert.That(resident.GetNeed(0), Is.EqualTo(ResidentNeedType.Cooling));
        }

        [Test]
        public void GameplayProduct_ProductInsideGameplayRoomWithoutRoomParent_IsIncluded()
        {
            var product = CreateProduct("Fun Product", UsageType.Fun, 1);
            product.Appliance.transform.SetParent(null);
            product.Appliance.transform.position = Vector3.zero;
            var controllerObject = new GameObject("Resident Demand Controller");
            _controller = controllerObject.AddComponent<ResidentDemandController>();

            var method = typeof(ResidentDemandController).GetMethod(
                "IsGameplayProduct",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(_controller, new object[] { product.Appliance }), Is.True);
        }

        [Test]
        public void AvailableUsagePool_IncludesEveryMultiUsageFlag()
        {
            CreateProduct("Multi Product", UsageType.Cooling | UsageType.Fun, 1);
            InitializeController();

            Assert.That(
                _controller.AvailableNeedTypes,
                Is.EqualTo(ResidentNeedType.Cooling | ResidentNeedType.Fun));
        }

        [Test]
        public void AvailabilityChanges_ReassignColdResidentImmediately()
        {
            var coldProduct = CreateProduct("Cold Product", UsageType.Cooling, 1);
            Connect(coldProduct);
            coldProduct.Appliance.SetPowered(true);
            InitializeController();

            var resident = AddColdResident();
            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.Using));

            coldProduct.Appliance.SetPowered(false);
            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.Waiting));
            Assert.That(_controller.CurrentAssignments.Assignments, Is.Empty);

            coldProduct.Appliance.SetPowered(true);
            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.Using));

            PlugSocketConnection.Disconnect(coldProduct.Plug);
            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.Waiting));
            Assert.That(_controller.CurrentAssignments.Assignments, Is.Empty);

            Assert.That(
                PlugSocketConnection.Connect(coldProduct.Plug, coldProduct.Socket),
                Is.True);
            Assert.That(resident.Status, Is.EqualTo(ResidentDemandStatus.Using));
            AssertAssignmentInvariants();
        }

        [Test]
        public void CapacityAndUnrelatedChanges_PreserveFifoAndPromoteEarliestWaiter()
        {
            var coldProduct = CreateProduct("Cold Product", UsageType.Cooling, 2);
            var funProduct = CreateProduct("Fun Product", UsageType.Fun, 1);
            Connect(coldProduct);
            Connect(funProduct);
            coldProduct.Appliance.SetPowered(true);
            funProduct.Appliance.SetPowered(true);
            InitializeController();

            var residents = new[]
            {
                AddColdResident(),
                AddColdResident(),
                AddColdResident(),
                AddColdResident(),
            };

            AssertResidentNumbers(_controller.CurrentAssignments.Assignments, 1, 2);
            AssertResidentNumbers(_controller.CurrentAssignments.Waiting, 3, 4);
            AssertAssignmentInvariants();

            funProduct.Appliance.SetPowered(false);
            AssertResidentNumbers(_controller.CurrentAssignments.Assignments, 1, 2);
            AssertResidentNumbers(_controller.CurrentAssignments.Waiting, 3, 4);

            PlugSocketConnection.Disconnect(funProduct.Plug);
            AssertResidentNumbers(_controller.CurrentAssignments.Assignments, 1, 2);
            AssertResidentNumbers(_controller.CurrentAssignments.Waiting, 3, 4);

            var outcome = residents[0].Advance(1f);
            Assert.That(outcome, Is.Not.Null);
            Assert.That(outcome.Resolution, Is.EqualTo(DemandResolution.Success));
            _controller.RecomputeAssignmentsForVerification();

            AssertResidentNumbers(_controller.CurrentAssignments.Assignments, 2, 3);
            AssertResidentNumbers(_controller.CurrentAssignments.Waiting, 4);
            Assert.That(residents[2].Status, Is.EqualTo(ResidentDemandStatus.Using));
            Assert.That(residents[3].Status, Is.EqualTo(ResidentDemandStatus.Waiting));
            AssertAssignmentInvariants();
        }

        [Test]
        public void ProductUsageSnapshot_TracksCapacityAvailabilityAndUnrelatedGraphChanges()
        {
            var coldProduct = CreateProduct("Cold Product", UsageType.Cooling, 2);
            var funProduct = CreateProduct("Fun Product", UsageType.Fun, 1);
            Connect(coldProduct);
            Connect(funProduct);
            coldProduct.Appliance.SetPowered(true);
            funProduct.Appliance.SetPowered(true);
            InitializeController();

            for (var index = 0; index < 4; index++)
            {
                AddColdResident();
            }

            AssertSnapshot(coldProduct.Appliance, new[] { 1, 2 }, new[] { 3, 4 });
            AssertSnapshot(funProduct.Appliance, Array.Empty<int>(), Array.Empty<int>());

            funProduct.Appliance.SetPowered(false);
            PlugSocketConnection.Disconnect(funProduct.Plug);
            AssertSnapshot(coldProduct.Appliance, new[] { 1, 2 }, new[] { 3, 4 });

            coldProduct.Appliance.SetPowered(false);
            AssertSnapshot(coldProduct.Appliance, Array.Empty<int>(), Array.Empty<int>());
            Assert.That(_controller.CurrentAssignments.Waiting.Count, Is.EqualTo(4));

            coldProduct.Appliance.SetPowered(true);
            AssertSnapshot(coldProduct.Appliance, new[] { 1, 2 }, new[] { 3, 4 });

            PlugSocketConnection.Disconnect(coldProduct.Plug);
            AssertSnapshot(coldProduct.Appliance, Array.Empty<int>(), Array.Empty<int>());
            Assert.That(_controller.CurrentAssignments.Waiting.Count, Is.EqualTo(4));

            Assert.That(
                PlugSocketConnection.Connect(coldProduct.Plug, coldProduct.Socket),
                Is.True);
            AssertSnapshot(coldProduct.Appliance, new[] { 1, 2 }, new[] { 3, 4 });
            AssertAssignmentInvariants();
        }

        private void InitializeController()
        {
            var controllerObject = new GameObject("Resident Demand Controller");
            _controller = controllerObject.AddComponent<ResidentDemandController>();
            _controller.InitializeForVerification(1);
        }

        private ResidentDemandState AddColdResident()
        {
            return _controller.AddResidentForVerification(new DemandBalanceRecord(
                "TEST-COLD",
                true,
                1,
                1,
                new[] { ResidentNeedType.Cooling },
                1f,
                10f,
                0,
                0,
                0,
                0f));
        }

        private ProductFixture CreateProduct(
            string name,
            UsageType usageType,
            int capacity)
        {
            var roomObject = new GameObject($"{name} Room");
            _roots.Add(roomObject);
            roomObject.AddComponent<BoxCollider2D>();
            roomObject.AddComponent<RoomArea>();

            var productObject = new GameObject(name);
            productObject.transform.SetParent(roomObject.transform);
            var appliance = productObject.AddComponent<ApplianceSource>();

            var cableObject = new GameObject("Cable");
            cableObject.transform.SetParent(productObject.transform);
            var cable = cableObject.AddComponent<CableInfo>();

            var plugObject = new GameObject("Plug");
            plugObject.transform.SetParent(cableObject.transform);
            var plug = plugObject.AddComponent<PlugConnector>();

            var socketObject = new GameObject($"{name} Socket");
            _roots.Add(socketObject);
            var socket = socketObject.AddComponent<SocketConnector>();

            var serializedCable = new SerializedObject(cable);
            serializedCable.FindProperty("plug").objectReferenceValue = plug;
            serializedCable.ApplyModifiedPropertiesWithoutUndo();

            var serializedAppliance = new SerializedObject(appliance);
            serializedAppliance.FindProperty("cable").objectReferenceValue = cable;
            serializedAppliance.FindProperty("usageTypes").intValue = (int)usageType;
            serializedAppliance.FindProperty("capacity").intValue = capacity;
            serializedAppliance.ApplyModifiedPropertiesWithoutUndo();

            return new ProductFixture(
                appliance,
                plug,
                socket,
                roomObject.GetComponent<RoomArea>());
        }

        private static void Connect(ProductFixture product)
        {
            Assert.That(
                PlugSocketConnection.Connect(product.Plug, product.Socket),
                Is.True);
        }

        private void AssertSnapshot(
            ApplianceSource product,
            IReadOnlyList<int> active,
            IReadOnlyList<int> waiting)
        {
            var snapshot = _controller.ProductUsage.Single(item => item.Product == product);
            Assert.That(
                snapshot.ActiveResidents.Select(number => number.Value),
                Is.EqualTo(active));
            Assert.That(
                snapshot.WaitingResidents.Select(number => number.Value),
                Is.EqualTo(waiting));
        }

        private void AssertAssignmentInvariants()
        {
            var assignments = _controller.CurrentAssignments.Assignments;
            Assert.That(
                assignments.Select(item => item.Request.ResidentNumber.Value).Distinct().Count(),
                Is.EqualTo(assignments.Count),
                "A Resident must not use two Products simultaneously.");

            foreach (var group in assignments.GroupBy(item => item.ProductId))
            {
                var snapshot = _controller.ProductUsage.Single(item => item.ProductId == group.Key);
                Assert.That(
                    group.Count(),
                    Is.LessThanOrEqualTo(snapshot.Product.Capacity),
                    "A Product assignment count must not exceed Capacity.");
            }
        }

        private static void AssertResidentNumbers(
            IEnumerable<ResidentProductAssignment> assignments,
            params int[] expected)
        {
            Assert.That(
                assignments.Select(item => item.Request.ResidentNumber.Value),
                Is.EqualTo(expected));
        }

        private static void AssertResidentNumbers(
            IEnumerable<ResidentAssignmentRequest> requests,
            params int[] expected)
        {
            Assert.That(
                requests.Select(item => item.ResidentNumber.Value),
                Is.EqualTo(expected));
        }

        private sealed class ProductFixture
        {
            public ProductFixture(
                ApplianceSource appliance,
                PlugConnector plug,
                SocketConnector socket,
                RoomArea roomArea)
            {
                Appliance = appliance;
                Plug = plug;
                Socket = socket;
                RoomArea = roomArea;
            }

            public ApplianceSource Appliance { get; }
            public PlugConnector Plug { get; }
            public SocketConnector Socket { get; }
            public RoomArea RoomArea { get; }
        }
    }
}
