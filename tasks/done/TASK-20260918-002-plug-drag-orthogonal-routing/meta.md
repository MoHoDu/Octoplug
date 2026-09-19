# Task Metadata

- **ID:** TASK-20260918-002
- **Title:** P0-1C: Plug Drag + Orthogonal Cable Routing + Length Limit
- **Status:** done
- **Owner:** User
- **Agent:** Claude Sonnet 5
- **Domain:** Connection / Power
- **Base:** dev
- **Branch:** feat/infinity-power-connection (existing branch)
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** DONE
- **Current Skill:** —

## Allowed Scope

- New C# under `Assets/01_Scripts/Power/{Routing,Input,Cable}/` (pathfinding, drag input, rendering, orchestration only).
- Additive components on the existing `Cable.prefab` (Plug, Line, Cable root) — no other prefab changes.
- Minimal, additive bug fix to `CableRoutingGridService` (Start-order race only; no behavior/API redesign).
- One new Product instance placed in `InfiniteMode.unity` so the feature is playable in the real demo scene.

## Do Not Modify

- Socket connect/disconnect state, power validation, PowerStrip movement, procedural Door/Room generation, power-flow visuals, product outline, audio (deferred to later stages).
- The user's manually-corrected Door Collider position/rotation in `Room.prefab` — not touched, verified via `git diff` (empty).
- Cable Material/Width/other authored LineRenderer design values.
- P0-1B foundation architecture (Grid/RoomArea/SocketConnector/etc.) — reused as-is, not redesigned.

## Exclusive Assets

- `Assets/03_Prefabs/Products/Cable.prefab`
- `Assets/00_Scenes/Demo/InfiniteMode.unity` (Products group: one new TV instance)

## Human Decisions

- (Carried from user's message) Door Collider position/rotation the user manually corrected in Editor is final; must never be recomputed or moved by code.
- (Carried from user's message) No separate test scene; verify in `InfiniteMode` itself.

## Open Decisions

- None blocking. Placement of the demo TV instance (world position, whether to also place a Wall_Outlet for visual completeness) is a minimal testing convenience the user may reposition or remove.

## Verification State

- Logic Test: PASS (6/6, reflection-driven synthetic drag)
- Runtime Integration: PASS (component wiring, Collider2D hit-test geometry, no Input console errors)
- Human Play Check: **PASS** (2026-09-18) — user confirmed real mouse click/drag/drop all work in InfiniteMode after the input-blocker fix
- **DONE**
