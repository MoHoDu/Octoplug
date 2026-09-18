using NUnit.Framework;
using Octoplug.CameraFraming;

namespace CameraFraming.Core.Tests
{
    public class PanGestureTrackerTests
    {
        [Test]
        public void CameraOwnershipSurvivesCrossingObjectUntilRelease()
        {
            var tracker = new PanGestureTracker();

            Assert.That(tracker.TryBegin(isGameplayBlocked: false), Is.True);
            Assert.That(tracker.TryBegin(isGameplayBlocked: true), Is.False);
            Assert.That(tracker.Ownership, Is.EqualTo(PanGestureOwnership.CameraPan));

            tracker.EndPointer();
            Assert.That(tracker.Ownership, Is.EqualTo(PanGestureOwnership.None));
        }

        [Test]
        public void GameplayOwnershipSurvivesCrossingIntoEmptyWorld()
        {
            var tracker = new PanGestureTracker();

            Assert.That(tracker.TryBegin(isGameplayBlocked: true), Is.True);
            Assert.That(tracker.TryBegin(isGameplayBlocked: false), Is.False);
            Assert.That(tracker.Ownership, Is.EqualTo(PanGestureOwnership.GameplayBlocked));
        }

        [Test]
        public void MultiTouchCancelsPanAndRequiresAllTouchesToEnd()
        {
            var tracker = new PanGestureTracker();
            tracker.TryBegin(isGameplayBlocked: false);

            tracker.SuppressForMultiTouch();
            tracker.EndPointer();

            Assert.That(tracker.Ownership, Is.EqualTo(PanGestureOwnership.MultiTouchSuppressed));
            Assert.That(tracker.TryBegin(isGameplayBlocked: false), Is.False);

            tracker.EndAllTouches();
            Assert.That(tracker.TryBegin(isGameplayBlocked: false), Is.True);
        }
    }
}
