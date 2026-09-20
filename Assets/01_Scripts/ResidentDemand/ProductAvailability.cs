using System;

namespace Octoplug.ResidentDemand
{
    public sealed class ProductAvailability
    {
        public ProductAvailability(
            string id,
            ResidentNeedType usageType,
            int capacity,
            bool isPowered)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Product id is required.", nameof(id));
            }

            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            Id = id;
            UsageType = usageType;
            Capacity = capacity;
            IsPowered = isPowered;
        }

        public string Id { get; }
        public ResidentNeedType UsageType { get; }
        public int Capacity { get; }
        public bool IsPowered { get; }
    }
}
