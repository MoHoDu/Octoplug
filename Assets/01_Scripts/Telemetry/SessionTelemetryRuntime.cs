using System;
using System.Collections;
using System.Collections.Generic;
using Octoplug.GameFlow.Unity;
using Octoplug.Power;
using Octoplug.Power.Connection;
using Octoplug.Power.Cable;
using Octoplug.ResidentDemand;
using Octoplug.ResidentDemand.Unity;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Octoplug.Telemetry
{
    public sealed class SessionTelemetryRuntime : MonoBehaviour
    {
        private const float SnapshotIntervalSeconds = 10f;
        private readonly HashSet<int> recordedObjects = new();
        private ProductionRoomGenerationController roomGeneration;
        private ResidentDemandController demand;
        private SessionProgressController progress;
        private GameFlowManager gameFlow;
        private HousePowerBudget housePower;
        private float nextSnapshotTime;
        private string lastSnapshotPayload;
        private bool finalized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntime()
        {
            if (FindFirstObjectByType<SessionTelemetryRuntime>() != null)
            {
                return;
            }

            var owner = new GameObject("Session Telemetry");
            DontDestroyOnLoad(owner);
            owner.AddComponent<SessionTelemetryRuntime>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            RuntimeWorldRegistry.ObjectFinalized += HandleObjectFinalized;
            PlugSocketConnection.ConnectionCreated += HandleConnectionCreated;
            PlugSocketConnection.ConnectionRemoved += HandleConnectionRemoved;
            PlugSocketConnection.ConnectionFailed += HandleConnectionFailed;
            PowerStripHeadController.MoveCommitted += HandleObjectMoved;
            StartCoroutine(BindCurrentSceneAtEndOfFrame());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            RuntimeWorldRegistry.ObjectFinalized -= HandleObjectFinalized;
            PlugSocketConnection.ConnectionCreated -= HandleConnectionCreated;
            PlugSocketConnection.ConnectionRemoved -= HandleConnectionRemoved;
            PlugSocketConnection.ConnectionFailed -= HandleConnectionFailed;
            PowerStripHeadController.MoveCommitted -= HandleObjectMoved;
            UnbindScene();
        }

        private void Update()
        {
            if (!SessionTelemetryService.IsRecording || finalized || Time.realtimeSinceStartup < nextSnapshotTime)
            {
                return;
            }

            nextSnapshotTime = Time.realtimeSinceStartup + SnapshotIntervalSeconds;
            RecordSnapshot(false);
        }

        [ContextMenu("Debug/Save Telemetry Checkpoint")]
        public void SaveCheckpoint()
        {
            if (SessionTelemetryService.Recorder?.TryCheckpoint(out var path) == true)
            {
                Debug.Log($"Telemetry checkpoint saved: {path}", this);
            }
        }

        [ContextMenu("Debug/Inspect Telemetry")]
        public void InspectTelemetry()
        {
            var document = SessionTelemetryService.Recorder?.Document;
            Debug.Log(document == null
                ? "Telemetry session is not active."
                : $"Telemetry: session={document.SessionID}, events={document.Events.Count}, rooms={document.Rooms.Count}, finalized={document.IsFinalized}.", this);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(BindCurrentSceneAtEndOfFrame());
        }

        private IEnumerator BindCurrentSceneAtEndOfFrame()
        {
            yield return null;
            if (SceneManager.GetActiveScene().name != Octoplug.GameFlow.DemoSceneNames.InfiniteMode)
            {
                yield break;
            }

            BindScene();
        }

        private void BindScene()
        {
            UnbindScene();
            finalized = false;
            recordedObjects.Clear();
            SessionTelemetryService.StartSession();
            roomGeneration = FindFirstObjectByType<ProductionRoomGenerationController>();
            demand = FindFirstObjectByType<ResidentDemandController>();
            progress = FindFirstObjectByType<SessionProgressController>();
            gameFlow = FindFirstObjectByType<GameFlowManager>();
            housePower = FindFirstObjectByType<HousePowerBudget>();

            if (roomGeneration != null)
            {
                roomGeneration.RoomUnlocked += HandleRoomUnlocked;
                var state = roomGeneration.State;
                if (state != null)
                {
                    foreach (var room in state.UnlockedLayout.Rooms) SessionTelemetryService.RecordRoom(room);
                    foreach (var door in state.UnlockedLayout.Doors) SessionTelemetryService.RecordDoor(door);
                }
            }

            if (demand != null)
            {
                demand.DemandCreated += HandleDemandCreated;
                demand.DemandResolvedDetailed += HandleDemandResolved;
                foreach (var resident in demand.Residents)
                {
                    if (resident.Status != ResidentDemandStatus.None)
                    {
                        HandleDemandCreated(resident);
                    }
                }
            }

            if (gameFlow != null)
            {
                gameFlow.GameOverRequested += HandleGameOver;
            }

            foreach (var product in RuntimeWorldRegistry.GetProducts()) RecordObject(product, "Starter");
            foreach (var strip in RuntimeWorldRegistry.GetPowerStrips()) RecordObject(strip, "Starter");
            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets()) RecordObject(outlet, "Starter");
            nextSnapshotTime = Time.realtimeSinceStartup;
            RecordSnapshot(true);
        }

        private void UnbindScene()
        {
            if (roomGeneration != null) roomGeneration.RoomUnlocked -= HandleRoomUnlocked;
            if (demand != null)
            {
                demand.DemandCreated -= HandleDemandCreated;
                demand.DemandResolvedDetailed -= HandleDemandResolved;
            }
            if (gameFlow != null) gameFlow.GameOverRequested -= HandleGameOver;
            roomGeneration = null;
            demand = null;
            progress = null;
            gameFlow = null;
            housePower = null;
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (!finalized && SessionTelemetryService.IsRecording && scene.name == Octoplug.GameFlow.DemoSceneNames.InfiniteMode)
            {
                FinalizeSession("SceneUnloaded");
            }
        }

        private void HandleRoomUnlocked(Octoplug.RoomGeneration.RoomPlacement room)
        {
            SessionTelemetryService.RecordRoom(room);
            if (roomGeneration?.State != null)
            {
                foreach (var door in roomGeneration.State.UnlockedLayout.Doors)
                {
                    SessionTelemetryService.RecordDoor(door);
                }
            }
            RecordSnapshot(true);
        }

        private void HandleObjectFinalized(Component value, Octoplug.RoomGeneration.RoomId roomId)
        {
            RecordObject(value, "RoomUnlock", roomId.Value);
        }

        private void RecordObject(Component value, string spawnReason, string explicitRoomId = null)
        {
            if (value == null || !recordedObjects.Add(value.GetInstanceID())) return;
            var roomId = explicitRoomId ?? (RuntimeWorldRegistry.TryGetRoomOwner(value, out var owner) ? owner.Value : string.Empty);
            var id = SessionTelemetryService.GetRuntimeId(value, GetObjectType(value).ToLowerInvariant());
            var position = value.transform.position;
            SessionTelemetryService.Record("ObjectSpawned", "Object", GetObjectType(value), id, roomId,
                JsonUtility.ToJson(new ObjectPayload(spawnReason, position)));
            var summary = SessionTelemetryService.Recorder.Document.Summary;
            summary.ObjectCount++;
        }

        private void HandleObjectMoved(PowerStrip strip, Vector3 from, Vector3 to)
        {
            if (strip == null) return;
            var roomId = RuntimeWorldRegistry.TryGetRoomOwner(strip, out var owner) ? owner.Value : string.Empty;
            SessionTelemetryService.Record("ObjectMoved", "Object", "PowerStrip",
                SessionTelemetryService.GetRuntimeId(strip, "powerstrip"), roomId,
                JsonUtility.ToJson(new MovePayload(from, to)));
        }

        private void HandleConnectionCreated(PlugConnector plug, SocketConnector socket)
        {
            var id = BuildConnectionId(plug, socket);
            var renderer = plug != null ? plug.GetComponentInParent<CablePathRenderer>() : null;
            SessionTelemetryService.Record("ConnectionCreated", "Connection", "Connection", id, string.Empty,
                JsonUtility.ToJson(new ConnectionPayload(plug, socket, renderer)));
            SessionTelemetryService.Recorder.Document.Summary.ConnectionCount++;
            RecordSnapshot(true);
        }

        private void HandleConnectionRemoved(PlugConnector plug, SocketConnector socket)
        {
            SessionTelemetryService.Record("ConnectionRemoved", "Connection", "Connection", BuildConnectionId(plug, socket), string.Empty,
                JsonUtility.ToJson(new ConnectionPayload(plug, socket, plug != null ? plug.GetComponentInParent<CablePathRenderer>() : null)));
            var summary = SessionTelemetryService.Recorder.Document.Summary;
            summary.ConnectionCount = Mathf.Max(0, summary.ConnectionCount - 1);
            RecordSnapshot(true);
        }

        private void HandleConnectionFailed(PlugConnector plug, SocketConnector socket, string reason)
        {
            SessionTelemetryService.Record("ConnectionFailed", "Connection", "Connection", BuildConnectionId(plug, socket), string.Empty,
                JsonUtility.ToJson(new FailurePayload(reason)));
        }

        private void HandleDemandCreated(ResidentDemandState resident)
        {
            var id = BuildDemandId(resident);
            SessionTelemetryService.Record("DemandCreated", "Demand", "Demand", id, string.Empty,
                JsonUtility.ToJson(new DemandPayload(resident.Demand, resident.ResidentNumber.Value, string.Empty)));
        }

        private void HandleDemandResolved(ResidentDemandState resident, DemandOutcome outcome)
        {
            var resolution = outcome.Resolution.ToString();
            SessionTelemetryService.Record("DemandResolved", "Demand", "Demand", BuildDemandId(resident), string.Empty,
                JsonUtility.ToJson(new DemandPayload(outcome.Demand, resident.ResidentNumber.Value, resolution)));
            var summary = SessionTelemetryService.Recorder.Document.Summary;
            if (outcome.Resolution == DemandResolution.Success) summary.DemandSuccessCount++;
            else summary.DemandFailureCount++;
            RecordSnapshot(true);
        }

        private void HandleGameOver()
        {
            SessionTelemetryService.Record("GameOver", "Session", "Session", SessionTelemetryService.Recorder.Document.SessionID, string.Empty);
            FinalizeSession("GameOver");
        }

        private void FinalizeSession(string reason)
        {
            if (finalized) return;
            RecordSnapshot(true);
            finalized = SessionTelemetryService.TryFinalize(reason, out var artifact);
            if (finalized) Debug.Log($"Telemetry session finalized: {artifact.Path}", this);
        }

        private void RecordSnapshot(bool important)
        {
            if (!SessionTelemetryService.IsRecording) return;
            var payload = new SnapshotPayload
            {
                Experience = progress != null && progress.IsInitialized ? progress.CurrentExperience : 0,
                RequiredExperience = progress != null && progress.IsInitialized ? progress.RequiredExperience : 0,
                Satisfaction = progress != null && progress.IsInitialized ? progress.GlobalSatisfaction : 0,
                RoomCount = roomGeneration != null && roomGeneration.IsInitialized ? roomGeneration.State.UnlockedLayout.Rooms.Count : 0,
                ProductCount = Count(RuntimeWorldRegistry.GetProducts()),
                PowerStripCount = Count(RuntimeWorldRegistry.GetPowerStrips()),
                WallOutletCount = Count(RuntimeWorldRegistry.GetWallOutlets()),
                AllowedPowerWatts = housePower != null ? housePower.AllowedPowerWatts : 0f
            };
            var json = JsonUtility.ToJson(payload);
            if (important && json == lastSnapshotPayload) return;
            lastSnapshotPayload = json;
            SessionTelemetryService.Record("GlobalSnapshot", "Snapshot", "Session", SessionTelemetryService.Recorder.Document.SessionID, string.Empty, json);
            var summary = SessionTelemetryService.Recorder.Document.Summary;
            summary.FinalExperience = payload.Experience;
            summary.FinalSatisfaction = payload.Satisfaction;
            summary.FinalAllowedPowerWatts = payload.AllowedPowerWatts;
        }

        private static int Count<T>(IEnumerable<T> values)
        {
            var count = 0;
            foreach (var _ in values) count++;
            return count;
        }

        private static string GetObjectType(Component value) => value switch
        {
            ApplianceSource => "Product",
            PowerStrip => "PowerStrip",
            WallOutlet => "WallOutlet",
            _ => value.GetType().Name
        };

        private static string BuildConnectionId(PlugConnector plug, SocketConnector socket) =>
            $"connection:{SessionTelemetryService.GetRuntimeId(plug, "plug")}:{SessionTelemetryService.GetRuntimeId(socket, "socket")}";

        private static string BuildDemandId(ResidentDemandState resident) =>
            $"demand:resident-{resident.ResidentNumber.Value}:{resident.Demand?.Id}";

        [Serializable] private sealed class ObjectPayload { public string SpawnReason; public float X; public float Y; public ObjectPayload(string reason, Vector3 p) { SpawnReason = reason; X = p.x; Y = p.y; } }
        [Serializable] private sealed class MovePayload { public float FromX; public float FromY; public float ToX; public float ToY; public MovePayload(Vector3 from, Vector3 to) { FromX = from.x; FromY = from.y; ToX = to.x; ToY = to.y; } }
        [Serializable]
        private sealed class ConnectionPayload
        {
            public string PlugID;
            public string SocketID;
            public List<Vector2> Path = new();

            public ConnectionPayload(PlugConnector plug, SocketConnector socket, CablePathRenderer renderer)
            {
                PlugID = SessionTelemetryService.GetRuntimeId(plug, "plug");
                SocketID = SessionTelemetryService.GetRuntimeId(socket, "socket");
                if (renderer == null) return;
                for (var index = 0; index < renderer.LastRenderedWorldPath.Count; index++)
                {
                    Path.Add(renderer.LastRenderedWorldPath[index]);
                }
            }
        }
        [Serializable] private sealed class FailurePayload { public string Reason; public FailurePayload(string reason) { Reason = reason; } }
        [Serializable] private sealed class DemandPayload { public string DemandID; public int ResidentNumber; public string Resolution; public int ExperienceReward; public int SatisfactionDelta; public DemandPayload(DemandBalanceRecord d, int resident, string resolution) { DemandID = d?.Id; ResidentNumber = resident; Resolution = resolution; ExperienceReward = d?.ExperienceReward ?? 0; SatisfactionDelta = resolution == DemandResolution.Success.ToString() ? d?.GlobalSatisfactionOnSuccess ?? 0 : d?.GlobalSatisfactionOnFailure ?? 0; } }
        [Serializable] private sealed class SnapshotPayload { public int Experience; public int RequiredExperience; public int Satisfaction; public int RoomCount; public int ProductCount; public int PowerStripCount; public int WallOutletCount; public float AllowedPowerWatts; }
    }
}
