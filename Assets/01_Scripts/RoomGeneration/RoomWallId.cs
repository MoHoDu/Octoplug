using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Identifies one complete side of one room.</summary>
    public readonly struct RoomWallId : IEquatable<RoomWallId>, IComparable<RoomWallId>
    {
        public RoomWallId(RoomId roomId, WallSide side)
        {
            if (!roomId.IsValid)
            {
                throw new ArgumentException("Room id must be valid.", nameof(roomId));
            }

            if (!Enum.IsDefined(typeof(WallSide), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side), side, "Wall side must be defined.");
            }

            RoomId = roomId;
            Side = side;
        }

        public RoomId RoomId { get; }
        public WallSide Side { get; }
        public bool IsValid => RoomId.IsValid && Enum.IsDefined(typeof(WallSide), Side);
        public WallOrientation Orientation => Side is WallSide.Left or WallSide.Right
            ? WallOrientation.Vertical
            : WallOrientation.Horizontal;

        public int CompareTo(RoomWallId other)
        {
            var roomComparison = RoomId.CompareTo(other.RoomId);
            return roomComparison != 0 ? roomComparison : Side.CompareTo(other.Side);
        }

        public bool Equals(RoomWallId other) => RoomId.Equals(other.RoomId) && Side == other.Side;
        public override bool Equals(object obj) => obj is RoomWallId other && Equals(other);
        public override int GetHashCode() => (RoomId.GetHashCode() * 397) ^ (int)Side;
        public override string ToString() => $"{RoomId}:{Side}";

        public static bool operator ==(RoomWallId left, RoomWallId right) => left.Equals(right);
        public static bool operator !=(RoomWallId left, RoomWallId right) => !left.Equals(right);
    }
}
