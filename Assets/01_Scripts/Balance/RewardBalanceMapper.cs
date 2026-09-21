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
                    
                    var effects = new List<RewardEffect>();
                    if (!string.IsNullOrWhiteSpace(row.effect1Type) && System.Enum.TryParse<RewardEffectType>(row.effect1Type, true, out var e1Type))
                    {
                        effects.Add(new RewardEffect(e1Type, row.effect1Value));
                    }
                    if (!string.IsNullOrWhiteSpace(row.effect2Type) && System.Enum.TryParse<RewardEffectType>(row.effect2Type, true, out var e2Type))
                    {
                        effects.Add(new RewardEffect(e2Type, row.effect2Value));
                    }

                    list.Add(new RewardBalanceRecord(
                        row.rewardId,
                        row.enabled,
                        row.minRoomCount,
                        row.weight,
                        row.displayName,
                        row.description,
                        targetType,
                        effects
                    ));
                }
            }
            return list;
        }
    }
}
