using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.CameraFraming.Unity;
using Octoplug.RoomGeneration;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Octoplug.Tests.Editor.CameraFraming
{
    public sealed class HouseCameraZoomControllerTests
    {
        private readonly List<GameObject> createdObjects = new();
        private CinemachineCamera cinemachineCamera;
        private HouseCameraZoomController controller;
        private Camera outputCamera;
        private HouseCameraPanController pan;

        [SetUp]
        public void SetUp()
        {
            var outputObject = new GameObject("Output Camera");
            createdObjects.Add(outputObject);
            outputCamera = outputObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            outputCamera.aspect = 2f;
            outputObject.transform.position = new Vector3(0f, 0f, -10f);

            var virtualObject = new GameObject("Cinemachine Camera");
            createdObjects.Add(virtualObject);
            cinemachineCamera = virtualObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Lens.OrthographicSize = 5f;

            var controllerObject = new GameObject("Camera System");
            createdObjects.Add(controllerObject);
            controller = controllerObject.AddComponent<HouseCameraZoomController>();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("cinemachineCamera").objectReferenceValue =
                cinemachineCamera;
            serializedController.FindProperty("outputCamera").objectReferenceValue =
                outputCamera;
            serializedController.FindProperty("zoomSmoothing").floatValue = 0f;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            controller.InitializeForVerification();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void RequestRoomReveal_ZoomsOutWithoutMovingAndCompletesWhenSettled()
        {
            var initialPosition = cinemachineCamera.transform.position;
            var completed = 0;
            controller.CameraRevealCompleted += () => completed++;

            controller.RequestRoomReveal(new RoomBounds2D(20f, -2f, 28f, 2f));

            Assert.That(completed, Is.Zero);
            controller.TickForVerification();

            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.GreaterThan(5f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(initialPosition));
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void RequestRoomReveal_WhenAlreadyVisible_CompletesWithoutZoomingIn()
        {
            var motionStarted = 0;
            var completed = 0;
            controller.CameraMotionStarted += () => motionStarted++;
            controller.CameraRevealCompleted += () => completed++;

            controller.RequestRoomReveal(new RoomBounds2D(-1f, -1f, 1f, 1f));

            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(motionStarted, Is.Zero);
            Assert.That(completed, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HintOrReveal_WhenOnlyPaddingFallsOutsideUsableViewport_DoesNotMoveOrZoom(bool reveal)
        {
            CreatePan();
            CreateInset("topHudArea", 0.2f, true);
            CreateInset("bottomResidentsArea", 0.1f, false);
            SetSmoothing(0.15f);
            var center = cinemachineCamera.transform.position;
            var motionStarted = 0;
            var completed = 0;
            controller.CameraMotionStarted += () => motionStarted++;
            controller.CameraRevealCompleted += () => completed++;
            // size5의 가용 Y 구간은 [-4, 3]. 방 경계는 보이지만 margin2를 더하면 벗어난다.
            var bounds = new RoomBounds2D(-9f, -3.5f, 18f, 6f);

            if (reveal) controller.RequestRoomReveal(bounds);
            else controller.RequestHintFraming(bounds);

            Assert.That(completed, Is.EqualTo(reveal ? 1 : 0));
            Assert.That(motionStarted, Is.Zero);
            for (var i = 0; i < 200; i++) controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(center));
            Assert.That(motionStarted, Is.Zero);
            Assert.That(completed, Is.EqualTo(reveal ? 1 : 0));
        }

        [TestCase(false, true, 8f)]
        [TestCase(true, true, 8f)]
        [TestCase(false, false, 7.5f)]
        [TestCase(true, false, 7.5f)]
        public void HintOrReveal_PartiallyOccludedEdge_UsesMinimumRawBoundsSize(
            bool reveal, bool topEdge, float expectedSize)
        {
            CreatePan();
            CreateInset("topHudArea", 0.25f, true);
            CreateInset("bottomResidentsArea", 0.1f, false);
            var center = cinemachineCamera.transform.position;
            var motionStarted = 0;
            var completed = 0;
            controller.CameraMotionStarted += () => motionStarted++;
            controller.CameraRevealCompleted += () => completed++;
            // 상단은 4 / (1 - 2 * .25), 하단은 6 / (1 - 2 * .1) 크기가 필요하다.
            var bounds = topEdge
                ? new RoomBounds2D(-1f, 1f, 2f, 3f)
                : new RoomBounds2D(-1f, -6f, 2f, 3f);

            if (reveal) controller.RequestRoomReveal(bounds);
            else controller.RequestHintFraming(bounds);

            Assert.That(completed, Is.Zero);
            controller.TickForVerification(0.02f);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(expectedSize).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(center));
            Assert.That(motionStarted, Is.EqualTo(1));
            Assert.That(completed, Is.EqualTo(reveal ? 1 : 0));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HintOrReveal_RepeatedPendingRequest_DoesNotAccumulateZoomOrCompletion(bool reveal)
        {
            SetSmoothing(0.15f);
            var center = cinemachineCamera.transform.position;
            var motionStarted = 0;
            var completed = 0;
            controller.CameraMotionStarted += () => motionStarted++;
            controller.CameraRevealCompleted += () => completed++;
            var bounds = new RoomBounds2D(-1f, -8f, 2f, 16f);

            for (var i = 0; i < 3; i++)
            {
                if (reveal) controller.RequestRoomReveal(bounds);
                else controller.RequestHintFraming(bounds);
                controller.TickForVerification(0.02f);
                Assert.That(completed, Is.Zero);
            }

            for (var i = 0; i < 200; i++) controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(8f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(center));
            Assert.That(motionStarted, Is.EqualTo(1));
            Assert.That(completed, Is.EqualTo(reveal ? 1 : 0));
        }

        [Test]
        public void InitialFitDynamicMaximumAndPan_RetainHouseMarginTwo()
        {
            CreatePan();
            controller.QueueInitialFraming(new RoomBounds2D(-4f, -6f, 8f, 12f));
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(8f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(Vector3.zero));

            controller.RecomputeDynamicMaxOrthographicSize(new RoomBounds2D(-20f, -15f, 40f, 30f));
            controller.ApplyZoomDelta(100f);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(17f).Within(0.001f));

            controller.ApplyZoomDelta(-100f);
            controller.TickForVerification(0.02f);
            pan.ApplyPanDelta(new Vector2(100f, 100f));
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(cinemachineCamera.transform.position.x, Is.EqualTo(12f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position.y, Is.EqualTo(12f).Within(0.001f));
        }

        [Test]
        public void ApplyZoomDelta_WhenTargetChanges_RaisesMotionStarted()
        {
            controller.RecomputeDynamicMaxOrthographicSize(
                new RoomBounds2D(-10f, -10f, 10f, 10f));
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.ApplyZoomDelta(1f);

            Assert.That(motionStarted, Is.EqualTo(1));
        }

        [Test]
        public void ApplyZoomDelta_WhenClampedAtMinimum_DoesNotRaiseMotionStarted()
        {
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.ApplyZoomDelta(-1f);

            Assert.That(motionStarted, Is.Zero);
        }

        [Test]
        public void InitialFraming_CoalescesLatestVisibleBoundsAndSynchronizesTarget()
        {
            CreatePan();
            CreateInset("topHudArea", 0.3f, true);
            CreateInset("bottomResidentsArea", 0.1f, false);
            cinemachineCamera.transform.position = new Vector3(-50f, 30f, -10f);
            var events = 0;
            controller.CameraMotionStarted += () => events++;
            controller.QueueInitialFraming(new RoomBounds2D(-100f, -100f, 200f, 200f));
            controller.QueueInitialFraming(new RoomBounds2D(-10f, -6f, 30f, 20f));
            Assert.That(cinemachineCamera.transform.position.x, Is.EqualTo(-50f));
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(20f).Within(0.001f));
            Assert.That(Vector3.Distance(cinemachineCamera.transform.position, new Vector3(5f, 8f, -10f)), Is.LessThan(0.001f));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(controller.QueueInitialFraming(new RoomBounds2D(100f, 100f, 20f, 20f)), Is.False);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(20f).Within(0.001f));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void InitialFraming_FullyOccludedWaitsForLayoutAndUsesLatestBounds()
        {
            CreatePan();
            var hud = CreateInset("topHudArea", 1f, true);
            controller.QueueInitialFraming(new RoomBounds2D(0f, 0f, 30f, 30f));
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(Vector3.zero));
            controller.QueueInitialFraming(new RoomBounds2D(10f, 20f, 4f, 4f));
            hud.gameObject.SetActive(false);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(new Vector3(12f, 22f, 0f)));
        }

        [Test]
        public void ManualZoom_NonzeroSmoothingCorrectsContinuouslyWithoutPerTickMotionEvents()
        {
            CreatePan();
            SetSmoothing(0.15f);
            cinemachineCamera.transform.position = new Vector3(12f, 12f, -10f);
            var events = 0;
            controller.CameraMotionStarted += () => events++;
            controller.ApplyZoomDelta(3f);
            controller.TickForVerification(0.02f);
            var first = cinemachineCamera.transform.position;
            Assert.That(first.x, Is.InRange(6.001f, 11.99999f));
            Assert.That(first.y, Is.InRange(9.001f, 11.99999f));
            for (var i = 0; i < 300; i++) controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(8f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position.x, Is.EqualTo(6f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position.y, Is.EqualTo(9f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position.z, Is.EqualTo(-10f));
            Assert.That(events, Is.EqualTo(1));
        }

        [Test]
        public void ManualZoom_InitiallyOutside_NoHardSnapAndDebtContinuesAfterLensSettles()
        {
            CreatePan();
            SetSmoothing(0.15f);
            cinemachineCamera.transform.position = new Vector3(100f, 100f, -10f);
            controller.ApplyZoomDelta(1f);
            controller.TickForVerification(0.02f);
            var first = cinemachineCamera.transform.position;
            Assert.That(first.x, Is.InRange(50f, 99.999f));
            Assert.That(first.y, Is.InRange(50f, 99.999f));
            cinemachineCamera.Lens.OrthographicSize = 6f;
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.transform.position.x, Is.LessThan(first.x));
            for (var i = 0; i < 300; i++) controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.transform.position.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position.y, Is.EqualTo(11f).Within(0.001f));
        }

        [Test]
        public void ManualZoom_UsesUnscaledTickWhilePaused()
        {
            CreatePan();
            SetSmoothing(0.15f);
            cinemachineCamera.transform.position = new Vector3(12f, 12f, -10f);
            var previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                controller.ApplyZoomDelta(3f);
                controller.TickForVerification(0.02f);
                Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.GreaterThan(5f));
                Assert.That(cinemachineCamera.transform.position.x, Is.LessThan(12f));
                Assert.That(outputCamera.transform.position, Is.EqualTo(cinemachineCamera.transform.position));
                Assert.That(outputCamera.orthographicSize, Is.EqualTo(cinemachineCamera.Lens.OrthographicSize));
            }
            finally { Time.timeScale = previousTimeScale; }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HintOrReveal_CancelsManualCorrectionAndPreservesAuthoritativeCenter(bool reveal)
        {
            CreatePan();
            SetSmoothing(0.15f);
            cinemachineCamera.transform.position = new Vector3(100f, 100f, -10f);
            controller.ApplyZoomDelta(1f);
            controller.TickForVerification(0.02f);
            var center = cinemachineCamera.transform.position;
            var bounds = new RoomBounds2D(center.x - 1f, center.y - 1f, 2f, 2f);
            if (reveal) controller.RequestRoomReveal(bounds);
            else controller.RequestHintFraming(bounds);
            for (var i = 0; i < 200; i++) controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(center));
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(6f).Within(0.001f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HintOrReveal_DuringManualZoomIn_DoesNotCarryNegativeLensVelocity(bool reveal)
        {
            CreatePan();
            controller.ApplyZoomDelta(3f);
            controller.TickForVerification(0.02f);
            SetSmoothing(0.15f);
            controller.ApplyZoomDelta(-3f);
            controller.TickForVerification(0.02f);
            var size = cinemachineCamera.Lens.OrthographicSize;
            var bounds = new RoomBounds2D(-1f, -size + 2f, 2f, 2f * size - 4f);
            if (reveal) controller.RequestRoomReveal(bounds);
            else controller.RequestHintFraming(bounds);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.GreaterThanOrEqualTo(size));
        }

        [Test]
        public void HintFraming_UsesVirtualCenterInsteadOfStaleOutputPosition()
        {
            cinemachineCamera.transform.position = new Vector3(100f, 100f, -10f);
            controller.RequestHintFraming(new RoomBounds2D(99f, 99f, 2f, 2f));
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(new Vector3(100f, 100f, -10f)));
        }

        [Test]
        public void HintFraming_AsymmetricInsets_FitsBothEdgesWithoutMoving()
        {
            CreatePan();
            CreateInset("topHudArea", 0.3f, true);
            CreateInset("bottomResidentsArea", 0.1f, false);
            controller.RequestHintFraming(new RoomBounds2D(-1f, -4f, 2f, 8f));
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(10f).Within(0.001f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(Vector3.zero));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Reveal_InvalidOrImpossibleViewport_RemainsPendingAndResumesWithoutFalseCompletion(bool impossible)
        {
            CreatePan();
            var top = CreateInset("topHudArea", impossible ? 0.6f : 1f, true);
            var completed = 0;
            controller.CameraRevealCompleted += () => completed++;
            controller.RequestRoomReveal(new RoomBounds2D(-1f, -1f, 2f, 2f));
            for (var i = 0; i < 3; i++) controller.TickForVerification(0.02f);
            Assert.That(completed, Is.Zero);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(Vector3.zero));
            top.gameObject.SetActive(false);
            controller.TickForVerification(0.02f);
            controller.TickForVerification(0.02f);
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void Reveal_ImpossibleFitAllowsQueuedPanToRecoverAndThenComplete()
        {
            CreatePan();
            CreateInset("topHudArea", 0.6f, true);
            var completed = 0;
            controller.CameraRevealCompleted += () => completed++;
            controller.RequestRoomReveal(new RoomBounds2D(-1f, -1f, 2f, 2f));
            pan.QueuePixelPanDelta(new Vector2(0f, -outputCamera.pixelRect.height * 0.5f), 1f);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.transform.position.y, Is.EqualTo(5f).Within(0.001f));
            Assert.That(completed, Is.Zero);
            controller.TickForVerification(0.02f);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void Reveal_ViewportBecomesImpossibleBeforeSettling_DoesNotCompleteUntilRestored()
        {
            CreatePan();
            SetSmoothing(0.15f);
            var hud = CreateInset("topHudArea", 0.1f, true);
            var completed = 0;
            controller.CameraRevealCompleted += () => completed++;
            controller.RequestRoomReveal(new RoomBounds2D(-1f, -6f, 2f, 12f));
            Assert.That(completed, Is.Zero, "The raw bounds must require zoom-out before the viewport changes.");
            hud.sizeDelta = new Vector2(0f, Screen.height * 0.6f);
            Canvas.ForceUpdateCanvases();
            controller.TickForVerification(0.02f);
            Assert.That(completed, Is.Zero);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            hud.gameObject.SetActive(false);
            for (var i = 0; i < 200; i++) controller.TickForVerification(0.02f);
            Assert.That(completed, Is.EqualTo(1));
        }

        [Test]
        public void Hint_ImpossibleFitIsRetriedWhenViewportReturns()
        {
            CreatePan();
            var hud = CreateInset("topHudArea", 0.6f, true);
            controller.RequestHintFraming(new RoomBounds2D(-1f, -6f, 2f, 12f));
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            hud.gameObject.SetActive(false);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(6f));
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ManualZoomAndQueuedPan_SameTickMapsPixelsAfterLensSmoothing()
        {
            CreatePan();
            SetSmoothing(0.15f);
            outputCamera.pixelRect = new Rect(0f, 0f, 1000f, 500f);
            outputCamera.aspect = 2f;
            outputCamera.orthographicSize = 100f;
            controller.ApplyZoomDelta(3f);
            pan.QueuePixelPanDelta(new Vector2(2f, 1f), 1f);
            controller.TickForVerification(0.02f);
            var size = cinemachineCamera.Lens.OrthographicSize;
            Assert.That(cinemachineCamera.transform.position.x, Is.EqualTo(-2f / 1000f * size * 4f).Within(0.00001f));
            Assert.That(cinemachineCamera.transform.position.y, Is.EqualTo(-1f / 500f * size * 2f).Within(0.00001f));
        }

        [Test]
        public void DynamicRange_ViewportChangeUsesCachedHouseWithoutGenerationEvent()
        {
            CreatePan();
            controller.RecomputeDynamicMaxOrthographicSize(new RoomBounds2D(-10f, -10f, 20f, 20f));
            CreateInset("topHudArea", 0.2f, true);
            CreateInset("bottomResidentsArea", 0.2f, false);
            controller.ApplyZoomDelta(100f);
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.Lens.OrthographicSize, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void ViewportAndHouseChangesWithoutManualZoom_DoNotCorrectPosition()
        {
            CreatePan();
            cinemachineCamera.transform.position = new Vector3(100f, 100f, -10f);
            CreateInset("topHudArea", 0.2f, true);
            CreateInset("bottomResidentsArea", 0.2f, false);
            controller.RecomputeDynamicMaxOrthographicSize(new RoomBounds2D(-10f, -10f, 20f, 20f));
            controller.TickForVerification(0.02f);
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(new Vector3(100f, 100f, -10f)));
        }

        private void CreatePan()
        {
            pan = controller.gameObject.AddComponent<HouseCameraPanController>();
            var serializedPan = new SerializedObject(pan);
            serializedPan.FindProperty("cinemachineCamera").objectReferenceValue = cinemachineCamera;
            serializedPan.FindProperty("outputCamera").objectReferenceValue = outputCamera;
            serializedPan.ApplyModifiedPropertiesWithoutUndo();
            pan.UpdateHouseBounds(new RoomBounds2D(-20f, -15f, 40f, 30f));
            var serializedZoom = new SerializedObject(controller);
            serializedZoom.FindProperty("cameraPan").objectReferenceValue = pan;
            serializedZoom.ApplyModifiedPropertiesWithoutUndo();
            controller.RecomputeDynamicMaxOrthographicSize(new RoomBounds2D(-20f, -15f, 40f, 30f));
        }

        private void SetSmoothing(float smoothing)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("zoomSmoothing").floatValue = smoothing;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private RectTransform CreateInset(string property, float fraction, bool top)
        {
            var canvasObject = new GameObject("Test Overlay", typeof(RectTransform), typeof(Canvas));
            createdObjects.Add(canvasObject);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var areaObject = new GameObject("Test UI Area", typeof(RectTransform));
            createdObjects.Add(areaObject);
            var area = areaObject.GetComponent<RectTransform>();
            area.SetParent(canvas.transform, false);
            area.anchorMin = new Vector2(0f, top ? 1f : 0f);
            area.anchorMax = new Vector2(1f, top ? 1f : 0f);
            area.pivot = new Vector2(0.5f, top ? 1f : 0f);
            area.sizeDelta = new Vector2(0f, Screen.height * fraction);
            area.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            var serialized = new SerializedObject(pan);
            serialized.FindProperty(property).objectReferenceValue = area;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return area;
        }

        [Test]
        public void RequestHintFraming_WhenZoomOutIsRequired_RaisesMotionStarted()
        {
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.RequestHintFraming(
                new RoomBounds2D(20f, -2f, 28f, 2f));

            Assert.That(motionStarted, Is.EqualTo(1));
        }
    }
}
