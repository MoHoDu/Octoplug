namespace Octoplug.RoomGeneration
{
    /// <summary>Policy-selected door center for one calculated opportunity.</summary>
    public readonly struct DoorPlacementProposal
    {
        public DoorPlacementProposal(int opportunityIndex, float centerCoordinate)
        {
            OpportunityIndex = opportunityIndex;
            CenterCoordinate = centerCoordinate;
        }

        public int OpportunityIndex { get; }
        public float CenterCoordinate { get; }
    }
}
