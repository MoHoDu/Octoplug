# Handoff

## Status
- Stage: **DONE** (2026-09-18) — human confirmed all round-4 items PASS and accepted remaining feel/polish gaps for current demo scope.
- Last: fixed Wall Outlet sorting/rotation and added a Socket "Approach Point" so first-time near-Socket drops (not just reconnects) work correctly.
- Next: superseded by TASK-20260918-005 (P0-1E, Power Validation + Powered State + Product Information UI). Deferred items carried into `docs/domains/connection-power.md` Deferred TODO and this Task's `meta.md`.

## Changed Files
- `Assets/01_Scripts/Power/Connection/PlugSocketConnection.cs` — unchanged since first implementation.
- `Assets/01_Scripts/Power/Cable/CableRoutingController.cs` — this round: `socketApproachSearchRadius` + `TryGetSocketApproachCell`/`TryGetSocketApproachPosition` (Socket's room-interior Approach Point, wall-thickness-scale search), acquisition/routing now anchor on it too; `connectedSortingOrder` 2→5, `draggingSortingOrder` 3→6 (to sit above the now-higher Wall Outlet).
- `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet_{One..Five}.prefab` — `Head/Body` `SpriteRenderer.sortingOrder` 1→4 (above every Wall's 2/3), one-line diff each.
- `Assets/00_Scenes/Demo/InfiniteMode.unity` — placed `Wall_Outlet_One` instance rotated to `(0,0,90)` (parallel to its vertical wall); Socket world position unaffected.
- `docs/domains/connection-power.md` — updated sorting-order note; added "Wall Outlet mounting principle (long-term, confirmed)".

## Root Cause (see log.md for full measurements)
1. **Near-socket connect failure**: acquisition only measured distance to the Socket's exact (wall-embedded) position; a first-time drop landing on a walkable cell right next to the visible outlet could still exceed the tight `0.25` radius and get rejected.
2. **Wall blocking**: Wall Outlet sprites sat at the generic Product sorting tier (`1`), below the Room's own Wall `LineRenderer`s (`2`/`3`) — rendered as if embedded in the wall.
3. **Sorting**: (carried from round 3, now re-tuned) Outlet needed to move above Wall, so Plug's Connected/Dragging orders had to move up too to stay above it.
4. **Rotation**: the placed instance's root rotation left the outlet's capsule shape horizontal against a vertical wall.

## Endpoint Model
- **Socket approach 처리**: new Approach Point = nearest walkable cell to the Socket's own cell, searched within a small, wall-thickness-scale `socketApproachSearchRadius` (3 cells) — separate from the room-scale `nearestValidSearchRadius` (24) used elsewhere. Both acquisition (`TryFindNearestSocket`) and routing (`TryRouteToSocket`) now use it.
- **Wall 반대편 방지 방식**: the small search radius structurally cannot tunnel through a wall to a different Room's floor on the far side (confirmed: a pointer past the wall's outer edge still fails). The Socket is always the route's terminal point — routing never continues past it — so Room-to-Room Cable movement remains Door-only, unchanged.

## Wall Outlet
- **Rotation**: `(0,0,90)` for the current (vertical-wall) instance — a per-instance Transform override, not a new prefab default, since rotation depends on which wall an instance ends up on.
- **Sorting**: `Head/Body` sortingOrder `1`→`4` on all 5 `Wall_Outlet_*` prefabs (above Wall's `2`/`3`).

## Automated Verification
PASS (via `eval_file` reflection against the real, live Play Mode objects):
- First-time drop at `x=3.9` and near the Approach Point (`x=3.75,y=0.05`): now connects (previously failed).
- Pointer past the wall's far outer edge (`x=4.3`, wrong side): still correctly stays unconnected.
- All round-2/3 regressions re-passed: exact-position connect, wiggle-retain, real-detach-then-reconnect, unrelated-far-drop-stays-unconnected.
- `Wall_Outlet_One`'s Body `sortingOrder` reads `4` at runtime.
- Console clean (`compilationFailed:false`, 0 errors/warnings).
- Not runtime-tested: a genuine second Room sharing this same wall (none exists in this test scene, so the "wrong-side Room" case was validated by the search-radius mechanism and the wrong-outer-edge test, not an actual second Room).

## Human Verification
1. Wall Outlet이 Wall 위에 보이고 벽 방향에 평행하게 자연스럽게 부착되어 있는가
2. Plug를 Wall Outlet Socket 위로 Drag/Drop했을 때 Wall Collider에 막히지 않고 Socket에 정상적으로 꽂히며 Plug가 Outlet 위에 보이는가
3. Socket으로 Plug를 꽂을 수는 있지만 그 Socket을 통해 Cable/Plug가 벽 반대편으로 넘어갈 수는 없는가

## Exclusive Assets
- `InfiniteMode.unity` — AI-owned only for the earlier delegated placement (2026-09-18), now including this round's rotation adjustment on that same instance.

## Resume Context
Read this handoff, `todo.md`, and `docs/domains/connection-power.md` (Wall Outlet mounting principle + Connected/Powered TODO); skip `log.md` by default.
