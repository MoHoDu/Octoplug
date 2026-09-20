using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.CameraFraming.Unity;
using Octoplug.Power.UI;
using Octoplug.RoomGeneration;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace Octoplug.Tests.Editor.CameraFraming
{
    public sealed class CameraProductTooltipDismissalBridgeTests
    {
        private readonly List<GameObject> createdObjects = new();

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
        public void CameraMotionStarted_HidesProductTooltip()
        {
            var outputObject = CreateObject("Output Camera");
            var outputCamera = outputObject.AddComponent<Camera>();
            outputCamera.orthographic = true;

            var virtualObject = CreateObject("Cinemachine Camera");
            var cinemachineCamera = virtualObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Lens.OrthographicSize = 5f;

            var cameraSystem = CreateObject("Camera System");
            var cameraZoom = cameraSystem.AddComponent<HouseCameraZoomController>();
            var serializedZoom = new SerializedObject(cameraZoom);
            serializedZoom.FindProperty("cinemachineCamera").objectReferenceValue =
                cinemachineCamera;
            serializedZoom.FindProperty("outputCamera").objectReferenceValue =
                outputCamera;
            serializedZoom.ApplyModifiedPropertiesWithoutUndo();
            cameraZoom.InitializeForVerification();

            var tooltipObject = CreateObject("Product Tooltip");
            var tooltip = tooltipObject.AddComponent<ProductTooltipController>();
            var panelObject = CreateObject("Panel");
            var panel = panelObject.AddComponent<RectTransform>();
            var serializedTooltip = new SerializedObject(tooltip);
            serializedTooltip.FindProperty("panel").objectReferenceValue = panel;
            serializedTooltip.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(true);

            var bridge = cameraSystem.AddComponent<CameraProductTooltipDismissalBridge>();
            var serializedBridge = new SerializedObject(bridge);
            serializedBridge.FindProperty("cameraZoom").objectReferenceValue = cameraZoom;
            serializedBridge.FindProperty("productTooltip").objectReferenceValue = tooltip;
            serializedBridge.ApplyModifiedPropertiesWithoutUndo();
            bridge.InitializeForVerification();

            cameraZoom.RequestHintFraming(
                new RoomBounds2D(20f, -2f, 28f, 2f));

            Assert.That(panel.gameObject.activeSelf, Is.False);
        }

        private GameObject CreateObject(string name)
        {
            var instance = new GameObject(name);
            createdObjects.Add(instance);
            return instance;
        }
    }
}
