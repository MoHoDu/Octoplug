using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace Octoplug.Telemetry
{
    public sealed class SessionTelemetryRecorder
    {
        private const string UserIdFileName = "anonymous_user_id.txt";
        private const string LatestCompletedFileName = "latest_completed_session.txt";
        private readonly Func<float> elapsedTime;
        private readonly string rootPath;
        private long nextSequence;

        public SessionTelemetryRecorder(string persistentDataPath, Func<float> elapsedTime = null)
        {
            rootPath = Path.Combine(persistentDataPath, "OctoplugLogs");
            this.elapsedTime = elapsedTime ?? (() => Time.realtimeSinceStartup);
        }

        public SessionTelemetryDocument Document { get; private set; }
        public bool IsRecording => Document != null && !Document.IsFinalized;

        public void Start()
        {
            if (IsRecording)
            {
                return;
            }

            var userId = LoadOrCreateUserId();
            Document = new SessionTelemetryDocument
            {
                UserID = userId,
                SessionID = $"session-{Guid.NewGuid():N}",
                StartedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            nextSequence = 1;
            Append("SessionStarted", "Session", "Session", Document.SessionID, string.Empty, "{}");
        }

        public void Append(
            string eventType,
            string category,
            string entityType,
            string entityId,
            string roomId,
            string payload)
        {
            if (!IsRecording)
            {
                return;
            }

            Document.Events.Add(new SessionTelemetryEvent
            {
                EventSeq = nextSequence++,
                EventTimeSec = Mathf.Max(0f, elapsedTime()),
                EventType = eventType ?? string.Empty,
                Category = category ?? string.Empty,
                EntityType = entityType ?? string.Empty,
                EntityID = entityId ?? string.Empty,
                RoomID = roomId ?? string.Empty,
                Payload = string.IsNullOrEmpty(payload) ? "{}" : payload
            });
        }

        public bool TryCheckpoint(out string path)
        {
            path = null;
            if (!IsRecording)
            {
                return false;
            }

            path = BuildSessionPath("checkpoint");
            return TryWrite(path);
        }

        public bool TryFinalize(string reason, out SessionTelemetryArtifact artifact)
        {
            artifact = default;
            if (!IsRecording)
            {
                return false;
            }

            Append("SessionEnded", "Session", "Session", Document.SessionID, string.Empty,
                JsonUtility.ToJson(new StringPayload { Value = reason ?? string.Empty }));
            Document.EndedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            Document.Summary.DurationSec = Mathf.Max(0f, elapsedTime());
            Document.IsFinalized = true;
            var path = BuildSessionPath("completed");
            if (!TryWrite(path))
            {
                Document.IsFinalized = false;
                return false;
            }

            try
            {
                var latestPath = Path.Combine(rootPath, Document.UserID, LatestCompletedFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(latestPath));
                File.WriteAllText(latestPath, path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Telemetry latest-session pointer failed: {exception.Message}");
            }

            artifact = new SessionTelemetryArtifact(
                path,
                Document.UserID,
                Document.SessionID,
                Document.LogSchemaVersion);
            return true;
        }

        public bool TryGetLatestCompletedSession(out SessionTelemetryArtifact artifact)
        {
            artifact = default;
            string userId;
            try
            {
                var userIdPath = Path.Combine(rootPath, UserIdFileName);
                if (!File.Exists(userIdPath))
                {
                    return false;
                }

                userId = File.ReadAllText(userIdPath).Trim();
                var latestPath = Path.Combine(rootPath, userId, LatestCompletedFileName);
                if (!File.Exists(latestPath))
                {
                    return false;
                }

                var sessionPath = File.ReadAllText(latestPath).Trim();
                if (!File.Exists(sessionPath))
                {
                    return false;
                }

                var document = JsonUtility.FromJson<SessionTelemetryDocument>(File.ReadAllText(sessionPath));
                if (document == null || !document.IsFinalized)
                {
                    return false;
                }

                artifact = new SessionTelemetryArtifact(
                    sessionPath,
                    document.UserID,
                    document.SessionID,
                    document.LogSchemaVersion);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Telemetry latest-session lookup failed: {exception.Message}");
                return false;
            }
        }

        private string LoadOrCreateUserId()
        {
            var path = Path.Combine(rootPath, UserIdFileName);
            try
            {
                if (File.Exists(path))
                {
                    var existing = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        return existing;
                    }
                }

                Directory.CreateDirectory(rootPath);
                var created = $"user-{Guid.NewGuid():N}";
                File.WriteAllText(path, created);
                return created;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Telemetry anonymous UserID persistence failed: {exception.Message}");
                return $"user-ephemeral-{Guid.NewGuid():N}";
            }
        }

        private string BuildSessionPath(string state)
        {
            var folder = Path.Combine(rootPath, Document.UserID, "sessions");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ", CultureInfo.InvariantCulture);
            return Path.Combine(folder, $"session_{timestamp}_{Document.SessionID}_{state}.json");
        }

        private bool TryWrite(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(Document, true));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Telemetry JSON write failed: {exception.Message}");
                return false;
            }
        }

        [Serializable]
        private sealed class StringPayload
        {
            public string Value;
        }
    }
}
