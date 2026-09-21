# Infinite Mode Domain

## Responsibility

Coordinate the priority demo session across connection/power, resident demand, room generation, reward/progression, products, and UI.

## Existing Core Files

- `Assets/00_Scenes/Demo/InfiniteMode.unity`
- `Assets/03_Prefabs/Rooms/Room.prefab`
- `Assets/03_Prefabs/UI/UI_ProductTooltip.prefab`
- `Assets/03_Prefabs/UI/UI_ResidentCard.prefab`
- Product, multitap, and wall-outlet prefab families under `Assets/03_Prefabs/`.

## Scene Evidence

The scene contains:

- `Gameworld/House/Floor/Grid` and `Rooms`.
- `Products` and `Connections` roots.
- `Connections/Multitaps` and `Connections/Wall_Outlets`.
- `Canvas/Residents_Area` and `UI_ProductTooltip`.
- Empty `GameManager`, `ConnectionDirector`, `ResidentController`, and `RoomGenerator` mount points.

The established Demo build flow is `Lobby → InfiniteMode → Result → Lobby`; build order is Lobby, InfiniteMode, Result, then the preserved SampleScene.

## Session Flow Boundary

- `DemoSceneFlow` centralizes scene loading and resets stale result state, input lock, and time scale before a new session.
- `SessionProgressState` owns exactly-once final Demand Success/Failure counts alongside Satisfaction, EXP, and Room progression.
- `GameOverResultTransition` snapshots authoritative Room/Solved/Failed values before loading Result and guards duplicate transitions.
- `SessionResultStore` is transient runtime memory and resets at subsystem registration; PlayerPrefs is not used.
- Lobby and Result bind only existing Button/TMP instances. Survey and Guide remain unbound until authoritative destinations exist.

## Design Direction

Infinite Mode is the highest-priority demo content and runs the documented demand → connection → satisfaction → reward → room/resident expansion loop.

Escalation, difficulty curve, Survey destination, and Guide destination remain Open Decisions.

## Related Domains

All initial Domain Maps. Infinite Mode integrates them; it should not absorb their internal rules.

## Modification Cautions

- The scene and its shared prefabs are Exclusive Assets.
- Build-setting changes require a scoped scene-flow task and explicit Exclusive Assets.
- Do not change hierarchy, lighting, camera, sorting, UI layout, or manager components without a scoped task and Human Decision where needed.
- Drive scene integration through a live Editor in an isolated worktree; do not hand-edit YAML.

## Narrow Search Order

1. Current Task and this map.
2. `InfiniteMode` hierarchy plus the specific related Domain Map.
3. Named shared prefabs.
4. Relevant concept PDF section.
5. Avoid loading every domain or asset family by default.

## Planned, Not Established

The session state machine, orchestration APIs, scene bootstrap, navigation, save/load, and tests are not established.
