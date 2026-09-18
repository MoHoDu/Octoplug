# Plan — P0-1E Power Validation + Powered State + Product Information UI

## Investigation Results (resolves prior Open Decisions)

- No top-alert/warning UI exists anywhere in `InfiniteMode.unity` or its Canvas hierarchy. **Confirmed Human Setup Required** — a `Debug.LogWarning` (distinguishing House vs. PowerStrip limit) is the temporary stand-in for this stage only.
- No `PowerStrip`/Multitap instance exists in `InfiniteMode` (`Multitaps` node is empty). **Confirmed**: PowerStrip validation logic is built and unit-testable via direct `eval` calls against a `PowerStrip` component, but real Human Verification of it needs a placed instance — **Human Setup Required**, not placed by AI without permission.
- All power-related numeric fields (`HousePowerBudget.allowedPowerWatts`, every `ApplianceSource.powerConsumptionWatts`, every `PowerStrip.allowedPowerWatts`) are literally `0`. Test placeholder values are required for any functional verification; they are explicitly not balance decisions.
- `UI_ProductTooltip.prefab` (confirmed via live-Editor hierarchy read, not the earlier research fork's summary) has exactly 5 sections: `Service` (Icon+Label — UsageType), `Power` (Icons×5 + **Label**, sprite `icon_electric`), `Cable` (Icon+Label, sprite `icon_line_length`), `Capacity` (Icon+Label, sprite `icon_allowed_human_count`), `Duration` (Icon+Label, sprite `icon_effect_time`). Power already has a Label sibling to its Icons group — the Tooltip's numeric-Power requirement is satisfied by using that Label and hiding the Icons group, no new node needed.
- `icon_electric.png` is already a proper colored (orange) icon and matches current UI style — kept as-is everywhere (ProductInfo icon-count, Tooltip Power's now-hidden Icons group left alone, no visual change there). The `_filled` icon set (`icon_fun_filled`, `icon_food_filled`, `icon_cold_filled`, `icon_hot_filled`, `icon_plug_filled`, `icon_capacity_filled`, `icon_timer_filled`) are all white silhouettes meant to be tinted via `Image.color` — the same pattern already used by `icon-person.png` in `UseInfo` (tinted teal/grey there). A dark-gray tint (`RGBA(0.20,0.20,0.20,1)`) is applied as a functional placeholder; exact tint/color is an art decision, not finalized here.

## Data Model

- New `Octoplug.Power.UsageType` — `[Flags] enum { None=0, Cooling=1, Heating=2, Meal=4, Fun=8 }` + a static `UsageTypeUtility.EnumerateFlags(UsageType)` helper (multi-tag iteration without a custom collection type).
- `ApplianceSource` (existing MonoBehaviour, already not an SO) gains: `usageTypes` (UsageType, Inspector shows Unity's native flags-mask dropdown), `capacity` (int), `satisfactionDurationSeconds` (float), and power-state surface: `IsPowered { get; }` + internal `SetPowered(bool)` called only by the connection/validation flow. `PowerConsumptionWatts`/`Cable` unchanged. `CableLength` for UI reads through `Cable.CableLength` (no duplication).
- New `Octoplug.Power.UI.UsageTypeIconLibrary` — a small ScriptableObject asset (icon lookup is a static design table, not per-product data, so this does not conflict with the "no SO-locked product data" rule) mapping each single `UsageType` flag to a `Sprite`. One asset instance created and assigned via the 4 confirmed `_filled` sprites.

## Power Validation

- New static `Octoplug.Power.Connection.PowerValidationService`:
  - `GetHouseUsage()` — sums `PowerConsumptionWatts` over all `ApplianceSource` in scene where `IsPowered`.
  - `GetStripUsage(PowerStrip strip)` — same sum, restricted to products whose `Cable.Plug.ConnectedSocket` is contained in `strip.Sockets`.
  - `TryValidate(ApplianceSource product, SocketConnector targetSocket, HousePowerBudget house, out string failureReason)` — House check always; if `targetSocket.GetComponentInParent<PowerStrip>()` is non-null, additionally checks that strip's own budget. No caching, recomputed on every call (per explicit instruction) — this is what keeps Connect/Disconnect from ever needing separate increment/decrement bookkeeping.
- Hook point: `CableRoutingController.TryConnectToNearbySocket`, inserted between the existing Cable-length check and `PlugSocketConnection.Connect(...)`:
  1. Socket connectable + routing/length (existing, unchanged).
  2. **Power Validation** (new) — reject (`return false`, no `Connect` call, no stale refs) with a `Debug.LogWarning` distinguishing House vs. Strip on failure.
  3. `PlugSocketConnection.Connect` (existing, unchanged).
  4. `applianceSource.SetPowered(true)` + `powerFlowEffect.SetPowered(true)` (replaces the old bare `Connected → Flow` call).
- Disconnect side (`OnDragged`'s real-detach branch) gains `applianceSource.SetPowered(false)` alongside the existing `PlugSocketConnection.Disconnect` + `powerFlowEffect.SetPowered(false)`.
- `CableRoutingController` resolves `applianceSource` once in `Start()` via `GetComponentInParent<ApplianceSource>()` (Connection is a child of the Product root that carries `ApplianceSource` — confirmed on TV) and `houseBudget` via `FindFirstObjectByType<HousePowerBudget>()`, mirroring the existing `powerFlowEffect = GetComponent<CablePowerFlowEffect>()` pattern.
- A Start()-time pass re-validates and applies Powered/Flow for any Plug that is already `IsConnected` at scene load (defensive — no such case exists in the current scene, but avoids a stuck "Connected but never validated" state if one is authored later).
- `previewPowered` on `CablePowerFlowEffect` is untouched — remains Inspector-only, still OR'd with real `isPowered` in `Update()`, never treated as a real state.

## ProductInfo (bottom, always-on)

- New `Octoplug.Power.UI.ProductInfoView` MonoBehaviour attached to each Product's existing `ProductInfo` instance. Reads the sibling `ApplianceSource` via `GetComponentInParent<ApplianceSource>()`.
- Power row: toggles the 5 existing `Icon` children active/inactive so exactly `min(PowerConsumptionWatts, 5)` are shown (no text). Values above 5 are clamped, not overflowed — pool size is a structural constraint of the existing prefab.
- Tag row: the existing single `Tag/Icon` Image is the pool template; `UsageTypeUtility.EnumerateFlags` drives 1 icon per active flag, cloning additional Image siblings under the same `HorizontalLayoutGroup` parent as needed (deactivated pool grows/shrinks, never destroyed, so this is stable across repeated updates) — single-tag products (all current ones) use exactly the existing single Image unchanged.
- Refreshed once in `Start()` and again whenever `ApplianceSource` values change is out of scope (products are static per this task) — a public `Refresh()` method exists for future callers.

## UseInfo (top, active/waiting)

- New `Octoplug.Power.UI.UseInfoView` MonoBehaviour attached to each Product's `UseInfo` instance. Exposes `SetCounts(int active, int waiting)`.
- Hides the whole `UseInfo` root when `active + waiting == 0`.
- Pools the existing `Using_Person` (active/teal) and `Icon` (waiting/grey) templates the same way as ProductInfo's Tag row — clones beyond the initial 2+2 are created on demand, so counts are not hard-capped at 4 even though the demo will only ever preview within that range.
- No real Resident wiring in this task — a `[SerializeField] previewActiveUsers/previewWaitingUsers` pair (Inspector-only, mirrors `CablePowerFlowEffect.previewPowered`'s precedent) drives `SetCounts` in `Start()` for now, explicitly commented as non-final so it cannot be mistaken for a game rule.

## Product Click + Tooltip

- New `Octoplug.Power.UI.ProductClickInput` MonoBehaviour: mirrors `PlugDragInput`'s exact pattern (manual `Mouse.current` + `Collider2D.OverlapPoint`, no EventSystem/Raycaster) so it never competes with Plug drag input for the same click. Requires a new `Collider2D` on each Product's `Icon` child (sized to the sprite bounds) — Products currently have none. Fires a `Clicked` event with the Product's `ApplianceSource` + world position.
- A separate always-on-top `EmptySpaceClickWatcher` (or logic inside the Tooltip controller) closes the tooltip when a left-click lands with no Product hit this frame — implemented as: the Tooltip controller listens to every `ProductClickInput.Clicked`, and additionally checks on every left-click-release frame whether any Product was hit; if not, closes. This avoids a second manual-input listener duplicating Mouse polling.
- New `Octoplug.Power.UI.ProductTooltipController` (singleton on the existing `UI_ProductTooltip` instance): `Show(ApplianceSource product, Vector2 productScreenPos)` binds the 5 fields (Service icon via `UsageTypeIconLibrary` — first active flag only when multi-tag, since the Tooltip has one Service icon slot; Power Label as plain number; Cable Label as `"{n}m"`; Capacity Label as plain number; Duration Label as `"{n}s"`) and repositions: below the product if `productScreenPos.y` is in the screen's top half, above if bottom half, clamped fully on-screen via `RectTransformUtility`. `Hide()` deactivates the root. Only one instance is ever shown; clicking a different Product replaces content + position in place (no double-show).

## Test Placeholder Values (explicitly not balance — Inspector-editable)

- `HousePowerBudget.allowedPowerWatts` = 10.
- `ApplianceSource`: TV = 5W / Fun / capacity 1 / duration 6s; Fan = 2W / Cooling / capacity 1 / duration 8s; Heater = 6W / Heating / capacity 1 / duration 10s; Induction = 4W / Meal / capacity 2 / duration 5s; Air_Conditioner = 5W / Cooling / capacity 1 / duration 12s. These make House=10 exceedable by connecting Heater(6)+TV(5)=11, giving a real reject case for Human Verification without editing anything at test time.
- Cable lengths are unchanged (already authored, out of this task's scope).
- No `PowerStrip.allowedPowerWatts` placeholder is set on the 5 Multitap prefabs beyond what already exists (0) — no instance exists to test against; changing it would be a value nobody can verify this stage. Left as Open/Deferred.

## Explicitly Not Done Here

Matches `meta.md` Do Not Modify: PowerStrip body drag, Socket upgrades, procedural Room Generation, resident demand/satisfaction/Level-Up/Reward, Product overlap alpha fade, audio, final Alert UI design, real UsageType→icon tint color decision (art), PowerStrip real instance placement.
