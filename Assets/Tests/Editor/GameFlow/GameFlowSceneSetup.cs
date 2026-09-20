using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Octoplug.GameFlow.Unity;
using Octoplug.CameraFraming.Unity;
using Octoplug.ResidentDemand.Unity;
using Octoplug.RoomGeneration.Unity;

namespace Octoplug.EditorSetup
{
    public static class GameFlowSceneSetup
    {
        public static void SetupInfiniteMode()
        {
            var scenePath = "Assets/00_Scenes/Demo/InfiniteMode.unity";
            var scene = EditorSceneManager.OpenScene(scenePath);

            var gameFlowObj = GameObject.Find("GameFlowManager");
            if (gameFlowObj == null)
            {
                gameFlowObj = new GameObject("GameFlowManager");
            }

            var manager = gameFlowObj.GetComponent<GameFlowManager>();
            if (manager == null)
            {
                manager = gameFlowObj.AddComponent<GameFlowManager>();
            }

            // Find dependencies
#if UNITY_2023_1_OR_NEWER
            var sessionProgress = Object.FindAnyObjectByType<SessionProgressController>();
            var roomGen = Object.FindAnyObjectByType<ProductionRoomGenerationController>();
            var residentDemand = Object.FindAnyObjectByType<ResidentDemandController>();
            var cameraBridge = Object.FindAnyObjectByType<RoomGenerationCameraFramingBridge>();
#else
            var sessionProgress = Object.FindObjectOfType<SessionProgressController>();
            var roomGen = Object.FindObjectOfType<ProductionRoomGenerationController>();
            var residentDemand = Object.FindObjectOfType<ResidentDemandController>();
            var cameraBridge = Object.FindObjectOfType<RoomGenerationCameraFramingBridge>();
#endif

            var so = new SerializedObject(manager);
            so.FindProperty("sessionProgress").objectReferenceValue = sessionProgress;
            so.FindProperty("roomGeneration").objectReferenceValue = roomGen;
            so.FindProperty("residentDemand").objectReferenceValue = residentDemand;
            so.FindProperty("cameraBridge").objectReferenceValue = cameraBridge;
            so.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("GameFlowManager successfully added and configured in InfiniteMode scene.");
        }
    }
}
