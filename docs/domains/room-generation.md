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

## Production Room Content

- `DefaultRoomContentBalance` is the validated local runtime mirror of the Sheet-authored RC001–RC007 room configurations; runtime generation does not access the Sheet or network.
- Every gameplay-active room finalizes its required Wall Outlet before Product placement.
- Product placement exhaustively searches full-footprint grid candidates, rejects positive-area authored-bounds overlap with Products/PowerStrips, and requires a production-routed path to a room-owned outlet within the initial cable length.
- Accepted Products reserve their placement cells synchronously. A selected RoomConfig succeeds only when its exact Product type/count composition is finalized; a shortfall rolls back that room-content transaction and suppresses `RoomContentReady`.

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

- **Production Integration Preflight must be re-run** against the then-current `feat/infinity-power-connection` once `TASK-20260918-007` (PowerStrip Placement + Drag + Power Chain, currently active) is committed/pushed and merged into it. `TASK-20260918-007` is actively changing pointer-interaction structure (`PointerInteractionResolver`, `HeadDragInput`, `PowerStrip`), so any Preflight taken before that lands does not reflect the real merge target.
- **Room Generation must not create a second production grid.** Room Generation Core (`RoomBounds2D`/`RoomLayout`, world-unit, grid-independent by design) must connect to the *existing* production grid, not a parallel one:
  `RoomPlan` → existing `CableRoutingGrid`/`GridPathfinder` → Wall/Door cell updates → `CableRoutingGridService` rebuild.
  This connection is unimplemented; it is a required step of the future Production Integration Task, not an Open Decision to relitigate.
- Production transfer of the copied binder/controller pattern into the then-current `Room.prefab` and `InfiniteMode.unity` after their current Exclusive Asset owner releases them. **`Assets/00_Scenes/Demo/RoomGenerationTest.unity` and `Assets/03_Prefabs/Rooms/Room_RoomGenTest.prefab` are not production copy targets** — nothing in either file is meant to be duplicated into `InfiniteMode.unity`/`Room.prefab`. Only these carry forward: the pure core (`Assets/01_Scripts/RoomGeneration/`), the `RoomGenerationRoomBinder` binder pattern, the `RoomGenerationTestController` controller pattern, the Camera adapter/controller pattern (see [Camera Framing](camera-framing.md)), and the pure test suites. Production integration means **re-wiring these patterns fresh against whatever `InfiniteMode.unity`/`Room.prefab` actually look like at that time** (their structure will have moved since this Task; do not assume it matches the copied test assets), not copying scene/prefab files.
- Production candidate construction and ordering; the deterministic balanced ordering is established only for the copied validation integration.
- Production Door policy ownership; the copied validation integration measures the authored Door and wall geometry and uses the widest-safe-interval midpoint policy.
- Progression/session orchestration, Resident/Product/Socket activation, camera framing, and no-successor behavior.
- Approved resize-safe locked-room hatching art.
- Persistence and deterministic seed ownership.

## Future Production / Game Flow Integration

Not implemented; recorded so a later Game Flow Task does not let Room Generation absorb responsibilities that belong to session orchestration.

See also: `docs/decisions/infinity-progression-and-reward-loop.md` — the authoritative confirmed design.

### Room Generation responsibilities (confirmed)

- Room planning (RoomPlan, candidates, bounds)
- Hint state: HintLocked / UnlockedGenerated
- Promoting the stored hint plan on unlock
- Planning the next hint after promotion
- Door planning and application
- Room content generation hooks (Products, Wall Outlets inside the new room)
- Emitting generation events for external orchestration

### Room Generation must NOT

- Compute EXP or judge Level Up
- Judge Reward eligibility or start the Reward Phase
- Calculate or update Satisfaction
- Write to `UI_RoomInfo` or `GameStatusInfo` directly
- Decide *when* a Room should unlock — that belongs to `GameManager` / `GameFlowController`

### Future GameFlow hooks (intent catalogue; exact names match production style at implementation time)

| Hook | Direction | Intent |
|---|---|---|
| `RoomGenerated` | Room Generation → GameFlow | New room instance exists in scene |
| `RoomUnlocked` | Room Generation → GameFlow | Hint promoted to unlocked |
| `RoomContentReady` | Room Generation → GameFlow | Products / Wall Outlets inside room are ready |
| `NextHintCreated` | Room Generation → GameFlow | Next locked hint room planned and placed |

GameFlow must orchestrate the Level-Up sequence through these events/APIs without
polling the Scene hierarchy.

### Expected API boundary (exact names TBD)

- `GameManager` → `PromoteCurrentHint()`: promote the exact stored hint plan.
- `GameManager` → `CreateNextHint()` or combined: plan the following hint.
- Room Generation → `HintRoomCreated` (already established; unchanged).

### Confirmed Level-Up sequence (owned by future GameFlow, not Room Generation)

```
Resident Need resolved
→ EXP increases
→ EXP Max reached
→ Room Generation requested → RoomGenerated / RoomContentReady
→ Camera Reveal requested → CameraRevealCompleted
→ Game Pause + 1-second wait
→ Reward 3-choice
→ Reward applied
→ Game resumes
```

Room Generation fires first; Camera Reveal follows; Reward comes last.

- `UI_RoomInfo` reads unlocked-Room-count from `GameManager` / a UI coordinator, not from Room Generation.
- `GameStatusInfo` reads EXP/Level state the same way.
- Room Generation exposes state (`RoomGenerationState`/`UnlockedLayout`) for that reader.
