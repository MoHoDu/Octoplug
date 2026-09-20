using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public sealed class DemandImportRecord
    {
        public DemandImportRecord(IReadOnlyDictionary<string, string> values)
        {
            Values = values;
        }

        public IReadOnlyDictionary<string, string> Values { get; }
    }
}
