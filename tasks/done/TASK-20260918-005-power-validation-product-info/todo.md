# Todo — TASK-20260918-005

## Done

- [x] Confirmed no top-alert UI exists anywhere in the scene/Canvas — Human Setup Required, `Debug.LogWarning` stands in for this stage.
- [x] Confirmed no PowerStrip instance exists in `InfiniteMode` — Human Setup Required for real verification; logic built and unit-tested via `eval` regardless.
- [x] Confirmed exact `UI_ProductTooltip` structure via live-Editor read (5 sections: Service/Power/Cable/Capacity/Duration; Power already had both an Icons pool and a Label).
- [x] `UsageType` flags enum + `UsageTypeUtility.EnumerateFlags`.
- [x] `ApplianceSource` extended with `UsageTypes`, `Capacity`, `SatisfactionDurationSeconds`, `IsPowered`/`SetPowered`.
- [x] `UsageTypeIconLibrary` ScriptableObject created and populated with the 4 confirmed `_filled` sprites.
- [x] `PowerValidationService` (House + PowerStrip, recompute-based, no caching).
- [x] `CableRoutingController` hooked: Power Validation before Connect, `ApplianceSource.SetPowered` + `CablePowerFlowEffect.SetPowered` on both connect and disconnect, defensive Start()-time validation for an already-connected Plug.
- [x] `ProductInfoView` (Power icon-count row, multi-tag Tag row with pooled clones).
- [x] `UseInfoView` (active/waiting person icons, hide-at-zero, pooled clones) — fixed a same-name-sibling bug where `Transform.Find` only captured one of two authored `Using_Person`/`Icon` objects.
- [x] `ProductClickInput` (passive per-Product click target) + `PlugDragInput.IsPointerOverAnyPlug` (Plug-drag priority) + `ProductTooltipController` (single input owner: Product click / empty-space close / Plug-priority).
- [x] Wired all 5 Product prefabs (TV/Fan/Heater/Induction/Air_Conditioner): ApplianceSource test data, click Collider2D, ProductInfoView, UseInfoView.
- [x] Wired `UI_ProductTooltip.prefab`: icon swaps (Cable→plug, Capacity→capacity, Duration→timer), hid the Power Icons pool (Label used instead), attached and wired `ProductTooltipController`.
- [x] Set test placeholder values: House=10W; TV=5W/Fun, Fan=2W/Cooling, Heater=6W/Heating, Induction=4W/Meal, Air_Conditioner=5W/Cooling.
- [x] Live-Editor Play-mode verification: connect/disconnect/reject-under-budget/reconnect-after-restore (all correct, including zero stale state on reject); ProductInfoView/UseInfoView on TV and (via temporary Play-mode instantiation) all 4 other products; Tooltip show/position(top→below, bottom→above, corner clamp)/data-binding/hide; Product-click hit routing and Plug-priority. Zero Console errors; only the two intentional rejection-test warnings.
- [x] Scene saved with the House=10W test value.

## Round 2 — UI/UX corrections (2026-09-18, from Human Verification feedback)

- [x] ProductInfo power display: 1 icon + numeric label (was N icons for N watts).
- [x] ProductInfo overall alpha 0.8 (power + tag row only, via `CanvasGroup` on the `ProductInfo` root — Product sprite and `UseInfo` untouched).
- [x] Tooltip panel scaled to ~70% (`Background.localScale`, not just font size).
- [x] Tooltip positioning now uses the Product's actual `SpriteRenderer` screen bounds (not just its pivot point), verified non-overlapping, with opposite-side retry if clamping would force an overlap.
- [x] Tooltip's Power row lightning icon restored (was fully hidden; Label alone was insufficient per spec).
- [x] Plug fallback position after a power-rejected connection pushed to ≥0.6 units from the Socket (verified 0.64 units in test) — applies only to that one rejection path.
- [x] Tooltip closes immediately when any Plug starts a drag (`PlugDragInput.AnyDragStarted` static event), not just on empty-space clicks.
- [x] Full regression re-verified (connect/disconnect/Powered/Flow) after all of the above. Zero Console errors.
- [x] **Round-2 Human Verification** — found 4 issues (Tooltip Power spec reversal + a real sizeDelta-reset regression + a layout overlap it caused). See round 3 below.

## Round 3 — ProductInfo/Tooltip UI fixes only, Cable Routing untouched (2026-09-18)

- [x] Tooltip Power reverted to icon-count style (`min(watts, 5)` icons, no number) — explicit spec reversal from round 2.
- [x] Root-caused and fixed the real regression: Power icon `RectTransform.sizeDelta` had been reset to `(0,0)` by round 2's `GridLayoutGroup`→`HorizontalLayoutGroup` swap. Restored to `(10,10)` on all 5 products; added a self-healing guard in `ProductInfoView` so this can't silently recur.
- [x] Fixed the Tag/Power overlap (`ProductInfo` root `HorizontalLayoutGroup.spacing` `-5`→`+2`), verified via world-space `RectTransform` bounds on all 5 products.
- [x] Guarded the Value label against text overflow bleed (TMP auto-size + Truncate).
- [x] Confirmed ProductInfo's own `RectTransform` sizes were never enlarged — the "too big" report traced entirely to the two bugs above.
- [x] Full regression re-verified (all 5 products' ProductInfo, TV connect/disconnect/Powered/Flow). Zero Console errors. Cable Routing code untouched.
- [x] **Round-3 Human Verification** — superseded: the user redesigned several Prefabs directly instead (see round 4).

## Round 4 — UI Integration correction after user's own Prefab redesign (2026-09-18)

- [x] Confirmed via `git log` that the user's Prefab edits are committed (`d057daf`, `ca3a87f`) and are now the source of truth; re-read current Prefab/hierarchy state fresh rather than assuming prior task history was still accurate.
- [x] Removed all runtime design-forcing code: `ProductInfoView.tagIconTint`/`powerIconSize`, `ProductTooltipController.serviceIconTint` — both classes now only toggle active state, set `.sprite`, and read data.
- [x] ProductInfo power display reverted to full icon-count (matches the user's own Prefab, which already has a real 5-icon pool with `Value` manually deactivated).
- [x] Added icon-pool-overflow `Debug.LogWarning` (product/needed/available) to both `ProductInfoView` and `ProductTooltipController` — found Heater's 6W exceeds the current 5-icon pool (Human Setup Required, not silently clamped-as-correct).
- [x] Raised `ProductInfo`/`UseInfo` Canvas `sortingOrder` `0`→`100` (root canvases, so this applies directly) so info UI always renders above gameplay sprites — no gameplay object's own sortingOrder touched. `UI_ProductTooltip` needed no change (`ScreenSpaceOverlay`).
- [x] Added `CableRoutingController.EffectiveDraggingSortingOrder()` — relative to the Product's own authored `Icon`/`Background` order, not a new hardcoded number — so a dragged Plug can't disappear behind the Product it's dragged over.
- [x] Full regression re-verified (5 icons for TV in both ProductInfo/Tooltip, preserved user-authored colors, alpha 0.8, connect/disconnect/Powered/Flow, dragging sort order). Zero Console errors. Cable Routing untouched, not started.
- [x] **Round-4 Human Verification: PASS** ("해당 작업 사용자 검증 성공. 완료."). Task closed **DONE**. Superseded by TASK-20260918-006 (Grid Spatial Foundation + 8-Direction Cable Routing).

## Not Started / Explicitly Deferred (see meta.md Do Not Modify + Human Setup Required)

- Final top-alert UI design — Human Setup Required.
- PowerStrip real-instance Human Verification — Human Setup Required (logic itself is built and tested).
- Final tint color for the newly-adopted `_filled` silhouette icon set (currently a flat dark-gray placeholder) — art/Human decision.
- Final power/capacity/duration balance numbers — currently test placeholders only.
- Tooltip positioning assumes the panel's RectTransform pivot is centered (0.5, 0.5) — confirmed true for the current authored prefab; a future repivot would need the clamp math revisited.
