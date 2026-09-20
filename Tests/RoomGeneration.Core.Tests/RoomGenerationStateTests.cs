using NUnit.Framework;
using Octoplug.RoomGeneration;

namespace Octoplug.RoomGeneration.Tests
{
    public sealed class RoomGenerationStateTests
    {
        private static readonly DoorPlanningOptions Options = new(2f, 1f);

        [Test]
        public void HintAndUnlockedStatesExposeExactVisualIntent()
        {
            var initial = RoomLayout.Create(new[] { Room("A", 0f, 0f) });
            var plan = Plan(initial, Candidate("B", 10f, 0f));
            var state = new RoomGenerationState(initial, plan);

            Assert.That(plan.State, Is.EqualTo(RoomLifecycleState.HintLocked));
            var locked = state.GetVisualIntent(new RoomId("B"));
            Assert.That(locked.State, Is.EqualTo(RoomLifecycleState.HintLocked));
            Assert.That(locked.WallStyle, Is.EqualTo(RoomWallStyle.Dashed));
            Assert.That(locked.WallColor.Hex, Is.EqualTo("#15786B"));
            Assert.That(locked.ShowLockIcon, Is.True);
            Assert.That(locked.ShowHatching, Is.True);
            Assert.That(locked.GameplayContentEnabled, Is.False);

            var unlocked = state.GetVisualIntent(new RoomId("A"));
            Assert.That(unlocked.State, Is.EqualTo(RoomLifecycleState.UnlockedGenerated));
            Assert.That(unlocked.WallStyle, Is.EqualTo(RoomWallStyle.Solid));
            Assert.That(unlocked.WallColor.Hex, Is.EqualTo("#87968E"));
            Assert.That(unlocked.ShowLockIcon, Is.False);
            Assert.That(unlocked.ShowHatching, Is.False);
            Assert.That(unlocked.GameplayContentEnabled, Is.True);
        }

        [Test]
        public void UnlockPromotesExactStoredPlanAndClearsHint()
        {
            var initial = RoomLayout.Create(new[] { Room("A", 0f, 0f) });
            var storedPlan = Plan(initial, Candidate("B", 10f, 0f));
            var before = new RoomGenerationState(initial, storedPlan);

            var after = before.UnlockNext();

            Assert.That(before.HasNextRoomPlan, Is.True);
            Assert.That(before.UnlockedLayout.Rooms, Has.Count.EqualTo(1));
            Assert.That(after.HasNextRoomPlan, Is.False);
            Assert.That(after.UnlockedLayout.Rooms, Has.Count.EqualTo(2));
            Assert.That(after.UnlockedLayout.Rooms[1], Is.EqualTo(storedPlan.Room));
            Assert.That(after.UnlockedLayout.Doors[0], Is.SameAs(storedPlan.DoorPlans[0]));
            Assert.That(after.GetRoomState(new RoomId("B")), Is.EqualTo(RoomLifecycleState.UnlockedGenerated));
        }

        [Test]
        public void FollowingHintIsPlannedAgainstExpandedLayoutAndStoredSeparately()
        {
            var initial = RoomLayout.Create(new[] { Room("A", 0f, 0f) });
            var firstPlan = Plan(initial, Candidate("B", 10f, 0f));
            var unlocked = new RoomGenerationState(initial, firstPlan).UnlockNext();

            var result = unlocked.PlanFollowing(
                new[] { Candidate("C", 20f, 0f) },
                Options,
                new SelectFirstMidpointPolicy());
            var withFollowingHint = unlocked.StoreNextPlan(result.Plan);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Plan.SharedWalls[0].AdjacentWall.RoomId, Is.EqualTo(new RoomId("B")));
            Assert.That(unlocked.HasNextRoomPlan, Is.False);
            Assert.That(withFollowingHint.NextRoomPlan, Is.SameAs(result.Plan));
            Assert.That(withFollowingHint.GetRoomState(new RoomId("C")), Is.EqualTo(RoomLifecycleState.HintLocked));
        }

        [Test]
        public void FailedFollowingPlanLeavesImmutableStateUnchanged()
        {
            var layout = RoomLayout.Create(new[] { Room("A", 0f, 0f) });
            var state = new RoomGenerationState(layout);

            var result = state.PlanFollowing(
                new[] { Candidate("Far", 100f, 100f) },
                Options,
                new SelectFirstMidpointPolicy());

            Assert.That(result.Success, Is.False);
            Assert.That(state.UnlockedLayout, Is.SameAs(layout));
            Assert.That(state.HasNextRoomPlan, Is.False);
        }

        [Test]
        public void UnknownLifecycleStateCannotEnableGameplayIntent()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                RoomVisualIntent.For((RoomLifecycleState)999));
        }

        [Test]
        public void PlanValidatedAgainstOldLayoutCannotBeStoredAfterLayoutAdvances()
        {
            var initial = RoomLayout.Create(new[] { Room("A", 0f, 0f) });
            var firstPlan = Plan(initial, Candidate("B", 10f, 0f));
            var stalePlan = Plan(initial, Candidate("C", 0f, 10f));
            var advanced = new RoomGenerationState(initial, firstPlan).UnlockNext();

            Assert.Throws<System.ArgumentException>(() => advanced.StoreNextPlan(stalePlan));
            Assert.That(advanced.HasNextRoomPlan, Is.False);
            Assert.That(advanced.UnlockedLayout.Rooms, Has.Count.EqualTo(2));
        }

        private static RoomPlan Plan(RoomLayout layout, RoomCandidate candidate)
            => RoomPlanner.PlanNext(layout, new[] { candidate }, Options, new SelectFirstMidpointPolicy()).Plan;

        private static RoomPlacement Room(string id, float x, float y)
            => new(new RoomId(id), new RoomBounds2D(x, y, 10f, 10f));

        private static RoomCandidate Candidate(string id, float x, float y)
            => new(new RoomId(id), new RoomBounds2D(x, y, 10f, 10f));
    }
}
