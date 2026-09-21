using NUnit.Framework;
using Octoplug.ResidentDemand;

namespace Octoplug.Tests.Editor.ResidentDemand
{
    public sealed class SessionResultCounterTests
    {
        [Test]
        public void State_CountsAcceptedFinalOutcomesExactlyOnce()
        {
            var state = new SessionProgressState(100, 0, 1, RequiredExperience());
            var success = Outcome(DemandResolution.Success);
            var failure = Outcome(DemandResolution.Failure);

            state.Apply(success);
            state.Apply(success);
            state.Apply(failure);
            state.Apply(failure);

            Assert.That(state.SolvedDemandCount, Is.EqualTo(1));
            Assert.That(state.FailedDemandCount, Is.EqualTo(1));
        }

        [Test]
        public void State_CountsFinalFailureBeforeDepletionSignal()
        {
            var state = new SessionProgressState(8, 0, 1, RequiredExperience());
            var failedCountAtDepletion = -1;
            state.SatisfactionDepleted += () =>
                failedCountAtDepletion = state.FailedDemandCount;

            state.Apply(Outcome(DemandResolution.Failure));

            Assert.That(state.GlobalSatisfaction, Is.Zero);
            Assert.That(failedCountAtDepletion, Is.EqualTo(1));
            Assert.That(state.FailedDemandCount, Is.EqualTo(1));
        }

        [Test]
        public void State_NewSessionResetsDemandCountsAndProgress()
        {
            var first = new SessionProgressState(8, 15, 1, RequiredExperience());
            first.Apply(Outcome(DemandResolution.Success));
            first.Apply(Outcome(DemandResolution.Failure));

            var next = new SessionProgressState(100, 0, 1, RequiredExperience());

            Assert.That(next.GlobalSatisfaction, Is.EqualTo(100));
            Assert.That(next.Experience, Is.Zero);
            Assert.That(next.RequiredExperience, Is.EqualTo(20));
            Assert.That(next.RoomCount, Is.EqualTo(1));
            Assert.That(next.SolvedDemandCount, Is.Zero);
            Assert.That(next.FailedDemandCount, Is.Zero);
        }

        [Test]
        public void TwoNeedDemand_ProducesNoOutcomeUntilWholeDemandSucceeds()
        {
            var resident = new ResidentDemandState(new ResidentNumber(1));
            resident.StartDemand(new DemandBalanceRecord(
                "TEST-TWO-NEED",
                true,
                1,
                1,
                new[] { ResidentNeedType.Cooling, ResidentNeedType.Heating },
                2f,
                10f,
                10,
                5,
                -8,
                0f));
            var state = new SessionProgressState(100, 0, 1, RequiredExperience());

            resident.SetUsing(true);
            var firstNeedOutcome = resident.Advance(2f);

            Assert.That(firstNeedOutcome, Is.Null);
            Assert.That(resident.CompletedNeedCount, Is.EqualTo(1));
            Assert.That(state.SolvedDemandCount, Is.Zero);

            resident.SetUsing(true);
            var finalOutcome = resident.Advance(2f);
            state.Apply(finalOutcome);

            Assert.That(finalOutcome.Resolution, Is.EqualTo(DemandResolution.Success));
            Assert.That(state.SolvedDemandCount, Is.EqualTo(1));
            Assert.That(state.FailedDemandCount, Is.Zero);
        }

        private static RequiredExperienceTable RequiredExperience()
        {
            return new RequiredExperienceTable(new[]
            {
                new RequiredExperienceEntry(1, 20),
                new RequiredExperienceEntry(2, 30),
            });
        }

        private static DemandOutcome Outcome(DemandResolution resolution)
        {
            return new DemandOutcome(
                new DemandBalanceRecord(
                    "TEST",
                    true,
                    1,
                    1,
                    new[] { ResidentNeedType.Fun },
                    1f,
                    1f,
                    10,
                    5,
                    -8,
                    0f),
                resolution);
        }
    }
}
