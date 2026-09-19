# Plan — TASK-20260918-007

## ActiveSocketCount source-completion addendum (2026-09-19; current authority)

- `Assets/03_Prefabs/Multitaps/Multitap.prefab` is the one persistent variable-capacity source; Socket01–05, Whole01–05, Cable/Plug, PowerInfo, and bindings are never replaced during expansion.
- Capacity is increase-only. Same-count is a successful no-op; every decrease returns `SocketCountDecreaseUnsupported` without state mutation.
- Finalized authored PowerInfo local positions are count 1 `(-0.23, 0.5)`, count 2 `(-0.46, 0.5)`, count 3 `(-0.69, 0.5)`, count 4 `(-0.92, 0.5)`, count 5 `(-1.15, 0.5)`. They are no longer Human Setup Required.
- The source-prefab 1→5 matrix is complete and passing. Remaining completion work is scene migration/readback, real connected-state preservation, blocked-expansion rollback/reservation integrity, inactive-topology runtime checks, project-test discovery, and Human pointer verification.
- Wall Outlet structural work remains forbidden until the requested Multitap Human Verification passes.

## Finalized balance-data addendum (2026-09-19; supersedes placeholder values only)

- Set the scene House current allowance to `8` and represent its finalized independent maximum as serialized/read-only data `22`.
- Set independent PowerStrip current allowances per prefab: `Multitap_One=3`, `Two=3`, `Three=4`, `Four=5`, `Five=5`; represent each strip's finalized independent maximum as serialized/read-only data `10`.
- Keep socket count, House allowance, and each strip allowance as separate authoritative state. Do not derive or synchronize any of them.
- Do not implement rewards or upgrades. The maximum fields are balance data only; no mutation mechanism is added.
- Do not expand or redesign UI in this pass. The House pool already supports 22; the current strip pool supports the finalized initial values but a separate future asset/setup pass must expand it to 10 before values 6–10 can be displayed.

## A. Wiring/data fixes (prerequisite, all 5 `Multitap_{One..Five}.prefab`)

1. `PowerStrip.sockets` list entries are all `null` despite being correctly sized — assign each slot to its already-present sibling `Sockets/Socket`/`Socket (N)` child's `SocketConnector`.
2. `PowerStrip.allowedPowerWatts = 0` on all 5 — set to a uniform demo placeholder (`10`, matching `HousePowerBudget`'s own placeholder) so the budget check is testable at all.

Both are prefab-source fixes via `PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset` (matches TASK-006 precedent), not scene-instance overrides — every future Multitap instance should be correct by default.

## B. `PowerStrip.cs` — add Powered surface

Add `IsPowered { get; private set; }` + `SetPowered(bool)`, mirroring `ApplianceSource`. No other field changes.

## C. `PowerValidationService.cs` — new `IsSocketSourceLive`

```csharp
public static bool IsSocketSourceLive(SocketConnector socket)
{
    if (socket == null) return false;
    var strip = socket.GetComponentInParent<PowerStrip>();
    return strip == null || strip.IsPowered;
}
```
Wall-Outlet-backed sockets (no `PowerStrip` ancestor) are always live (subject to the existing House-budget check elsewhere); a `PowerStrip`-backed socket is live only if that strip `IsPowered`. `TryValidate`'s existing wattage-only gate is UNCHANGED — it still purely decides whether the physical connection is allowed. `IsSocketSourceLive` is a separate, later-applied factor for the resulting Powered value (see D) — a Product/Strip's Plug can physically connect to a currently-unpowered upstream Socket (mirrors real life: plug a lamp into an unplugged power strip, it just doesn't light up yet).

## D. `CableRoutingController.cs` — generalize to PowerStrip + add cascade/reroute

1. Add `private PowerStrip powerStrip;` resolved in `Start()` via `GetComponentInParent<PowerStrip>()` (alongside the existing `applianceSource` resolution — exactly one of the two will ever be non-null for a real Cable instance).
2. New private `ApplyPowered(bool powered)` helper — replaces the 3 existing scattered `applianceSource?.SetPowered(x); powerFlowEffect?.SetPowered(x);` call sites (`ValidateInitialConnection`, `OnDragged`'s detach branch, `TryConnectToNearbySocket`'s connect branch):
   ```csharp
   private void ApplyPowered(bool powered)
   {
       applianceSource?.SetPowered(powered);
       powerFlowEffect?.SetPowered(powered);
       if (powerStrip != null)
       {
           var changed = powerStrip.IsPowered != powered;
           powerStrip.SetPowered(powered);
           if (changed) CascadeToDownstream(powerStrip);
       }
   }
   ```
3. New private static `CascadeToDownstream(PowerStrip strip)` — for each `strip.Sockets` with a non-null `ConnectedPlug`, resolve `plug.GetComponentInParent<CableRoutingController>()` and call its new `RefreshPoweredFromUpstream()`. Generic (component-type-based, no prefab-name branching) — this recursion also naturally reaches a nested strip's own downstream if one ever exists, though nested strips are explicitly unverified this Task (see Human Decisions).
4. New public `RefreshPoweredFromUpstream()`:
   ```csharp
   public void RefreshPoweredFromUpstream()
   {
       if (cableInfo == null || cableInfo.Plug == null || !cableInfo.Plug.IsConnected) return;
       ApplyPowered(ResolvePoweredForConnectedSocket(cableInfo.Plug.ConnectedSocket));
   }
   ```
5. New private `ResolvePoweredForConnectedSocket(SocketConnector socket) => socket != null && PowerValidationService.IsSocketSourceLive(socket);` — the single place the final Powered *value* (post physical-connection) is computed, used uniformly by connect, initial-load, and cascade paths.
6. Update the 3 call sites:
   - `TryConnectToNearbySocket`: wattage `TryValidate` gate unchanged (still the only thing that can reject the physical connection for a Product's Plug — unchanged for `applianceSource == null`, i.e. a PowerStrip's own Plug skips it exactly as today). On success, replace the old unconditional `applianceSource?.SetPowered(true); powerFlowEffect?.SetPowered(true);` with `ApplyPowered(ResolvePoweredForConnectedSocket(socket));`.
   - `OnDragged`'s detach branch: replace `applianceSource?.SetPowered(false); powerFlowEffect?.SetPowered(false);` with `ApplyPowered(false);`.
   - `ValidateInitialConnection`: drop the early-return on `applianceSource == null` (so a PowerStrip's own authored-connected-at-load Plug is also handled); keep the existing wattage check only when `applianceSource != null`; finish with `ApplyPowered(existingWattageResult && ResolvePoweredForConnectedSocket(socket))`.
7. New public `RecomputePathFromCurrentOrigin()` — re-finds the route between the Cable's current `Origin` and its `Plug`'s current position and re-renders (Base+Flow, unchanged shared-path guarantee), with **no** Cable Length truncation/enforcement (Human Decision, deferred). No-op if no route currently exists (e.g. Head dragged to a disconnected room) — leaves the last-rendered path in place rather than clearing it.

## E. New `Octoplug.Power.Input.HeadDragInput`

Copy of `PlugDragInput`'s mouse-drag detection idiom (`Mouse.current`, `Camera.main` ray-to-Z0-plane, `hitCollider.OverlapPoint`), same `DragStarted`/`Dragged`/`Dragged(Vector2)`/`DragEnded` event shape, **without** the Plug-specific `AnyDragStarted`/`IsPointerOverAnyPlug` static registry (Head-drag is a distinct capability from Plug-drag; keeping them separate avoids Head grabs being misreported as Plug grabs to `ProductTooltipController`/`ProductClickInput`).

## F. New `Octoplug.Power.Cable.PowerStripHeadController`

Placed on the Multitap root (sibling of the existing `PowerStrip` component). Fields: `[SerializeField] private HeadDragInput headDragInput;` (on `Head`), `[SerializeField] private CableRoutingController ownCableController;` (on `Cable`).

- Drives the **root's** `transform.position` (not just `Head`'s) — `Head`/`Sockets`/`Cable`'s `CableOrigin`/`PowerInfo` are already siblings under the root, so moving the root moves all of them together as one rigid unit with zero reparenting.
- `Start()`: registers the root's current cell as occupied via `CableRoutingGrid.SetObjectOccupied`.
- On `Dragged(pointerWorldPos)`: candidate cell = `grid.WorldToCell(pointerWorldPos)`. Valid iff `grid.IsWalkable(candidateCell)` AND (`!grid.IsObjectOccupied(candidateCell)` OR it's this strip's own current cell). If valid, move root to the raw pointer position (smooth follow, matching Plug-drag feel) and call `ownCableController.RecomputePathFromCurrentOrigin()`; if invalid, hold the last valid position (no movement that frame).
- On `DragEnded()`: snap the root's final position to its current valid cell's center (clean logical anchor, matching the Grid-anchor design used everywhere else), update occupied-cell bookkeeping (free the old cell, occupy the new one), and do a final `RecomputePathFromCurrentOrigin()`.
- Deliberately does **not** attempt Product-footprint collision (Products still don't call `SetObjectOccupied`, unchanged from TASK-006) — only Wall (`Blocked`, already excluded by `IsWalkable`) and other PowerStrip roots are respected as obstacles this Task.

## G. Prefab wiring (all 5 `Multitap_{One..Five}.prefab`)

- Add `Collider2D` (matches the existing Plug/Product collider sizing convention — reuse `Body`'s sprite bounds) + `HeadDragInput` to `Head`.
- Add `PowerStripHeadController` to the root, wire `headDragInput` → `Head`'s new component, `ownCableController` → `Cable`'s existing `CableRoutingController`.
- Apply the A.1/A.2 wiring fixes.

## H. Verification

Automated (via `eval`, Play mode, same technique as TASK-006): grid placement (walkable/blocked/occupied), PowerStrip's own Plug connect to `Wall_Outlet_One` → `PowerStrip.IsPowered == true`, Product's Plug connect to a PowerStrip Socket → validates wattage + `IsSocketSourceLive` → `ApplianceSource.IsPowered` reflects both, disconnect the strip's own Plug → cascade sets the downstream Product `IsPowered = false` + Flow off, over-budget rejection (strip and House), Head drag moves the root + re-renders the strip's own Cable path, Head blocked by Wall/another strip, existing Product→Wall-Outlet-direct regression unaffected, zero Console errors.

Human Verification (max 3, per the request): Head drag feel/bounds, full chain Flow/Powered continuity, over-budget rejection.

## I. Interaction UX + Connection Safety follow-up (confirmed 2026-09-18)

1. Add one shared pointer resolver that selects exactly one target on pointer-down using `UI > Plug > Product > Socket > eligible Head > Empty`, and captures it through pointer-up. Plug/Head inputs register as candidates instead of independently winning by Update/collider order; a central driver releases non-drag captures.
2. Inspect every candidate within a category and resolve ties by distance then stable instance ID. Head input uses the visible Body-sized collider and is eligible only while its own Plug and every own Socket are disconnected. No visible or hierarchy/layout changes.
3. Add typed `ConnectionFailureReason` values and a `CableRoutingController.ConnectionRejected` hook. Reject Self/Cycle before routing and before `PlugSocketConnection.Connect`; apply the existing rejected-drop fallback and never mutate Powered/Flow on failure.
4. Support arbitrary-depth acyclic PowerStrip chains by graph traversal over actual `PowerStrip.Sockets`/connected Plugs. Use visited sets for cycle checks, branch wattage, and power propagation.
5. Recompute full downstream branch wattage for every ancestor strip and validate House/strip budgets for both Product and PowerStrip connection attempts.
6. Verify interaction priority/capture, self and 2-/N-node cycle rejection, Wall→A→B→C/Product, downstream depower/Flow off, nested branch and House overages, direct Product→Wall regression, and zero Console errors.
7. Keep TASK-007 active and HUMAN_VERIFY_REQUIRED; do not start another feature.
8. Stabilization extension (confirmed 2026-09-19): reserve owner-aware multi-cell Product/PowerStrip footprints while keeping cable traversal independent; select Sockets from raw-pointer visual/cell radius with Wall terminal approach metadata; validate prospective physical load independently of Powered state; repair the existing Pretendard dynamic source/glyphs; normalize one shared Base/Flow cable path without visual property changes.

## J. Existing UI integration extension (confirmed 2026-09-18)

1. Reuse only the placed `UI_GameInfo`/`HousePowerInfo` and `UI_AlertText` instances; do not alter any UI visual, hierarchy, RectTransform, layout, sprite, color, sorting, or font property.
2. Add one active scene-level `PowerUiCoordinator` to `ConnectionDirector`. Wire exact existing icon objects and the existing alert TMP label/root through serialized references.
3. House display reads only `PowerValidationService.GetHouseUsage()` and `HousePowerBudget.AllowedPowerWatts`. Use the authored used/unused icon states inside the allowed range and deactivate capacity beyond Allowed. Stop with Human Setup Required if the authored two states cannot be mapped unambiguously.
4. Add a minimal `CableRoutingController.PowerStateChanged` event, raised only on an actual owner Powered-state change. Coalesce nested cascade notifications into one House refresh; never poll full graph usage every frame.
5. Map the four typed failure reasons to the approved Korean copy in one presentation method, activate the existing alert, and restart one Inspector-configurable auto-hide timer. Never instantiate an alert or mutate connection state from UI.
6. Leave PowerStrip `PowerInfo` unwired: 5 authored icons cannot truthfully represent the current 10W allowance. Record Human Setup Required instead of clamping.
7. Verify source-of-truth equality, all four alerts, no duplicate UI instances, rejection preservation, protected-prefab zero visual diff, and Console Error 0. Keep Human Verification to House readability, alert behavior, and chain/input regression.

## K. Final stabilization implementation (2026-09-19)

This section supersedes the earlier preview-blocking placement behavior in F and the earlier House UI blocker in J.

1. Head targeting uses the existing Body renderer's world bounds plus serialized `0.08` padding; physical collider size and resolver priority are unchanged.
2. Head drag is a free visual preview. The existing placement reservation is retained throughout drag; Pointer Up alone validates the complete footprint and atomically commits the desired Grid anchor, the deterministic nearest valid anchor, or restores the exact drag-start transform.
3. `CableRoutingGridService` starts at execution order `-100`, synchronizes Physics2D transforms, and performs one authoritative `Start()` rebuild. `PlacementFootprint` retries initialization once in `Start()` rather than polling each frame.
4. Initial Base/Flow points are cleared before route validation. An authored Plug outside the walkable Grid remains unmoved and renders no stale detached cable.
5. A valid Socket candidate is queried from the raw pointer before ordinary clamp on every drag frame and may preview-snap without graph mutation.
6. `PlugSocketConnection.GraphChanged` plus allowance-change notifications drive a coalesced House meter refresh using authoritative usage/allowance and the existing Orange/Grey colors. Strip meters remain unbound because each five-icon pool is short by five icons for its 10W allowance.
7. Runtime waypoint and baked-mesh capture classified the cable defect as endpoint route assembly: sub-width endpoint segments reversed as the pointer crossed adjacent Grid centers. `BuildWorldPath` now absorbs endpoint-adjacent segments shorter than `minRenderSegmentLength`; interior A*, LineRenderer properties, materials, Flow animation, and sorting 5/6 are unchanged.
8. Automated checks passed, but real input feel and final Game-view visual quality remain `HUMAN_VERIFY_REQUIRED`. No Git closure is authorized.

## L. Mini Metro interaction stabilization (2026-09-19, final)

This section supersedes F/K's preview-then-validate placement model, the small `0.08` Head padding, and the prior "LineRenderer viable, no mesh needed" conclusion — the user's latest Human Verification found all three insufficient and adopted Mini Metro (continuous intent recognition, hard restriction of invalid states, no pixel-perfect input) as the explicit UX reference.

1. `HeadDragInput.hitPadding` raised to `0.5` world units (Inspector-adjustable per prefab); `DragStarted` now carries the pointer-down world position. Priority order and eligibility (`UI > Plug > Product > Socket > eligible Head > Empty`, disconnected-only) are unchanged.
2. `PowerStripHeadController` rewritten: a grab offset keeps the clicked point under the pointer; every dragged frame advances toward the desired position in half-Grid-cell substeps, testing combined/X-only/Y-only candidates through the existing non-mutating `PlacementFootprint.CanReserveAt` and taking whichever is legal (axis-separated sliding). The visual root is therefore never in an illegal position, so Pointer Up only calls `TryReserveAt` on the already-legal current transform — no nearest-valid search, no teleport-to-start, no reservation churn during preview. A same-frame click-with-no-drag is a no-op rather than an error.
3. Footprint fidelity: each Multitap's placement footprint (previously `Head/Body`'s full, transparent-margin-inclusive sprite bounds) now uses an added, non-visual, trigger `BoxCollider2D` on the root sized to that prefab's alpha-trimmed Body opacity (computed offline from the source PNGs). Product prefabs were audited and left unchanged (footprint within ~9% of the visible Icon+Background union already).
4. Socket acquisition radius is floored at `socketDetachRadius` (`Mathf.Max`) and the cell-padding multiplier raised `0.5 → 1.5`, so first-touch acquisition (~0.585 units for a `0.42`-bounds/`0.25`-cell Socket) is never stricter than the forgiving-reconnect radius. Ordering/filtering (raw pointer, terminal approach-side, route/length, graph, power, nearest-valid-wins) is unchanged.
5. Transactional drag: `OnDragged` no longer calls `PlugSocketConnection.Disconnect`/`ApplyPowered(false)` when the pointer exceeds `socketDetachRadius` — it only records a sticky `driftedFromOriginalSocket` flag. The real Disconnect/Powered-false mutation, and the subsequent reconnect/loose-drop resolution, all happen exactly once in `ResolveDragEnd` (release), matching "no physical/Powered/Flow mutation before release."
6. Stable cable anchor: the resolved target Grid cell and its A* cell path are cached and reused for as long as the pointer stays in the same cell (`cachedLooseOriginCell`/`cachedLooseTargetCell`/`cachedLooseCellPath`); `BuildWorldPath` gained an `alwaysDropFinalCellCenter` parameter (default `false`, so every other call site — Socket routing, initial render, reject fallback — is unchanged) used only by the ordinary loose-drag terminal, so the target cell's own center point is never part of the rendered/measured polyline and the terminal jog always originates from the last stable cell-boundary point.
7. `CablePathRenderer.numCornerVertices`/join-rounding, silently dropped during the prior Stabilization pass (confirmed via `git diff` against the committed baseline), is restored from a serialized field — the highest-confidence, lowest-risk fix for the reported spike/width-pop, applied before any mesh-renderer work.
8. `PowerUiCoordinator`'s House color source aliasing bug (found during exploration: the two color-reference `Image`s were the same objects as `housePowerIcons[0]`/`[1]`, so the first refresh destroyed the authored Orange) is fixed by caching both colors once.
9. Evidence-based renderer decision: with (6) and (7) in place, the runtime/logic harness showed stable, cell-quantized route topology and restored corner rounding on both Base and Flow, with no reproduced flip/spike/reversal in the captured checks — LineRenderer is kept; no mesh conversion was built or required. See `handoff.md` for the full evidence trail.
10. Automated verification: `dotnet build`/Unity `recompile` clean; a 9-check Play-mode logic/runtime-integration harness (grab offset, sliding, no-teleport, immobility, radius floor, corner vertices, House color stability, transactional defer, stable-anchor cache reuse) all PASS; Console ground-truth 0 errors. TASK-007 remains active/`HUMAN_VERIFY_REQUIRED`; no commit/push/DONE/next feature.

## M. Head/Plug drag unification (2026-09-19, latest — Interaction/Drag UX only)

Explicit scope this pass: Interaction/Drag UX only. Power UI, Cable Grid size, Cable renderer, and Power graph are unchanged (per user directive) — sections H–L above remain the last word on those.

1. `HeadDragInput` rewritten to mirror `PlugDragInput` exactly: an explicit, Inspector-sized `Collider2D` (`hitCollider`) is the sole hit-test source (`hitCollider.OverlapPoint`) — the Renderer-bounds+padding computation (`bodyRenderer`/`hitPadding`/`TryGetHitBounds`) is removed entirely, not just bypassed. `DragStarted` still carries the pointer-down position for the grab offset.
2. Eligibility (`IsOwnerDisconnected`) is consulted only inside `CanStartDragAt`, i.e. only at the Pointer-Down resolution the shared `PointerInteractionResolver` already performs once per press. The per-frame re-check that previously ran inside `HeadDragInput.Update()`'s dragging branch — and silently released capture the instant it returned false — is removed; capture is held unconditionally from a successful `TryCapture` to the next real Pointer Up.
3. `PowerStripHeadController` rewritten to match Plug's cost profile: `OnDragged` is now a direct `pointer + grabOffset` assignment (`MovePreservingPlug`) plus one cheap O(1) clamp to the strip's own Room's floor bounds (found once, at `OnDragStarted`, via `RoomArea.FloorArea`, inset by the footprint's half-size) — no per-frame `PlacementFootprint.CanReserveAt`, no substep loop, no Grid/cell computation while dragging. `IsFullyDisconnected` is likewise consulted exactly once, in `OnDragStarted`.
4. Full placement legality (Room/Wall containment, Product/PowerStrip overlap, the complete multi-cell footprint) moved back to Pointer-Up only, restoring the deterministic nearest-valid-Grid-anchor-or-exact-drag-start-restore policy (`TryCommitPlacement`/`TryFindNearestValidPlacement`/`RestoreDragStart`) that predates the per-frame-sliding design — this is what the request calls "이미 확정된 규칙."
5. Bug found and fixed: `Physics2D.autoSyncTransforms` is disabled project-wide, so `PlacementFootprint`'s `Collider2D.bounds` could still reflect the pre-drag position immediately after `OnDragged`'s plain `transform.position` writes — `OnDragEnded` now calls `Physics2D.SyncTransforms()` once before validating, or release-time placement checks would silently validate the wrong (stale) location.
6. Investigated the "capture suddenly releases mid-drag" report (`PointerInteractionDriver`/`PointerInteractionResolver`/eligibility/Grid-failure/root-move-invalidates-collider-reference, per the request's own checklist): the only actual release path found was `HeadDragInput`'s own per-frame `IsOwnerDisconnected()` re-check (item 2) — removed. No other code path calls `Release`/`ReleasePointer` outside a genuine mouse-up.
7. `CableRoutingController.ValidateInitialPlugPosition` extended: a loose (not authored-connected) Plug whose initial position lands on a non-walkable cell is now corrected at runtime to the nearest walkable cell that is also actually path-reachable from Origin within Cable Length (reusing `GridPathfinder.TryFindNearestValidDropCell`, the same Door-only/no-cross-wall-reachability guarantee already used for a live drop), falling back to Origin's own position if none is found. Only the runtime Plug `Transform` is moved — never saved, never the Product/PowerStrip root. A `Debug.LogWarning` names the corrected Cable.
8. Each Multitap's Head `BoxCollider2D` (previously an unused, Body-sized fallback) is now the live interaction hit area, enlarged by a flat `+0.5` world units per side as a sensible starting default and left fully Inspector-adjustable (Size/Offset) — satisfying the request's "user tunes Size/Offset directly in the Prefab" requirement. Product's existing `ProductClickInput`/Plug's existing `PlugDragInput` already used an explicit Collider2D as their hit source and needed no change.
9. Verification: `dotnet build`/Unity `recompile` clean; a 7-check Play-mode logic/runtime-integration harness (collider-only hit source, direct-follow-never-blocked-mid-drag, Room clamp both extremes, invalid-drop-not-committed-and-final-position-valid, valid-drop-commits-near-target, no per-frame eligibility re-derivation, `Multitap_One`'s Plug now walkable) all PASS on a fresh Play session; a regression sweep (Self-connection rejection, Cable corner-vertex/sorting sanity) also PASS; Console ground-truth 0 errors (1 expected diagnostic warning from item 7). TASK-007 remains active/`HUMAN_VERIFY_REQUIRED`; no commit/push/DONE/next feature.

## N. Runtime upgrade controls (2026-09-20, latest authority)

This section supersedes only earlier statements that upgrade mutation, Cable Length strengthening, and PowerInfo capacity 6–10 are deferred. All existing graph, drag, placement, visual-authorship, and scene-preservation contracts remain authoritative.

1. Add exactly three independent runtime production operations: `PowerStrip.TryUpgradeActiveSocketCount`, `PowerStrip.TryUpgradeAllowedPowerWatts`, and `CableInfo.TryUpgradeCableLength`. Each increases its own authoritative value by exactly one and returns a typed failure without partial mutation.
2. Socket +1 reuses the verified owner-aware placement transaction, persistent Socket01–05 identities, authored module/terminal colliders, count-specific PowerInfo positions, and post-commit topology notification. Arbitrary count mutation is not a public gameplay/debug surface; decreases remain unsupported.
3. Allowed Power +1 changes only `PowerStrip.allowedPowerWatts`, is capped at `min(MaxAllowedPowerWatts, 10)`, and raises `AllowanceChanged` once. It remains independent from House Power, socket count, Cable Length, connections, Usage, Powered, and Flow.
4. `CableInfo.cableLength` remains the sole Cable Length authority. Cable +1 raises a `CableLengthChanged` event; a live Plug drag invalidates its loose-route cache and reruns the existing length-aware preview from the last pointer position. Idle/connected Plugs are not moved or disconnected. Head movement's intentionally length-agnostic reroute is unchanged.
5. Existing Product tooltip Cable Length and PowerStrip PowerInfo refresh from authority events. PowerInfo continues to mean one icon per allowed watt; socket count is represented only by actual authored Socket/Whole modules. Verify the unified `PowerInfo.prefab`'s ten authored icons and binding references through the matching Editor; repair references only if needed, with no hierarchy/sprite/transform/layout/scale/sorting/color redesign.
6. Add one removable custom Inspector under `Assets/Editor` showing the three runtime values and three Play Mode-only +1 buttons. It calls production APIs only—no serialized/private-field overwrite, reflection, direct visual toggle, prefab replacement, dirty/save operation, or production gameplay UI.
7. Runtime upgrades are Play-session-only. Reward selection, probability, generation, persistence, production upgrade UI, Wall Outlet work, InfiniteMode migration, and Head-drag Cable Length enforcement remain out of scope.
8. Verification targets `2 / 5 / 3 → 3 / 5 / 3 → 3 / 6 / 3 → 3 / 6 / 4`, connection/identity preservation, atomic footprint rollback, allowance cap 10, immediate future validation, real Plug reach expansion, UI refresh, and Editor-only debug isolation. Actual Inspector-button interaction remains Human Verification.
9. Work only in `D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration` at baseline `5b6b1a0`; preserve `.vscode/settings.json`, `Assets/00_Scenes/Demo/InfiniteMode.unity`, and root `TODO.md` exactly as found. No commit, push, DONE transition, or scene save.

## O. Minimal reusable Unity verification foundation (2026-09-20, current authority)

1. Add one Editor-only Unity Test Framework assembly under `Assets/Tests/Editor`, one reusable `UnityVerificationFixture`, and exactly four representative Multitap tests: runtime upgrade independence, connected Socket01/02 identity preservation, blocked expansion rollback, and inactive Socket rejection.
2. Fixture state must come from authored prefabs and public production APIs. Do not overwrite private serialized production state through reflection or `SerializedObject`; connections use `PlugSocketConnection.Connect`/`Disconnect` only. Wall Outlet support is fixture capability only, not feature implementation or a Wall Outlet test.
3. Add `scripts/verify-task.ps1` as the single exact-worktree entry point. It orchestrates build, matching-Editor compile/readiness, EditMode discovery/run, Console baseline/delta without clearing, and existing `verify-fast.ps1`, then prints the fixed result labels and returns nonzero for required failure or unreliable Console delta.
4. Add only a concise usage section to `harness/mcp/README.md`. Do not redesign harness directories, migrate Task document formats, add CI, or build a report framework.
5. Preserve the dirty settings/TODO state; do not commit, push, mark DONE, or implement Reward System. TASK-007 remains `active` / `HUMAN_VERIFY_REQUIRED`.

## P. Unified variable-count Wall Outlet continuation (2026-09-20, latest authority)

This section supersedes N/O statements that Wall Outlet implementation and `InfiniteMode.unity` migration are forbidden. It does not supersede the independent Multitap upgrade, connection graph, terminal routing, visual-authorship, or Human Verification contracts.

1. Keep the work in TASK-007. `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet.prefab` becomes the single production Wall Outlet source with persistent authored Socket01–05 and Whole01–05 identities. The five legacy Wall Outlet prefab assets remain unchanged, undeleted, and read-only migration inputs.
2. Extract only persistent module/socket/First/End/authored-collider behavior into a narrow shared layout used by PowerStrip and WallOutlet. PowerInfo positioning, PlacementFootprint reservation, Plug/Cable, drag, local allowance, powered cascade, and meter binding stay PowerStrip-only.
3. Add independent Wall Outlet `InitialSocketCount`/runtime `ActiveSocketCount`, contiguous-prefix activation for counts 1–5, increase-only public +1 mutation with typed failure, identity preservation, one matching event, and topology notification. Runtime count changes may not instantiate, destroy, replace, rename, or reconnect persistent objects.
4. Make Socket activity and source-live validation owner-aware without prefab-name branching. Active Wall Outlet sockets remain House-backed and have no local allowance; inactive sockets are excluded from pointer, magnetic, validation, graph, usage, and final Connect paths.
5. Preserve existing terminal routing and magnetic UX: room-side approach, exact Socket endpoint, opposite-side rejection, no Wall portal, and Door-only cross-room traversal. Do not add a Wall Outlet-specific router.
6. Wire only the unified source through the exact matching live Editor after code compiles. Preserve authored transforms, sprites, hierarchy, scale, sorting, collider sizes/offsets, and visual design; keep the large Head-root collider disabled and remove/disable Wall-Outlet-instance drag-only behavior without changing shared nested source assets.
7. Add focused official Wall Outlet EditMode coverage and keep all four existing Multitap tests passing. Broaden `verify-task.ps1` to run both suites by default and report explicit Console continuity. No scratch verifier or private-state fabrication is allowed.
8. Only after automated correctness, migrate every legacy scene instance one-for-one in `InfiniteMode.unity` through the exact matching Editor, preserving parent, sibling, transform, scale, name, count, and unrelated overrides. Save only that scene and preserve House allowance `8` / maximum `22` plus all unrelated existing changes.
9. Document the future `CreateWallOutlet(wall, position, socketCount)` Room Generation contract only; do not implement generation or Reward integration.
10. Stop at the seven focused Human Verification checks. Before Human Verification PASS: no DONE status, final commit, push, Task move, or completion claim.
