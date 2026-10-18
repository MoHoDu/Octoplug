# Task Metadata

- **ID:** TASK-20260920-008
- **Title:** Room Generation Core Integration
- **Status:** active
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** Room Generation / Camera Framing / Connection Grid
- **Base:** dev
- **Branch:** task/TASK-20260920-008-room-generation-integration
- **Started / Updated:** 2026-09-20
- **Current Stage:** merge and code compatibility
- **Current Skill:** implement-code

## Current Phase Scope

- Use only committed `origin/feat/infinity-room-generation-core` history as the merge source; preserve all dirty worktrees.
- Preflight with current remote refs, merge-base, changed-file overlap, and non-mutating `git merge-tree`.
- Merge into TASK-008 only and apply minimal code-level compatibility fixes.
- Keep production `PointerInteractionResolver` authoritative; Camera Pan must not remain an independent ownership/priority system.
- Keep the existing Power/Cable grid authoritative; Room Generation pure models may remain but must not introduce a second gameplay grid.
- Preserve single persistent Multitap and Wall Outlet prefabs with `ActiveSocketCount` and current Power behavior.
- Preserve Room Generation and Camera Framing behavior and run required regression checks.
- If successful, commit and push TASK-008. Do not merge to `dev` or mark DONE.

## Forbidden This Phase

- No mutation of `InfiniteMode.unity`, production `Room.prefab`, production UI layout, `GameManager`, `UI_RoomInfo`, `GameStatusInfo`, Resident Demand, Satisfaction/EXP, Reward System, or production grid wiring.
- Do not copy `RoomGenerationTest.unity` into production.
- Do not incorporate, discard, stash, or reset uncommitted changes from any pre-existing worktree.
- Do not reintroduce legacy Wall Outlet/Multitap prefab selection or alter package versions unnecessarily.

## Deferred Production Integration

Production Room prefab adaptation, controller/wiring, `RoomPlan → CableRoutingGrid`, dynamic Doors, unified Wall Outlet placement, grid rebuild, pointer-to-Pan scene integration, Cinemachine scene setup, framing bridge, Hint visuals, debug trigger, and `InfiniteMode` Human Verification remain for a later phase.

## Exclusive Assets — TASK023 한정 이관

- 2026-10-19 후속 승인: CameraFraming의 PanBoundaryCalculator/신규 UsableViewport와 CameraFraming.Unity의 HouseCameraPanController/HouseCameraZoomController/CameraPanInputReader/RoomGenerationCameraFramingBridge 및 관련 순수/Editor 테스트를 TASK-20261018-023의 초기fit·상하단UI·manual zoom 연속보정에 한정 순차 이관한다. 다른 Camera/통합 범위·기존 상태는 유지하며 동시 수정하지 않는다.
- 승인 계획: `C:/Users/imvag/.claude/plans/magical-dazzling-kurzweil.md`. TASK023 workspace `D:/github-worktrees/Octoplug/shared-wall-outlet`; 이번 갱신은 소유권 기록만이며 코드/Scene 수정 또는 TASK008 완료 선언이 아니다.

## Human Decisions / Open Decisions

- Confirmed: committed remote branch only; existing grid is authoritative; Game Flow requests unlocks while Room Generation only creates/promotes on request.
- Open for production phase: Room bounds/candidate order, Door policy/tolerance, no-successor behavior, visual/layout details, and exact scene/prefab wiring.
- Verification remains `HUMAN_VERIFY_REQUIRED`; this phase cannot complete the Task.
