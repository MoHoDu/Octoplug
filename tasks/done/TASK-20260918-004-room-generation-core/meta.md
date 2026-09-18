# Task Metadata

- **ID:** TASK-20260918-004
- **Title:** Room Generation Core
- **Status:** done
- **Owner:** User
- **Agent:** Claude Code
- **Domain:** Room Generation
- **Base:** dev
- **Branch:** feat/infinity-room-generation-core
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** done
- **Current Skill:** implement-code

## Goal

Create the pure calculation and state core needed to precompute, hint, unlock, and hand off future Room prefab generation without Scene or Prefab integration.

## Acceptance Criteria

- Room candidates can be checked for overlap and four-directional adjacency.
- Pairwise shared wall spans can be calculated without Scene assumptions.
- Accepted candidates have at least one valid DoorPlan.
- Door plans remain on shared walls, avoid corners/intersections, match wall orientation, and occupy each complete room wall at most once.
- HintLocked and UnlockedGenerated states and their visual intents are data-only.
- The exact stored hint plan can be unlocked, then a separate following hint can be stored.
- Pure logic tests pass without Unity Editor.
- Main-worktree prefab/scene integration requirements are documented.

## Allowed Scope

- New pure C# under `Assets/01_Scripts/RoomGeneration/**`.
- A scoped Room Generation runtime assembly definition.
- Editor-free Room Generation tests under `Tests/RoomGeneration.Core.Tests/**`.
- This Task's metadata, plan, todo, and handoff.
- `docs/domains/room-generation.md` after implementation facts are verified.

## Do Not Modify

- `Assets/00_Scenes/Demo/InfiniteMode.unity` or any Scene/Hierarchy.
- `Assets/03_Prefabs/Rooms/Room.prefab` or any Door, Product, Multitap, Wall Outlet, or UI prefab.
- `Assets/01_Scripts/Power/**`, Product UI, Cable, Connection, or other existing domain code.
- `ProjectSettings/**`, `Packages/**`, art, materials, or audio.
- Unity Editor placement, component wiring, renderer/material/sprite changes, or prefab instantiation.

## AI Setup Allowed

- None. This is a pure computation task.

## Exclusive Assets

- None. Serialized/shared Unity assets are read-only.

## Human Decisions

- 2026-09-18, User: Room states are `HintLocked` and `UnlockedGenerated`.
- 2026-09-18, User: the next RoomPlan is computed and stored before level-up.
- 2026-09-18, User: rooms expand on a 2D plane through top/bottom/left/right adjacency and may not overlap.
- 2026-09-18, User: a generated room must connect to at least one existing adjacent room through a valid DoorPlan.
- 2026-09-18, User: doors exist only on shared walls, never at corners/wall intersections, and each complete room wall has at most one door.
- 2026-09-18, User: a room can have multiple doors when distinct walls permit them.
- 2026-09-18, User: locked visual intent is dashed `#15786B`, lock/hatching shown, gameplay inactive.
- 2026-09-18, User: unlocked visual intent is solid `#87968E`, lock/hatching hidden, gameplay active.

## Open Decisions

These are non-blocking core inputs and are not resolved by this Task:

- Fixed versus variable Room size and the production source of candidate bounds.
- Random, weighted, or other production candidate ordering.
- Production Door width and corner/intersection safety margin.
- Production Door placement policy within a safe shared-wall interval.
- World-coordinate snapping/tolerance before exact core geometry is evaluated.
- Session behavior when no next valid RoomPlan can be produced.
- Unity prefab rotation mapping for horizontal versus vertical doors.

## Verification State

- Logic Test: 38/38 passed; strict build completed with 0 warnings and 0 errors.
- Fast Harness: passed with 3 pre-existing context-budget warnings.
- Full Harness: passed with the same 3 pre-existing warnings.
- Diff / Scope Audit: passed; no forbidden paths changed.
- Runtime Integration: N/A — pure computation task.
- Human Play Check: N/A — no player-facing integration in this worktree.
