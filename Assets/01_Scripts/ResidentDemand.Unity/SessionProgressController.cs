using System;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class SessionProgressController : MonoBehaviour
    {
        [SerializeField]
        private ResidentDemandController residentDemand;

        [SerializeField]
        private ProductionRoomGenerationController roomGeneration;

        [SerializeField]
        private SessionProgressConfig config;

        private SessionProgressState _state;
        private bool _subscribed;

        public event Action ProgressChanged;
        public event Action SatisfactionDepleted;
        public event Action ExperienceThresholdReached;

        public bool IsInitialized => _state != null;
        public int GlobalSatisfaction => RequireState().GlobalSatisfaction;
        public int CurrentExperience => RequireState().Experience;
        public int RequiredExperience => RequireState().RequiredExperience;
        public int RoomCount => RequireState().RoomCount;
        public int SolvedDemandCount => RequireState().SolvedDemandCount;
        public int FailedDemandCount => RequireState().FailedDemandCount;
        public bool IsSatisfactionDepleted => RequireState().IsSatisfactionDepleted;
        public float SatisfactionNormalized => RequireState().SatisfactionNormalized;
        public float ExperienceNormalized => RequireState().ExperienceNormalized;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            TryInitialize();
        }

        private void Update()
        {
            if (_state == null)
            {
                TryInitialize();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public bool TryInitialize()
        {
            if (_state != null)
            {
                return true;
            }

            if (residentDemand == null || roomGeneration == null || config == null ||
                !roomGeneration.IsInitialized)
            {
                return false;
            }

            Initialize(
                config.InitialGlobalSatisfaction,
                0,
                roomGeneration.State.UnlockedLayout.Rooms.Count,
                config.CreateRequiredExperienceTable());
            return true;
        }

        public void InitializeForVerification(
            ResidentDemandController demandController,
            int initialGlobalSatisfaction,
            int initialExperience,
            int roomCount,
            RequiredExperienceTable requiredExperience)
        {
            residentDemand = demandController != null
                ? demandController
                : throw new ArgumentNullException(nameof(demandController));
            Subscribe();
            Initialize(
                initialGlobalSatisfaction,
                initialExperience,
                roomCount,
                requiredExperience);
        }

        public void AcknowledgeExperienceThreshold(int newRoomCount, bool resetExperience)
        {
            RequireState().AcknowledgeExperienceThreshold(newRoomCount, resetExperience);
            ProgressChanged?.Invoke();
        }

        public void ApplyOutcomeForVerification(DemandOutcome outcome)
        {
            HandleDemandResolved(outcome);
        }

        [ContextMenu("Debug/Inspect Session Progress")]
        public void InspectProgressFromInspector()
        {
            if (!Application.isPlaying || _state == null)
            {
                Debug.LogWarning("Session progress is available only after Play Mode initialization.", this);
                return;
            }

            Debug.Log(
                $"Session Progress: satisfaction={GlobalSatisfaction}/100, " +
                $"experience={CurrentExperience}/{RequiredExperience}, rooms={RoomCount}, " +
                $"depleted={IsSatisfactionDepleted}.",
                this);
        }

        private void Initialize(
            int initialGlobalSatisfaction,
            int initialExperience,
            int roomCount,
            RequiredExperienceTable requiredExperience)
        {
            if (_state != null)
            {
                throw new InvalidOperationException("Session Progress is already initialized.");
            }

            _state = new SessionProgressState(
                initialGlobalSatisfaction,
                initialExperience,
                roomCount,
                requiredExperience);
            _state.SatisfactionDepleted += HandleSatisfactionDepleted;
            _state.ExperienceThresholdReached += HandleExperienceThresholdReached;
            ProgressChanged?.Invoke();
        }

        private void Subscribe()
        {
            if (_subscribed || residentDemand == null)
            {
                return;
            }

            residentDemand.DemandResolved += HandleDemandResolved;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || residentDemand == null)
            {
                return;
            }

            residentDemand.DemandResolved -= HandleDemandResolved;
            _subscribed = false;
        }

        private void HandleDemandResolved(DemandOutcome outcome)
        {
            if (_state == null)
            {
                Debug.LogError("Session Progress received a Demand outcome before initialization.", this);
                return;
            }

            var previousSatisfaction = _state.GlobalSatisfaction;
            var previousExperience = _state.Experience;
            _state.Apply(outcome);
            if (previousSatisfaction != _state.GlobalSatisfaction ||
                previousExperience != _state.Experience)
            {
                ProgressChanged?.Invoke();
            }
        }

        private void HandleSatisfactionDepleted()
        {
            SatisfactionDepleted?.Invoke();
        }

        private void HandleExperienceThresholdReached()
        {
            ExperienceThresholdReached?.Invoke();
        }

        private SessionProgressState RequireState()
        {
            return _state ?? throw new InvalidOperationException(
                "Session Progress has not initialized.");
        }
    }
}
