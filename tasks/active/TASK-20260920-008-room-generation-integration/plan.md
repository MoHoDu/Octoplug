# Plan

## Context

The committed Room Generation and Camera Framing work was developed from an older `dev` baseline while TASK-007 substantially changed the production scene, Room/Grid integration surfaces, pointer behavior, packages, and task documentation. This task exists to merge those histories safely and then perform a fresh production integration against current assets.

## Approach

1. Preserve all dirty pre-existing worktrees and use only committed branch tips as merge inputs.
2. Audit branch ancestry, file overlap, package changes, task-ID collisions, and likely production Scene/Prefab conflicts.
3. Merge `feat/infinity-room-generation-core` into the task branch; resolve textual conflicts with both branches' contracts intact.
4. Run pure Room Generation and Camera Framing tests before Unity asset mutation.
5. Inspect current `InfiniteMode.unity`, `Room.prefab`, Cable Routing Grid, Door/Wall components, and established test-integration patterns through the matching task Editor.
6. Request/confirm any remaining player-facing decisions and Exclusive Assets before production Scene/Prefab edits.
7. Recreate only the approved binder/controller/camera adapters against current production assets and connect topology changes to the existing production grid.
8. Run focused compile/tests/runtime regressions, Console delta, harness checks, diff review, and Human Play verification.
9. Update affected domain maps and the Task handoff. Git completion remains separately authorized.

## Files

- Expected merge inputs: `Assets/01_Scripts/RoomGeneration/**`, `Assets/01_Scripts/RoomGeneration.Unity/**`, `Assets/01_Scripts/CameraFraming/**`, `Assets/01_Scripts/CameraFraming.Unity/**`, `Tests/{RoomGeneration,CameraFraming}.Core.Tests/**`, isolated validation scene/prefab, packages, domain maps, and historical task docs.
- Expected production integration: `Assets/00_Scenes/Demo/InfiniteMode.unity`, `Assets/03_Prefabs/Rooms/Room.prefab`, and minimal existing Grid integration surfaces after approval.
- Read-only references: `Assets/00_Scenes/Demo/RoomGenerationTest.unity`, `Assets/03_Prefabs/Rooms/Room_RoomGenTest.prefab`, completed Room Generation/Camera task handoffs, and TASK-007 handoff.

## Risks

- Production scene/prefab histories diverged and must not be resolved by wholesale choosing one branch.
- Both branches contain task folders with overlapping numeric IDs but different domains.
- Package and workspace settings may be incidental rather than feature requirements.
- The Room Generation branch's dirty worktree contains uncommitted font changes that must not leak into the merge.
- Generated topology must update the existing Power cable grid without invalidating placement, routes, connections, or authored objects.

## Verification

- Git ancestry/diff/conflict audit and `git diff --check`.
- `RoomGeneration.Core.Tests` and `CameraFraming.Core.Tests` pass with zero failures; strict builds clean.
- Matching Unity Editor compile and focused Unity tests where applicable.
- Runtime: exact hint promotion, following hint, variable room sizing, Door pairing/rotation, lock centering, existing-grid rebuild, cable/placement regression, camera zoom/pan/hint framing, and zero new Console errors.
- `scripts/verify-fast.ps1` and the applicable task/Unity verifier.
- Human Play verification for Room/Hint/Door and Camera behavior.
