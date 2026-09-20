# Todo

- [x] Confirm metadata, scope, exclusions, Human Decisions, and Open Decisions.
- [x] Confirm Exclusive Assets and isolated worktree.
- [x] Record protected original hashes.
- [x] Add pure lattice/frontier, Door policy, and Door transform logic.
- [x] Add and pass focused editor-free tests.
- [x] Add Unity adapter/controller code and compile cleanly.
- [x] Duplicate scene/prefab through targeted Unity Editor and wire only copies.
- [x] Run fast/full harness and direct Unity compile checks.
- [x] Run automated PlayMode/runtime inspection and capture evidence.
- [x] Recheck protected hashes and review final scope.
- [x] Update Domain Map and merge-transfer handoff.

## Verification

- [x] Logic Test — 66 passed, 0 failed, 0 skipped; strict build 0 warnings/errors.
- [x] Runtime Integration — repeated exact-plan promotion, variable footprints, balanced expansion, Door count/width/rotations, unit root transforms, and zero runtime errors verified.
- [x] Actual Space Input Entry — Human PlayMode check confirmed repeated Space promotion and separate following hints.
- [x] Human Play Check — User reported PASS on 9 of 10 checks; 1 defect (lock icon off-center).
- [x] Lock icon centering fix — `RoomGenerationRoomBinder.CenterLockIcon()` derives the icon's local position from authored bounds + current placement size (not the transform pivot), applied every `ApplyPlacement`.
- [x] Re-verification after fix — 21 checked hint instances across all four footprint variants show exact zero centering delta in Play Mode; full 8-promotion regression re-run matches prior evidence exactly; console has only previously documented expected/inherited warnings, 0 errors.
