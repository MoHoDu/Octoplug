namespace Octoplug.ResidentDemand
{
    public static class DefaultDemandBalance
    {
        public static DemandBalanceCatalog Create()
        {
            return DemandBalanceCatalog.FromAuthoringRows(new[]
            {
                Row("D001", true, 1, 10, "Cold", null, 6f, 10f, 10, 5, -8, 4f),
                Row("D002", true, 1, 10, "Fun", null, 6f, 10f, 10, 5, -8, 4f),
                Row("D003", true, 2, 9, "Food", null, 7f, 9f, 12, 5, -8, 4f),
                Row("D004", true, 2, 9, "Hot", null, 7f, 9f, 12, 5, -8, 4f),
                Row("D005", true, 15, 6, "Cold", "Fun", 6f, 8f, 18, 7, -10, 5f),
                Row("D006", true, 15, 6, "Food", "Hot", 6f, 8f, 20, 7, -10, 5f),
                Row("D007", true, 15, 4, "Cold", "Food", 5f, 7f, 22, 8, -12, 5f),
                Row("D008", true, 15, 4, "Hot", "Fun", 5f, 7f, 22, 8, -12, 5f),
            });
        }

        private static DemandAuthoringRow Row(
            string id,
            bool enabled,
            int requiredRoomCount,
            int weight,
            string firstNeed,
            string secondNeed,
            float satisfactionFillSeconds,
            float patienceFillSeconds,
            int experienceReward,
            int successDelta,
            int failureDelta,
            float cooldownSeconds)
        {
            return new DemandAuthoringRow(
                id,
                enabled,
                requiredRoomCount,
                weight,
                firstNeed,
                secondNeed,
                satisfactionFillSeconds,
                patienceFillSeconds,
                experienceReward,
                successDelta,
                failureDelta,
                cooldownSeconds);
        }
    }
}
