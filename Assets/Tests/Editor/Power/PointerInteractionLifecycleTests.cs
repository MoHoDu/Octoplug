using NUnit.Framework;
using Octoplug.GameFlow;
using Octoplug.Power.Input;

namespace Octoplug.Tests.Editor.Power
{
    public sealed class PointerInteractionLifecycleTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayInputLock.IsLocked = false;
            GameplayInputLock.AllowCameraMovementDuringLock = false;
            GameplayInputLock.SuppressUntilPointerRelease = false;
            PointerInteractionResolver.ReleasePointer();
        }

        [TearDown]
        public void TearDown()
        {
            GameplayInputLock.IsLocked = false;
            GameplayInputLock.AllowCameraMovementDuringLock = false;
            GameplayInputLock.SuppressUntilPointerRelease = false;
            PointerInteractionResolver.ReleasePointer();
        }

        [Test]
        public void EndPointerGesture_ClearsCaptureAndSuppressionForNextGesture()
        {
            GameplayInputLock.SuppressUntilPointerRelease = true;

            PointerInteractionResolver.EndPointerGesture();

            Assert.That(GameplayInputLock.SuppressUntilPointerRelease, Is.False);
            Assert.That(PointerInteractionResolver.IsEmptyCapture, Is.False);
            Assert.That(PointerInteractionResolver.CapturedProduct, Is.Null);
        }
    }
}
