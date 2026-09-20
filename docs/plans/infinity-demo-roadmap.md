# InfinityMode Demo Roadmap

## Goal

InfinityMode에서 핵심 코어 루프를 플레이 가능한 상태로 완성
최종 목표: 요구 → 전력 연결 → 만족도 → 레벨업 → 방 확장 → Reward 선택

## Completed (Human Verification PASS)

- [x] Grid 기반 8방향 Cable Routing
- [x] Plug Drag
- [x] Socket 연결/해제
- [x] Wall Outlet 연결
- [x] House Power Validation
- [x] Powered / Power Flow
- [x] ProductInfo / Tooltip
- [x] PowerStrip (Placement, Drag, Power Chain, Allowed Power, Runtime Socket/CableLength upgrade)
- [x] Room Generation Core (pure logic, 66 tests PASS)
- [x] Camera Core (Zoom, Pan, Hint Framing, 46 tests PASS)
- [x] Room Generation + Camera Core merge into dev

## Current

- [ ] Design Documentation & Integration Scope (TASK-20260920-009 Phase 1)
- [ ] TASK-20260920-008 Unity Verification (pending Editor environment)

## Execution Order (Confirmed 2026-09-20)

### 1. Production Room Integration

- Wire RoomGenerationController into production `Room.prefab`
- RoomPlan → CableRoutingGrid bridge
- Dynamic Doors applied to production prefab
- Unified Wall Outlet + ActiveSocketCount generation
- Grid rebuild on Room unlock
- HintLocked / UnlockedGenerated in InfiniteMode scene

### 2. Production Camera Integration

- Wire Zoom/Pan/Hint adapters into InfiniteMode Cinemachine brain
- Camera Reveal stub: zoom-out to show new Room on Level Up
  (triggered by GameFlow, not by Camera itself)

### 3. InfiniteMode Room/Camera/Power Full Regression

- End-to-end Human Verification of Room + Camera + Power/Connection in InfiniteMode

### 4. Resident Demand

- Need generation
- UsageType connection
- Wait / Active state
- Need success / failure

### 5. Satisfaction / Game Over

- Satisfaction 0–100
- Need result → Satisfaction gain/loss
- Satisfaction = 0 → Game Over

### 6. EXP / Level Progression

- EXP accumulation from Need outcomes
- EXP Max → Level Up trigger
- EXP threshold scaling with room count (formula: Open Decision)

### 7. GameFlow Orchestration

- GameManager / GameFlowController
- Level-Up sequence: Room Generation → Camera Reveal → Pause → Wait → Reward → Resume
- Orchestration via events: RoomGenerated, RoomContentReady, CameraRevealCompleted, etc.
- No polling; no Scene hierarchy search

### 8. Reward System + Target Selection

- 3-choice Reward display on Level Up
- Reward Pool: Basic Rewards + Upgrades + Composite Rewards
- Immediate-apply vs. Target-required flow
- Target Selection: dim world → pick target → apply

### 9. Endless / Result / Restart

- Game Over screen
- Best Score
- Restart

### 10. Polish

- Settings
- Sound
- Deferred visual TODO

---

## Notes

- GameFlow and Reward are **not** mixed into Production Room/Camera Integration tasks.
- No Rent, no Currency, no separate Upgrade Phase. See:
  `docs/decisions/infinity-progression-and-reward-loop.md`
