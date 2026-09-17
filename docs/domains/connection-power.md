# Connection / Power Domain

## Responsibility

Represent how wall outlets, cables, multitaps, and appliances form a power network and communicate capacity/consumption state.

## Existing Evidence

- `Assets/03_Prefabs/Products/Cable.prefab`
  - `CableOrigin`, `Plug`, `Handle`, `Line`, and `ElectricEffects` design anchors.
- `Assets/03_Prefabs/Multitaps/Multitap_One.prefab` through `Multitap_Five.prefab`.
- `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet_One.prefab` through `Wall_Outlet_Five.prefab`.
- Product prefabs under `Assets/03_Prefabs/Products/`.
- `Assets/03_Prefabs/Multitaps/PowerInfo.prefab`.
- `Assets/00_Scenes/Demo/InfiniteMode.unity` roots: `Connections`, `Multitaps`, `Wall_Outlets`, and `ConnectionDirector` (now hosts `HousePowerBudget` + `CableRoutingGridService`).
- `Assets/01_Scripts/Power/` — data foundation (`CableInfo`, `PlugConnector`, `SocketConnector`, `ApplianceSource`, `PowerStrip`, `WallOutlet`, `RoomArea`, `HousePowerBudget`) and `Power/Grid/` (`CableRoutingGrid`, `CableRoutingGridService`).
- `Assets/01_Scripts/Power/Routing/GridPathfinder.cs`, `Power/Input/PlugDragInput.cs`, `Power/Cable/CablePathRenderer.cs` + `CableRoutingController.cs` — Plug drag, orthogonal shortest-path routing, and Cable-length-limited rendering (P0-1C). Socket connection, power validation, and PowerStrip movement are not implemented yet.
- `Power/Cable/CablePowerFlowEffect.cs` (P0-1C.1) — lives on the Cable root (alongside `CableInfo`/`CableRoutingController`, e.g. `TV/Connection`), not on `ElectricEffects`, specifically so it is discoverable one level under the Product/PowerStrip in the Hierarchy. It only shows/hides and scrolls the Flow LineRenderer on `ElectricEffects`; `SetPowered(bool)` is the real entry point, `previewPowered` is an Inspector-only test toggle and must never become a game rule. Base Cable sorting order is `-1`, Flow is `0`, Product/Multitap sprites stay at `1` (unchanged) — this is the free integer slot between Base Cable and Product. `CableFlow.mat` uses transparent `Universal Render Pipeline/Unlit` because the original `Sprites/Default` shader ignored `_MainTex_ST`, making runtime texture-offset animation visually stationary.
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

### Deferred TODO: Product overlap alpha fade

Not implemented yet. When Cable (Base and/or Flow) visually overlaps a Product/PowerStrip's sprite bounds, only that overlapping segment should have its alpha lowered — never the whole cable — to keep the product recognizable. Visual-only: must not affect Routing, Physics/Collider, or Input. Re-evaluate need and approach after a play test; Sorting Order alone (Cable behind Product) may already be sufficient.

## Narrow Search Order

1. This map and the current Task.
2. The named prefab families and `InfiniteMode` hierarchy.
3. The connection/power pages in the concept PDF.
4. Broaden only if those sources do not answer the task.

## Planned, Not Established

Plug ↔ Socket connection state transitions, power/capacity validation (product, strip, house), PowerStrip movement, Door procedural generation, power-flow visuals, product outline feedback, audio, and tests have not been implemented yet.
