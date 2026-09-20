using NUnit.Framework;

namespace Octoplug.ResidentDemand.Tests
{
    public sealed class ResidentDemandStateTests
    {
        [Test]
        public void ResidentNumbers_AreSequentialAndAtLeastTwoDigits()
        {
            var sequence = new ResidentNumberSequence();

            Assert.That(sequence.Next().DisplayValue, Is.EqualTo("01"));
            Assert.That(sequence.Next().DisplayValue, Is.EqualTo("02"));
        }

        [Test]
        public void Using_IncreasesSatisfactionAndDecreasesPatience()
        {
            var state = CreateState("D001");
            state.Advance(5f);
            state.SetUsing(true);

            state.Advance(3f);

            Assert.That(state.SatisfactionProgress, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(state.PatienceProgress, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void Waiting_DecreasesSatisfactionAndIncreasesPatience()
        {
            var state = CreateState("D001");
            state.SetUsing(true);
            state.Advance(3f);
            state.SetUsing(false);

            state.Advance(1f);

            Assert.That(state.SatisfactionProgress, Is.EqualTo(1f / 3f).Within(0.0001f));
            Assert.That(state.PatienceProgress, Is.EqualTo(0.1f).Within(0.0001f));
        }

        [Test]
        public void SingleNeed_SuccessReturnsOutcomeAndEntersCooldown()
        {
            var state = CreateState("D001");
            state.SetUsing(true);

            var outcome = state.Advance(6f);

            Assert.That(outcome.Resolution, Is.EqualTo(DemandResolution.Success));
            Assert.That(outcome.ExperienceReward, Is.EqualTo(10));
            Assert.That(outcome.GlobalSatisfactionDelta, Is.EqualTo(5));
            Assert.That(state.Status, Is.EqualTo(ResidentDemandStatus.Cooldown));
            Assert.That(state.Demand, Is.Null);
        }

        [Test]
        public void SingleNeed_PatienceFailureAwardsNoExperience()
        {
            var state = CreateState("D001");

            var outcome = state.Advance(10f);

            Assert.That(outcome.Resolution, Is.EqualTo(DemandResolution.Failure));
            Assert.That(outcome.ExperienceReward, Is.Zero);
            Assert.That(outcome.GlobalSatisfactionDelta, Is.EqualTo(-8));
        }

        [Test]
        public void TwoNeed_FirstCompletionCreatesHalfFloorAndWaitsForNextAssignment()
        {
            var state = CreateState("D005");
            state.SetUsing(true);

            var outcome = state.Advance(6f);

            Assert.That(outcome, Is.Null);
            Assert.That(state.CompletedNeedCount, Is.EqualTo(1));
            Assert.That(state.ActiveNeedIndex, Is.EqualTo(1));
            Assert.That(state.Status, Is.EqualTo(ResidentDemandStatus.Waiting));
            Assert.That(state.SatisfactionProgress, Is.EqualTo(0.5f).Within(0.0001f));

            state.Advance(2f);

            Assert.That(state.SatisfactionProgress, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void TwoNeed_BothCompletedResolveWholeDemandOnce()
        {
            var state = CreateState("D005");
            state.SetUsing(true);
            Assert.That(state.Advance(6f), Is.Null);
            state.SetUsing(true);

            var outcome = state.Advance(6f);

            Assert.That(outcome.Resolution, Is.EqualTo(DemandResolution.Success));
            Assert.That(state.Advance(1f), Is.Null);
            Assert.That(state.LastOutcome, Is.SameAs(outcome));
        }

        [Test]
        public void Cooldown_ClearsToNoneAfterConfiguredDuration()
        {
            var state = CreateState("D001");
            state.SetUsing(true);
            state.Advance(6f);

            state.Advance(3.9f);
            Assert.That(state.Status, Is.EqualTo(ResidentDemandStatus.Cooldown));
            state.Advance(0.1f);

            Assert.That(state.Status, Is.EqualTo(ResidentDemandStatus.None));
            Assert.That(state.CooldownRemaining, Is.Zero);
        }

        private static ResidentDemandState CreateState(string demandId)
        {
            DemandBalanceRecord record = null;
            foreach (var candidate in DefaultDemandBalance.Create().Records)
            {
                if (candidate.Id == demandId)
                {
                    record = candidate;
                    break;
                }
            }

            var state = new ResidentDemandState(new ResidentNumber(1));
            state.StartDemand(record);
            return state;
        }
    }
}
