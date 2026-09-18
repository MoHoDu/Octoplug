# Todo

- [x] Create TASK-20260918-004 and confirm planning-only scope.
- [x] Inspect connector fields, Cable drag/routing seams, Socket prefab structure, and current scene contents.
- [x] Select distance-based Socket hit testing; do not add Socket colliders.
- [x] Propose a centralized symmetric connection-state arbiter.
- [x] Identify Human Setup Required and blocking player-facing decisions.
- [x] Propose the minimal implementation and verification order.
- [x] Confirm exact-position snap and `0.25` acquisition radius.
- [x] Human: add and visually position at least one wall-outlet instance in `InfiniteMode` for verification (AI Setup Allowed, delegated for this Task only; see log.md).
- [x] Approve the implementation plan and change Task status to active.
- [x] Implement approved code-only changes (`PlugSocketConnection`, `CableRoutingController` connect/disconnect wiring).
- [ ] Update the Connection / Power Domain Map after implementation (deferred — not requested for this step).

- [x] Diagnose the "Socket drop does not connect" blocker reported by human verification; found and fixed root cause (see log.md).
- [x] Human Play Check round 2: connect + grab-to-detach PASS; found 2 UX issues (Drag Plug renders behind Wall Outlet/PowerStrip; grabbing a connected Plug disconnects it immediately even for a tiny wiggle). Fixed both — see log.md.
- [x] Human Play Check round 3: drag-sorting PASS; found near-Socket drop still sometimes failed to (re)connect, and sortingOrder dropped behind Outlet the instant Mouse Up fired even on a successful connect. Also asked for a temporary "Connected" visual using `CablePowerFlowEffect`. All fixed — see log.md.
- [x] Human Play Check round 4: basic drag/connect/Flow all PASS. Found near-wall-overlap drops still sometimes failed, Wall Outlet rendered behind the Wall, Plug couldn't physically approach a wall-mounted Socket, and the outlet wasn't parallel to its wall. Fixed via a Socket "Approach Point" concept, Wall Outlet sorting (all 5 prefabs), and rotating the placed instance to match its wall — see log.md.

## Verification (future implementation)

- [x] Logic Test (real runtime `eval`/reflection against the live Play Mode instance, not a synthetic-only check): connect at exact Socket position, disconnect-on-grab, reconnect after imprecise (~0.14) drop, correct rejection beyond acquisition radius (0.4) with P0-1C fallback intact, symmetric `Plug.ConnectedSocket`/`Socket.ConnectedPlug` state in every case. Occupied-Socket rejection re-confirmed by code inspection only (only one Plug exists in this scene, so a real second-plug collision could not be runtime-tested without adding scope).
- [x] Logic Test round 2: grab-connected-plug no longer disconnects immediately; wiggle within `socketDetachRadius=0.4` on release snaps back to the exact original Socket with references intact; moving past `0.4` disconnects exactly once mid-drag; re-grab + drop back onto the Socket reconnects; Plug sprite sortingOrder elevates during any drag and restores afterward, every path.
- [x] Logic Test round 3: reconnecting to the *same* just-detached Socket now succeeds at offsets up to `socketDetachRadius` (0.25–0.4 dead zone fixed via `recentlyDetachedSocket`), while a genuinely different/far drop after a real detach still correctly stays unconnected (no false-positive reconnect); sortingOrder now resolves to `connectedSortingOrder` after a successful connect/retained-release and only to the authored resting order (0) after a real fallback/disconnect, decided after (not before) the drop outcome; `CablePowerFlowEffect.SetPowered` confirmed wired to Connect/Disconnect (`isPowered` field flips correctly; `flowLine.enabled` follows on the next `Update()` tick, confirmed via direct field/method inspection since a synchronous `eval` call has no frame boundary to let `Update()` run).
- [x] Logic Test round 4: first-time (never-connected) drops right at/near a wall-mounted Socket's visual footprint (not just recently-detached reconnects) now succeed via the new Socket Approach Point; a pointer past the Wall's far edge (outside the room, "wrong side" of the Socket) correctly still fails to connect; `Wall_Outlet_One`'s Body sprite confirmed `sortingOrder=4` at runtime (above every Wall order); all round-2/3 regressions (wiggle-retain, real-detach-then-reconnect, unrelated-far-drop-stays-unconnected, exact-position connect) re-passed unchanged.
- [x] Runtime Integration: compiled clean (no console errors) with `Wall_Outlet_One` instance now in `InfiniteMode`; `SocketConnector` confirmed present on the runtime instance; instance rotated 90° to sit parallel to its (vertical) wall, confirmed visually via `capture_scene_view`.
- [x] Human Play Check round 5 (final): all items PASS — connect/disconnect, Wall Outlet as Wall-mounted endpoint, Plug reaching Socket through Wall clamp, Connected/Dragging sorting, Door-only crossing, Flow feedback. Human accepted remaining polish gaps for current demo scope and approved **DONE**.
- [x] `NO_PROJECT_TESTS` recorded — no EditMode/PlayMode test assembly exists.

## Superseded by TASK-20260918-005 (P0-1E)

- [ ] House/PowerStrip power validation, Product Powered state, and splitting Connected vs. actually-Powered (Flow currently mirrors Connected as a temporary P0-1D stand-in) — now TASK-005's scope, not this Task's.
- [ ] PowerStrip body movement, Socket count changes, Cable length balancing, procedural Wall Outlet placement/rotation system — remain deferred beyond TASK-005 too; see `docs/domains/connection-power.md` Deferred TODO.
