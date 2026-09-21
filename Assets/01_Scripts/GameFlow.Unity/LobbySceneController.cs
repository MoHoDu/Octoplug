using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Octoplug.GameFlow.Unity
{
    public sealed class LobbySceneController : MonoBehaviour
    {
        [SerializeField] private Button infiniteModeButton;
        [SerializeField] private Button exitButton;

        private Action startNewSession = DemoSceneFlow.StartNewSession;
        private Action quitApplication = QuitApplication;

        private void OnEnable()
        {
            AddListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        public void InitializeForVerification(
            Button startButton,
            Button quitButton,
            Action startRequest,
            Action quitRequest)
        {
            RemoveListeners();
            infiniteModeButton = startButton != null
                ? startButton
                : throw new ArgumentNullException(nameof(startButton));
            exitButton = quitButton != null
                ? quitButton
                : throw new ArgumentNullException(nameof(quitButton));
            startNewSession = startRequest ?? throw new ArgumentNullException(nameof(startRequest));
            quitApplication = quitRequest ?? throw new ArgumentNullException(nameof(quitRequest));
            AddListeners();
        }

        private void AddListeners()
        {
            if (infiniteModeButton != null)
            {
                infiniteModeButton.onClick.AddListener(HandleStartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(HandleExitClicked);
            }
        }

        private void RemoveListeners()
        {
            if (infiniteModeButton != null)
            {
                infiniteModeButton.onClick.RemoveListener(HandleStartClicked);
            }

            if (exitButton != null)
            {
                exitButton.onClick.RemoveListener(HandleExitClicked);
            }
        }

        private void HandleStartClicked()
        {
            startNewSession();
        }

        private void HandleExitClicked()
        {
            quitApplication();
        }

        private static void QuitApplication()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
