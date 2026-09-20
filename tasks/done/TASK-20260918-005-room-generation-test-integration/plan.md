# Plan

## Context

Integrate the existing pure Room Generation core in an isolated validation scene without modifying the `InfiniteMode.unity` and `Room.prefab` assets owned by active Task 001. Use copied assets as an additive reference implementation and document how a later merger recreates the verified component/reference delta on current production assets.

## Approach

1. Record protected-asset hashes and keep source scene/prefab, Power assets, and ProjectSettings read-only.
2. Add pure integer-lattice footprint/frontier generation, balanced origin BFS ordering, widest-safe-interval Door selection, and pure Door transform mapping.
3. Add editor-free tests for variable sizes, deterministic balance, exact geometry, Door policy, and allowed rotations.
4. Create Unity-facing typed binders/controllers outside the no-engine core assembly.
5. Through the exact worktree Editor, copy the source scene/prefab, wire only the copies, and save only owned assets.
6. Verify compile/console/runtime behavior, then leave at `HUMAN_VERIFY_REQUIRED` with three PlayMode checks.
7. Update the Domain Map and handoff with protected-file proof and production transfer steps.

## Files

- Expected new/updated code: `Assets/01_Scripts/RoomGeneration/`, `Assets/01_Scripts/RoomGeneration.Unity/`, `Tests/RoomGeneration.Core.Tests/`.
- Exclusive copied assets: `Assets/00_Scenes/Demo/RoomGenerationTest.unity`, `Assets/03_Prefabs/Rooms/Room_RoomGenTest.prefab`.
- Documentation: this Task and `docs/domains/room-generation.md`.
- Read-only references: source `InfiniteMode.unity`, source `Room.prefab`, `Assets/01_Scripts/Power/Routing/GridPathfinder.cs`.

## Technical Decisions

- Code-driven wall/collider resizing on a copied prefab; never scale the Room transform or alter visual assets.
- Exact integer lattice based on measured authored geometry; 0.01 tolerance only validates the seed transform and never silently snaps core geometry.
- Safety margin derives from half wall thickness; Door width derives from authored Door geometry.
- One Door at the midpoint of the widest safe interval; one visible Door per shared opening.
- Z mapping uses only 0/90/180; a Top candidate opening is rendered from the connected room's Bottom wall to avoid 270.
- Missing hatching art remains a reported design dependency; no substitute art is fabricated.

## Risks

- Serialized asset commands may require multi-step Editor tooling; inspect and save only copied assets.
- Door gaps must be rebuilt on both connected rooms without accumulating runtime clones.
- Concurrent human-workspace Editor requires exact `--project-path` on every command.
- Scene/Inspector wiring and actual Space input require human PlayMode verification before Done.

## Verification

- Editor-free NUnit tests and warnings-as-errors build.
- Fast and full harness checks.
- Targeted Unity compile, Console audit, hierarchy/reference inspection, and Scene/Game captures.
- Actual Space-key PlayMode progression in the copied scene; automated status stops at `AUTO_VERIFIED`.
- Recompare protected hashes and confirm Build Settings/source scene/source prefab are unchanged.
