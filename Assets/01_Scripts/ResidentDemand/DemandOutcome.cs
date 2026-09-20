namespace Octoplug.ResidentDemand
{
    public sealed class DemandOutcome
    {
        public DemandOutcome(DemandBalanceRecord demand, DemandResolution resolution)
        {
            Demand = demand;
            Resolution = resolution;
        }

        public DemandBalanceRecord Demand { get; }
        public DemandResolution Resolution { get; }
        public int ExperienceReward => Resolution == DemandResolution.Success ? Demand.ExperienceReward : 0;
        public int GlobalSatisfactionDelta => Resolution == DemandResolution.Success
            ? Demand.GlobalSatisfactionOnSuccess
            : Demand.GlobalSatisfactionOnFailure;
    }
}
