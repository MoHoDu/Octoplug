# Task Metadata

- **ID:** TASK-20260920-008
- **Title:** Room Generation Core Integration
- **Status:** proposed
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** Room Generation / Camera Framing / Connection Grid
- **Base:** dev
- **Branch:** task/TASK-20260920-008-room-generation-integration
- **Started:** 2026-09-20
- **Updated:** 2026-09-20
- **Current Stage:** planning
- **Current Skill:** plan-task

## Context

`TASK-20260918-007` and `feat/infinity-power-connection` are merged into `dev`. The next task will integrate the committed `feat/infinity-room-generation-core` work against that current production baseline. The room-generation branch also has an existing dirty worktree; those uncommitted changes are preserved and are not part of the initial merge input.

## Allowed Scope

- Preflight and merge the committed `feat/infinity-room-generation-core` history into this task branch.
- Resolve conflicts against the current `dev` production Power/Grid/UI implementation without dropping either branch's intended behavior.
- Integrate the pure `Assets/01_Scripts/RoomGeneration/` and `Assets/01_Scripts/CameraFraming/` cores and their tests.
- Recreate the verified Room Generation binder/controller and Camera Framing patterns against the current production `Room.prefab` and `InfiniteMode.unity`; do not copy validation-scene YAML into production.
- Connect generated Room/Wall/Door topology to the existing `CableRoutingGrid`/`GridPathfinder` and `CableRoutingGridService`; do not introduce a second production grid.
- Update Room Generation, Camera Framing, and affected Connection/Grid domain documentation and focused verification.

## Do Not Modify

- Do not include or discard uncommitted changes from the existing `feat/infinity-room-generation-core` worktree or any other dirty worktree.
- Do not replace production assets with `RoomGenerationTest.unity` or `Room_RoomGenTest.prefab`; they remain validation/reference assets.
- Do not redesign PowerStrip, Wall Outlet, Plug/Socket, Cable routing, power validation, or authored UI behavior established by TASK-007.
- Do not implement Reward System, EXP/Level logic, resident spawning, persistence, production upgrade UI, or locked-room hatching art.
- Do not choose unresolved player-facing generation/balance/UX values without the Human Decision Gate.
- Do not stage, commit, push, or delete legacy branches/worktrees without explicit authorization.

## AI Setup Allowed

- Additive production C# adapters/components required to connect the established Room Generation and Camera Framing cores to current production systems.
- Focused automated test fixtures and temporary runtime verification objects that do not persist in production scenes.
- Unity Editor wiring only after Exclusive Assets are confirmed and through the exact task worktree Editor.

## Exclusive Assets

- Initial merge/preflight: committed files changed by `feat/infinity-room-generation-core`, including `.gitattributes`, package manifests, Room Generation/Camera Framing scripts and tests, isolated validation assets, domain maps, and historical task documents.
- Production integration requires explicit confirmation before mutation of:
  - `Assets/00_Scenes/Demo/InfiniteMode.unity`
  - `Assets/03_Prefabs/Rooms/Room.prefab`
  - existing `Assets/01_Scripts/Power/Grid/**` integration surfaces
  - `Packages/manifest.json` and `Packages/packages-lock.json`
- Dirty files in pre-existing worktrees are not owned by this Task.

## Human Decisions

- The committed Room Generation branch is the integration source; its uncommitted worktree changes remain preserved and excluded unless separately authorized.
- Room Generation must reuse the existing production Cable Routing Grid rather than create a parallel production grid.
- Production integration must re-wire verified patterns against current assets instead of copying the isolated test scene/prefab.

## Open Decisions

- Production Room bounds source/order and candidate ordering after inspecting current `InfiniteMode` structure.
- Production Door dimensions/policy ownership and coordinate tolerance.
- No-successor behavior and progression/session orchestration boundary.
- Exact conflict resolutions for shared `.vscode/settings.json`, package files, task IDs/documents, `docs/domains/INDEX.md`, and any overlapping production scene/prefab content.
- Whether preserved uncommitted changes in the Room Generation worktree should later be incorporated as a separate reviewed commit.

## Verification State

- Not started.
- Required baseline: branch/merge ancestry audit, focused pure test suites, Unity compile, project tests where available, runtime Room/Hint/Door/Grid/Camera regression, Console delta, `verify-fast`, `git diff --check`, and Human Play verification for player-facing behavior.
- Verification: HUMAN_VERIFY_REQUIRED
