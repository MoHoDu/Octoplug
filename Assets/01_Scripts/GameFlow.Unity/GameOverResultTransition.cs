using System;
using Octoplug.ResidentDemand.Unity;
using UnityEngine;

namespace Octoplug.GameFlow.Unity
{
    public sealed class GameOverResultTransition : MonoBehaviour
    {
        [SerializeField] private GameFlowManager gameFlow;
        [SerializeField] private SessionProgressController sessionProgress;

        private Action showResult = DemoSceneFlow.ShowResult;
        private bool subscribed;
        private bool transitionRequested;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void InitializeForVerification(
            GameFlowManager flowManager,
            SessionProgressController progressController,
            Action resultRequest)
        {
            Unsubscribe();
            gameFlow = flowManager != null
                ? flowManager
                : throw new ArgumentNullException(nameof(flowManager));
            sessionProgress = progressController != null
                ? progressController
                : throw new ArgumentNullException(nameof(progressController));
            showResult = resultRequest ?? throw new ArgumentNullException(nameof(resultRequest));
            transitionRequested = false;
            Subscribe();
        }

        public void RequestTransitionForVerification()
        {
            HandleGameOverRequested();
        }

        private void Subscribe()
        {
            if (subscribed || gameFlow == null)
            {
                return;
            }

            gameFlow.GameOverRequested += HandleGameOverRequested;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || gameFlow == null)
            {
                return;
            }

            gameFlow.GameOverRequested -= HandleGameOverRequested;
            subscribed = false;
        }

        private void HandleGameOverRequested()
        {
            if (transitionRequested)
            {
                return;
            }

            if (sessionProgress == null || !sessionProgress.IsInitialized)
            {
                Debug.LogError(
                    "Cannot show Result because Session Progress is not initialized.",
                    this);
                return;
            }

            transitionRequested = true;
            SessionResultStore.Publish(new SessionResultSnapshot(
                sessionProgress.RoomCount,
                sessionProgress.SolvedDemandCount,
                sessionProgress.FailedDemandCount));
            Time.timeScale = 1f;
            showResult();
        }
    }
}
