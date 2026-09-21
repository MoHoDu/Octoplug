using System;
using System.Collections.Generic;
using System.Linq;
using Octoplug.Power;
using Octoplug.Reward.Unity;

namespace Octoplug.Reward
{
    public interface IRewardContext
    {
        int CurrentRoomCount { get; }
        float CurrentHouseAllowedPower { get; }
        float MaxHouseAllowedPower { get; }
        IEnumerable<PowerStrip> GetPowerStrips();
        IEnumerable<CableOwnerRewardTarget> GetCableOwners();
        IEnumerable<RoomPlacementRewardTarget> GetRoomPlacements();
    }

    public static class RewardCandidateGenerator
    {
        public static List<RewardBalanceRecord> GenerateCandidates(
            IReadOnlyList<RewardBalanceRecord> pool,
            IRewardContext context,
            int count,
            System.Random random)
        {
            var validCandidates = new List<RewardBalanceRecord>();
            foreach (var reward in pool)
            {
                if (IsRewardApplicable(reward, context))
                {
                    validCandidates.Add(reward);
                }
            }

            var result = new List<RewardBalanceRecord>();
            for (var i = 0; i < count && validCandidates.Count > 0; i++)
            {
                var totalWeight = validCandidates.Sum(reward => reward.Weight);
                var roll = random.Next(totalWeight);
                var currentWeight = 0;
                for (var index = 0; index < validCandidates.Count; index++)
                {
                    currentWeight += validCandidates[index].Weight;
                    if (roll < currentWeight)
                    {
                        result.Add(validCandidates[index]);
                        validCandidates.RemoveAt(index);
                        break;
                    }
                }
            }

            return result;
        }

        public static bool IsRewardApplicable(
            RewardBalanceRecord reward,
            IRewardContext context)
        {
            if (!reward.Enabled || context.CurrentRoomCount < reward.MinRoomCount)
            {
                return false;
            }

            switch (reward.TargetType)
            {
                case RewardTargetType.None:
                    return AreEffectsApplicable(reward.Effects, context, null);
                case RewardTargetType.PowerStrip:
                    return context.GetPowerStrips().Any(strip =>
                        AreEffectsApplicable(reward.Effects, context, strip));
                case RewardTargetType.CableOwner:
                    return context.GetCableOwners().Any(target =>
                        AreEffectsApplicable(reward.Effects, context, target));
                case RewardTargetType.RoomPlacement:
                    return context.GetRoomPlacements().Any(target =>
                        AreEffectsApplicable(reward.Effects, context, target));
                default:
                    return false;
            }
        }

        public static bool AreEffectsApplicable(
            IReadOnlyList<RewardEffect> effects,
            IRewardContext context,
            object target)
        {
            for (var index = 0; index < effects.Count; index++)
            {
                if (!IsEffectApplicable(effects[index], context, target))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsEffectApplicable(
            RewardEffect effect,
            IRewardContext context,
            object target)
        {
            switch (effect.EffectType)
            {
                case RewardEffectType.HouseAllowedPower:
                    return target == null
                        && context.CurrentHouseAllowedPower + effect.Value
                            <= context.MaxHouseAllowedPower;
                case RewardEffectType.PowerStripAllowedPower:
                    return target is PowerStrip powerStrip
                        && effect.Value == 1
                        && powerStrip.CanUpgradeAllowedPowerWatts(out _);
                case RewardEffectType.PowerStripSocketCount:
                    return target is PowerStrip socketStrip
                        && effect.Value > 0
                        && socketStrip.CanUpgradeActiveSocketCount(effect.Value, out _);
                case RewardEffectType.CableLength:
                    return target is CableOwnerRewardTarget cableOwner
                        && effect.Value == 1
                        && cableOwner.Cable != null
                        && cableOwner.Cable.CanUpgradeCableLength();
                case RewardEffectType.AddPowerStrip:
                    return target is RoomPlacementRewardTarget && effect.Value == 1;
                default:
                    return false;
            }
        }
    }
}
