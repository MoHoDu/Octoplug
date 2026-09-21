namespace Octoplug.RoomGeneration
{
    public sealed class SocketCountWeightAuthoringRow
    {
        public SocketCountWeightAuthoringRow(int socketCount, int weight)
        {
            SocketCount = socketCount;
            Weight = weight;
        }

        public int SocketCount { get; }
        public int Weight { get; }
    }
}
