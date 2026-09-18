# Todo

- [x] Confirm metadata, scope, branch, exclusions, and workspace.
- [x] Record supplied Human Decisions and non-blocking Open Decisions.
- [x] Confirm Exclusive Assets are `None`.
- [x] Add the engine-independent Room Generation assembly boundary.
- [x] Implement immutable room geometry and shared-wall calculation.
- [x] Implement validated DoorPlan opportunities and occupied-wall rules.
- [x] Implement ordered candidate planning and diagnostics.
- [x] Implement HintLocked / UnlockedGenerated lifecycle and visual intent.
- [x] Add editor-free logic tests.
- [x] Run logic, fast, harness, and diff verification.
- [x] Audit forbidden paths and generated files.
- [x] Update Domain Map and final handoff.

## Verification

- [x] Logic Test — 38/38 passed; strict build 0 warnings / 0 errors
- [x] Fast Harness — passed with 3 pre-existing context-budget warnings
- [x] Full Harness — passed with the same 3 pre-existing warnings
- [x] Diff / Scope Audit — passed; no forbidden paths changed
- [x] Runtime Integration — N/A, pure computation only
- [x] Human Play Check — N/A, no player-facing integration
