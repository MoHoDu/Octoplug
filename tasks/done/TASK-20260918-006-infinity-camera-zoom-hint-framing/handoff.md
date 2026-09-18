# Handoff

## Status

**DONE.** The user's final Human Verification round confirmed PASS on Camera Pan, Zoom In reaching
House/Hint edges, Zoom/Hint Framing, and Room Generation/Hint/Door functionality. Task 006's
Camera Framing scope (Zoom, Hint full-bounds framing, Camera Pan, and the Zoom-dependent
Hint-inclusive Pan boundary) is complete and verified. See **Final Regression** and **Final Human
Verification** below for the closing evidence.

## Root Cause

`HouseCameraPanController.ApplyPanDelta` already recomputed the Pan boundary from the **current**
`cinemachineCamera.Lens.OrthographicSize`, the current `outputCamera.aspect`, and
`panBoundaryMargin` on every single Pan request — this was confirmed correct, live, in the actual
scene, and is not cached or dependent on a Room Generation event to "refresh." Zooming in already
reduced the effective half-width/half-height used in that formula on the very next Pan request.

The real defect was one level up: the "House bounds" fed into that formula
(`RoomGenerationCameraFramingBridge.OnHintRoomCreated` → `cameraPan.UpdateHouseBounds`) only
combined **unlocked** Room bounds. A `HintLocked` room sitting just outside the unlocked footprint
was invisible to Pan's boundary entirely, so no matter how far the user zoomed in, the clamp range
was still capped at the unlocked house's edge + margin — it could widen, but never far enough to
reach a Room that Pan's boundary didn't know existed. That matches the reported screenshot exactly:
Zoom In did make *some* Pan range appear, but it stopped short of the actual edge Hint Room.

## New Boundary Formula

- **House Bounds:** now the union of the current unlocked Room bounds **and** the current Hint
  (`HintLocked`) Room's bounds — the "visible house" the user is meant to be able to reach, not
  only the confirmed/unlocked footprint. Computed once per `HintRoomCreated` event (initial setup
  and every promotion), exactly like before; no Room Generation data structure changed.
- **Viewport Extent:** unchanged — `halfHeight = orthographicSize`,
  `halfWidth = orthographicSize * outputCamera.aspect`, read live at the moment of every Pan
  request, never a Design Reference (1920x1080/16:9) constant.
- **Margin:** unchanged — `panBoundaryMargin`, Inspector-tunable, applied outside the combined
  bounds on every axis.
- **Hint 포함 여부:** now included. Zoom's own dynamic-max-zoom-out policy
  (`HouseCameraZoomController.RecomputeDynamicMaxOrthographicSize`) is deliberately **not**
  changed and stays unlocked-only, per the existing established policy that a brand-new Hint
  widens the effective *zoom* range itself, separately, only when its full bounds require it.

## Zoom Dependency

- **Zoom In:** on the very next Pan request after Zoom In (Mouse Wheel / Touchpad / Pinch / Hint
  auto-framing — any of them, since all of them only ever change
  `cinemachineCamera.Lens.OrthographicSize`), the smaller half-width/half-height immediately widens
  the computed range. No Room event is needed for this to take effect.
- **Zoom Out:** symmetrically, the next Pan request after Zoom Out immediately narrows the range,
  collapsing an axis back to "stay centered" once the visible house (plus margin) fits inside the
  viewport again.
- **Room 변경:** only changes the cached House-bounds input to the same live formula (a new/promoted
  Room, or the Hint moving), not the zoom-dependent viewport half-extent math itself.

## Automated Verification

- `CameraFraming.Core.Tests`: **46 passed**, 0 failed, 0 skipped (40 prior + 6 new); strict build 0
  warnings/errors. New tests cover exactly the requested cases:
  - Small House fully visible at current Zoom → both axes collapse.
  - Zooming In on the same House, no new Room → range opens immediately.
  - Zooming back Out on the same House → range narrows/collapses again.
  - Large House at a given Zoom → all four edge Rooms are fully reachable (verified against each
    edge's exact coordinate, not just "some" widening).
  - Hint Room outside the unlocked footprint → boundary computed from unlocked-only bounds cannot
    reach it; boundary computed from unlocked+Hint bounds reaches its far edge plus margin exactly.
  - Hint framing's zero Pan delta is a strict no-op (Boundary math itself never moves the Camera).
- `RoomGeneration.Core.Tests`: 66 passed, 0 failed, 0 skipped (no regression).
- Unity recompiled cleanly (`compilationFailed=false`).
- Live runtime evidence (Editor `eval` against the actual running, already-wired scene — real
  `RoomGenerationTestController` state, real fixed `RoomGenerationCameraFramingBridge`, real
  `HouseCameraPanController.ApplyPanDelta`, not a synthetic reproduction):
  - Actual unlocked Room bounds `(-4,-3)-(4,3)` and actual current Hint bounds `(4,-3)-(12,3)`
    combine, exactly as the fixed bridge now computes, to visible-house bounds `(-4,-3)-(12,3)`.
  - At Orthographic Size `8` (house+margin already fits the viewport): `ApplyPanDelta` with a huge
    requested delta left the Camera at `(0.00, 0.00)` — unchanged, both axes collapsed, matching
    "House 전체가 보이면 Pan이 거의 안 되는 것은 정상."
  - At Orthographic Size `3` (Zoom In, same House, no Room event in between): the same huge
    requested delta moved the Camera to `(8.667, 0.000)` — `8.667 + halfWidth(5.333) = 14.0 =
    visibleHouse.MaxX(12) + margin(2)`, i.e. the Hint Room's full right edge plus margin is exactly
    reachable, matching the pure-test formula by hand.
  - Hint framing re-verified to still preserve the Camera position exactly:
    `(1.00, -0.50)` before and after `RequestHintFraming`.
  - The Orthographic Size floor still clamps internally to `5` (`targetOrthographicSize` read via
    the controller's own field after 50 hard zoom-in deltas); a `Lens.OrthographicSize` read
    moments later reflects unsmoothed transient interpolation only, not a real floor violation.
  - A fresh Play session reported zero Console errors; only the same pre-existing missing-script
    and locked-hatching warnings remain.
- `scripts/verify-fast.ps1`: PASS. Git scope re-audited: only the expected Camera Framing
  core/Unity files, `Tests/`, the exclusive copied scene, and Task/domain docs changed; no stray
  files. `scripts/verify-harness.ps1` still fails only on its pre-existing generic Unity-content
  allowlist (documented in earlier rounds; unchanged and not silently worked around).
- Unity project test discovery still reports zero tests: `NO_PROJECT_TESTS`.

## Final Human Verification (PASS)

The user confirmed PASS on all of:

- Camera Pan 정상.
- Zoom In 상태에서 House/Hint 끝까지 이동 가능.
- Zoom/Hint Framing 정상.
- Room Generation / Hint / Door 기능 정상.

## Final Regression

Re-run at Task closure, in addition to everything already recorded above:

- `RoomGeneration.Core.Tests`: 66 passed, 0 failed, 0 skipped.
- `CameraFraming.Core.Tests`: 46 passed, 0 failed, 0 skipped. Both strict builds clean.
- Unity: `up_to_date` (no scripts needed recompilation), confirming the wired scene and scripts are
  in a consistent, already-verified state.
- Live Play Mode, real `RoomGenerationTestController` API (`PromoteStoredPlanAndPlanFollowing`,
  not a synthetic reproduction), 6 sequential promotions:
  - **HintLocked → UnlockedGenerated:** each promoted plan's own `State` was `HintLocked` at the
    moment it was promoted, and `State.UnlockedLayout.Rooms.Count` grew `2 → 3 → 4 → 5 → 6 → 7`
    across the 6 promotions, confirming each promoted Room actually joins the unlocked layout.
  - **Variable Room sizes:** promoted Rooms measured `8x6`, `12x6`, `8x9`, `12x9`, `8x6`, `12x6` —
    not a fixed size.
  - **Door generation/rotation:** every promoted Room carried at least one `DoorPlan`, with a mix
    of `HorizontalWall` and `VerticalWall` orientations across the run (i.e. door placement varies
    correctly with which wall is shared, not a single hardcoded rotation).
  - **Lock-icon centering:** the currently `HintLocked` Room's lock-icon world position read
    `(8.00, -7.50)`, exactly matching that Room's own authored bounds center
    `((MinX+MaxX)/2, (MinY+MaxY)/2) = (8.0, -7.5)` computed independently from its `RoomBounds2D`.
  - **Camera Zoom / min-5:** after 80 hard manual zoom-in deltas, the controller's own
    `targetOrthographicSize` field clamped to exactly `5.000`.
  - **Hint full-bounds framing:** `RequestHintFraming` on the current Hint left the Camera position
    unchanged (`(0.00, 0.00)` before and after).
  - **Camera Pan / Pan boundary:** with the expanded 7-Room house plus its new Hint, `ApplyPanDelta`
    reached both extremes correctly (`panMin=(-12.67,-11.00)`, `panMax=(8.67,14.00)`), consistent
    with the fixed unlocked+Hint boundary formula.
  - **Console:** zero errors throughout; all 22 observed warnings were the same two known
    pre-existing categories (missing-script on copied test assets; "locked-room hatching is
    requested but no authored hatching root is assigned," once per Room as expected) — not new
    defects.
  - The scene was left clean/non-dirty after this verification Play session (no unintended save).
- `scripts/verify-fast.ps1`: PASS.
- Protected-scope audit: zero Git diff on `Assets/00_Scenes/Demo/InfiniteMode.unity`,
  `Assets/03_Prefabs/Rooms/Room.prefab`, and `ProjectSettings/**`; zero diff on any Power/Cable/
  Connection script or Product UI outside this Task's Camera Framing scope. The only non-Camera-
  Framing code change in the whole Task remains the single additive `HintRoomCreated` event on
  `RoomGenerationTestController.cs`, re-confirmed unchanged.

## Task Status

**DONE.** Committed and pushed under the Harness Git Workflow (see the completion report for
branch/commit/push details). The whole Task folder has moved from `tasks/active/` to
`tasks/done/`. This Task's Camera Framing feature work is complete; no further automatic feature
work follows from this Task.
