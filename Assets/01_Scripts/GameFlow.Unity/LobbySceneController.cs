using System;
using System.Collections;
using Octoplug.SurveySubmission;
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
        [SerializeField] private Button surveyButton;
        [SerializeField] private Button exitButton;

        private Action startNewSession = DemoSceneFlow.StartNewSession;
        private Action quitApplication = QuitApplication;
        private Func<IEnumerator> openLatestSurvey;
        private bool surveyRequestRunning;

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
            Action quitRequest,
            Button existingSurveyButton = null,
            Func<IEnumerator> surveyRequest = null)
        {
            RemoveListeners();
            infiniteModeButton = startButton != null
                ? startButton
                : throw new ArgumentNullException(nameof(startButton));
            surveyButton = existingSurveyButton;
            exitButton = quitButton != null
                ? quitButton
                : throw new ArgumentNullException(nameof(quitButton));
            startNewSession = startRequest ?? throw new ArgumentNullException(nameof(startRequest));
            quitApplication = quitRequest ?? throw new ArgumentNullException(nameof(quitRequest));
            openLatestSurvey = surveyRequest;
            AddListeners();
        }

        private void AddListeners()
        {
            if (infiniteModeButton != null)
            {
                infiniteModeButton.onClick.AddListener(HandleStartClicked);
            }

            if (surveyButton != null)
            {
                surveyButton.onClick.AddListener(HandleSurveyClicked);
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

            if (surveyButton != null)
            {
                surveyButton.onClick.RemoveListener(HandleSurveyClicked);
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

        private void HandleSurveyClicked()
        {
            if (surveyRequestRunning) return;
            surveyRequestRunning = true;
            if (openLatestSurvey != null)
            {
                StartCoroutine(RunSurvey(openLatestSurvey()));
                return;
            }

            SurveySubmissionRuntime.SubmitLatest(HandleSurveyCompleted);
        }

        private IEnumerator RunSurvey(IEnumerator request)
        {
            if (request != null) yield return request;
            surveyRequestRunning = false;
        }

        private void HandleSurveyCompleted(SurveySubmissionOutcome outcome)
        {
            surveyRequestRunning = false;
            LogSurveyOutcome(outcome);
        }

        private void HandleExitClicked()
        {
            quitApplication();
        }

        private void LogSurveyOutcome(SurveySubmissionOutcome outcome)
        {
            if (outcome == SurveySubmissionOutcome.NoCompletedSession)
            {
                Debug.LogWarning("먼저 게임을 한 판 플레이해주세요.", this);
            }
            else if (outcome != SurveySubmissionOutcome.Opened)
            {
                Debug.LogWarning($"Survey could not be opened ({outcome}).", this);
            }
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
