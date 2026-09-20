namespace Octoplug.RoomGeneration
{
    /// <summary>Structured reason an ordered candidate could not produce a plan.</summary>
    public readonly struct RoomCandidateRejectionRecord
    {
        public RoomCandidateRejectionRecord(int candidateIndex, RoomId roomId, RoomCandidateRejection reason)
        {
            CandidateIndex = candidateIndex;
            RoomId = roomId;
            Reason = reason;
        }

        public int CandidateIndex { get; }
        public RoomId RoomId { get; }
        public RoomCandidateRejection Reason { get; }
    }
}
