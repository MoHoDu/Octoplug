## Technical Plan

1. **Starter Configuration Integration**:
   - Create `StarterRoomConfigRecord` mapping to Google Sheet starter values.
   - Inject these records in `DefaultRoomContentBalance`.
   - Update `RoomContentGenerationController` to use starter config for Room 1 and Room 2.

2. **Room Layout Promotion Sequence**:
   - Update `ProductionRoomGenerationController.Initialize()` to promote the next hint immediately, ensuring starting room count is exactly 2.

3. **Door & Wall Outlet Collision**:
   - Expand `DoorPlanningOptions` to receive `ExtraBlockedIntervals`.
   - Before planning a hint in `ProductionRoomGenerationController`, compute bounds of existing `WallOutlets` and pass as `ExtraBlockedIntervals`.
   - `RoomPlanner` uses these intervals to subtract from valid candidate coordinates for doors.

4. **Power Feasibility Validation**:
   - Filter `RoomConfig` list by querying `HousePowerBudget`. If a config has products consuming more than `AllowedPowerWatts`, reject it.

5. **Cable Reachability**:
   - Implemented within `RoomContentGenerationController` using `CanReachWallOutlet` function.
