namespace Octoplug.RoomGeneration
{
    public static class DefaultSocketCountBalance
    {
        public static SocketCountWeightCatalog CreateMultitapCatalog()
        {
            return CreateCatalog(40, 30, 15, 10, 5);
        }

        public static SocketCountWeightCatalog CreateWallOutletCatalog()
        {
            return CreateCatalog(60, 25, 10, 4, 1);
        }

        private static SocketCountWeightCatalog CreateCatalog(
            int oneSocketWeight,
            int twoSocketWeight,
            int threeSocketWeight,
            int fourSocketWeight,
            int fiveSocketWeight)
        {
            return SocketCountWeightCatalog.FromAuthoringRows(new[]
            {
                new SocketCountWeightAuthoringRow(1, oneSocketWeight),
                new SocketCountWeightAuthoringRow(2, twoSocketWeight),
                new SocketCountWeightAuthoringRow(3, threeSocketWeight),
                new SocketCountWeightAuthoringRow(4, fourSocketWeight),
                new SocketCountWeightAuthoringRow(5, fiveSocketWeight)
            });
        }
    }
}
