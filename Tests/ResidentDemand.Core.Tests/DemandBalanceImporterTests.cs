using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Octoplug.ResidentDemand.Tests
{
    public sealed class DemandBalanceImporterTests
    {
        [Test]
        public void Import_MapsEditorProvidedColumnsWithoutNetworkDependency()
        {
            var values = ValidValues();
            values[DemandBalanceImporter.SecondNeed] = "Fun";

            var record = DemandBalanceImporter.Import(new[] { new DemandImportRecord(values) }).Records[0];

            Assert.That(record.Id, Is.EqualTo("D005"));
            Assert.That(record.Enabled, Is.True);
            Assert.That(record.Needs, Is.EqualTo(new[] { ResidentNeedType.Cooling, ResidentNeedType.Fun }));
            Assert.That(record.GlobalSatisfactionOnFailure, Is.EqualTo(-10));
        }

        [Test]
        public void Import_MissingRequiredColumnIsRejectedWithColumnName()
        {
            var values = ValidValues();
            values.Remove(DemandBalanceImporter.Weight);

            var error = Assert.Throws<FormatException>(() =>
                DemandBalanceImporter.Import(new[] { new DemandImportRecord(values) }));

            Assert.That(error.Message, Does.Contain(DemandBalanceImporter.Weight));
        }

        [Test]
        public void Import_UsesInvariantNumericParsing()
        {
            var values = ValidValues();
            values[DemandBalanceImporter.SatisfactionFillSeconds] = "6,5";

            Assert.Throws<FormatException>(() =>
                DemandBalanceImporter.Import(new[] { new DemandImportRecord(values) }));
        }

        private static Dictionary<string, string> ValidValues()
        {
            return new Dictionary<string, string>
            {
                [DemandBalanceImporter.Id] = "D005",
                [DemandBalanceImporter.Enabled] = "true",
                [DemandBalanceImporter.RequiredRoomCount] = "3",
                [DemandBalanceImporter.Weight] = "6",
                [DemandBalanceImporter.FirstNeed] = "Cold",
                [DemandBalanceImporter.SecondNeed] = string.Empty,
                [DemandBalanceImporter.SatisfactionFillSeconds] = "6",
                [DemandBalanceImporter.PatienceFillSeconds] = "8",
                [DemandBalanceImporter.ExperienceReward] = "18",
                [DemandBalanceImporter.GlobalSatisfactionOnSuccess] = "7",
                [DemandBalanceImporter.GlobalSatisfactionOnFailure] = "-10",
                [DemandBalanceImporter.CooldownSeconds] = "5",
            };
        }
    }
}
