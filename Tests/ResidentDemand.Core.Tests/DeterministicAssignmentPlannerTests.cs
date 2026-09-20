using NUnit.Framework;

namespace Octoplug.ResidentDemand.Tests
{
    public sealed class DeterministicAssignmentPlannerTests
    {
        [Test]
        public void Compute_UsesPoweredMatchingProductsOnly()
        {
            var products = new[]
            {
                Product("B", ResidentNeedType.Cooling, 1, true),
                Product("A", ResidentNeedType.Cooling, 1, false),
                Product("C", ResidentNeedType.Fun, 1, true),
            };
            var request = Request(1, 0, ResidentNeedType.Cooling, 0);

            var plan = DeterministicAssignmentPlanner.Compute(products, new[] { request });

            Assert.That(plan.Assignments, Has.Count.EqualTo(1));
            Assert.That(plan.Assignments[0].ProductId, Is.EqualTo("B"));
        }

        [Test]
        public void Compute_MatchesProductsWithMultipleUsageFlags()
        {
            var product = Product(
                "Combo",
                ResidentNeedType.Cooling | ResidentNeedType.Fun,
                1,
                true);

            var plan = DeterministicAssignmentPlanner.Compute(
                new[] { product },
                new[] { Request(1, 0, ResidentNeedType.Fun, 0) });

            Assert.That(plan.Assignments, Has.Count.EqualTo(1));
        }

        [Test]
        public void Compute_UsesStableProductIdOrderAndRespectsCapacity()
        {
            var products = new[]
            {
                Product("B", ResidentNeedType.Cooling, 2, true),
                Product("A", ResidentNeedType.Cooling, 1, true),
            };
            var requests = new[]
            {
                Request(3, 0, ResidentNeedType.Cooling, 2),
                Request(1, 0, ResidentNeedType.Cooling, 0),
                Request(2, 0, ResidentNeedType.Cooling, 1),
                Request(4, 0, ResidentNeedType.Cooling, 3),
            };

            var plan = DeterministicAssignmentPlanner.Compute(products, requests);

            Assert.That(plan.Assignments[0].ProductId, Is.EqualTo("A"));
            Assert.That(plan.Assignments[1].ProductId, Is.EqualTo("B"));
            Assert.That(plan.Assignments[2].ProductId, Is.EqualTo("B"));
            Assert.That(plan.Waiting[0].ResidentNumber.Value, Is.EqualTo(4));
        }

        [Test]
        public void Compute_AssignsOnlyOneNeedPerResident()
        {
            var products = new[]
            {
                Product("Cooling", ResidentNeedType.Cooling, 1, true),
                Product("Fun", ResidentNeedType.Fun, 1, true),
            };
            var requests = new[]
            {
                Request(1, 1, ResidentNeedType.Fun, 1),
                Request(1, 0, ResidentNeedType.Cooling, 0),
            };

            var plan = DeterministicAssignmentPlanner.Compute(products, requests);

            Assert.That(plan.Assignments, Has.Count.EqualTo(1));
            Assert.That(plan.Assignments[0].Request.NeedIndex, Is.Zero);
            Assert.That(plan.Waiting, Has.Count.EqualTo(1));
        }

        [Test]
        public void Compute_RepeatedInputProducesSamePlan()
        {
            var products = new[] { Product("Fan", ResidentNeedType.Cooling, 1, true) };
            var requests = new[]
            {
                Request(2, 0, ResidentNeedType.Cooling, 1),
                Request(1, 0, ResidentNeedType.Cooling, 0),
            };

            var first = DeterministicAssignmentPlanner.Compute(products, requests);
            var second = DeterministicAssignmentPlanner.Compute(products, requests);

            Assert.That(second.Assignments[0].Request.ResidentNumber.Value,
                Is.EqualTo(first.Assignments[0].Request.ResidentNumber.Value));
            Assert.That(second.Waiting[0].ResidentNumber.Value,
                Is.EqualTo(first.Waiting[0].ResidentNumber.Value));
        }

        private static ProductAvailability Product(
            string id,
            ResidentNeedType need,
            int capacity,
            bool powered)
        {
            return new ProductAvailability(id, need, capacity, powered);
        }

        private static ResidentAssignmentRequest Request(
            int resident,
            int needIndex,
            ResidentNeedType need,
            long sequence)
        {
            return new ResidentAssignmentRequest(new ResidentNumber(resident), needIndex, need, sequence);
        }
    }
}
