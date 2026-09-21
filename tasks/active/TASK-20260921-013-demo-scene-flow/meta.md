# Task Metadata

- **ID:** TASK-20260921-013
- **Title:** Demo Scene Flow / Result Integration
- **Status:** active
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** Infinite Mode / GameFlow / Resident Demand / UI
- **Base:** dev (`0039599`)
- **Branch:** feat/demo-scene-flow
- **Worktree:** `D:\github-worktrees\Octoplug\demo-scene-flow`
- **Started/Updated:** 2026-09-21
- **Current Stage:** automated verification and Human Verification preparation
- **Verification:** HUMAN_VERIFY_REQUIRED

## Scope

- Preserve authored Demo visuals while connecting Lobby → InfiniteMode → GameOver → Result → Lobby.
- Count accepted final Demand Success/Failure outcomes exactly once and snapshot authoritative Room/Solved/Failed values before Result load.
- Reset transient session-flow state on new play; use runtime memory, not PlayerPrefs.
- Bind existing Lobby/Result controls and configure verified build scenes.

## Do Not Modify

- Visual hierarchy/layout/RectTransforms/sprites/colors/fonts or Resident Demand, Power, Room Generation, Reward, Camera, and balance rules beyond narrow integration.
- Placeholder Survey/Guide destinations or speculative UI.

## Exclusive Assets

- `Assets/00_Scenes/Demo/{Lobby,InfiniteMode,Result}.unity`
- `ProjectSettings/EditorBuildSettings.asset`

## Decisions

- `reward-system` Lobby/Result scenes are the user-designated latest source; exact target replacement was approved on 2026-09-21 with target `.meta`/GUID preserved.
- Result uses authoritative `SessionProgressController.RoomCount` and final Demand-resolution counts.
- Survey and Guide remain unbound because no authoritative destination exists.

## Open Decisions

- Survey destination.
- Guide destination/behavior.
