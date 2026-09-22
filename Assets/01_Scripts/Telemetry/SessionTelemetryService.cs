using System;
using System.Collections.Generic;
using Octoplug.RoomGeneration;
using UnityEngine;

namespace Octoplug.Telemetry
{
    public static class SessionTelemetryService
    {
        private static readonly Dictionary<int, string> RuntimeIds = new();
        private static SessionTelemetryRecorder recorder;
        private static int nextRuntimeId;

        public static SessionTelemetryRecorder Recorder => recorder;
        public static bool IsRecording => recorder != null && recorder.IsRecording;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            recorder = null;
            RuntimeIds.Clear();
            nextRuntimeId = 0;
        }

        public static void StartSession()
        {
            if (IsRecording)
            {
                return;
            }

            recorder = new SessionTelemetryRecorder(Application.persistentDataPath);
            recorder.Start();
        }

        public static void SetRecorderForVerification(SessionTelemetryRecorder verificationRecorder)
        {
            recorder = verificationRecorder;
            RuntimeIds.Clear();
            nextRuntimeId = 0;
        }

        public static void Record(
            string eventType,
            string category,
            string entityType,
            string entityId,
            string roomId,
            string payload = "{}")
        {
            recorder?.Append(eventType, category, entityType, entityId, roomId, payload);
        }

        public static string GetRuntimeId(Component value, string prefix = "object")
        {
            if (value == null)
            {
                return string.Empty;
            }

            var instanceId = value.GetInstanceID();
            if (!RuntimeIds.TryGetValue(instanceId, out var id))
            {
                id = $"{prefix}-{++nextRuntimeId:D6}";
                RuntimeIds.Add(instanceId, id);
            }

            return id;
        }

        public static void RecordRoom(RoomPlacement room)
        {
            if (!IsRecording || ContainsRoom(room.Id.Value))
            {
                return;
            }

            var bounds = room.Bounds;
            recorder.Document.Rooms.Add(new TelemetryRoomGeometry
            {
                RoomID = room.Id.Value,
                MinX = bounds.MinX,
                MinY = bounds.MinY,
                MaxX = bounds.MaxX,
                MaxY = bounds.MaxY
            });
            AddWall(room.Id.Value, "Bottom", bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MinY);
            AddWall(room.Id.Value, "Right", bounds.MaxX, bounds.MinY, bounds.MaxX, bounds.MaxY);
            AddWall(room.Id.Value, "Top", bounds.MaxX, bounds.MaxY, bounds.MinX, bounds.MaxY);
            AddWall(room.Id.Value, "Left", bounds.MinX, bounds.MaxY, bounds.MinX, bounds.MinY);
            recorder.Document.Summary.RoomCount = recorder.Document.Rooms.Count;
            Record("RoomUnlocked", "World", "Room", room.Id.Value, room.Id.Value,
                JsonUtility.ToJson(new BoundsPayload(bounds)));
        }

        public static void RecordDoor(DoorPlan door)
        {
            if (!IsRecording)
            {
                return;
            }

            var id = BuildDoorId(door);
            for (var i = 0; i < recorder.Document.Doors.Count; i++)
            {
                if (recorder.Document.Doors[i].DoorID == id)
                {
                    return;
                }
            }

            recorder.Document.Doors.Add(new TelemetryDoorGeometry
            {
                DoorID = id,
                RoomA = door.ConnectedRoomA.Value,
                RoomB = door.ConnectedRoomB.Value,
                X = door.Position.X,
                Y = door.Position.Y,
                Orientation = door.Orientation.ToString(),
                Width = door.Span.Length
            });
            Record("DoorCreated", "World", "Door", id, door.ConnectedRoomA.Value,
                JsonUtility.ToJson(new DoorPayload(door)));
        }

        public static bool TryFinalize(string reason, out SessionTelemetryArtifact artifact)
        {
            artifact = default;
            return recorder != null && recorder.TryFinalize(reason, out artifact);
        }

        public static bool TryGetLatestCompletedSession(out SessionTelemetryArtifact artifact)
        {
            var lookup = recorder ?? new SessionTelemetryRecorder(Application.persistentDataPath);
            return lookup.TryGetLatestCompletedSession(out artifact);
        }

        private static bool ContainsRoom(string roomId)
        {
            for (var i = 0; i < recorder.Document.Rooms.Count; i++)
            {
                if (recorder.Document.Rooms[i].RoomID == roomId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddWall(string roomId, string side, float startX, float startY, float endX, float endY)
        {
            recorder.Document.Walls.Add(new TelemetryWallGeometry
            {
                WallID = $"{roomId}:{side}",
                RoomID = roomId,
                Side = side,
                StartX = startX,
                StartY = startY,
                EndX = endX,
                EndY = endY
            });
        }

        private static string BuildDoorId(DoorPlan door)
        {
            var first = string.CompareOrdinal(door.ConnectedRoomA.Value, door.ConnectedRoomB.Value) <= 0
                ? door.ConnectedRoomA.Value
                : door.ConnectedRoomB.Value;
            var second = first == door.ConnectedRoomA.Value
                ? door.ConnectedRoomB.Value
                : door.ConnectedRoomA.Value;
            return $"door:{first}:{second}:{door.Position.X:R}:{door.Position.Y:R}";
        }

        [Serializable]
        private sealed class BoundsPayload
        {
            public float MinX;
            public float MinY;
            public float MaxX;
            public float MaxY;

            public BoundsPayload(RoomBounds2D bounds)
            {
                MinX = bounds.MinX;
                MinY = bounds.MinY;
                MaxX = bounds.MaxX;
                MaxY = bounds.MaxY;
            }
        }

        [Serializable]
        private sealed class DoorPayload
        {
            public string RoomA;
            public string RoomB;
            public float X;
            public float Y;
            public string Orientation;
            public float Width;

            public DoorPayload(DoorPlan door)
            {
                RoomA = door.ConnectedRoomA.Value;
                RoomB = door.ConnectedRoomB.Value;
                X = door.Position.X;
                Y = door.Position.Y;
                Orientation = door.Orientation.ToString();
                Width = door.Span.Length;
            }
        }
    }
}
