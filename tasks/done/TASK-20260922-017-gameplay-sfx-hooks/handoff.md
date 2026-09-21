## Implemented
- Added Resources `GameplaySfxCatalog` referencing all four existing WAV files without modifying them.
- Added a persistent runtime-created 2D `AudioSource` and `PlayOneShot` service.
- Demand spawn plays after committed `StartDemand`; failed outcomes play `need_fail`.
- Plug connect/disconnect play only for actual pairing mutations; no-op, rejection, reconnect detach, repeated disconnect, and topology notifications stay silent.
- Added Audio Domain ownership documentation and focused test seam.

## Automated Verification
- Unity batch compilation: PASS, compiler errors 0.
- Audio catalog: 1/1 PASS.
- Plug SFX exactly-once behavior: 2/2 PASS.
- Resident Demand controller including spawn/no-candidate/refresh boundary: 9/9 PASS.
- `git diff --check`: PASS.
- `verify-fast.ps1`: functional checks PASS; pre-existing context warnings remain. TASK-017 metadata was compacted afterward.

## Human Verification
- 2026-09-22: PASS. User confirmed the current task verification succeeded.
