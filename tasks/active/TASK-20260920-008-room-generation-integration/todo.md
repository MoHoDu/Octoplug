# Todo

## Merge / Compatibility Phase

- [x] Create TASK-008 from current `origin/dev`; preserve dirty worktrees.
- [x] Fetch refs and limit input to committed `origin/feat/infinity-room-generation-core`.
- [x] Re-run merge-tree/changed-file preflight from actual merge-base `ca3a87f`; no direct overlap or textual conflict.
- [x] Merge committed history and restore forbidden production assets plus workspace settings to TASK-008 versions.
- [x] Make Camera Pan use authoritative production pointer ownership; retain explicit blocker support and multitouch release.
- [x] Confirm no second gameplay grid or legacy Wall Outlet/Multitap selector enters production code.
- [x] Run Room Generation Core (66/66) and Camera Framing Core (46/46) tests.
- [ ] Run matching TASK-008 Unity compile and official Room Generation, Camera, and Production Power tests.
- [ ] Capture reliable Console baseline/delta and re-run `verify-task.ps1` / `verify-unity.ps1`.
- [x] Run `verify-fast.ps1`, task-context check, and `git diff --check` (PASS/WARN/no errors).
- [x] Review scope; production scene/prefab/UI/Game Flow targets remain unchanged.
- [x] Update handoff and compact current-phase docs.
- [ ] Commit and push TASK-008 only after Unity verification succeeds.

## Deferred Production Phase

- [ ] Adapt production `Room.prefab`; add controller and `RoomPlan → CableRoutingGrid` bridge.
- [ ] Apply dynamic Doors, unified Wall Outlet + SocketCount, and grid rebuild.
- [ ] Wire authoritative pointer/Pan, Cinemachine, camera framing, Hint visuals, and debug trigger.
- [ ] Run `InfiniteMode` end-to-end Human Verification.

Task remains active and not DONE until the deferred production phase completes.
