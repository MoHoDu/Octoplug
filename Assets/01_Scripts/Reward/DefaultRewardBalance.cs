using System.Collections.Generic;

namespace Octoplug.Reward
{
    public static class DefaultRewardBalance
    {
        public static IReadOnlyList<RewardBalanceRecord> GetDefaultRewards()
        {
            return new[]
            {
                new RewardBalanceRecord(
                    id: "RW001",
                    enabled: true,
                    minRoomCount: 1,
                    weight: 10,
                    displayName: "집 전력 확장",
                    description: "집 전체의 허용 전력량을 1 증가시킵니다.",
                    targetType: RewardTargetType.None,
                    effects: new[] { new RewardEffect(RewardEffectType.HouseAllowedPower, 1) }),

                new RewardBalanceRecord(
                    id: "RW002",
                    enabled: true,
                    minRoomCount: 1,
                    weight: 10,
                    displayName: "멀티탭 출력 강화",
                    description: "선택한 멀티탭의 허용 전력량을 1 증가시킵니다.",
                    targetType: RewardTargetType.PowerStrip,
                    effects: new[] { new RewardEffect(RewardEffectType.PowerStripAllowedPower, 1) }),

                new RewardBalanceRecord(
                    id: "RW003",
                    enabled: true,
                    minRoomCount: 1,
                    weight: 10,
                    displayName: "소켓 확장 I",
                    description: "선택한 멀티탭의 소켓을 1개 추가합니다.",
                    targetType: RewardTargetType.PowerStrip,
                    effects: new[] { new RewardEffect(RewardEffectType.PowerStripSocketCount, 1) }),

                new RewardBalanceRecord(
                    id: "RW004",
                    enabled: true,
                    minRoomCount: 3,
                    weight: 6,
                    displayName: "소켓 확장 II",
                    description: "선택한 멀티탭의 소켓을 2개 추가합니다.",
                    targetType: RewardTargetType.PowerStrip,
                    effects: new[] { new RewardEffect(RewardEffectType.PowerStripSocketCount, 2) }),

                new RewardBalanceRecord(
                    id: "RW005",
                    enabled: true,
                    minRoomCount: 5,
                    weight: 3,
                    displayName: "소켓 확장 III",
                    description: "선택한 멀티탭의 소켓을 3개 추가합니다.",
                    targetType: RewardTargetType.PowerStrip,
                    effects: new[] { new RewardEffect(RewardEffectType.PowerStripSocketCount, 3) }),

                new RewardBalanceRecord(
                    id: "RW006",
                    enabled: true,
                    minRoomCount: 2,
                    weight: 8,
                    displayName: "케이블 연장",
                    description: "선택한 제품 또는 멀티탭의 케이블 길이를 1 연장합니다.",
                    targetType: RewardTargetType.CableOwner,
                    effects: new[] { new RewardEffect(RewardEffectType.CableLength, 1) }),

                new RewardBalanceRecord(
                    id: "RW008",
                    enabled: true,
                    minRoomCount: 4,
                    weight: 4,
                    displayName: "멀티탭 종합 강화",
                    description: "선택한 멀티탭의 허용 전력량과 소켓을 각각 1개씩 추가합니다.",
                    targetType: RewardTargetType.PowerStrip,
                    effects: new[]
                    {
                        new RewardEffect(RewardEffectType.PowerStripAllowedPower, 1),
                        new RewardEffect(RewardEffectType.PowerStripSocketCount, 1)
                    }),

                new RewardBalanceRecord(
                    id: "RW009",
                    enabled: true,
                    minRoomCount: 1,
                    weight: 6,
                    displayName: "멀티탭 추가",
                    description: "선택한 방에 새 멀티탭을 추가합니다.",
                    targetType: RewardTargetType.RoomPlacement,
                    effects: new[] { new RewardEffect(RewardEffectType.AddPowerStrip, 1) })
            };
        }
    }
}
