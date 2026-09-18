# Room Generation Domain

## Responsibility

Represent rooms, their visual locked/unlocked states, placement relationships, and procedural expansion of the house.

## Existing Evidence

- `Assets/03_Prefabs/Rooms/Room.prefab` with `Walls`, `Doors`, `Door`, `Quarter`, `Locked`, and `Unlocked` design anchors.
- `Assets/00_Scenes/Demo/InfiniteMode.unity` contains `Gameworld/House/Floor`, `Grid`, `Rooms`, a Room prefab instance, and an empty `RoomGenerator` object.
- House artwork exists under `Assets/02_Resources/Art/House/`.

## Existing Core

- `Assets/01_Scripts/RoomGeneration/` contains an engine-independent calculation and state core.
- `Tests/RoomGeneration.Core.Tests/` runs the same pure sources outside Unity Editor.
- The core accepts caller-ordered candidates and explicit positive Door width/margin values; it does not choose room sizes, randomness, weights, or balance constants.

Current rules:

- Rooms use axis-aligned bounds and exact top/bottom/left/right adjacency.
- Positive-area overlap is rejected; corner-only contact is not adjacency.
- Doors are planned only on positive-length shared walls, clear supplied corner/intersection margins, and match the wall orientation.
- Each complete room wall can be occupied by at most one door. A room can have several doors on distinct walls.
- An accepted RoomPlan has at least one valid DoorPlan.
- `HintLocked` and `UnlockedGenerated` visual intent is data-only; the next hint plan is stored separately and the exact stored plan is promoted on unlock.

## Verified Test Integration

- `Assets/00_Scenes/Demo/RoomGenerationTest.unity` and `Assets/03_Prefabs/Rooms/Room_RoomGenTest.prefab` are isolated copied validation assets; they do not replace the production scene or prefab.
- `BalancedFrontierCandidateGenerator` builds deterministic variable-size candidates from the seed using adjacency-graph BFS depth, shortest cardinal extent, rotating `Right → Top → Left → Bottom` ties, rotating footprint priority, and centered-first shared-edge alignments.
- The copied prefab currently measures an 8×6 base footprint and produces 8×6, 12×6, 8×9, and 12×9 test footprints without scaling Room roots.
- `WidestSafeIntervalMidpointPolicy` selects exactly one Door at the midpoint of the globally widest safe center interval. Door width and safety margin are measured at the Unity boundary from authored Door geometry and half the authored wall-collider thickness.
- `RoomGenerationRoomBinder` resizes wall endpoints, wall colliders, and the existing floor collider while preserving authored materials, styling, overhangs, Door geometry, and unit Room scale.
- `RoomGenerationRoomBinder.CenterLockIcon()` positions the locked-hint lock icon at the Room's true visual center (authored bounds + current placement size, not the transform pivot or a hardcoded offset) on every `ApplyPlacement`, so it stays centered across every variable footprint.
- `DoorViewTransformMapper` opens both connected walls, renders one Door, and restricts runtime rotation to Z 0/90/180. A Top candidate opening is canonicalized to the connected Room's Bottom wall.
- `RoomGenerationTestController` renders one stored locked hint and promotes that exact stored plan before separately planning the next hint.
- Locked hatching remains unavailable because no approved resize-safe hatching anchor exists; the copied test uses dashed walls plus the lock icon and reports `ART_DESIGN_REQUIRED`.

## Design Direction

The concept document describes procedural room generation and unlocking a new room and resident as progression advances. The copied test integration verifies the Room hint/unlock lifecycle and rendering contract; production progression/session ownership remains separate.

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

- Production transfer of the copied binder/controller pattern into the then-current `Room.prefab` and `InfiniteMode.unity` after their current Exclusive Asset owner releases them.
- Production candidate construction and ordering; the deterministic balanced ordering is established only for the copied validation integration.
- Production Door policy ownership; the copied validation integration measures the authored Door and wall geometry and uses the widest-safe-interval midpoint policy.
- Progression/session orchestration, Resident/Product/Socket activation, routing-grid rebuild, camera framing, and no-successor behavior.
- Approved resize-safe locked-room hatching art.
- Persistence and deterministic seed ownership.
