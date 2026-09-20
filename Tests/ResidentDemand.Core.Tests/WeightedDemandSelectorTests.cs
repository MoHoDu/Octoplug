using NUnit.Framework;

namespace Octoplug.ResidentDemand.Tests
{
    public sealed class WeightedDemandSelectorTests
    {
        [Test]
        public void Select_FiltersByRoomCountAndUsesStableWeightOrder()
        {
            var catalog = DefaultDemandBalance.Create();

            Assert.That(new WeightedDemandSelector(catalog, new FixedRandomSource(0)).Select(1).Id, Is.EqualTo("D001"));
            Assert.That(new WeightedDemandSelector(catalog, new FixedRandomSource(9)).Select(1).Id, Is.EqualTo("D001"));
            Assert.That(new WeightedDemandSelector(catalog, new FixedRandomSource(10)).Select(1).Id, Is.EqualTo("D002"));
            Assert.That(new WeightedDemandSelector(catalog, new FixedRandomSource(20)).Select(2).Id, Is.EqualTo("D003"));
        }

        [Test]
        public void Select_RequestsTotalEligibleWeight()
        {
            var random = new RecordingRandomSource();

            new WeightedDemandSelector(DefaultDemandBalance.Create(), random).Select(3);

            Assert.That(random.MaximumExclusive, Is.EqualTo(38));
        }

        [Test]
        public void TrySelect_RequiresEveryNeedAndRoomThreshold()
        {
            Assert.That(
                new WeightedDemandSelector(
                    DefaultDemandBalance.Create(),
                    new FixedRandomSource(0)).TrySelect(
                        15,
                        ResidentNeedType.Cooling,
                        out _),
                Is.True,
                "Single-Need Cooling remains eligible.");

            Assert.That(
                new WeightedDemandSelector(
                    DefaultDemandBalance.Create(),
                    new FixedRandomSource(20)).TrySelect(
                        15,
                        ResidentNeedType.Cooling | ResidentNeedType.Fun,
                        out var selected),
                Is.True);
            Assert.That(selected.Id, Is.EqualTo("D005"));

            Assert.That(
                new WeightedDemandSelector(
                    DefaultDemandBalance.Create(),
                    new FixedRandomSource(0)).TrySelect(
                        14,
                        ResidentNeedType.Cooling | ResidentNeedType.Fun,
                        out selected),
                Is.True);
            Assert.That(selected.Id, Is.EqualTo("D001"));
        }

        [Test]
        public void TrySelect_NoCandidateReturnsFalseWithoutRandomCall()
        {
            var random = new RecordingRandomSource();
            var selector = new WeightedDemandSelector(DefaultDemandBalance.Create(), random);

            Assert.That(
                selector.TrySelect(1, ResidentNeedType.Meal, out var selected),
                Is.False);
            Assert.That(selected, Is.Null);
            Assert.That(random.CallCount, Is.Zero);
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly int _value;

            public FixedRandomSource(int value)
            {
                _value = value;
            }

            public int Next(int maximumExclusive) => _value;
        }

        private sealed class RecordingRandomSource : IRandomSource
        {
            public int MaximumExclusive { get; private set; }
            public int CallCount { get; private set; }

            public int Next(int maximumExclusive)
            {
                MaximumExclusive = maximumExclusive;
                CallCount++;
                return 0;
            }
        }
    }
}
