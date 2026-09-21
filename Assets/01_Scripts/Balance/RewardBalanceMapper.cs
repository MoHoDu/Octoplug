using System;
using System.Collections.Generic;
using Octoplug.Balance;
using UnityEngine;

namespace Octoplug.Reward
{
    public static class RewardBalanceMapper
    {
        public static IReadOnlyList<RewardBalanceRecord> GetDefaultRewards()
        {
            var list = new List<RewardBalanceRecord>();
            var archive = BalanceRegistry.Instance;
            if (archive == null)
            {
                return list;
            }

            foreach (var row in archive.RewardRows)
            {
                var targetType = Enum.TryParse<RewardTargetType>(
                    row.targetType,
                    true,
                    out var parsedTargetType)
                    ? parsedTargetType
                    : RewardTargetType.None;

                var effects = new List<RewardEffect>(2);
                if (!TryAddEffect(effects, row.effect1Type, row.effect1Value)
                    || !TryAddEffect(effects, row.effect2Type, row.effect2Value)
                    || effects.Count == 0)
                {
                    Debug.LogWarning(
                        $"[Balance] Reward '{row.rewardId}' was skipped because its effects are not supported by the current runtime.");
                    continue;
                }

                list.Add(new RewardBalanceRecord(
                    row.rewardId,
                    row.enabled,
                    row.minRoomCount,
                    row.weight,
                    row.displayName,
                    row.description,
                    targetType,
                    effects));
            }

            return list;
        }

        private static bool TryAddEffect(
            ICollection<RewardEffect> effects,
            string effectType,
            int effectValue)
        {
            if (string.IsNullOrWhiteSpace(effectType))
            {
                return true;
            }

            if (!Enum.TryParse<RewardEffectType>(effectType, true, out var parsedType)
                || effectValue <= 0)
            {
                return false;
            }

            effects.Add(new RewardEffect(parsedType, effectValue));
            return true;
        }
    }
}
