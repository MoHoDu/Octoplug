using NUnit.Framework;
using Octoplug.CameraFraming;
using Octoplug.RoomGeneration;

namespace CameraFraming.Core.Tests
{
    public class HintFramingCalculatorTests
    {
        [Test]
        public void Compute_ReportsUnchanged_WhenFullHintBoundsAlreadyFitCurrentView()
        {
            // Camera centered at origin, size 10, aspect 1: view covers -10..10 both axes.
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 10f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-2f, -2f, 4f, 4f),
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.Changed, Is.False);
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(10f));
        }

        [Test]
        public void Compute_ZoomsOut_WhenCenterIsVisibleButRightEdgeIsNot()
        {
            // Explicitly asymmetric room so the center sits well inside the current view (size 5,
            // spans -5..5) while one edge does not: bounds x in [-1, 9], center x = 4 (inside),
            // right edge x = 9 (outside). Framing on the center alone would wrongly report
            // "no change needed"; the full bounds must drive the decision.
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 5f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-1f, -1f, 10f, 2f),
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.Changed, Is.True);
            // Farthest x-distance from camera center (0) is |9 - 0| = 9.
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(9f));
        }

        [Test]
        public void Compute_ZoomsOut_WhenCenterIsVisibleButTopEdgeIsNot()
        {
            // Bounds y in [-1, 9], center y = 4 (inside a view of half-height 5), top edge y = 9
            // (outside). The center-only reading would wrongly report "already visible".
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 5f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-1f, -1f, 2f, 10f),
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.Changed, Is.True);
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(9f));
        }

        [Test]
        public void Compute_ReportsUnchanged_WhenFullBoundsVisible_NotJustCenter()
        {
            // A large room whose center is at the origin and whose full extent (half-size 6) is
            // still smaller than the current view (half-height/width 8 at aspect 1).
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 8f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-6f, -6f, 12f, 12f),
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.Changed, Is.False);
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(8f));
        }

        [Test]
        public void Compute_NeverZoomsInPastCurrentSize()
        {
            // The hint would only need size 4, but the user is already at 10 — never zoom in.
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 10f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-1f, -1f, 2f, 2f),
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.Changed, Is.False);
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(10f));
        }

        [Test]
        public void Compute_IsNeverCappedByAnyPriorZoomLimit()
        {
            // There is no upper bound at all in the calculator itself — the full hint bounds must
            // always end up visible, however large the required size is. Any interaction with a
            // house-only dynamic max is the caller's responsibility (HouseCameraZoomController),
            // not this pure calculation.
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 5f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-1f, 20f, 2f, 6f), // far edge at y=26
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.Changed, Is.True);
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(26f));
        }

        [Test]
        public void Compute_WideRoom_AtDesignReferenceAspect_LeftAndRightBothVisible()
        {
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 5f,
                aspect: CameraDesignReference.DesignAspect,
                hintBounds: new RoomBounds2D(-15f, -1f, 30f, 2f), // x in [-15, 15]
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            var expectedFromWidth = 15f / CameraDesignReference.DesignAspect;
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(expectedFromWidth).Within(0.001f));

            // The resulting orthographic size must make both edges fit: verify directly.
            var halfViewportWidth = result.TargetOrthographicSize * CameraDesignReference.DesignAspect;
            Assert.That(halfViewportWidth, Is.GreaterThanOrEqualTo(15f - 0.001f), "Right/left edges (x=±15) must both fit.");
        }

        [Test]
        public void Compute_TallRoom_AtDesignReferenceAspect_TopAndBottomBothVisible()
        {
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 5f,
                aspect: CameraDesignReference.DesignAspect,
                hintBounds: new RoomBounds2D(-1f, -10f, 2f, 20f), // y in [-10, 10]
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.TargetOrthographicSize, Is.EqualTo(10f).Within(0.001f));
            Assert.That(result.TargetOrthographicSize, Is.GreaterThanOrEqualTo(10f - 0.001f), "Top/bottom edges (y=±10) must both fit.");
        }

        [Test]
        public void Compute_VariableSizeAndOffAxisRoom_FullBoundsVisible()
        {
            // Room bounds are always the final axis-aligned bounds regardless of any upstream
            // rotation (Room roots are validated at zero rotation elsewhere in this project), so
            // an arbitrary, non-square, off-center footprint stands in for "rotated/variable-size".
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(2f, -3f),
                currentOrthographicSize: 5f,
                aspect: CameraDesignReference.DesignAspect,
                hintBounds: new RoomBounds2D(10f, 4f, 12f, 9f), // x in [10, 22], y in [4, 13]
                margin: 1f);

            var result = HintFramingCalculator.Compute(request);

            var expectedVertical = System.Math.Max(System.Math.Abs(13f - (-3f)), System.Math.Abs(-3f - 4f)) + 1f;
            var expectedHorizontal = (System.Math.Max(System.Math.Abs(22f - 2f), System.Math.Abs(2f - 10f)) + 1f) / CameraDesignReference.DesignAspect;
            var expected = System.Math.Max(expectedVertical, expectedHorizontal);

            Assert.That(result.Changed, Is.True);
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void Compute_HintLeftOfCamera_FullBoundsVisible()
        {
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(10f, 0f),
                currentOrthographicSize: 5f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-10f, -3f, 8f, 6f), // x in [-10, -2]
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            // Farthest x-distance from camera center (10) is |-10 - 10| = 20.
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(20f));
        }

        [Test]
        public void Compute_HintBelowCamera_FullBoundsVisible()
        {
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 10f),
                currentOrthographicSize: 5f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-3f, -20f, 6f, 8f), // y in [-20, -12]
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            // Farthest y-distance from camera center (10) is |-20 - 10| = 30.
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(30f));
        }

        [Test]
        public void Compute_AccountsForAspectOnWidth()
        {
            // Wide hint room, aspect 2 (wider than tall): width dominates.
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 5f,
                aspect: 2f,
                hintBounds: new RoomBounds2D(-20f, -1f, 40f, 2f),
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            // half width 20 / aspect 2 = 10, greater than half height (1).
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(10f));
        }

        [Test]
        public void Compute_AddsMarginToRequiredSize()
        {
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(0f, 0f),
                currentOrthographicSize: 5f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(-1f, 9f, 2f, 2f), // far edge at y=11, half-height needed without margin is 11
                margin: 2f);

            var result = HintFramingCalculator.Compute(request);

            Assert.That(result.TargetOrthographicSize, Is.EqualTo(13f));
        }

        [Test]
        public void Compute_UsesOffsetCameraCenter_NotHintCenter()
        {
            // Camera is not centered on the hint; distances are measured from the camera, not the hint center.
            var request = new HintFramingRequest(
                cameraCenter: new Point2D(5f, 0f),
                currentOrthographicSize: 5f,
                aspect: 1f,
                hintBounds: new RoomBounds2D(10f, -1f, 2f, 2f), // x in [10, 12]
                margin: 0f);

            var result = HintFramingCalculator.Compute(request);

            // Farthest x-distance from camera center (5) is |12 - 5| = 7.
            Assert.That(result.TargetOrthographicSize, Is.EqualTo(7f));
        }
    }
}
