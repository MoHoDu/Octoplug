# Room Generation Domain

## Responsibility

Represent rooms, their visual locked/unlocked states, placement relationships, and procedural expansion of the house.

## Existing Evidence

- `Assets/03_Prefabs/Rooms/Room.prefab` with `Walls`, `Doors`, `Door`, `Quarter`, `Locked`, and `Unlocked` design anchors.
- `Assets/00_Scenes/Demo/InfiniteMode.unity` contains `Gameworld/House/Floor`, `Grid`, `Rooms`, a Room prefab instance, and an empty `RoomGenerator` object.
- House artwork exists under `Assets/02_Resources/Art/House/`.

No room-generation code, data model, tests, or approved placement rules exist.

## Design Direction

The concept document describes procedural room generation and unlocking a new room and resident as progression advances. Unreleased areas use fog/dim visual treatment.

Adjacency, door matching, placement, unlock order, generation constraints, room size, and failure/retry behavior are Open Decisions.

## Related Domains

- Infinite Mode owns session-level expansion.
- Reward / Progression triggers or purchases unlocks.
- Resident / Demand introduces residents associated with new rooms.

## Modification Cautions

- `Room.prefab` is instantiated by `InfiniteMode`; prefab or scene changes require Exclusive Assets.
- Layout, door positions, locked/unlocked presentation, fog, and camera framing are player-facing.
- Do not infer procedural algorithms from current transforms or object names.

## Narrow Search Order

1. This map and current Task.
2. `Room.prefab` and the `Gameworld/House` scene subtree.
3. Room/progression pages in the concept PDF.
4. Broaden only when evidence is insufficient.

## Planned, Not Established

Generation algorithms, room data, deterministic seeds, persistence, validation, and tests are not established.
