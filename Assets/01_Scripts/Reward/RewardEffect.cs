using System;

namespace Octoplug.Reward
{
    public readonly struct RewardEffect
    {
        public RewardEffectType EffectType { get; }
        public int Value { get; }

        public RewardEffect(RewardEffectType effectType, int value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Reward effect value must be positive.");
            }
            EffectType = effectType;
            Value = value;
        }
    }
}
