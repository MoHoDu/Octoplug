using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.Reward;
using Octoplug.Reward.Unity;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;
using UnityEngine.TestTools;

namespace Octoplug.Tests.Reward
{
    public class MockRewardContext : IRewardContext
    {
        public int CurrentRoomCount { get; set; } = 1;
        public float CurrentHouseAllowedPower { get; set; } = 10f;
        public float MaxHouseAllowedPower { get; set; } = 22f;
        public List<PowerStrip> PowerStrips { get; } = new();
        public List<CableOwnerRewardTarget> CableOwners { get; } = new();
        public List<RoomPlacementRewardTarget> RoomPlacements { get; } = new();

        public IEnumerable<PowerStrip> GetPowerStrips() => PowerStrips;
        public IEnumerable<CableOwnerRewardTarget> GetCableOwners() => CableOwners;
        public IEnumerable<RoomPlacementRewardTarget> GetRoomPlacements() => RoomPlacements;
    }

    [TestFixture]
    public class RewardCandidateGeneratorTests
    {
        private MockRewardContext context;

        [SetUp]
        public void Setup()
        {
            context = new MockRewardContext();
        }

        [Test]
        public void GenerateCandidates_ReturnsThreeUniqueApplicableRecords()
        {
            var pool = new[]
            {
                CreateHouseReward("A", 1),
                CreateHouseReward("B", 2),
                CreateHouseReward("C", 3),
                CreateHouseReward("D", 20)
            };

            var candidates = RewardCandidateGenerator.GenerateCandidates(
                pool,
                context,
                3,
                new System.Random(42));

            Assert.That(candidates, Has.Count.EqualTo(3));
            Assert.That(new HashSet<RewardBalanceRecord>(candidates), Has.Count.EqualTo(3));
            Assert.That(candidates, Has.None.Matches<RewardBalanceRecord>(reward => reward.Id == "D"));
        }

        [Test]
        public void CandidateFiltering_HousePowerMax_ExcludesRW001()
        {
            context.CurrentHouseAllowedPower = 22f;
            context.MaxHouseAllowedPower = 22f;
            context.CurrentRoomCount = 10;

            var candidates = RewardCandidateGenerator.GenerateCandidates(
                DefaultRewardBalance.GetDefaultRewards(),
                context,
                10,
                new System.Random(42));

            Assert.That(candidates, Has.None.Matches<RewardBalanceRecord>(reward => reward.Id == "RW001"));
        }

        [Test]
        public void DefaultRewards_ContainExactRemainingRewardIds_AndExcludeRW007()
        {
            var rewards = DefaultRewardBalance.GetDefaultRewards();

            Assert.That(rewards.Count, Is.EqualTo(8));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW001"));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW002"));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW003"));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW004"));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW005"));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW006"));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW008"));
            Assert.That(
                rewards,
                Has.Exactly(1).Matches<RewardBalanceRecord>(reward => reward.Id == "RW009"));
            Assert.That(
                rewards,
                Has.None.Matches<RewardBalanceRecord>(reward => reward.Id == "RW007"));
        }

        [Test]
        public void CandidateFiltering_NoConcreteTargets_ExcludesRW006AndRW009()
        {
            context.CurrentRoomCount = 10;

            var candidates = RewardCandidateGenerator.GenerateCandidates(
                DefaultRewardBalance.GetDefaultRewards(),
                context,
                10,
                new System.Random(42));

            Assert.That(candidates, Has.None.Matches<RewardBalanceRecord>(reward => reward.Id == "RW006"));
            Assert.That(candidates, Has.None.Matches<RewardBalanceRecord>(reward => reward.Id == "RW009"));
        }

        [Test]
        public void CandidateFiltering_ConcreteCableOwnersAndRooms_EnableRW006AndRW009()
        {
            context.CurrentRoomCount = 10;
            var cableObject = new GameObject("CableOwner");
            var binderObject = new GameObject("RoomBinder");
            try
            {
                var cable = cableObject.AddComponent<CableInfo>();
                context.CableOwners.Add(new CableOwnerRewardTarget(cable, cable));
                var room = new RoomPlacement(
                    new RoomId("reward-room"),
                    new RoomBounds2D(0f, 0f, 4f, 4f));
                LogAssert.Expect(
                    LogType.Error,
                    "RoomGenerationRoomBinder on 'RoomBinder' is invalid: Floor collider is not assigned.");
                var binder = binderObject.AddComponent<RoomGenerationRoomBinder>();
                context.RoomPlacements.Add(new RoomPlacementRewardTarget(room, binder));

                var candidates = RewardCandidateGenerator.GenerateCandidates(
                    DefaultRewardBalance.GetDefaultRewards(),
                    context,
                    20,
                    new System.Random(42));

                Assert.That(candidates, Has.Some.Matches<RewardBalanceRecord>(reward => reward.Id == "RW006"));
                Assert.That(candidates, Has.Some.Matches<RewardBalanceRecord>(reward => reward.Id == "RW009"));
            }
            finally
            {
                Object.DestroyImmediate(cableObject);
                Object.DestroyImmediate(binderObject);
            }
        }

        private static RewardBalanceRecord CreateHouseReward(string id, int value)
        {
            return new RewardBalanceRecord(
                id,
                true,
                1,
                1,
                $"Reward {id}",
                $"Description {id}",
                RewardTargetType.None,
                new[] { new RewardEffect(RewardEffectType.HouseAllowedPower, value) });
        }
    }
}
