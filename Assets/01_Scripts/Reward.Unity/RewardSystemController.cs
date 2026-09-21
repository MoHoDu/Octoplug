using System.Collections.Generic;
using Octoplug.GameFlow.Unity;
using Octoplug.Power;
using Octoplug.Power.Grid;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;

namespace Octoplug.Reward.Unity
{
    public class RewardSystemController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameFlowManager gameFlowManager;
        [SerializeField] private RewardUiController rewardUi;
        [SerializeField] private RewardTargetSelectionController targetSelection;
        [SerializeField] private HousePowerBudget housePowerBudget;
        [SerializeField] private ProductionRoomGenerationController roomGeneration;
        [SerializeField] private CableRoutingGridService routingGrid;
        [SerializeField] private PowerStrip powerStripPrefab;
        [SerializeField] private Transform powerStripParent;

        private SceneRewardContext context;
        private RewardPlacementService placementService;
        private bool phaseActive;
        private int cycle;

        private void OnEnable()
        {
            if (gameFlowManager != null)
            {
                gameFlowManager.RewardPhaseRequested += HandleRewardPhaseRequested;
            }
        }

        private void OnDisable()
        {
            if (gameFlowManager != null)
            {
                gameFlowManager.RewardPhaseRequested -= HandleRewardPhaseRequested;
            }

            cycle++;
            phaseActive = false;
            rewardUi?.Hide();
            targetSelection?.CancelSelection();
        }

        private void HandleRewardPhaseRequested()
        {
            cycle++;
            var currentCycle = cycle;
            phaseActive = true;
            targetSelection?.CancelSelection();
            rewardUi?.Hide();

            ResolvePlacementDependencies();
            placementService = new RewardPlacementService(
                roomGeneration,
                routingGrid,
                powerStripPrefab,
                powerStripParent);

            context = new SceneRewardContext(
                roomGeneration,
                housePowerBudget,
                placementService);

            var candidates = RewardCandidateGenerator.GenerateCandidates(
                DefaultRewardBalance.GetDefaultRewards(),
                context,
                3,
                new System.Random());

            if (candidates.Count != 3)
            {
                Debug.LogError(
                    $"Reward phase requires exactly three applicable rewards, but found {candidates.Count}.",
                    this);
                return;
            }

            if (rewardUi == null || !rewardUi.Show(
                    candidates,
                    reward => HandleRewardSelected(currentCycle, reward),
                    () => HandlePass(currentCycle)))
            {
                Debug.LogError("Reward UI is missing or incomplete. Reward phase remains paused.", this);
            }
        }

        private void HandleRewardSelected(int selectedCycle, RewardBalanceRecord reward)
        {
            if (!IsCurrentCycle(selectedCycle))
            {
                return;
            }

            if (reward.TargetType == RewardTargetType.None)
            {
                if (TryApplyReward(reward, null))
                {
                    CompletePhase(selectedCycle);
                }
                return;
            }

            if (targetSelection == null)
            {
                Debug.LogError("Targeted reward cannot start without RewardTargetSelectionController.", this);
                return;
            }

            rewardUi.Hide();
            targetSelection.BeginSelection(reward, context, target =>
            {
                if (!IsCurrentCycle(selectedCycle) || target == null)
                {
                    return false;
                }

                if (!TryApplyReward(reward, target))
                {
                    return false;
                }

                CompletePhase(selectedCycle);
                return true;
            }, placementService);
        }

        private void HandlePass(int selectedCycle)
        {
            if (!IsCurrentCycle(selectedCycle))
            {
                return;
            }

            CompletePhase(selectedCycle);
        }

        private bool TryApplyReward(RewardBalanceRecord reward, object target)
        {
            var strip = target as PowerStrip;
            if (!RewardCandidateGenerator.AreEffectsApplicable(reward.Effects, context, target))
            {
                Debug.LogError($"Reward {reward.Id} is no longer applicable.", this);
                return false;
            }

            var orderedEffects = GetApplicationOrder(reward.Effects);
            for (var index = 0; index < orderedEffects.Count; index++)
            {
                var effect = orderedEffects[index];
                switch (effect.EffectType)
                {
                    case RewardEffectType.HouseAllowedPower:
                        housePowerBudget.SetAllowedPowerWatts(
                            housePowerBudget.AllowedPowerWatts + effect.Value);
                        break;
                    case RewardEffectType.PowerStripSocketCount:
                        if (!strip.TryUpgradeActiveSocketCount(effect.Value, out var socketFailure))
                        {
                            return LogApplicationFailure(reward, socketFailure.ToString());
                        }
                        break;
                    case RewardEffectType.PowerStripAllowedPower:
                        if (!strip.TryUpgradeAllowedPowerWatts(out var powerFailure))
                        {
                            return LogApplicationFailure(reward, powerFailure.ToString());
                        }
                        break;
                    case RewardEffectType.CableLength:
                        var cableTarget = target as CableOwnerRewardTarget;
                        if (cableTarget?.Cable == null)
                        {
                            return LogApplicationFailure(
                                reward,
                                "Cable target is unavailable");
                        }

                        if (!cableTarget.Cable.TryUpgradeCableLength(
                                out var cableFailure))
                        {
                            return LogApplicationFailure(
                                reward,
                                cableFailure.ToString());
                        }
                        break;
                    case RewardEffectType.AddPowerStrip:
                        if (!TryRollSocketCount(
                                DefaultSocketCountBalance.CreateMultitapCatalog(),
                                1,
                                5,
                                out var powerStripSocketCount))
                        {
                            return LogApplicationFailure(
                                reward,
                                "Multitap socket roll failed");
                        }

                        if (!placementService.TryPlacePowerStrip(
                                target as RoomPlacementRewardTarget,
                                powerStripSocketCount,
                                out _,
                                out var powerStripFailure))
                        {
                            return LogApplicationFailure(
                                reward,
                                powerStripFailure);
                        }
                        break;
                    default:
                        return LogApplicationFailure(reward, $"Unsupported effect {effect.EffectType}");
                }
            }

            return true;
        }

        private static List<RewardEffect> GetApplicationOrder(IReadOnlyList<RewardEffect> effects)
        {
            var ordered = new List<RewardEffect>(effects.Count);
            for (var index = 0; index < effects.Count; index++)
            {
                if (effects[index].EffectType == RewardEffectType.PowerStripSocketCount)
                {
                    ordered.Add(effects[index]);
                }
            }

            for (var index = 0; index < effects.Count; index++)
            {
                if (effects[index].EffectType != RewardEffectType.PowerStripSocketCount)
                {
                    ordered.Add(effects[index]);
                }
            }

            return ordered;
        }

        private static bool TryRollSocketCount(
            SocketCountWeightCatalog catalog,
            int minimum,
            int maximum,
            out int socketCount)
        {
            var selector = new WeightedSocketCountSelector(catalog);
            var totalWeight = selector.GetTotalWeight(minimum, maximum);
            socketCount = selector.Select(
                minimum,
                maximum,
                UnityEngine.Random.Range(0, totalWeight));
            return socketCount >= minimum && socketCount <= maximum;
        }

        private void ResolvePlacementDependencies()
        {
            routingGrid ??= CableRoutingGridService.Instance;
            powerStripParent ??= GameObject.Find("Multitaps")?.transform;
        }

        private bool LogApplicationFailure(RewardBalanceRecord reward, string failure)
        {
            Debug.LogError($"Failed to apply reward {reward.Id}: {failure}.", this);
            return false;
        }

        private bool IsCurrentCycle(int selectedCycle)
        {
            return phaseActive && selectedCycle == cycle;
        }

        private void CompletePhase(int selectedCycle)
        {
            if (!IsCurrentCycle(selectedCycle))
            {
                return;
            }

            phaseActive = false;
            rewardUi?.Hide();
            targetSelection?.CancelSelection();
            gameFlowManager.CompleteRewardPhase();
        }
    }
}
