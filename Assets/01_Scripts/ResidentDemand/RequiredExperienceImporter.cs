using System;
using System.Collections.Generic;
using System.Globalization;

namespace Octoplug.ResidentDemand
{
    public static class RequiredExperienceImporter
    {
        public const string RoomCount = "RoomCount";
        public const string RequiredExperience = "RequiredEXP";

        public static RequiredExperienceTable Import(
            IReadOnlyList<IReadOnlyDictionary<string, string>> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            var entries = new RequiredExperienceEntry[records.Count];
            for (var index = 0; index < records.Count; index++)
            {
                var values = records[index]
                    ?? throw new ArgumentException("Import records cannot contain null.", nameof(records));
                entries[index] = new RequiredExperienceEntry(
                    ParseInt(values, RoomCount),
                    ParseInt(values, RequiredExperience));
            }

            return new RequiredExperienceTable(entries);
        }

        public static RequiredExperienceTable FromAuthoringRows(
            IReadOnlyList<RequiredExperienceAuthoringRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            var entries = new RequiredExperienceEntry[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index]
                    ?? throw new ArgumentException("Authoring rows cannot contain null.", nameof(rows));
                entries[index] = new RequiredExperienceEntry(
                    row.RoomCount,
                    row.RequiredExperience);
            }

            return new RequiredExperienceTable(entries);
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var value) ||
                string.IsNullOrWhiteSpace(value) ||
                !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                throw new FormatException(
                    $"Progression column '{key}' must be an integer; received '{value}'.");
            }

            return result;
        }
    }
}
