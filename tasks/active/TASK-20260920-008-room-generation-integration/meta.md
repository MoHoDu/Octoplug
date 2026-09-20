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

## Human Decisions / Open Decisions

- Confirmed: committed remote branch only; existing grid is authoritative; Game Flow requests unlocks while Room Generation only creates/promotes on request.
- Open for production phase: Room bounds/candidate order, Door policy/tolerance, no-successor behavior, visual/layout details, and exact scene/prefab wiring.
- Verification remains `HUMAN_VERIFY_REQUIRED`; this phase cannot complete the Task.
