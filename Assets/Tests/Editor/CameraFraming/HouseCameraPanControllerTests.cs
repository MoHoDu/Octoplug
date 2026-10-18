using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.CameraFraming.Unity;
using Octoplug.RoomGeneration;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Octoplug.Tests.Editor.CameraFraming
{
    public sealed class HouseCameraPanControllerTests
    {
        private readonly List<GameObject> createdObjects = new();
        private CinemachineCamera cinemachineCamera;
        private Camera outputCamera;
        private HouseCameraPanController controller;

        [SetUp]
        public void SetUp()
        {
            var outputObject = new GameObject("Output Camera");
            createdObjects.Add(outputObject);
            outputCamera = outputObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
            outputCamera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            outputCamera.aspect = 2f;

            var virtualObject = new GameObject("Cinemachine Camera");
            createdObjects.Add(virtualObject);
            cinemachineCamera = virtualObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Lens.OrthographicSize = 5f;

            var controllerObject = new GameObject("Camera System");
            createdObjects.Add(controllerObject);
            controller = controllerObject.AddComponent<HouseCameraPanController>();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("cinemachineCamera").objectReferenceValue =
                cinemachineCamera;
            serializedController.FindProperty("outputCamera").objectReferenceValue =
                outputCamera;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            controller.UpdateHouseBounds(new RoomBounds2D(-20f, -15f, 40f, 30f));
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
        public void ApplyPanDelta_WhenCameraMoves_RaisesMotionStarted()
        {
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.ApplyPanDelta(Vector2.right);

            Assert.That(cinemachineCamera.transform.position, Is.Not.EqualTo(Vector3.zero));
            Assert.That(motionStarted, Is.EqualTo(1));
        }

        [Test]
        public void ApplyPanDelta_WithoutHud_PreservesFullViewportLimitsAndZ()
        {
            cinemachineCamera.transform.position = new Vector3(0f, 0f, -10f);

            controller.ApplyPanDelta(new Vector2(100f, 100f));
            AssertPosition(12f, 12f, -10f);
            controller.ApplyPanDelta(new Vector2(-100f, -100f));
            AssertPosition(-12f, -12f, -10f);
        }

        [Test]
        public void ApplyPanDelta_OverlayHud_ExpandsTopOnlyAndEmitsOnlyForActualMotion()
        {
            CreateTopHud(0.25f);
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;
            cinemachineCamera.transform.position = new Vector3(0f, 0f, -10f);

            controller.ApplyPanDelta(new Vector2(100f, 100f));
            AssertPosition(12f, 14.5f, -10f);
            Assert.That(motionStarted, Is.EqualTo(1));
            controller.ApplyPanDelta(new Vector2(100f, 100f));
            Assert.That(motionStarted, Is.EqualTo(1));
            controller.ApplyPanDelta(new Vector2(-100f, -100f));
            AssertPosition(-12f, -12f, -10f);
            Assert.That(motionStarted, Is.EqualTo(2));
        }

        [Test]
        public void ApplyPanDelta_HudResize_UsesLatestHeightWithoutHouseUpdate()
        {
            var hud = CreateTopHud(0.2f);
            controller.ApplyPanDelta(Vector2.up * 100f);
            AssertPosition(0f, 14f);

            hud.sizeDelta = new Vector2(0f, Screen.height * 0.4f);
            Canvas.ForceUpdateCanvases();
            controller.ApplyPanDelta(Vector2.up * 100f);
            AssertPosition(0f, 16f);

            hud.sizeDelta = new Vector2(0f, Screen.height * 0.1f);
            Canvas.ForceUpdateCanvases();
            controller.ApplyPanDelta(Vector2.up * 100f);
            AssertPosition(0f, 16f);
            controller.ApplyPanDelta(Vector2.down * 0.25f);
            AssertPosition(0f, 15.75f);
        }

        [Test]
        public void ApplyPanDelta_CanvasScaleChanges_UsesRenderedHudHeight()
        {
            var hud = CreateTopHud(0.2f);
            controller.ApplyPanDelta(Vector2.up * 100f);
            AssertPosition(0f, 14f);

            hud.GetComponentInParent<Canvas>().scaleFactor = 2f;
            Canvas.ForceUpdateCanvases();
            controller.ApplyPanDelta(Vector2.up * 100f);

            AssertPosition(0f, 16f);
        }

        [Test]
        public void ApplyPanDelta_OffsetPixelRect_UsesClippedIntersectionAndViewportHeight()
        {
            CreateTopHud(0.3f);
            // HUD occupies screen Y .7..1; viewport Y .2...9 leaves .2/.7 occluded.
            outputCamera.pixelRect = new Rect(Screen.width * 0.1f, Screen.height * 0.2f,
                Screen.width * 0.8f, Screen.height * 0.7f);
            outputCamera.aspect = 2f;

            controller.ApplyPanDelta(new Vector2(100f, 100f));

            AssertPosition(12f, 12f + 10f * (2f / 7f));
        }

        [Test]
        public void ApplyPanDelta_LatestZoomAndAspect_UpdatesHudAwareLimits()
        {
            CreateTopHud(0.25f);
            controller.ApplyPanDelta(new Vector2(100f, 100f));
            AssertPosition(12f, 14.5f);

            cinemachineCamera.Lens.OrthographicSize = 2f;
            outputCamera.aspect = 1f;
            controller.ApplyPanDelta(new Vector2(100f, 100f));

            AssertPosition(20f, 16f);
        }

        [TestCase("InactiveHud")]
        [TestCase("InactiveCanvas")]
        [TestCase("DisabledCanvas")]
        [TestCase("NoCanvas")]
        [TestCase("OutsideViewport")]
        [TestCase("NoTopIntersection")]
        public void ApplyPanDelta_UnusableHud_FallsBackToFullViewport(string condition)
        {
            var hud = CreateTopHud(0.25f);
            var canvas = hud.GetComponentInParent<Canvas>();
            switch (condition)
            {
                case "InactiveHud":
                    hud.gameObject.SetActive(false);
                    break;
                case "InactiveCanvas":
                    canvas.gameObject.SetActive(false);
                    break;
                case "DisabledCanvas":
                    canvas.enabled = false;
                    break;
                case "NoCanvas":
                    hud.SetParent(null, true);
                    break;
                case "OutsideViewport":
                    hud.anchoredPosition = new Vector2(Screen.width * 2f, 0f);
                    break;
                case "NoTopIntersection":
                    hud.anchoredPosition = new Vector2(0f, -Screen.height * 0.3f);
                    break;
            }
            Canvas.ForceUpdateCanvases();

            controller.ApplyPanDelta(new Vector2(100f, 100f));

            AssertPosition(12f, 12f);
        }

        [TestCase(1f)]
        [TestCase(1.2f)]
        public void ApplyPanDelta_HudCoversEntireViewport_SkipsPanAndEvent(float fraction)
        {
            CreateTopHud(fraction);
            var initial = new Vector3(3f, -2f, -10f);
            cinemachineCamera.transform.position = initial;
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.ApplyPanDelta(new Vector2(100f, 100f));

            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(initial));
            Assert.That(motionStarted, Is.Zero);
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 0f)]
        public void ApplyPanDelta_EmptyPixelRect_SkipsPanAndEvent(float width, float height)
        {
            CreateTopHud(0.25f);
            outputCamera.pixelRect = new Rect(0f, 0f, width, height);
            outputCamera.aspect = 2f;
            var initial = new Vector3(3f, -2f, -10f);
            cinemachineCamera.transform.position = initial;
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.ApplyPanDelta(Vector2.up * 100f);

            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(initial));
            Assert.That(motionStarted, Is.Zero);
        }

        [Test]
        public void HudZoomAspectAndHouseChanges_WithoutPan_DoNotMoveOrEmit()
        {
            var hud = CreateTopHud(0.25f);
            var initial = new Vector3(100f, 100f, -10f);
            cinemachineCamera.transform.position = initial;
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            hud.sizeDelta = new Vector2(0f, Screen.height * 0.4f);
            cinemachineCamera.Lens.OrthographicSize = 2f;
            outputCamera.aspect = 1f;
            controller.UpdateHouseBounds(new RoomBounds2D(-10f, -10f, 20f, 20f));
            Canvas.ForceUpdateCanvases();

            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(initial));
            Assert.That(motionStarted, Is.Zero);
            controller.ApplyPanDelta(Vector2.zero);
            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(initial));
            Assert.That(motionStarted, Is.Zero);
        }

        [TestCase(0.25f, 1.5f, 1)]
        [TestCase(0.1f, 0f, 1)]
        public void ApplyPanDelta_HudUsableHeightDeterminesCollapse(
            float fraction, float expectedY, int expectedEvents)
        {
            CreateTopHud(fraction);
            // Margin 2 gives a bounded height of 8: full height 10 used to collapse Y.
            controller.UpdateHouseBounds(new RoomBounds2D(-1f, -2f, 2f, 4f));
            cinemachineCamera.transform.position = new Vector3(3f, -2f, -10f);
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.ApplyPanDelta(new Vector2(100f, 100f));

            AssertPosition(3f, expectedY, -10f);
            Assert.That(motionStarted, Is.EqualTo(expectedEvents));
        }

        [Test]
        public void ApplyPanDelta_WhilePaused_UpdatesOutputCameraAtHudAwareBoundary()
        {
            CreateTopHud(0.25f);
            cinemachineCamera.transform.position = new Vector3(0f, 0f, -10f);
            var previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                controller.ApplyPanDelta(Vector2.up * 100f);

                AssertPosition(0f, 14.5f, -10f);
                Assert.That(outputCamera.transform.position,
                    Is.EqualTo(cinemachineCamera.transform.position));
            }
            finally
            {
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void ApplyPanDelta_BottomResidents_ExpandsBottomOnly()
        {
            CreateBottomResidents(0.2f);
            controller.ApplyPanDelta(new Vector2(100f, -100f));
            AssertPosition(12f, -14f);
            controller.ApplyPanDelta(Vector2.up * 100f);
            AssertPosition(12f, 12f);
        }

        [Test]
        public void ApplyPanDelta_AsymmetricTopAndBottom_UsesBothIntersections()
        {
            CreateTopHud(0.25f);
            CreateBottomResidents(0.1f);
            controller.ApplyPanDelta(Vector2.up * 100f);
            AssertPosition(0f, 14.5f);
            controller.ApplyPanDelta(Vector2.down * 100f);
            AssertPosition(0f, -13f);
        }

        [Test]
        public void ApplyPanDelta_OverlappingTopAndBottom_SkipsUntilUsableAgain()
        {
            CreateTopHud(0.6f);
            var bottom = CreateBottomResidents(0.5f);
            controller.ApplyPanDelta(Vector2.down);
            AssertPosition(0f, 0f);
            bottom.gameObject.SetActive(false);
            controller.ApplyPanDelta(Vector2.down);
            AssertPosition(0f, -1f);
        }

        [Test]
        public void ApplyPanDelta_BottomOffsetPixelRect_UsesActualClippedHeight()
        {
            CreateBottomResidents(0.3f);
            outputCamera.pixelRect = new Rect(Screen.width * 0.1f, Screen.height * 0.1f,
                Screen.width * 0.8f, Screen.height * 0.7f);
            outputCamera.aspect = 2f;
            controller.ApplyPanDelta(Vector2.down * 100f);
            AssertPosition(0f, -12f - 10f * (2f / 7f));
        }

        [TestCase("Inactive")]
        [TestCase("DisabledCanvas")]
        [TestCase("OutsideViewport")]
        [TestCase("NoBottomIntersection")]
        public void ApplyPanDelta_UnusableBottom_LeavesFullViewport(string condition)
        {
            var bottom = CreateBottomResidents(0.25f);
            switch (condition)
            {
                case "Inactive": bottom.gameObject.SetActive(false); break;
                case "DisabledCanvas": bottom.GetComponentInParent<Canvas>().enabled = false; break;
                case "OutsideViewport": bottom.anchoredPosition = Vector2.right * Screen.width * 2f; break;
                case "NoBottomIntersection": bottom.anchoredPosition = Vector2.up * Screen.height * 0.4f; break;
            }
            Canvas.ForceUpdateCanvases();
            controller.ApplyPanDelta(Vector2.down * 100f);
            AssertPosition(0f, -12f);
        }

        [Test]
        public void ApplyPanDelta_OutsideAfterZoom_TinyInwardMovesOnlyRequestedDistance()
        {
            cinemachineCamera.transform.position = new Vector3(12f, 12f, -10f);
            cinemachineCamera.Lens.OrthographicSize = 8f;
            controller.ApplyPanDelta(new Vector2(-0.01f, -0.02f));
            AssertPosition(11.99f, 11.98f, -10f);
            controller.ApplyPanDelta(Vector2.one * 0.01f);
            AssertPosition(11.99f, 11.98f, -10f);
        }

        [Test]
        public void ApplyPanDelta_CollapsedUnsafeAxis_RecoversInwardWithoutSnap()
        {
            controller.UpdateHouseBounds(new RoomBounds2D(-1f, -1f, 2f, 2f));
            cinemachineCamera.transform.position = new Vector3(20f, 20f, -10f);
            controller.ApplyPanDelta(new Vector2(-0.01f, -0.02f));
            AssertPosition(19.99f, 19.98f, -10f);
            controller.ApplyPanDelta(new Vector2(-100f, -100f));
            AssertPosition(7f, 2f, -10f);
            controller.ApplyPanDelta(new Vector2(-1f, -1f));
            AssertPosition(7f, 2f, -10f);
        }

        [Test]
        public void QueuedPixels_UseLatestVirtualLensRatherThanStaleOutputLens()
        {
            outputCamera.pixelRect = new Rect(0f, 0f, 1000f, 500f);
            outputCamera.aspect = 2f;
            outputCamera.orthographicSize = 100f;
            controller.QueuePixelPanDelta(new Vector2(2f, 1f), 1f);
            cinemachineCamera.Lens.OrthographicSize = 8f;
            AssertPosition(0f, 0f);
            controller.ApplyQueuedPixelPan();
            AssertPosition(-0.064f, -0.032f);
            controller.ApplyQueuedPixelPan();
            AssertPosition(-0.064f, -0.032f);
        }

        [Test]
        public void QueuedPixels_InvalidViewportAreDiscardedRatherThanReplayed()
        {
            outputCamera.pixelRect = new Rect(0f, 0f, 0f, 0f);
            controller.QueuePixelPanDelta(Vector2.one * 100f, 1f);
            controller.ApplyQueuedPixelPan();
            outputCamera.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            outputCamera.aspect = 2f;
            controller.ApplyQueuedPixelPan();
            AssertPosition(0f, 0f);
        }

        private RectTransform CreateBottomResidents(float screenHeightFraction)
        {
            var previousTop = new SerializedObject(controller).FindProperty("topHudArea").objectReferenceValue;
            var bottom = CreateTopHud(screenHeightFraction);
            bottom.anchorMin = Vector2.zero;
            bottom.anchorMax = Vector2.right;
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            var serializedController = new SerializedObject(controller);
            // top 연결은 별도 Canvas에서 먼저 만든 기존 HUD를 보존한다.
            serializedController.FindProperty("topHudArea").objectReferenceValue = previousTop;
            serializedController.FindProperty("bottomResidentsArea").objectReferenceValue = bottom;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            return bottom;
        }

        private RectTransform CreateTopHud(float screenHeightFraction)
        {
            var canvasObject = new GameObject("Test Overlay Canvas", typeof(RectTransform));
            createdObjects.Add(canvasObject);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = 1f;

            var hudObject = new GameObject("Test Top HUD", typeof(RectTransform));
            createdObjects.Add(hudObject);
            var hud = hudObject.GetComponent<RectTransform>();
            hud.SetParent(canvas.transform, false);
            hud.anchorMin = new Vector2(0f, 1f);
            hud.anchorMax = Vector2.one;
            hud.pivot = new Vector2(0.5f, 1f);
            hud.anchoredPosition = Vector2.zero;
            hud.sizeDelta = new Vector2(0f, Screen.height * screenHeightFraction);
            Canvas.ForceUpdateCanvases();

            var serializedController = new SerializedObject(controller);
            serializedController.FindProperty("topHudArea").objectReferenceValue = hud;
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        private void AssertPosition(float x, float y, float z = 0f)
        {
            var position = cinemachineCamera.transform.position;
            Assert.That(position.x, Is.EqualTo(x).Within(1e-3f));
            Assert.That(position.y, Is.EqualTo(y).Within(1e-3f));
            Assert.That(position.z, Is.EqualTo(z).Within(1e-3f));
        }

        [Test]
        public void ApplyPanDelta_WhenBoundaryPreventsMovement_DoesNotRaiseMotionStarted()
        {
            controller.UpdateHouseBounds(new RoomBounds2D(-1f, -1f, 1f, 1f));
            var motionStarted = 0;
            controller.CameraMotionStarted += () => motionStarted++;

            controller.ApplyPanDelta(Vector2.right);

            Assert.That(cinemachineCamera.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(motionStarted, Is.Zero);
        }
    }
}
