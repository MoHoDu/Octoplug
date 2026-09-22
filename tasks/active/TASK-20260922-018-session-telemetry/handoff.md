# Handoff

## Status
- Current stage: human-verification
- Last completed action: fixed the Room 4 shared-Wall terminal approach bug after the first human re-check; focused regressions pass.
- Next action: run the InfiniteMode human flow below in a matching `feat/session-telemetry` Editor.

## Actual Failure Reason
- Play Mode Console: `[RoomContentProductShortfall]`, Room `room-0002`, Config `R001` then `R002`, Requested `1`, Placed `0`.
- Exact `Reason:`: `no usable room Wall Outlet socket is reachable within the Product's initial CableLength`.
- No `[RoomContentRequiredInfrastructure]` failure preceded it. The required Outlet finalized and was registered to `room-0002`; retries reached the same Product failure and correctly restored the hint.

## Root Cause
- The routing grid was rebuilt after Outlet finalization and was current. This was not a stale-grid failure.
- `room-0002` is below the seed Room and its Top Outlet sits on their shared Wall. The first Socket is slightly offset to the seed side of the wall (`y=-2.99` around a wall at `y=-3`).
- `CableRouteReachability` allowed both terminal sides and selected the geometrically nearest walkable approach cell. On this shared Wall that cell was in the seed Room, so the generated Product had to route through the Door and back to its own Outlet. The route exceeded the initial `CableLength=3` for every placement candidate.
- The exhaustive failed candidate scans explain the one-time hitch before rollback. The successful Room 4 regression now completes in under one second in the connected Editor.

## Fixes
- `SessionProgressState.RearmExperienceThreshold` and controller wrapper preserve EXP/RoomCount while allowing the next valid outcome to republish the threshold.
- GameFlow re-arms and returns to `Playing` for missing plans, false promotion, and promotion exceptions. Acknowledge/reset remains success-only.
- Room content performs one required grid rebuild after Wall Outlet finalization and before Product reachability; TASK-016's post-redistribution rebuild remains unchanged.
- Generated Product reachability now opts into the terminal's authored room-facing side. General drag/drop routing keeps its existing default behavior.
- Failed reachability logs now distinguish missing outlets/sockets/routes from a routed path that exceeds CableLength and include the relevant grid cells and shortest routed length.
- Room/Door telemetry moved after successful content finalization, the promotion commit boundary. Rollbacks no longer persist geometry.
- Active telemetry also finalizes idempotently when its runtime is disabled.

## Verification
- Connected-Editor focused progression/grid/telemetry suite: 9/9 PASS.
- Consecutive production promotion through Room 4: 1/1 PASS. Console confirms `[RoomContent] Room: room-0002`, `Products Generated: 1`, `WallOutlets Generated: 1`, `Outlet Walls: Top`.
- C# production and Editor test builds: PASS with 0 warnings / 0 errors.
- Covered: success→reward, false failure→retry success, missing plan, exception, EXP preservation/rearm, failed reward count 0, required outlet+product promotion, telemetry rollback 0, successful telemetry exactly once.
- Unity compiled the changed scripts as part of the successful test run.
- Full EditMode run: 137 PASS / 11 FAIL. The targeted tests pass; failures include pre-existing starter balance/SFX EditMode issues. The legacy broad GameFlow scenario also fails because test setup initializes two rooms while asserting one.
- One excluded legacy room connection test reaches successful content generation, then fails on the unrelated EditMode-only `GameplaySfxPlayer.DontDestroyOnLoad` defect.
- `verify-fast.ps1`: PASS; `verify-task-context.ps1`: WARN only, no compaction required; `git diff --check`: PASS with line-ending warnings.
- `verify-unity.ps1`: no reachable live Editor; batch Unity tests above supplied the compile/test verdict.

## Human Verification
1. At RoomCount 3, reach exactly 40 EXP and confirm Room 4, resident +1, Camera Reveal, Reward Delay, and the second Reward UI occur once without the prior hitch/rollback.
2. Force one transient promotion failure, confirm EXP stays max and no Reward UI/rolled-back Room appears, then gain EXP once and confirm retry succeeds.
3. Stop Play Mode and confirm the completed JSON contains only committed Rooms/Doors and `latest_completed_session.txt` points to it.

## Constraints
- HUMAN_VERIFY_REQUIRED; do not mark done or commit/push before the user reports PASS.
- No scene, prefab, package, ProjectSettings, or balance-value changes were made.
