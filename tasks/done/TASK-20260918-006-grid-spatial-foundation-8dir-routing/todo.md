# Todo — TASK-20260918-006

## Done

- [x] Investigated `CableRoutingGrid`/`CableRoutingGridService`/`GridCellState`/`GridPathfinder` — confirmed extendable in place, no second Grid system created.
- [x] Root-caused the LineRenderer straightness/width complaint: Flow's `Alignment=Local` vs. Base's `Alignment=View` (fixed, Cable.prefab source); malformed single-key `widthCurve` normalized to a flat 2-key curve at the same value on both. Sorting Order and authored widths untouched.
- [x] `GridPathfinder.TryFindPath` rewritten to 8-direction weighted A* (octile heuristic), diagonal cost `√2`, orthogonal `1`.
- [x] Diagonal corner-cutting prevention implemented inside A*'s neighbor expansion (`CanCutDiagonal`) — verified a direct diagonal cut is rejected in favor of the orthogonal detour.
- [x] `TryFindNearestWalkable`/`TryFindNearestValidDropCell`/`CableRoutingController`'s power-reject fallback search extended to 8-direction proximity search, for consistency.
- [x] Confirmed (no code change needed): Cable Length accounting, Path Simplification, Base/Flow shared-path rendering, and smooth pointer-following drag were already diagonal-correct by construction.
- [x] `CableRoutingGrid` extended with `CellEdge`/`CellEdgeWorld` (wall-edge position utility) and `SetObjectOccupied`/`IsObjectOccupied`/`IsFreeForPlacement` (object-placement occupancy, independent of Cable-traversal `IsWalkable`) — additive only, nothing currently calls the new occupancy API.
- [x] Recorded the Room Generation data contract in the Domain Map (not implemented).
- [x] Automated verification: synthetic-grid pathfinding tests (World↔Grid, horizontal/vertical/diagonal/mixed shortest-path costs, corner-cutting rejection, full-wall block, Door-only crossing) — all passed. Scene-level regression (connect/disconnect/reject-fallback/endpoint-non-pass-through, Base/Flow identical path, Flow alignment fixed, diagonal Cable Length clamping) — all passed. Zero Console errors.
- [x] No Prefab visual value (Sprite/Layout/Scale/Hierarchy/Position/Sorting Order/color) was reverted or redesigned — confirmed via targeted diffs limited to script files + `Cable.prefab`'s two `LineRenderer` components' `alignment`/`widthCurve` only.

## Not Started / Explicitly Deferred

- Real PowerStrip Head dragging (foundation only — `CellEdgeWorld`/occupancy API ready, not wired).
- Real procedural Room Generation (data contract recorded only).
- Product footprint >1 cell (not precluded, not implemented).
- Object placement UI/logic for Product/WallOutlet/PowerStrip/Door spawn (API ready, not wired to any current object).

## Round 2 — Render Path post-processing correction (2026-09-18)

- [x] Measured raw grid path, pre-simplification world path, and final LineRenderer positions for a real TV→Wall_Outlet_One connection attempt, per explicit instruction (investigate before touching anything).
- [x] Root-caused and fixed the actual zig-zag: `CableRoutingController.BuildWorldPath` excluded the last grid cell from the direct-append loop, routing it through `AppendOrthogonalJog` instead — rewriting a legitimate diagonal step into a forced L-bend. Fixed: every cell (including the last) is now appended directly; `AppendOrthogonalJog` is reserved for only the two genuine sub-cell endpoint gaps.
- [x] Root-caused and fixed the residual short "hook": `AppendOrthogonalJog` now skips its intermediate bend when either axis gap is below `minRenderSegmentLength` (0.15), drawing one direct segment instead — verified this never affects interior (already 8-direction-aligned) A* segments.
- [x] Tried and reverted a generic post-hoc "merge short segments" pass in `CablePathRenderer` — verification caught it could merge two different-direction interior segments into a non-canonical angle, violating "8-direction alignment except at the Socket terminal." The surgical `AppendOrthogonalJog` fix above cannot do this.
- [x] Found and fixed (separately) `GridPathfinder.TryFindNearestWalkable`'s BFS-queue-order bug — only guaranteed fewest hops, not true nearest, once diagonals were mixed in. Rewritten to an exhaustive true-nearest-by-distance scan.
- [x] Discovered (not fixed — flagged Human Setup Required) that TV's own scene position moved during the user's Prefab redesign, putting it outside `CableLength=3` of `Wall_Outlet_One` entirely — confirmed via straight-line distance, independent of any pathfinding change in this Task.
- [x] Re-verified: all directions (horizontal/vertical/diagonal) render with correct 8-direction-aligned interior segments; a real in-range connection (temporary Play-mode-only TV move) shows a clean single-segment Socket approach with no hook; synthetic pathfinding tests still pass; Cable Length still clamps exactly. Zero Console errors.
- [ ] **Round-2 Human Verification** — not yet confirmed. TASK-006 stays HUMAN_VERIFY_REQUIRED, not DONE.

## Round 3 — Power Flow direction fix (2026-09-18)

- [x] Round-2 Human Verification: PASS on everything except Flow direction (was Origin→Plug, needed Plug/Socket→Origin, i.e. supply→consumer).
- [x] Fixed with a single sign flip (`scrollOffset -=` → `+=`) in `CablePowerFlowEffect.Update()` — no other code touched.
- [x] Verified: Base/Flow still share identical rendered positions; Flow still gated on genuine `IsPowered`; direction-agnostic (no Product/Outlet name hardcoded, applies to every Cable uniformly); synthetic pathfinding regression re-passed; zero Console errors.
- [x] **Round-3 (final) Human Verification — PASS** (2026-09-18). Full checklist confirmed: Wall Outlet → Product Power Flow direction correct, 8-direction Cable Routing normal, Door movement normal, no corner cutting, unnecessary zig-zag resolved, Cable thickness visually normal, Base Cable / Power Flow identical path.

## Final regression pass (pre-DONE closure, 2026-09-18)

- [x] Synthetic-grid pathfinding tests re-run: World↔Grid, diagonal/horizontal/vertical/mixed shortest-path costs, corner-cutting rejection, wall blocking, Door-only crossing — all still passing.
- [x] Scene-level: Socket connect (Powered=True), Base/Flow LineRenderer positions identical, Flow visibility gated on Powered, `scrollOffset` delta now positive (Socket→CableOrigin direction) — confirmed via reflection.
- [x] Console checked: 0 errors, 0 unexpected warnings (13 warnings present are all pre-existing/expected: House power-limit rejections from test drags, one Sprite-tiling import-setting notice, the carried-over Heater icon-pool mismatch, and the carried-over authored-Plug-off-grid notice — see Deferred/Human Setup Required).
- [x] Play mode exited; scene saved.

## Human Verification

**PASS** (2026-09-18, round 3/final) — see `handoff.md` for the full confirmed checklist. TASK-006 is now **DONE**.
