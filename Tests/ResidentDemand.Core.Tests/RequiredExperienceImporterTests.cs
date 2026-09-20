using System.Collections.Generic;
using NUnit.Framework;

namespace Octoplug.ResidentDemand.Tests
{
    public sealed class RequiredExperienceImporterTests
    {
        [Test]
        public void Import_UsesExplicitRoomCountAndRequiredExperienceColumns()
        {
            var table = RequiredExperienceImporter.Import(new[]
            {
                Row("1", "20"),
                Row("2", "30"),
            });

            Assert.That(table.GetRequiredExperience(1), Is.EqualTo(20));
            Assert.That(table.GetRequiredExperience(2), Is.EqualTo(30));
        }

        [Test]
        public void Import_DuplicateRoomCount_IsRejected()
        {
            Assert.Throws<System.ArgumentException>(() =>
                RequiredExperienceImporter.Import(new[]
                {
                    Row("1", "20"),
                    Row("1", "30"),
                }));
        }

        [Test]
        public void Import_MissingOrInvalidValues_AreRejected()
        {
            Assert.Throws<System.FormatException>(() =>
                RequiredExperienceImporter.Import(new[]
                {
                    Row("one", "20"),
                }));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                RequiredExperienceImporter.Import(new[]
                {
                    Row("1", "0"),
                }));
        }

        private static IReadOnlyDictionary<string, string> Row(
            string roomCount,
            string requiredExperience)
        {
            return new Dictionary<string, string>
            {
                [RequiredExperienceImporter.RoomCount] = roomCount,
                [RequiredExperienceImporter.RequiredExperience] = requiredExperience,
            };
        }
    }
}
