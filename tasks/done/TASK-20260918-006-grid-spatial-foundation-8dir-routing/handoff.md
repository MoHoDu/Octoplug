# Handoff — TASK-20260918-006

## Status

Stage: **DONE** (2026-09-18). Round-3/final Human Verification PASS received on the full checklist (Wall Outlet → Product Power Flow direction, 8-direction Cable Routing, Door movement, no corner cutting, zig-zag resolved, Cable thickness, Base/Flow identical path). Final pre-closure regression pass re-confirmed all synthetic and scene-level checks with zero Console errors. Task closed.

## Round-3 correction (this pass) — Flow direction only

`CablePowerFlowEffect.Update()` scrolled `mainTextureOffset.x` via `scrollOffset -= scrollSpeed * Time.deltaTime`, which — per the LineRenderer's Tile UV.x running 0 at the Origin end to max at the Plug/Socket end — visually moved the pattern Origin→Plug. Changed to `+=`, reversing it to Plug/Socket→Origin (supply side → consuming Product/PowerStrip body), matching the requested "전력 공급원에서 소비 대상 방향" direction. This is the only change: Base/Flow still render the identical point list (verified), Flow still only shows while genuinely Powered (verified), and the fix is direction-agnostic — no Product/Outlet name or Prefab is referenced, so it applies uniformly to every Cable.

## Round-2 correction (this pass) — Render Path post-processing only

**Measured first, per instruction.** Dumped raw grid-path coordinates, the pre-simplification world path, and the final `LineRenderer` positions for a real TV→Wall_Outlet_One connection attempt.

**Root cause #1 (the actual zig-zag/hook):** `CableRoutingController.BuildWorldPath`'s loop excluded the *last* grid cell from the direct-append path and routed it through `AppendOrthogonalJog` instead — the same treatment correctly reserved for the two genuine sub-cell endpoints (Origin's/a Socket's exact continuous position). Once 8-direction routing made the last step into a Wall Outlet's Approach Point a legitimate diagonal, this bug rewrote that one clean diagonal segment into an ugly forced 2-segment orthogonal L-bend every time. **Fixed**: the loop now appends every cell in `cellPath` (including the last) directly, exactly like the interior cells always did — `AppendOrthogonalJog` is now called *only* for the two real sub-cell gaps (Origin↔first cell, last cell↔exact target).

**Root cause #2 (the residual short "hook"):** even after fix #1, the sub-cell gap between the Approach Point and a Socket's *exact* mount position is often a small but non-zero offset on both axes (e.g. ~0.13 units, roughly half a grid cell) — `AppendOrthogonalJog` inserted a visible short bend for it. **Fixed**: `AppendOrthogonalJog` now skips inserting that bend when either axis's gap is below a new `minRenderSegmentLength` threshold (0.15, well under one 0.25 grid cell), instead drawing one direct segment at whatever resulting angle reaches the exact target — this is the one place a non-8-direction angle is expected and correct (the Origin/Socket's exact position was never grid-aligned to begin with). This only ever touches the two genuine endpoint corrections, never an interior A* grid segment, so interior segments stay exactly 8-direction-aligned (verified).

**A discarded approach, for the record:** an earlier attempt fixed this via a generic post-hoc "merge any short segment near either end" pass in `CablePathRenderer`. Verification caught that it could merge two *different* canonical-direction interior-adjacent segments (e.g. a 45° diagonal + a -90° vertical) into a single non-canonical-angle segment (27°) — violating "8-direction alignment everywhere except the Socket terminal." Reverted in favor of the surgical `AppendOrthogonalJog` fix above, which cannot do this because it only ever sees the two genuine sub-cell gaps.

**Root cause #3 (the connection outright failing during testing, unrelated to the above):** `GridPathfinder.TryFindNearestWalkable` picked whichever walkable cell BFS-queue order discovered first, which only guarantees *fewest hops*, not *nearest by real distance*, once diagonal neighbors are mixed into the same search — with 8 directions this could return a farther cell than a proper distance search would. **Fixed**: rewritten to scan the full bounding square and return the true minimum-Euclidean-distance walkable cell. (Verified this did not change the result for the current Wall_Outlet_One case specifically — see the separate, unrelated finding below — but it is a real correctness fix for the general case and for the Wall Outlet Approach Point specifically.)

## Separate, pre-existing finding (not a bug in this Task, not fixed here)

While investigating root cause #1, discovered that **TV's own placed position in the scene is currently `(0.76, -0.19)`**, not `(2, -0.15)` as it was throughout TASK-005 — it moved as part of the user's own committed Prefab/scene redesign (`d057daf`/`ca3a87f`). The straight-line distance from this new position to `Wall_Outlet_One`'s Socket is `3.246`, which **already exceeds `CableLength=3` before any pathfinding runs at all** — confirmed by testing: even after every fix in this round, TV cannot reach `Wall_Outlet_One` within its `CableLength`, but moving TV closer (a temporary, Play-mode-only, non-persisted test) connects instantly and cleanly with the fixed render path. This is a test-placeholder-value-vs-current-scene-layout mismatch from the redesign, not a Grid/Pathfinding/render-path issue, and per this Task's explicit boundaries (`CableLength`/Prefab positions are not to be touched), it was **not** changed. See Human Setup Required.

## What changed

- `Assets/01_Scripts/Power/Routing/GridPathfinder.cs` — rewritten: 8-direction weighted A* replacing 4-direction uniform-cost BFS; diagonal corner-cutting prevention; `TryFindNearestWalkable` rewritten to a true nearest-by-real-distance search (was BFS-queue-order, only "fewest hops"); `TryFindNearestValidDropCell` searches all 8 directions. All public method signatures unchanged — no caller needed to change.
- `Assets/01_Scripts/Power/Grid/CableRoutingGrid.cs` — added `CellEdge` enum + `CellEdgeWorld` (wall-edge position utility) and `SetObjectOccupied`/`IsObjectOccupied`/`IsFreeForPlacement` (object-placement occupancy, independent of `IsWalkable`/Cable traversal). Purely additive; nothing currently calls the new occupancy API.
- `Assets/01_Scripts/Power/Cable/CableRoutingController.cs` — **round 2**: `BuildWorldPath`'s cell-append loop fixed to include the last cell directly (was routed through an orthogonal jog, the actual zig-zag root cause); `AppendOrthogonalJog` gained a `minRenderSegmentLength` (0.15) threshold so a small sub-cell endpoint correction draws as one direct segment instead of a short visible "hook" — applies only to the two genuine endpoint gaps, never an interior grid segment. Power-reject fallback search's local direction array extended from 4 to 8 (round 1, unchanged this round).
- `Assets/03_Prefabs/Products/Cable.prefab` — Flow (`ElectricEffects`) `LineRenderer.alignment` changed `Local`→`View` (matches Base, root-causes the reported width inconsistency and would have worsened with diagonals). Both Base and Flow `LineRenderer.widthCurve` normalized from a malformed single-keyframe curve to a proper flat 2-key curve at the identical evaluated value (`0.140625`) — no visual change, just removes a fragile degenerate curve. `CablePathRenderer.minSegmentLength`-equivalent tuning was tried and reverted here (see "discarded approach" above) — no lasting change to `CablePathRenderer` itself beyond what round 1 already had (plain collinear merge). Sorting Order (Base=5, Flow=6) and both authored widths untouched. This propagates to every Product's and Multitap's nested Cable instance (verified on Fan).
- `docs/domains/connection-power.md` — recorded the confirmed 8-direction/Grid/Room-Generation-contract facts (short, per Context Budget) plus the render-path endpoint-correction principle.

## Automated Verification

**PASS** (all):
- World↔Grid↔World conversion and cell-center round trip.
- 8-direction shortest-path costs verified numerically: pure horizontal/vertical = N; pure diagonal (5,5) = 5√2 (not 10); mixed (7,3) = octile-exact 4+3√2.
- Diagonal corner-cutting: a direct 1-step diagonal cut past a blocking wall cell is rejected; the path correctly reroutes through the orthogonal detour instead.
- A fully-blocked column returns no path (Wall still fully blocks).
- Room-to-Room crossing still requires the Door cell specifically — verified the found path passes through it.
- Scene-level regression: normal connect (Powered=true, Flow on), real disconnect (Powered=false, Flow off), Power-Validation-reject-then-fallback-position (still ≥0.6 units from the Socket, still Disconnected), far-side-of-wall pointer still rejected (Socket endpoint non-pass-through intact).
- Base and Flow `LineRenderer`s render the exact identical simplified point list (verified point-for-point) with matching `alignment=View` now.
- A diagonal-heavy drag beyond Cable Length correctly clamps the *rendered* total length to exactly the authored `CableLength` (3), not a cruder step-count approximation.
- Zero Console errors throughout.

**FAIL**: none.

## Human Verification (round 3 — final) — PASS (2026-09-18)

User-confirmed PASS on the full checklist:
- Wall Outlet → Product Power Flow 방향 정상 (Outlet → Plug → Product, supply → consumer).
- 8방향 Cable Routing 정상.
- Door 이동 정상.
- Corner cutting 없음.
- 불필요한 zig-zag 해결.
- Cable 굵기 육안상 정상.
- Base Cable / Power Flow 동일 경로 정상.

This closes TASK-006 as DONE.

## Final regression pass (2026-09-18, before closure)

Re-ran the full checklist "가능한 범위에서" per the user's closure instructions:
- 8-direction Cable Routing, diagonal corner-cutting prevention, Door-only room crossing, Cable Length clamping — synthetic-grid tests re-passed (octile costs, corner-cut rejection, wall block, Door crossing all numerically re-verified).
- Plug Drag, Socket Connect/Disconnect, Wall Outlet endpoint (non-pass-through), Power Validation, Powered state — re-verified via a real connection in Play mode (temporary, non-persisted TV reposition, same technique as round 2/3, since TV's authored scene position remains outside `CableLength` of `Wall_Outlet_One` — see Human Setup Required below, unrelated to this Task).
- Base/Flow identical path — re-confirmed point-for-point identical LineRenderer positions.
- Power Flow direction (Socket → CableOrigin) — re-confirmed via reflection: `scrollOffset` delta is positive per frame.
- Console — 0 errors. 13 warnings present, all pre-existing/expected: repeated House power-limit connection rejections (test drags exceeding the demo's authored budget), one Sprite Mesh Type (Full Rect) import notice, the carried-over Heater icon-pool-vs-wattage mismatch, and the carried-over authored-Plug-off-grid notice for one Product — none introduced by this Task, none new.
- Play mode exited; scene saved (`InfiniteMode.unity`) with no outstanding changes from verification (all test moves were Play-mode-only and non-persisted).

## Human Setup Required

1. **TV can no longer reach `Wall_Outlet_One` within `CableLength=3`.** TV's own scene position changed to `(0.76, -0.19)` as part of the user's committed Prefab/scene redesign; the straight-line distance to the Socket is now `3.246`, exceeding the (test-placeholder, never-finalized) `CableLength=3` regardless of routing. Needs a human decision: raise TV's `CableLength`, move TV or the Outlet closer, or accept this pairing as out of range for the demo. Verified this is unrelated to any Grid/Pathfinding/render-path change in this Task — even a hypothetical straight-line cable would not fit.
2. Carried over from TASK-005, unrelated to this Task: top-alert UI, PowerStrip real instance, Heater icon-pool mismatch — see `docs/domains/connection-power.md` Deferred TODO.

## Next

Not scoped further per explicit instruction. Awaiting Human Verification.
