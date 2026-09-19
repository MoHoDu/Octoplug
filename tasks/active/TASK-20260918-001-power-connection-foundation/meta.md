# Task Metadata

- **ID:** TASK-20260918-001
- **Title:** P0-1B: Power/Cable connection foundation
- **Status:** active
- **Owner:** User
- **Agent:** Claude Sonnet 5
- **Domain:** Connection / Power
- **Base:** dev
- **Branch:** feat/infinity-power-connection (existing branch, created before this task)
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** implementation
- **Current Skill:** implement-code / integrate-unity-editor

## Allowed Scope

- New C# under `Assets/01_Scripts/Power/` (data structures, no drag/connect/validation gameplay logic).
- Add components/Collider2D to: `Cable.prefab`, `Room.prefab`, Product prefabs (TV/Fan/Heater/Induction/Air_Conditioner), Multitap prefabs (One–Five), Wall_Outlet prefabs (One–Five).
- Add two ProjectSettings user Layers: `Wall`, `Door`.
- Add `HousePowerBudget` + `CableRoutingGridService` to `ConnectionDirector` in `InfiniteMode.unity`.

## Do Not Modify

- Plug drag, socket connection, power validation, multitap movement, or final cable visuals (explicitly deferred to P0-1C+).
- Existing sprite/LineRenderer visuals, hierarchy names (including `CableOrigin`), or `m_IsActive` toggles.
- Anything outside the Connection/Power domain.

## Exclusive Assets

- `Assets/03_Prefabs/Products/Cable.prefab`
- `Assets/03_Prefabs/Rooms/Room.prefab`
- `Assets/03_Prefabs/Products/{TV,Fan,Heater,Induction,Air_Conditioner}.prefab`
- `Assets/03_Prefabs/Multitaps/Multitap_{One,Two,Three,Four,Five}.prefab`
- `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet_{One,Two,Three,Four,Five}.prefab`
- `Assets/00_Scenes/Demo/InfiniteMode.unity` (ConnectionDirector object only)
- `ProjectSettings/TagManager.asset` (adding 2 user layers only)

## Human Decisions

- 2026-09-18 — `CableOrigin` naming is kept as-is; no Prefab renames. Code must reference it via SerializeField, never by name string.
- 2026-09-18 — Room valid space / cable routing uses Collider2D+Layer (Room floor area, Wall, Door) for physical space, plus a separate Runtime Grid (4-directional, extensible for procedural rooms) for cable/plug/power-strip movement and routing. Algorithm choice (A* or equivalent) deferred to implementer at the routing-search stage; no generic pathfinding framework.

## Open Decisions

- Actual balance numbers (per-product Cable Length, appliance power consumption, PowerStrip allowed power, House allowed power) are placeholder Inspector defaults only — real values need design/Human input before P0-1C validation logic can be meaningful.

## Verification State

- See handoff.md
