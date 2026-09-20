namespace Octoplug.ResidentDemand
{
    public sealed class DemandAuthoringRow
    {
        public DemandAuthoringRow(
            string id,
            bool enabled,
            int requiredRoomCount,
            int weight,
            string firstNeed,
            string secondNeed,
            float satisfactionFillSeconds,
            float patienceFillSeconds,
            int experienceReward,
            int globalSatisfactionOnSuccess,
            int globalSatisfactionOnFailure,
            float cooldownSeconds)
        {
            Id = id;
            Enabled = enabled;
            RequiredRoomCount = requiredRoomCount;
            Weight = weight;
            FirstNeed = firstNeed;
            SecondNeed = secondNeed;
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
        public string FirstNeed { get; }
        public string SecondNeed { get; }
        public float SatisfactionFillSeconds { get; }
        public float PatienceFillSeconds { get; }
        public int ExperienceReward { get; }
        public int GlobalSatisfactionOnSuccess { get; }
        public int GlobalSatisfactionOnFailure { get; }
        public float CooldownSeconds { get; }
    }
}
