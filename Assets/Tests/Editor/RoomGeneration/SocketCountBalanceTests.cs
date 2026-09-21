using System;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.RoomGeneration;

namespace Octoplug.Tests.Editor.RoomGeneration
{
    public sealed class SocketCountBalanceTests
    {
        [Test]
        public void DefaultMultitapCatalog_HasExactEntries()
        {
            AssertRecords(
                DefaultSocketCountBalance.CreateMultitapCatalog().Records,
                (1, 40),
                (2, 30),
                (3, 15),
                (4, 10),
                (5, 5));
        }

        [Test]
        public void DefaultWallOutletCatalog_HasExactEntries()
        {
            AssertRecords(
                DefaultSocketCountBalance.CreateWallOutletCatalog().Records,
                (1, 60),
                (2, 25),
                (3, 10),
                (4, 4),
                (5, 1));
        }

        [Test]
        public void DefaultRoomConfigs_PreserveWallOutletSocketRanges()
        {
            var configs = DefaultRoomContentBalance.GetDefaultConfigs();

            Assert.That(configs.Count, Is.EqualTo(7));
            foreach (var config in configs)
            {
                Assert.That(config.WallOutletSocketMin, Is.InRange(1, 5));
                Assert.That(config.WallOutletSocketMax, Is.InRange(1, 5));
                Assert.That(
                    config.WallOutletSocketMin,
                    Is.LessThanOrEqualTo(config.WallOutletSocketMax));
            }

            Assert.That(configs[3].Id, Is.EqualTo("RC004"));
            Assert.That(configs[3].WallOutletSocketMin, Is.EqualTo(2));
            Assert.That(configs[3].WallOutletSocketMax, Is.EqualTo(4));
            Assert.That(configs[6].Id, Is.EqualTo("RC007"));
            Assert.That(configs[6].WallOutletSocketMin, Is.EqualTo(3));
            Assert.That(configs[6].WallOutletSocketMax, Is.EqualTo(5));
        }

        [Test]
        public void DefaultRoomConfigs_PreserveGoogleSheetDerivedProductCounts()
        {
            var configs = DefaultRoomContentBalance.GetDefaultConfigs();
            var expected = new[]
            {
                ("RC001", 1, 0, 0, 0, 0),
                ("RC002", 0, 1, 0, 0, 0),
                ("RC003", 0, 0, 1, 0, 0),
                ("RC004", 1, 1, 0, 0, 0),
                ("RC005", 0, 0, 0, 1, 0),
                ("RC006", 0, 0, 0, 0, 1),
                ("RC007", 1, 0, 1, 1, 0)
            };

            Assert.That(configs.Count, Is.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(configs[i].Id, Is.EqualTo(expected[i].Item1));
                Assert.That(configs[i].TvCount, Is.EqualTo(expected[i].Item2));
                Assert.That(configs[i].FanCount, Is.EqualTo(expected[i].Item3));
                Assert.That(configs[i].HeaterCount, Is.EqualTo(expected[i].Item4));
                Assert.That(configs[i].InductionCount, Is.EqualTo(expected[i].Item5));
                Assert.That(configs[i].AirConditionerCount, Is.EqualTo(expected[i].Item6));
            }
        }

        [Test]
        public void Selector_FiltersEntriesBeforeCalculatingTotalWeight()
        {
            var selector = new WeightedSocketCountSelector(
                DefaultSocketCountBalance.CreateMultitapCatalog());

            var eligible = selector.GetEligibleRecords(2, 4);

            AssertRecords(eligible, (2, 30), (3, 15), (4, 10));
            Assert.That(selector.GetTotalWeight(2, 4), Is.EqualTo(55));
        }

        [TestCase(0, 2)]
        [TestCase(29, 2)]
        [TestCase(30, 3)]
        [TestCase(44, 3)]
        [TestCase(45, 4)]
        [TestCase(54, 4)]
        public void Select_UsesFilteredCumulativeIntervalBoundaries(int roll, int expectedSocketCount)
        {
            var selector = new WeightedSocketCountSelector(
                DefaultSocketCountBalance.CreateMultitapCatalog());

            Assert.That(selector.Select(2, 4, roll), Is.EqualTo(expectedSocketCount));
        }

        [Test]
        public void Select_UsesCatalogOrderForCumulativeIntervals()
        {
            var selector = new WeightedSocketCountSelector(new SocketCountWeightCatalog(new[]
            {
                new SocketCountWeightRecord(5, 2),
                new SocketCountWeightRecord(1, 100),
                new SocketCountWeightRecord(3, 3)
            }));

            Assert.That(selector.GetTotalWeight(3, 5), Is.EqualTo(5));
            Assert.That(selector.Select(3, 5, 0), Is.EqualTo(5));
            Assert.That(selector.Select(3, 5, 1), Is.EqualTo(5));
            Assert.That(selector.Select(3, 5, 2), Is.EqualTo(3));
            Assert.That(selector.Select(3, 5, 4), Is.EqualTo(3));
        }

        [Test]
        public void Selector_RejectsInvalidRangesAndRolls()
        {
            var selector = new WeightedSocketCountSelector(
                DefaultSocketCountBalance.CreateWallOutletCatalog());

            Assert.Throws<ArgumentOutOfRangeException>(() => selector.GetTotalWeight(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => selector.GetTotalWeight(1, 6));
            Assert.Throws<ArgumentException>(() => selector.GetTotalWeight(4, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => selector.Select(2, 3, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => selector.Select(2, 3, 35));
        }

        [Test]
        public void Selector_RejectsRangeWithoutAnEligibleEntry()
        {
            var selector = new WeightedSocketCountSelector(new SocketCountWeightCatalog(new[]
            {
                new SocketCountWeightRecord(1, 10),
                new SocketCountWeightRecord(5, 5)
            }));

            Assert.Throws<InvalidOperationException>(() => selector.GetEligibleRecords(2, 4));
            Assert.Throws<InvalidOperationException>(() => selector.GetTotalWeight(2, 4));
            Assert.Throws<InvalidOperationException>(() => selector.Select(2, 4, 0));
        }

        [Test]
        public void RecordsAndCatalog_RejectInvalidData()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SocketCountWeightRecord(0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SocketCountWeightRecord(6, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SocketCountWeightRecord(1, 0));
            Assert.Throws<ArgumentException>(() => new SocketCountWeightCatalog(null));
            Assert.Throws<ArgumentException>(() =>
                new SocketCountWeightCatalog(Array.Empty<SocketCountWeightRecord>()));
            Assert.Throws<ArgumentException>(() => new SocketCountWeightCatalog(new SocketCountWeightRecord[]
            {
                new SocketCountWeightRecord(1, 1),
                null
            }));
            Assert.Throws<ArgumentException>(() => new SocketCountWeightCatalog(new[]
            {
                new SocketCountWeightRecord(2, 1),
                new SocketCountWeightRecord(2, 2)
            }));
        }

        [Test]
        public void Import_MapsInvariantIntegerColumnsAndValidatesResult()
        {
            var catalog = SocketCountWeightImporter.Import(new[]
            {
                ImportRecord(" 2 ", "30"),
                ImportRecord("3", "15")
            });

            AssertRecords(catalog.Records, (2, 30), (3, 15));
            Assert.Throws<FormatException>(() => SocketCountWeightImporter.Import(new[]
            {
                ImportRecord("two", "30")
            }));
            Assert.Throws<FormatException>(() => SocketCountWeightImporter.Import(new[]
            {
                new SocketCountWeightImportRecord(new Dictionary<string, string>
                {
                    [SocketCountWeightImporter.SocketCount] = "2"
                })
            }));
            Assert.Throws<ArgumentException>(() => SocketCountWeightImporter.Import(new[]
            {
                ImportRecord("2", "30"),
                ImportRecord("2", "15")
            }));
        }

        private static SocketCountWeightImportRecord ImportRecord(string socketCount, string weight)
        {
            return new SocketCountWeightImportRecord(new Dictionary<string, string>
            {
                [SocketCountWeightImporter.SocketCount] = socketCount,
                [SocketCountWeightImporter.Weight] = weight
            });
        }

        private static void AssertRecords(
            IReadOnlyList<SocketCountWeightRecord> actual,
            params (int SocketCount, int Weight)[] expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Length));
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.That(actual[index].SocketCount, Is.EqualTo(expected[index].SocketCount));
                Assert.That(actual[index].Weight, Is.EqualTo(expected[index].Weight));
            }
        }
    }
}
