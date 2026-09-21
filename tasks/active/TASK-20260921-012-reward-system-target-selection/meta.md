# Task Metadata
- **ID:** TASK-20260921-012
- **Title:** Reward System & Target Selection
- **Status:** active
- **Owner:** AI
- **Agent:** claude
- **Domain:** GameFlow, Reward, Room Generation, Connection / Power
- **Base:** dev
- **Branch:** feat/reward-system
- **Started:** 2026-09-21
- **Updated:** 2026-09-21
- **Current Stage:** implementation
- **Current Skill:** implement-code
## Allowed Scope
- Reward catalog, applicability, application, and target-selection lifecycle
- RW006 CableOwner and RW009 room point-placement; RW007 is removed from the reward catalog
- Automatic exactly-one Wall Outlet generation for every seed/promoted gameplay-active room
- RoomConfig socket-count min/max and local weighted socket-generation balance catalogs
- Shared runtime discovery and production spawn/finalization for Multitaps and Wall Outlets
- Wall/Outlet bounds, bidirectional Door exclusion, and eligible wall rendering
- Generated Product batch reservation, physics overlap, and routed cable reachability
- Product Tooltip outside-click and persistent/transient Alert lifecycle
- GameFlow integration and focused automated verification
## Do Not Modify
- Connection and power validation rules except narrow shared runtime readiness/reachability reuse
- Resident Demand core mechanics
- Camera framing rules or authored UI visuals
- Production prefab hierarchy or artwork
## AI Setup Allowed
- Existing scene component/reference wiring required by approved rewards
- Existing target dim/highlight integration
## Exclusive Assets
- `Assets/03_Prefabs/UI/UI_Reward.prefab`
- `Assets/00_Scenes/Demo/InfiniteMode.unity`
- `Assets/03_Prefabs/Multitaps/Multitap.prefab` only if reference inspection requires mutation
- `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet.prefab` only if reference inspection requires mutation
## Human Decisions
- Use the authored Reward UI and production Multitap/Wall Outlet prefabs without visual or hierarchy redesign.
- Updated Google Sheet is the authoring source; runtime uses local validated balance catalogs with no network access, and Google Sheet sync remains out of scope.
- RW006 targets Products and PowerStrips by real `CableInfo` capability.
- RW007 is removed entirely; it is not offered, targeted, or applied.
- Every seed/promoted gameplay-active room automatically receives exactly one Wall Outlet.
- RW009 selects a desired point inside an unlocked gameplay-active room, tries it first, and permits only a bounded nearest-safe correction.
- Multitap weights remain `1:40, 2:30, 3:15, 4:10, 5:5`; Wall Outlet socket-count weights remain `1:60, 2:25, 3:10, 4:4, 5:1`, bounded by each RoomConfig's socket min/max.
- Target selection keeps simulation paused, allows camera movement, blocks gameplay manipulation, and consumes the selecting gesture through release.
## Open Decisions
- None.
## Verification State
- Prior milestone Human Verification passed; expanded scope is implementing.
- Verification: HUMAN_VERIFY_REQUIRED
