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
- `Assets/00_Scenes/Demo/InfiniteMode.unity` roots: `Connections`, `Multitaps`, `Wall_Outlets`, and empty `ConnectionDirector`.

The manager name is an intended mount point, not implemented behavior.

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

## Narrow Search Order

1. This map and the current Task.
2. The named prefab families and `InfiniteMode` hierarchy.
3. The connection/power pages in the concept PDF.
4. Broaden only if those sources do not answer the task.

## Planned, Not Established

Custom C# contracts, runtime graph representation, validation algorithms, tests, and serialization locations have not been decided or created.
