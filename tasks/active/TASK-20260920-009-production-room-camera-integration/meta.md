# Task Metadata

- **ID:** TASK-20260920-009
- **Title:** Production Room & Camera Integration
- **Status:** active
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** Room Generation / Camera Framing / Infinite Mode / Reward-Progression
- **Base:** dev
- **Branch:** task/TASK-20260920-009-production-room-camera-integration
- **Started:** 2026-09-20
- **Updated:** 2026-09-20
- **Current Stage:** design documentation
- **Current Skill:** plan-task

## Allowed Scope

- `docs/decisions/infinity-progression-and-reward-loop.md` (new)
- `docs/plans/infinity-demo-roadmap.md` (update)
- `docs/domains/reward-progression.md`, `room-generation.md`, `camera-framing.md` (update)
- Task files in this folder

## Do Not Modify

- `Assets/00_Scenes/Demo/InfiniteMode.unity`
- `Assets/03_Prefabs/Rooms/Room.prefab`
- Any production C# source files or other Unity scene/prefab
- `tasks/active/TASK-20260920-008-room-generation-integration/`

## AI Setup Allowed

- None this phase

## Exclusive Assets

- None (documentation-only phase)

## Human Decisions

- Exact Satisfaction gain/loss amounts per Need result
- Exact EXP-per-level scaling formula and constants

## Open Decisions

- Exact names/signatures of Future GameFlow hooks (confirmed at GameFlow task)
- Whether Target Selection supports Room and Wall-position types in initial Reward task

## Verification State

- Phase 1: PASS when docs written, no Unity changes. Phase 2–4: HUMAN_VERIFY_REQUIRED.
