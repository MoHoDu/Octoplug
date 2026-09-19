# Connection / Power Domain

## Responsibility

Represent how wall outlets, cables, multitaps, and appliances form a power network and communicate capacity/consumption state.

## Existing Evidence

- `Assets/03_Prefabs/Products/Cable.prefab`
  - `CableOrigin`, `Plug`, `Handle`, `Line`, and `ElectricEffects` design anchors.
  - Both `Line` (Base) and `ElectricEffects` (Flow) `LineRenderer`s must use `Alignment = View` (camera-billboarded) — `Local` alignment fixes the width ribbon to one axis and looks visibly inconsistent across differently-oriented segments (confirmed root cause of a reported width-inconsistency bug, fixed TASK-20260918-006). Keep both in sync if either is ever touched again.
- `Assets/03_Prefabs/Multitaps/Multitap_One.prefab` through `Multitap_Five.prefab`.
- `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet_One.prefab` through `Wall_Outlet_Five.prefab`.
- Product prefabs under `Assets/03_Prefabs/Products/`.
- `Assets/03_Prefabs/Multitaps/PowerInfo.prefab`.
- `Assets/00_Scenes/Demo/InfiniteMode.unity` roots: `Connections`, `Multitaps`, `Wall_Outlets`, and `ConnectionDirector` (now hosts `HousePowerBudget` + `CableRoutingGridService`).
- `Assets/01_Scripts/Power/` — data foundation (`CableInfo`, `PlugConnector`, `SocketConnector`, `ApplianceSource` [now also UsageTypes/Capacity/SatisfactionDurationSeconds/IsPowered], `PowerStrip`, `WallOutlet`, `RoomArea`, `HousePowerBudget`, `UsageType`) and `Power/Grid/` (`CableRoutingGrid`, `CableRoutingGridService`).
- `Assets/01_Scripts/Power/Routing/GridPathfinder.cs`, `Power/Input/PlugDragInput.cs`, `Power/Cable/CablePathRenderer.cs` + `CableRoutingController.cs` — Plug drag, orthogonal shortest-path routing, Cable-length-limited rendering, physical Plug↔Socket connect/disconnect/reconnect via `Power/Connection/PlugSocketConnection.cs`, and (P0-1E) Power Validation via `Power/Connection/PowerValidationService.cs`. PowerStrip Head movement (TASK-20260918-007) via `Power/Input/HeadDragInput.cs` + `Power/Cable/PowerStripHeadController.cs` — Grid-validated, moves the Multitap root (Head/Sockets/Cable/PowerInfo are siblings, move together with zero reparenting), consumes the previously-unused `CableRoutingGrid.SetObjectOccupied`/`IsObjectOccupied` API from TASK-006.
- `Assets/01_Scripts/Power/UI/` (P0-1E) — `UsageTypeIconLibrary` (ScriptableObject + `.asset`), `ProductInfoView`, `UseInfoView`, `ProductClickInput`, `ProductTooltipController`. Drive the existing `ProductInfo`/`UseInfo`/`UI_ProductTooltip` prefabs; no new UI prefabs were created.
- `Power/Cable/CablePowerFlowEffect.cs` (P0-1C.1) — lives on the Cable root (alongside `CableInfo`/`CableRoutingController`, e.g. `TV/Connection`), not on `ElectricEffects`, specifically so it is discoverable one level under the Product/PowerStrip in the Hierarchy. It only shows/hides and scrolls the Flow LineRenderer on `ElectricEffects`; `SetPowered(bool)` is the real entry point, `previewPowered` is an Inspector-only test toggle and must never become a game rule. `CableFlow.mat` uses transparent `Universal Render Pipeline/Unlit` because the original `Sprites/Default` shader ignored `_MainTex_ST`, making runtime texture-offset animation visually stationary. **Flow direction (confirmed, TASK-20260918-006)**: power visually flows from the supplying Socket/Plug side toward the `CableOrigin` (Product/PowerStrip body) side — Wall Outlet → Product, Wall Outlet → PowerStrip, PowerStrip Socket → Product — never the reverse. Implemented purely as the sign of `scrollOffset`'s per-frame increment (`+=`, not `-=`); Base/Flow geometry and point order are never touched to achieve this. Applies uniformly to every Cable (no Product/Outlet name is ever checked). Flow only ever shows while actually Powered (Connected ≠ Powered still holds).
- Sorting order (as of P0-1D): Base Cable `-1`, Flow `0`, Product/Multitap sprites `1`, Room walls `2`/`3` (`Wall`'s own `LineRenderer.sortingOrder`, varies per wall — unchanged by P0-1D), Wall Outlet sprites `4` (all five `Wall_Outlet_*.prefab`, above every wall order so it never renders as embedded-in-the-wall), the Plug's own sprite(s) `5` while connected and `6` while actively being dragged (`CableRoutingController`'s `connectedSortingOrder`/`draggingSortingOrder`, applied only to `SpriteRenderer`s found under the Plug — Cable Base/Flow `LineRenderer` order is never touched by this). The Plug's authored resting order (currently `0`) applies only when neither connected nor being dragged.
- `Wall`/`Door` are ProjectSettings user Layers (indices 8/9); Room walls and the door carry `BoxCollider2D` on those layers.

## Design Direction

The concept document calls for cable connection/disconnection, length restrictions, allowed-versus-consumed power validation, dotted powered-flow animation, and powered-product outline feedback.

Exact validity, routing, capacity, overload, failure, and recovery rules are Open Decisions.

## Related Domains

- Resident / Demand consumes appliance service.
- Infinite Mode integrates the network into a session.
- Reward / Progression may unlock or upgrade network assets.

## Modification Cautions

- `Cable.prefab` is nested in every multitap and product prefab; treat it as a high-blast-radius Exclusive Asset.
- `PowerInfo.prefab` is shared by all multitap prefabs.
- Do not hand-edit Unity YAML while a matching Editor is reachable.
- Scene/prefab/UI feedback changes require the Human Decision Gate and Unity Editor integration stage.
- Once a human has manually corrected a Door (or any) Collider's position/rotation in the Editor, code must not recompute or move it. Treat the authored value as correct.

### Door / Room routing principle (long-term, confirmed)

Cable routing must never assume a fixed room or door count, a fixed door rotation, or "one door per room." Doors are Wall-adjacent openings that can be created procedurally later, in any number, at any rotation, wherever two rooms are validly connectable.

- Routing reads doors purely as spatial data: whatever currently carries the `Door` layer/Collider2D in the scene is a passable opening; whatever carries `Wall` is not.
- `CableRoutingGridService.RebuildFromScene()` is the single re-entry point after rooms/doors change (procedural generation or manual edits) — routing code must keep working unchanged after it is called again with different room/door data.
- No routing code may key off a door's name, hierarchy position, count, or hard-coded rotation.

### Grid Spatial Foundation + 8-Direction Routing (confirmed, TASK-20260918-006)

- `CableRoutingGrid` is the one shared invisible logical grid (`WorldToCell`/`CellToWorld`/`CellEdgeWorld`/`GetState`/`IsWalkable`) — extended in place rather than a second parallel Grid system; a Grid coordinate is a *logical* position only and never forces any Prefab's authored visual Transform to move.
- Occupancy is split in two, independent questions: `IsWalkable` (Cable traversal — Wall/Door/Walkable only) vs. `IsObjectOccupied`/`IsFreeForPlacement` (object placement). A Product sitting on a cell does not, by itself, block Cable traversal through it. Nothing currently calls the placement API — it is a ready capability for a future placement system, not wired to any current object.
- `GridPathfinder.TryFindPath` is 8-direction weighted A* (octile heuristic): orthogonal cost `1`, diagonal cost `√2`. A diagonal step is only legal when both orthogonal neighbors it would cut past are also walkable — this can never cut through a wall corner, and the Door-only Room-crossing rule above is unaffected by allowing diagonals elsewhere.
- Cable Length accounting, Path Simplification (`CablePathRenderer.Simplify`), and Base/Flow sharing one rendered path were already diagonal-correct by construction (Euclidean-distance- and dot-product-based, not axis-aligned special cases) — no changes were needed there for 8-direction support.
- **Room Generation data contract** (for the separate Room Generation Core worktree, not implemented here): `RoomPlan → Room Grid Bounds → Wall/Edge → DoorPlan → Grid occupancy update → CableRoutingGridService.RebuildFromScene()`.
- **Render-path endpoint correction principle**: `CableRoutingController.AppendOrthogonalJog` is the *only* place a non-8-direction angle may appear — it reconciles the two genuine sub-cell gaps (Origin's/a Socket's exact continuous position vs. the nearest grid cell center), skipping its own bend point when either axis gap is below `minRenderSegmentLength` (a visible-hook guard). Every interior point between those two gaps must be appended directly from the A* path, never re-jogged — routing an interior (already grid-aligned) cell through this jog logic was the root cause of a real zig-zag/hook bug at Wall Outlet approaches (TASK-20260918-006 round 2). `CablePathRenderer.Simplify` stays a plain collinear-merge; do not add a generic "merge short segments" pass there — it can combine two different-direction interior segments into a non-canonical angle.

### Wall Outlet mounting principle (long-term, confirmed)

A Wall Outlet is a Wall-mounted endpoint, not free-standing floor furniture like a Product or PowerStrip:

- It renders above the Wall it is mounted on (sorting order `4`, above the Wall's `2`/`3` — see above) so it is never visually embedded in/behind the wall.
- Its root rotates to match the Wall it is mounted against (parallel to a vertical Wall for a vertical Wall, parallel to a horizontal Wall for a horizontal one) so the body sprite hugs the wall face rather than sitting sideways to it. This is a per-instance placement choice (each instance's own Transform rotation), not a prefab default — a procedural Room Generator placing outlets later must compute this the same way (from the Wall it is mounting against), not copy one fixed rotation.
- It is never placed at a Wall/Wall corner or intersection.
- Its `SocketConnector` is a **terminal endpoint for Cable routing, never a pass-through**: a Cable may route to a Socket's exact position as the last step of connecting to it, but no routing path may continue past a Socket to whatever is on the far side of its Wall. Room-to-Room Cable movement remains Door-only, unconditionally — see the Door / Room routing principle above, which this does not relax.
- Concretely, `CableRoutingController` resolves a small, wall-thickness-scale **Approach Point** (the nearest walkable cell to the Socket's own — typically Wall-Blocked — cell, searched within `socketApproachSearchRadius`, deliberately much smaller than the general-purpose `nearestValidSearchRadius`) and treats it as the Socket's routable/acquirable anchor alongside the Socket's exact position. This keeps the Approach Point on the Socket's own room-interior side without adding a room-membership system: the search radius is simply too short to tunnel through a Wall to a different Room's floor on the far side.

### Connected vs. Powered (confirmed, P0-1E)

- **Connected**: Plug is physically plugged into a Socket (`PlugConnector`/`SocketConnector` pairing via `PlugSocketConnection`).
- **Powered**: Connected *and* `Octoplug.Power.Connection.PowerValidationService.TryValidate` passed at the moment of connecting. Stored on the Product's own `ApplianceSource.IsPowered` (set only by `CableRoutingController`, read-only elsewhere).
- `CablePowerFlowEffect.SetPowered` is only ever called with the real Powered result — never with bare Connected. `previewPowered` remains Inspector-only.
- Validation order at connect time: Socket connectable → Routing/CableLength → Power Validation → `PlugSocketConnection.Connect` → `ApplianceSource.SetPowered(true)` → Flow on. A failed Power Validation leaves no Plug/Socket reference at all (rejected before `Connect` runs), so there is nothing to unwind.
- `PowerValidationService` recomputes House/Strip usage from scratch every call (sum of `PowerConsumptionWatts` over currently-`IsPowered` products) instead of keeping a running counter — Disconnect therefore never needs separate decrement bookkeeping; setting `IsPowered = false` and letting the next validation recompute is sufficient by construction.
- A Wall Outlet's Socket has no `PowerStrip` ancestor, so only the House budget applies through it (matches "Wall Outlet has no own limit"). A PowerStrip's Socket additionally requires that strip's own `AllowedPowerWatts` to pass (`SocketConnector.GetComponentInParent<PowerStrip>()`).

### UI ↔ Code contract (confirmed, TASK-20260918-005 round 4)

- **Prefab visual design is source of truth.** `ProductInfoView`/`ProductTooltipController`/`UseInfoView` only toggle active state, pick a sprite, and read data — they never set a `RectTransform`, Scale, Position, Spacing, Layout, icon size, or color. If the current Prefab can't be data-bound as-is, that is Human Setup Required, not something the code silently changes or "fixes."
- **Power display = icon count only**, both in `ProductInfo` and `UI_ProductTooltip` — no numeric Label anywhere. If a product's `PowerConsumptionWatts` exceeds the icon pool currently authored in a Prefab, the display clamps and logs a `Debug.LogWarning` naming the product/needed/available counts — it is never silently presented as correct; growing the pool (or lowering the value) is a Human Setup / balance decision.
- **Sorting**: gameplay object sortingOrder values (Cable Base=5, Flow=6, Wall Outlet/PowerStrip Body/Head, Product Icon/Background) are entirely author-owned per Prefab and are never changed by code. `ProductInfo`/`UseInfo` (World Space Canvas, root canvas so `sortingOrder` applies directly) are set to `100` — comfortably above every current gameplay order — specifically to satisfy "info UI always on top," not to unify a numbering scheme. `UI_ProductTooltip` is under a `ScreenSpaceOverlay` Canvas, which already renders above all world-space content regardless of sortingOrder. A dragged Plug's sortingOrder is computed relative to its own Product's authored `Icon`/`Background` order (`CableRoutingController.EffectiveDraggingSortingOrder`, never below the authored `draggingSortingOrder` field) rather than a new hardcoded number, so it can't disappear behind a Product it's dragged over.

### Deferred TODO: P0-1E feel/polish gaps (TASK-20260918-005)

- No top-alert/warning UI exists anywhere in the project; `CableRoutingController` logs a `Debug.LogWarning` (distinguishing House vs. PowerStrip limit) as a functional stand-in only, not a UX deliverable.
- ~~No `PowerStrip`/Multitap instance exists in `InfiniteMode`~~ — resolved TASK-20260918-007: `Multitap_One`/`Multitap_Two` are placed and wired; the full Wall Outlet → PowerStrip → Product chain (`PowerValidationService.GetStripUsage`/`IsSocketSourceLive`/the Strip branch of `TryValidate`) is now verified live through real drag/drop, not just synthetic `eval` calls.
- `UsageTypeIconLibrary` currently maps exactly the 4 confirmed tags (Cooling/Heating/Meal/Fun) to the 4 confirmed `_filled` sprites; a new UsageType added later needs both a new enum flag and a new library entry.
- Heater's current `PowerConsumptionWatts` (6, a test placeholder) exceeds every current Power icon pool (5 in both `ProductInfo` and `UI_ProductTooltip`) — Human Setup Required (grow the pool, or lower the test value).

### Deferred TODO: Product overlap alpha fade

Not implemented yet. When Cable (Base and/or Flow) visually overlaps a Product/PowerStrip's sprite bounds, only that overlapping segment should have its alpha lowered — never the whole cable — to keep the product recognizable. Visual-only: must not affect Routing, Physics/Collider, or Input. Re-evaluate need and approach after a play test; Sorting Order alone (Cable behind Product) may already be sufficient.

### Deferred TODO: P0-1D feel/polish gaps (accepted for current demo scope, TASK-20260918-004 closed DONE)

- `socketAcquisitionRadius` (0.25), `socketDetachRadius` (0.4), `socketApproachSearchRadius` (3 cells), and the Plug's `connectedSortingOrder`/`draggingSortingOrder` values are feel-tuning placeholders, not confirmed balance/art decisions — expect them to move once more Sockets/products exist to test against.
- Wall Outlet rotation-to-match-its-wall is set manually per scene instance; there is no procedural "compute rotation from the Wall being mounted against" helper yet for a future Room Generator to call.
- The "never connect through a Socket to the far side of its Wall" rule is enforced structurally (a deliberately short Approach-Point search radius) and was verified against a pointer on the wrong/outer side of a Wall, but never against an actual second Room sharing that same Wall — none exists in the current test scene. Re-verify once two Rooms share a Wall with Sockets on it.

## Narrow Search Order

1. This map and the current Task.
2. The named prefab families and `InfiniteMode` hierarchy.
3. The connection/power pages in the concept PDF.
4. Broaden only if those sources do not answer the task.

### PowerStrip Powered chain (confirmed, TASK-20260918-007)

- `PowerStrip.IsPowered` mirrors `ApplianceSource.IsPowered` — set only by `CableRoutingController.ApplyPowered`, on the strip's own Cable/Plug connect-disconnect.
- `PowerValidationService.IsSocketSourceLive(socket)`: a Wall-Outlet-backed socket (no `PowerStrip` ancestor) is always live; a `PowerStrip`-backed socket is live only if that strip `IsPowered`. Separate from the existing wattage-only `TryValidate` gate — a Plug can physically connect to a currently-unpowered upstream Socket and just stay not-Powered until the chain goes live (real-life "plug into an unplugged strip" semantics).
- PowerStrip → PowerStrip chains are explicitly allowed with no fixed depth. Self-connection (`Plug Owner == Socket Owner`) and every directed graph cycle are rejected before `PlugSocketConnection.Connect`; traversal uses component ownership and visited sets, never prefab names.
- When a `PowerStrip`'s Powered value changes, `CableRoutingController` propagates Powered/Flow through every downstream Product/strip with visited-set safety. Disconnecting/depowering an intermediate strip depowers its complete downstream branch.
- Each PowerStrip budget includes the complete downstream product branch, including products behind nested strips; House usage remains the total Powered product consumption. Validation recomputes graph usage for accuracy rather than caching.
- **Finalized allowance balance (2026-09-19):** House current/initial allowance is `8` with independent maximum `22`. PowerStrip current/initial allowances are independent per prefab (`One=3`, `Two=3`, `Three=4`, `Four=5`, `Five=5`), with independent maximum `10` per strip. Socket count never derives Allowed Power and Allowed Power never derives socket count. Reward/upgrade mutation is not established yet; these maxima are serialized read-only balance data until that future system is scoped.
- UI capacity follows the same independent contract: House has 22 authored icons; the shared PowerStrip meter currently has 5, enough for every finalized initial allowance but not future values 6–10. Icon count must never clamp or mutate authoritative Allowed Power.

### Persistent PowerStrip socket capacity (established, TASK-20260918-007 follow-up)

- `Assets/03_Prefabs/Multitaps/Multitap.prefab` is the single variable-capacity source prefab. Persistent `Socket01`–`Socket05`/`SocketConnector`s, `Whole01`–`Whole05`, Cable/Plug, PowerInfo, `PowerStripPowerInfoBinding`, and `PowerStrip` identity are authored once. Capacity changes never replace, instantiate, or destroy them at runtime.
- `PowerStrip.InitialSocketCount` initializes runtime `ActiveSocketCount`; `InitialAllowedPowerWatts` independently initializes runtime `AllowedPowerWatts`. `CableInfo.CableLength` is a third, per-Cable authored authority. Socket capacity, electrical allowance, and cable length must not derive, reset, or mutate one another.
- Gameplay graph traversal uses `PowerStrip.ActiveSockets`. The complete ordered `Sockets` list remains available for identity, diagnostics, and safe capacity transitions. Pointer discovery, magnetic acquisition, validation, final connection mutation, usage/cycle traversal, and power propagation all reject inactive strip sockets defensively.
- `TrySetActiveSocketCount` is an atomic, increase-only in-place transaction. It rejects out-of-range or incomplete configuration, rejects every decrease with `SocketCountDecreaseUnsupported`, treats a same-count request as a successful no-op, and preflights the prospective placement reservation before committing. Failure leaves count, visuals, reservation, connections, Allowed Power, Powered state, Flow, PowerInfo, and binding unchanged.
- Head hit testing and variable-size placement use the explicit prefab-authored child `BoxCollider2D` Size/Offset data through `PowerStripSocketLayout`; Renderer bounds are not an authority. For count N, geometry is the union of active module colliders plus only module N's terminal-End colliders.
- Count-specific PowerInfo local positions are finalized authored data in the single source prefab: count 1 `(-0.23, 0.5)`, count 2 `(-0.46, 0.5)`, count 3 `(-0.69, 0.5)`, count 4 `(-0.92, 0.5)`, count 5 `(-1.15, 0.5)`. Capacity changes move only the persistent PowerInfo Transform to the matching position; they do not change its scale, sprite, layout, hierarchy, color, identity, or binding.
- Verified matrix: source `1→5` PASS with Allowed Power `7` preserved; connected `2→3` PASS with identities, connections, Usage, Powered, and Flow preserved; inactive-socket exclusion PASS; blocked expansion atomic rollback/reservation PASS; `3→2` rejected with `SocketCountDecreaseUnsupported` and no mutation.
- `InfiniteMode` migration is not complete or verified. The current local scene diff contains an incomplete migration attempt (two legacy instances absent, only one new-source generic Multitap present), so the scene is excluded from the checkpoint commit. A correct two-for-two Editor migration and readback—or restoration of only that incomplete migration portion while retaining legitimate House budget data—requires an explicit decision. Wall Outlet single-prefab work has not started in TASK-007.

## Current Verification Status (TASK-20260918-007)

- Build: **PASS**, 0 warnings/0 errors. Unity recompile: `up_to_date`. Matching-Editor `verify-unity`: **PASS**.
- Unity project tests: zero discovered — **NO_PROJECT_TESTS**, not PASS.
- `verify-fast.ps1`: **FAIL** (exit 1) only on Unity-generated trailing spaces in `Multitap.prefab`; do not report PASS or manually clean prefab YAML.
- Real pointer behavior: **HUMAN_VERIFY_REQUIRED**. Direct/reflection runtime checks are not pointer-input PASS.

## Planned, Not Established

- Cable Length strengthening is named in the Infinite Mode reward roadmap, but no CableLength upgrade state/setter or coupling to socket/power upgrades exists. `CableInfo.CableLength` currently remains an authored prefab-instance value, and Head-drag-beyond-Cable-Length behavior remains a pending Human Decision.
- Wall Outlet single-prefab consolidation, Door procedural generation, product outline feedback, audio, Reward mechanics, and project test assemblies have not been implemented yet.
