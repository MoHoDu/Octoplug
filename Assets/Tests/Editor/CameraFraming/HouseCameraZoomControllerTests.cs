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

        [SetUp]
        public void SetUp()
        {
            var outputObject = new GameObject("Output Camera");
            createdObjects.Add(outputObject);
            var outputCamera = outputObject.AddComponent<Camera>();
            outputCamera.orthographic = true;
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
