using System;
using System.Collections.Generic;
using System.Linq;
using Octoplug.GameFlow;
using Octoplug.GameFlow.Unity;
using Octoplug.ResidentDemand.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Octoplug.EditorSetup
{
    public static class DemoSceneFlowSceneSetup
    {
        public static void Apply()
        {
            BindLobby();
            BindInfiniteMode();
            BindResult();
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
        }

        private static void BindLobby()
        {
            var scene = EditorSceneManager.OpenScene(DemoSceneNames.LobbyPath);
            var canvas = FindUnique<Canvas>(scene, component => component.name == "Canvas");
            var infiniteMode = FindByLocalId<Button>(scene, 308002797);
            var exit = FindByLocalId<Button>(scene, 1777999832);
            var controller = GetOrAdd<LobbySceneController>(canvas.gameObject);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("infiniteModeButton").objectReferenceValue = infiniteMode;
            serialized.FindProperty("exitButton").objectReferenceValue = exit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static void BindInfiniteMode()
        {
            var scene = EditorSceneManager.OpenScene(DemoSceneNames.InfiniteModePath);
            var gameFlow = FindUnique<GameFlowManager>(scene, _ => true);
            var progress = FindUnique<SessionProgressController>(scene, _ => true);
            var transition = GetOrAdd<GameOverResultTransition>(gameFlow.gameObject);
            var serialized = new SerializedObject(transition);
            serialized.FindProperty("gameFlow").objectReferenceValue = gameFlow;
            serialized.FindProperty("sessionProgress").objectReferenceValue = progress;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static void BindResult()
        {
            var scene = EditorSceneManager.OpenScene(DemoSceneNames.ResultPath);
            var canvas = FindUnique<Canvas>(scene, component => component.name == "Canvas");
            var solved = FindByLocalId<TMP_Text>(scene, 966661745);
            var failed = FindByLocalId<TMP_Text>(scene, 1981654951);
            var backToLobby = FindByLocalId<Button>(scene, 1738518265);
            var roomCount = FindUnique<TMP_Text>(scene, IsRoomCountText);
            var controller = GetOrAdd<ResultSceneController>(canvas.gameObject);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("roomCountText").objectReferenceValue = roomCount;
            serialized.FindProperty("solvedDemandCountText").objectReferenceValue = solved;
            serialized.FindProperty("failedDemandCountText").objectReferenceValue = failed;
            serialized.FindProperty("backToLobbyButton").objectReferenceValue = backToLobby;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static bool IsRoomCountText(TMP_Text text)
        {
            if (text.name != "Count" || text.transform.parent == null)
            {
                return false;
            }

            var current = text.transform.parent;
            while (current != null)
            {
                if (current.name == "UI_RoomInfo")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void ConfigureBuildScenes()
        {
            var orderedPaths = new[]
            {
                DemoSceneNames.LobbyPath,
                DemoSceneNames.InfiniteModePath,
                DemoSceneNames.ResultPath,
            };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in orderedPaths)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            scenes.AddRange(EditorBuildSettings.scenes.Where(scene =>
                !orderedPaths.Contains(scene.path, StringComparer.Ordinal)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T GetOrAdd<T>(GameObject target)
            where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private static T FindByLocalId<T>(Scene scene, long localId)
            where T : Component
        {
            return FindUnique<T>(scene, component =>
                GlobalObjectId.GetGlobalObjectIdSlow(component).targetObjectId == (ulong)localId);
        }

        private static T FindUnique<T>(Scene scene, Func<T, bool> predicate)
            where T : Component
        {
            var matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Where(predicate)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {typeof(T).Name} in {scene.path}, found {matches.Length}.");
            }

            return matches[0];
        }
    }
}
