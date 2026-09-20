using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Immutable set of unlocked rooms, committed doors, and occupied room walls.</summary>
    public sealed class RoomLayout
    {
        private readonly HashSet<RoomId> roomIds;
        private readonly HashSet<RoomWallId> occupiedWalls;

        private RoomLayout(
            IReadOnlyList<RoomPlacement> rooms,
            IReadOnlyList<DoorPlan> doors,
            HashSet<RoomWallId> occupiedWalls)
        {
            Rooms = rooms;
            Doors = doors;
            this.occupiedWalls = occupiedWalls;
            roomIds = new HashSet<RoomId>();
            for (var i = 0; i < rooms.Count; i++)
            {
                roomIds.Add(rooms[i].Id);
            }

            ValidationToken = new object();
        }

        public IReadOnlyList<RoomPlacement> Rooms { get; }
        public IReadOnlyList<DoorPlan> Doors { get; }
        internal object ValidationToken { get; }

        public static RoomLayout Create(
            IEnumerable<RoomPlacement> rooms,
            IEnumerable<RoomWallId> initiallyOccupiedWalls = null)
        {
            if (rooms == null)
            {
                throw new ArgumentNullException(nameof(rooms));
            }

            var roomList = new List<RoomPlacement>(rooms);
            var ids = new HashSet<RoomId>();
            for (var i = 0; i < roomList.Count; i++)
            {
                if (!roomList[i].IsValid)
                {
                    throw new ArgumentException("Room layout may not contain invalid placements.", nameof(rooms));
                }

                if (!ids.Add(roomList[i].Id))
                {
                    throw new ArgumentException($"Duplicate room id: {roomList[i].Id}", nameof(rooms));
                }

                for (var j = 0; j < i; j++)
                {
                    if (roomList[i].Bounds.Overlaps(roomList[j].Bounds))
                    {
                        throw new ArgumentException("Room layout may not contain overlapping rooms.", nameof(rooms));
                    }
                }
            }

            var occupied = new HashSet<RoomWallId>();
            if (initiallyOccupiedWalls != null)
            {
                foreach (var wall in initiallyOccupiedWalls)
                {
                    if (!wall.IsValid)
                    {
                        throw new ArgumentException("Occupied wall must be valid.", nameof(initiallyOccupiedWalls));
                    }

                    if (!ids.Contains(wall.RoomId))
                    {
                        throw new ArgumentException("Occupied wall must belong to a room in the layout.", nameof(initiallyOccupiedWalls));
                    }

                    occupied.Add(wall);
                }
            }

            return new RoomLayout(roomList.AsReadOnly(), Array.Empty<DoorPlan>(), occupied);
        }

        public bool Contains(RoomId roomId) => roomIds.Contains(roomId);
        public bool IsWallOccupied(RoomWallId wallId) => occupiedWalls.Contains(wallId);

        internal RoomLayout Apply(RoomPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (!ReferenceEquals(plan.SourceLayoutToken, ValidationToken))
            {
                throw new InvalidOperationException("Room plan was not validated against this layout instance.");
            }

            var nextRooms = new List<RoomPlacement>(Rooms) { plan.Room };
            var nextDoors = new List<DoorPlan>(Doors);
            var nextOccupied = new HashSet<RoomWallId>(occupiedWalls);
            for (var i = 0; i < plan.DoorPlans.Count; i++)
            {
                var door = plan.DoorPlans[i];
                if (!nextOccupied.Add(door.WallA) || !nextOccupied.Add(door.WallB))
                {
                    throw new InvalidOperationException("Validated plan attempts to reuse an occupied room wall.");
                }

                nextDoors.Add(door);
            }

            return new RoomLayout(nextRooms.AsReadOnly(), nextDoors.AsReadOnly(), nextOccupied);
        }
    }
}
