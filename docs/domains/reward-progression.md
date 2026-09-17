# Reward / Progression Domain

## Responsibility

Represent satisfaction, level-up/rent outcomes, reward selection, upgrades, unlocks, and long-term session progression.

## Existing Evidence

The concept document is currently the only stable source for this domain. No dedicated progression prefab, custom C#, balance data, persistence implementation, or automated tests exist.

Related design assets may eventually reuse product, multitap, room, and resident content, but those assets do not establish reward behavior.

## Design Direction

The documented loop is:

1. Resolve resident demand.
2. Gain satisfaction.
3. Level up and receive rent based on residents.
4. Buy or upgrade a reward.
5. Unlock a new room and resident.

Reward formulas, prices, choices, upgrade effects, pacing, failure states, and infinite scaling are Open Decisions.

## Related Domains

- Resident / Demand produces satisfaction outcomes.
- Room Generation consumes unlock outcomes.
- Connection / Power may receive capacity or equipment upgrades.
- Infinite Mode coordinates the loop.

## Modification Cautions

- Do not invent balance values or progression formulas.
- Google Sheets integration is planned but absent; do not assume it is the final authoring source.
- Reward UI, purchase confirmation, and presentation require the Human Decision Gate.

## Narrow Search Order

1. This map and the current Task.
2. Reward/progression sections of the concept PDF.
3. Related Domain Maps for integration boundaries.
4. Broaden only after a concrete implementation source exists.

## Planned, Not Established

Economy data, reward catalogs, upgrade application, save data, analytics, and tests are not established.
