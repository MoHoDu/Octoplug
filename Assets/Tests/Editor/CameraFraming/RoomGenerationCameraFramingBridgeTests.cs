using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Octoplug.CameraFraming.Unity;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Octoplug.Tests.Editor.CameraFraming
{
    public sealed class RoomGenerationCameraFramingBridgeTests
    {
        private readonly List<GameObject> createdObjects = new();
        private ProductionRoomGenerationController generation;
        private RoomGenerationCameraFramingBridge bridge;
        private HouseCameraZoomController zoom;
        private CinemachineCamera virtualCamera;
        private static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            var output = CreateObject("Output").AddComponent<Camera>();
            output.orthographic = true;
            output.pixelRect = new Rect(0f, 0f, Screen.width, Screen.height);
            output.aspect = 2f;
            virtualCamera = CreateObject("Virtual Camera").AddComponent<CinemachineCamera>();
            virtualCamera.Lens.OrthographicSize = 5f;
            virtualCamera.transform.position = new Vector3(0f, 0f, -10f);
            var pan = CreateObject("Pan").AddComponent<HouseCameraPanController>();
            SetReference(pan, "cinemachineCamera", virtualCamera);
            SetReference(pan, "outputCamera", output);
            zoom = CreateObject("Zoom").AddComponent<HouseCameraZoomController>();
            SetReference(zoom, "cinemachineCamera", virtualCamera);
            SetReference(zoom, "outputCamera", output);
            SetReference(zoom, "cameraPan", pan);
            var serializedZoom = new SerializedObject(zoom);
            serializedZoom.FindProperty("zoomSmoothing").floatValue = 0f;
            serializedZoom.ApplyModifiedPropertiesWithoutUndo();
            zoom.InitializeForVerification();
            generation = CreateObject("Generation").AddComponent<ProductionRoomGenerationController>();
            bridge = CreateObject("Bridge").AddComponent<RoomGenerationCameraFramingBridge>();
            SetReference(bridge, "roomGeneration", generation);
            SetReference(bridge, "cameraZoom", zoom);
            SetReference(bridge, "cameraPan", pan);
            typeof(RoomGenerationCameraFramingBridge).GetMethod("OnEnable", PrivateInstance).Invoke(bridge, null);
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null) Object.DestroyImmediate(createdObjects[i]);
            }
            createdObjects.Clear();
        }

        [Test]
        public void StartupHintBurst_FitsLatestUnlockedHouseAndHintOnlyAtFirstTick()
        {
            var seed = Room("seed", -4f, -4f);
            var promoted = Room("promoted", 4f, -4f);
            var latestHint = Room("latest", 4f, 4f);
            SetUnlocked(seed);
            RaiseHint(promoted);
            SetUnlocked(seed, promoted);
            RaiseHint(latestHint);

            Assert.That(virtualCamera.Lens.OrthographicSize, Is.EqualTo(5f));
            Assert.That(virtualCamera.transform.position, Is.EqualTo(new Vector3(0f, 0f, -10f)));
            zoom.TickForVerification(0.02f);
            Assert.That(virtualCamera.Lens.OrthographicSize, Is.EqualTo(10f).Within(0.001f));
            Assert.That(virtualCamera.transform.position, Is.EqualTo(new Vector3(4f, 4f, -10f)));
            zoom.TickForVerification(0.02f);
            Assert.That(virtualCamera.Lens.OrthographicSize, Is.EqualTo(10f).Within(0.001f));
        }

        [Test]
        public void LaterHintAndReveal_DoNotRepeatBootstrapAndForwardOneCompletion()
        {
            var seed = Room("seed", -4f, -4f);
            SetUnlocked(seed);
            RaiseHint(Room("first", 4f, -4f));
            zoom.TickForVerification(0.02f);
            var center = virtualCamera.transform.position;
            RaiseHint(Room("far", 20f, 12f));
            zoom.TickForVerification(0.02f);
            Assert.That(virtualCamera.transform.position, Is.EqualTo(center));
            var completed = 0;
            bridge.CameraRevealCompleted += () => completed++;
            bridge.RequestRoomReveal(Room("reveal", 20f, 12f));
            zoom.TickForVerification(0.02f);
            zoom.TickForVerification(0.02f);
            Assert.That(completed, Is.EqualTo(1));
            Assert.That(virtualCamera.transform.position, Is.EqualTo(center));
        }

        [Test]
        public void FartherNextHintThenNearerReveal_SameFrame_PreservesSmoothedTargetAndCompletesOnce()
        {
            var seed = Room("seed", -4f, -4f);
            var promoted = Room("promoted", 4f, -4f);
            SetUnlocked(seed);
            RaiseHint(promoted);
            zoom.TickForVerification(0.02f);
            Assert.That(virtualCamera.Lens.OrthographicSize, Is.EqualTo(6f).Within(0.001f));
            var center = virtualCamera.transform.position;
            var serializedZoom = new SerializedObject(zoom);
            serializedZoom.FindProperty("zoomSmoothing").floatValue = 0.15f;
            serializedZoom.ApplyModifiedPropertiesWithoutUndo();
            var motionStarted = 0;
            var completed = 0;
            var sizeAtCompletion = 0f;
            zoom.CameraMotionStarted += () => motionStarted++;
            bridge.CameraRevealCompleted += () =>
            {
                completed++;
                sizeAtCompletion = virtualCamera.Lens.OrthographicSize;
            };

            // 실제 생성/UI 입력이 아닌 이벤트 연결 검증이다. 다음 Hint와 GameFlow의
            // 승격방 Reveal 요청 사이에 tick을 넣지 않아 실제 호출 순서를 재현한다.
            SetUnlocked(seed, promoted);
            RaiseHint(Room("farther-next", 20f, 12f));
            bridge.RequestRoomReveal(promoted);

            Assert.That(virtualCamera.Lens.OrthographicSize, Is.EqualTo(6f).Within(0.001f));
            Assert.That(completed, Is.Zero, "An already-visible promoted room must await the farther Hint target.");
            Assert.That(motionStarted, Is.EqualTo(1));
            zoom.TickForVerification(0.02f);
            Assert.That(virtualCamera.Lens.OrthographicSize, Is.InRange(6.001f, 19.999f));
            Assert.That(completed, Is.Zero);
            for (var i = 0; i < 200; i++) zoom.TickForVerification(0.02f);

            // 중심 Y=0을 유지할 때 방 상단은 Y=20이며 여백 때문에 size2를 더하면 안 된다.
            Assert.That(virtualCamera.Lens.OrthographicSize, Is.EqualTo(20f).Within(0.001f));
            Assert.That(sizeAtCompletion, Is.EqualTo(20f).Within(0.001f));
            Assert.That(virtualCamera.transform.position, Is.EqualTo(center));
            Assert.That(motionStarted, Is.EqualTo(1));
            Assert.That(completed, Is.EqualTo(1));
        }

        private void SetUnlocked(params RoomPlacement[] rooms)
        {
            typeof(ProductionRoomGenerationController).GetField("state", PrivateInstance)
                .SetValue(generation, new RoomGenerationState(RoomLayout.Create(rooms)));
        }

        private void RaiseHint(RoomPlacement room)
        {
            // 실제 Room 생성/UI 입력 검증이 아니라, bridge의 이벤트 연결과 병합 계약을 검증한다.
            var handler = (Action<RoomPlacement>)typeof(ProductionRoomGenerationController)
                .GetField("NextHintCreated", PrivateInstance).GetValue(generation);
            Assert.That(handler, Is.Not.Null);
            handler.Invoke(room);
        }

        private static RoomPlacement Room(string id, float x, float y) =>
            new(new RoomId(id), new RoomBounds2D(x, y, 8f, 8f));

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            createdObjects.Add(instance);
            return instance;
        }

        private static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
