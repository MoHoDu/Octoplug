using System;
using NUnit.Framework;

namespace Octoplug.ResidentDemand.Tests
{
    public sealed class DemandBalanceTests
    {
        [TestCase("Cold", ResidentNeedType.Cooling)]
        [TestCase("Hot", ResidentNeedType.Heating)]
        [TestCase("Food", ResidentNeedType.Meal)]
        [TestCase("Fun", ResidentNeedType.Fun)]
        public void MapNeed_UsesExplicitAuthoringMapping(string value, ResidentNeedType expected)
        {
            Assert.That(DemandAuthoringMapper.MapNeed(value), Is.EqualTo(expected));
        }

        [Test]
        public void MapNeed_UnknownValueIsRejected()
        {
            Assert.Throws<ArgumentException>(() => DemandAuthoringMapper.MapNeed("Television"));
        }

        [Test]
        public void DefaultCatalog_ContainsSheetRowsInStableOrder()
        {
            var records = DefaultDemandBalance.Create().Records;

            Assert.That(records, Has.Count.EqualTo(8));
            Assert.That(records[0].Id, Is.EqualTo("D001"));
            Assert.That(records, Has.All.Property(nameof(DemandBalanceRecord.Enabled)).True);
            Assert.That(records[4].RequiredRoomCount, Is.EqualTo(15));
            Assert.That(records[4].Needs, Is.EqualTo(new[] { ResidentNeedType.Cooling, ResidentNeedType.Fun }));
            Assert.That(records[7].RequiredRoomCount, Is.EqualTo(15));
            Assert.That(records[7].ExperienceReward, Is.EqualTo(22));
        }

        [Test]
        public void Catalog_DuplicateIdsAreRejected()
        {
            var row = new DemandAuthoringRow("D001", true, 1, 1, "Cold", null, 1f, 1f, 0, 0, 0, 0f);

            Assert.Throws<ArgumentException>(() => DemandBalanceCatalog.FromAuthoringRows(new[] { row, row }));
        }
    }
}
