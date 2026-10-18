using System;
using NUnit.Framework;
using Octoplug.CameraFraming;
using Octoplug.RoomGeneration;

namespace CameraFraming.Core.Tests
{
    public class PanBoundaryCalculatorTests
    {
        [Test]
        public void Compute_ClampsCameraCenterAtAllEdges()
        {
            var range = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-20f, -15f, 40f, 30f),
                orthographicSize: 5f,
                aspect: 2f,
                margin: 2f);

            var high = range.Clamp(new Point2D(0f, 0f), new Point2D(100f, 100f));
            var low = range.Clamp(new Point2D(0f, 0f), new Point2D(-100f, -100f));

            Assert.That(high.X, Is.EqualTo(12f));
            Assert.That(high.Y, Is.EqualTo(12f));
            Assert.That(low.X, Is.EqualTo(-12f));
            Assert.That(low.Y, Is.EqualTo(-12f));
        }

        [Test]
        public void Compute_CollapsedAxisKeepsCurrentCoordinateStable()
        {
            var range = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-4f, -3f, 8f, 6f),
                orthographicSize: 5f,
                aspect: 16f / 9f,
                margin: 0f);

            var result = range.Clamp(new Point2D(3f, -2f), new Point2D(100f, 100f));

            Assert.That(range.X.IsCollapsed, Is.True);
            Assert.That(range.Y.IsCollapsed, Is.True);
            Assert.That(result, Is.EqualTo(new Point2D(3f, -2f)));
        }

        [Test]
        public void Compute_LargerHouseExpandsAllowedRange()
        {
            var small = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-6f, -6f, 12f, 12f), 5f, 1f, 0f);
            var large = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-20f, -20f, 40f, 40f), 5f, 1f, 0f);

            Assert.That(large.X.Min, Is.LessThan(small.X.Min));
            Assert.That(large.X.Max, Is.GreaterThan(small.X.Max));
            Assert.That(large.Y.Min, Is.LessThan(small.Y.Min));
            Assert.That(large.Y.Max, Is.GreaterThan(small.Y.Max));
        }

        [Test]
        public void Compute_SmallHouseFullyVisibleAtCurrentZoom_CollapsesBothAxes()
        {
            // The whole house (plus margin) already fits inside the current viewport: Pan should
            // not be able to move at all, and the camera should stay centered.
            var range = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-4f, -3f, 8f, 6f),
                orthographicSize: 8f,
                aspect: 16f / 9f,
                margin: 1f);

            Assert.That(range.X.IsCollapsed, Is.True);
            Assert.That(range.Y.IsCollapsed, Is.True);
        }

        [Test]
        public void Compute_ZoomingInOnTheSameHouseWithNoNewRoomWidensRangeImmediately()
        {
            const float aspect = 16f / 9f;
            const float margin = 1f;
            var house = new RoomBounds2D(-4f, -3f, 8f, 6f);

            // Zoomed out: whole house already visible, no room ever added or removed.
            var zoomedOut = PanBoundaryCalculator.Compute(house, orthographicSize: 8f, aspect, margin);
            Assert.That(zoomedOut.X.IsCollapsed, Is.True);
            Assert.That(zoomedOut.Y.IsCollapsed, Is.True);

            // Same exact house bounds, only Orthographic Size dropped (Zoom In): the range must
            // open up immediately from that alone.
            var zoomedIn = PanBoundaryCalculator.Compute(house, orthographicSize: 2f, aspect, margin);
            Assert.That(zoomedIn.X.IsCollapsed, Is.False);
            Assert.That(zoomedIn.Y.IsCollapsed, Is.False);
            Assert.That(zoomedIn.X.Max, Is.GreaterThan(zoomedIn.X.Min));
            Assert.That(zoomedIn.Y.Max, Is.GreaterThan(zoomedIn.Y.Min));
        }

        [Test]
        public void Compute_ZoomingBackOutOnTheSameHouseNarrowsOrCollapsesRangeAgain()
        {
            const float aspect = 16f / 9f;
            const float margin = 1f;
            var house = new RoomBounds2D(-4f, -3f, 8f, 6f);

            var zoomedIn = PanBoundaryCalculator.Compute(house, orthographicSize: 2f, aspect, margin);
            var zoomedBackOut = PanBoundaryCalculator.Compute(house, orthographicSize: 8f, aspect, margin);

            Assert.That(zoomedIn.X.IsCollapsed, Is.False);
            Assert.That(zoomedBackOut.X.IsCollapsed, Is.True);
            Assert.That(zoomedBackOut.Y.IsCollapsed, Is.True);
        }

        [Test]
        public void Compute_ZoomedInOnLargeHouseReachesEachEdgeRoomFully()
        {
            const float aspect = 16f / 9f;
            const float margin = 1f;
            const float orthographicSize = 5f;
            var halfWidth = orthographicSize * aspect;
            var halfHeight = orthographicSize;

            // A large house made of small edge rooms far from the origin.
            var house = new RoomBounds2D(-40f, -30f, 80f, 60f);
            var range = PanBoundaryCalculator.Compute(house, orthographicSize, aspect, margin);

            var farRight = range.Clamp(new Point2D(0f, 0f), new Point2D(1000f, 0f));
            var farLeft = range.Clamp(new Point2D(0f, 0f), new Point2D(-1000f, 0f));
            var farTop = range.Clamp(new Point2D(0f, 0f), new Point2D(0f, 1000f));
            var farBottom = range.Clamp(new Point2D(0f, 0f), new Point2D(0f, -1000f));

            // At the far-right clamp, the viewport's right edge must reach the house's right edge
            // (plus margin) so a room flush against that edge is fully visible, not just its center.
            Assert.That(farRight.X + halfWidth, Is.EqualTo(house.MaxX + margin).Within(1e-4f));
            Assert.That(farLeft.X - halfWidth, Is.EqualTo(house.MinX - margin).Within(1e-4f));
            Assert.That(farTop.Y + halfHeight, Is.EqualTo(house.MaxY + margin).Within(1e-4f));
            Assert.That(farBottom.Y - halfHeight, Is.EqualTo(house.MinY - margin).Within(1e-4f));
        }

        [Test]
        public void Compute_VisibleHouseBoundsIncludingHintRoomAllowsReachingTheHintEdge()
        {
            const float aspect = 16f / 9f;
            const float margin = 1f;
            const float orthographicSize = 5f;
            var halfWidth = orthographicSize * aspect;

            // Small unlocked house; a HintLocked room sits far outside it, to the right.
            var unlocked = new RoomBounds2D(-4f, -3f, 8f, 6f);
            var hint = new RoomBounds2D(20f, -3f, 8f, 6f);

            var unlockedOnlyRange = PanBoundaryCalculator.Compute(unlocked, orthographicSize, aspect, margin);
            var visibleHouseBounds = DynamicZoomLimit.CombineBounds(new[] { unlocked, hint });
            var visibleRange = PanBoundaryCalculator.Compute(visibleHouseBounds, orthographicSize, aspect, margin);

            // Unlocked-only bounds cannot reach the hint's right edge at all.
            var reachUsingUnlockedOnly = unlockedOnlyRange.Clamp(new Point2D(0f, 0f), new Point2D(1000f, 0f));
            Assert.That(reachUsingUnlockedOnly.X + halfWidth, Is.LessThan(hint.MaxX));

            // Including the hint's bounds lets the camera reach far enough right that the hint's
            // full right edge (plus margin) is inside the viewport.
            var reachUsingVisibleHouse = visibleRange.Clamp(new Point2D(0f, 0f), new Point2D(1000f, 0f));
            Assert.That(reachUsingVisibleHouse.X + halfWidth, Is.GreaterThanOrEqualTo(hint.MaxX + margin - 1e-4f));
        }

        [TestCase(5f)]
        [TestCase(50f)]
        public void Compute_ZeroTopInsetPreservesFourArgumentContract(float size)
        {
            var house = new RoomBounds2D(-20f, -15f, 40f, 30f);
            var legacy = PanBoundaryCalculator.Compute(house, size, 2f, 2f);
            var inset = PanBoundaryCalculator.Compute(house, size, 2f, 2f, topInsetFraction: 0f);

            Assert.That(inset.X.Min, Is.EqualTo(legacy.X.Min));
            Assert.That(inset.X.Max, Is.EqualTo(legacy.X.Max));
            Assert.That(inset.X.IsCollapsed, Is.EqualTo(legacy.X.IsCollapsed));
            Assert.That(inset.Y.Min, Is.EqualTo(legacy.Y.Min));
            Assert.That(inset.Y.Max, Is.EqualTo(legacy.Y.Max));
            Assert.That(inset.Y.IsCollapsed, Is.EqualTo(legacy.Y.IsCollapsed));
        }

        [TestCase(0.1f)]
        [TestCase(0.25f)]
        [TestCase(0.75f)]
        public void Compute_TopInsetExtendsOnlyUpperBoundaryAndReachesUsableTop(float inset)
        {
            const float size = 5f;
            const float margin = 2f;
            var house = new RoomBounds2D(-20f, -15f, 40f, 30f);
            var range = PanBoundaryCalculator.Compute(house, size, 2f, margin, inset);
            var high = range.Clamp(new Point2D(0f, 0f), new Point2D(100f, 100f));
            var low = range.Clamp(new Point2D(0f, 0f), new Point2D(-100f, -100f));

            Assert.That(low.X, Is.EqualTo(-12f));
            Assert.That(high.X, Is.EqualTo(12f));
            Assert.That(low.Y, Is.EqualTo(-12f));
            Assert.That(high.Y, Is.EqualTo(12f + 2f * size * inset).Within(1e-5f));
            Assert.That(high.Y + size - 2f * size * inset,
                Is.EqualTo(house.MaxY + margin).Within(1e-5f));
            Assert.That((range.Y.Min + range.Y.Max) / 2f,
                Is.EqualTo(size * inset).Within(1e-5f));
        }

        [Test]
        public void Compute_TopInsetReleasesPreviouslyCollapsedVerticalAxis()
        {
            var house = new RoomBounds2D(-4f, -4f, 8f, 8f);
            var legacy = PanBoundaryCalculator.Compute(house, 5f, 2f, 0f);
            var inset = PanBoundaryCalculator.Compute(house, 5f, 2f, 0f, 0.3f);

            Assert.That(legacy.Y.IsCollapsed, Is.True);
            Assert.That(inset.X.IsCollapsed, Is.True);
            Assert.That(inset.Y.IsCollapsed, Is.False);
            Assert.That(inset.Y.Min, Is.EqualTo(1f));
            Assert.That(inset.Y.Max, Is.EqualTo(2f).Within(1e-5f));
            Assert.That(inset.Clamp(new Point2D(3f, -2f), new Point2D(100f, 100f)),
                Is.EqualTo(new Point2D(3f, 2f)));
        }

        [Test]
        public void Compute_HouseFitsUsableHeightButCurrentIsOutside_RecoversOnlyToNearestSafeCoordinate()
        {
            var range = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-4f, -4f, 8f, 8f), 5f, 2f, 0f, 0.1f);
            var current = new Point2D(3f, -2f);

            Assert.That(range.X.IsCollapsed, Is.True);
            Assert.That(range.Y.IsCollapsed, Is.True);
            var recovered = range.Clamp(current, new Point2D(100f, 100f));
            Assert.That(recovered.X, Is.EqualTo(3f));
            Assert.That(recovered.Y, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void Compute_UsableHeightExactlyMatchesHouse_IsSinglePointNotCollapsed()
        {
            // Size 4 and inset 1/4 leave exactly six world units of usable height.
            var range = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-4f, -3f, 8f, 6f), 4f, 1f, 0f, 0.25f);

            Assert.That(range.X.IsCollapsed, Is.False);
            Assert.That(range.Y.IsCollapsed, Is.False);
            Assert.That(range.Y.Min, Is.EqualTo(1f));
            Assert.That(range.Y.Max, Is.EqualTo(1f));
            Assert.That(range.Clamp(new Point2D(3f, -2f), new Point2D(100f, 100f)),
                Is.EqualTo(new Point2D(3f, 1f)));
        }

        [TestCase(-0.01f)]
        [TestCase(1f)]
        [TestCase(1.01f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Compute_InvalidTopInsetThrows(float inset)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanBoundaryCalculator.Compute(new RoomBounds2D(-20f, -15f, 40f, 30f),
                    5f, 2f, 2f, inset));

            Assert.That(exception!.ParamName, Is.EqualTo("topInsetFraction"));
        }

        [Test]
        public void Compute_TopInsetUsesCurrentZoomAndAspect()
        {
            var house = new RoomBounds2D(-20f, -15f, 40f, 30f);
            var initial = PanBoundaryCalculator.Compute(house, 5f, 2f, 2f, 0.25f);
            var zoomedIn = PanBoundaryCalculator.Compute(house, 2f, 2f, 2f, 0.25f);
            var narrowAspect = PanBoundaryCalculator.Compute(house, 2f, 1f, 2f, 0.25f);

            Assert.That(initial.Y.Min, Is.EqualTo(-12f));
            Assert.That(initial.Y.Max, Is.EqualTo(14.5f));
            Assert.That(zoomedIn.Y.Min, Is.EqualTo(-15f));
            Assert.That(zoomedIn.Y.Max, Is.EqualTo(16f));
            Assert.That(zoomedIn.X.Max, Is.EqualTo(18f));
            Assert.That(narrowAspect.X.Max, Is.EqualTo(20f));
            Assert.That(narrowAspect.Y.Min, Is.EqualTo(zoomedIn.Y.Min));
            Assert.That(narrowAspect.Y.Max, Is.EqualTo(zoomedIn.Y.Max));
        }

        [TestCase(0.25f, 0.125f)]
        [TestCase(0f, 0.75f)]
        [TestCase(0.75f, 0.125f)]
        public void Compute_BothInsetsReachUsableEdgesAndPreserveHorizontalRange(float top, float bottom)
        {
            var house = new RoomBounds2D(-20f, -15f, 40f, 30f);
            var range = PanBoundaryCalculator.Compute(house, 5f, 2f, 2f, top, bottom);

            Assert.That(range.X.Min, Is.EqualTo(-12f));
            Assert.That(range.X.Max, Is.EqualTo(12f));
            Assert.That(range.Y.Min + 5f * (2f * bottom - 1f), Is.EqualTo(-17f).Within(1e-5f));
            Assert.That(range.Y.Max + 5f * (1f - 2f * top), Is.EqualTo(17f).Within(1e-5f));
        }

        [Test]
        public void Compute_ZeroBottomInsetPreservesFiveArgumentContract()
        {
            var house = new RoomBounds2D(-4f, -4f, 8f, 8f);
            var legacy = PanBoundaryCalculator.Compute(house, 5f, 2f, 0f, 0.1f);
            var both = PanBoundaryCalculator.Compute(house, 5f, 2f, 0f, 0.1f, 0f);
            Assert.That(both.X, Is.EqualTo(legacy.X));
            Assert.That(both.Y, Is.EqualTo(legacy.Y));
        }

        [Test]
        public void Compute_CollapsedEndpointsDescribeFullContentContainment()
        {
            var range = PanBoundaryCalculator.Compute(new RoomBounds2D(-4f, -3f, 8f, 6f),
                10f, 2f, 1f, 0.25f, 0.125f);
            Assert.That(range.X.IsCollapsed, Is.True);
            Assert.That(range.X.Min, Is.EqualTo(-15f));
            Assert.That(range.X.Max, Is.EqualTo(15f));
            Assert.That(range.Y.IsCollapsed, Is.True);
            Assert.That(range.Y.Min, Is.EqualTo(-1f));
            Assert.That(range.Y.Max, Is.EqualTo(3.5f));
            Assert.That(range.Clamp(new Point2D(1f, 1f), new Point2D(-100f, 100f)),
                Is.EqualTo(new Point2D(1f, 1f)));
        }

        [TestCase(-0.1f)]
        [TestCase(1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Compute_InvalidBottomInsetThrows(float bottom)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanBoundaryCalculator.Compute(new RoomBounds2D(-4f, -3f, 8f, 6f), 5f, 2f, 1f, 0f, bottom));
            Assert.That(exception!.ParamName, Is.EqualTo("bottomInsetFraction"));
        }

        [TestCase(0.5f, 0.5f)]
        [TestCase(0.75f, 0.5f)]
        public void Compute_NoUsableHeightThrows(float top, float bottom)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanBoundaryCalculator.Compute(new RoomBounds2D(-4f, -3f, 8f, 6f), 5f, 2f, 1f, top, bottom));
        }

        [TestCase(0f, 1f, 0f)]
        [TestCase(-1f, 1f, 0f)]
        [TestCase(float.NaN, 1f, 0f)]
        [TestCase(float.PositiveInfinity, 1f, 0f)]
        [TestCase(5f, 0f, 0f)]
        [TestCase(5f, float.NaN, 0f)]
        [TestCase(5f, float.PositiveInfinity, 0f)]
        [TestCase(5f, 1f, -1f)]
        [TestCase(5f, 1f, float.NaN)]
        [TestCase(5f, 1f, float.PositiveInfinity)]
        [TestCase(float.MaxValue, float.MaxValue, 0f)]
        public void Compute_InvalidOrOverflowedGeometryThrows(float size, float aspect, float margin)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PanBoundaryCalculator.Compute(new RoomBounds2D(-4f, -3f, 8f, 6f), size, aspect, margin));
        }

        [Test]
        public void Compute_DefaultBoundsThrows()
        {
            Assert.Throws<ArgumentException>(() => PanBoundaryCalculator.Compute(default, 5f, 1f, 0f));
        }

        [TestCase(10f, -0.125f, 9.875f)]
        [TestCase(10f, 0.125f, 10f)]
        [TestCase(10f, 0f, 10f)]
        [TestCase(10f, -100f, -2f)]
        [TestCase(-10f, 0.125f, -9.875f)]
        [TestCase(-10f, -0.125f, -10f)]
        [TestCase(-10f, 0f, -10f)]
        [TestCase(-10f, 100f, 2f)]
        [TestCase(0f, 100f, 2f)]
        [TestCase(0f, -100f, -2f)]
        public void Clamp_NormalRangeOnlyLimitsRequestedDelta(float current, float delta, float expected)
        {
            var range = new PanAxisRange(-2f, 2f, false);
            Assert.That(range.ClampDelta(current, delta), Is.EqualTo(expected));
            Assert.That(range.Clamp(current, current + delta), Is.EqualTo(expected));
        }

        [TestCase(10f, -0.125f, 9.875f)]
        [TestCase(10f, 0.125f, 10f)]
        [TestCase(10f, 0f, 10f)]
        [TestCase(10f, -100f, 2f)]
        [TestCase(-10f, 0.125f, -9.875f)]
        [TestCase(-10f, -0.125f, -10f)]
        [TestCase(-10f, 0f, -10f)]
        [TestCase(-10f, 100f, -2f)]
        [TestCase(0f, 100f, 0f)]
        [TestCase(0f, -100f, 0f)]
        [TestCase(-2f, 1f, -2f)]
        [TestCase(2f, -1f, 2f)]
        public void Clamp_CollapsedRangeAllowsOnlyMinimalRecovery(float current, float delta, float expected)
        {
            var range = new PanAxisRange(-2f, 2f, true);
            Assert.That(range.ClampDelta(current, delta), Is.EqualTo(expected));
            Assert.That(range.Clamp(current, current + delta), Is.EqualTo(expected));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClosestSafeCoordinate_ReturnsNearestEndpointOrCurrentWithoutInput(bool collapsed)
        {
            var range = new PanAxisRange(-2f, 3f, collapsed);
            Assert.That(range.ClosestSafeCoordinate(-100f), Is.EqualTo(-2f));
            Assert.That(range.ClosestSafeCoordinate(100f), Is.EqualTo(3f));
            Assert.That(range.ClosestSafeCoordinate(1f), Is.EqualTo(1f));
        }

        [Test]
        public void ClampDelta_CenterUsesIndependentAxisRules()
        {
            var range = new PanCenterRange(new PanAxisRange(-2f, 2f, false), new PanAxisRange(-3f, 3f, true));
            var current = new Point2D(10f, -10f);
            Assert.That(range.ClampDelta(current, new Point2D(-0.125f, 0.25f)), Is.EqualTo(new Point2D(9.875f, -9.75f)));
            Assert.That(range.ClosestSafeCoordinate(current), Is.EqualTo(new Point2D(2f, -3f)));
        }

        [Test]
        public void Clamp_FiniteExtremeCoordinatesDoNotOverflowTheirDelta()
        {
            var range = new PanAxisRange(-2f, 2f, false);
            Assert.That(range.Clamp(float.MaxValue, -float.MaxValue), Is.EqualTo(-2f));
            Assert.That(range.Clamp(-float.MaxValue, float.MaxValue), Is.EqualTo(2f));
            Assert.That(range.Clamp(float.MaxValue, 1f), Is.EqualTo(1f));
            Assert.That(range.ClampDelta(float.MaxValue, float.MaxValue), Is.EqualTo(float.MaxValue));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void Clamp_NonFiniteInputThrows(float invalid)
        {
            var range = new PanAxisRange(-2f, 2f, false);
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Clamp(invalid, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.Clamp(0f, invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.ClampDelta(0f, invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => range.ClosestSafeCoordinate(invalid));
        }

        [Test]
        public void Compute_HintFramingDoesNotChangeCurrentPositionAndPanRemainsUsableAfter()
        {
            // Boundary math itself never moves the camera; it only clamps a REQUESTED delta. A
            // zero delta (as during one-shot Hint framing, which only changes Lens size) must be a
            // strict no-op regardless of the current range.
            var range = PanBoundaryCalculator.Compute(
                new RoomBounds2D(-4f, -3f, 8f, 6f), orthographicSize: 2f, aspect: 16f / 9f, margin: 1f);

            var current = new Point2D(1f, -0.5f);
            var unchanged = range.Clamp(current, current);

            Assert.That(unchanged, Is.EqualTo(current));
        }
    }
}
