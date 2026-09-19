# Task Metadata

- **ID:** TASK-20260918-004
- **Title:** P0-1D Plug-Socket Connection
- **Status:** done
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** Connection / Power
- **Base:** dev
- **Branch:** feat/infinity-power-connection
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** done — human confirmed functionally complete for demo purposes
- **Current Skill:** qa-feature

## Allowed Scope

- Investigate and plan Plug ↔ Socket connect, disconnect, and reconnect behavior.
- Future code-only implementation may reuse `PlugConnector`, `SocketConnector`, `CableInfo`, `PlugDragInput`, `CableRoutingController`, and routing helpers.
- Future connection logic may enforce symmetric endpoint state, nearest-socket hit testing, occupied-socket rejection, disconnect-on-drag-start, and existing invalid-drop fallback.
- Planning documentation in this Task folder.

## Do Not Modify

- No P0-1D implementation during this planning stage.
- No power/capacity validation, Product Powered state, Flow activation rule, PowerStrip movement, product outline, or overlap-alpha work.
- Do not add Socket colliders or restructure wall-outlet, multitap, Cable, Room, or Product prefabs (structural/hierarchy changes) — a single numeric `SpriteRenderer.sortingOrder` property fix, explicitly requested in round 4, is not a restructure; see AI Setup Allowed below.
- Do not move or recalculate the authored Door Collider.
- Do not alter unrelated scene placement or UI.

## AI Setup Allowed

- 2026-09-18 (this Task only, delegated by the human after finding `Wall_Outlets` empty): place exactly one existing `Wall_Outlet_One` prefab instance under `Connections/Wall_Outlets` in `InfiniteMode`, on an actual Room wall, clear of corners/Door, rotated to match that wall, via the live Unity Editor — no prefab/Socket structure change, no new outlet system.
- 2026-09-18 (round 4, explicitly requested): set `Head/Body` `SpriteRenderer.sortingOrder` `1`→`4` on all 5 `Wall_Outlet_{One..Five}.prefab` (one-line value fix, no structural change), and rotate the already-placed `Wall_Outlet_One` scene instance to `(0,0,90)` to sit parallel to its wall (per-instance Transform override, not a new prefab default).

## Exclusive Assets

- `Assets/00_Scenes/Demo/InfiniteMode.unity` — owned by AI for this one delegated placement only (2026-09-18); the placement (now including round 4's rotation) is a temporary P0-1D verification instance the human may reposition or replace afterward.
- `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet_{One..Five}.prefab` — owned by AI for the round-4 sortingOrder fix only (2026-09-18); no structural changes made.

## Human Decisions

- 2026-09-18: occupied Socket rejects another Plug.
- 2026-09-18 (superseded 2026-09-18): dragging a connected Plug disconnects it immediately; dropping on another valid Socket reconnects it.
- 2026-09-18 (supersedes the above, per round-2 Human Play Check feedback): grabbing a connected Plug does **not** disconnect it immediately. It stays connected while the pointer remains within `socketDetachRadius` (0.4) of the original Socket — a small wiggle-and-release snaps back to that exact Socket with the connection intact. Only moving past that radius disconnects (once); after that, the existing free-drag/reconnect rules apply unchanged.
- 2026-09-18: the Plug's own sprite renders at 3 fixed levels — authored resting order (not connected/not dragging), `connectedSortingOrder` (connected, above Outlet), `draggingSortingOrder` (actively dragging, topmost) — decided only after a drag's drop outcome is known, never on Mouse Up itself.
- 2026-09-18: reconnecting to the Socket a drag just detached from accepts the looser `socketDetachRadius`, not the tighter `socketAcquisitionRadius`, so a real detach followed by "came right back" still reconnects; a genuinely different/far Socket still uses the normal tight radius.
- 2026-09-18: invalid drops retain P0-1C nearest-valid-position behavior.
- 2026-09-18: power validation, Product Powered state, and PowerStrip movement are deferred.
- 2026-09-18 (superseded 2026-09-18): physical connection alone does not drive `CablePowerFlowEffect.SetPowered(true)`.
- 2026-09-18 (supersedes the above, per round-3 Human Play Check feedback): for this stage only, Connect/Disconnect *does* directly call `CablePowerFlowEffect.SetPowered` as a temporary "Connected" visual — explicitly not a Powered computation. Must be replaced (not extended) once Power Validation exists; recorded in `docs/domains/connection-power.md`.
- 2026-09-18 (round 4, long-term, confirmed): a Wall Outlet is a Wall-mounted endpoint — it renders above its Wall, rotates to match the Wall it's mounted against (per-instance, not a fixed prefab default), and is never placed at a Wall corner/intersection. Its Socket is a terminal endpoint for Cable routing only, never a pass-through to the far side of its Wall; Room-to-Room Cable movement remains Door-only. Recorded in `docs/domains/connection-power.md`.

## Open Decisions

- None open; `0.25` acquisition / `0.4` detach / `3`-cell approach-search radii and the `0/1/2-3/4/5/6` sortingOrder scheme (resting/Product/Wall/Wall Outlet/Connected Plug/Dragging Plug) confirmed and implemented as Inspector-tunable defaults; final feel to be judged by the human's Play Check.

## Verification State

- Round 1: Human Play Check found the drag/drop connect did not actually work; root-caused and fixed via live-Editor `eval` reflection testing against the real runtime objects. Fix: `OnDragEnded` searches for a Socket around the raw last pointer position instead of the Plug's own wall-clamped transform position.
- Round 2: Human Play Check found connect/detach worked but flagged 2 UX issues (drag-Plug sorting order, immediate-disconnect-on-grab); both root-caused and fixed.
- Round 3: Human Play Check found a reconnect dead zone (0.25–0.4 offset from a just-left Socket) and premature sorting restore (even on success), and asked for temporary Flow-based Connected feedback; all three addressed.
- Round 4: Human Play Check found near-Socket first-time drops could still fail, the Wall Outlet rendered behind its Wall, the Wall effectively blocked the Plug from reaching the Socket, and the outlet wasn't parallel to its wall. Addressed via a Socket Approach Point (acquisition/routing anchor, wall-thickness-scale search so it can't tunnel to a different Room), a Wall Outlet sortingOrder fix across all 5 prefabs, and rotating the placed instance.
- Round 5 (2026-09-18, final): human confirmed PASS — connect/disconnect, Wall Outlet as Wall-mounted endpoint, Plug reaching the Socket through the Wall clamp, Connected/Dragging Plug sorting, Door-only room crossing, and Power Flow connection feedback all working. Human explicitly accepted remaining visual/feel polish as sufficient for current demo scope and approved **DONE**.
- `pwsh -File scripts/verify-fast.ps1` passed throughout. Recompiled clean (`compilationFailed: false`, 0 console errors/warnings) after every change this Task made.
- No project tests exist: `NO_PROJECT_TESTS`.
- **Final Status: DONE.** Superseded Human Decisions above show the full evolution; the last-listed value under each superseded chain is the one that shipped.

## Deferred (carried forward — see `docs/domains/connection-power.md` Deferred TODO)

- `socketAcquisitionRadius` (0.25) / `socketDetachRadius` (0.4) / `socketApproachSearchRadius` (3 cells) and the `connectedSortingOrder`/`draggingSortingOrder` values are feel-tuning placeholders, not final balance/art decisions.
- Wall Outlet rotation-to-match-wall is manual per instance; no procedural "rotate to match the Wall it's mounted on" helper exists yet for future Room Generation to call.
- The Socket-approach "never cross to the wall's far side" rule was validated via the search-radius mechanism and a wrong-side-of-this-wall pointer test only — never against a real second Room sharing the same wall (none exists in the current test scene).
- Product overlap alpha fade (Cable visually crossing a Product/PowerStrip sprite) remains not implemented — carried from P0-1C.1/P0-1D, unchanged.
