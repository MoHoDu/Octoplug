# Plan

## Context

Connect the three authored Demo scenes into a complete session flow without changing their visual design: Lobby → InfiniteMode → GameOver → Result → Lobby. Result values must survive scene destruction, and starting another game must reset all session-owned state.

## Approach

1. Audit exact scene paths, hierarchy/components, build settings, current GameFlow reset/GameOver APIs, Room-count authority, Demand resolution events, and any existing scene-flow, Survey, or Guide destination.
2. Design the smallest centralized scene-flow/session-result boundary that fits existing architecture and avoids PlayerPrefs.
3. Add demand-resolution counters that increment once per final SUCCESS/FAILURE resolution, then create a final Room/Solved/Failed snapshot before a one-shot Result load.
4. Bind existing Lobby buttons and Result text/button instances without changing hierarchy or visual properties; isolate Editor-only quit support.
5. Add the verified scenes to build settings if needed and run focused/full automated verification.
6. Keep status `HUMAN_VERIFY_REQUIRED` until the approved end-to-end PlayMode scenarios pass.

## Files

- Expected modifications: narrow GameFlow/ResidentDemand integration scripts, new scene-flow/result-state/binder scripts, focused tests, the three Demo scenes, and possibly `ProjectSettings/EditorBuildSettings.asset`.
- Read-only references: authored UI prefabs, Room/Reward/Power/Camera systems, domain maps, and the concept source only if repository evidence is insufficient.

## Risks

- Latest `dev` may not contain later unmerged Reward/Room runtime APIs; integrate only with committed authoritative APIs available in this worktree.
- Scene mutation requires the exact matching Unity Editor and must preserve all authored visual values.
- Survey/Guide remain blocked if no authoritative destination exists.

## Verification

- Result counters, exactly-once final resolution, Two-Need semantics, new-session reset, snapshot correctness, one-shot GameOver transition, Lobby start, Result return, and Result text binding tests.
- Exact Unity compile, full project tests, fast/harness/task-context/diff checks.
- Human verification of Start, Result, Return/new-session reset, and Build Exit scenarios.
