using System.Linq;
using NUnit.Framework;
using Octoplug.GameFlow;
using Octoplug.GameFlow.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Octoplug.Tests.Editor.GameFlow
{
    public sealed class DemoSceneFlowSceneTests
    {
        [Test]
        public void BuildSettings_EnableDemoFlowInOrderAndPreserveSampleScene()
        {
            var scenes = EditorBuildSettings.scenes;

            Assert.That(scenes.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(scenes.Take(4).Select(scene => scene.path), Is.EqualTo(new[]
            {
                DemoSceneNames.LobbyPath,
                DemoSceneNames.InfiniteModePath,
                DemoSceneNames.ResultPath,
                "Assets/00_Scenes/SampleScene.unity",
            }));
            Assert.That(scenes.Take(4).All(scene => scene.enabled), Is.True);
            Assert.That(scenes.Select(scene => scene.path).Distinct().Count(),
                Is.EqualTo(scenes.Length));
        }

        [Test]
        public void Lobby_BindsExistingStartSurveyAndExitButtons()
        {
            var scene = EditorSceneManager.OpenScene(DemoSceneNames.LobbyPath);
            var controller = FindUnique<LobbySceneController>(scene);
            var serialized = new SerializedObject(controller);

            Assert.That(
                LocalId(serialized.FindProperty("infiniteModeButton").objectReferenceValue),
                Is.EqualTo(308002797));
            Assert.That(
                LocalId(serialized.FindProperty("surveyButton").objectReferenceValue),
                Is.EqualTo(1920432204));
            Assert.That(serialized.FindProperty("surveyButton").objectReferenceValue.name,
                Is.EqualTo("Servey"));
            Assert.That(
                LocalId(serialized.FindProperty("exitButton").objectReferenceValue),
                Is.EqualTo(1777999832));
            Assert.That(FindByLocalId<Button>(scene, 33045517).onClick.GetPersistentEventCount(),
                Is.Zero);
            Assert.That(FindByLocalId<Button>(scene, 1920432204).onClick.GetPersistentEventCount(),
                Is.Zero);
            AssertNoMissingScripts(scene);
        }

        [Test]
        public void InfiniteMode_BindsOneShotTransitionToExistingControllers()
        {
            var scene = EditorSceneManager.OpenScene(DemoSceneNames.InfiniteModePath);
            var transition = FindUnique<GameOverResultTransition>(scene);
            var serialized = new SerializedObject(transition);

            Assert.That(serialized.FindProperty("gameFlow").objectReferenceValue,
                Is.SameAs(transition.GetComponent<GameFlowManager>()));
            Assert.That(serialized.FindProperty("sessionProgress").objectReferenceValue,
                Is.Not.Null);
            AssertNoMissingScripts(scene);
        }

        [Test]
        public void Result_BindsExistingTextsAndBackButton()
        {
            var scene = EditorSceneManager.OpenScene(DemoSceneNames.ResultPath);
            var controller = FindUnique<ResultSceneController>(scene);
            var serialized = new SerializedObject(controller);
            var roomText = serialized.FindProperty("roomCountText").objectReferenceValue as TMP_Text;

            Assert.That(roomText, Is.Not.Null);
            Assert.That(roomText.name, Is.EqualTo("Count"));
            Assert.That(roomText.transform.parent.parent.name, Is.EqualTo("UI_RoomInfo"));
            Assert.That(
                LocalId(serialized.FindProperty("solvedDemandCountText").objectReferenceValue),
                Is.EqualTo(966661745));
            Assert.That(
                LocalId(serialized.FindProperty("failedDemandCountText").objectReferenceValue),
                Is.EqualTo(1981654951));
            Assert.That(
                LocalId(serialized.FindProperty("backToLobbyButton").objectReferenceValue),
                Is.EqualTo(1738518265));
            Assert.That(FindByLocalId<Button>(scene, 1738518265).onClick.GetPersistentEventCount(),
                Is.Zero);
            AssertNoMissingScripts(scene);
        }

        private static long LocalId(Object target)
        {
            return (long)GlobalObjectId.GetGlobalObjectIdSlow(target).targetObjectId;
        }

        private static T FindUnique<T>(Scene scene)
            where T : Component
        {
            var matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1));
            return matches[0];
        }

        private static T FindByLocalId<T>(Scene scene, long localId)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Single(component => LocalId(component) == localId);
        }

        private static void AssertNoMissingScripts(Scene scene)
        {
            var missingCount = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Sum(transform =>
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        transform.gameObject));
            Assert.That(missingCount, Is.Zero);
        }
    }
}
