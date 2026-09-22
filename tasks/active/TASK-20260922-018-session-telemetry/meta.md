# Task Metadata

- **ID:** TASK-20260922-018
- **Title:** Session Telemetry and Replay Logging
- **Status:** active
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** telemetry; infinite-mode; cross-domain integration
- **Base:** dev plus completed TASK-015/016/017 baselines
- **Branch:** feat/session-telemetry
- **Started / Updated:** 2026-09-22
- **Current Stage:** verification
- **Current Skill:** implement-task

## Allowed Scope
- Implement telemetry, events, snapshots, and tests.
- Integrate automatically; manual scene editing allowed only via isolated task Editor.
- Fix human-verification defects in runtime finalization and RoomCount 3 EXP threshold progression; add focused regression tests.

## Do Not Modify
- Gameplay outcomes, generation algorithms, SFX behavior, dashboards, analytics, servers.
- Prohibited identifiers: names, email, IP, device ID, OS username, Google account, ad ID, location.

## AI Setup Allowed
- Runtime bootstrap, anonymous UserID, JSON file, Editor test objects, sample JSON.

## Exclusive Assets
- `Assets/00_Scenes/Demo/InfiniteMode.unity` (conditional)
- `Assets/01_Scripts/Telemetry/`, `Assets/Tests/Editor/Telemetry/`
- `Assets/01_Scripts/Power/Cable/CablePathRenderer.cs`, `PowerStripHeadController.cs`
- `Assets/01_Scripts/Power/Routing/CableRouteReachability.cs`
- `Assets/01_Scripts/Power/Connection/PlugSocketConnection.cs`
- `Assets/01_Scripts/Power/RuntimeWorldRegistry.cs`
- `Assets/01_Scripts/ResidentDemand.Unity/ResidentDemandController.cs`
- `Assets/01_Scripts/ResidentDemand/SessionProgressState.cs`, `ResidentDemand.Unity/SessionProgressController.cs`
- `Assets/01_Scripts/GameFlow.Unity/GameFlowManager.cs`
- Focused Editor tests for telemetry, SessionProgress, and GameFlow
- `Assets/01_Scripts/Reward.Unity/RewardSystemController.cs`
- `Assets/01_Scripts/RoomGeneration.Unity/ProductionRoomGenerationController.cs`, `RoomContentGenerationController.cs`
- `Assets/Tests/Editor/RoomGeneration/ProductionRoomGenerationControllerTests.cs`

## Human Decisions
- 2026-09-22: schema/ID/privacy constraints confirmed. Local JSON only.

## Open Decisions
- Unverified exact hidden Sheet headers documented.

## Verification State
- Verification: HUMAN_VERIFY_REQUIRED
