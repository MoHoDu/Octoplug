# Todo

- [x] Confirm metadata, scope, exclusions, Human Decisions, and Open Decisions.
- [x] Confirm Exclusive Assets (shared copied test scene from Task 005) and isolated worktree.
- [x] Add pure `Octoplug.CameraFraming` zoom-range/dynamic-max/input-mapping/hint-framing logic.
- [x] Add and pass focused editor-free tests (`Tests/CameraFraming.Core.Tests`).
- [x] Add the minimal, additive `RoomGenerationTestController.HintRoomCreated` event.
- [x] Add Unity adapter code (`CameraZoomInputReader`, `HouseCameraZoomController`, `RoomGenerationCameraFramingBridge`) and compile cleanly.
- [x] Wire a `CinemachineBrain`/`CinemachineCamera`/new components into the copied test scene through the targeted Unity Editor; verify serialized references.
- [x] Run Play Mode runtime verification and capture evidence.
- [x] Run fast/full harness checks; fix the `git diff --check` whitespace false-positive on Unity YAML.
- [x] Update Domain Map (`camera-framing.md`) and `docs/domains/INDEX.md`.

## Verification

- [x] Logic Test — final Camera Framing suite 40 passed, 0 failed, 0 skipped; includes prior Zoom/Hint tests plus Pan math/state coverage; strict build 0 warnings/errors.
- [x] Runtime Integration — prior Zoom/Hint checks plus actual Input System mouse Pan entry, latched Pan/gameplay ownership, dynamic boundary clamping, expanded House bounds, and panned-position-preserving Hint framing.
- [x] Prior Human Play Check — user confirmed all Zoom and Hint framing checks PASS.
- [ ] Current Human Play Check — physical Mouse/Touch Pan feel, interaction exclusion, and post-Zoom/Hint boundary feel require the three checks in `handoff.md`.

## UX Tuning Round (2026-09-18)

- [x] Raise `scrollSensitivity`/`pinchSensitivity` defaults (0.01 → 0.05) in both the script and the live scene component; no new fields added since Mouse Wheel and Touchpad scroll share one Input System control.
- [x] Verify live Inspector-value change immediately changes zoom delta (10x sensitivity → 10x delta), via a queued Input System scroll event in Play Mode.
- [x] Add `CameraDesignReference` (1920x1080 / 16:9) as a documented, non-computational constant.
- [x] Add pure tests pinned at the 16:9 design aspect for both wide- and tall-bounds House/Hint framing.
- [x] Verify in Play Mode (forced `Camera.aspect = 16/9`) that a wide hint room is framed without left/right clipping, and that a non-16:9 aspect remains safe.
- [x] Re-run full pure test suites, strict builds, and `scripts/verify-fast.ps1`.
- [x] Diagnose partial Hint Room visibility: actual bounds were passed and min/max edges were calculated, but the result was capped by the unlocked-House dynamic max, which excluded the new hint.
- [x] Remove the upper clamp from pure hint framing and widen the controller's effective zoom range when full Hint Room bounds require more than the unlocked-House max.
- [x] Add center-visible/right-edge-outside and center-visible/top-edge-outside regression tests, plus variable-size/off-axis and all-edge containment coverage.
- [x] Verify actual initial + eight generated Hint Room bounds fit independently inside the runtime viewport after smoothing.
- [x] User confirmed all Zoom and Hint full-bounds framing checks PASS.

## Camera Pan Round (2026-09-18)

- [x] Add pure pointer-delta mapping, dynamic House-bound Pan ranges, collapsed-axis stability, and latched gesture ownership.
- [x] Add editor-free Pan math/state tests.
- [x] Add separate mouse/single-touch Pan input, explicit interaction resolver, and Cinemachine Pan controller without changing Zoom ownership.
- [x] Extend the existing Room Generation camera bridge to pass actual unlocked-House bounds to Pan.
- [x] Compile and wire only `RoomGenerationTest.unity` through the targeted Unity Editor; inspect all serialized Pan references.
- [x] Verify actual Input System mouse Pan entry, Pan/gameplay ownership persistence, dynamic House bounds, boundary clamps, and Hint-framing position preservation in Play Mode. Physical Touch/Pinch and final interaction feel remain Human Play checks.
- [x] Run Camera/Room Generation suites and Harness checks; audit Console and scope. Camera 40/40, strict build clean, Room Generation 66/66, fast harness PASS, Unity `NO_PROJECT_TESTS`; full harness FAIL on its generic Unity-content allowlist and is documented in `handoff.md`.
- [x] Human Play Check (first round) — user reported Pan produced zero Camera movement in real empty-world Mouse Drag, Zoom sensitivity still felt slow, and Zoom sensitivity fields were not clearly discoverable/separable per device.

## Camera Input / Tuning Fix Round (2026-09-18)

- [x] Trace the real Pointer Down → Empty World → Pan Capture → Drag → Cinemachine chain end to end via live Editor `eval`/`console` probes (not reflection-forced calls) instead of assuming the pure Pan math was the problem.
- [x] Directly verify `CameraPanInteractionResolver.IsReserved` returns `false` for a genuinely empty point far outside the House at runtime — confirmed the resolver/world-hit logic itself was already correct.
- [x] Root-cause the real "Pan does nothing" bug: Unity's Editor Play Mode default (`InputSystem.settings.editorInputBehaviorInPlayMode = PointersAndKeyboardsRespectGameViewFocus`) gates pointer BUTTON state behind Game View window OS focus, while Windows delivers mouse-wheel Scroll to the hovered window regardless of focus — exactly explaining "Scroll works, Drag never works at all" in the same session.
- [x] Fix: `CameraPanInputReader.Awake()` sets `InputSystem.settings.editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView`, guarded by `#if UNITY_EDITOR`, in memory only for the current Play session. No `ProjectSettings` asset is written; no effect on a build.
- [x] Remove all temporary `[PanDiag]` `Debug.Log` diagnostics added during tracing.
- [x] Rename `CameraZoomInputReader.scrollSensitivity`→`desktopScrollZoomSensitivity` and `pinchSensitivity`→`pinchZoomSensitivity` (via `[FormerlySerializedAs]`, preserving the scene's existing serialized values) with Inspector Header/Tooltip stating explicitly that Mouse Wheel + Touchpad share one field because the Input System cannot reliably tell them apart, while Pinch is independent.
- [x] Rename `HouseCameraZoomController.zoomSmoothTime`→`zoomSmoothing` (via `[FormerlySerializedAs]`) and add `[Zoom]`/`[Pan]`/`[Hint Framing]` Inspector Header/Tooltip tags across the Zoom and Pan components so every user-tunable value is easy to find without merging the components' separate runtime responsibilities.
- [x] Raise default Zoom sensitivity 0.05 → 0.2 (4x) for both `desktopScrollZoomSensitivity` and `pinchZoomSensitivity`, in code and in the live scene component; verified via `ZoomInputMapper.MapScrollDelta` that the per-unit delta scales proportionally; the `minOrthographicSize = 5` floor is unchanged and still structurally enforced by `ZoomRange.Clamp`.
- [x] Kept Pan sensitivity as one shared `panSensitivity` field (mouse and single-touch) since both already convert identical on-screen pixel distance through the same current Orthographic Size; documented this decision in the Tooltip and Task Human Decisions rather than force-splitting it.
- [x] Recompiled cleanly; re-ran Camera Framing (40/40) and Room Generation (66/66) pure suites; re-verified Hint framing position-preservation (`before=(1.00,-0.50) after=(1.00,-0.50)`) and the raised-sensitivity delta scaling live in Play Mode; confirmed a fresh Play Mode restart has zero Console errors (only the same pre-existing missing-script/hatching warnings); re-ran `scripts/verify-fast.ps1` (PASS) and re-audited Git scope (no stray files).
- [x] Human Play Check (second round) — user confirmed Pan input, Zoom sensitivity, Zoom Inspector clarity, and Hint full-bounds framing all PASS, but reported Pan Boundary did not expand enough after Zoom In to reach an edge Room/Hint.

## Pan Boundary Fix Round (2026-09-18)

- [x] Re-audited `PanBoundaryCalculator`/`HouseCameraPanController.ApplyPanDelta`: confirmed the boundary is already recomputed from the CURRENT Orthographic Size, current aspect, and `panBoundaryMargin` on every single Pan request — not cached, not requiring a Room event to refresh after a pure Zoom change. No bug found here.
- [x] Root-caused the real defect: the "House bounds" fed into Pan boundary (`RoomGenerationCameraFramingBridge.OnHintRoomCreated` → `cameraPan.UpdateHouseBounds`) only combined **unlocked** Room bounds. A HintLocked room sitting just outside the unlocked footprint could never be reached by Pan no matter how far Zoomed In, since the clamp range was capped at the unlocked house's edge + margin.
- [x] Fixed by computing a separate "visible house bounds" = unlocked Room bounds **plus the current Hint's bounds** for Pan boundary only; Zoom's own dynamic-max-zoom-out policy (`RecomputeDynamicMaxOrthographicSize`) is unchanged and still unlocked-only, per the existing established Hint-framing policy.
- [x] Added 6 new pure tests to `PanBoundaryCalculatorTests`: small house fully visible at current zoom (collapses), zooming in on the same house with no new Room widens the range immediately, zooming back out narrows/collapses it again, a large house's four edges are each fully reachable (not just their centers) at a given zoom, including the current Hint's bounds lets Pan reach the Hint's far edge (with an explicit before/after comparison against unlocked-only bounds), and Hint framing's zero-delta case is a strict no-op. Camera Framing suite is now 46/46; strict build clean.
- [x] Re-ran `RoomGeneration.Core.Tests` (66/66, no regression) and `scripts/verify-fast.ps1` (PASS).
- [x] Verified live in Play Mode with the actual wired scene (real `RoomGenerationTestController`/`RoomGenerationCameraFramingBridge`/`HouseCameraPanController`, not a synthetic reproduction): with the real unlocked+Hint bounds already combined by the fixed bridge at scene startup, zooming the actual Cinemachine Lens out to a size where the visible house already fits collapsed the Pan range (camera stayed at its position); zooming in to a smaller size — with no Room event in between — immediately widened the range and let `ApplyPanDelta` reach exactly the Hint room's far edge plus margin (`14.0`, matching the formula by hand). Re-verified Hint framing still preserves camera position exactly and the Orthographic Size floor still clamps to `5` internally (a raw `Lens.OrthographicSize` read moments later just reflects unsmoothed transient interpolation, not a real floor violation). Confirmed zero Console errors on this Play session.
- [x] Updated `HouseCameraPanController`'s Pan Boundary Tooltip and class remarks, and `docs/domains/camera-framing.md`, to describe the boundary as always zoom-live and now covering the unlocked House plus the current Hint.
- [x] Human Play Check (third round) — user confirmed PASS: Camera Pan normal, Zoom In reaches House/Hint edges, Zoom/Hint Framing normal, Room Generation/Hint/Door functionality normal.

## Task Closure (2026-09-18)

- [x] Final regression re-run: Room Generation Core (66/66), Camera Framing Core (46/46), both strict builds clean.
- [x] Final live Play Mode verification: 6 promotions confirm HintLocked → UnlockedGenerated (unlocked count 2→7), variable Room sizes (8x6/12x6/8x9/12x9), at least one Door per Room with correct Horizontal/Vertical orientation variety, lock-icon world position exactly matches its Room's authored bounds center, Hint full-bounds framing preserves Camera position, Orthographic Size floor still clamps to 5 internally, Camera Pan boundary reaches both extremes of the expanded House+Hint.
- [x] Confirmed zero new Console errors; all 22 observed warnings are the same two known pre-existing categories (missing-script on copied test assets; locked-room hatching root not assigned), repeated once per Room as expected.
- [x] Confirmed scene left clean/non-dirty after the verification Play session (no unintended save).
- [x] `scripts/verify-fast.ps1` PASS. Confirmed zero diff on protected assets (`InfiniteMode.unity`, `Room.prefab`, `ProjectSettings/**`) and zero diff on Power/Cable/Connection code or Product UI outside this Task's Camera Framing scope.
- [x] Task moved from `tasks/active/` to `tasks/done/`; `meta.md` Status set to `done`.
