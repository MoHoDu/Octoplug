# Plan

## Context

The copied Room Generation validation scene now needs Camera Pan in addition to its verified Cinemachine zoom and full Hint Room bounds framing. Pan is limited to mouse/single-touch drags that begin on empty world. Pointer ownership is decided once at Down and retained until release, while UI and explicit gameplay interactions take priority. Pinch remains the sole multitouch owner.

This extension remains isolated to the existing Task 006, pure Camera Framing code/tests, Unity camera adapters, and `Assets/00_Scenes/Demo/RoomGenerationTest.unity`. It must not change Room/Door generation, Hint visuals, zoom sensitivity, minimum Orthographic Size 5, Product/Power/Cable behavior, prefab design, sorting, package versions, or production assets.

## Approach

1. Add engine-independent Pan logic under `Octoplug.CameraFraming`:
   - map screen displacement to opposite orthographic world displacement using current size, aspect, viewport pixels, and `panSensitivity`;
   - derive Camera-center ranges from actual House bounds, current viewport extents, and a margin;
   - retain the current coordinate on collapsed axes where the viewport exceeds the bounded House;
   - model latched `CameraPan`, `GameplayBlocked`, and `MultiTouchSuppressed` gesture ownership.
2. Add focused pure tests for movement direction/scaling, every boundary edge, dynamic range expansion, collapsed-axis stability, ownership persistence, and multitouch suppression.
3. Add separate Unity adapters:
   - `CameraPanInteractionResolver` classifies UI and explicit gameplay interaction hits while ignoring broad non-interactive Room colliders;
   - `CameraPanInputReader` owns Mouse/single-touch gesture input and suppresses Pan for a complete multitouch sequence;
   - `HouseCameraPanController` moves only the `CinemachineCamera`, using current House bounds and viewport data.
4. Extend the existing `RoomGenerationCameraFramingBridge` only to forward the unlocked-House bounds to both Zoom and Pan before requesting unchanged Hint framing.
5. Wire the Pan components and references only into the copied validation scene through the matching live Unity Editor.
6. Verify pure logic, Room Generation regressions, Unity compile/Console, actual Input System mouse entry, pointer ownership, dynamic boundaries, and Hint-position preservation. Keep physical Touch/Pinch feel and interaction feel for Human Play verification.

## Files

- Camera core: `Assets/01_Scripts/CameraFraming/`.
- Unity adapters: `Assets/01_Scripts/CameraFraming.Unity/`.
- Pure tests: `Tests/CameraFraming.Core.Tests/`.
- Existing additive bridge event source: `Assets/01_Scripts/RoomGeneration.Unity/RoomGenerationTestController.cs`.
- Exclusive copied scene: `Assets/00_Scenes/Demo/RoomGenerationTest.unity`.
- Task and domain records only.

## Risks and Mitigations

- Existing interaction scripts do not share one pointer coordinator. Pan therefore classifies and latches ownership independently at Pointer Down rather than relying on Update order.
- Broad Room colliders must not make all world space non-pannable. The resolver blocks only explicit interaction semantics.
- Authored PowerStrip body art has no complete collider coverage. A renderer-bounds fallback under `PowerStrip`, excluding Cable renderers, reserves that visual interaction region without changing prefabs.
- Product prefabs reference `ProductClickInput`, but its source/meta are absent in this worktree. Product blocking uses the present `ApplianceSource` hierarchy; no replacement gameplay component is invented.
- Headless probes cannot establish physical touch/pinch feel or final interaction feel. Task status remains `HUMAN_VERIFY_REQUIRED` until the three manual checks pass.
- `scripts/verify-unity.ps1 -Compile` currently calls unavailable Pipeline command `refresh_and_wait_for_compile`; direct `recompile`/`recompile_status` evidence is used and the harness script is not silently changed.

## Verification

- Camera Framing pure suite and strict warning-as-error build.
- Room Generation pure regressions.
- Unity compile, serialized-reference inspection, clean Play Mode restart, and Console audit.
- Actual New Input System mouse state events through `CameraPanInputReader` for Pan and ownership behavior.
- Dynamic House-bound clamp and Hint-framing position-preservation runtime probes.
- `scripts/verify-fast.ps1`; faithfully record the full-harness Unity-content allowlist failure.
- Unity project test discovery; report zero discovered tests as `NO_PROJECT_TESTS`.
- Exactly three Human Play checks in `handoff.md`.
