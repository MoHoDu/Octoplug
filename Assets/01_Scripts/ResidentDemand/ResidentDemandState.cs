using System;

namespace Octoplug.ResidentDemand
{
    public sealed class ResidentDemandState
    {
        private const float CompletionEpsilon = 0.00001f;

        private readonly ResidentNumber _residentNumber;
        private DemandBalanceRecord _demand;
        private bool[] _completedNeeds;
        private int _activeNeedIndex;
        private float _cooldownRemaining;

        public ResidentDemandState(ResidentNumber residentNumber)
        {
            _residentNumber = residentNumber;
            Status = ResidentDemandStatus.None;
        }

        public ResidentNumber ResidentNumber => _residentNumber;
        public DemandBalanceRecord Demand => _demand;
        public ResidentDemandStatus Status { get; private set; }
        public float SatisfactionProgress { get; private set; }
        public float PatienceProgress { get; private set; }
        public DemandOutcome LastOutcome { get; private set; }
        public int ActiveNeedIndex => _activeNeedIndex;
        public float CooldownRemaining => _cooldownRemaining;

        public int NeedCount => _completedNeeds?.Length ?? 0;
        public int CompletedNeedCount
        {
            get
            {
                if (_completedNeeds == null)
                {
                    return 0;
                }

                var count = 0;
                foreach (var completed in _completedNeeds)
                {
                    if (completed)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void StartDemand(DemandBalanceRecord demand)
        {
            if (demand == null)
            {
                throw new ArgumentNullException(nameof(demand));
            }

            if (Status != ResidentDemandStatus.None)
            {
                throw new InvalidOperationException("A demand can start only when the resident is idle.");
            }

            _demand = demand;
            _completedNeeds = new bool[demand.Needs.Count];
            _activeNeedIndex = 0;
            SatisfactionProgress = 0f;
            PatienceProgress = 0f;
            LastOutcome = null;
            Status = ResidentDemandStatus.Waiting;
        }

        public ResidentNeedType GetNeed(int index)
        {
            EnsureActiveDemand();
            return _demand.Needs[index];
        }

        public bool IsNeedCompleted(int index)
        {
            EnsureActiveDemand();
            return _completedNeeds[index];
        }

        public void SetUsing(bool isUsing)
        {
            if (Status != ResidentDemandStatus.Waiting && Status != ResidentDemandStatus.Using)
            {
                return;
            }

            Status = isUsing ? ResidentDemandStatus.Using : ResidentDemandStatus.Waiting;
        }

        public DemandOutcome Advance(float deltaSeconds)
        {
            RequireDelta(deltaSeconds);

            if (Status == ResidentDemandStatus.Cooldown)
            {
                AdvanceCooldown(deltaSeconds);
                return null;
            }

            if (Status != ResidentDemandStatus.Waiting && Status != ResidentDemandStatus.Using)
            {
                return null;
            }

            var completedFloor = CompletedNeedCount / (float)NeedCount;
            if (Status == ResidentDemandStatus.Using)
            {
                SatisfactionProgress = Clamp01(
                    SatisfactionProgress +
                    (deltaSeconds / (_demand.SatisfactionFillSeconds * NeedCount)));
                PatienceProgress = Clamp01(
                    PatienceProgress - (deltaSeconds / _demand.PatienceFillSeconds));

                var activeNeedCeiling = (_activeNeedIndex + 1f) / NeedCount;
                if (SatisfactionProgress + CompletionEpsilon >= activeNeedCeiling)
                {
                    SatisfactionProgress = activeNeedCeiling;
                    _completedNeeds[_activeNeedIndex] = true;
                    if (CompletedNeedCount == NeedCount)
                    {
                        return Resolve(DemandResolution.Success);
                    }

                    _activeNeedIndex = FindNextIncompleteNeed();
                    Status = ResidentDemandStatus.Waiting;
                }
            }
            else
            {
                SatisfactionProgress = Math.Max(
                    completedFloor,
                    SatisfactionProgress -
                    (deltaSeconds / (_demand.SatisfactionFillSeconds * NeedCount)));
                PatienceProgress = Clamp01(
                    PatienceProgress + (deltaSeconds / _demand.PatienceFillSeconds));

                if (PatienceProgress + CompletionEpsilon >= 1f)
                {
                    PatienceProgress = 1f;
                    return Resolve(DemandResolution.Failure);
                }
            }

            return null;
        }

        private DemandOutcome Resolve(DemandResolution resolution)
        {
            var resolvedDemand = _demand;
            LastOutcome = new DemandOutcome(resolvedDemand, resolution);
            _cooldownRemaining = resolvedDemand.CooldownSeconds;
            Status = _cooldownRemaining > 0f ? ResidentDemandStatus.Cooldown : ResidentDemandStatus.None;
            _demand = null;
            _completedNeeds = null;
            _activeNeedIndex = 0;
            SatisfactionProgress = 0f;
            PatienceProgress = 0f;
            return LastOutcome;
        }

        private void AdvanceCooldown(float deltaSeconds)
        {
            _cooldownRemaining = Math.Max(0f, _cooldownRemaining - deltaSeconds);
            if (_cooldownRemaining <= CompletionEpsilon)
            {
                _cooldownRemaining = 0f;
                Status = ResidentDemandStatus.None;
            }
        }

        private int FindNextIncompleteNeed()
        {
            for (var index = 0; index < _completedNeeds.Length; index++)
            {
                if (!_completedNeeds[index])
                {
                    return index;
                }
            }

            throw new InvalidOperationException("No incomplete need remains.");
        }

        private void EnsureActiveDemand()
        {
            if (_demand == null)
            {
                throw new InvalidOperationException("The resident has no active demand.");
            }
        }

        private static void RequireDelta(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
