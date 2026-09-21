using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    public sealed class SocketCountWeightCatalog
    {
        private readonly SocketCountWeightRecord[] _records;

        public SocketCountWeightCatalog(IReadOnlyList<SocketCountWeightRecord> records)
        {
            if (records == null || records.Count == 0)
            {
                throw new ArgumentException("At least one socket-count weight record is required.", nameof(records));
            }

            var socketCounts = new HashSet<int>();
            _records = new SocketCountWeightRecord[records.Count];
            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index]
                    ?? throw new ArgumentException("Socket-count weight records cannot contain null.", nameof(records));
                if (!socketCounts.Add(record.SocketCount))
                {
                    throw new ArgumentException(
                        $"Duplicate socket count '{record.SocketCount}'.",
                        nameof(records));
                }

                _records[index] = record;
            }
        }

        public IReadOnlyList<SocketCountWeightRecord> Records => _records;

        public static SocketCountWeightCatalog FromAuthoringRows(
            IReadOnlyList<SocketCountWeightAuthoringRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            var records = new SocketCountWeightRecord[rows.Count];
            for (var index = 0; index < rows.Count; index++)
            {
                var row = rows[index]
                    ?? throw new ArgumentException("Authoring rows cannot contain null.", nameof(rows));
                records[index] = new SocketCountWeightRecord(row.SocketCount, row.Weight);
            }

            return new SocketCountWeightCatalog(records);
        }
    }
}
