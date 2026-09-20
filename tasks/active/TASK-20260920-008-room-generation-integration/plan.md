# Plan

## Current Phase: Merge and Compatibility

1. Verify task context, clean TASK-008 state, current remote tips, and merge-base.
2. Classify changed files, direct overlap, textual conflicts, package differences, task-folder collisions, and semantic overlap without mutating the working tree.
3. Preserve current production versions of forbidden assets, current policies/task state, and non-feature workspace settings while merging the committed Room Generation branch.
4. Keep the imported pure Room Generation and Camera Framing cores, tests, isolated validation assets, and required Cinemachine package addition.
5. Integrate Camera Pan into the authoritative production pointer ownership model at code level; do not wire the production scene.
6. Confirm Room Generation remains a planning/model layer and does not become a second gameplay grid authority.
7. Run core builds/tests, Unity compile and official test suites, task verifier, Console delta, `verify-fast`, and `git diff --check` where supported.
8. Update current Task snapshot and production-integration targets. If successful, commit and push TASK-008 only.

## Compatibility Rules

- `PointerInteractionResolver` owns pointer priority/capture for UI, Plug, Product, Socket, PowerStrip Head, and Empty-world Pan.
- `CableRoutingGrid`, `GridPathfinder`, and `CableRoutingGridService` remain the production spatial authority.
- `Wall_Outlet.prefab + SocketCount` and `Multitap.prefab + ActiveSocketCount` remain the only future production generation targets.
- Keep Cinemachine 3.1.7; do not downgrade Input System, Unity Pipeline, URP, or other current packages.
- Preserve exact Hint promotion/following Hint, room size/rotation/Door planning/lock-centering, and camera zoom/framing/Pan behavior.

## Deferred Phase Targets

Production `Room.prefab`, Room Generation Controller, `RoomPlan → CableRoutingGrid`, dynamic Doors, unified Wall Outlet placement, grid rebuild, pointer/Pan scene binding, Cinemachine Brain/Camera, framing bridge, Hint visuals, debug promotion trigger, and end-to-end `InfiniteMode` Human Verification.

Archived superseded plan: `history/2026-09-20-initial-production-integration-plan.md`.
