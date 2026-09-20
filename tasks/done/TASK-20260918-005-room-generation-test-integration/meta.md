# Task Metadata

- **ID:** TASK-20260918-005
- **Title:** Room Generation test integration
- **Status:** done
- **Owner:** User
- **Agent:** Claude
- **Domain:** Room Generation
- **Base:** dev
- **Branch:** feat/infinity-room-generation-core (existing isolated worktree branch)
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** done
- **Current Skill:** verify-work / integrate-unity-editor

## Allowed Scope

- Pure Room Generation additions under `Assets/01_Scripts/RoomGeneration/` and focused editor-free tests.
- Unity adapters under `Assets/01_Scripts/RoomGeneration.Unity/`.
- New copied test assets: `RoomGenerationTest.unity` and `Room_RoomGenTest.prefab`.
- This Task and `docs/domains/room-generation.md`.

## Do Not Modify

- `Assets/00_Scenes/Demo/InfiniteMode.unity` and `Assets/03_Prefabs/Rooms/Room.prefab`.
- Existing Power, UI, Product, Resident, art, material, audio, Packages, or ProjectSettings assets.
- Build Settings or production progression/session behavior.

## AI Setup Allowed

- Duplicate and isolate the test scene and Room prefab through the targeted Unity Editor.
- Add typed binder references and test-only technical roots to the copied prefab.
- Replace the copied scene's seed Room and wire its existing `RoomGenerator` mount.
- Add runtime-generated Room/Door instances and test-only camera framing.

## Exclusive Assets

- `Assets/00_Scenes/Demo/RoomGenerationTest.unity`
- `Assets/03_Prefabs/Rooms/Room_RoomGenTest.prefab`

## Human Decisions

- 2026-09-18 — Create a separate copied validation scene to avoid conflict with concurrent `InfiniteMode.unity` work; this supersedes the earlier no-separate-test-scene direction.
- 2026-09-18 — Room sizes vary; the implementer may choose authored variants or code-driven geometry without changing prefab visual design.
- 2026-09-18 — Candidate placement expands BFS-like from the center and balances all four cardinal directions.
- 2026-09-18 — Door width comes from the authored Room prefab; position/angle may be dynamic, but visual design is immutable.
- 2026-09-18 — Runtime rotations are Z-only and restricted to exactly 0, 90, or 180 degrees.

## Open Decisions

- Exact hatching presentation is blocked: the authored Room prefab has no distinct hatching anchor or approved hatching art. Baseline validation uses existing dashed walls plus lock icon and records `ART_DESIGN_REQUIRED`.
- Production no-successor/progression behavior remains outside this test-integration scope.

## Verification State

- Automated logic, compile, copied-asset wiring, and repeated runtime integration checks completed.
- User performed the manual PlayMode check and reported PASS on hint promotion, following-hint generation, variable-size expansion, room rotation, Door connection/design/rotation, and zero runtime errors.
- User reported one defect: the locked hint's lock icon was not positioned at the Room's exact visual center. Fixed in `RoomGenerationRoomBinder.CenterLockIcon()`, which derives the icon's local position from the same authored-bounds-plus-current-placement math already used for the floor collider offset (not the transform pivot), so it stays centered under Room rotation-zero and every variable footprint.
- Re-verified in Play Mode after the fix: 21 total checked hint instances (initial + 6 promotions, twice, across the four footprint variants 8x6/12x6/8x9/12x9) all show zero position delta between the lock icon and the room's true bounds center. Re-ran the full 8-promotion regression script with identical results to the prior evidence (9 unlocked rooms, 8 doors, rotations 0:3/90:4/180:1) — no regression. Console showed only the previously documented inherited/expected warnings; zero errors.
- Final status: `DONE`.
