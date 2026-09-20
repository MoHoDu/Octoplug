using System;
using System.Collections.Generic;
using System.Globalization;

namespace Octoplug.ResidentDemand
{
    public static class DemandBalanceImporter
    {
        public const string Id = "DemandId";
        public const string Enabled = "Enabled";
        public const string RequiredRoomCount = "RequiredRoomCount";
        public const string Weight = "Weight";
        public const string FirstNeed = "Need1";
        public const string SecondNeed = "Need2";
        public const string SatisfactionFillSeconds = "SatisfactionFillSec";
        public const string PatienceFillSeconds = "PatienceFillSec";
        public const string ExperienceReward = "ExpReward";
        public const string GlobalSatisfactionOnSuccess = "GlobalSuccess";
        public const string GlobalSatisfactionOnFailure = "GlobalFailure";
        public const string CooldownSeconds = "CooldownSec";

        public static DemandBalanceCatalog Import(IReadOnlyList<DemandImportRecord> records)
        {
            if (records == null)
            {
                throw new ArgumentNullException(nameof(records));
            }

            var rows = new DemandAuthoringRow[records.Count];
            for (var index = 0; index < records.Count; index++)
            {
                var values = records[index]?.Values
                    ?? throw new ArgumentException("Import records cannot contain null.", nameof(records));
                rows[index] = new DemandAuthoringRow(
                    Required(values, Id),
                    ParseBool(values, Enabled),
                    ParseInt(values, RequiredRoomCount),
                    ParseInt(values, Weight),
                    Required(values, FirstNeed),
                    Optional(values, SecondNeed),
                    ParseFloat(values, SatisfactionFillSeconds),
                    ParseFloat(values, PatienceFillSeconds),
                    ParseInt(values, ExperienceReward),
                    ParseInt(values, GlobalSatisfactionOnSuccess),
                    ParseInt(values, GlobalSatisfactionOnFailure),
                    ParseFloat(values, CooldownSeconds));
            }

            return DemandBalanceCatalog.FromAuthoringRows(rows);
        }

        private static string Required(IReadOnlyDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new FormatException($"Required demand column '{key}' is missing or empty.");
            }

            return value.Trim();
        }

        private static string Optional(IReadOnlyDictionary<string, string> values, string key)
        {
            return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        private static bool ParseBool(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = Required(values, key);
            if (!bool.TryParse(value, out var result))
            {
                throw new FormatException($"Demand column '{key}' must be true or false; received '{value}'.");
            }

            return result;
        }

        private static int ParseInt(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = Required(values, key);
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                throw new FormatException($"Demand column '{key}' must be an integer; received '{value}'.");
            }

            return result;
        }

        private static float ParseFloat(IReadOnlyDictionary<string, string> values, string key)
        {
            var value = Required(values, key);
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                throw new FormatException($"Demand column '{key}' must be a number; received '{value}'.");
            }

            return result;
        }
    }
}
