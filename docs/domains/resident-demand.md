# Resident / Demand Domain

## Responsibility

Represent Resident demand, authoritative assignment/progress state, and binding to the authored Resident Card and Product UseInfo UI.

## Demand Generation

- Pure core under `Assets/01_Scripts/ResidentDemand/` owns numbering, one/two-Need state, Satisfaction/Patience, outcomes/cooldown, weighted selection, and deterministic assignment planning.
- Local validated D001–D008 records are authoritative at runtime; Google Sheet data is authoring-only. Each row has explicit Enabled, MinRoomCount, weight, and required Needs.
- D005–D008 two-Need rows use MinRoomCount 15. There is no separate room-15 code branch.
- Selection filters Enabled rows by RoomCount and requires every Need flag in Available Usage Pool before applying stable weights.
- No candidate is normal: the Resident remains Idle/None, no fallback Demand is created, and randomness is not consumed.

## Available Usage Pool and Assignment

- `ResidentDemandController` discovers active `ApplianceSource` objects under `RoomArea.IsGameplayEnabled` content and aggregates every `UsageTypes` flag.
- HintLocked/unrevealed rooms, inactive Products, and Products outside gameplay-active House content are excluded. `RoomContentReady` refreshes the pool for future normal generation opportunities.
- Power and connection do not affect candidate eligibility. They, along with Capacity, compatibility, stable order, and FIFO, remain authoritative for assignment/resolution.
- Product availability changes recompute assignment and UI immediately but never replace or mutate an active Demand.
- `ResidentsChanged`, `AssignmentsChanged`, and read-only `ProductUsageSnapshot` remain the event/data boundaries. Product names are never matching keys.

## Resident Card Binding

- Reuse the preplaced `Canvas/Residents_Area/UI_ResidentCard` as Resident 01; create each later card once and preserve controller/sibling order.
- Resident numbering always copies `ResidentNumber.DisplayValue`.
- Idle/Cooldown shows authored `None` and hides Icon01, Icon02, and progress. One Need uses Icon01 only. Two Needs remain visible in Demand order.
- Incomplete slots use canonical `UsageTypeIconLibrary` sprites. Authoritatively completed two-Need slots use the canonical green `icon-check`; UI never infers completion.
- USING displays exact `SatisfactionProgress` with authored green. WAITING displays exact `PatienceProgress` with `#FF0032`. Existing events provide immediate switching without resetting either value.

## Product UseInfo Binding

- `UseInfoView` consumes `ProductUsageSnapshot` only; it does not calculate assignment.
- The shared prefab owns one ordered 20-person direct-child Image pool. Preserved authored references supply orange active and grey waiting colors and locate the common parent.
- First active-count slots render orange, following waiting-count slots render grey, and the remainder are hidden. Active residents have overflow priority; one warning reports clamping.
- Hide UseInfo when disconnected, unpowered, or total count is zero. Never instantiate, clone, rename, or reparent person icons.

## Session Progression Outcome Boundary

- `ResidentDemandController.DemandResolved` is the production boundary from resolved Demand state into session progression.
- `DemandOutcome` carries the selected balance row and resolution; exact row Satisfaction deltas and success-only `ExpReward` remain authoritative.
- `SessionProgressState` owns Global Satisfaction and EXP mutation. Resident Demand core never finds or mutates HUD objects.
- `SessionProgressController` subscribes to resolved outcomes, exposes read-only progression values, and forwards one-shot depletion and EXP-threshold signals.
- `SessionProgressHudCoordinator` updates `GameStatusInfoView` from controller change events. The view presents normalized values only and never calculates rewards or changes gameplay state.

## Debug and Lifecycle Boundaries

- Inspector controls delegate to production APIs for Add Resident, normal Demand refresh, RoomCount/Available Usage/current Demand inspection, and existing `PromoteCurrentHint()`.
- Space-key Room promotion remains unchanged.
- Satisfaction depletion and EXP threshold are signals only. Automatic Room/Resident generation, Camera reveal, pause/delay, rewards, Result UI, Restart UI, and full GameFlow remain excluded.
- Human Verification PASS is required before TASK-010 can be DONE.

## Modification Cautions

- Claim `UI_ResidentCard.prefab`, `UseInfo.prefab`, and `InfiniteMode.unity` before serialized edits.
- Preserve authored hierarchy, transforms, sprites, colors, active states, and layout.
- Use the matching isolated Editor; do not hand-edit scene/prefab YAML while it is reachable.
- UI must not create Demand state, calculate assignment/availability/Capacity, or own hidden progress.

## Narrow Search Order

1. Current Resident Demand Task and this map.
2. `ResidentDemandState`, `WeightedDemandSelector`, `ResidentDemandController`, `ResidentCardView`, `ResidentDemandUiCoordinator`, and `UseInfoView`.
3. The Resident Card/UseInfo prefabs and production Residents/Product subtrees.
4. Broaden to Connection/Power or Room Generation only when their boundary requires it.
