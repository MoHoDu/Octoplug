# Task Metadata
- **ID:** TASK-20260920-010
- **Title / Status:** Resident Demand / done
- **Owner / Agent:** MoHoDu / Claude Code
- **Domain:** Resident Demand / Infinite Mode / Product Usage
- **Base / Branch:** origin/dev @ 2f89045; feature includes a22b105 / feat/resident-demand
- **Worktree:** D:/github-worktrees/Octoplug/resident-demand
- **Updated / Stage:** 2026-09-20 / `DONE`

## Allowed Scope
- Resident generation and Demand selection from gameplay-room Product UsageTypes.
- Single/two-Need assignment, Capacity, stable Waiting/FIFO, and USING/WAITING transitions.
- Per-resident Satisfaction/Patience, Resident Card, and Product UseInfo presentation.
- Authoritative Global Satisfaction 0–100 and success-only EXP from exact Demand balance rows.
- Explicit RoomCount→RequiredEXP data and Satisfaction/EXP HUD binding.
- Official pure-core and Unity EditMode tests for progression one-shot behavior.

## Excluded
- Automatic Room progression, Camera reveal orchestration, pause/delay, rewards, Result UI, Restart, and full GameFlow.
- Currency, Rent, purchase/upgrade systems; Power/Room/Camera/routing redesign.
- Runtime Google Sheet/network dependency, runtime RequiredEXP formulas, and Product-name matching.

## Exclusive Assets
- `Assets/03_Prefabs/UI/UI_ResidentCard.prefab`
- `Assets/03_Prefabs/Products/UseInfo.prefab`
- `Assets/03_Prefabs/UI/GameStatusInfo.prefab`
- `Assets/03_Prefabs/UI/UI_GameInfo.prefab`
- `Assets/04_Data/ResidentDemand/SessionProgressConfig.asset`
- `Assets/00_Scenes/Demo/InfiniteMode.unity`

## Decisions — 2026-09-20, Owner: user
- Available Usage Pool uses every UsageType flag on active Products in gameplay-enabled room content; power/connection affect assignment, not Demand eligibility.
- Candidate rows require Enabled, MinRoomCount, and all required UsageTypes; no candidate leaves the Resident Idle/None without fallback.
- Existing Demands survive Product changes; assignment preserves Capacity, stable order, and FIFO.
- Resident Card and UseInfo reuse authored visuals/pools without icon cloning.
- Initial Global Satisfaction is 100.
- Required EXP temporarily uses explicit rows Room 1=20 through Room 15=160; no runtime formula.
- Resident Demand core does not mutate UI. GameFlow owns future threshold acknowledgement and orchestration.

## Completion Gate
- User directly recorded Human Verification PASS for the full player-facing Resident Demand and HUD behavior on 2026-09-20.
- Official tests passed for one-shot depletion/threshold behavior and RoomCount-specific RequiredEXP updates.
- TASK-010 is complete; GameFlow continues separately in TASK-011.
