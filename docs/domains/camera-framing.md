# Camera Framing Domain

## Responsibility

Own the 2D orthographic Camera's manual zoom, empty-world drag Pan, dynamic House-based limits, and one-shot zoom-only framing that keeps a newly generated Hint Room visible without moving the Camera.

## Engine-Independent Core

`Assets/01_Scripts/CameraFraming/` is a `noEngineReferences` calculation library. It references `Octoplug.RoomGeneration` only for `RoomBounds2D` and `Point2D`. `Tests/CameraFraming.Core.Tests/` compiles the same sources outside Unity.

Current rules:

- `ZoomRange` is an inclusive positive Orthographic Size range.
- `DynamicZoomLimit` derives maximum manual zoom-out from the union of actual generated Room bounds, actual Camera aspect, and a caller-provided margin. It never uses room-count or fixed-coordinate assumptions.
- `ZoomInputMapper` maps wheel/touchpad scroll and pinch distance deltas; device reading remains outside the core.
- `CameraDesignReference` records 1920x1080 / 16:9 as the tuning reference only. Runtime calculations always use actual `Camera.aspect`.
- `HintFramingCalculator` fits the complete Hint min/max bounds plus margin at the current Camera center, never moves the Camera, never forces zoom-in, and has no unlocked-House upper clamp.
- `PanDeltaMapper` converts pointer pixel displacement into opposite Camera world displacement using current Orthographic Size, actual aspect, viewport dimensions, and sensitivity.
- `PanBoundaryCalculator` derives legal Camera-center ranges from actual House bounds plus margin and current viewport half-extents. If the viewport exceeds the bounded House on an axis, that axis is collapsed and Pan retains its current coordinate.
- `PanGestureTracker` latches ownership as `CameraPan` or `GameplayBlocked` at Pointer Down. `MultiTouchSuppressed` prevents Pan from resuming from a remaining pinch finger; all touches must release before a new Pan can begin.

## Unity Integration

The production integration is wired fresh in `Assets/00_Scenes/Demo/InfiniteMode.unity`. The isolated `RoomGenerationTest.unity` scene remains reference-only and is not a production copy target.

### Zoom and Hint framing

- `CameraZoomInputReader` reads `Mouse.current.scroll` and two-finger `Touchscreen` pinch through the New Input System. Its Inspector fields are `desktopScrollZoomSensitivity` (Mouse Wheel + Windows touchpad scroll, which the Input System cannot reliably tell apart, so they intentionally share one field) and `pinchZoomSensitivity` (independent, mobile pinch only); both default to `0.2`.
- `HouseCameraZoomController` exclusively owns `CinemachineCamera.Lens.OrthographicSize`, including smoothing, the fixed minimum size `5`, dynamic House maximum, and full-bounds Hint framing.
- A new Hint may require more zoom-out than the unlocked-House maximum because it is not yet in that union. Hint framing widens the effective range rather than clipping the Hint; later House recomputation preserves the larger current target so promotion does not implicitly zoom in.

### Pan ownership and movement

- `CameraPanInputReader.Awake()` corrects Unity's Editor Play Mode default (`InputSystem.settings.editorInputBehaviorInPlayMode`), which otherwise gates pointer button state behind Game View OS focus while mouse-wheel scroll bypasses that gate — the exact reason empty-world Mouse Drag could silently do nothing while Zoom still worked in the same session. The override is applied in memory only, guarded by `#if UNITY_EDITOR`, writes no `ProjectSettings` asset, and has no effect in a build.
- `CameraPanInteractionResolver` adapts Camera Pan to the authoritative production `PointerInteractionResolver`. It requests Empty World capture only on Pointer Down; the production resolver applies UI, Plug, Product, Socket, eligible PowerStrip Head, and explicit-blocker priority.
- Broad Room floor/wall/door colliders do not block empty-world Pan.
- `CameraPanInputReader` separately owns Mouse and single-touch Pan input. It reads `panSensitivity` live, latches Down ownership until Up, suppresses emulated Mouse while touch is active, and yields completely to pinch during multitouch.
- `HouseCameraPanController` moves only the `CinemachineCamera` transform. It applies the latest cached "visible house" bounds (see below), the **current** Lens size, the **current** output aspect, and `panBoundaryMargin` — recomputed fresh on every single Pan request, never cached as a fixed world-space range. Zoom In/Out therefore widens/narrows the reachable range immediately, with no Room Generation event required. Main Camera is never arbitrarily transformed.
- Boundary clamping occurs only when Pan is requested; Zoom, aspect change, House growth, and Hint framing do not themselves recenter or move the Camera.

### Room Generation bridge

- `RoomGenerationCameraFramingBridge` is the sole Room Generation → Camera connection.
- On `ProductionRoomGenerationController.NextHintCreated`, it computes two distinct bounds from the same unlocked Room list: the **unlocked-only** house bounds (fed to `RecomputeDynamicMaxOrthographicSize`) and **visible-house** bounds — unlocked Rooms plus the current Hint's bounds (fed to `cameraPan.UpdateHouseBounds`). It then requests unchanged full-bounds Hint framing.
- Future GameFlow may call `RequestRoomReveal(RoomPlacement)` after Room content is ready. The bridge forwards the zoom-only request and emits `CameraRevealCompleted` only after the smoothed target is reached (or immediately when no zoom-out is needed).
- Room Generation remains unaware of Camera, Cinemachine, Zoom, or Pan.

## Verification Evidence

- Camera Framing pure suite: 46 passing tests; strict build has 0 warnings/errors.
- Room Generation regression suite: 66 passing tests.
- Actual Input System mouse state events verified empty-world Pan direction and latched ownership in Play Mode.
- Runtime probes verified gameplay-blocked crossing, dynamic expanded-House ranges, exact boundary clamps, and Hint framing with unchanged panned X/Y.
- Unity recompilation and a clean Play Mode restart reported zero Console errors. Existing copied-scene warnings are documented in the Task handoff.
- Unity discovered zero project tests (`NO_PROJECT_TESTS`); pure tests and focused runtime probes are recorded separately.
- Human Play Check (Task 006, DONE): the user confirmed PASS on Camera Pan feel, Zoom In reaching House/Hint edges via the boundary fix, Zoom/Hint framing, and Room Generation/Hint/Door functionality. Physical Touch/Pinch feel was exercised as part of that same sign-off.

## Design Direction

The concept document does not define detailed Camera feel. Production `InfiniteMode.unity` now owns the verified Zoom/Hint/Pan adapters and an external Room Reveal completion boundary. Future GameFlow orchestration and cinematic behavior remain deferred.

### Camera Reveal (confirmed 2026-09-20)

On Level Up, the Camera must automatically zoom out to show the newly generated Room to
the player before the Reward Phase begins.

- Camera Reveal is a zoom-out operation (similar in mechanism to Hint framing, but
  triggered by the GameFlow rather than by Room Generation's HintRoomCreated event).
- Camera does **not** decide when to reveal; it responds to a request from future
  `GameManager` / `GameFlowController`.
- Camera must emit a `CameraRevealCompleted` event (exact name at implementation time)
  so GameFlow knows when to proceed to the Reward Phase.
- Camera is **not** responsible for EXP, Level Up judgment, Satisfaction, or Reward.

See: `docs/decisions/infinity-progression-and-reward-loop.md` section 7 and 8.

## Related Domains

- [Room Generation](room-generation.md) supplies `NextHintCreated` and actual `RoomLayout`/`RoomPlacement` bounds.
- Infinite Mode owns the production Camera hierarchy and fresh component/reference wiring.
- `Octoplug.Power.Input.PointerInteractionResolver` is the sole production pointer-ownership authority. `CameraPanInteractionResolver` is only its screen-to-world adapter for Empty World capture.

## Modification Cautions

- Preserve Pointer Down ownership through release; never transfer an object-started gesture to Pan or an empty-world Pan to gameplay while crossing boundaries.
- Preserve priority: UI → Plug → Socket/PowerStrip → Product → explicit gameplay → empty-world Pan.
- Preserve pinch priority and full-sequence multitouch suppression; Pan and pinch must not run simultaneously.
- Do not turn broad Room colliders into Pan blockers.
- Hint framing must remain zoom-only and position-preserving.
- Do not merge Pan ownership into Zoom input/ownership without a new approved design decision.
- Do not reintroduce a Room Generation dependency on Camera code.
- `com.unity.cinemachine` is user-installed; do not reinstall it or change its pinned version.

## Narrow Search Order

1. This map and current Task 006.
2. `Assets/01_Scripts/CameraFraming/` and `Assets/01_Scripts/CameraFraming.Unity/`.
3. [Room Generation](room-generation.md) for `HintRoomCreated` and actual bounds.
4. Interaction-domain files only when explicit hit semantics must be verified.
5. Broaden only when evidence is insufficient.

## Open Decisions

- **`Assets/00_Scenes/Demo/RoomGenerationTest.unity` is not a production copy target.** Production integration is wired fresh against `InfiniteMode.unity`; retain the test scene only as historical verification reference.
- Any automatic Camera relocation; Hint framing and Room Reveal remain zoom-only after manual Pan.
- Cinematic/session-level framing beyond the external Room Reveal request/completion boundary.
