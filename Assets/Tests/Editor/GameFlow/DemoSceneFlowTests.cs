using System;
using System.Collections.Generic;
using NUnit.Framework;
using Octoplug.GameFlow;
using Octoplug.GameFlow.Unity;
using Octoplug.ResidentDemand;
using Octoplug.ResidentDemand.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Octoplug.Tests.Editor.GameFlow
{
    public sealed class DemoSceneFlowTests
    {
        private readonly List<Object> objects = new();

        [SetUp]
        public void SetUp()
        {
            DemoSceneFlow.ResetVerification();
        }

        [TearDown]
        public void TearDown()
        {
            DemoSceneFlow.ResetVerification();
            for (var index = objects.Count - 1; index >= 0; index--)
            {
                if (objects[index] != null)
                {
                    Object.DestroyImmediate(objects[index]);
                }
            }

            objects.Clear();
        }

        [Test]
        public void SessionResultSnapshot_ValidatesAndStoresValues()
        {
            var snapshot = new SessionResultSnapshot(6, 10, 3);

            Assert.That(snapshot.FinalRoomCount, Is.EqualTo(6));
            Assert.That(snapshot.SolvedDemandCount, Is.EqualTo(10));
            Assert.That(snapshot.FailedDemandCount, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SessionResultSnapshot(0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SessionResultSnapshot(1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SessionResultSnapshot(1, 0, -1));
        }

        [Test]
        public void SessionResultStore_PublishesReadsAndClearsSnapshot()
        {
            var expected = new SessionResultSnapshot(4, 7, 2);

            SessionResultStore.Publish(expected);

            Assert.That(SessionResultStore.TryGet(out var actual), Is.True);
            Assert.That(actual, Is.EqualTo(expected));

            SessionResultStore.Clear();

            Assert.That(SessionResultStore.TryGet(out _), Is.False);
        }

        [Test]
        public void StartNewSession_ClearsTransientStateBeforeLoadingInfiniteMode()
        {
            var loads = new List<string>();
            SessionResultStore.Publish(new SessionResultSnapshot(3, 2, 1));
            GameplayInputLock.IsLocked = true;
            Time.timeScale = 0f;
            DemoSceneFlow.InitializeForVerification(sceneName =>
            {
                Assert.That(SessionResultStore.TryGet(out _), Is.False);
                Assert.That(GameplayInputLock.IsLocked, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                loads.Add(sceneName);
            });

            DemoSceneFlow.StartNewSession();

            Assert.That(loads, Is.EqualTo(new[] { DemoSceneNames.InfiniteMode }));
        }

        [Test]
        public void CentralFlow_UsesCanonicalSceneNames()
        {
            var loads = new List<string>();
            DemoSceneFlow.InitializeForVerification(loads.Add);

            DemoSceneFlow.ShowResult();
            DemoSceneFlow.ReturnToLobby();

            Assert.That(loads, Is.EqualTo(new[]
            {
                DemoSceneNames.Result,
                DemoSceneNames.Lobby,
            }));
            Assert.That(DemoSceneNames.LobbyPath,
                Is.EqualTo("Assets/00_Scenes/Demo/Lobby.unity"));
            Assert.That(DemoSceneNames.InfiniteModePath,
                Is.EqualTo("Assets/00_Scenes/Demo/InfiniteMode.unity"));
            Assert.That(DemoSceneNames.ResultPath,
                Is.EqualTo("Assets/00_Scenes/Demo/Result.unity"));
        }

        [Test]
        public void LobbyButtons_InvokeInjectedActionsExactlyOncePerClick()
        {
            var root = Track(new GameObject("Lobby"));
            var controller = root.AddComponent<LobbySceneController>();
            var startButton = CreateButton(root.transform, "InfiniteMode");
            var exitButton = CreateButton(root.transform, "Exit");
            var starts = 0;
            var exits = 0;
            controller.InitializeForVerification(
                startButton,
                exitButton,
                () => starts++,
                () => exits++);

            startButton.onClick.Invoke();
            exitButton.onClick.Invoke();

            Assert.That(starts, Is.EqualTo(1));
            Assert.That(exits, Is.EqualTo(1));
        }

        [Test]
        public void ResultController_BindsSnapshotAndReturnsToLobby()
        {
            var root = Track(new GameObject("Result"));
            root.SetActive(false);
            var controller = root.AddComponent<ResultSceneController>();
            var roomText = CreateText(root.transform, "Room");
            var solvedText = CreateText(root.transform, "Solved");
            var failedText = CreateText(root.transform, "Failed");
            var backButton = CreateButton(root.transform, "BackToLobby");
            var returns = 0;
            controller.InitializeForVerification(
                roomText,
                solvedText,
                failedText,
                backButton,
                () => returns++);

            controller.Bind(new SessionResultSnapshot(5, 8, 2));
            backButton.onClick.Invoke();

            Assert.That(roomText.text, Is.EqualTo("5"));
            Assert.That(solvedText.text, Is.EqualTo("8"));
            Assert.That(failedText.text, Is.EqualTo("2"));
            Assert.That(returns, Is.EqualTo(1));
        }

        [Test]
        public void GameOverTransition_PublishesAuthoritativeSnapshotAndLoadsOnce()
        {
            var demandController = Track(new GameObject("Demand Controller"))
                .AddComponent<ResidentDemandController>();
            demandController.InitializeForVerification(1);
            var progress = Track(new GameObject("Session Progress"))
                .AddComponent<SessionProgressController>();
            progress.InitializeForVerification(
                demandController,
                100,
                0,
                2,
                new RequiredExperienceTable(new[]
                {
                    new RequiredExperienceEntry(2, 30),
                }));
            progress.ApplyOutcomeForVerification(Outcome(DemandResolution.Success));
            progress.ApplyOutcomeForVerification(Outcome(DemandResolution.Failure));

            var gameFlow = Track(new GameObject("Game Flow"))
                .AddComponent<GameFlowManager>();
            var transition = Track(new GameObject("Result Transition"))
                .AddComponent<GameOverResultTransition>();
            var resultLoads = 0;
            transition.InitializeForVerification(
                gameFlow,
                progress,
                () => resultLoads++);
            Time.timeScale = 0f;

            transition.RequestTransitionForVerification();
            transition.RequestTransitionForVerification();

            Assert.That(resultLoads, Is.EqualTo(1));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(SessionResultStore.TryGet(out var snapshot), Is.True);
            Assert.That(snapshot.FinalRoomCount, Is.EqualTo(2));
            Assert.That(snapshot.SolvedDemandCount, Is.EqualTo(1));
            Assert.That(snapshot.FailedDemandCount, Is.EqualTo(1));
        }

        private static DemandOutcome Outcome(DemandResolution resolution)
        {
            return new DemandOutcome(
                new DemandBalanceRecord(
                    "TEST",
                    true,
                    1,
                    1,
                    new[] { ResidentNeedType.Fun },
                    1f,
                    1f,
                    10,
                    5,
                    -8,
                    0f),
                resolution);
        }

        private Button CreateButton(Transform parent, string name)
        {
            var buttonObject = Track(new GameObject(name));
            buttonObject.transform.SetParent(parent);
            return buttonObject.AddComponent<Button>();
        }

        private TMP_Text CreateText(Transform parent, string name)
        {
            var textObject = Track(new GameObject(name));
            textObject.transform.SetParent(parent);
            return textObject.AddComponent<TextMeshProUGUI>();
        }

        private T Track<T>(T target)
            where T : Object
        {
            objects.Add(target);
            return target;
        }
    }
}
