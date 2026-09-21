# Session Telemetry

## Runtime artifact

InfiniteMode automatically creates a schema-v1 in-memory recorder. Events append in memory; disk writes occur only for an explicit checkpoint or session finalization.

Finalized files:

```text
Application.persistentDataPath/
  OctoplugLogs/
    anonymous_user_id.txt
    {UserID}/
      latest_completed_session.txt
      sessions/
        session_*_completed.json
```

`SessionTelemetryService.TryGetLatestCompletedSession(out SessionTelemetryArtifact)` returns only a finalized artifact. Checkpoint files are never published through this API.

The persistent `UserID` is a generated anonymous UUID. Each run receives a new immutable `SessionID`. The implementation does not read or store a name, email, IP address, device identifier, OS username, Google account, advertising ID, or location.

## Event envelope

Every event has:

- `EventSeq`: monotonic session-local sequence.
- `EventTimeSec`: unscaled realtime from session recording.
- `EventType`, `Category`, `EntityType`, `EntityID`, `RoomID`.
- `Payload`: event-specific JSON serialized as a string.

Supported event names are `SessionStarted`, `SessionEnded`, `GameOver`, `RoomUnlocked`, `DoorCreated`, `ObjectSpawned`, `ObjectMoved`, `ConnectionCreated`, `ConnectionRemoved`, `ConnectionFailed`, `DemandCreated`, `DemandResolved`, `RewardPresented`, `RewardSelected`, `RewardPassed`, `RewardApplied`, and `GlobalSnapshot`.

Connection payloads copy the already-authoritative `CablePathRenderer.LastRenderedWorldPath`; telemetry does not invoke routing or pathfinding. Room geometry is captured from `RoomGenerationState`; runtime objects come from `RuntimeWorldRegistry`. Snapshot enumeration uses those existing registries only.

## Google Sheet flattening

The supplied public workbook exposes the intended tabs but did not expose internal telemetry-tab headers. The columns below are therefore the canonical JSON flattening contract for a future uploader, not a claim about currently configured external headers.

- **세션 요약**: `UserID`, `SessionID`, `LogSchemaVersion`, `StartedUtc`, `EndedUtc`, `DurationSec`, summary counters/final values, `LogFileURL`.
- **상태 스냅샷**: envelope columns plus flattened `GlobalSnapshot` payload.
- **방 구조**: `UserID`, `SessionID`, `RoomID`, `MinX`, `MinY`, `MaxX`, `MaxY`.
- **벽 구조**: `UserID`, `SessionID`, `WallID`, `RoomID`, `Side`, `StartX`, `StartY`, `EndX`, `EndY`.
- **문 구조**: `UserID`, `SessionID`, `DoorID`, `RoomA`, `RoomB`, `X`, `Y`, `Orientation`, `Width`.
- **오브젝트 로그**: envelope columns plus object spawn/move payload fields.
- **연결 로그**: envelope columns plus `PlugID`, `SocketID`, failure reason, and path points.
- **요구 로그**: envelope columns plus demand/resident/resolution/reward/delta fields.
- **보상 로그**: envelope columns plus offered IDs, selected reward, target/effect fields.
- **전체 이벤트**: the unchanged event envelope and raw `Payload`.
- **분석 지표**: derived offline; it is not produced by the runtime.

This task performs no Google Sheets API write, Drive upload, Google Form integration, server request, or dashboard update.

## Debugging

The persistent `Session Telemetry` runtime object offers Inspector context menus:

- `Debug/Inspect Telemetry`
- `Debug/Save Telemetry Checkpoint`

Serialization/write failures log warnings and never stop gameplay.
