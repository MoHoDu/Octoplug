using System.Linq;
using NUnit.Framework;
using Octoplug.Balance;
using Octoplug.Reward;
using UnityEngine;

namespace Octoplug.Tests.Editor.Balance
{
    public sealed class BalanceRegistryTests
    {
        [Test]
        public void RuntimeArchive_IsLoadableFromExpectedResourcesPath()
        {
            var archive = Resources.Load<GameBalanceArchive>("Balance/GameBalanceArchive");

            Assert.That(archive, Is.Not.Null);
            Assert.That(archive.DemandRows, Is.Not.Empty);
            Assert.That(archive.RoomConfigRows, Is.Not.Empty);
            Assert.That(archive.ProductRedistributionRows, Is.Not.Empty);
            Assert.That(archive.ProductSpawnPoolRows, Is.Not.Empty);
            Assert.That(archive.ProgressionRows, Is.Not.Empty);
            Assert.That(archive.RewardRows, Is.Not.Empty);
            Assert.That(archive.PowerStripSpawnRows, Is.Not.Empty);
            Assert.That(archive.WallOutletSpawnRows, Is.Not.Empty);
            Assert.That(archive.StarterConfigRows, Is.Not.Empty);
        }

        [Test]
        public void RoomThreeUsesProductPoolWithoutRedistribution()
        {
            var archive = Resources.Load<GameBalanceArchive>(
                "Balance/GameBalanceArchive");
            var rule = archive.ProductRedistributionRows.Single(
                row => row.minRoomCount <= 3 && row.maxRoomCount >= 3);
            var eligiblePool = archive.ProductSpawnPoolRows
                .Where(row =>
                    row.enabled
                    && row.weight > 0
                    && row.minRoomCount <= 3)
                .ToList();

            Assert.That(rule.minExtraProducts, Is.Zero);
            Assert.That(rule.maxExtraProducts, Is.Zero);
            Assert.That(eligiblePool, Is.Not.Empty);
            Assert.That(
                eligiblePool.All(row => !string.IsNullOrWhiteSpace(row.productType)),
                Is.True);
        }

        [Test]
        public void RewardMapper_SkipsUnsupportedSheetEffects()
        {
            var rewards = RewardBalanceMapper.GetDefaultRewards();

            Assert.That(rewards, Is.Not.Empty);
            Assert.That(rewards, Has.None.Matches<RewardBalanceRecord>(
                reward => reward.Id == "RW007"));
            Assert.That(rewards.All(reward => reward.Effects.Count is >= 1 and <= 2), Is.True);
        }
    }
}
