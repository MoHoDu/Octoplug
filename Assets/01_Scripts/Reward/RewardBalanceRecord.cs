using System;
using System.Collections.Generic;

namespace Octoplug.Reward
{
    public sealed class RewardBalanceRecord
    {
        private readonly RewardEffect[] _effects;

        public RewardBalanceRecord(
            string id,
            bool enabled,
            int minRoomCount,
            int weight,
            string displayName,
            string description,
            RewardTargetType targetType,
            IReadOnlyList<RewardEffect> effects)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Reward id is required.", nameof(id));
            }

            if (minRoomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minRoomCount));
            }

            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            if (effects == null || effects.Count == 0 || effects.Count > 2)
            {
                throw new ArgumentException("A reward must contain one or two effects.", nameof(effects));
            }

            Id = id;
            Enabled = enabled;
            MinRoomCount = minRoomCount;
            Weight = weight;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            TargetType = targetType;

            _effects = new RewardEffect[effects.Count];
            for (var index = 0; index < effects.Count; index++)
            {
                _effects[index] = effects[index];
            }
        }

        public string Id { get; }
        public bool Enabled { get; }
        public int MinRoomCount { get; }
        public int Weight { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public RewardTargetType TargetType { get; }
        public IReadOnlyList<RewardEffect> Effects => _effects;
    }
}
