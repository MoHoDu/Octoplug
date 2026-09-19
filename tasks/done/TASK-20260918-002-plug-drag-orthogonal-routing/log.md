# Task Log

## 2026-09-18

- Reused all P0-1B foundation unchanged (CableInfo, PlugConnector, SocketConnector, RoomArea, Wall/Door layers, CableRoutingGrid).
- Added: `GridPathfinder` (BFS shortest path — equivalent to A* on this uniform-cost grid; also nearest-walkable and nearest-valid-drop search + length truncation), `PlugDragInput` (OnMouseDown/Drag/Up, Z=0 plane raycast), `CablePathRenderer` (world→local conversion, collinear-point simplification, `numCornerVertices` for corner rounding — no path-point changes), `CableRoutingController` (orchestrates the three; owns no pathfinding/render math itself).
- Bug found and fixed: `CableRoutingGridService.Grid` could be read before `Start()` populated it (Unity does not order different objects' `Start()` calls), causing a false "authored Plug position not walkable" warning. Fixed by lazily ensuring one build on first `Grid` access — additive, no API change.
- Bug found and fixed: the very first/last path segment (exact Origin/target position to its nearest cell center) could be diagonal. Fixed with `AppendOrthogonalJog`, which inserts one axis-aligned bend only when needed — caught by the automated orthogonal-path test before being reported.
- Placed one `TV.prefab` instance in `InfiniteMode.unity` under `Products` (previously empty) so the feature has something to drag; its Plug landed exactly at the Room's floor-area center, verified clear of the Wall layer before saving.
- PlayMode regression via `eval` + reflection (`OnDragged`/`OnDragEnded` invoked directly, no scene mutation persisted): short drag reaches target; path is fully axis-aligned; long drag clamps near `CableLength`; drag toward the wall stops before crossing it; the Door cell reads as `GridCellState.Door`; forcing the Plug onto a wall then releasing snaps it back to a walkable cell. All 6 checks passed after the two fixes above.
- Confirmed via `git diff` that `Room.prefab` (and its Door Collider the user manually corrected) has zero changes.
- Not implemented (explicitly out of scope): Socket connect/disconnect, power validation, PowerStrip movement, procedural Door/Room generation, power-flow visuals/audio.

## 2026-09-18 (input blocker fix)

- User reported: Plug not clickable in real Mouse PlayMode, despite Logic Test passing.
- Diagnosed via live runtime inspection (`Physics2D.OverlapPointAll`, `Physics2D.GetRayIntersectionAll`, component/collider/layer/camera checks): RoomArea's full-floor `BoxCollider2D` (P0-1B) and Plug's `CircleCollider2D` sit at the same Z; Unity's single-hit ray pick (which `OnMouseDown` uses) returned "Room", never "Plug".
- Fix: `PlugDragInput` no longer uses `OnMouseDown/Drag/Up`. It now hit-tests its own `Collider2D` directly (`hitCollider.OverlapPoint(pointerWorldPos)`) each frame, which is unaffected by other overlapping colliders. `RoomArea`'s floor collider was not touched.
- While testing that fix, found a second, more severe bug: this project's Active Input Handling is Input System-only (`activeInputHandler: 1`), so `UnityEngine.Input.GetMouseButtonDown`/`.mousePosition` threw `InvalidOperationException` every frame (2000+ console errors during testing).
- Fix: switched `PlugDragInput` to `UnityEngine.InputSystem.Mouse.current` (`leftButton.wasPressedThisFrame/isPressed/wasReleasedThisFrame`, `position.ReadValue()`). No other script changed.
- Re-verified after both fixes: `hitCollider` resolves to Plug's own collider and geometrically contains the Plug's position; console has zero new errors during Play; existing 6/6 Logic Test suite still passes (no regression to Routing/Cable).
- Real user mouse-click entry into drag state cannot be verified by this session (no OS input injection available) — left as `HUMAN_VERIFY_REQUIRED` per `harness/policies/runtime-interaction-validation.md`.
