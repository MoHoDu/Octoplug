using System;
using System.Collections.Generic;

namespace Octoplug.Telemetry
{
    [Serializable]
    public sealed class SessionTelemetryDocument
    {
        public int LogSchemaVersion = 1;
        public string UserID;
        public string SessionID;
        public string StartedUtc;
        public string EndedUtc;
        public bool IsFinalized;
        public SessionTelemetrySummary Summary = new();
        public List<TelemetryRoomGeometry> Rooms = new();
        public List<TelemetryWallGeometry> Walls = new();
        public List<TelemetryDoorGeometry> Doors = new();
        public List<SessionTelemetryEvent> Events = new();
    }

    [Serializable]
    public sealed class SessionTelemetrySummary
    {
        public float DurationSec;
        public int RoomCount;
        public int ObjectCount;
        public int ConnectionCount;
        public int DemandSuccessCount;
        public int DemandFailureCount;
        public int RewardSelectedCount;
        public int RewardPassedCount;
        public int FinalExperience;
        public int FinalSatisfaction;
        public float FinalAllowedPowerWatts;
    }

    [Serializable]
    public sealed class SessionTelemetryEvent
    {
        public long EventSeq;
        public float EventTimeSec;
        public string EventType;
        public string Category;
        public string EntityType;
        public string EntityID;
        public string RoomID;
        public string Payload;
    }

    [Serializable]
    public sealed class TelemetryRoomGeometry
    {
        public string RoomID;
        public float MinX;
        public float MinY;
        public float MaxX;
        public float MaxY;
    }

    [Serializable]
    public sealed class TelemetryWallGeometry
    {
        public string WallID;
        public string RoomID;
        public string Side;
        public float StartX;
        public float StartY;
        public float EndX;
        public float EndY;
    }

    [Serializable]
    public sealed class TelemetryDoorGeometry
    {
        public string DoorID;
        public string RoomA;
        public string RoomB;
        public float X;
        public float Y;
        public string Orientation;
        public float Width;
    }

    public readonly struct SessionTelemetryArtifact
    {
        public SessionTelemetryArtifact(string path, string userId, string sessionId, int schemaVersion)
        {
            Path = path;
            UserID = userId;
            SessionID = sessionId;
            LogSchemaVersion = schemaVersion;
        }

        public string Path { get; }
        public string UserID { get; }
        public string SessionID { get; }
        public int LogSchemaVersion { get; }
    }
}
