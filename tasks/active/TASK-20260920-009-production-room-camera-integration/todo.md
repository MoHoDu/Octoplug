# Todo

## Phase 1 — Design Documentation

- [x] Write `docs/decisions/infinity-progression-and-reward-loop.md`
- [x] Update `docs/domains/reward-progression.md` (supersede rent/currency/upgrade-phase)
- [x] Update `docs/domains/room-generation.md` (add responsibility boundaries + GameFlow hooks)
- [x] Update `docs/domains/camera-framing.md` (add Camera Reveal note)
- [x] Rewrite `docs/plans/infinity-demo-roadmap.md` with confirmed post-integration order
- [x] Run `verify-fast.ps1`; PASS, exit 0, no Unity/production changes staged
- [x] Update handoff with phase 1 completion status
- [x] Stop — production code/scene untouched

## Phase 2 (Deferred) — Production Room Integration

- [ ] Create adjacent worktree from latest dev
- [ ] Wire RoomGenerationController into production Room.prefab
- [ ] Implement RoomPlan → CableRoutingGrid bridge
- [ ] Apply dynamic Doors and unified Wall Outlet + ActiveSocketCount
- [ ] Grid rebuild integration
- [ ] Human Verification: Room generation in InfiniteMode

## Phase 3 (Deferred) — Production Camera Integration

- [ ] Wire Zoom/Pan/Hint adapters into InfiniteMode Cinemachine brain
- [ ] Wire Camera Reveal (zoom-out on Level Up) — stub hook only; GameFlow drives it
- [ ] Human Verification: Camera in InfiniteMode

## Phase 4 (Deferred) — InfiniteMode Full Regression

- [ ] End-to-end Human Verification: Room/Camera/Power/Connection in InfiniteMode
