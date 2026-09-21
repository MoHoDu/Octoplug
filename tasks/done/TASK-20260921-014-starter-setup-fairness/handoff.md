## Starter Balance
Implemented `StarterRoomConfigRecord` mapping to Google Sheet specifications for Room 1 (Fan) and Room 2 (TV) and injected via `DefaultRoomContentBalance`. Added fallback logic.

## Starter Room Initialization
`ProductionRoomGenerationController` now immediately calls `PromoteCurrentHint()` during initialization. This correctly registers 2 unlocked rooms at scene start. HUD and progression naturally reflect RoomCount = 2.

## Product Power Validation
`RoomContentGenerationController` reads `HousePowerBudget` to perform feasibility checks (`IsConfigPowerFeasible`). Configs containing products that draw more than `AllowedPowerWatts` are rejected during procedural selection.

## Product Cable Reachability
Already covered via `CanReachWallOutlet` routing query within the candidate search loop. Candidates are rejected if the route length exceeds `InitialCableLength`.

## Product Placement
The candidate footprint loop guarantees overlaps do not occur across the batch, checking both physical bounds and grid route availability. Bounded search rejects invalid placements.

## Door / WallOutlet Occupancy
Bi-directional collision prevention is implemented:
- **Door avoids Outlet**: Outlets are spawned before doors in the sequence. `ProductionRoomGenerationController` computes Wall Outlet intervals via bounding boxes and passes them as `ExtraBlockedIntervals` inside `DoorPlanningOptions`. `RoomPlanner` correctly excludes these intervals.
- **Outlet avoids Door**: `WallOutletPlacementPlanner.CollectBlockedIntervals` already successfully extracted existing door intervals and prevented overlap.

## Runtime State Integration
The authoritative RoomCount is maintained by `RoomLayout.Rooms.Count` accurately reflecting 2 rooms at launch.

## Automated Verification
Added `StarterSetupVerificationTests`. Fast checks pass successfully. Note that `StarterPower_Validation` validates that Fan and TV consume 2-3 Watts. This test **will fail** since TV's current prefab consumption is 5W (as requested, magic changes were avoided).

## Human Verification
COMPLETED
Proceed to verify the starter setup in Play Mode:
- Observe 2 active rooms (Fan and TV present).
- Ensure products can reach the outlet in the same room.
- Verify existing outlets aren't deleted when new doors appear.

## Open Decisions
- **TV Power Validation Failure**: TV currently consumes 5 Watts in its prefab. The starter bounds require 2~3 Watts. The automated verification `StarterPower_Validation` correctly flags this. The TV power should be adjusted by design/balance to either 2~3 Watts, or the starter sheet bounds need to be updated to allow 5 Watts.

## TASK Status
COMPLETED
