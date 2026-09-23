# Telemetry Domain Map

## Responsibility

Session Telemetry records an InfiniteMode run as low-overhead in-memory events and geometry, then checkpoints/finalizes one local schema-v1 JSON artifact. It never owns gameplay decisions or recomputes gameplay results.

## Privacy and Storage

- Persistent anonymous `UserID`; immutable per-run `SessionID`.
- No personal identifiers, device identifiers, accounts, network addresses, advertising IDs, or location.
- Files live below `Application.persistentDataPath/OctoplugLogs/{UserID}/sessions/`.
- Upload and Form opening are owned by `Assets/01_Scripts/SurveySubmission/`; telemetry only exposes the authoritative finalized artifact.
- Sheet write, dashboards, and servers remain outside the Unity client.

## Performance Boundary

Telemetry consumes existing event payloads, registries, counters, and authoritative cable paths. It must not cause `FindObjects*` polling, scene scans, room rerendering, routing-grid rebuilds, physics synchronization, or pathfinding.

## Search Order

1. TASK-018 context and this map.
2. `Assets/01_Scripts/Telemetry/` and `Assets/Tests/Editor/Telemetry/`.
3. Exact authoritative producer named by the event catalog.
4. Related domain map only when needed.
