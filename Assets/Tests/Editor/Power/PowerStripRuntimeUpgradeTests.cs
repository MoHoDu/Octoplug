using System;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.Power.Grid;
using UnityEngine;

namespace Octoplug.Tests.Editor
{
    public class PowerStripRuntimeUpgradeTests : UnityVerificationFixture
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
        public void RuntimeUpgradesRemainIndependent()
        {
            var strip = CreatePowerStrip();
            Assert.That(strip.InitialSocketCount, Is.EqualTo(1));
            Assert.That(strip.InitialAllowedPowerWatts, Is.EqualTo(3f));
            Assert.That(strip.Cable.CableLength, Is.EqualTo(3f));

            UpgradeSocketCountTo(strip, 2);
            UpgradeAllowedPowerTo(strip, 5f);
            AssertState(strip, 2, 5f, 3f);

            var socketEvents = 0;
            var powerEvents = 0;
            var cableEvents = 0;
            Action<PowerStrip, int, int> socketHandler = (changed, _, _) =>
            {
                if (changed == strip)
                {
                    socketEvents++;
                }
            };
            Action<PowerStrip> powerHandler = changed =>
            {
                if (changed == strip)
                {
                    powerEvents++;
                }
            };
            Action<CableInfo, float, float> cableHandler = (changed, _, _) =>
            {
                if (changed == strip.Cable)
                {
                    cableEvents++;
                }
            };

            PowerStrip.ActiveSocketCountChanged += socketHandler;
            PowerStrip.AllowanceChanged += powerHandler;
            strip.Cable.CableLengthChanged += cableHandler;
            try
            {
                Assert.That(
                    strip.TryUpgradeActiveSocketCount(out var socketFailure),
                    Is.True);
                Assert.That(socketFailure, Is.EqualTo(PowerStripSocketCountFailure.None));
                AssertState(strip, 3, 5f, 3f);
                Assert.That(socketEvents, Is.EqualTo(1));
                Assert.That(powerEvents, Is.Zero);
                Assert.That(cableEvents, Is.Zero);

                Assert.That(
                    strip.TryUpgradeAllowedPowerWatts(out var powerFailure),
                    Is.True);
                Assert.That(powerFailure, Is.EqualTo(PowerStripUpgradeFailure.None));
                AssertState(strip, 3, 6f, 3f);
                Assert.That(socketEvents, Is.EqualTo(1));
                Assert.That(powerEvents, Is.EqualTo(1));
                Assert.That(cableEvents, Is.Zero);

                Assert.That(
                    strip.Cable.TryUpgradeCableLength(out var cableFailure),
                    Is.True);
                Assert.That(cableFailure, Is.EqualTo(CableLengthUpgradeFailure.None));
                AssertState(strip, 3, 6f, 4f);
                Assert.That(socketEvents, Is.EqualTo(1));
                Assert.That(powerEvents, Is.EqualTo(1));
                Assert.That(cableEvents, Is.EqualTo(1));
            }
            finally
            {
                PowerStrip.ActiveSocketCountChanged -= socketHandler;
                PowerStrip.AllowanceChanged -= powerHandler;
                strip.Cable.CableLengthChanged -= cableHandler;
            }
        }

        [Test]
        public void SocketUpgradePreservesConnectedIdentities()
        {
            var strip = CreatePowerStrip();
            UpgradeSocketCountTo(strip, 2);
            UpgradeAllowedPowerTo(strip, 5f);

            var productA = CreateProduct("TV", new Vector2(2f, 0f));
            var productB = CreateProduct("Fan", new Vector2(3f, 0f));
            var socket01 = strip.Sockets[0];
            var socket02 = strip.Sockets[1];
            var socket03 = strip.Sockets[2];
            var stripId = strip.GetInstanceID();
            var cableId = strip.Cable.GetInstanceID();
            var stripPlugId = strip.Cable.Plug.GetInstanceID();
            var socket01Id = socket01.GetInstanceID();
            var socket02Id = socket02.GetInstanceID();

            Assert.That(Connect(productA.Cable.Plug, socket01), Is.True);
            Assert.That(Connect(productB.Cable.Plug, socket02), Is.True);
            Assert.That(socket03.IsActiveSocket, Is.False);

            Assert.That(
                strip.TryUpgradeActiveSocketCount(out var failure),
                Is.True);
            Assert.That(failure, Is.EqualTo(PowerStripSocketCountFailure.None));

            Assert.That(strip.GetInstanceID(), Is.EqualTo(stripId));
            Assert.That(strip.Cable.GetInstanceID(), Is.EqualTo(cableId));
            Assert.That(strip.Cable.Plug.GetInstanceID(), Is.EqualTo(stripPlugId));
            Assert.That(strip.Sockets[0].GetInstanceID(), Is.EqualTo(socket01Id));
            Assert.That(strip.Sockets[1].GetInstanceID(), Is.EqualTo(socket02Id));
            Assert.That(productA.Cable.Plug.ConnectedSocket, Is.SameAs(socket01));
            Assert.That(socket01.ConnectedPlug, Is.SameAs(productA.Cable.Plug));
            Assert.That(productB.Cable.Plug.ConnectedSocket, Is.SameAs(socket02));
            Assert.That(socket02.ConnectedPlug, Is.SameAs(productB.Cable.Plug));
            Assert.That(socket03.IsActiveSocket, Is.True);
            AssertState(strip, 3, 5f, 3f);
        }

        [Test]
        public void BlockedSocketExpansionRollsBack()
        {
            var strip = CreatePowerStrip();
            UpgradeSocketCountTo(strip, 3);
            UpgradeAllowedPowerTo(strip, 5f);

            var footprint = RequireComponent<PlacementFootprint>(strip.gameObject);
            var currentCells = new List<GridCoord>();
            var prospectiveCells = new List<GridCoord>();
            footprint.GetCoveredCells(Grid, strip.transform.position, 3, currentCells);
            footprint.GetCoveredCells(Grid, strip.transform.position, 4, prospectiveCells);
            var currentSet = new HashSet<GridCoord>(currentCells);
            var blockedCell = default(GridCoord);
            var foundBlockedCell = false;
            for (var i = 0; i < prospectiveCells.Count; i++)
            {
                if (currentSet.Contains(prospectiveCells[i]))
                {
                    continue;
                }

                blockedCell = prospectiveCells[i];
                foundBlockedCell = true;
                break;
            }

            Assert.That(foundBlockedCell, Is.True,
                "Count 4 must introduce a prospective footprint cell.");

            var foreignOwner = new object();
            Grid.SetObjectOccupied(blockedCell, foreignOwner, true);
            var socketEvents = 0;
            Action<PowerStrip, int, int> socketHandler = (changed, _, _) =>
            {
                if (changed == strip)
                {
                    socketEvents++;
                }
            };
            PowerStrip.ActiveSocketCountChanged += socketHandler;
            try
            {
                Assert.That(
                    strip.TryUpgradeActiveSocketCount(out var failure),
                    Is.False);
                Assert.That(
                    failure,
                    Is.EqualTo(PowerStripSocketCountFailure.PlacementUnavailable));
                AssertState(strip, 3, 5f, 3f);
                Assert.That(socketEvents, Is.Zero);
                foreach (var cell in currentCells)
                {
                    Assert.That(Grid.IsObjectOccupied(cell), Is.True);
                    Assert.That(Grid.IsObjectOccupiedByOther(cell, footprint), Is.False);
                }
                Assert.That(Grid.IsObjectOccupiedByOther(blockedCell, footprint), Is.True);
                foreach (var cell in prospectiveCells)
                {
                    if (!currentSet.Contains(cell) && !cell.Equals(blockedCell))
                    {
                        Assert.That(Grid.IsObjectOccupied(cell), Is.False);
                    }
                }
            }
            finally
            {
                PowerStrip.ActiveSocketCountChanged -= socketHandler;
                Grid.SetObjectOccupied(blockedCell, foreignOwner, false);
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void NewInstanceInitializationAppliesRequestedCountAndPreservesIdentity(
            int requestedCount)
        {
            var strip = CreatePowerStrip();
            var footprint = RequireComponent<PlacementFootprint>(strip.gameObject);
            var stripId = strip.GetInstanceID();
            var cableId = strip.Cable.GetInstanceID();
            var plugId = strip.Cable.Plug.GetInstanceID();
            var socketIds = new int[strip.Sockets.Count];
            for (var i = 0; i < socketIds.Length; i++)
            {
                socketIds[i] = strip.Sockets[i].GetInstanceID();
            }

            var topologyEvents = 0;
            Action topologyHandler = () => topologyEvents++;
            PlugSocketConnection.GraphChanged += topologyHandler;
            PowerStripSocketCountFailure failure;
            try
            {
                Assert.That(
                    strip.TryInitializeActiveSocketCount(requestedCount, out failure),
                    Is.True);
            }
            finally
            {
                PlugSocketConnection.GraphChanged -= topologyHandler;
            }

            Assert.That(failure, Is.EqualTo(PowerStripSocketCountFailure.None));
            Assert.That(topologyEvents, Is.EqualTo(1));
            Assert.That(strip.ActiveSocketCount, Is.EqualTo(requestedCount));
            Assert.That(strip.AllowedPowerWatts, Is.EqualTo(strip.InitialAllowedPowerWatts));
            Assert.That(strip.GetInstanceID(), Is.EqualTo(stripId));
            Assert.That(strip.Cable.GetInstanceID(), Is.EqualTo(cableId));
            Assert.That(strip.Cable.Plug.GetInstanceID(), Is.EqualTo(plugId));

            for (var i = 0; i < strip.Sockets.Count; i++)
            {
                Assert.That(strip.Sockets[i].GetInstanceID(), Is.EqualTo(socketIds[i]));
                Assert.That(strip.Sockets[i].IsActiveSocket, Is.EqualTo(i < requestedCount));
            }

            var reservedCells = new List<GridCoord>();
            footprint.GetCoveredCells(
                Grid,
                strip.transform.position,
                requestedCount,
                reservedCells);
            Assert.That(reservedCells, Is.Not.Empty);
            foreach (var cell in reservedCells)
            {
                Assert.That(Grid.IsObjectOccupied(cell), Is.True);
                Assert.That(Grid.IsObjectOccupiedByOther(cell, footprint), Is.False);
            }
        }

        [Test]
        public void InitializationMayReduceCountButUpgradeStillRejectsDecrease()
        {
            var strip = CreatePowerStrip();
            var footprint = RequireComponent<PlacementFootprint>(strip.gameObject);
            Assert.That(
                strip.TryInitializeActiveSocketCount(5, out var expandFailure),
                Is.True);
            Assert.That(expandFailure, Is.EqualTo(PowerStripSocketCountFailure.None));
            var expandedCells = new List<GridCoord>();
            var reducedCells = new List<GridCoord>();
            footprint.GetCoveredCells(Grid, strip.transform.position, 5, expandedCells);
            footprint.GetCoveredCells(Grid, strip.transform.position, 2, reducedCells);
            var reducedSet = new HashSet<GridCoord>(reducedCells);

            Assert.That(
                strip.TryInitializeActiveSocketCount(2, out var initializeFailure),
                Is.True);
            Assert.That(initializeFailure, Is.EqualTo(PowerStripSocketCountFailure.None));
            Assert.That(strip.ActiveSocketCount, Is.EqualTo(2));
            foreach (var cell in expandedCells)
            {
                Assert.That(
                    Grid.IsObjectOccupied(cell),
                    Is.EqualTo(reducedSet.Contains(cell)),
                    $"Reservation mismatch at {cell} after initialization shrink.");
            }

            Assert.That(
                strip.TryUpgradeActiveSocketCount(-1, out var decreaseFailure),
                Is.False);
            Assert.That(
                decreaseFailure,
                Is.EqualTo(PowerStripSocketCountFailure.SocketCountDecreaseUnsupported));
            Assert.That(strip.ActiveSocketCount, Is.EqualTo(2));

            Assert.That(
                strip.TryUpgradeActiveSocketCount(out var upgradeFailure),
                Is.True);
            Assert.That(upgradeFailure, Is.EqualTo(PowerStripSocketCountFailure.None));
            Assert.That(strip.ActiveSocketCount, Is.EqualTo(3));
        }

        [Test]
        public void BlockedInitializationLeavesCountAndReservationUnchanged()
        {
            var strip = CreatePowerStrip();
            var footprint = RequireComponent<PlacementFootprint>(strip.gameObject);
            var currentCells = new List<GridCoord>();
            var prospectiveCells = new List<GridCoord>();
            footprint.GetCoveredCells(Grid, strip.transform.position, 1, currentCells);
            footprint.GetCoveredCells(Grid, strip.transform.position, 2, prospectiveCells);
            var currentSet = new HashSet<GridCoord>(currentCells);
            var blockedCell = prospectiveCells.Find(cell => !currentSet.Contains(cell));
            Assert.That(currentSet.Contains(blockedCell), Is.False);

            var foreignOwner = new object();
            Grid.SetObjectOccupied(blockedCell, foreignOwner, true);
            try
            {
                Assert.That(
                    strip.TryInitializeActiveSocketCount(2, out var failure),
                    Is.False);
                Assert.That(
                    failure,
                    Is.EqualTo(PowerStripSocketCountFailure.PlacementUnavailable));
                Assert.That(strip.ActiveSocketCount, Is.EqualTo(1));
                foreach (var cell in currentCells)
                {
                    Assert.That(Grid.IsObjectOccupied(cell), Is.True);
                    Assert.That(Grid.IsObjectOccupiedByOther(cell, footprint), Is.False);
                }

                Assert.That(Grid.IsObjectOccupiedByOther(blockedCell, footprint), Is.True);
            }
            finally
            {
                Grid.SetObjectOccupied(blockedCell, foreignOwner, false);
            }
        }

        [TestCase(0)]
        [TestCase(6)]
        public void InitializationRejectsOutOfRangeWithoutMutation(int requestedCount)
        {
            var strip = CreatePowerStrip();

            Assert.That(
                strip.TryInitializeActiveSocketCount(requestedCount, out var failure),
                Is.False);
            Assert.That(failure, Is.EqualTo(PowerStripSocketCountFailure.OutOfRange));
            Assert.That(strip.ActiveSocketCount, Is.EqualTo(strip.InitialSocketCount));
        }

        [Test]
        public void InitializationRejectsMissingAuthoredConfigurationEvenAtCurrentCount()
        {
            var strip = CreatePowerStrip();
            UnityEngine.Object.DestroyImmediate(
                RequireComponent<PowerStripSocketLayout>(strip.gameObject));

            Assert.That(
                strip.TryInitializeActiveSocketCount(
                    strip.ActiveSocketCount,
                    out var failure),
                Is.False);
            Assert.That(
                failure,
                Is.EqualTo(PowerStripSocketCountFailure.MissingAuthoredConfiguration));
            Assert.That(strip.ActiveSocketCount, Is.EqualTo(strip.InitialSocketCount));
        }

        [Test]
        public void InactiveSocketRejectsConnection()
        {
            var strip = CreatePowerStrip();
            var product = CreateProduct("TV", new Vector2(2f, 0f));
            var inactiveSocket = strip.Sockets[1];

            Assert.That(strip.ActiveSocketCount, Is.EqualTo(1));
            Assert.That(inactiveSocket.IsActiveSocket, Is.False);
            Assert.That(Connect(product.Cable.Plug, inactiveSocket), Is.False);
            Assert.That(product.Cable.Plug.ConnectedSocket, Is.Null);
            Assert.That(inactiveSocket.ConnectedPlug, Is.Null);
        }

        private static void AssertState(
            PowerStrip strip,
            int socketCount,
            float allowedPower,
            float cableLength)
        {
            Assert.That(strip.ActiveSocketCount, Is.EqualTo(socketCount));
            Assert.That(strip.AllowedPowerWatts, Is.EqualTo(allowedPower));
            Assert.That(strip.Cable.CableLength, Is.EqualTo(cableLength));
        }
    }
}
