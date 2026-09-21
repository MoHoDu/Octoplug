using System;

namespace Octoplug.ResidentDemand
{
    public sealed class SessionProgressState
    {
        private readonly RequiredExperienceTable _requiredExperience;
        private readonly System.Collections.Generic.HashSet<DemandOutcome> _appliedOutcomes = new();
        private bool _experienceThresholdPublished;
        private bool _satisfactionDepleted;

        public SessionProgressState(
            int initialGlobalSatisfaction,
            int initialExperience,
            int roomCount,
            RequiredExperienceTable requiredExperience)
        {
            if (initialGlobalSatisfaction < 0 || initialGlobalSatisfaction > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(initialGlobalSatisfaction));
            }

            if (initialExperience < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialExperience));
            }

            if (roomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roomCount));
            }

            _requiredExperience = requiredExperience ?? throw new ArgumentNullException(nameof(requiredExperience));
            GlobalSatisfaction = initialGlobalSatisfaction;
            Experience = initialExperience;
            RoomCount = roomCount;
            _satisfactionDepleted = initialGlobalSatisfaction == 0;
            _experienceThresholdPublished = initialExperience >= RequiredExperience;
        }

        public event Action SatisfactionDepleted;
        public event Action ExperienceThresholdReached;

        public int GlobalSatisfaction { get; private set; }
        public int Experience { get; private set; }
        public int RoomCount { get; private set; }
        public int SolvedDemandCount { get; private set; }
        public int FailedDemandCount { get; private set; }
        public int RequiredExperience => _requiredExperience.GetRequiredExperience(RoomCount);
        public bool IsSatisfactionDepleted => _satisfactionDepleted;
        public float SatisfactionNormalized => GlobalSatisfaction / 100f;
        public float ExperienceNormalized => Math.Min(1f, Experience / (float)RequiredExperience);

        public void Apply(DemandOutcome outcome)
        {
            if (outcome == null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            if (_satisfactionDepleted || !_appliedOutcomes.Add(outcome))
            {
                return;
            }

            checked
            {
                switch (outcome.Resolution)
                {
                    case DemandResolution.Success:
                        SolvedDemandCount++;
                        Experience += outcome.ExperienceReward;
                        break;
                    case DemandResolution.Failure:
                        FailedDemandCount++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(outcome),
                            outcome.Resolution,
                            "Unsupported Demand resolution.");
                }
            }

            GlobalSatisfaction = Clamp(GlobalSatisfaction + outcome.GlobalSatisfactionDelta, 0, 100);

            if (GlobalSatisfaction == 0 && !_satisfactionDepleted)
            {
                _satisfactionDepleted = true;
                SatisfactionDepleted?.Invoke();
            }

            if (Experience >= RequiredExperience && !_experienceThresholdPublished)
            {
                _experienceThresholdPublished = true;
                ExperienceThresholdReached?.Invoke();
            }
        }

        public void AcknowledgeExperienceThreshold(int newRoomCount, bool resetExperience)
        {
            if (newRoomCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(newRoomCount));
            }

            _requiredExperience.GetRequiredExperience(newRoomCount);
            RoomCount = newRoomCount;
            if (resetExperience)
            {
                Experience = 0;
            }

            _experienceThresholdPublished = Experience >= RequiredExperience;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum ? minimum : value > maximum ? maximum : value;
        }
    }
}
