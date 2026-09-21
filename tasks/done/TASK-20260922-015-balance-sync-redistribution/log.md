# Task Log

Append dated implementation details, command outcomes, investigations, and discarded approaches here. This file is historical detail and is not part of default agent context.

## 2026-09-22

- Reproduced Lobby → InfiniteMode in the task Editor through the actual Lobby button callback. Console produced a repeated `BalanceRegistry` error followed by demand-catalog exceptions.
- Confirmed the synchronized archive existed and contained all nine tables, but `Resources.Load("Balance/GameBalanceArchive")` returned null because the asset was under `Assets/02_Resources/Balance`, which is not a Unity `Resources` folder.
- Moved the archive through Unity AssetDatabase to `Assets/02_Resources/Resources/Balance/GameBalanceArchive.asset` and updated the sync destination and documentation.
- Verified typed and untyped `Resources.Load` both resolve the archive, Unity compilation completes, and a clean Lobby → InfiniteMode replay reports 0 Console errors. Two pre-existing locked-room hatching warnings remain.
- Added `BalanceRegistryTests.RuntimeArchive_IsLoadableFromExpectedResourcesPath`; targeted EditMode run passed 1/1.
- Fixed reward mapping crash caused by synced RW007 (`AddWallOutlet`), which is not implemented by the current reward runtime. Unsupported rows/effects are now skipped rather than constructing a zero-effect record; reward mapper tests passed 2/2 and reward candidate tests passed 5/5.
- Removed post-planning physics overlap correction from product generation. That correction moved a product away from its validated grid position and could then clamp it into another product without atomically updating its reservation. Products now remain at planner-approved positions and finalize through the reservation transaction; placement tests passed 6/6.
- Real Play Mode verification disproved that this alone fixed overlap. In paused room-3 state, `room-0001` contained Fan, Heater, and Air Conditioner at x=0.13/0.38/0.63 with positive-area selection-bounds overlap.
- Console provenance confirmed these were the three base Products from synced `R003`, not redistribution. The room-count 1–3 redistribution row remained `0..0`.
- Confirmed inactive staged clones were querying live `Collider2D.bounds`; authored BoxCollider bounds are now calculated from local size/offset and transforms so planning and reservation remain valid before activation.
- Redistribution now runs after `RoomContentReady`, ranks distinct Rooms by Product count ascending then area descending, selects one weighted Product per target Room with half-open cumulative intervals, and retries alternate eligible types only within that same Room.
- Added regression coverage for inactive production-prefab bounds (Fan/Heater/Air Conditioner), inactive footprint reservations, deterministic Room ranking, cumulative weight boundaries, and room-3 base-config provenance. Placement/redistribution tests passed 16/16; balance tests passed 3/3; Unity compilation and `scripts/verify-fast.ps1` passed (context-size warnings only).
- User clarified that the synced RoomConfig Product counts are not the intended base composition. Every room must have exactly one base Product; Room 3+ selects it from `제품 등장 풀`. The earlier R003 three-Product interpretation was therefore discarded.
- Reworked base content generation to require exactly one Wall Outlet and one Product. Starter rooms retain their exact Fan/TV selection; Room 3+ filters and retries weighted Product Spawn Pool entries without consuming RoomConfig Product counts.
- Made room promotion reversible: required content failure restores the prior locked hint/state and publishes no unlock/readiness events. Redistribution is now invoked explicitly after successful base content rather than through an event subscription.
- Unity compilation succeeded. Balance tests passed 3/3, placement/redistribution tests passed 16/16, and production room-generation integration tests passed 4/4, including exact-one Product/Outlet assertions for both starter rooms.
- User completed Play Mode verification and reported no errors related to the changes. Human verification passed; Task moved to DONE pending required commit/push lifecycle actions.
