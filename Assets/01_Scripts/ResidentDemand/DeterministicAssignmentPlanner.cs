using System;
using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public static class DeterministicAssignmentPlanner
    {
        public static ProductAssignmentPlan Compute(
            IReadOnlyList<ProductAvailability> products,
            IReadOnlyList<ResidentAssignmentRequest> requests)
        {
            if (products == null)
            {
                throw new ArgumentNullException(nameof(products));
            }

            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            var orderedProducts = CopyProducts(products);
            var orderedRequests = CopyRequests(requests);
            var occupied = new Dictionary<string, int>(StringComparer.Ordinal);
            var assignedResidents = new HashSet<int>();
            var assignments = new List<ResidentProductAssignment>(orderedRequests.Count);
            var waiting = new List<ResidentAssignmentRequest>();

            foreach (var request in orderedRequests)
            {
                if (assignedResidents.Contains(request.ResidentNumber.Value))
                {
                    waiting.Add(request);
                    continue;
                }

                ProductAvailability selected = null;
                foreach (var product in orderedProducts)
                {
                    occupied.TryGetValue(product.Id, out var currentUsers);
                    if (product.IsPowered &&
                        (product.UsageType & request.NeedType) != 0 &&
                        currentUsers < product.Capacity)
                    {
                        selected = product;
                        break;
                    }
                }

                if (selected == null)
                {
                    waiting.Add(request);
                    continue;
                }

                occupied.TryGetValue(selected.Id, out var users);
                occupied[selected.Id] = users + 1;
                assignedResidents.Add(request.ResidentNumber.Value);
                assignments.Add(new ResidentProductAssignment(request, selected.Id));
            }

            return new ProductAssignmentPlan(assignments, waiting);
        }

        private static List<ProductAvailability> CopyProducts(IReadOnlyList<ProductAvailability> products)
        {
            var result = new List<ProductAvailability>(products.Count);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var product in products)
            {
                if (product == null)
                {
                    throw new ArgumentException("Product list cannot contain null.", nameof(products));
                }

                if (!ids.Add(product.Id))
                {
                    throw new ArgumentException($"Duplicate product id '{product.Id}'.", nameof(products));
                }

                result.Add(product);
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return result;
        }

        private static List<ResidentAssignmentRequest> CopyRequests(IReadOnlyList<ResidentAssignmentRequest> requests)
        {
            var result = new List<ResidentAssignmentRequest>(requests.Count);
            foreach (var request in requests)
            {
                result.Add(request ?? throw new ArgumentException("Request list cannot contain null.", nameof(requests)));
            }

            result.Sort(CompareRequests);
            return result;
        }

        private static int CompareRequests(ResidentAssignmentRequest left, ResidentAssignmentRequest right)
        {
            var sequence = left.Sequence.CompareTo(right.Sequence);
            if (sequence != 0)
            {
                return sequence;
            }

            var resident = left.ResidentNumber.Value.CompareTo(right.ResidentNumber.Value);
            return resident != 0 ? resident : left.NeedIndex.CompareTo(right.NeedIndex);
        }
    }
}
