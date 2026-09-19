# Plan — Grid Spatial Foundation + 8-Direction Cable Routing

## A. Existing Grid Assessment (resolves Open Decision)

`CableRoutingGrid`/`CableRoutingGridService`/`GridCellState`/`GridCoord` were already exactly the right level of abstraction for a shared logical grid — `WorldToCell`/`CellToWorld`/`GetState`/`IsWalkable` are the common conversion/query surface every spatial system needs. **Extended in place, no second Grid system created.** Additions: `CellEdge` enum + `CableRoutingGrid.CellEdgeWorld` (wall-edge position for a future Wall-mounted-object placement system), and `SetObjectOccupied`/`IsObjectOccupied`/`IsFreeForPlacement` (object-placement occupancy, tracked independently of `GridCellState` — see Occupancy below). No existing method signature changed.

## B. LineRenderer Quality Root Cause (resolves Open Decision)

Root cause confirmed on `Cable.prefab` (shared by every Product and Multitap instance): the Flow (`ElectricEffects`) `LineRenderer` used `Alignment = Local` while Base (`Line`) correctly used `Alignment = View`. `Local` alignment fixes the width ribbon to the Transform's local up-axis regardless of segment direction, so it is only perpendicular to the line for one specific orientation — a horizontal vs. vertical segment already looked inconsistent before this Task, and would have looked far worse once diagonal segments existed. Fixed by setting Flow's alignment to `View` (matching Base) — **not** a width/design change, purely an orientation-correctness fix. Also normalized both LineRenderers' `widthCurve` from a malformed single off-center keyframe (`t=0.0202…`, an Editor authoring artifact) to a proper flat 2-key curve at the exact same evaluated value (`0.140625`) — zero visual change, just removes a fragile degenerate curve. Sorting Order (Base=5, Flow=6) and both authored widths were not touched. `numCornerVertices`/`numCapVertices` were left alone — `CablePathRenderer` already applies its own `cornerVertices` value (4) once at runtime regardless of the Editor-time authored value, so the scene-saved `0` was never actually reaching Play Mode as a problem.

## C. 8-Direction Weighted Routing

`GridPathfinder.TryFindPath` rewritten from uniform-cost BFS to A* (octile heuristic — exact/never-overestimating for this move set, so the result is always truly shortest). Orthogonal step cost `1`, diagonal cost `√2`. A diagonal step from a cell is only legal when both orthogonal neighbors it would otherwise cut past are also walkable (`CanCutDiagonal`) — this is checked inside the A* neighbor expansion itself, so it is structurally impossible for a returned path to cut a wall corner. Room-to-Room crossing is unaffected: `GridCellState.Door` is still the only passable boundary; a diagonal step across a Wall cell is rejected by the same `IsWalkable` check every other step uses. `TryFindNearestWalkable`/`TryFindNearestValidDropCell` (proximity/fallback searches, not routes) now expand across all 8 directions too, for better diagonal-aware recovery — corner-cutting does not apply to their own search order, since the actual reachability check inside `TryFindNearestValidDropCell` calls `TryFindPath`, which enforces it. `CableRoutingController.TryFindPowerRejectFallbackCell`'s local search was extended the same way, for consistency. No caller changed its call site — every public `GridPathfinder` method kept its exact signature.

## D. No Change Needed (already correct for 8 directions)

- **Cable Length**: `GridPathfinder.PathLength`/`TruncateToLength` already sum/interpolate real Euclidean distances between waypoints — this was already diagonal-correct with zero changes, since it never assumed axis-aligned segments.
- **Path Simplification**: `CablePathRenderer.Simplify` already merges collinear points via a direction-agnostic dot-product check (not an axis-aligned special case) — already correctly collapses runs of diagonal cells into one segment. No changes needed.
- **Base/Flow share one path**: `CablePathRenderer.Render` already applies the identical simplified point list to both `line` and `flowLine` — unaffected by this Task, verified still true (identical `positionCount` and every point, live-tested).
- **Plug drag smoothness**: `CableRoutingController.OnDragged` already uses the pointer's raw continuous position directly whenever its cell is walkable (only the nearest-cell recovery path snaps to a cell center, for an off-floor pointer) — dragging within open floor was already smooth/pointer-following, not grid-stepped. No changes needed.
- **Wall Outlet Socket endpoint rule**: `TryGetSocketApproachCell`'s short-radius Approach Point search is unaffected by 8-direction routing — re-verified live (Socket remains a terminal endpoint, never a pass-through).

## E. Occupancy: Cable Traversal vs. Object Placement

`CableRoutingGrid` gained a separate `objectOccupied` cell set, queried via `IsObjectOccupied`/`IsFreeForPlacement`, entirely independent of `GridCellState`/`IsWalkable`. `IsWalkable` (Cable traversal) is untouched — a Product sitting on a cell does not, by itself, block Cable routing through it, matching the existing (and unchanged) demo behavior. Nothing currently calls `SetObjectOccupied` — this is a ready capability for a future placement system, not wired to any current object, so there is zero behavior change or regression risk from adding it.

## F. Object Logical Placement / Room / Wall Outlet / Door

No new component was added to Product/PowerStrip/WallOutlet/Door/Room — their current Transform-based positions plus the existing Physics2D-collider-scan (`CableRoutingGridService.RebuildFromRoom`) already correctly express "where is a Wall," "where is a Door," and "where is a Room's floor" at cell granularity, which is what the Grid needs. A Wall Outlet's "wall-edge" concept is already correctly captured by `GridCellState.Blocked` (impassable) vs. `Door` (passable) plus the Socket's own rotation (an existing, confirmed principle — see Domain Map). The new `CellEdgeWorld` utility is available if a future explicit placement system wants an anchor point computed from a cell + direction, but nothing is forced onto the current Prefabs. Product footprint remains single-cell (sufficient for the demo); nothing in the new Grid API assumes single-cell, so a future multi-cell footprint is not precluded.

## G. Room Generation Data Contract (recorded, not implemented)

`RoomPlan → Room Grid Bounds → Wall/Edge → DoorPlan → Grid occupancy update → CableRoutingGridService.RebuildFromScene()`. Recorded in the Domain Map only — no Room Generation Core code touched.

## Verification

Synthetic-grid unit-style tests (via `eval`, no scene dependency) for World↔Grid round trip, diagonal/horizontal/vertical/mixed shortest paths (weighted costs verified numerically), corner-cutting rejection, full-wall blocking, Door-only crossing. Scene-level regression (real TV drag) for connect/disconnect/reject-fallback/endpoint-non-pass-through, Base/Flow identical simplified path, Flow alignment now matching Base, and Cable Length correctly clamping a diagonal-heavy path at exactly the authored length. All passed; zero Console errors.
