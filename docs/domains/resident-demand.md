# Resident / Demand Domain

## Responsibility

Represent residents, their appliance/service needs, satisfaction, and visible status during play.

## Existing Evidence

- `Assets/03_Prefabs/UI/UI_ResidentCard.prefab` with `ResidentInfo`, `NeedInfo`, and `StatusInfo` design fields.
- `Assets/00_Scenes/Demo/InfiniteMode.unity` contains `Residents_Area`, a resident card instance, and an empty `ResidentController` object.
- Product-related information prefabs include `UseInfo.prefab` with a `Using_Person` design field.

No resident runtime model, spawning logic, demand logic, or tests exist.

## Design Direction

The concept document describes resident demand appearing, the player resolving it with powered appliances, and satisfaction being awarded. Resident count contributes to rent at level-up.

Spawning, selection, demand generation/decay, timing, satisfaction, failure thresholds, and appliance-tag matching are Open Decisions.

## Related Domains

- Connection / Power determines whether a requested appliance can operate.
- Reward / Progression consumes satisfaction and resident-count outcomes.
- Room Generation introduces rooms and residents.
- Infinite Mode coordinates demand over a session.

## Modification Cautions

- `UI_ResidentCard.prefab` is shared and instantiated in the main scene; list it as an Exclusive Asset for edits.
- UI wording, hierarchy, pictograms, timing, and status feedback are player-facing decisions.
- Do not infer behavior from `ResidentController` or UI child names.

## Narrow Search Order

1. This map and the current Task.
2. `UI_ResidentCard.prefab`, `UseInfo.prefab`, and the `Residents_Area` scene subtree.
3. Resident/demand sections of the concept PDF.
4. Broaden only when necessary.

## Planned, Not Established

Resident data structures, demand scheduling, state transitions, scoring, persistence, and automated tests are not established.
