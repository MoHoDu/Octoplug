# Todo — TASK-20260918-007

Historical checklist preserved in `history/2026-09-20-pre-wall-outlet-todo.md`.

## Unified variable-count Wall Outlet — completed automated scope

- [x] Keep this continuation in TASK-007; declare unified prefab, shared/runtime code, official tests, verifier/docs, and `InfiniteMode.unity` as Exclusive Assets. Keep all five legacy Wall Outlet prefabs read-only.
- [x] Extract shared `SocketModuleLayout` without moving PowerStrip-only PowerInfo, placement, Cable/Plug, drag, allowance, powered-cascade, or binding responsibilities.
- [x] Add Wall Outlet initial/runtime count 1–5, persistent contiguous-prefix activation, increase-only public API, typed failure, count event, and topology notification.
- [x] Make inactive-Socket interaction, connection, validation, source-live, usage, and graph gates owner-aware for both PowerStrip and WallOutlet.
- [x] Wire and read back `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet.prefab` through the exact matching Editor: Socket01–05, shared module layout, terminal metadata, initial count 1, no Wall Outlet drag behavior, disabled large Head collider retained.
- [x] Add seven official Wall Outlet EditMode tests and retain all four PowerStrip regressions.
- [x] Broaden `scripts/verify-task.ps1` and `harness/mcp/README.md` for the combined `RuntimeUpgradeTests` suites and reliable Console delta.
- [x] Run the full verifier against the authoritative staged scene: build PASS, Unity compile `up_to_date`, 11/11 PASS (PowerStrip 4/4, WallOutlet 7/7), runtime verification PASS, reliable Console delta, 0 new errors, 0 new warnings, verify-fast PASS, Overall PASS.
- [x] Prove all five legacy Wall Outlet prefab assets have zero staged and unstaged diff.
- [x] Audit `InfiniteMode.unity`: exactly one unified Wall Outlet; no legacy GUID; parent `Wall_Outlets`; sibling index 0; name `Wall_Outlet`; local transform `(4, 1, 0)`, Z `90°`, scale `(1, 1, 1)`; initial count 1; Socket01–05/layout assigned; House `8 / 22`; scene remains clean with no new unstaged scene diff.
- [x] Document the unified production source and deferred `CreateWallOutlet(wall, position, socketCount)` Room Generation contract without implementing it.

## Human Verification — PASS 7/7 (2026-09-20)

- [x] `1 → 2 → 3 → 4 → 5` grows one module at a time.
- [x] Visible and usable Socket counts always match.
- [x] Products A/B remain connected through `2 → 3`; only Socket03 is added.
- [x] Wall Socket magnetic snap remains easy from the room side.
- [x] Inactive Sockets never highlight, snap, or connect.
- [x] Opposite-side connection and Cable-through-Wall remain impossible; cross-room routes remain Door-only.
- [x] House allowance remains unchanged while Socket count increases.

## Completion

- [x] User explicitly reported Human Verification PASS for all seven checks.
- [x] Implementation changes were committed and pushed by the user (`88cc436`).
- [x] TASK-007 moved to `tasks/done/`.
- Reward System, persistence, production Room Generation, production gameplay upgrade UI, Wall Outlet Cable/Plug/drag/local allowance/PowerInfo, and socket-count decrease remain out of scope.
