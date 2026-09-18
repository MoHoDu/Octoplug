# Task Metadata

- **ID:** TASK-20260918-005
- **Title:** P0-1E Power Validation + Powered State + Product Information UI
- **Status:** done
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** Connection / Power (primary); Resident / Demand (UseInfo preview only, no real logic)
- **Base:** dev
- **Branch:** feat/infinity-power-connection
- **Started:** 2026-09-18
- **Updated:** 2026-09-18
- **Current Stage:** done — human confirmed round-4 verification PASS
- **Current Skill:** qa-feature

## Allowed Scope

- Power Validation: House allowed-power check (Wall Outlet connections) and House+PowerStrip allowed-power check (PowerStrip connections), evaluated before a Socket connection is finalized.
- Split `Connected` (physical Plug↔Socket pairing, existing P0-1D) from `Powered` (Connected AND Power Validation PASS); relocate `CablePowerFlowEffect.SetPowered` to follow `Powered`, not `Connected`.
- A minimal, non-`ScriptableObject`-locked-in product data surface exposing UsageTypes (multi-tag), PowerConsumption, CableLength, Capacity, SatisfactionDuration per product, read by UI.
- Wire the existing `ProductInfo` (bottom, always-on: power-as-lightning-icons + UsageType icon) and `UseInfo` (top, active/waiting-as-person-icons, hidden at 0) prefabs on each Product to real/placeholder data — reuse existing structure, no new UI prefab.
- Wire the existing `UI_ProductTooltip` prefab to real product data on click; position it above/below the clicked product based on screen half, clamped on-screen; single-tooltip-at-a-time; closes on outside click; must not conflict with Plug drag input.
- Minimal, explicitly separated Power-related service/component (not a God Class) computing current House/PowerStrip usage from currently-Powered connections, recomputed simply (no caching) each time a connection changes.
- Console-level (not final UI) signal for House/PowerStrip limit-exceeded, clearly marked temporary.

## Do Not Modify

- `GridPathfinder`, `CableRoutingGrid`, `CableLength` values/enforcement, Door routing, Room/Door Collider position or rotation.
- `PlugConnector`/`SocketConnector`/`PlugSocketConnection` pairing semantics (Connected state itself) — only consume them, do not change how Connect/Disconnect pairs references.
- PowerStrip body drag/movement, Socket count/upgrade, procedural Room Generation, Resident demand/satisfaction/Level-Up/Reward logic, Product overlap alpha fade, audio.
- Do not finalize real balance numbers (wattages, allowed power, satisfaction duration) as game design — Inspector-editable placeholders only; flag any requested exact value as a Human Decision.
- Do not hardcode UsageType→icon or Product-name→icon mapping by product identity; must be a UsageType-keyed mapping.
- Do not design/place a new top alert UI without an existing one to reuse — Human Setup Required if none exists (to confirm after investigation).

## AI Setup Allowed

- Investigation complete (see `plan.md`). AI Setup Allowed for: attaching new driver/controller MonoBehaviours to the existing `ProductInfo`/`UseInfo`/`UI_ProductTooltip` prefab instances, adding a `Collider2D` to each Product's `Icon` child for click detection, creating the `UsageTypeIconLibrary` ScriptableObject asset, and editing test-placeholder numeric fields (`HousePowerBudget`, `ApplianceSource` per product). Not extended to anything under Do Not Modify.

## Exclusive Assets

- `Assets/03_Prefabs/Products/*.prefab` (TV, Fan, Heater, Induction, Air_Conditioner — ApplianceSource fields, ProductInfoView/UseInfoView/ProductClickInput wiring, new Collider2D).
- `Assets/03_Prefabs/UI/UI_ProductTooltip.prefab` (ProductTooltipController wiring, Tooltip Power section Icons-hide).
- `Assets/03_Prefabs/Products/ProductInfo.prefab`, `Assets/03_Prefabs/Products/UseInfo.prefab` (if edited at the shared-prefab level rather than per-instance).
- `Assets/00_Scenes/Demo/InfiniteMode.unity` (`HousePowerBudget` test value; any scene-level Tooltip singleton reference wiring).
- New file: `Assets/01_Scripts/Power/UI/UsageTypeIconLibrary.asset` (new ScriptableObject data asset).

## Human Decisions

- None yet. Candidates flagged for later: final top-alert UI design; exact tint color for the newly-adopted `_filled` silhouette icon set; final power/capacity/duration balance numbers; PowerStrip real-instance placement.

## Open Decisions — RESOLVED

- Top-alert/warning UI: **confirmed absent** anywhere in the scene/Canvas hierarchy. Human Setup Required for the final UI; a `Debug.LogWarning` (House vs. Strip, distinguished) stands in for this stage only.
- PowerStrip instance in `InfiniteMode`: **confirmed absent** (`Multitaps` node has no children). PowerStrip validation logic is built and directly testable via `eval` against a `PowerStrip` component; real Human Verification of it needs a placed instance — Human Setup Required, not placed by AI without explicit permission.
- Numeric test placeholder values are listed in `plan.md`'s "Test Placeholder Values" section — explicitly not balance decisions, Inspector-editable.

## Verification State

- Round 1 automated verification (2026-09-18): Power Validation connect/disconnect/reject/reconnect (House-budget case, real drag/drop via `eval` reflection), ProductInfo/UseInfo on all 5 products, Tooltip show/position/data-binding/hide, Product-click routing + Plug-drag priority. Zero Console errors.
- Round 1 Human Verification: **PASS** on House-limit connect/reject, Powered/Flow, ProductInfo/Tooltip basic data, Tooltip open/close — with 7 UI/UX corrections requested (see `handoff.md` "Round-2 corrections").
- Round 2 automated verification (2026-09-18): ProductInfo icon+number display and 0.8 alpha; Tooltip 0.7 scale, power icon restored, non-overlapping position (verified against TV's actual `SpriteRenderer` bounds, not just its pivot), close-on-any-Plug-drag-start; Plug fallback distance (≥0.6 units) after a power-rejected connection; full regression (normal connect/disconnect/Flow) re-verified. Zero Console errors.
- Round 2 Human Verification: found a real regression (Power icon's `RectTransform.sizeDelta` reset to zero by round 2's own `GridLayoutGroup`→`HorizontalLayoutGroup` prefab edit) plus a Tag/Power layout overlap, and reversed the Tooltip Power display spec back to icon-count (no number).
- Round 3 automated verification (2026-09-18): Tooltip Power now shows exactly `min(watts, 5)` icons (verified 1/3/5/8→5-clamped) with the numeric label hidden; ProductInfo icon `sizeDelta` restored to `(10,10)` and self-healing guard added; Tag/Power overlap fixed (root `HorizontalLayoutGroup.spacing` `-5`→`+2`, verified via world-space `RectTransform` bounds — no overlap on any of the 5 products); Value label overflow-guarded; full regression (connect/disconnect/Powered/Flow, all 5 products' ProductInfo) re-verified. Zero Console errors. Cable Routing untouched this round.
- **Between rounds**: the user directly redesigned several Product/UI Prefabs in the Editor and committed the changes (`d057daf`, `ca3a87f`) — the round-3 self-healing/forced-value code (`powerIconSize`, `tagIconTint`, `serviceIconTint`) was by then obsolete and, per the user's explicit instruction, actively harmful (it would have silently overwritten the user's own authored values). Round 4 re-read the actual current Prefab state fresh and removed all such code.
- Round 4 automated verification (2026-09-18): confirmed the round-3 forced-value code was gone and had not left stale state; ProductInfo power reverted to full icon-count (5/5 icons for TV, matching its own Prefab's already-correct pool); Tag/Service icon colors confirmed preserved exactly as user-authored (not overwritten); Heater's 6W-vs-5-icon-pool mismatch found and now logs a warning instead of silently clamping; `ProductInfo`/`UseInfo` Canvas sortingOrder raised `0`→`100` (Tooltip needed no change, already `ScreenSpaceOverlay`); dragging Plug sortingOrder confirmed computed relative to the Product's own authored order (TV Icon=11 → Handle=12 while dragging, was `6`); full connect/disconnect/Powered/Flow regression re-confirmed. Zero Console errors. Cable Routing untouched and not started.
- Round 4 Human Verification (2026-09-18): **PASS**. "해당 작업 사용자 검증 성공. 완료." Final Status: **DONE**.
- Open Human Setup Required items (top-alert UI, PowerStrip real instance, Heater's icon-pool-vs-wattage mismatch) are carried forward — see `docs/domains/connection-power.md` Deferred TODO and this file's "Human Decisions" section above. Not resolved by this Task's closure; left for whenever a human addresses them.
