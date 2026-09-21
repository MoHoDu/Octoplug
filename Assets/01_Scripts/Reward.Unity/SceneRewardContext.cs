using System.Collections.Generic;
using Octoplug.Power;
using Octoplug.RoomGeneration.Unity;

namespace Octoplug.Reward.Unity
{
    public class SceneRewardContext : IRewardContext
    {
        private readonly ProductionRoomGenerationController roomGeneration;
        private readonly HousePowerBudget housePowerBudget;
        private readonly RewardPlacementService placementService;

        public SceneRewardContext(
            ProductionRoomGenerationController roomGeneration,
            HousePowerBudget housePowerBudget,
            RewardPlacementService placementService)
        {
            this.roomGeneration = roomGeneration;
            this.housePowerBudget = housePowerBudget;
            this.placementService = placementService;
        }

        public int CurrentRoomCount => roomGeneration != null && roomGeneration.IsInitialized
            ? roomGeneration.State.UnlockedLayout.Rooms.Count
            : 1;

        public float CurrentHouseAllowedPower => housePowerBudget != null
            ? housePowerBudget.AllowedPowerWatts
            : float.PositiveInfinity;

        public float MaxHouseAllowedPower => housePowerBudget != null
            ? housePowerBudget.MaxAllowedPowerWatts
            : float.NegativeInfinity;

        public IEnumerable<PowerStrip> GetPowerStrips()
        {
            foreach (var strip in RuntimeWorldRegistry.GetPowerStrips())
            {
                yield return strip;
            }
        }

        public IEnumerable<CableOwnerRewardTarget> GetCableOwners()
        {
            foreach (var strip in RuntimeWorldRegistry.GetPowerStrips())
            {
                if (strip.Cable != null && strip.Cable.CanUpgradeCableLength())
                {
                    yield return new CableOwnerRewardTarget(strip, strip.Cable);
                }
            }

            foreach (var product in RuntimeWorldRegistry.GetProducts())
            {
                if (product.Cable != null && product.Cable.CanUpgradeCableLength())
                {
                    yield return new CableOwnerRewardTarget(product, product.Cable);
                }
            }
        }

        public IEnumerable<RoomPlacementRewardTarget> GetRoomPlacements() =>
            placementService != null
                ? placementService.GetEligibleRooms()
                : System.Array.Empty<RoomPlacementRewardTarget>();
    }
}
