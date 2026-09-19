# Todo

- [x] Check current sorting orders (Grid -90, Floor -1, Cable 0, Product 1, Wall 2/3) — confirmed Base Cable was already behind Product; shifted Base to -1 to free integer 0 for Flow.
- [x] Extend `CablePathRenderer` to also drive an optional Flow LineRenderer from the same simplified path (no duplicate routing calc).
- [x] Add `CablePowerFlowEffect` (on/off + UV scroll only; no power decision logic; `previewPowered` clearly marked Editor-only).
- [x] Reuse existing empty `ElectricEffects` node for the Flow LineRenderer — no new GameObjects.
- [x] Create `CableFlow.mat` with the existing `Dot.png` texture; after runtime diagnosis, use transparent `Universal Render Pipeline/Unlit` because `Sprites/Default` ignores `_MainTex_ST` and cannot display UV-offset animation.
- [x] Verify Base/Flow LineRenderers receive identical positions from one drag.
- [x] Re-run full P0-1C regression (6/6) — no regression.
- [x] Confirm `Room.prefab` diff empty (Door Collider untouched).
- [x] User reported: Base Cable behind Product PASS; `previewPowered` not findable in Inspector — diagnosed and fixed (see log.md).
- [x] User reported round 2: initial Cable not visible, Flow only visible in selected Scene View, Flow pattern perpendicular — all diagnosed and fixed (see log.md).
- [x] User reported round 3: Flow visible and parallel but stationary — root cause (`Sprites/Default` ignores `_MainTex_ST`) fixed by switching only `CableFlow.mat` to transparent URP Unlit; forced-offset and natural-time Game View captures now differ.
- [ ] **Deferred TODO (not this task): Product/PowerStrip overlap alpha fade** — see meta.md Open Decisions.
- [x] **User confirmed final Human Play Check:** initial Cable display, Product-behind sorting, continuous Game View Flow, and Origin → Plug direction all PASS.
- [ ] Note: unrelated pre-existing TV.prefab UI prefab-mismatch auto-repair found in the scene diff — flagged to user, not fixed here (out of scope).
