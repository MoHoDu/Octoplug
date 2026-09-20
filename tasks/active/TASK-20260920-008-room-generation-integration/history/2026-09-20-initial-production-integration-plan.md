# Initial Production Integration Plan

Archived before the merge/compatibility phase because the user narrowed this phase to committed-history merge, code compatibility, and regression verification only.

## Original Plan

The committed Room Generation and Camera Framing work was developed from an older `dev` baseline while TASK-007 substantially changed the production scene, Room/Grid integration surfaces, pointer behavior, packages, and task documentation. This task exists to merge those histories safely and then perform a fresh production integration against current assets.

1. Preserve all dirty pre-existing worktrees and use only committed branch tips as merge inputs.
2. Audit branch ancestry, file overlap, package changes, task-ID collisions, and likely production Scene/Prefab conflicts.
3. Merge `feat/infinity-room-generation-core` into the task branch; resolve textual conflicts with both branches' contracts intact.
4. Run pure Room Generation and Camera Framing tests before Unity asset mutation.
5. Inspect current `InfiniteMode.unity`, `Room.prefab`, Cable Routing Grid, Door/Wall components, and established test-integration patterns through the matching task Editor.
6. Request/confirm any remaining player-facing decisions and Exclusive Assets before production Scene/Prefab edits.
7. Recreate only the approved binder/controller/camera adapters against current production assets and connect topology changes to the existing production grid.
8. Run focused compile/tests/runtime regressions, Console delta, harness checks, diff review, and Human Play verification.
9. Update affected domain maps and the Task handoff. Git completion remains separately authorized.

## Original Expected Production Integration Assets

- `Assets/00_Scenes/Demo/InfiniteMode.unity`
- `Assets/03_Prefabs/Rooms/Room.prefab`
- minimal existing Grid integration surfaces after approval

These are deferred and forbidden in the current merge/compatibility phase.
