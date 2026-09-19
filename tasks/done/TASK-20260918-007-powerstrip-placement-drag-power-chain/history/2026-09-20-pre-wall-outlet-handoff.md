# Handoff — TASK-20260918-007

## Status

Stage: **Unified variable-count Wall Outlet continuation authorized and in implementation; HUMAN_VERIFY_REQUIRED** (2026-09-20). `Assets/03_Prefabs/Multitaps/Multitap.prefab` remains the persistent Multitap source, and `Assets/03_Prefabs/Wall_Outlets/Wall_Outlet.prefab` is now authorized as the single variable-count Wall Outlet production source. Runtime/shared code, official combined tests, exact-Editor prefab wiring, and automated verification must complete before the authorized one-for-one `InfiniteMode.unity` Wall Outlet migration. Legacy Wall Outlet prefab assets stay unchanged/read-only. Keep the Task active. Do not commit, push, move it to `tasks/done/`, implement Reward System/production Room Generation, or claim completion before Human Verification PASS.

## Unified Wall Outlet continuation checkpoint (2026-09-20)

- This work remains in TASK-007 because it extends the same persistent Socket-capacity architecture and Connection/Power gates and must preserve the existing Multitap regressions.
- Authorized runtime contract: persistent Socket01–05/Whole01–05, contiguous count 1–5 prefix, Whole01 First active, only the final active End active, increase-only count mutation, House-only electrical authority, and no runtime instantiate/destroy/replace.
- Authorized serialized integration: exact matching clean Editor only; unified `Wall_Outlet.prefab` wiring first, scene migration only after automated correctness. Preserve authored visual values and every unrelated scene change.
- Verification target now includes focused official Wall Outlet tests, all four Multitap tests, explicit Console continuity, zero new errors, legacy prefab zero-diff, and seven remaining Human Play checks.
- Current implementation result for this continuation: not yet compiled or integrated. Do not inherit prior PASS labels as evidence for the new Wall Outlet behavior.

## Current authoritative checkpoint (2026-09-20)

- **Persistent authored source:** `Assets/03_Prefabs/Multitaps/Multitap.prefab` contains persistent `Socket01`–`Socket05`, `Whole01`–`Whole05`, Cable/Plug, PowerInfo, and `PowerStripPowerInfoBinding`. Capacity changes activate/deactivate and reposition these authored objects in place. Runtime code does not replace, instantiate, or destroy Socket, Whole, Cable, Plug, PowerInfo, or binding identities.
- **Independent authorities:** `InitialSocketCount` initializes runtime `ActiveSocketCount`; `InitialAllowedPowerWatts` initializes runtime `AllowedPowerWatts`. Changing one never derives, resets, or mutates the other. `CableInfo.CableLength` remains separate authored per-Cable data. The roadmap names Cable Length strengthening as future Reward direction only; no CableLength upgrade state/setter or coupling was implemented here, and Head-drag-beyond-CableLength behavior remains pending a Human Decision.
- **Source matrix — PASS:** `1→2→3→4→5` passed sequential Whole/Socket activation, First/terminal-only End state, exact authored PowerInfo positions, persistent component/GameObject identity, collider composition, and footprint growth. Runtime Allowed Power stayed `7`. Same-count was a successful no-op; decrease was rejected with `SocketCountDecreaseUnsupported` without mutation.
- **Connected integration matrix — PASS:** `2→3` preserved PowerStrip, Socket01/02, PowerInfo/binding identities, both physical connections, Usage `5`, Powered state, and Flow; Socket03 became available; Allowed Power stayed `7`. Inactive Socket04 was excluded from pointer hit, magnetic acquisition, direct Connect, validation, source-live, usage, and cycle traversal. Forced blocked `3→4` expansion returned `PlacementUnavailable` with atomic rollback: old reservation retained, no prospective reservation leaked, and count/visual/PowerInfo/connections/Allowed/Usage/Powered/Flow unchanged. `3→2` returned `SocketCountDecreaseUnsupported` with no mutation.
- **Automated status:** `dotnet build Assembly-CSharp.csproj --no-restore` **PASS** with 0 warnings/0 errors; Unity `recompile` is `up_to_date`; `verify-unity.ps1` against the matching isolated Editor **PASS** (Unity 6000.3.9f1, ready, not compiling, Play stopped, `InfiniteMode` clean). Unity discovered zero tests: **NO_PROJECT_TESTS**, not PASS. `verify-fast.ps1` exits 1: **FAIL**, solely at `git diff --check` for Unity-generated trailing spaces in `Assets/03_Prefabs/Multitaps/Multitap.prefab` (`m_Name: ` and an empty override value). Do not claim `verify-fast` PASS and do not manually clean Unity prefab YAML.
- **Scene migration incomplete and excluded from checkpoint commit:** the working-tree diff of `Assets/00_Scenes/Demo/InfiniteMode.unity` contains an incomplete, unverified migration attempt: two legacy prefab instances are absent and only one new-source `Multitap` instance is present (`initialSocketCount=3`, `initialAllowedPowerWatts=3`), so instance count/name/transform/capacity preservation is not satisfied. The corrected Editor migration is prepared but has not been run to completion or read back. The scene must not be committed in this state. Resolving it requires an explicit decision to either restore only the incomplete migration portion while preserving legitimate House budget data, or authorize the correct two-for-two migration and saving this exact scene. Legacy prefab assets remain untouched.
- **Remaining real verification:** focused real-pointer Human Verification is still required; reflection/direct-method runtime probes are not pointer-input PASS. Wall Outlet single-prefab consolidation has not started and must remain untouched until the Multitap verification/migration scope is authorized and completed.

## ActiveSocketCount follow-up — supporting evidence (2026-09-19)

- The live source audit superseded earlier assumptions: the source is `Assets/03_Prefabs/Multitaps/Multitap.prefab`; Whole05/End was initially active; PowerInfo contains ten icons, not five.
- Added `PowerStripSocketLayout`: explicit persistent Socket/Whole/End mapping, module colliders, terminal-only End colliders, and exact authored PowerInfo positions. For prospective count N, hit/placement geometry uses active module colliders plus only N's terminal geometry. It consumes authored `BoxCollider2D` Size/Offset directly and never Renderer bounds.
- Finalized PowerInfo local positions are authored in the source prefab: count 1 `(-0.23, 0.5)`, count 2 `(-0.46, 0.5)`, count 3 `(-0.69, 0.5)`, count 4 `(-0.92, 0.5)`, count 5 `(-1.15, 0.5)`. This is no longer `HUMAN_SETUP_REQUIRED`; scale, sprite, layout, hierarchy, color, GameObject identity, and `PowerStripPowerInfoBinding` remain unchanged.

The earlier Power UI binding report and the Head/Plug drag-unification report are preserved below for historical verification context. Statements about the old uniform `10` placeholder and its 5-icon shortage describe the state at that time; `## Finalized Power Balance` supersedes them for current behavior.

## Wiring Investigation

Confirmed the actual, live Scene/Prefab wiring before writing any binding code — not just that code compiles:

- `/Managers/ConnectionDirector` in `InfiniteMode.unity` genuinely carries `PowerUiCoordinator` (alongside `HousePowerBudget`/`CableRoutingGridService`), and its serialized `housePowerIcons` list is 22 real `Image` components at `/Canvas/HUD_Area/UI_GameInfo/HousePowerInfo/Power` (Unity allows duplicate sibling names — all 22 are literally named "Power"). This is a real runtime instance already wired to real scene objects, not orphaned code.
- `houseUsedColorSource`/`houseUnusedColorSource` alias `housePowerIcons[0]`/`[1]` — this exact aliasing bug was already fixed in a prior round via one-time color caching (`CacheHouseColorsOnce`); reconfirmed still correct this round.
- Each `Multitap_{One..Five}.prefab`'s `PowerInfo` object had **no binding component at all** before this round — the Human Verification report's finding was accurate. `PowerValidationService.GetStripUsage(strip)` already matched the recursive downstream-load logic exactly; only the UI-side binding was missing.

## House Power UI Binding

- Extracted the shared icon-state logic into a new, presenter-only static class `Octoplug.Power.UI.PowerMeterPresenter.TryApply(usage, allowed, icons, usedColor, unusedColor, out error)`: exactly `allowed` icons active, `index < usage` → used color (Orange), remaining active icons → unused color (Grey), beyond `allowed` → inactive. Returns `false` with a precise error string if `usage`/`allowed` aren't whole numbers representable by the icon count, rather than silently clamping or partially rendering.
- `PowerUiCoordinator.RefreshHousePowerMeter()` now reads `PowerValidationService.GetHouseUsage()`/`HousePowerBudget.AllowedPowerWatts` and delegates to `PowerMeterPresenter.TryApply` — removed the old inline duplicated logic and its private `TryGetWholeIconCount` helper (now only in `PowerMeterPresenter`, shared by both meters). No new serialized reference, no visual/hierarchy change; the existing authored Orange/Grey colors (already cached once, from the prior round's alias fix) are reused as-is.
- No change to the existing coalesced-refresh mechanism (`GraphChanged`/`AnyConnectionRejected`/`HousePowerBudget.AllowanceChanged`/`PowerStrip.AllowanceChanged` → `QueueRefresh()` → one `StartCoroutine`-based end-of-frame refresh).

## PowerStrip Power UI Binding

- New `Octoplug.Power.UI.PowerStripPowerInfoBinding` (MonoBehaviour): resolves its own `PowerStrip strip` via `GetComponentInParent<PowerStrip>()` if unset; subscribes in `OnEnable()`/unsubscribes in `OnDisable()` to `PlugSocketConnection.GraphChanged` and `PowerStrip.AllowanceChanged` (filtered to `changedStrip == strip`); coalesces refresh via the identical `QueueRefresh()`/`RefreshAtEndOfFrame()` coroutine pattern already used by `PowerUiCoordinator`; caches its two color sources once; `Refresh()` sources Usage from `PowerValidationService.GetStripUsage(strip)` and Allowed from `strip.AllowedPowerWatts`, then calls the same shared `PowerMeterPresenter.TryApply`.
- Wired via the matching live Editor onto the single shared `Assets/03_Prefabs/Multitaps/PowerInfo.prefab` asset (`PrefabUtility.LoadPrefabContents`/`SaveAsPrefabAsset`) — since this prefab is nested identically under all 5 `Multitap_{One..Five}.prefab`s, wiring it once propagates the binding to all 5 Multitap instances automatically. Icon references = the 5 existing `Icon`/`Icon (1..4)` `Image` components; used/unused color sources = `Icon`/`Icon (1)`; `strip` left unset (auto-resolves at runtime). Confirmed live in the scene: both `Multitap_One/PowerInfo` and `Multitap_Two/PowerInfo` show the new component.
- **Exact shortage, reported precisely as requested, not clamped or partially shown:** every Multitap's `PowerInfo` has 5 icons against `AllowedPowerWatts = 10` — a shortage of exactly **5 icons per prefab** (`Multitap_One` through `Multitap_Five`, all identical). `PowerMeterPresenter.TryApply` returns `false` for this exact case (`allowedCount(10) > icons.Count(5)`), and `PowerStripPowerInfoBinding.Refresh()` logs: `"{name}: PowerStrip power meter cannot represent Usage={u} and Allowed={a} with {n} icons. Human Setup Required: this PowerInfo's icon pool must be resized to at least {a} icons to display {strip.name}'s allowance correctly — not clamped or partially shown."` Confirmed firing on a real, event-driven refresh (Console, not reflection) during verification: `"...cannot represent Usage=3 and Allowed=10 with 5 icons... Multitap_Two's allowance..."`.
- **No further code change will be needed once the pool is resized to 10** — `PowerMeterPresenter.TryApply` already handles any `allowed <= icons.Count` case correctly; only the icon count itself (a Human Setup / asset action, out of this round's scope) blocks correct display today.

## Power Graph Exception (user-approved, one line)

While building genuine event-driven verification for the nested-chain scenario the request itself asks for, found a real, pre-existing bug in `PowerValidationService.CollectDownstreamProducts` unrelated to anything built this round:

```csharp
// before:
var edgeIsBeingReplaced = socket != proposedTarget
    && (product == proposedProduct || childStrip == proposedStrip);
```

A plain query (`GetHouseUsage()`/`GetStripUsage()`) calls this with `proposedProduct`/`proposedStrip` both `null`. `product == proposedProduct` and `childStrip == proposedStrip` then degenerate to `null == null` — always `true` — so every genuinely-connected downstream product (or deeper nested strip) was silently treated as "the edge being replaced" and excluded. Result: `GetHouseUsage()`/`GetStripUsage()` always returned `0` for anything connected through a PowerStrip rather than directly to a Wall Outlet — which would have made this round's own required nested-chain verification impossible to pass, and was not a symptom of anything built this round.

Asked the user whether to apply a minimal one-line fix now or defer as a separate bug report. **User selected the minimal fix ("한 줄만 최소 수정 (권장)")**. Applied:

```csharp
// after:
var edgeIsBeingReplaced = socket != proposedTarget
    && ((proposedProduct != null && product == proposedProduct)
        || (proposedStrip != null && childStrip == proposedStrip));
```

Each half is now guarded by its own proposed reference actually being set. `TryValidate`/`TryValidateGraphConnection` — the two call sites that matter for a live connection attempt — always pass a real, non-null `proposedProduct` or `proposedStrip`, so their behavior is unchanged; only plain queries (which never did) now correctly include every real downstream edge. This is the only Power-graph change made this round.

## Scene/Prefab Source of Truth Compliance

- No hierarchy, layout, position, sprite, authored-color-asset, or scale change was made to any Scene or Prefab object.
- Allowed actions taken: two new binding components (`PowerMeterPresenter` is a plain static class, not a component; `PowerStripPowerInfoBinding` is the only new component type), reference wiring (icon lists, color sources, `strip`) on the existing `PowerInfo.prefab`, and runtime-only active-state/color changes driven by real data.
- `git diff --check`'s only flag on `PowerInfo.prefab` is Unity's own universal empty `m_Name: ` field, which every Unity Component serializes (confirmed identically present 9× elsewhere in the same file and 5× in an untouched `Multitap_One.prefab`) — not new whitespace hygiene introduced by this change, and not hand-edited per the no-hand-edit-Unity-YAML policy.

## Runtime Verification

Environment note first: this session's Play-mode frame loop was found stuck at `FrameCount: 1` despite real time advancing (up to 661s observed), which stalls the `StartCoroutine`-based refresh both meters use — `set_autotick` toggling alone did not resolve it; **exiting and re-entering Play mode after autotick was confirmed active did** (frame count then advanced normally, e.g. ~6500 frames over ~27s). This unblocked the genuine end-to-end checks below.

Per the explicit constraint — **"reflection으로 controller를 직접 호출한 것만으로 UI PASS라고 주장하지 마라"** — every check below drives only the real public API (`PlugSocketConnection.Connect`/`Disconnect`, the same calls a real drag-and-drop makes) and reads only the real Scene `Image` components externally. `PowerUiCoordinator`/`PowerStripPowerInfoBinding`'s own methods were never invoked directly in any of these:

- **House meter, direct connection** — PASS. Before: icon[0] Grey (`RGBA(0.525, 0.580, 0.545, 1)`). `Connect(TV.Plug, Wall_Outlet_One.Socket)` → polled the real icon externally → turned Orange (`RGBA(0.961, 0.416, 0.149, 1)`). `Disconnect` → reverted to the exact original Grey.
- **House meter, nested PowerStrip chain** — PASS. `Connect(Multitap_Two.Cable.Plug, Wall_Outlet_One.Socket)` then `Connect(TV.Plug, Multitap_Two.Sockets[0])` (Wall → Strip → Product, two hops) → `GetHouseUsage()` went `0 → 3`, and the same real icon[0] followed Grey → Orange; disconnecting both restored `GetHouseUsage() == 0` and the icon to Grey.
- **PowerStrip meter Human-Setup-Required error, real event path** — PASS. During the nested-chain test above, the real Console captured (event-driven, not reflection-invoked): `"PowerInfo: PowerStrip power meter cannot represent Usage=3 and Allowed=10 with 5 icons. Human Setup Required: ...Multitap_Two's allowance..."`.
- **0 products → Usage 0**: confirmed as the verified baseline/restored state in every check above (`GetHouseUsage() == 0`, icon Grey) — no separate isolated check needed since every test above starts and ends there.
- **3W → first 3 icons Orange**: the House meter checks above use TV (confirmed `PowerConsumptionWatts = 3`), so "connected → Orange" in those checks is exactly the 3W case; only one product-color-icon boundary was directly inspected (icon[0]), not all three, since the automated check reads a representative icon rather than re-deriving `PowerMeterPresenter`'s already-covered per-icon loop.
- **1W → first icon Orange**: **not exercised literally** — only TV (3W) currently exists as a placed `ApplianceSource` in the demo scene; no product was added or scene-placed to manufacture a 1W case, since Scene/Prefab placement changes are out of scope this round. The underlying logic (`PowerMeterPresenter.TryApply`) is generic over any whole-number Usage and was already confirmed correct for 0 and 3; a literal 1W scenario needs Human Verification with a real 1W product placed by the user, or a future round if a 1W product/scene state is authored.
- **Disconnect → Grey restored**: confirmed in both the direct and nested checks above.
- Scene confirmed restored to a fully clean baseline after all tests: `TV.Plug.IsConnected=False`, `Multitap_One`/`Multitap_Two` Cable-Plug and Socket `IsConnected=False`, `HouseUsage=0`.

## Automated Verification

- `dotnet build Assembly-CSharp.csproj --no-restore`: PASS, 0 warnings, 0 errors.
- Unity `recompile`: `up_to_date`, no compilation failure.
- Genuine end-to-end Play-mode verification (see `## Runtime Verification` above) — the specific technique required this round (real public API + external `Image` readback, no reflection into the coordinator/binding) — all PASS.
- `pwsh -File scripts/verify-fast.ps1`: PASS (context-budget line-count warnings on task docs, pre-existing; line-ending conversion notices only).
- `git diff --check`: one flag, Unity's own universal empty `m_Name: ` component field on the newly added `PowerStripPowerInfoBinding` entry in `PowerInfo.prefab` — confirmed identical to Unity's serialization of every other component in the same file and in untouched prefabs; not new hygiene debt, not hand-edited.
- Unity project tests: zero discovered — **NO_PROJECT_TESTS**, not PASS.
- `AgentScripts/` cleaned up (all one-off debug/verify scripts deleted) before finishing.
- Console ground truth after full verification: 0 errors from anything other than the expected, real, event-driven PowerStrip Human-Setup-Required log itself.

## Human Verification

Use project `D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration`, scene `Assets/00_Scenes/Demo/InfiniteMode.unity`. Only these 3, per the request:

1. When you connect or disconnect a Product's Plug (directly to a Wall Outlet, or through a PowerStrip), does the House lightning-icon meter update immediately?
2. When multiple Products are connected, does the Orange fill grow from the left (first icons Orange, matching total Usage)?
3. When you disconnect, does the affected icon(s) revert to Grey?

At the time of this Human Verification ask, PowerStrip's own `PowerInfo` meter was explicitly excluded because the then-current placeholder Allowed=10 exceeded its 5-icon pool. That historical blocker is superseded by `## Finalized Power Balance`: all finalized initial allowances (`3/3/4/5/5`) now fit and display; expansion to 10 icons is required only before future upgraded values `6–10`.

Regression reminder (already PASS, not being re-asked, and not touched this round): Head grab reliability/smoothness, Prefab-Inspector collider tuning, corrected initial Plug position, Wall Socket magnetic acquisition, cable flip/spike-free rendering, Self/Circular/Chain rejection.

## Follow-up Fix — PowerStrip Log Spam (2026-09-19)

User's Human Verification on the House meter: **PASS on all 3 questions** (icons show exactly the Allowed range; multiple Products fill Orange from the left; disconnect reverts to Grey).

User also reported: "멀티탭의 전력 표시는 에러가 생겨서 아무것도 안돼" (the multitap's power display errors out and does nothing). Investigated live in the Editor before assuming it was a new bug:

- Diagnosed both placed instances (`Multitap_One`, `Multitap_Two`) directly: `PowerStripPowerInfoBinding` is correctly wired on both (`strip`/5 `icons`/2 color sources all resolve, no null references), and invoking `Refresh()` directly threw **no exception**. The "nothing happens" visually is the exact, already-reported Human Setup Required limitation — `PowerMeterPresenter.TryApply` correctly refuses to touch any icon when the pool (5) can't represent Allowed (10), by design (never clamp/partially show).
- Found the actual defect behind "에러가 생겨서" (an error occurs): the Human Setup Required error re-logged on **every** graph change anywhere in the house (not scoped to that specific strip), so a handful of connect/disconnect actions could flood the console with repeated identical errors for both Multitaps — easily read as "everything is broken."
- **Fixed** (pure code change inside the already-Exclusive `PowerStripPowerInfoBinding.cs`, no Scene/Prefab/visual change): the error is now logged once per distinct `(icon pool size, AllowedPowerWatts)` pair and stays silent on repeat occurrences of the same unresolved shortage; it will log again only if the underlying mismatch actually changes (e.g., `AllowedPowerWatts` changes) or clears (e.g., the pool is resized) and later regresses.
- Verified via a live 3-cycle connect/disconnect test (6 graph-change events × 2 placed strips = up to 12 potential duplicate log calls): console error count stayed flat (no new entries) after the fix, versus one guaranteed entry per event before it. Confirmed via direct diagnostic that wiring/no-exception status is unchanged.
- `dotnet build`/Unity `recompile` clean (0 warnings/errors). Scene confirmed restored to a disconnected baseline (`HouseUsage=0`) afterward.
- **This does not resolve the underlying Human Setup Required limitation** — the PowerStrip meter still shows nothing until its icon pool is resized from 5 to 10; that remains a Scene/Prefab asset action for the user, unchanged from the original report. The fix only removes the misleading repeated-error appearance.

## Power Capacity Data-Structure Audit (2026-09-19, no code/prefab change)

Requested before proceeding with any PowerStrip PowerInfo Human Setup: verify House and PowerStrip capacity are genuinely independent data, not assumed. Read-only investigation, zero files changed this round.

**1. House**
- `HousePowerBudget.AllowedPowerWatts` (`Assets/01_Scripts/Power/HousePowerBudget.cs`): a plain `[SerializeField] private float allowedPowerWatts`, no default value in code, exposed only via the read-only `AllowedPowerWatts` property. Lives on exactly one scene object, `/Managers/ConnectionDirector`. Confirmed live value: **`10`**.
- `PowerValidationService.GetHouseUsage()`: recomputes from scratch on every call (no caching) — walks every `ApplianceSource` and top-level `PowerStrip` actually connected to a Wall-side Socket (i.e. not through another strip) and sums `PowerConsumptionWatts`. It reads no `PowerStrip.AllowedPowerWatts` value at all; Allowed is only ever compared against Usage inside `TryValidate`, using `house.AllowedPowerWatts` — the one specific `HousePowerBudget` instance passed in by the caller (`PowerUiCoordinator` always passes its own serialized `houseBudget` reference).

**2. PowerStrip**
- `PowerStrip.AllowedPowerWatts` (`Assets/01_Scripts/Power/PowerStrip.cs`): a plain `[SerializeField] private float allowedPowerWatts` — a **completely separate field on a completely separate class**, no default value in code, no reference to `HousePowerBudget` anywhere in the file. Exposed only via its own read-only `AllowedPowerWatts` property, and only ever changed through `SetAllowedPowerWatts(value)` (which raises the strip's own `AllowanceChanged` event, checked against `Mathf.Approximately` so it's a no-op if unchanged).
- `PowerValidationService.GetStripUsage(strip)`: recomputes from scratch per call, for that **specific strip instance only** — walks only `strip.Sockets`, recursing into any nested downstream strip, summing connected `ApplianceSource.PowerConsumptionWatts`. Never touches House's usage/allowance, never touches another strip's data. Verified this is not merely code-shaped-right but actually independent at runtime: `GetStripUsage(Multitap_One)`/`GetStripUsage(Multitap_Two)` and `GetHouseUsage()` were all separately exercised in the prior round's runtime verification and produced consistent, non-interfering results (e.g., a nested Wall→Strip→Product chain moved `GetHouseUsage()` 0→3 while the strip's own usage separately tracked the same 3W for that branch — the two numbers happened to match here only because there was exactly one product in the chain, not because they are the same value).

**3. The "all five = 10" finding — verified per-prefab, not assumed**

Inspected each of the 5 `Multitap_{One..Five}.prefab` ASSET files directly (not the live scene, not cached data) via `SerializedObject`/`SerializedProperty` on the `PowerStrip` component — the same raw path Unity itself deserializes, bypassing any code-level default:

| Prefab | `AllowedPowerWatts` (raw serialized) | `Sockets.Count` |
|---|---|---|
| `Multitap_One.prefab` | `10` | 1 |
| `Multitap_Two.prefab` | `10` | 2 |
| `Multitap_Three.prefab` | `10` | 3 |
| `Multitap_Four.prefab` | `10` | 4 |
| `Multitap_Five.prefab` | `10` | 5 |

Each is its own independent prefab asset with its own independently-serialized `PowerStrip` component data (confirmed distinct by their differing, correctly-scaled `Sockets.Count` per size) — not a shared/linked/inherited value, not a Prefab Variant chain, not a single base asset five prefabs point to.

**Answering the three options precisely, per the actual project history (`meta.md`'s own "Human Setup Check" section from this Task's original implementation):**

- **Not (B):** confirmed by direct source read — no `10` (or any numeric constant) appears anywhere in `PowerStrip.cs`, `HousePowerBudget.cs`, or `PowerValidationService.cs`. Not hardcoded in code.
- **Not (C) as stated:** it was never adopted as a finalized design maximum. It is explicitly recorded in this Task's own `meta.md` (Human Setup Check, 2026-09-18) as: *"`PowerStrip.allowedPowerWatts = 0` on all 5 prefabs... A `0` budget cannot be demonstrated... Set uniformly to `10` (matching `HousePowerBudget`'s own placeholder magnitude) on all 5 prefabs — explicitly a placeholder, not a balance decision."* This is also still listed under `## Open Decisions` today, unchanged.
- **Closest to (A), precisely stated:** it is the prefabs' actual current Inspector-set value — but not an accident/side-effect. It was a deliberate placeholder choice made in this Task's very first implementation round (to make budget-rejection testable at all, since `0` rejects every connection), uniformly set to the same *magnitude* House happened to use, for demo convenience — **not** because the two are linked in code or design. The two `10`s are numerically equal today by coincidence of that placeholder choice, not by any structural dependency; either can be changed independently at any time with zero effect on the other, and nothing in the codebase would need to change if they diverged.

**4. UI binding read path — already future-proof, no change needed**

- `PowerUiCoordinator.RefreshHousePowerMeter()` reads `houseBudget.AllowedPowerWatts` — its own serialized `HousePowerBudget` reference — fresh on every refresh, never a cached/hardcoded number.
- `PowerStripPowerInfoBinding.Refresh()` reads `strip.AllowedPowerWatts` — that specific `PowerInfo`'s own resolved `PowerStrip` (via `GetComponentInParent`, one per Multitap instance) — fresh on every refresh, never a cached/hardcoded number, never House's value.
- Both `PowerStrip.SetAllowedPowerWatts`/`HousePowerBudget.SetAllowedPowerWatts` raise their own `AllowanceChanged` event on real change, which each binding already subscribes to (`PowerStripPowerInfoBinding.OnStripAllowanceChanged` is filtered to `changedStrip == strip` — its own instance only). **This already satisfies "future PowerStrip마다 허용 전력이 다르거나 업그레이드로 변할 가능성"** with zero further code change: if any single strip's `AllowedPowerWatts` changes at runtime (a balance tweak, an upgrade), only that strip's own meter refreshes with the new value — House and every other strip are unaffected.
- No icon-count-driven `AllowedPowerWatts` mutation exists anywhere, and none was added — `PowerMeterPresenter.TryApply` only ever reads `allowed`, never writes it, and refuses (rather than clamping) when the pool can't represent it.

**No prefab or icon was modified this round.** This audit is read-only; Human Setup Required sizing for the PowerStrip PowerInfo icon pool (and any future decision to give different Multitaps different `AllowedPowerWatts`) remains entirely the user's call, now with confirmed-accurate underlying data to decide from.

## Finalized Power Balance (2026-09-19)

The placeholder `10` values audited above have now been replaced by the human-finalized balance matrix. This pass changes balance data only; no Reward System, upgrade action, socket upgrade, or automatic stat coupling was added.

| Authority | Initial/current Allowed | Maximum | UI capacity now |
|---|---:|---:|---:|
| House (`InfiniteMode/ConnectionDirector`) | 8 | 22 | 22 |
| `Multitap_One.prefab` | 3 | 10 | 5 |
| `Multitap_Two.prefab` | 3 | 10 | 5 |
| `Multitap_Three.prefab` | 4 | 10 | 5 |
| `Multitap_Four.prefab` | 5 | 10 | 5 |
| `Multitap_Five.prefab` | 5 | 10 | 5 |

- House current/max values are owned by `HousePowerBudget`; each strip current/max values are owned by that specific `PowerStrip`. They remain mutually independent.
- `PowerStrip.sockets` remains a separate list. The serialized socket counts are unchanged at 1/2/3/4/5 and are not used to derive Allowed Power.
- Maximums are represented as serialized read-only balance data (`HousePowerBudget.MaxAllowedPowerWatts`, `PowerStrip.MaxAllowedPowerWatts`) so the finalized facts exist in runtime data without prematurely implementing reward/upgrade mutation.
- The House meter already has the required maximum capacity of 22 icons. The shared PowerStrip meter still has 5 icons; this is sufficient for all finalized initial values, but a future UI-capacity setup must expand it to 10 before upgrades 6–10 can be displayed truthfully.
- Direct display verification (CLI frame loop remained frozen, so direct `Refresh()` invocation was used only to bypass its stalled coroutine): House Usage=3/Allowed=8 showed icons 0–2 Orange, 3–7 Grey, and 8–21 inactive; `Multitap_One` Usage=3/Allowed=3 showed icons 0–2 Orange and 3–4 inactive; disconnect restored Grey. The underlying graph was changed only through the public `PlugSocketConnection.Connect`/`Disconnect` APIs. Genuine event-driven binding behavior was already established in the previous round and is not re-claimed from this direct invocation.
- Continuation limitation: the serialized current values are present on disk as House `8` and strips `3/3/4/5/5`, and the maximum fields are serialized C# defaults (`22`/`10`). An explicit live-Editor write/readback of the newly added maximum fields, followed by final Unity recompile/build/harness checks, remains pending because every relevant command was rejected before execution by the command-safety classifier outage (`claude-sonnet-5 is temporarily unavailable`). This handoff does not claim those pending checks as PASS.

## Task Status

`active` / `HUMAN_VERIFY_REQUIRED`. House Power UI Human Verification remains **PASS (3/3)**. The current single-prefab source/runtime matrices are **PASS**; build is **PASS** (0 warnings/errors); Unity recompile is `up_to_date`; matching-Editor `verify-unity` is **PASS**; zero Unity tests means **NO_PROJECT_TESTS**; `verify-fast` is **FAIL** only at Unity-generated trailing whitespace in `Multitap.prefab` and must not be reported as PASS or fixed by hand-editing YAML. Finalized initial PowerStrip values fit the current PowerInfo capacity; Reward System and CableLength strengthening remain unimplemented direction, not completed functionality. Next actions are: (1) perform focused real-pointer Human Verification, (2) obtain explicit authorization before replacing legacy InfiniteMode instances and saving `Assets/00_Scenes/Demo/InfiniteMode.unity`, (3) only then run/read back that migration, and (4) keep Wall Outlet single-prefab work unstarted. No commit, push, DONE move, migration, Wall Outlet work, or next-feature work has been performed.

---

# Previous round — Head/Plug drag unification (for reference)

Stage: **Head/Plug drag unification complete; HUMAN_VERIFY_REQUIRED** (2026-09-19). Scope that pass was Interaction/Drag UX only — Power UI, Cable Grid size, Cable renderer, and Power graph were unchanged and untouched.

## Interaction Collider Structure

- `HeadDragInput` is rewritten to be structurally identical to `PlugDragInput`: an explicit, Inspector-sized `Collider2D` (`hitCollider`) is the **sole** source of truth for "what did the pointer hit" (`hitCollider.OverlapPoint(worldPos)`). The Renderer-bounds-plus-padding computation (`bodyRenderer`, `hitPadding`, `TryGetHitBounds`) is removed from the class entirely — not bypassed, not defaulted to zero, gone — so there is no runtime bounds math left to fight with.
- This collider is deliberately separate from two other things it must never be confused with, per the request:
  - **Placement footprint / Grid occupancy**: `PlacementFootprint`'s own `boundsCollider` (the trigger `BoxCollider2D` added on each Multitap root in the prior pass, sized to the alpha-trimmed opaque Body extent) is a completely different component reference, used only for Room/Wall/Product/PowerStrip legality — never consulted for hit-testing.
  - **Cable/Grid occupancy**: unrelated to either of the above; untouched this pass.
- `PlugDragInput` and `ProductClickInput` already used exactly this explicit-Collider2D pattern before this pass (both have a `hitCollider` field editable in the Inspector) — no change was needed there. Head now matches them.
- Each Multitap's Head already carried a `BoxCollider2D` (previously present but functionally decorative — sized to the Body sprite's full bounds but never read for hit-testing). It is now the live interaction hit area. Enlarged by a flat `+0.5` world units per side on all five prefabs as a sensible starting default (final sizes: `One` 2.672×2.672, `Two`/`Three` 3.090×3.090, `Four` 3.508×3.508, `Five` 3.926×3.926, all centered, offset 0,0) — and, per the request, fully Inspector-adjustable (Size/Offset) from here on with no code involvement required to retune it.

## State-Based Eligibility

Cleanly separated per the request's own framing:

- **"What did you click?" → Collider.** `HeadDragInput.ContainsPoint`/`CanStartDragAt` answer this from the explicit `hitCollider` alone.
- **"Can you grab it right now?" → State**, and this is decided **exactly once**, at Pointer Down:
  - `HeadDragInput.CanStartDragAt` = `ContainsPoint(worldPos) && IsOwnerDisconnected()` — evaluated by `PointerInteractionResolver.ResolveHead` only during that single frame's `BeginPointerDown` resolution.
  - `PowerStripHeadController.OnDragStarted` separately (belt-and-suspenders, unchanged relationship to the above) evaluates its own `IsFullyDisconnected()` once, caching the result in `dragAllowed`.
- **Neither check is ever re-run for the rest of an active drag.** The previous implementation re-ran `IsOwnerDisconnected()` inside `HeadDragInput.Update()`'s dragging branch on every frame the mouse was held, and silently released capture (`Release`+`DragEnded`) the instant it returned false. That per-frame re-check is now gone; `dragAllowed`/capture are read-only for the remainder of the drag.
- A fully-disconnected strip's Head captures normally; a connected strip's Head is simply never eligible in the first place (`CanStartDragAt` returns false at Pointer Down, so `PointerInteractionResolver` never selects it) — silent, no Alert, exactly as before.
- Plug eligibility rules are completely unchanged.

## Plug vs. Head Drag Hot Path

Directly compared and reconciled — Head's per-frame work is now the same *shape* of cost as Plug's:

| | **Plug** (`CableRoutingController.OnDragged`, unchanged) | **Head**, before this pass | **Head**, after this pass |
|---|---|---|---|
| Hit-test re-evaluated while dragging? | No — capture held from Pointer Down. | No, but **eligibility** (`IsOwnerDisconnected`) was re-checked every frame, with a silent release on failure. | No — nothing re-checked; capture and `dragAllowed` are both decided once. |
| Position update | Direct: raw pointer clamped to the nearest walkable cell/Socket-candidate search. | Every frame: half-Grid-cell **substep loop**, testing 3 candidates (combined/X/Y) via `PlacementFootprint.CanReserveAt` **each substep** — for a large pointer delta this could be dozens of substeps × 3 footprint-cell scans (tens of cells each) in one frame. | Direct: `pointer + grabOffset`, one cheap `Mathf.Clamp` against cached Room bounds. No Grid/footprint call at all while dragging. |
| Placement/overlap validation timing | N/A (Plug has no placement footprint). | Continuously, every rendered frame (by design, last pass). | Deferred entirely to Pointer Up. |
| Cable re-route per frame | `GridPathfinder.TryFindPath` (A*) — real work, same as Head. | Same A* call via `RecomputePathFromCurrentOrigin`. | Unchanged — same call, same cost, now the **only** non-trivial per-frame work, matching Plug's own profile. |

The substep+per-frame-`CanReserveAt` design from the previous pass is exactly what was making Head feel "버벅임" relative to Plug — it was doing meaningfully more work per dragged frame than Plug ever did. Removing it (deferring full legality to release, as the request specifies) brings Head's hot path down to the same order of cost as Plug's.

## Capture Lifetime

Investigated every item on the request's checklist:

- **`PointerInteractionDriver` release** — only clears capture on a genuine `wasReleasedThisFrame` (`LateUpdate`). Unaffected, not a cause.
- **Mouse button state** — read consistently (`Mouse.current`); no change, not a cause.
- **Eligibility re-evaluated during drag** — **found and fixed**: `HeadDragInput.Update()`'s dragging branch called `IsOwnerDisconnected()` every frame and released capture the instant it returned false. This is architecturally wrong under the new "decide once, hold to Pointer Up" contract and is the only actual capture-drop path found in the code. Removed.
- **Collider-exit release** — never existed; once dragging, `HeadDragInput` does not re-test `ContainsPoint` at all (matching Plug, which never did either).
- **Grid/placement failure releasing capture** — `PowerStripHeadController.CancelPreview`/the old per-frame `CanReserveAt` path never touched `HeadDragInput`'s own capture state (separate components) — ruled out, and moot now that placement is only checked once, at release, after capture has already ended.
- **Root movement invalidating the collider reference** — the Head `Collider2D` moves with the root (same rigid unit) but the reference itself never becomes null or re-resolves mid-drag; ruled out.
- **New rule, enforced by construction:** a successful Pointer-Down capture is held unconditionally until an explicit Pointer Up (`wasReleasedThisFrame`) or component disable. Nothing else clears it.

## Initial Plug Correction

- `CableRoutingController.ValidateInitialPlugPosition` now corrects a **loose** (not authored-connected) Plug whose initial world position lands on a non-walkable Grid cell: it searches for the nearest cell that is both walkable **and** actually path-reachable from Origin within this Cable's own length (reusing `GridPathfinder.TryFindNearestValidDropCell` — the same Door-only, no-wall-crossing reachability guarantee already used for a live Plug drop), and moves only the runtime `Plug` Transform there. Falls back to Origin's own position (always valid) if no such cell exists.
- An authored-**connected** Plug is left untouched by this path — it's handled by the existing `ValidateInitialConnection` Approach-Point routing instead, since its resting position is meaningful (it sits at its Socket).
- Constraints honored: only out-of-room Plugs are touched; a normally-placed Plug is read but never moved; the search is reachability-based so it cannot cross a Wall into a different Room; the Product/PowerStrip root and the authored Prefab Transform are never written to disk or otherwise mutated — this is a pure runtime correction, re-applied fresh every Play session.
- Diagnostic: a `Debug.LogWarning` names the corrected Cable's GameObject and both the fact of correction and the resulting position. `Multitap_One`'s previously-documented out-of-grid authored Plug (`(3.25, -3.42)`, several sessions old) is now corrected automatically at Start to `(3.38, -2.63)` (a genuinely walkable, Origin-reachable cell) and its Cable now renders instead of staying empty.

## Automated Verification

- `dotnet build Assembly-CSharp.csproj --no-restore`: PASS, 0 warnings, 0 errors.
- Unity `recompile`/`recompile_status`: PASS.
- 7-check Play-mode logic/runtime-integration harness (reflection-driven direct calls to the private drag-event handlers — a Logic/Runtime Integration check per `runtime-interaction-validation.md`, not a substitute for real Human pointer input), fresh Play session, **7/7 PASS**:
  1. Collider is the sole hit source (`bodyRenderer`/`hitPadding` fields verifiably absent from the compiled type; inside/outside the collider both resolve correctly).
  2. Direct pointer-follow is never blocked mid-drag, even with the destination cells occupied by a fake owner (proves the per-frame footprint check is gone).
  3. Room-bounds clamp holds both the low and high extreme when dragged far outside the room.
  4. An invalid release drop is never committed to the invalid spot, and the actual landing position is confirmed grid-valid after release.
  5. A valid release drop commits at (or immediately snaps near) the requested point.
  6. No per-frame eligibility re-derivation observed across a dragged frame.
  7. `Multitap_One`'s Plug is now on a walkable cell after the new Start-time correction.
- Regression sweep (2/2 PASS): Self-connection still rejected with `ConnectionFailureReason.SelfConnection`; Cable Base/Flow still have `numCornerVertices == 4` and sorting `5`/`6` (both untouched by this pass, confirmed unaffected).
- **Bug found and fixed during this pass**: `Physics2D.autoSyncTransforms` is `0` (disabled) project-wide (`ProjectSettings/DynamicsManager.asset`, `ProjectSettings/Physics2DSettings.asset`) — `PlacementFootprint`'s `Collider2D.bounds` could still report the pre-drag position immediately after `OnDragged`'s plain `transform.position` write, since nothing re-syncs Physics2D on a bare Transform assignment. `OnDragEnded` now calls `Physics2D.SyncTransforms()` once before validating; without it, release-time validation could silently check the wrong (stale) location. This was caught by the automated harness itself, not left for Human Verification to discover.
- Console ground truth after the full harness + regression sweep: **0 errors**, 1 expected diagnostic warning (the new `Multitap_One` initial-Plug-correction notice from `## Initial Plug Correction`).
- `pwsh -File scripts/verify-fast.ps1`: PASS (context-budget line-count warnings on task docs, pre-existing line-ending notices only).
- `git diff --check`: no whitespace errors; line-ending conversion notices only (pre-existing).
- Unity project tests: zero discovered — **NO_PROJECT_TESTS**, not PASS.
- Real Game-view pointer feel (grab reliability, drag smoothness parity with Plug, no sudden jump) is not overstated as automated PASS.

## Human Verification

Use project `D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration`, scene `Assets/00_Scenes/Demo/InfiniteMode.unity`. Only these 4, per the request:

1. Pressing on a disconnected PowerStrip's Head at several different points on its visible Body — does it grab on the first attempt, most of the time?
2. Once grabbed, does the Head move as smoothly as a Plug drag, with no mid-drag stutter, sudden jump, or unexpected capture release?
3. In the Prefab Inspector, can you directly adjust the Size/Offset of the Head / Plug / Product click colliders yourself?
4. On starting Play, is a Plug that was previously outside the Room now corrected to a valid in-Room position, so it no longer looks abnormally detached from its Head/Product?

Regression reminder (already PASS, not being re-asked, but should not have silently broken): Wall Socket magnetic acquisition, cable flip/spike-free rendering, Self/Circular/Chain rejection, House Power UI, PowerStrip PowerInfo state, Power graph behavior — none of these were touched this pass.

## Task Status

`active` / `HUMAN_VERIFY_REQUIRED`. The Editor is out of Play Mode. No commit, push, DONE move, or next-feature work has been performed.

## Minimal Unity Verification Foundation (authorized 2026-09-20)

- Exact scope: one reusable Editor-test fixture, one minimal EditMode assembly with four representative Multitap regressions, one `scripts/verify-task.ps1` entry point, Console baseline/delta tracking without clearing, and concise command documentation.
- Production state must be established through authored prefab values and public production APIs. Private serialized production fields may not be overwritten for the normal verification path.
- The wrapper contract is build → matching-Editor compile/readiness → EditMode discovery/run → Console delta → `verify-fast`, followed by the fixed summary labels and a nonzero exit code for required failure or unreliable Console tracking.
- Wall Outlet is supported only as a future fixture instantiation path. Reward System, Wall Outlet behavior/tests, CI, broad harness restructuring, scene/prefab saving, commit, push, and DONE remain out of scope.
- Final results and reuse details will be recorded here after the one-command run. TASK-007 remains `active` / `HUMAN_VERIFY_REQUIRED`; the three real Inspector-button scenarios remain pending.

## Runtime Upgrade Controls Pass (authorized 2026-09-20)

- Exact scope: runtime-only Active Socket Count +1, Allowed Power +1, and Cable Length +1 production APIs, plus a removable Editor-only Play Mode Inspector that invokes those APIs.
- Baseline/workspace: `5b6b1a0` in `D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration`.
- Preserve without mutation: pre-existing `.vscode/settings.json`, incomplete `Assets/00_Scenes/Demo/InfiniteMode.unity` migration, and root `TODO.md`.
- `CableInfo.cableLength` is the authoritative Cable Length state. Socket Count and Allowed Power remain authoritative on `PowerStrip`; all three are independent.
- Debug controls may not overwrite serialized/private fields, use reflection, directly toggle visuals, replace prefabs, dirty/save assets, or add production gameplay UI.
- Reward System, persistence, scene migration, Wall Outlet work, and Head-drag-beyond-Cable-Length behavior remain out of scope.
- Implementation and verification results follow. TASK-007 remains `active` / `HUMAN_VERIFY_REQUIRED` and no commit/push/DONE is authorized.

## Runtime Upgrade Controls

- Added a removable Editor-only custom Inspector at `Assets/Editor/Power/PowerStripEditor.cs`.
- It displays authoritative runtime Active Socket Count, Allowed Power, and Cable Length, and exposes exactly three Play Mode-only buttons.
- Each button calls its matching public production API. The Inspector does not write serialized/private runtime fields, use reflection, toggle authored visuals directly, dirty/save assets, or add production gameplay UI.
- Runtime upgrades last only for the current Play session. Reward selection/generation, reward probabilities, persistence, and production upgrade UI were not implemented.

## Socket Upgrade

- `PowerStrip.TryUpgradeActiveSocketCount(out PowerStripSocketCountFailure)` increments by exactly one and delegates to the existing private transactional count mutation.
- The transaction validates the complete prospective owner-aware footprint before replacing this strip's reservations, then commits count/layout and publishes topology events.
- Authored `PowerStrip`, Cable, Plug, Socket01-05, PowerInfo, and binding identities are retained. Existing Socket01/02 connections are not replaced by a `2 → 3` upgrade; only Socket03 becomes newly usable.
- A blocked prospective `3 → 4` expansion returned `PlacementUnavailable`, retained count/event state, and preserved every previous footprint claim.

## Allowed Power Upgrade

- `PowerStrip.TryUpgradeAllowedPowerWatts(out PowerStripUpgradeFailure)` increments runtime allowance by exactly one, changes no other authority, and emits `AllowanceChanged` once on success.
- The effective maximum is `min(MaxAllowedPowerWatts, 10)`. A call at 10 returns `MaximumReached` with no mutation or event.
- `PowerValidationService` continues to read `PowerStrip.AllowedPowerWatts`, so future validation sees the increased allowance immediately. House Power remains independent.
- `PowerStripPowerInfoBinding` refreshes from the same authority events. Matching-Editor readback of `Assets/03_Prefabs/Multitaps/PowerInfo.prefab` found ten authored `Image`s, ten serialized binding entries, and zero null references; no prefab repair/save was needed.

## Cable Length Upgrade

- **Authoritative owner:** `CableInfo`, specifically its existing serialized `cableLength` state.
- `CableInfo.TryUpgradeCableLength(out CableLengthUpgradeFailure)` increments that authority by exactly one and emits `CableLengthChanged` once after success.
- `CableRoutingController` invalidates its loose-route cache on that event. During an active Plug drag it reruns the existing length-aware preview from the last pointer position; while idle or connected it does not move or disconnect the Plug solely because length increased.
- `ProductTooltipController` refreshes the visible matching Product cable label from the same event and unsubscribes when hidden/replaced/disabled.
- The normal drag verifier proved that a pointer outside length 3 but inside length 4 became reachable immediately, the preview extended beyond the old boundary, and the old cached path was replaced: `PASS|checks=9`.

## State Independence

- A matching-Editor ephemeral verifier instantiated the unified source prefab at authored `1 / 3 / 3`, used only production +1 APIs to establish `2 / 5 / 3`, then verified:
  - Socket +1: `3 / 5 / 3`
  - Allowed Power +1: `3 / 6 / 3`
  - Cable Length +1: `3 / 6 / 4`
- That production-path matrix, identity checks, one-event-per-authority checks, allowance cap, and transactional rollback passed: `PASS|checks=91`.
- Reflection in that verifier was limited to isolated grid-service infrastructure and invoking initialization for the in-memory fixture; it did not overwrite PowerStrip/Cable serialized state or fake successful upgrade transitions. The previous verifier's private-field baseline setup was superseded.
- Synthetic invalid/non-finite-state rejection had previously been checked through explicit malformed-state injection. That boundary check is recorded separately and is not represented as a production-path upgrade.

## Connection Preservation

- The committed single-prefab foundation already verified a real Product A/B `2 → 3` connection-preservation regression: persistent strip/socket/PowerInfo identities, both physical connections, Usage, Powered, Flow, Allowed Power, and old footprint claims remained intact while Socket03 became available.
- The current pass did not reconstruct that graph through the real Inspector; it preserved the same production transaction and reran production-path identity/rollback/state-independence coverage. The real button-driven connected scenario remains Human Verification rather than an automated PASS.

## Automated Verification

- Production-path upgrade verifier: `PASS|checks=91`.
- Active-drag Cable reach/cache verifier: `PASS|checks=9`.
- C# solution build: PASS, 0 warnings / 0 errors.
- Matching Unity Editor recompile: clean/up-to-date; `compilationFailed: false`.
- `pwsh -File scripts/verify-fast.ps1`: PASS with context-budget/line-ending warnings only.
- `git diff --check`: no whitespace errors; line-ending conversion notices only.
- `pwsh -File scripts/verify-unity.ps1 -ProjectPath D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration`: PASS.
- Unity Test Framework now discovers exactly four focused EditMode tests in `Assembly-CSharp-Editor`. The first official run invoked all four but failed `0/4` during fixture setup before business assertions because `CableRoutingGridService.Awake` is not automatic in EditMode and `grid` remained null. The fixture now invokes that real lifecycle method through `SendMessage` without overwriting private serialized production state; a corrected official result is still pending.
- Authoritative foundation command: `pwsh -File scripts/verify-task.ps1 -ProjectPath D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration`. It provides the exact-worktree build/compile/discovery/async-test/Console-delta/verify-fast entry point and always prints the stable summary labels. Its bounded smoke run reported Build PASS and verify-fast PASS, but Unity Compile/Tests FAIL and Console delta UNRELIABLE because the prior synchronous test request left the Pipeline execution gate unresponsive; no cancellation/restart was performed.
- Console audit preserves existing history and never clears it. A reliable post-fix cursor/session delta is still pending because the current Pipeline gate times out even for `console_status`.
- No scene or prefab was saved. The incomplete `InfiniteMode.unity` migration remains untouched.

## Human Verification

Use `D:\github-worktrees\Octoplug\TASK-20260918-007-ui-integration` and the actual Play Mode `PowerStrip` custom Inspector. Run exactly these three scenarios:

1. **Connected multitap Socket +1:** Set up Wall → PowerStrip with Product A on Socket01 and Product B on Socket02. Press `Upgrade Socket Count +1`; confirm the same multitap/Cable/Plug/PowerInfo/binding and Socket01/02 connections remain, only Socket03 becomes usable, Powered/Flow/Usage do not change, PowerInfo/footprint refresh, and no overlap appears.
2. **Allowed Power +1:** Below 10, press `Upgrade Allowed Power +1`; confirm sockets/connections remain, PowerInfo updates immediately, future validation reads the new allowance, and Powered/Flow/Usage do not change. At 10, confirm the button reports `MaximumReached` without mutation.
3. **Cable Length +1:** Hold a real Plug preview outside the old reach but inside the new reach, with an applicable Product tooltip visible when possible. Press `Upgrade Cable Length +1`; confirm Inspector/tooltip length updates and real route reach expands immediately while Socket Count, Allowed Power, existing connection, Powered, Flow, and Usage remain unchanged.

## Task Status

`active` / `HUMAN_VERIFY_REQUIRED`. Runtime-control implementation is complete, but authoritative verification is not: the corrected four-test run and reliable Console delta are blocked by the unresponsive Pipeline gate, and the three real Inspector-button scenarios above have not been performed. No commit, push, DONE move, Reward System work, Wall Outlet work, production upgrade UI, persistence, scene migration, or prefab/scene save was performed.

## Verification Fixes & Architecture Audit (2026-09-19)

- **Power Values Checked:** Verified that `House` starts at 8 (Max 22) and `Multitap_One/Two` start at 3. The `allowedPowerWatts` values in prefabs, scene instances, and Play Mode were all correct. The UI representation logic also safely uses these exact values. No changes were necessary.
- **Head Drag Rule Changed:** Modified `HeadDragInput.cs` and `PowerStripHeadController.cs` to base Head mobility solely on having zero downstream Socket connections. The strip's own Plug being connected upstream no longer blocks Head movement. Dragging an upstream-connected Head correctly updates the cable route while preserving the logical connection and the physical plug placement in the socket.
- **Reward System Architecture Audit:** Evaluated options for a future "Socket +1" feature.
  - Current PowerStrip identity and connection persistence relies entirely on C# GameObject/Component instance references (`SocketConnector`, `PlugConnector`).
  - Replacing prefabs at runtime to expand socket capacity (Option A) would orphan upstream/downstream connections and reset `allowedPowerWatts` to prefab defaults.
  - **Recommendation:** Adopt a "Socket activation" model (Option C) where the prefab contains the maximum 5 sockets authored, but limits interaction via an `ActiveSocketCount` state. Upgrading increments this count and enables dormant socket colliders/visuals, perfectly preserving all existing Graph references, identity, and the decoupled `allowedPowerWatts` budget.
- Do not close TASK-007 yet: the current authoritative checkpoint requires real-pointer Human Verification and explicit authorization before the prepared InfiniteMode legacy-instance replacement/save. Wall Outlet single-prefab work remains unstarted.
