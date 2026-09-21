using System;
using System.Collections;
using Octoplug.CameraFraming.Unity;
using Octoplug.ResidentDemand.Unity;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.GameFlow.Unity
{
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private SessionProgressController sessionProgress;
        [SerializeField] private ProductionRoomGenerationController roomGeneration;
        [SerializeField] private ResidentDemandController residentDemand;
        [SerializeField] private RoomGenerationCameraFramingBridge cameraBridge;

        [Header("Debug Visibility")]
        [SerializeField] private GameFlowState currentState = GameFlowState.Playing;
        [SerializeField] private int globalSatisfaction;
        [SerializeField] private int currentExp;
        [SerializeField] private int requiredExp;
        [SerializeField] private int currentRoomCount;
        [SerializeField] private bool progressionInProgress;
        [SerializeField] private bool rewardRequested;
        [SerializeField] private bool isGameOver;

        public event Action RewardPhaseRequested;
        public event Action GameOverRequested;

        public GameFlowState CurrentState => currentState;

        public void InitializeForVerification()
        {
            if (sessionProgress != null)
            {
                sessionProgress.ExperienceThresholdReached -= HandleExperienceThresholdReached;
                sessionProgress.SatisfactionDepleted -= HandleSatisfactionDepleted;

                sessionProgress.ExperienceThresholdReached += HandleExperienceThresholdReached;
                sessionProgress.SatisfactionDepleted += HandleSatisfactionDepleted;
            }
            if (cameraBridge != null)
            {
                cameraBridge.CameraRevealCompleted -= HandleCameraRevealCompleted;
                cameraBridge.CameraRevealCompleted += HandleCameraRevealCompleted;
            }
        }

        private void OnEnable()
        {
            if (sessionProgress != null)
            {
                sessionProgress.ExperienceThresholdReached += HandleExperienceThresholdReached;
                sessionProgress.SatisfactionDepleted += HandleSatisfactionDepleted;
            }
            if (cameraBridge != null)
            {
                cameraBridge.CameraRevealCompleted += HandleCameraRevealCompleted;
            }
        }

        private void OnDisable()
        {
            if (sessionProgress != null)
            {
                sessionProgress.ExperienceThresholdReached -= HandleExperienceThresholdReached;
                sessionProgress.SatisfactionDepleted -= HandleSatisfactionDepleted;
            }
            if (cameraBridge != null)
            {
                cameraBridge.CameraRevealCompleted -= HandleCameraRevealCompleted;
            }
        }

        private void Update()
        {
            if (sessionProgress != null && sessionProgress.IsInitialized)
            {
                globalSatisfaction = sessionProgress.GlobalSatisfaction;
                currentExp = sessionProgress.CurrentExperience;
                requiredExp = sessionProgress.RequiredExperience;
                currentRoomCount = sessionProgress.RoomCount;
            }

            progressionInProgress = currentState == GameFlowState.RoomProgression ||
                                    currentState == GameFlowState.CameraReveal ||
                                    currentState == GameFlowState.RewardDelay;
            rewardRequested = currentState == GameFlowState.AwaitingReward;
            isGameOver = currentState == GameFlowState.GameOver;

            bool shouldLock = currentState == GameFlowState.RewardDelay ||
                              currentState == GameFlowState.AwaitingReward ||
                              currentState == GameFlowState.GameOver;

            if (!shouldLock && Octoplug.GameFlow.GameplayInputLock.IsLocked)
            {
                Octoplug.GameFlow.GameplayInputLock.SuppressUntilPointerRelease = true;
            }

            Octoplug.GameFlow.GameplayInputLock.IsLocked = shouldLock;
        }

        private void HandleExperienceThresholdReached()
        {
            if (currentState != GameFlowState.Playing)
            {
                return;
            }

            currentState = GameFlowState.RoomProgression;

            if (roomGeneration != null && roomGeneration.State.HasNextRoomPlan)
            {
                var nextRoom = roomGeneration.State.NextRoomPlan.Room;
                bool promoted = roomGeneration.PromoteCurrentHint();

                if (promoted)
                {
                    if (sessionProgress != null)
                    {
                        sessionProgress.AcknowledgeExperienceThreshold(
                            roomGeneration.State.UnlockedLayout.Rooms.Count,
                            resetExperience: true);
                    }

                    if (residentDemand != null)
                    {
                        residentDemand.AddResident();
                    }

                    currentState = GameFlowState.CameraReveal;
                    if (cameraBridge != null)
                    {
                        cameraBridge.RequestRoomReveal(nextRoom);
                    }
                    else
                    {
                        HandleCameraRevealCompleted();
                    }
                }
                else
                {
                    currentState = GameFlowState.Playing;
                }
            }
            else
            {
                currentState = GameFlowState.Playing;
            }
        }

        private void HandleCameraRevealCompleted()
        {
            if (currentState != GameFlowState.CameraReveal)
            {
                return;
            }

            currentState = GameFlowState.RewardDelay;
            StartCoroutine(RewardDelayRoutine());
        }

        private IEnumerator RewardDelayRoutine()
        {
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(1f);

            currentState = GameFlowState.AwaitingReward;
            RewardPhaseRequested?.Invoke();
        }

        public void CompleteRewardPhase()
        {
            if (currentState != GameFlowState.AwaitingReward)
            {
                Debug.LogWarning("CompleteRewardPhase called but state is not AwaitingReward.", this);
                return;
            }

            Time.timeScale = 1f;
            currentState = GameFlowState.Playing;

            // Trigger next progression if EXP threshold was reached during the previous progression or reward phase
            if (sessionProgress != null && sessionProgress.IsInitialized && sessionProgress.CurrentExperience >= sessionProgress.RequiredExperience)
            {
                HandleExperienceThresholdReached();
            }
        }

        private void HandleSatisfactionDepleted()
        {
            if (currentState == GameFlowState.GameOver) return;

            currentState = GameFlowState.GameOver;
            GameOverRequested?.Invoke();
        }

        [ContextMenu("Debug/Add EXP")]
        public void DebugAddExp()
        {
            if (!Application.isPlaying || sessionProgress == null) return;

            // Hacky debug method to simulate demand outcome success to gain EXP
            var debugDemand = new Octoplug.ResidentDemand.DemandBalanceRecord(
                "debug-add-exp", true, 1, 1, new[] { Octoplug.ResidentDemand.ResidentNeedType.Cooling },
                1f, 1f, requiredExp, 0, 0, 1f);

            sessionProgress.ApplyOutcomeForVerification(new Octoplug.ResidentDemand.DemandOutcome(
                debugDemand,
                Octoplug.ResidentDemand.DemandResolution.Success
            ));
        }

        [ContextMenu("Debug/Complete Reward Phase")]
        public void DebugCompleteRewardPhase()
        {
            if (!Application.isPlaying) return;
            CompleteRewardPhase();
        }
    }
}
