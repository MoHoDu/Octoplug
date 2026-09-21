using System;
using System.Collections.Generic;
using System.Globalization;

namespace Octoplug.RoomGeneration
{
    public static class SocketCountWeightImporter
    {
        public const string SocketCount = "SocketCount";
        public const string Weight = "Weight";

        public static SocketCountWeightCatalog Import(
            IReadOnlyList<SocketCountWeightImportRecord> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            var rows = new SocketCountWeightAuthoringRow[records.Count];
            for (var index = 0; index < records.Count; index++)
            {
                var values = records[index]?.Values
                    ?? throw new ArgumentException("Import records cannot contain null.", nameof(records));
                rows[index] = new SocketCountWeightAuthoringRow(
                    ParseInt(values, SocketCount),
                    ParseInt(values, Weight));
            }

            return SocketCountWeightCatalog.FromAuthoringRows(rows);
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException($"Required socket-count column '{key}' is missing or empty.");
            }

            value = value.Trim();
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                throw new FormatException(
                    $"Socket-count column '{key}' must be an integer; received '{value}'.");
            }

            return result;
        }
    }
}
