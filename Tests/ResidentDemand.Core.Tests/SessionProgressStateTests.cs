using NUnit.Framework;

namespace Octoplug.ResidentDemand.Tests
{
    public sealed class SessionProgressStateTests
    {
        [Test]
        public void Success_AppliesSatisfactionAndExperienceOncePerCall()
        {
            var state = CreateProgress(50, 0);

            state.Apply(Outcome("D001", DemandResolution.Success));

            Assert.That(state.GlobalSatisfaction, Is.EqualTo(55));
            Assert.That(state.Experience, Is.EqualTo(10));
            Assert.That(state.SatisfactionNormalized, Is.EqualTo(0.55f).Within(0.0001f));
        }

        [Test]
        public void SameOutcome_IsAppliedOnlyOnce()
        {
            var state = CreateProgress(50, 0);
            var outcome = Outcome("D001", DemandResolution.Success);

            state.Apply(outcome);
            state.Apply(outcome);

            Assert.That(state.GlobalSatisfaction, Is.EqualTo(55));
            Assert.That(state.Experience, Is.EqualTo(10));
        }

        [Test]
        public void Failure_AppliesSatisfactionButNoExperience()
        {
            var state = CreateProgress(50, 5);

            state.Apply(Outcome("D001", DemandResolution.Failure));

            Assert.That(state.GlobalSatisfaction, Is.EqualTo(42));
            Assert.That(state.Experience, Is.EqualTo(5));
        }

        [Test]
        public void SatisfactionDepleted_FiresOnceAndStopsFurtherProgression()
        {
            var state = CreateProgress(8, 0);
            var calls = 0;
            state.SatisfactionDepleted += () => calls++;

            state.Apply(Outcome("D001", DemandResolution.Failure));
            state.Apply(Outcome("D001", DemandResolution.Success));

            Assert.That(state.GlobalSatisfaction, Is.Zero);
            Assert.That(state.Experience, Is.Zero);
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void ExperienceThresholdReached_FiresOnceUntilAcknowledged()
        {
            var state = CreateProgress(50, 15);
            var calls = 0;
            state.ExperienceThresholdReached += () => calls++;

            state.Apply(Outcome("D001", DemandResolution.Success));
            state.Apply(Outcome("D001", DemandResolution.Success));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(state.ExperienceNormalized, Is.EqualTo(1f));
        }

        [Test]
        public void Satisfaction_IsClampedToAuthoritativeRange()
        {
            var upper = CreateProgress(98, 0);
            var lower = CreateProgress(3, 0);

            upper.Apply(Outcome("D001", DemandResolution.Success));
            lower.Apply(Outcome("D001", DemandResolution.Failure));

            Assert.That(upper.GlobalSatisfaction, Is.EqualTo(100));
            Assert.That(lower.GlobalSatisfaction, Is.Zero);
            Assert.That(upper.SatisfactionNormalized, Is.EqualTo(1f));
            Assert.That(lower.SatisfactionNormalized, Is.Zero);
        }

        [Test]
        public void AcknowledgeThreshold_UsesExplicitNextRoomAndCanResetExperience()
        {
            var state = CreateProgress(50, 15);
            var calls = 0;
            state.ExperienceThresholdReached += () => calls++;
            state.Apply(Outcome("D001", DemandResolution.Success));

            state.AcknowledgeExperienceThreshold(2, true);

            Assert.That(state.RoomCount, Is.EqualTo(2));
            Assert.That(state.RequiredExperience, Is.EqualTo(30));
            Assert.That(state.Experience, Is.Zero);
            state.Apply(Outcome("D001", DemandResolution.Success));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void AcknowledgeThreshold_WithoutResetPublishesWhenNextThresholdIsCrossed()
        {
            var state = CreateProgress(50, 25);
            var calls = 0;
            state.ExperienceThresholdReached += () => calls++;

            state.AcknowledgeExperienceThreshold(2, false);
            state.Apply(Outcome("D001", DemandResolution.Success));
            state.Apply(Outcome("D001", DemandResolution.Success));

            Assert.That(state.Experience, Is.EqualTo(45));
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void MissingRoomThreshold_IsRejectedInsteadOfUsingFormula()
        {
            var table = new RequiredExperienceTable(new[] { new RequiredExperienceEntry(1, 20) });

            Assert.Throws<System.InvalidOperationException>(() => table.GetRequiredExperience(2));
        }

        private static SessionProgressState CreateProgress(int satisfaction, int experience)
        {
            return new SessionProgressState(
                satisfaction,
                experience,
                1,
                new RequiredExperienceTable(new[]
                {
                    new RequiredExperienceEntry(1, 20),
                    new RequiredExperienceEntry(2, 30),
                }));
        }

        private static DemandOutcome Outcome(string id, DemandResolution resolution)
        {
            foreach (var demand in DefaultDemandBalance.Create().Records)
            {
                if (demand.Id == id)
                {
                    return new DemandOutcome(demand, resolution);
                }
            }

            throw new AssertionException("Demand not found.");
        }
    }
}
