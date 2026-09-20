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
