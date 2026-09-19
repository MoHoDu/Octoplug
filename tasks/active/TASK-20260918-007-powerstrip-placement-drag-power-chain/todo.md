# Todo — TASK-20260918-007

## Investigation (done)

- [x] Checked InfiniteMode scene for existing PowerStrip instances — 2 exist (`Multitap_One`, `Multitap_Two`), both validly placed (Walkable cells, unconnected). No Human Setup Required. **Later discovered (see below): these lived only in the running Editor's unsaved in-memory scene state — never written to disk before this Task's `save_scene` calls.**
- [x] Found `PowerStrip.sockets` list entries are all `null` across all 5 Multitap prefabs despite correct sizing.
- [x] Found `PowerStrip.allowedPowerWatts = 0` across all 5 Multitap prefabs.
- [x] Confirmed no design-doc decision exists for nested PowerStrip→PowerStrip (checked `docs/plans/infinity-demo-roadmap.md`, `docs/domains/connection-power.md`) — flagged as Human Decision, not resolved.
- [x] Confirmed no design-doc decision exists for Head-drag-beyond-CableLength — flagged as Human Decision, not resolved.
- [x] `plan.md` written.

## A. Wiring/data fixes

- [x] Wired `PowerStrip.sockets` list entries on all 5 `Multitap_{One..Five}.prefab` (each slot → its already-present sibling `SocketConnector`).
- [x] Set `PowerStrip.allowedPowerWatts = 10` (placeholder, matching `HousePowerBudget`'s own placeholder) on all 5.

## B–D. Script changes

- [x] `PowerStrip.cs`: added `IsPowered`/`SetPowered`.
- [x] `PowerValidationService.cs`: added `IsSocketSourceLive`.
- [x] `CableRoutingController.cs`: resolves `powerStrip`; added `ApplyPowered`/`CascadeToDownstream`/`RefreshPoweredFromUpstream`/`ResolvePoweredForConnectedSocket`/`RecomputePathFromCurrentOrigin`/public `CableInfo` accessor; updated the 3 Powered-setting call sites.
- [x] **Bug found + fixed during verification**: `RecomputePathFromCurrentOrigin` originally searched a direct path to the Plug's own cell, which always fails once the Plug is snapped onto a (deliberately non-walkable) Socket mount point — fixed to reuse `TryRouteToSocket`'s Approach-Point logic when the Plug `IsConnected`, exactly like the connect flow already does.

## E–F. New components

- [x] `HeadDragInput.cs`.
- [x] `PowerStripHeadController.cs`.
- [x] **Bug found + fixed during verification**: moving the root (for Head drag) silently dragged the descendant `Plug` transform along via ordinary Unity parent-child propagation, desyncing it from its actual snapped Socket position. Fixed by capturing/restoring the Plug's world position around every root move in `PowerStripHeadController`.
- [x] Added a `Debug.LogWarning` to `PowerStripHeadController.Start()` matching `CableRoutingController.ResolveGrid()`'s existing convention, for the same (pre-existing, non-deterministic, environment-level) `CableRoutingGridService.Instance`-not-yet-set startup race — observed intermittently in this CLI-driven Play-mode environment, not reproducible on every run, and does not affect any of the functional checks below when it does not fire. Not a redesign of initialization order (out of scope).

## G. Prefab wiring

- [x] Added Collider2D (`BoxCollider2D`, sized from `Body`'s sprite bounds) + `HeadDragInput` to `Head` on all 5 Multitap prefabs.
- [x] Added `PowerStripHeadController` to root on all 5, wired `headDragInput`/`ownCableController` references.

## H. Automated Verification — all PASS (Play mode, `eval`, fresh session each time)

- [x] PowerStrip Head Drag possible (smooth pointer-follow, snaps to cell center on release).
- [x] Room-interior movement only: Wall-mounted cell (Wall Outlet's own Socket cell) correctly blocked; far-outside-any-room position correctly blocked.
- [x] Other-PowerStrip-occupied cell correctly blocked (`SetObjectOccupied`/`IsObjectOccupied`, previously-unused TASK-006 API, now consumed).
- [x] PowerStrip Plug → Wall Outlet connect → `PowerStrip.IsPowered == true`.
- [x] Product Plug → PowerStrip Socket connect → `ApplianceSource.IsPowered == true` (full Wall Outlet → PowerStrip → Product chain, end-to-end, verified live).
- [x] PowerStrip allowed-power exceeded → connection rejected (physical connect fails, `IsPowered` stays `false`).
- [x] House allowed-power exceeded → connection rejected, independently of Strip budget.
- [x] Disconnect PowerStrip's own Plug → downstream Product `IsPowered` cascades to `false`, Flow off — Product stays physically Connected throughout (only Powered/Flow react), matching the request's "연결은 유지, Powered만 반영" semantics.
- [x] Reconnecting the PowerStrip's own Plug cascades the downstream Product's `IsPowered` back to `true` (same mechanism, both directions).
- [x] Flow direction correct through the full chain (reuses the existing, unmodified `CablePowerFlowEffect` scroll-direction logic — verified `flowLine.enabled` true on both the Strip's own Cable and the Product's Cable once genuinely Powered).
- [x] Head drag while the Strip's own Cable stays connected: the rendered Base/Flow path correctly re-routes from the moved Origin all the way to the Socket (via the Approach-Point fix above); the connection itself (and `IsPowered`) is undisturbed by the move.
- [x] Existing Product→Wall-Outlet-direct connection (no PowerStrip involved): unaffected, verified after freeing the (single) Wall Outlet Socket.
- [x] Zero Console errors throughout every verification pass. Warnings present are all pre-existing/expected (House/Strip budget-rejection logs from deliberate over-budget test cases) or the documented non-deterministic startup race noted above (intermittent, harmless, not new to this Task's pattern).

## I. Interaction UX + Connection Safety follow-up

- [x] Recorded user PASS for Head Drag, Wall blocking, Wall→PowerStrip→Product, and Flow.
- [x] Recorded remaining UX blocker: Plug/Head overlap makes intent ambiguous.
- [x] Added deterministic pointer-down resolver and capture: `UI > Plug > Product > Socket > eligible Head > Empty`; capture is held through pointer-up and released centrally by `PointerInteractionDriver`.
- [x] All candidate objects in each category are inspected; distance and stable instance ID resolve ties instead of collider/update order.
- [x] Resized each Head collider to the existing visible `Head/Body` bounds; Plug/Product/Socket priority keeps overlapping intent out of Head drag without visual changes.
- [x] Head eligibility requires its own Plug and every own Socket to be disconnected; connected-Head attempts are silent and motionless.
- [x] Added typed `ConnectionFailureReason` (`HousePowerExceeded`, `PowerStripPowerExceeded`, `SelfConnection`, `CircularConnection`) and rejection event hook.
- [x] Added pre-Connect Self/Cycle graph validation with visited sets.
- [x] Added arbitrary-depth downstream propagation with visited-set safety.
- [x] Added recursive downstream branch usage and ancestor-strip/House budget validation for Product and PowerStrip connections.
- [ ] Actual Input System pointer verification: implementation and deterministic resolver logic are compiled, but synthetic mouse state queued through Unity Pipeline did not enter the focused Game-view input loop; priority/capture-through-release therefore remains HUMAN_VERIFY_REQUIRED rather than being overstated as an automated pass.
- [x] Added owner-aware multi-cell `PlacementFootprint` reservations for all five Product and five Multitap prefabs; placement reservations do not affect cable `IsWalkable` traversal.
- [ ] Runtime/Human verification: Product↔Product, Product↔PowerStrip, and PowerStrip↔PowerStrip overlap rejection plus last-valid-position feel.
- [x] Automated verification: SelfConnection, 2-node/N-node CircularConnection, Wall→A→B→C/Product — runtime-synthetic matrix PASS; all rejected validations preserved valid edges.
- [x] Automated verification: nested Powered/logical Flow cascade, root/intermediate depower, and recovery — runtime-synthetic matrix PASS.
- [x] Automated verification: recursive PowerStrip/House budgets, Product→Wall regression, pre-existing state restoration, and Console Error 0 — PASS. Visual LineRenderer Flow and actual drag/drop fallback remain Human/runtime checks.

## J. Existing UI integration extension

- [x] Human decisions recorded: approved alert copy, single House capacity range using authored two-state icons, and no misleading PowerStrip clamp.
- [x] Expanded TASK-007 scope/ownership for scene-level data/event binding; protected UI prefab sources remain inspection-only.
- [x] Inspected/captured House/Alert/PowerInfo authored state in the isolated matching Editor.
- [x] House source-of-truth binding: the existing 22-icon pool reads authoritative House usage/allowance and applies the authored Orange/Grey colors at runtime; sprite/hierarchy/transform/layout/sorting remain unchanged.
- [x] Bound four typed failures to the existing alert and one restartable auto-hide timer; verified timed auto-hide deactivates the same root.
- [x] Wired existing scene instances only; no duplicate UI and no visual property changes.
- [ ] Human verify House meter transitions after direct/nested connect/disconnect, preserved-graph rejection, and runtime allowance change. Automated binding uses `GetHouseUsage()`/`AllowedPowerWatts`, coalesces graph/rejection/allowance notifications, and creates no duplicate UI.
- [x] Verified four alert mappings, one reused alert instance, protected-prefab zero diff, clean Unity compile, and Console Error 0.
- [x] Repaired the existing dynamic Pretendard SDF source reference to the existing OTF, retained dynamic/multi-atlas behavior, and verified all four complete Korean messages with `HasCharacters`; no font asset was created.
- [x] PowerStrip `PowerInfo` historical audit completed against the then-placeholder 10W values (5-icon shortage per prefab). The finalized initial allowances `3/3/4/5/5` supersede that placeholder mismatch and now fit the unchanged 5-icon pool; future values `6–10` remain deferred to a separate UI-capacity pass.

## K. Socket and Cable stabilization

- [x] Added explicit Socket interaction size and terminal room-side metadata to all five Multitap and five Wall Outlet prefab families; no prefab-name branching.
- [x] Raw-pointer acquisition uses Socket visual size plus Grid-cell padding, filters side/route/length, and evaluates every candidate.
- [x] Nearest typed-invalid Socket no longer hides a farther graph/power-valid Socket; if no valid candidate exists, the nearest typed failure drives the existing Alert.
- [x] Base and Flow receive the same normalized planar waypoint list; duplicate and same-direction collinear points are removed without changing authored width/material/sorting.
- [x] Retained-Socket fallback caches/reuses a valid route and no longer fabricates a direct through-wall two-point path.
- [ ] Human visual verification: rotated terminal acquisition, opposite-wall-side silent rejection, constant apparent cable width, no flip/micro-hook/odd bend, Base order 5 and Flow order 6.

## L. Remaining stabilization scope (approved 2026-09-19)

- [x] Added small Inspector-adjustable Head padding derived from Body renderer bounds; proven pointer priority/capture is unchanged.
- [x] Replaced per-frame placement blocking with free drag preview, retained reservation, Pointer-Up full-footprint validation, deterministic nearest-valid Grid search, and exact drag-start fallback.
- [x] Diagnosed first-visible-frame separation: one authored Plug is outside the walkable Grid while stale serialized LineRenderer points remained visible. Base/Flow now clear before initial route validation; Grid startup has explicit execution order, transform sync, and one authoritative rebuild with no frame polling.
- [x] Query and snap-preview valid Socket candidates from raw pointer before ordinary Grid/wall clamp every drag frame, without graph mutation before release.
- [x] Added event-driven authoritative House power meter binding; refreshes coalesce graph/rejection/allowance changes and preserve authored UI structure.
- [x] Captured route points and baked LineRenderer geometry frame-by-frame. The defect was endpoint route assembly (sub-width/reversing segments), not interior A*; the small endpoint absorption fix preserves authored LineRenderer configuration and Base/Flow shared paths.
- [x] Re-ran automated verification. Narrowed real-input and visual checks remain below; TASK-007 stays active/HUMAN_VERIFY_REQUIRED with no commit/push/DONE move/new task.

## Still deferred

- Head-drag beyond the Strip's own Cable Length: no enforcement either way (no disconnect, no movement block).
- PowerStrip `PowerInfo` is bound and truthfully displays all finalized initial allowances with the existing 5-icon pool. A separate future UI-capacity pass must expand the pool before upgraded allowances `6–10` can display; do not clamp, clone, change units, or mutate Allowed Power from icon count.

## Human Verification

Original scope and the previous priority/capture, disconnected-only movement, nested-chain, graph-preservation, and Korean-glyph checks received PASS and remain regression constraints. Final stabilization remains HUMAN_VERIFY_REQUIRED for: (1) `0.08` Body-bounds Head padding feel, (2) free preview plus valid/nearest/restored placement outcomes and reservation integrity, (3) raw-pointer Wall Socket magnetic preview/exit/opposite-side behavior, (4) House icon transitions from authoritative values, and (5) final Game-view cable appearance/first-frame behavior. TASK-007 remains active; no commit/push or next feature.

## M. Mini Metro interaction stabilization (2026-09-19, final)

- [x] Raised `HeadDragInput.hitPadding` to `0.5` on all five Multitap prefabs; `DragStarted` now carries the pointer-down world position for grab-offset computation. Priority/eligibility regression-verified unchanged.
- [x] Rewrote `PowerStripHeadController`: grab offset, half-cell-substepped axis-separated sliding via `PlacementFootprint.CanReserveAt`, no reservation churn during preview, Pointer Up commits the already-legal current position with no nearest-search/teleport fallback.
- [x] **Bug found + fixed during verification**: a same-frame click-with-no-drag (no intervening `Dragged` event) hit `OnDragEnded`'s new commit path with `lastLegalPosition` never validated, producing a spurious `Debug.LogError` on `Multitap_One` (whose own placement is not currently `CanReserveAt`-valid, a pre-existing condition consistent with its documented out-of-grid authored Plug). Fixed by treating an unchanged (`lastLegalPosition == dragStartPosition`) drag as a no-op — nothing to commit, no error.
- [x] Audited Multitap footprint sources (`Head/Body`'s full sprite bounds, ~2–3× the alpha-trimmed opaque art, computed via offline PNG alpha-channel analysis) and replaced them with a purpose-sized, non-visual, trigger `BoxCollider2D` on each root, wired to `PlacementFootprint.boundsCollider`. Verified via live-Editor readback on all five prefabs. No sprite/scale/hierarchy/visual change.
- [x] Audited the five Product prefabs' footprint (Icon collider) against their visible Icon+Background union — within ~9% in every case; left unchanged (no evidence of oversize/undersize).
- [x] Floored Socket acquisition radius at `socketDetachRadius` and raised `socketCellPaddingMultiplier` (`0.5 → 1.5`); verified numerically (`0.585` for the current `0.42`-bounds/`0.25`-cell Wall Outlet, ≥ the `0.4` detach radius). Ordering/filtering/terminal-side/Door-only unchanged.
- [x] Deferred the Plug/Socket Disconnect/Powered-false mutation from mid-drag (`OnDragged` exceeding `socketDetachRadius`) to release (`ResolveDragEnd`), via a sticky `driftedFromOriginalSocket` flag; verified a connected Plug stays `IsConnected` throughout a far mid-drag and resolves correctly (restore-original or reconnect) only at release.
- [x] Cached the loose-drag route's resolved target cell and A* cell path, reused unless the resolved cell changes; verified dense within-cell pointer samples reuse the identical cached path instance.
- [x] Added an `alwaysDropFinalCellCenter` option to `BuildWorldPath` (default `false`, every existing call site unchanged) so the ordinary loose-drag terminal never renders/measures the flickering target-cell-center point; the terminal jog always originates from the last stable cell-boundary point instead.
- [x] Restored `CablePathRenderer`'s `numCornerVertices` join-rounding, confirmed via `git diff` to have been silently dropped during the prior Stabilization pass (`Cable.prefab` still carried the corresponding, now-reconnected, dead `cornerVertices: 4` key). Verified `numCornerVertices == 4` on both Base and Flow after a live render.
- [x] Fixed `PowerUiCoordinator`'s House meter color-source aliasing (the two color references were the same `Image` objects as `housePowerIcons[0]`/`[1]`, destroying the authored Orange after the first refresh) by caching both colors once. Verified stable Orange/Grey across three consecutive refreshes.
- [x] Evidence-based renderer decision: no mesh renderer built or required — stable-anchor caching plus corner-vertex restoration is stable in the automated harness; recorded as the `## Cable Renderer Comparison`/`## Final Renderer Choice` evidence in `handoff.md`.
- [x] Automated verification: 9/9 Play-mode logic/runtime-integration checks PASS on a fresh session (grab offset + open-space move, axis-separated sliding, no-teleport release, connected-Head immobility informational check, Socket radius floor, corner-vertex restoration, House color stability, transactional-drag deferred mutation, stable-anchor cache reuse). `dotnet build`/Unity `recompile` clean. Console ground-truth 0 errors (2 pre-existing/expected warnings). `verify-fast.ps1` PASS. `git diff --check` clean.
- [ ] Human Play verification of real pointer feel (Head acquisition, sliding smoothness/no-teleport, Socket magnetism, cable stability during slow motion/Grid transitions) remains required — reflection-driven Play-mode checks are Logic/Runtime Integration only, not a substitute for real Human input per `runtime-interaction-validation.md`.

## N. Head/Plug drag unification (2026-09-19, latest — Interaction/Drag UX only)

- [x] Rewrote `HeadDragInput` to use only an explicit, Inspector-sized `Collider2D` as its hit source (`hitCollider.OverlapPoint`), matching `PlugDragInput` exactly; removed `bodyRenderer`/`hitPadding`/`TryGetHitBounds` entirely (not merely bypassed).
- [x] Removed the per-frame `IsOwnerDisconnected()` re-check that previously ran inside `HeadDragInput.Update()`'s dragging branch and silently released capture the instant it returned false — eligibility is now consulted only once, at Pointer Down (`CanStartDragAt`), matching the "captured once, held until Pointer Up" contract.
- [x] Rewrote `PowerStripHeadController.OnDragged` to directly follow `pointer + grabOffset` every frame (no substep loop, no per-frame `PlacementFootprint.CanReserveAt`) plus one cheap O(1) clamp to the strip's own Room's floor bounds, computed once at drag start.
- [x] Moved full placement legality (Room/Wall, Product/PowerStrip overlap, complete footprint) back to Pointer-Up only: valid drop commits in place, invalid drop snaps to the nearest valid Grid anchor or restores the exact drag-start position (`TryFindNearestValidPlacement`/`RestoreDragStart` reinstated).
- [x] Removed the per-frame `IsFullyDisconnected()` re-check from `OnDragged`/`OnDragEnded`; eligibility is consulted exactly once, in `OnDragStarted`.
- [x] **Bug found + fixed during verification**: `Physics2D.autoSyncTransforms` is disabled project-wide (`ProjectSettings/Physics2DSettings.asset`), so `PlacementFootprint`'s `Collider2D.bounds` could read stale (pre-drag) data immediately after `OnDragged`'s plain `transform.position` writes, causing `OnDragEnded`'s release-time validation to silently check the wrong location. Fixed with one `Physics2D.SyncTransforms()` call before validation.
- [x] Investigated the "capture suddenly releases mid-drag" report per the requested checklist (driver release, mouse state, eligibility re-evaluation, Collider-exit release, Grid/placement-failure release, root-move invalidating the collider reference) — the only real release path found was the per-frame `IsOwnerDisconnected()` re-check above; removed. No other path calls `Release`/`ReleasePointer` outside a genuine mouse-up.
- [x] Extended `CableRoutingController.ValidateInitialPlugPosition`: a loose Plug authored outside the walkable Room is now corrected at runtime (Plug `Transform` only, never saved, never the Product/PowerStrip root) to the nearest walkable, Origin-reachable-within-CableLength cell, with a diagnostic `Debug.LogWarning` naming the corrected Cable. `Multitap_One`'s previously-documented out-of-grid Plug is now corrected automatically.
- [x] Enlarged each Multitap's existing Head `BoxCollider2D` by a flat `+0.5` world units per side (a sensible starting default) and confirmed it is fully Inspector-adjustable (Size/Offset) going forward; confirmed `ProductClickInput`/`PlugDragInput` already used an explicit Collider2D and needed no change.
- [x] Automated verification: 7-check Play-mode logic/runtime-integration harness (collider-only hit source, direct-follow never blocked mid-drag by Product/PowerStrip occupancy, Room clamp at both extremes, invalid release drop not committed and final position grid-valid, valid release drop commits near target, no per-frame eligibility re-derivation, `Multitap_One` Plug now walkable) — 7/7 PASS on a fresh Play session. Regression sweep (Self-connection rejection, Cable corner-vertex/sorting sanity) also PASS. Console ground-truth 0 errors, 1 expected diagnostic warning.
- [ ] Human Play verification of real Head-grab reliability, Head-vs-Plug drag smoothness parity, Prefab-Inspector collider tuning, and the corrected initial Plug position remains required — reflection-driven Play-mode checks are Logic/Runtime Integration only, not a substitute for real Human input.

## O. Power UI binding (2026-09-19, latest — Power UI binding only)

- [x] Investigated actual Scene/Prefab wiring: confirmed `PowerUiCoordinator` genuinely exists on `/Managers/ConnectionDirector`, referencing 22 real `Image` components at `/Canvas/HUD_Area/UI_GameInfo/HousePowerInfo/Power` — a real runtime instance, not just code that compiles.
- [x] Extracted the shared icon-state logic (Usage/Allowed → active/Orange/Grey) into a new, presenter-only `PowerMeterPresenter` used by both meters; `PowerUiCoordinator.RefreshHousePowerMeter()` now delegates to it instead of duplicating the logic inline.
- [x] Added `PowerStripPowerInfoBinding`: subscribes to `PlugSocketConnection.GraphChanged`/`PowerStrip.AllowanceChanged` (filtered to its own strip), coalesces refresh identically to `PowerUiCoordinator`, sources Usage from `PowerValidationService.GetStripUsage(strip)` and Allowed from `PowerStrip.AllowedPowerWatts`. Wired once onto the shared `Assets/03_Prefabs/Multitaps/PowerInfo.prefab` (propagates to all 5 Multitap instances via nested-prefab inheritance) — icon references (5), used/unused color-source references, `strip` left null (auto-resolves via `GetComponentInParent`).
- [x] Confirmed and reported the exact PowerStrip icon shortage (`+5` icons per Multitap, Allowed=10 vs. pool=5) as Human Setup Required, both via the binding's own `Debug.LogError` firing on real events and in these task docs; structured `PowerMeterPresenter.TryApply` so it needs no code change once the pool is resized to 10.
- [x] Confirmed Scene/Prefab Source of Truth compliance: no hierarchy/layout/position/sprite/authored-color/scale change; only binding-component addition, reference wiring, and runtime active/color changes.
- [x] **Bug found + fixed, user-approved exception to the Power-graph exclusion**: `PowerValidationService.CollectDownstreamProducts`'s `edgeIsBeingReplaced` check degenerated to `null == null` (always true) for any plain query (`GetHouseUsage`/`GetStripUsage`, which pass no proposed reference), silently excluding every real downstream product/nested strip and always returning Usage=0 for anything not directly on a Wall Outlet. User selected the minimal one-line fix; applied (guard each half on its own proposed reference being non-null). Confirmed `TryValidate`/`TryValidateGraphConnection` call sites (which always pass a real proposed reference) are unaffected.
- [x] **Environment bug found and resolved**: this session's Play-mode frame loop was stuck at `FrameCount: 1` despite real time passing, stalling both meters' `StartCoroutine`-based refresh. `set_autotick` toggling alone did not fix it; exiting and re-entering Play mode after autotick was confirmed active did (frames then advanced normally).
- [x] Genuine end-to-end runtime verification, real public-API calls only, external `Image` readback only (`PowerUiCoordinator`/`PowerStripPowerInfoBinding` methods never invoked directly): House meter direct connect/disconnect PASS; nested Wall→Strip→Product chain PASS (`GetHouseUsage()` 0→3→0, first icon Grey→Orange→Grey); PowerStrip meter's Human-Setup-Required error confirmed firing from a real event-driven refresh (Console log, not reflection).
- [ ] The "1W→first icon Orange" specific numeric example from the request was not exercised literally — only TV (3W) exists as a placed `ApplianceSource` in the current demo scene; no product was added/placed to manufacture a 1W case (Scene/Prefab placement changes are out of scope this pass). The "3W→first 3 icons Orange" and "disconnect→Grey" cases were verified with TV directly.
- [x] Confirmed scene fully restored to disconnected baseline (`HouseUsage=0`, all Plugs/Sockets `IsConnected=false`) after all verification.
- [x] Cleaned up `AgentScripts/` (all one-off debug/verify scripts deleted).
- [x] Final checks: `dotnet build Assembly-CSharp.csproj --no-restore` PASS (0 warnings/errors); Unity `recompile` `up_to_date`; `verify-fast.ps1` PASS (context-budget line-count warnings only); `git diff --check`'s only flag is Unity's own universal empty `m_Name: ` field on the newly added component (present identically elsewhere, not a real hygiene issue, not hand-edited). Unity project tests zero (`NO_PROJECT_TESTS`).
- [x] Human Verification passed all 3 requested House-meter questions: the Allowed range displays correctly, multiple Products fill Orange from the left, and disconnect restores Grey. The subsequent finalized strip allowances now fit the existing 5-icon PowerStrip pool; only future values `6–10` require a separate expansion.

## P. Finalized power-balance values (2026-09-19, balance data only)

- [x] Replaced the scene House placeholder allowance with finalized initial/current `8`; preserved complete independence from every PowerStrip.
- [x] Replaced the five PowerStrip placeholder allowances with finalized independent initial/current values: `One=3`, `Two=3`, `Three=4`, `Four=5`, `Five=5`; socket lists/counts were not changed or derived from these values.
- [x] Added read-only serialized maximum balance data only: `HousePowerBudget.MaxAllowedPowerWatts=22`, `PowerStrip.MaxAllowedPowerWatts=10`. No upgrade setter, reward grant, socket mutation, or automatic coupling was added. The C# defaults carry these values for newly deserialized components; an explicit live-Editor reserialization/readback of the new fields remains pending because Unity CLI commands were blocked by the command-safety classifier outage during the continuation.
- [x] Confirmed the existing 22-icon House pool covers the finalized House maximum and the existing 5-icon PowerStrip pool covers every finalized initial PowerStrip value.
- [x] Direct runtime display check under the frozen CLI frame-loop limitation: House Usage=3/Allowed=8 rendered 3 Orange + 5 Grey + the remainder inactive; `Multitap_One` Usage=3/Allowed=3 rendered 3 Orange + the remaining 2 inactive; disconnect restored Grey. The graph was mutated only through public `PlugSocketConnection.Connect`/`Disconnect`; `Refresh()` was directly invoked only to bypass the environment's stalled coroutine, not claimed as a new event-subscription proof (that was already genuinely verified in the prior round).
- [ ] Future PowerStrip UI asset/setup: expand the shared pool from 5 to 10 icons before an upgrade can display allowances 6–10. Not part of this balance-only pass.
- [ ] Future Reward System: apply upgrades/rewards while enforcing the finalized maxima. Explicitly not implemented now.

## Still deferred (unchanged)

- PowerStrip `PowerInfo` is already bound and now displays every finalized initial allowance correctly with its current 5-icon pool. Expanding the pool to 10 is deferred until the separate upgrade/UI-capacity pass; do not clamp or let icon count mutate Allowed Power.
- Head-drag beyond the Strip's own Cable Length: no enforcement either way (unchanged from prior phases).
