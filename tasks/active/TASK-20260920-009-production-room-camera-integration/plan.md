# Plan

## Phase 1 (Current): Design Documentation & Integration Scope

This phase only produces documents. No production code, scene, or prefab changes.

### Step 1 — Record confirmed design decisions

Write `docs/decisions/infinity-progression-and-reward-loop.md` capturing all confirmed
design decisions from the 2026-09-20 brief:

- Removal of Rent / Currency system
- Unified Reward-only progression (no separate Upgrade Phase)
- Vampire-Survivors-style 3-choice Reward at every Level Up
- Reward Pool contents including Composite Rewards
- Reward Target Selection flow (popup hide → dim world → select target → apply)
- Satisfaction range (0–100), Game Over at 0
- EXP / Level Progression shape (exact formula deferred)
- Confirmed Level-Up Flow sequence (Room Generation → Camera Reveal → Reward)
- System responsibility split (Room Generation, Camera, future GameFlow)
- Future GameFlow Hooks catalogue

Mark superseded content explicitly: rent/currency, separate Upgrade Phase, and any
"buy reward" language.

### Step 2 — Update domain documents

- `docs/domains/reward-progression.md`: replace old rent/currency/upgrade design
  direction with new confirmed loop; mark old items superseded.
- `docs/domains/room-generation.md`: add confirmed responsibility boundaries and
  future GameFlow hook catalogue; preserve all existing evidence.
- `docs/domains/camera-framing.md`: add Camera Reveal responsibility note
  (zoom-out to show new room on Level Up); no implementation change.

### Step 3 — Update roadmap

Rewrite `docs/plans/infinity-demo-roadmap.md` to reflect the confirmed post-integration
development order:

1. Production Room Integration
2. Production Camera Integration
3. InfiniteMode Room/Camera/Power full regression
4. Resident Demand
5. Satisfaction / Game Over
6. EXP / Level Progression
7. GameFlow orchestration
8. Reward System + Target Selection
9. Endless / Result / Restart
10. Polish

### Step 4 — Verify and stop

Run `verify-fast.ps1`. Confirm no Unity or production changes are staged.
Document scope confirmation in handoff. Stop before any production code/scene work.

## Phase 2 (Deferred): Production Room Integration

Wire Room Generation controller and RoomPlan → CableRoutingGrid into production
`Room.prefab` and `InfiniteMode.unity`. Requires adjacent worktree.

## Phase 3 (Deferred): Production Camera Integration

Wire Camera Zoom/Pan/Hint/Reveal adapters into `InfiniteMode.unity` Cinemachine brain.
Requires adjacent worktree and Phase 2 complete.

## Phase 4 (Deferred): InfiniteMode Full Regression

End-to-end Human Verification of Room/Camera/Power in InfiniteMode scene.
