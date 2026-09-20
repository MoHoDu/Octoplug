using System;
using System.Collections.Generic;

namespace Octoplug.ResidentDemand
{
    public sealed class DemandBalanceRecord
    {
        private readonly ResidentNeedType[] _needs;

        public DemandBalanceRecord(
            string id,
            bool enabled,
            int requiredRoomCount,
            int weight,
            IReadOnlyList<ResidentNeedType> needs,
            float satisfactionFillSeconds,
            float patienceFillSeconds,
            int experienceReward,
            int globalSatisfactionOnSuccess,
            int globalSatisfactionOnFailure,
            float cooldownSeconds)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Demand id is required.", nameof(id));
            }

            if (requiredRoomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredRoomCount));
            }

            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            if (needs == null || needs.Count < 1 || needs.Count > 2)
            {
                throw new ArgumentException("A demand must contain one or two needs.", nameof(needs));
            }

            if (needs.Count == 2 && needs[0] == needs[1])
            {
                throw new ArgumentException("A two-need demand must contain distinct needs.", nameof(needs));
            }

            RequirePositive(satisfactionFillSeconds, nameof(satisfactionFillSeconds));
            RequirePositive(patienceFillSeconds, nameof(patienceFillSeconds));
            RequireNonNegative(experienceReward, nameof(experienceReward));
            RequireNonNegative(globalSatisfactionOnSuccess, nameof(globalSatisfactionOnSuccess));
            if (globalSatisfactionOnFailure > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(globalSatisfactionOnFailure));
            }

            RequireNonNegative(cooldownSeconds, nameof(cooldownSeconds));

            Id = id;
            Enabled = enabled;
            RequiredRoomCount = requiredRoomCount;
            Weight = weight;
            _needs = new ResidentNeedType[needs.Count];
            for (var index = 0; index < needs.Count; index++)
            {
                _needs[index] = needs[index];
            }

            SatisfactionFillSeconds = satisfactionFillSeconds;
            PatienceFillSeconds = patienceFillSeconds;
            ExperienceReward = experienceReward;
            GlobalSatisfactionOnSuccess = globalSatisfactionOnSuccess;
            GlobalSatisfactionOnFailure = globalSatisfactionOnFailure;
            CooldownSeconds = cooldownSeconds;
        }

        public string Id { get; }
        public bool Enabled { get; }
        public int RequiredRoomCount { get; }
        public int Weight { get; }
        public IReadOnlyList<ResidentNeedType> Needs => _needs;
        public float SatisfactionFillSeconds { get; }
        public float PatienceFillSeconds { get; }
        public int ExperienceReward { get; }
        public int GlobalSatisfactionOnSuccess { get; }
        public int GlobalSatisfactionOnFailure { get; }
        public float CooldownSeconds { get; }

        private static void RequirePositive(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void RequireNonNegative(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }
}
