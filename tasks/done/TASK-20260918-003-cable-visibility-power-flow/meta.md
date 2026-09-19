# Task Metadata

- **ID:** TASK-20260918-003
- **Title:** P0-1C.1: Cable Visibility + Powered Flow Rendering Foundation
- **Status:** done
- **Owner:** User
- **Agent:** Claude Sonnet 5
- **Domain:** Connection / Power
- **Base:** dev
- **Branch:** feat/infinity-power-connection (existing branch)
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** DONE
- **Current Skill:** verify-unity

## Allowed Scope

- Rendering only: Base Cable sorting order, new Power Flow LineRenderer + material, `CablePathRenderer` extended to also drive the Flow line from the same path, new `CablePowerFlowEffect` for on/off + scroll.
- New Material asset `Assets/02_Resources/Materials/CableFlow.mat`.
- Additive component/LineRenderer on `Cable.prefab`'s existing `ElectricEffects` node only — no new GameObjects.

## Do Not Modify

- Routing, Input, Cable Length, Drop, Socket, Power logic — reused/unchanged (`GridPathfinder`, `CableRoutingGrid`, `CableRoutingGridService`, `PlugDragInput`, `CableRoutingController`'s decision logic).
- Product/PowerStrip sprites and their sorting order.
- Room/Door Collider (including the user's manual correction) and Scene/Prefab layout.
- Base Cable Material/Width/Routing/Corner settings, other than the sorting-order shift needed to make room for Flow.

## Exclusive Assets

- `Assets/03_Prefabs/Products/Cable.prefab`
- `Assets/02_Resources/Materials/CableFlow.mat` (new)

## Human Decisions

- None new — this stage is rendering-structure-only, per user's explicit instruction.

## Open Decisions

- Final Power Flow color/speed/texture are placeholder (orange dot texture, 1.5 u/s) pending art/design pass — not a blocker for this foundation stage.
- **Deferred TODO (not this task):** Product/PowerStrip overlap alpha fade — see `docs/domains/connection-power.md` "Deferred" note. Re-evaluate after a play test shows whether Sorting Order alone is visually sufficient.

## Verification State

- Logic/Runtime: PASS before the final shader-only change; targeted animation rendering PASS afterward; final full regression rerun pending due temporary CLI safety-classifier timeout (see handoff.md)
- User feedback round 2 (2026-09-18):
  - Base Cable behind Product — **PASS**
  - Initial Cable rendering (before any click) — **FAIL** → diagnosed and fixed
  - Power Flow visible only in Scene View while selected — **FAIL** → root cause was the same initial-render bug (tiny stale stub, invisible at normal Game View zoom); fixed as a side effect
  - Flow pattern orientation perpendicular to Cable — **FAIL** → separate real bug (missing `widthCurve` copy), fixed
  - Product overlap alpha fade — **DEFERRED**, unchanged, not mixed into this fix
- User feedback round 3 (2026-09-18):
  - Initial Cable visible — **PASS**
  - Flow visible in Game View — **PASS**
  - Flow parallel to Cable — **PASS**
  - Flow pattern stationary — **FAIL** → root cause diagnosed and fixed: `Sprites/Default` ignores `_MainTex_ST`; `CableFlow.mat` now uses transparent URP Unlit, which visibly responds to the existing UV scroll
- Human Play Check (2026-09-18): **PASS** — continuous Game View Flow, initial Cable visibility, Product-behind sorting, and Origin → Plug direction confirmed by the user
- **DONE** — Product overlap alpha fade remains explicitly deferred

## Note for the user (found during verification, not requested)

While re-verifying, an unrelated pre-existing scene inconsistency in `TV.prefab`'s nested UI (a "Prefab mismatch: Transform vs RectTransform... some references might be lost" condition, first seen in the console before this session's edits began) appears to have been auto-repaired by Unity into `InfiniteMode.unity` at some point this session when the scene was saved. The written values look like harmless zeroed defaults, but Unity's own warning says references could be lost — please review that specific diff hunk (search the scene diff for `guid: 85b0eaeb6254de240bc8ee2aa321069c`) before accepting it. Not touched or caused by this task's Cable-rendering work; flagged for transparency.
