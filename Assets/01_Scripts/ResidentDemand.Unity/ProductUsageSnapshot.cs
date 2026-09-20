using System;
using System.Collections.Generic;
using Octoplug.Power;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class ProductUsageSnapshot
    {
        public ProductUsageSnapshot(
            string productId,
            ApplianceSource product,
            IReadOnlyList<ResidentNumber> activeResidents,
            IReadOnlyList<ResidentNumber> waitingResidents)
        {
            ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
            Product = product != null
                ? product
                : throw new ArgumentNullException(nameof(product));
            ActiveResidents = activeResidents ??
                throw new ArgumentNullException(nameof(activeResidents));
            WaitingResidents = waitingResidents ??
                throw new ArgumentNullException(nameof(waitingResidents));
        }

        public string ProductId { get; }
        public ApplianceSource Product { get; }
        public IReadOnlyList<ResidentNumber> ActiveResidents { get; }
        public IReadOnlyList<ResidentNumber> WaitingResidents { get; }
    }
}
