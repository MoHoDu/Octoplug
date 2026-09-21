# Plan

## Context
The focused integration defect is Plug connection to a runtime-generated Wall Outlet. Static and lifecycle checks show runtime and authored outlets use the same production prefab terminal metadata, runtime socket activation/ownership is complete, the final routing grid is refreshed before `RoomContentReady`, and the approach direction points into the generated room. The apparent wall rejection came from gameplay routing previously owning a separate terminal-approach implementation, while generation used `CableRouteReachability`.

## Approach
1. Reproduce the complete production lifecycle: room promotion, runtime outlet/Product generation, physics synchronization, final grid rebuild, and `RoomContentReady`.
2. Resolve generated objects by runtime room ownership and verify active terminal metadata, inward approach, populated grid state, shared route, magnetic candidate, exact terminal endpoint, connection, power, disconnect, and reconnect.
3. Keep generation and gameplay on the same `CableRouteReachability` terminal implementation; do not open walls, ignore collision, teleport the Plug, or add runtime-prefab special cases.
4. Expose a narrow non-injecting drop entry point that commits the same production candidate/routing/graph/power transaction as pointer-up, solely so lifecycle integration tests do not bypass connection behavior.
5. Preserve authored prefab hierarchy/visuals and all unrelated Reward, balance, UI, camera, and Resident Demand behavior.
6. Keep Google Sheet sync out of scope and status `HUMAN_VERIFY_REQUIRED`.

## Verification
- Exact Unity compile; focused generated-outlet lifecycle test; full EditMode suite.
- Existing authored terminal, blocked-wall/Door, active-socket, and reconnect regressions remain in the full suite.
- Fast, harness, task-context, and diff checks with failures/limitations reported exactly.
- Human Verification only for the newest defect: generate a room, connect its Product Plug to its generated outlet, confirm magnetic correction/cable/power, then disconnect/reconnect.
- Do not commit, push, mark DONE, or start Google Sheet sync.
