using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    public sealed class SocketCountWeightImportRecord
    {
        public SocketCountWeightImportRecord(IReadOnlyDictionary<string, string> values)
        {
            Values = values;
        }

        public IReadOnlyDictionary<string, string> Values { get; }
    }
}
