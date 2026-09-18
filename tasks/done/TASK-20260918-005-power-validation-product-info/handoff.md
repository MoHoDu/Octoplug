# Handoff — TASK-20260918-005

## Status

Stage: **DONE** (2026-09-18) — human confirmed round-4 verification PASS: "해당 작업 사용자 검증 성공. 완료." Between round 3 and round 4, the user directly redesigned several Product/UI Prefabs in the Unity Editor and **committed** those changes (`d057daf`, `ca3a87f`) — the currently-saved Prefab visual design (RectTransform, Scale, Position, Spacing, Layout, icon size, Sprite, color, Hierarchy, Sorting Order) is the source of truth and was never reverted or redesigned by this Task; code was re-read against and adapted to it instead.

Next: superseded by TASK-20260918-006 (Grid Spatial Foundation + 8-Direction Cable Routing). The 3 open Human Setup Required items (top-alert UI, PowerStrip real instance, Heater's icon-pool-vs-wattage mismatch) are carried into `docs/domains/connection-power.md`'s Deferred TODO and this Task's `meta.md`.

## Round-4 corrections (this pass)

1. **Removed runtime design-overriding code** — the exact anti-pattern flagged this round: `ProductInfoView.tagIconTint` (forced `Tag` icon color every refresh, silently discarding the user's own authored teal tint), `ProductInfoView.powerIconSize` (forced `RectTransform.sizeDelta` every refresh — a round-3 band-aid for a bug that no longer exists now that the user's own Prefab already authors the correct size), and `ProductTooltipController.serviceIconTint` (same forced-color issue on the Tooltip's Service icon) are all deleted. Both classes now only toggle active state, set `.sprite`, and read data — never `RectTransform`/color/scale.
2. **Power display reverted to pure icon-count everywhere** (matches the user's own Prefab authoring: `ProductInfo/Power`'s `Value` label is already inactive by the user's own hand — code just stopped writing to it or forcing anything else): `ProductInfoView.RefreshPowerRow` toggles the *entire* icon pool by count again (not just icon `[0]`), matching `ProductTooltipController`'s existing icon-count logic for the Tooltip (unchanged this round — it already worked this way).
3. **Icon-pool-overflow now reported, not silently clamped-as-correct**: both `ProductInfoView` and `ProductTooltipController` log a `Debug.LogWarning` naming the product, the needed count, and the available pool size whenever `PowerConsumptionWatts` exceeds the current icon pool, in addition to the existing (necessary) clamp for rendering. Found immediately: **Heater's placeholder 6W exceeds every current 5-icon pool** — see Human Setup Required.
4. **Information UI now always renders above gameplay sprites**: `ProductInfo`/`UseInfo`'s Canvas (World Space, root canvas, so `sortingOrder` applies directly) raised from the previously-unset `0` to `100` on all 5 Products — comfortably above the current highest gameplay order (Product `Icon`=11) — without touching any gameplay object's own sortingOrder. `UI_ProductTooltip` needed no change (its Canvas is `ScreenSpaceOverlay`, which already renders above all world-space content).
5. **Dragged Plug sorting made relative, not hardcoded**: `CableRoutingController.EffectiveDraggingSortingOrder()` reads the owning Product's own authored `Icon`/`Background` `sortingOrder` (e.g. TV=11) and raises the dragging order only as far as needed to stay above it (verified: Handle went from a would-be `6` to `12` while dragging TV's Plug) — the authored `draggingSortingOrder` Inspector field is a floor, never lowered, and no new absolute magic number was introduced. `connectedSortingOrder` (resting-connected state) was left untouched — no reported issue there.

## Round-3 corrections (this pass)

1. **Tooltip Power display reversed back to icon-count** (explicit spec change from round 2's icon+number): `ProductTooltipController` now toggles the existing 5-icon pool by `min(watts, 5)` — no number shown — mirroring `ProductInfo`'s original style. The numeric `powerLabel` is hidden at `Awake` and no longer bound.
2. **Real regression found and fixed**: round 2's prefab edit (replacing `ProductInfo/Power`'s `GridLayoutGroup` with a `HorizontalLayoutGroup`) reset every Power icon's `RectTransform.sizeDelta` to `(0,0)`, making the icon invisible — this was the root cause of "power icon missing" and contributed to the reported size/overlap issues. Fixed by restoring `sizeDelta` to `(10,10)` on all icons in all 5 Product prefabs, **and** adding a self-healing guard in `ProductInfoView.RefreshPowerRow` that re-asserts the visible icon's size every refresh (`powerIconSize`, Inspector-adjustable) so this class of bug can't silently recur from a future layout-component swap.
3. **Tag/Power overlap fixed**: `ProductInfo` root's `HorizontalLayoutGroup.spacing` was `-5` (tuned for the old multi-icon overlapping Power display) and pulled the new Power content 5 units into Tag's own box. Changed to `+2`. Verified via world-space `RectTransform` bounds: Tag/PowerIcon and Tag/Value no longer overlap on any of the 5 products.
4. **Value label overflow guarded**: `powerValueLabel` now uses TMP auto-sizing (`fontSizeMin=4, fontSizeMax=8`) and `Truncate` overflow mode so it can never visually bleed outside its own small box regardless of digit count.
5. Root `ProductInfo`/`Power` `RectTransform.sizeDelta` values were NOT enlarged — kept at their pre-existing, compact authored scale; the earlier "too big" perception was the sizeDelta-reset bug (#2) and the overlap (#3), not an actual size increase.

## Round-2 corrections (this pass)

1. **ProductInfo power display**: was N lightning icons (one per watt); now exactly 1 icon + a numeric label (`ProductInfoView.powerValueLabel`, new `Value` TMP object added under each product's `ProductInfo/Power`).
2. **ProductInfo alpha**: `ProductInfoView` now adds a `CanvasGroup` (alpha 0.8, Inspector-adjustable) to the `ProductInfo` root itself, covering both the Power row and the Tag row. Does not touch the Product's own sprite or `UseInfo`.
3. **Tooltip size**: `UI_ProductTooltip`'s `Background` panel `localScale` set to `(0.7, 0.7, 0.7)` — the whole panel (icons, labels, layout) scales together, not just font size.
4. **Tooltip positioning**: `ProductTooltipController.PositionPanel` now reads the clicked Product's actual `SpriteRenderer` bounds (not just its transform/pivot point) projected to screen space, keeps the panel fully clear of that rect (checked, not assumed), and retries the opposite side if the on-screen clamp would have forced an overlap.
5. **Tooltip power icon**: the Tooltip's own Power section had its Icons pool fully hidden in the first pass (relying on the Label alone); now the first icon of that pool is shown standalone (`ProductTooltipController.powerIcon`) alongside the existing numeric Label.
6. **Plug fallback after power rejection**: `CableRoutingController.ApplyPowerRejectFallbackPosition` (called only from the Power-Validation-failure branch, not any other invalid-drop case) pushes the Plug to the nearest walkable/reachable cell at least `powerRejectFallbackDistance` (0.6, Inspector-adjustable) from the Socket, so it no longer visually lands on/overlapping the rejected Outlet.
7. **Tooltip closes on Plug grab**: `PlugDragInput.AnyDragStarted` (new static event, raised alongside the existing per-instance `DragStarted`) is subscribed by `ProductTooltipController.Hide` — closes for a grab on any Plug, not just the tooltipped Product's own. Kept as a narrow, single-purpose static event, not a general event bus. Plug-hit priority over Product clicks (from the first pass) is unchanged.

## What changed (see `git status` for the full file list)

- New: `UsageType.cs`, `Power/Connection/PowerValidationService.cs`, `Power/UI/UsageTypeIconLibrary.cs` (+ its new `.asset`), `Power/UI/ProductInfoView.cs`, `Power/UI/UseInfoView.cs`, `Power/UI/ProductClickInput.cs`, `Power/UI/ProductTooltipController.cs`.
- Modified: `ApplianceSource.cs` (new fields + Powered state), `CableRoutingController.cs` (Power Validation hook, Connected/Powered split), `PlugDragInput.cs` (added `IsPointerOverAnyPlug` static helper only — drag behavior itself unchanged), `docs/domains/connection-power.md`.
- Prefabs: `TV/Fan/Heater/Induction/Air_Conditioner.prefab` (ApplianceSource test data, Icon Collider2D + ProductClickInput, ProductInfoView/UseInfoView wiring), `UI_ProductTooltip.prefab` (icon swaps, Power Icons hidden, ProductTooltipController wired).
- Scene: `InfiniteMode.unity` — `HousePowerBudget.allowedPowerWatts` set to `10` (test placeholder).

## Human Setup Required

1. **Top-alert UI** — confirmed no such UI exists anywhere in the project. A `Debug.LogWarning` (distinguishing House vs. PowerStrip limit) stands in for this stage only; it is not a UX deliverable.
2. **PowerStrip real instance** — confirmed none exists in `InfiniteMode` (`Multitaps` node is empty). `PowerValidationService`'s Strip-budget path is implemented and directly verified via `eval` against a `PowerStrip` component, but real drag/drop Human Verification of it needs a placed instance. Not placed by AI — awaiting either an instance or explicit "AI Setup Allowed" permission.
3. **Heater's Power icon pool is too small for its current test wattage**: Heater's `PowerConsumptionWatts` is `6` (a placeholder from an earlier round, never a confirmed balance number), but both `ProductInfo/Power` and `UI_ProductTooltip/Background/Power/Icons` currently have exactly 5 icon slots (all 5 Products/the Tooltip share this pool size). Display clamps to 5 and logs a warning rather than silently showing 5-as-if-correct, but the underlying mismatch needs a human decision: either add a 6th icon slot to both Prefabs (a Prefab-design change, out of this round's scope to make unilaterally) or lower Heater's test wattage to ≤5.

## Human Verification (3 items, round 4)

1. TV and Tooltip's power display are both pure lightning-icon counts (no number anywhere) — TV should show 5 lit icons in both places.
2. Your own Prefab edits (sizes, spacing, positions, colors, Tooltip's ~70% scale, etc.) still look exactly as you left them in Play Mode — nothing was reset or redesigned.
3. Drag TV's Plug over the TV's own sprite — the Plug should stay visible on top, never disappear behind it; and ProductInfo/UseInfo should always render above Cable/Outlet/Product art, never behind it.

## Next

Not scoped further per explicit instruction ("다음 8-direction Cable Routing Task를 시작하지 마라"). TASK-005 stays HUMAN_VERIFY_REQUIRED until this round passes.
