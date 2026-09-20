using NUnit.Framework;
using Octoplug.Power;
using Octoplug.ResidentDemand;
using Octoplug.ResidentDemand.Unity;

namespace Octoplug.Tests.Editor.ResidentDemand
{
    public sealed class UsageTypeMapperTests
    {
        [Test]
        public void Map_PreservesAllAuthoritativeUsageFlags()
        {
            var usageTypes = UsageType.Cooling | UsageType.Fun;

            var result = UsageTypeMapper.Map(usageTypes);

            Assert.That(
                result,
                Is.EqualTo(ResidentNeedType.Cooling | ResidentNeedType.Fun));
        }

        [Test]
        public void Map_NoneReturnsNone()
        {
            Assert.That(
                UsageTypeMapper.Map(UsageType.None),
                Is.EqualTo(ResidentNeedType.None));
        }

        [TestCase(ResidentNeedType.Cooling, UsageType.Cooling)]
        [TestCase(ResidentNeedType.Heating, UsageType.Heating)]
        [TestCase(ResidentNeedType.Meal, UsageType.Meal)]
        [TestCase(ResidentNeedType.Fun, UsageType.Fun)]
        public void MapSingle_ReturnsCanonicalUsageType(
            ResidentNeedType needType,
            UsageType expected)
        {
            Assert.That(UsageTypeMapper.MapSingle(needType), Is.EqualTo(expected));
        }

        [Test]
        public void MapSingle_RejectsCombinedNeedFlags()
        {
            Assert.That(
                () => UsageTypeMapper.MapSingle(
                    ResidentNeedType.Cooling | ResidentNeedType.Fun),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
