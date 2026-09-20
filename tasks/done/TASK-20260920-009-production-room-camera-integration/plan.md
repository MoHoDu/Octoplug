# Plan

## Phase A — Production Room / Grid

1. Audit current InfiniteMode hierarchy, Room prefab, production grid, pointer ownership, and test-scene reference patterns.
2. Wire the binder/controller pattern fresh against production assets; never copy test hierarchy or test-only objects.
3. Keep `CableRoutingGrid`, `GridPathfinder`, and `CableRoutingGridService` authoritative.
4. Ensure HintLocked is excluded from gameplay/content/grid until Promote.
5. On Promote: activate Room, apply Doors/content, rebuild grid, emit orchestration events, then create the next Hint.
6. Use only `Wall_Outlet.prefab` with runtime socket count; do not invent Product generation policy.
7. Add official Unity integration tests and run the Phase A gate before Camera wiring.

## Phase B — Production Camera

1. Wire existing Cinemachine 3.1.7 controllers/adapters to InfiniteMode's production Camera.
2. Use production `PointerInteractionResolver` as the only pointer priority/capture authority; Pan owns empty-world gestures only.
3. Preserve min zoom 5, input sensitivities, dynamic limits, full-Hint zoom-out-only framing, position preservation, and zoom-dependent Pan bounds.
4. Expose an externally requested Room Reveal operation and completion event without implementing GameFlow.
5. Add official integration tests and run Camera/Pointer regressions.

## Phase C — InfiniteMode Verification

1. Verify Debug flow: Hint → framing → Promote → Room/Door/content → grid rebuild → Door-only cable route → next Hint → bounds refresh.
2. Run official Room/Camera/Power/Multitap/Outlet tests, compile, Console delta, verify-task, verify-unity, and verify-fast.
3. Request the six-item Human Play Check; stop at HUMAN_VERIFY_REQUIRED.

## Constraints

- No EXP/Level/Reward/GameManager/UI-state implementation.
- No raw test-scene or test-prefab copying.
- Use live matching Unity Editor for serialized asset mutation; no hand-edited Unity YAML.
- Archive Phase A details before Phase B if task context grows.
