using NUnit.Framework;
using Octoplug.CameraFraming;
using Octoplug.RoomGeneration;

namespace CameraFraming.Core.Tests
{
    public class UsableViewportTests
    {
        [Test]
        public void DefaultViewport_IsFullViewAndMatchesExistingInitialFit()
        {
            var viewport = default(UsableViewport);
            var bounds = new RoomBounds2D(-8f, -4f, 24f, 16f);
            Assert.That(viewport.IsValid, Is.True);
            Assert.That(viewport.TopInsetFraction, Is.Zero);
            Assert.That(viewport.BottomInsetFraction, Is.Zero);
            Assert.That(viewport.TryComputeInitialFit(bounds, 2f, 1f, 5f, out var size, out var center), Is.True);
            Assert.That(size, Is.EqualTo(DynamicZoomLimit.ComputeMaxOrthographicSize(bounds, 2f, 1f, 5f)));
            Assert.That(center, Is.EqualTo(bounds.Center));
        }

        [TestCase(0.25f, 0.125f, 1f)]
        [TestCase(0.125f, 0.25f, 1f)]
        [TestCase(0.25f, 0.25f, 1f)]
        [TestCase(0.75f, 0.125f, 2f)]
        [TestCase(0.125f, 0.75f, 2f)]
        [TestCase(0.5f, 0.25f, 4f / 3f)]
        [TestCase(0.1f, 0.2f, 16f / 9f)]
        public void InitialFit_FitsPaddedBoundsAndAlignsUsableCenter(float top, float bottom, float aspect)
        {
            var viewport = new UsableViewport(top, bottom);
            var bounds = new RoomBounds2D(-7f, 3f, 20f, 12f);
            Assert.That(viewport.TryComputeInitialFit(bounds, aspect, 1f, 5f, out var size, out var center), Is.True);
            Assert.That(size, Is.GreaterThanOrEqualTo(5f));
            Assert.That(center.X, Is.EqualTo(3f));
            Assert.That(center.Y + size * (bottom - top), Is.EqualTo(9f).Within(1e-4f));
            AssertContains(viewport, bounds, center, size, aspect, 1f);
        }

        [Test]
        public void InitialFit_WidthDominatesAtNarrowAspect()
        {
            var viewport = new UsableViewport(0.25f, 0.125f);
            Assert.That(viewport.TryComputeInitialFit(new RoomBounds2D(-20f, -1f, 40f, 2f),
                0.5f, 2f, 5f, out var size, out var center), Is.True);
            Assert.That(size, Is.EqualTo(44f));
            Assert.That(center, Is.EqualTo(new Point2D(0f, 5.5f)));
        }

        [Test]
        public void InitialFit_MinimumSizeStillOffsetsUsableCenter()
        {
            var viewport = new UsableViewport(0.125f, 0.25f);
            Assert.That(viewport.TryComputeInitialFit(new RoomBounds2D(-1f, -1f, 2f, 2f),
                2f, 0f, 10f, out var size, out var center), Is.True);
            Assert.That(size, Is.EqualTo(10f));
            Assert.That(center, Is.EqualTo(new Point2D(0f, -1.25f)));
        }

        [TestCase(0f, 0f)]
        [TestCase(15f, -10f)]
        [TestCase(-15f, 10f)]
        public void FixedCenter_FullViewportMatchesExistingHintFit(float x, float y)
        {
            var bounds = new RoomBounds2D(-7f, -4f, 16f, 12f);
            var center = new Point2D(x, y);
            var request = new HintFramingRequest(center, 5f, 2f, bounds, 1f);
            Assert.That(default(UsableViewport).TryComputeFixedCenterFit(bounds, center, 2f, 1f, 5f, out var size), Is.True);
            Assert.That(size, Is.EqualTo(HintFramingCalculator.Compute(request).TargetOrthographicSize));
        }

        [TestCase(0.25f, 0.125f)]
        [TestCase(0.125f, 0.25f)]
        [TestCase(0.25f, 0.25f)]
        public void FixedCenter_FitsBothPaddedEdgesWithoutChangingCenter(float top, float bottom)
        {
            var viewport = new UsableViewport(top, bottom);
            var bounds = new RoomBounds2D(-4f, -3f, 8f, 6f);
            var center = new Point2D(2f, -1f);
            Assert.That(viewport.TryComputeFixedCenterFit(bounds, center, 2f, 1f, 5f, out var size), Is.True);
            AssertContains(viewport, bounds, center, size, 2f, 1f);
        }

        [Test]
        public void FixedCenter_NeverZoomsInWhenAlreadyFitted()
        {
            var viewport = new UsableViewport(0.25f, 0.125f);
            Assert.That(viewport.TryComputeFixedCenterFit(new RoomBounds2D(-1f, -1f, 2f, 2f),
                default, 2f, 0f, 20f, out var size), Is.True);
            Assert.That(size, Is.EqualTo(20f));
        }

        [TestCase(0.5f, 0.125f, -4f, 4f, 6f, 6f)]
        [TestCase(0.125f, 0.5f, 0f, 4f, 6f, 6f)]
        [TestCase(0.75f, 0.125f, -8f, 2f, 5f, 32f / 3f)]
        [TestCase(0.125f, 0.75f, 6f, 2f, 5f, 32f / 3f)]
        [TestCase(0.75f, 0f, -8f, 4f, 5f, 8f)]
        [TestCase(0f, 0.75f, 4f, 4f, 5f, 8f)]
        public void FixedCenter_HalfOrLargerInsetSucceedsWhenInequalitiesHaveSolution(
            float top, float bottom, float minY, float height, float current, float expected)
        {
            var viewport = new UsableViewport(top, bottom);
            var bounds = new RoomBounds2D(-1f, minY, 2f, height);
            Assert.That(viewport.IsValid, Is.True);
            Assert.That(viewport.TryComputeFixedCenterFit(bounds, default, 2f, 0f, current, out var size), Is.True);
            Assert.That(size, Is.EqualTo(expected).Within(1e-5f));
            AssertContains(viewport, bounds, default, size, 2f, 0f);
        }

        [TestCase(0.5f, 0.125f, -1f, 2f, 5f)]
        [TestCase(0.125f, 0.5f, -1f, 2f, 5f)]
        [TestCase(0.75f, 0.125f, -8f, 4f, 5f)]
        [TestCase(0.125f, 0.75f, 4f, 4f, 5f)]
        [TestCase(0.75f, 0.125f, -8f, 2f, 13f)]
        [TestCase(0.125f, 0.75f, 6f, 2f, 13f)]
        public void FixedCenter_ImpossibleInequalitiesPreserveCurrentFiniteSize(
            float top, float bottom, float minY, float height, float current)
        {
            var viewport = new UsableViewport(top, bottom);
            Assert.That(viewport.IsValid, Is.True);
            Assert.That(viewport.TryComputeFixedCenterFit(new RoomBounds2D(-1f, minY, 2f, height),
                default, 2f, 0f, current, out var size), Is.False);
            Assert.That(size, Is.EqualTo(current));
        }

        [Test]
        public void FixedCenter_HorizontalRequirementCanExceedVerticalUpperSize()
        {
            var viewport = new UsableViewport(0.75f, 0.125f);
            Assert.That(viewport.TryComputeFixedCenterFit(new RoomBounds2D(-30f, -8f, 60f, 2f),
                default, 2f, 0f, 5f, out var size), Is.False);
            Assert.That(size, Is.EqualTo(5f));
        }

        [Test]
        public void FixedCenter_MarginCanMakeZeroCoefficientImpossible()
        {
            var viewport = new UsableViewport(0.5f, 0.125f);
            Assert.That(viewport.TryComputeFixedCenterFit(new RoomBounds2D(-1f, -4f, 2f, 4f),
                default, 2f, 1f, 5f, out var size), Is.False);
            Assert.That(size, Is.EqualTo(5f));
        }

        [TestCase(-0.1f, 0f)]
        [TestCase(0f, -0.1f)]
        [TestCase(1f, 0f)]
        [TestCase(0f, 1f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(0.75f, 0.5f)]
        [TestCase(float.NaN, 0f)]
        [TestCase(0f, float.NaN)]
        [TestCase(float.PositiveInfinity, 0f)]
        [TestCase(0f, float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity, 0f)]
        [TestCase(0f, float.NegativeInfinity)]
        public void InvalidViewport_FitsFailWithFiniteOutputs(float top, float bottom)
        {
            var viewport = new UsableViewport(top, bottom);
            var bounds = new RoomBounds2D(-4f, -3f, 8f, 6f);
            Assert.That(viewport.IsValid, Is.False);
            Assert.That(viewport.TryComputeInitialFit(bounds, 2f, 1f, 5f, out var initial, out var center), Is.False);
            Assert.That(initial, Is.Zero);
            Assert.That(center, Is.EqualTo(default(Point2D)));
            Assert.That(viewport.TryComputeFixedCenterFit(bounds, default, 2f, 1f, 5f, out var fixedSize), Is.False);
            Assert.That(fixedSize, Is.EqualTo(5f));
        }

        [TestCase(0f, 0f, 5f)]
        [TestCase(-1f, 0f, 5f)]
        [TestCase(float.NaN, 0f, 5f)]
        [TestCase(float.PositiveInfinity, 0f, 5f)]
        [TestCase(float.NegativeInfinity, 0f, 5f)]
        [TestCase(2f, -1f, 5f)]
        [TestCase(2f, float.NaN, 5f)]
        [TestCase(2f, float.PositiveInfinity, 5f)]
        [TestCase(2f, float.NegativeInfinity, 5f)]
        [TestCase(2f, 0f, 0f)]
        [TestCase(2f, 0f, -1f)]
        [TestCase(2f, 0f, float.NaN)]
        [TestCase(2f, 0f, float.PositiveInfinity)]
        [TestCase(2f, 0f, float.NegativeInfinity)]
        public void InvalidArguments_FailWithoutNonFiniteOutput(float aspect, float margin, float current)
        {
            var viewport = default(UsableViewport);
            var bounds = new RoomBounds2D(-4f, -3f, 8f, 6f);
            Assert.That(viewport.TryComputeInitialFit(bounds, aspect, margin, current, out var initial, out var center), Is.False);
            Assert.That(initial, Is.Zero);
            Assert.That(center, Is.EqualTo(default(Point2D)));
            Assert.That(viewport.TryComputeFixedCenterFit(bounds, default, aspect, margin, current, out var fixedSize), Is.False);
            Assert.That(fixedSize, Is.EqualTo(current == 5f ? 5f : 0f));
        }

        [Test]
        public void DefaultBounds_FitsFail()
        {
            var viewport = default(UsableViewport);
            Assert.That(viewport.TryComputeInitialFit(default, 2f, 0f, 5f, out _, out _), Is.False);
            Assert.That(viewport.TryComputeFixedCenterFit(default, default, 2f, 0f, 5f, out var size), Is.False);
            Assert.That(size, Is.EqualTo(5f));
        }

        [Test]
        public void HugeBounds_ProducesFiniteSizeAndCenter()
        {
            var bounds = new RoomBounds2D(-float.MaxValue, -1f, float.MaxValue, 2f);
            Assert.That(default(UsableViewport).TryComputeInitialFit(bounds, 2f, 0f, 5f, out var size, out var center), Is.True);
            Assert.That(float.IsInfinity(size), Is.False);
            Assert.That(float.IsInfinity(center.X), Is.False);
        }

        [Test]
        public void UnrepresentableSize_FailsInsteadOfEmittingInfinity()
        {
            var bounds = new RoomBounds2D(-4f, -3f, 8f, 6f);
            var viewport = new UsableViewport(0.25f, 0.125f);
            Assert.That(viewport.TryComputeInitialFit(bounds, float.Epsilon, 0f, 5f, out var size, out var center), Is.False);
            Assert.That(size, Is.Zero);
            Assert.That(center, Is.EqualTo(default(Point2D)));
            Assert.That(viewport.TryComputeFixedCenterFit(bounds, default, float.Epsilon, 0f, 5f, out size), Is.False);
            Assert.That(size, Is.EqualTo(5f));
        }

        [Test]
        public void InitialFit_UnrepresentableCenterFailsWithDefaultOutputs()
        {
            var viewport = new UsableViewport(0.75f, 0f);
            var bounds = new RoomBounds2D(-1f, float.MaxValue * 0.75f, 2f, float.MaxValue * 0.125f);
            Assert.That(viewport.TryComputeInitialFit(bounds, 2f, 0f, float.MaxValue, out var size, out var center), Is.False);
            Assert.That(size, Is.Zero);
            Assert.That(center, Is.EqualTo(default(Point2D)));
        }

        [Test]
        public void FixedCenter_HalfInsetUsesCameraCenterInsteadOfOrigin()
        {
            var viewport = new UsableViewport(0.5f, 0.125f);
            var bounds = new RoomBounds2D(9f, 16f, 2f, 4f);
            var center = new Point2D(10f, 20f);
            Assert.That(viewport.TryComputeFixedCenterFit(bounds, center, 2f, 0f, 5f, out var size), Is.True);
            Assert.That(size, Is.EqualTo(16f / 3f).Within(1e-5f));
            AssertContains(viewport, bounds, center, size, 2f, 0f);
        }

        [Test]
        public void FixedCenter_RoundsRequiredSizeUpWithoutClippingContent()
        {
            var viewport = new UsableViewport(0.25f, 0.125f);
            var bounds = new RoomBounds2D(-1f, -8f, 2f, 9f);
            Assert.That(viewport.TryComputeFixedCenterFit(bounds, default, 2f, 0f, 5f, out var size), Is.True);
            Assert.That((double)size * 0.75d, Is.GreaterThanOrEqualTo(8d));
        }

        [Test]
        public void AlmostFullyOccludedViewport_RemainsValidAndFinite()
        {
            var viewport = new UsableViewport(0.99999994f, 0f);
            var bounds = new RoomBounds2D(-1f, -1f, 2f, 2f);
            Assert.That(viewport.IsValid, Is.True);
            Assert.That(viewport.TryComputeInitialFit(bounds, 2f, 0f, 5f, out var size, out var center), Is.True);
            Assert.That(float.IsInfinity(size), Is.False);
            AssertContains(viewport, bounds, center, size, 2f, 0f);
        }

        private static void AssertContains(UsableViewport viewport, RoomBounds2D bounds, Point2D center,
            float size, float aspect, float margin)
        {
            Assert.That((double)center.X - (double)size * aspect, Is.LessThanOrEqualTo((double)bounds.MinX - margin + 1e-4d));
            Assert.That((double)center.X + (double)size * aspect, Is.GreaterThanOrEqualTo((double)bounds.MaxX + margin - 1e-4d));
            Assert.That(center.Y + size * (2d * viewport.BottomInsetFraction - 1d), Is.LessThanOrEqualTo((double)bounds.MinY - margin + 1e-4d));
            Assert.That(center.Y + size * (1d - 2d * viewport.TopInsetFraction), Is.GreaterThanOrEqualTo((double)bounds.MaxY + margin - 1e-4d));
        }
    }
}
