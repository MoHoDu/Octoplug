# Task Metadata

- **ID:** TASK-20260918-006
- **Title:** Grid Spatial Foundation + 8-Direction Cable Routing
- **Status:** done
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** Connection / Power (primary — routing/grid); future Room Generation (data-contract only, not implemented here)
- **Base:** dev
- **Branch:** feat/infinity-power-connection
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** done
- **Current Skill:** qa-feature

## Context

TASK-20260918-005 closed DONE (human verification PASS, 2026-09-18) — see `tasks/done/TASK-20260918-005-power-validation-product-info/`. The user has directly authored and **committed** the current Product/UI/Wall Outlet/Cable/Multitap Prefab designs and Sorting Order scheme (see `docs/domains/connection-power.md`'s "UI ↔ Code contract" section) — this is confirmed final and is explicitly **not** something this Task revisits or redesigns.

This Task's purpose is spatial/logic infrastructure, not visual design: unify how Room/Wall/Door/Product/PowerStrip/Wall Outlet/Socket/Plug-reachable-positions/Cable Routing all reason about position onto one invisible logical Grid, and upgrade Cable Routing from 4-direction to 8-direction (diagonal) weighted pathfinding without corner-cutting through walls and without relaxing the Door-only room-crossing rule.

## Allowed Scope

- Investigate the existing `Octoplug.Power.Grid` (`CableRoutingGrid`, `CableRoutingGridService`, `GridCellState`) and `Octoplug.Power.Routing.GridPathfinder` first. Prefer extending `CableRoutingGrid` from a Cable-only grid into the shared logical-position grid over building a second parallel Grid system. If extension is structurally inappropriate, report why before proceeding (do not silently build a parallel system).
- World↔Grid coordinate conversion (world position → cell, cell → world/cell-center, cell edge position) as shared, reusable methods — no per-system reimplementation of round/floor math. Reuse the existing cell size if one is already defined; do not introduce a second cell-size setting.
- A minimal logical-placement concept (e.g. a Grid Anchor / placement component) for Product, PowerStrip, Wall Outlet, Door, Room — logical position only, never forcing a Prefab's visual Transform to move or resize. Current authored visual offsets are preserved; a Product's Sprite pivot need not coincide exactly with its logical cell.
- Wall Outlet/Door represented as Wall-edge-relative (cell/room + edge direction), not plain cell-center objects — keep the existing "Socket is a terminal endpoint, never a room-to-room pass-through" rule; Door remains the only passable room boundary.
- Room represented as a Grid cell-bounds region (data model only — no procedural generation implemented here).
- Extend Grid occupancy beyond a single `Blocked` bool only as far as needed to distinguish Cable-traversal from object-placement legality (e.g. a Door is Cable-passable; a cell under a placed Product is not necessarily Cable-blocked). Keep this minimal and compatible with existing `GridCellState` usage — do not introduce a large generic occupancy framework.
- Cable Routing: 8-direction (orthogonal + diagonal) weighted shortest-path (diagonal cost = cellSize×√2, orthogonal = cellSize) replacing the current uniform-cost BFS if BFS can no longer guarantee shortest path under weighted costs. Prefer a simple, fast approach (e.g. A*) sized to the current small grid — do not build a general-purpose navigation framework.
- Diagonal corner-cutting prevention: a diagonal step is only legal if both orthogonal neighbors it "cuts past" are also non-blocking. Room/Wall/Door crossing rules (Door-only) are unaffected by allowing diagonals — never relaxed.
- Path simplification: collapse consecutive same-direction cells into single segments before handing points to `CablePathRenderer`/`LineRenderer` — both Base Cable and Power Flow must render the identical simplified path (no separate pathfinding for Flow).
- Plug drag: pointer world position → grid-based path/reachability decision, but the Plug's own visual position may follow the pointer smoothly on its final segment rather than snapping to cell centers every frame — avoid a visibly jittery/stepped drag feel.
- Cable Length accounting includes diagonal segment lengths (cellSize×√2 per diagonal step) in the total; over-length behavior (truncate to the reachable point) matches the existing P0-1C UX.
- Investigate and fix the reported `LineRenderer` quality issues (non-dead-straight segments, inconsistent width across orientations) as part of this same Cable-rendering work — root-cause via the checklist in the user's request (Transform/parent scale, widthCurve/widthMultiplier, Alignment, textureMode, numCornerVertices/numCapVertices, duplicate/near-duplicate path points, corner rounding, diagonal-segment width). Fix the root cause; do not change the currently-confirmed authored Base/Flow widths themselves.
- Record a short data-contract note (not an implementation) for how a future Room Generation system would plug into this Grid (`RoomPlan → Grid bounds → Wall Edge → Door Plan → occupancy update → CableRoutingGridService.RebuildFromScene()`).
- Automated verification per the user's checklist (Grid conversion, Path in all 8 directions + corner-cutting rejection + Door-only crossing, Cable length/simplification/Base-Flow-parity/straightness/width-consistency, Connection regression). Zero Console errors.

## Do Not Modify

- Any currently-authored Prefab visual value: Sprite, Layout, Scale, Hierarchy, visual Position, Sorting Order, color — for Product, ProductInfo, UseInfo, Tooltip, Wall Outlet, Multitap/PowerStrip Head, Room. Add only Grid Anchor / logical-coordinate / placement metadata alongside existing visual Transforms; never move/resize/recolor/re-sort anything the user has already confirmed (see `docs/domains/connection-power.md`).
- `PlugConnector`/`SocketConnector`/`PlugSocketConnection` pairing semantics, Power Validation, Powered state, Power Flow on/off logic, Power Reject fallback distance behavior, socket acquisition/detach hysteresis — these must regress-test clean, not be redesigned.
- Do not implement PowerStrip Head dragging itself this Task (only prepare the Grid-anchor concept it would use later).
- Do not implement real procedural Room Generation this Task (data-contract note only).
- Do not implement Product footprint >1 cell this Task — single-cell footprint is sufficient for the current demo; only keep the data model from being hard-coded to preclude a larger footprint later.
- Do not build a second, parallel Grid system alongside `CableRoutingGrid` without first reporting why extending it is inappropriate.
- Do not touch Resident/demand systems, Room Generation Core (a separate worktree's concern), audio, or Reward/Progression.

## AI Setup Allowed

- None yet — pending investigation of `CableRoutingGrid`/`CableRoutingGridService`/`GridPathfinder`'s current structure and confirmation this can be extended in place, before any Editor mutation or `plan.md` is finalized.

## Exclusive Assets

- To be confirmed after investigation. Expected candidates: `Assets/01_Scripts/Power/Grid/*.cs`, `Assets/01_Scripts/Power/Routing/GridPathfinder.cs`, `Assets/01_Scripts/Power/Cable/CableRoutingController.cs`, `Assets/01_Scripts/Power/Cable/CablePathRenderer.cs`, `Assets/01_Scripts/Power/Cable/CablePowerFlowEffect.cs` (LineRenderer quality fixes only — not the confirmed sortingOrder/width design values), `Assets/03_Prefabs/Products/Cable.prefab` (LineRenderer component settings only, e.g. numCornerVertices — never Sorting Order/Scale/authored width).

## Human Decisions

- None yet. Likely candidates once investigation/planning starts: whether diagonal movement should be a permanent design choice or a toggleable option; final Grid cell size if the current `CableRoutingGrid` size needs to change for 8-direction routing to feel right (a balance/feel decision, not purely technical).

## Open Decisions — RESOLVED

- `CableRoutingGrid` extension: **confirmed appropriate**, extended in place (see `plan.md` section A). No second Grid system created.
- LineRenderer root cause: **confirmed** — Flow's `Alignment=Local` vs. Base's `Alignment=View`, plus a malformed single-key `widthCurve` on both (see `plan.md` section B). Fixed at the `Cable.prefab` source.

## Verification State

- Round 1 automated verification (2026-09-18): synthetic-grid pathfinding tests (World↔Grid, 8-direction weighted costs, corner-cutting rejection, wall blocking, Door-only crossing) and scene-level regression (connect/disconnect/reject-fallback/endpoint, Base/Flow identical path, alignment fix, diagonal Cable Length clamping) — all passed. Zero Console errors.
- Round 1 Human Verification: found real zig-zag/hook artifacts near a Wall Outlet Socket approach and inconsistent short-segment width, despite the automated checks passing (the automated tests hadn't specifically exercised a live Wall Outlet approach's exact geometry).
- Round 2: measured raw/simplified/final path coordinates first (per instruction), root-caused to `BuildWorldPath`'s last-cell handling (the real bug) plus a residual sub-cell endpoint gap, and separately found (unrelated) `TryFindNearestWalkable`'s BFS-order was not true-nearest. Fixed all three via render-path-only changes; 8-direction A*, weighted cost, corner-cutting, Door crossing, Grid cellSize, and Power/Connection logic untouched. Also discovered (not fixed, flagged Human Setup Required) that TV's own redesigned scene position now puts it out of `CableLength` range of `Wall_Outlet_One` entirely, independent of pathfinding. Re-verified all round-1 checks plus the new fixes — zero Console errors. See `handoff.md` for full detail.
- Round 2 Human Verification: **PASS** on 8-direction routing, Door crossing, zig-zag/hook resolved, Cable thickness consistent, Base/Flow identical path — one remaining item: Power Flow's animated direction was backwards (Origin→Plug instead of supply→consumer).
- Round 3: fixed via a single sign flip in `CablePowerFlowEffect.Update()` (`scrollOffset -=` → `+=`). No path/A*/Grid/CableLength/Drag/Connection/Validation/LineRenderer-geometry/Prefab/Sorting change. Re-verified: Base/Flow still identical positions, Flow still only shows when genuinely Powered, direction-agnostic (no name hardcoding). Synthetic pathfinding regression re-confirmed passing. Zero Console errors.
- Round 3 Human Verification: **PASS** (2026-09-18) on the full checklist — Wall Outlet → Product Power Flow direction, 8-direction Cable Routing, Door movement, no corner cutting, zig-zag resolved, Cable thickness, Base/Flow identical path.
- Final pre-closure regression pass (2026-09-18): synthetic-grid pathfinding tests and scene-level checks (connect/disconnect, endpoint, Power Validation, Powered state, Base/Flow parity, Flow direction) all re-confirmed. Zero Console errors (13 pre-existing/expected warnings only). Play mode exited, scene saved.
- **Verification: DONE.** Task closed 2026-09-18.

## Explicit Instruction

"다음 기능으로 자동 진행하지 마라." Task creation only was requested this turn — planning/implementation begins on separate, explicit continuation.
