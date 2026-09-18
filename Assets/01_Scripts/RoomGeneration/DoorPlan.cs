using System;

namespace Octoplug.RoomGeneration
{
    /// <summary>Validated data required to place one door on a shared wall.</summary>
    public sealed class DoorPlan
    {
        internal DoorPlan(SharedWall sharedWall, float centerCoordinate, float width)
        {
            SharedWall = sharedWall;
            CenterCoordinate = centerCoordinate;
            Span = new WallSpan(
                sharedWall.Span.Orientation,
                sharedWall.Span.FixedCoordinate,
                centerCoordinate - width * 0.5f,
                centerCoordinate + width * 0.5f);
            Position = sharedWall.Span.PointAt(centerCoordinate);
            Orientation = sharedWall.Span.Orientation == WallOrientation.Vertical
                ? DoorOrientation.VerticalWall
                : DoorOrientation.HorizontalWall;
        }

        public SharedWall SharedWall { get; }
        public RoomId ConnectedRoomA => SharedWall.CandidateWall.RoomId;
        public RoomId ConnectedRoomB => SharedWall.AdjacentWall.RoomId;
        public RoomWallId WallA => SharedWall.CandidateWall;
        public RoomWallId WallB => SharedWall.AdjacentWall;
        public float CenterCoordinate { get; }
        public Point2D Position { get; }
        public DoorOrientation Orientation { get; }
        public WallSpan Span { get; }
    }
}
