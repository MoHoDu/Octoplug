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
- `Assets/00_Scenes/Demo/InfiniteMode.unity` (conditional): 2026-10-19 후속 승인으로 기존 `Canvas/HUD_Area`/`Canvas/Residents_Area` 및 기존 카메라 controller 간 필요한 참조 연결만 TASK-20261018-023에 순차 한정 이관. 다른 Scene 설정·UI 레이아웃/크기/Raycast·Telemetry 범위는 TASK018에 유지하며 동시 편집하지 않는다. 승인 계획 `C:/Users/imvag/.claude/plans/magical-dazzling-kurzweil.md`; 이번 meta 갱신은 소유권 기록만.
- `Assets/01_Scripts/Telemetry/`, `Assets/Tests/Editor/Telemetry/`
- `Assets/01_Scripts/Power/Cable/CablePathRenderer.cs`, `PowerStripHeadController.cs`
- `Assets/01_Scripts/Power/Routing/CableRouteReachability.cs`: TASK-20261018-021로 이번 공유 벽 양방향 라우팅 수정에 한정 이관; Telemetry 및 기타 소유 범위 유지.
- `Assets/01_Scripts/Power/Connection/PlugSocketConnection.cs`
- `Assets/01_Scripts/Power/RuntimeWorldRegistry.cs`
- `Assets/01_Scripts/ResidentDemand.Unity/ResidentDemandController.cs`
- `Assets/01_Scripts/ResidentDemand/SessionProgressState.cs`, `ResidentDemand.Unity/SessionProgressController.cs`
- `Assets/01_Scripts/GameFlow.Unity/GameFlowManager.cs`
- Focused Editor tests for telemetry, SessionProgress, and GameFlow
- `Assets/01_Scripts/Reward.Unity/RewardSystemController.cs`
- `Assets/01_Scripts/RoomGeneration.Unity/ProductionRoomGenerationController.cs`; `RoomContentGenerationController.cs`는 TASK-20261018-021 공유벽 도달 생성 위치 보정에 한정 이관.
- `Assets/Tests/Editor/RoomGeneration/ProductionRoomGenerationControllerTests.cs`: TASK-20261018-021 생성 위치 보정 회귀 검증에 한정 이관.

## Human Decisions
- 2026-09-22: schema/ID/privacy constraints confirmed. Local JSON only.

## Open Decisions
- Unverified exact hidden Sheet headers documented.

## Verification State
- Verification: HUMAN_VERIFY_REQUIRED
