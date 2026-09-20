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
        private HouseCameraPanController controller;

        [SetUp]
        public void SetUp()
        {
            var outputObject = new GameObject("Output Camera");
            createdObjects.Add(outputObject);
            var outputCamera = outputObject.AddComponent<Camera>();
            outputCamera.orthographic = true;

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
            controller.UpdateHouseBounds(new RoomBounds2D(-20f, -20f, 20f, 20f));
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
