using System.Collections.Generic;
using Octoplug.Balance;

namespace Octoplug.Reward
{
    public static class RewardBalanceMapper
    {
        public static IReadOnlyList<RewardBalanceRecord> GetDefaultRewards()
        {
            var list = new List<RewardBalanceRecord>();
            var archive = BalanceRegistry.Instance;
            if (archive != null)
            {
                foreach (var row in archive.RewardRows)
                {
                    RewardTargetType targetType = System.Enum.TryParse<RewardTargetType>(row.targetType, true, out var tType) ? tType : RewardTargetType.None;
                    RewardEffectType effectType = System.Enum.TryParse<RewardEffectType>(row.effectType, true, out var eType) ? eType : RewardEffectType.HouseAllowedPower;
                    
                    list.Add(new RewardBalanceRecord(
                        row.rewardId,
                        targetType,
                        row.weight,
                        row.minRoomCount,
                        new[] { new RewardEffect(effectType, row.effectValue) }
                    ));
                }
            }
            return list;
        }
    }
}
