using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Selects door centers from safe opportunities without defining gameplay policy in the core.</summary>
    public interface IDoorPlacementPolicy
    {
        IReadOnlyList<DoorPlacementProposal> SelectDoors(IReadOnlyList<DoorPlacementOpportunity> opportunities);
    }
}
