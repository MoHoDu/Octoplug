using Octoplug.Power;
using Octoplug.RoomGeneration;
using Octoplug.RoomGeneration.Unity;

namespace Octoplug.Reward.Unity
{
    public sealed class CableOwnerRewardTarget
    {
        public CableOwnerRewardTarget(UnityEngine.Component owner, CableInfo cable)
        {
            Owner = owner;
            Cable = cable;
        }

        public UnityEngine.Component Owner { get; }
        public CableInfo Cable { get; }
    }

    public sealed class RoomPlacementRewardTarget
    {
        public RoomPlacementRewardTarget(
            RoomPlacement room,
            RoomGenerationRoomBinder binder,
            UnityEngine.Vector2? desiredPosition = null)
        {
            Room = room;
            Binder = binder;
            DesiredPosition = desiredPosition;
        }

        public RoomPlacement Room { get; }
        public RoomGenerationRoomBinder Binder { get; }
        public UnityEngine.Vector2? DesiredPosition { get; }
    }
}
