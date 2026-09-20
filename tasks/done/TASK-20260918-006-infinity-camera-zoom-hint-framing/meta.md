# Task Metadata

- **ID:** TASK-20260918-006
- **Title:** Infinity Camera Zoom + Hint Room Framing + Pan
- **Status:** done
- **Owner:** User
- **Agent:** Claude
- **Domain:** Camera Framing
- **Base:** dev
- **Branch:** feat/infinity-room-generation-core (existing isolated worktree branch)
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** Done — Camera Zoom, Hint Room framing, and Camera Pan (with Zoom-dependent, Hint-inclusive boundary) all verified PASS
- **Current Skill:** verify-work / integrate-unity-editor (complete)

## Allowed Scope

- Pure Camera Framing additions under `Assets/01_Scripts/CameraFraming/` and focused editor-free tests.
- Unity adapters under `Assets/01_Scripts/CameraFraming.Unity/`.
- A minimal, additive `RoomGenerationTestController.HintRoomCreated` event (Room Generation announces a hint was created; it takes no dependency on the camera).
- Wiring new camera objects/components into the existing copied test scene `Assets/00_Scenes/Demo/RoomGenerationTest.unity` (already an Exclusive Asset from Task 005).
- This Task, `docs/domains/camera-framing.md`, and `docs/domains/INDEX.md`.
- `com.unity.cinemachine` is already installed by the user; this task's Packages/manifest diff only reflects that pre-existing install.

## Do Not Modify

- `Assets/00_Scenes/Demo/InfiniteMode.unity` and `Assets/03_Prefabs/Rooms/Room.prefab`.
- Room Generation's core algorithm, Door generation, or the Room prefab's design.
- Existing Power, UI, Product, Resident, art, material, audio, or ProjectSettings assets.
- Do not reinstall Cinemachine or change its pinned version.

## AI Setup Allowed

- Add a `CinemachineBrain` to the copied scene's `Main Camera` and a new `CinemachineCamera` + input/controller/bridge GameObjects under `/Managers/CameraSystem` in the copied test scene.
- Wire typed serialized references between the new components through the targeted Unity Editor.

## Exclusive Assets

- `Assets/00_Scenes/Demo/RoomGenerationTest.unity` (shared with Task 005; already exclusive to this worktree).

## Human Decisions

- 2026-09-18 — Minimum orthographic size is fixed at 5 and is never crossed by manual zoom or hint framing.
- 2026-09-18 — Dynamic max zoom-out is computed from the actual generated house bounds plus an Inspector-tunable margin; never hardcoded to a room count.
- 2026-09-18 — Hint framing only zooms out from the current camera position; it never zooms in and never moves the camera. The complete Hint Room bounds plus margin must fit, so a new hint may widen the effective manual zoom range beyond the unlocked-House dynamic max.
- 2026-09-18 — Mouse/single-touch drag beginning on empty world owns Camera Pan until release; UI, Plug, Socket/PowerStrip, Product, and explicit gameplay interactions take priority at Pointer Down. Pinch cancels Pan and owns multitouch. Pan uses actual House bounds plus an Inspector margin and does not move during Hint framing.
- 2026-09-18 — Room Generation and the camera are connected only through one loose event (`HintRoomCreated`); no general event bus.
- 2026-09-18 — Editor Play Mode's default `InputSystem.settings.editorInputBehaviorInPlayMode` (Game-View-focus gating of pointer/keyboard state) is corrected to `AllDeviceInputAlwaysGoesToGameView` in memory only, from `CameraPanInputReader.Awake()`, guarded by `#if UNITY_EDITOR`. This never touches a persisted `ProjectSettings` asset and has no effect in a build; it only fixes empty-world Mouse/Touch Pan silently failing to start inside the Editor Game View during testing.
- 2026-09-18 — Mouse Wheel and Touchpad scroll sensitivity remain one shared field (`desktopScrollZoomSensitivity`) because the New Input System cannot reliably distinguish them on this platform; Pinch sensitivity (`pinchZoomSensitivity`) is independent. Mouse and single-touch Pan sensitivity remain one shared field (`panSensitivity`) because both convert the same on-screen pixel distance through the same current Orthographic Size and already feel identical; none of these are force-split into fake per-device fields.
- 2026-09-18 — Pan Boundary is never a cached/fixed world-space range: it is recomputed from the current House bounds, current Orthographic Size, current Camera aspect, and `panBoundaryMargin` on every Pan request, so Zoom changes widen/narrow the reachable range immediately with no Room Generation event required. The "House bounds" fed into Pan boundary is the unlocked House **plus the current HintLocked room**, so a Hint sitting just outside the unlocked footprint is always fully reachable by Pan; Zoom's own separate dynamic-max-zoom-out policy (unlocked-only) is unchanged.

## Open Decisions

- Hint auto-framing remains zoom-only even after manual Pan; any future automatic camera relocation remains a separate Human Decision.
- Production camera ownership and transfer of this pattern into `InfiniteMode.unity` are out of scope for this test-integration task.

## Verification State

- Automated logic (pure unit tests) and Play Mode runtime verification completed; see `handoff.md`.
- 2026-09-18 user PlayMode round: PASS on Mouse Wheel/Touchpad zoom, min-5 clamp, hint auto zoom-out, camera-position hold, and no-op when a hint is already visible. Two tuning requests (zoom felt too slow; needed an explicit 1920x1080/16:9 design reference) were addressed: sensitivity defaults raised 5x with live-Inspector-effect re-verified, and a documented `CameraDesignReference` (16:9) constant plus dedicated design-aspect tests were added without changing any aspect-handling behavior (it was already aspect-general and correct).
- The subsequent full-bounds defect was corrected: the bridge already passed actual `hint.Bounds` and the calculator already considered min/max edges, but the correct required size was capped by a dynamic maximum derived only from unlocked Rooms. Hint framing now applies the uncapped full-bounds requirement and widens the effective range when needed. Pure tests are 31/31 and actual initial + eight generated hints independently passed viewport-edge containment checks.
- The user passed all Zoom/Hint framing checks, then passed Camera Pan wiring/regressions but reported three real PlayMode issues: empty-world Mouse Drag produced no Pan at all, Zoom sensitivity still felt too slow, and Zoom sensitivity fields were not clearly discoverable/separable per input device in the Inspector. All three were root-caused and fixed.
- The user then confirmed Pan input itself now works, Zoom sensitivity/Inspector are clear, and Hint full-bounds framing is correct, but reported that Pan Boundary did not sufficiently reflect the current Zoom state: after zooming in, Pan range did not expand enough to reach an edge Room/Hint, matching a screenshot of the camera unable to reach the House's far edge. Root cause: Pan boundary already recomputed live from the current Orthographic Size on every Pan request (no caching bug), but the "House bounds" fed into it only included **unlocked** rooms — a HintLocked room sitting just outside the unlocked footprint could never be reached no matter how far zoomed in. Fixed by feeding Pan boundary the unlocked House **plus the current Hint's bounds**; Zoom's own dynamic-max policy is unchanged.
- 2026-09-18 — Final Human Verification: user confirmed PASS on Camera Pan, Zoom In reaching House/Hint edges, Zoom/Hint Framing, and Room Generation/Hint/Door functionality. Task 006 is DONE. See `handoff.md` for the full final regression evidence (Room Generation core, HintLocked→UnlockedGenerated, variable Room sizes, Door generation/rotation, lock-icon centering, Zoom, min-5 floor, Hint full-bounds framing, Pan, Pan boundary, zero new Console errors).
