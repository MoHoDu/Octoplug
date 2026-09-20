using System;
using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public sealed class DemandBalanceCatalog
    {
        private readonly DemandBalanceRecord[] _records;

        public DemandBalanceCatalog(IReadOnlyList<DemandBalanceRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                throw new ArgumentException("At least one demand balance record is required.", nameof(records));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            _records = new DemandBalanceRecord[records.Count];
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index] ?? throw new ArgumentException("Demand records cannot contain null.", nameof(records));
                if (!ids.Add(record.Id))
                {
                    throw new ArgumentException($"Duplicate demand id '{record.Id}'.", nameof(records));
                }

                _records[index] = record;
            }
        }

        public IReadOnlyList<DemandBalanceRecord> Records => _records;

        public static DemandBalanceCatalog FromAuthoringRows(IReadOnlyList<DemandAuthoringRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            var records = new DemandBalanceRecord[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                records[index] = DemandAuthoringMapper.Map(rows[index]);
            }

            return new DemandBalanceCatalog(records);
        }
    }
}
