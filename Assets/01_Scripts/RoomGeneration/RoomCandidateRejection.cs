namespace Octoplug.RoomGeneration
{
    public enum RoomCandidateRejection
    {
        DuplicateRoomId,
        OverlapsExistingRoom,
        NoAdjacentRoom,
        NoUsableSharedWall,
        InvalidDoorProposal,
        NoDoorSelected
    }
}
