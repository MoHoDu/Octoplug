# Todo

- [x] Reuse P0-1B foundation as-is (no redesign/relocation).
- [x] Implement Input (PlugDragInput), Routing (GridPathfinder, pure), Cable (CablePathRenderer + CableRoutingController) as separate responsibilities.
- [x] Fix CableRoutingGridService Start-order race (additive bug fix).
- [x] Wire new components onto existing Cable.prefab only.
- [x] Place one Product instance in InfiniteMode so the feature is playable (no new test scene).
- [x] Automated PlayMode regression via reflection-driven synthetic drag (short drag, length clamp, wall block, door-walkable, invalid-drop snap).
- [x] Confirm Room.prefab (Door Collider) diff is empty.
- [x] Update `docs/domains/connection-power.md` with Door/Routing long-term principle.
- [x] Diagnose reported input blocker (Plug not clickable) via runtime state inspection.
- [x] Fix root cause #1: ambiguous same-Z 2D ray pick (RoomArea floor collider vs Plug) — switched to explicit `Collider2D.OverlapPoint` hit-test on Plug's own collider.
- [x] Fix root cause #2 (found while testing the first fix): project's Active Input Handling is Input System-only; `UnityEngine.Input` throws at runtime — switched `PlugDragInput` to `UnityEngine.InputSystem.Mouse`.
- [x] Re-ran Logic Test (6/6) and Runtime Integration checks after both fixes — no regression.
- [ ] **Manual Mouse PlayMode confirmation by user — required before DONE** (see handoff.md Human Verification, max 3 items).
