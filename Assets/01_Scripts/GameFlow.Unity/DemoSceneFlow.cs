using System;
using Octoplug.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Octoplug.GameFlow.Unity
{
    public static class DemoSceneFlow
    {
        private static Action<string> loadScene = SceneManager.LoadScene;

        public static void StartNewSession()
        {
            SessionResultStore.Clear();
            GameplayInputLock.IsLocked = false;
            Time.timeScale = 1f;
            loadScene(DemoSceneNames.InfiniteMode);
        }

        public static void ShowResult()
        {
            loadScene(DemoSceneNames.Result);
        }

        public static void ReturnToLobby()
        {
            loadScene(DemoSceneNames.Lobby);
        }

        public static void InitializeForVerification(Action<string> sceneLoader)
        {
            loadScene = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        }

        public static void ResetVerification()
        {
            loadScene = SceneManager.LoadScene;
            SessionResultStore.Clear();
            GameplayInputLock.IsLocked = false;
            Time.timeScale = 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            loadScene = SceneManager.LoadScene;
            GameplayInputLock.IsLocked = false;
            Time.timeScale = 1f;
        }
    }
}
