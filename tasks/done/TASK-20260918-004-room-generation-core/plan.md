# Plan

## Context

Build a deterministic Room Generation calculation core so a later integration task only needs to map `RoomPlan` data into `Room.prefab`. Current Scene/Prefab evidence is read-only and supplies no approved room-size or placement-priority rule.

## Approach

1. Establish an engine-independent Room Generation assembly and immutable geometry/value types.
2. Calculate overlap, four-directional adjacency, and maximal pairwise shared-wall spans with exact normalized coordinates.
3. Derive safe door opportunities from caller-supplied width/margin, then validate an injected placement policy's proposals.
4. Enforce one door per complete room wall, corner/intersection clearance, correct wall orientation, and at least one door per accepted candidate.
5. Select the first valid item from a caller-ordered candidate list without inventing randomness or weights.
6. Model HintLocked, unlock, exact stored-plan promotion, and separately stored following hints.
7. Add editor-free NUnit tests using explicit fixture-only dimensions and values.
8. Verify, update the Room Generation Domain Map, and write the main-worktree integration handoff.

## Files

- Expected additions: `Assets/01_Scripts/RoomGeneration/**`, `Tests/RoomGeneration.Core.Tests/**`.
- Expected documentation changes: this Task and `docs/domains/room-generation.md`.
- Read-only references: `Assets/03_Prefabs/Rooms/Room.prefab`, `Assets/01_Scripts/Power/RoomArea.cs`, `docs/architecture.md`.

## Risks

- Exact float geometry requires the later Unity adapter to normalize/snaps coordinates; no hidden epsilon is introduced here.
- The production door-position policy is intentionally unresolved and injected.
- The runtime assembly boundary is new but scoped only to new Room Generation code; existing scripts are not migrated.
- Actual Door prefab Euler rotation cannot be inferred safely and remains an integration mapping.

## Verification

- `dotnet test Tests/RoomGeneration.Core.Tests/RoomGeneration.Core.Tests.csproj`
- `pwsh -File scripts/verify-fast.ps1`
- `pwsh -File scripts/verify-harness.ps1`
- `git diff --check`
- Full diff/status and forbidden-path audit.
