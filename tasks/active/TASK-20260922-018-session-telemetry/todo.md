# Todo

- [x] Implement schema-v1 telemetry, event hooks, snapshots, local persistence, and documentation.
- [x] Integrate automatic InfiniteMode bootstrap and low-cost recording.
- [x] Add telemetry recorder tests and generate an inspectable JSON artifact.
- [x] Investigate human report: anonymous ID exists but no completed JSON.
- [x] Finalize active telemetry when the runtime is disabled / Play Mode stops.
- [x] Investigate RoomCount 3 RequiredEXP 40 progression stall.
- [x] Re-arm consumed threshold signals after unavailable/failed Room promotion.
- [x] Refresh routing state after Wall Outlet finalization and before Product reachability.
- [x] Reproduce the second reward-cycle Room 4 failure and isolate shared-Wall terminal approach selection.
- [x] Require the generated Product reachability check to use the Outlet's room-facing approach cell.
- [x] Move Room/Door telemetry to the promotion commit boundary.
- [x] Cover false, missing-plan, and exception retry paths plus telemetry rollback/exactly-once behavior.
- [x] Add exact-threshold and over-threshold RoomCount 3 regression tests.
- [x] Compile and run focused automated checks.
- [x] Run fast harness and task-context checks.
- [ ] Complete human play verification.

## Verification
- [x] Focused telemetry lifecycle test: 1/1 PASS
- [x] SessionProgress exact/over threshold tests: 2/2 PASS
- [x] GameFlow Room 3→4 exact/over threshold tests: 2/2 PASS
- [x] Progression/grid/telemetry regression suite: 9/9 PASS
- [x] Consecutive promotion through Room 4 shared-Wall content: 1/1 PASS
- [x] Unity compilation: PASS
- [x] Fast harness: PASS with pre-existing context warnings
- [ ] Human Play re-check through the second reward phase
