# Reward / Progression Domain

## Responsibility

Represent satisfaction, level-up outcomes, reward selection, upgrades, unlocks, and
long-term session progression.

---

## Confirmed Design (as of 2026-09-20)

See the authoritative record:
`docs/decisions/infinity-progression-and-reward-loop.md`

Summary:

- **No Rent.** No Currency. No purchase cost for Rewards. No currency-gated Upgrades.
- **No separate Upgrade Phase or Upgrade UI.** All upgrades are Rewards.
- **Reward trigger:** Every Level Up → 3-choice Reward → player picks 1.
- **Reward Pool:** Basic Rewards + Basic Upgrades + Composite Rewards (2 effects in 1).
- **Target-required Rewards:** popup hides → world dims → player selects target → applied.
- **Satisfaction:** 0–100 fixed. Need satisfied → +gain. Need failed → −loss. At 0 → Game Over.
- **EXP/Level:** threshold grows with room count; exact formula is Open Decision.
- **Level-Up flow:** Room Generated → Camera Reveal → 1 s wait → Reward → Resume.

---

## Superseded Items

The following phrases from older documents no longer reflect the authoritative design:

| Old description | Status |
|---|---|
| "Level up and receive rent based on residents" | **Superseded** |
| "Buy or upgrade a reward" | **Superseded** |
| "Upgrade Phase / Upgrade UI" | **Superseded** |
| "재화 소비를 통한 Upgrade" | **Superseded** |
| "Rent 획득" | **Superseded** |

---

## Established Runtime Boundary

- `SessionProgressState` is the authoritative pure state for Global Satisfaction (`0..100`), current EXP, RoomCount-specific Required EXP, and one-shot depletion/threshold signals.
- Infinite Mode starts with Global Satisfaction `100`.
- Success applies the resolved Demand row's exact positive Satisfaction delta and `ExpReward`; failure applies its exact negative Satisfaction delta and grants no EXP.
- `SessionProgressConfig.asset` supplies explicit RoomCount→RequiredEXP entries. The temporary Human Verification table is Room 1=`20`, Room 2=`30`, continuing in increments of 10 through Room 15=`160`.
- These entries are temporary balance data, not a runtime formula. The final scaling formula remains an Open Decision.
- `RequiredExperienceImporter` defines the future Sheet-editable schema as `RoomCount` and `RequiredEXP`; no runtime Google Sheet transport exists.
- `SessionProgressController` consumes `ResidentDemandController.DemandResolved`, while `SessionProgressHudCoordinator` presents state through the existing `GameStatusInfo` sliders.
- Satisfaction below 30 uses `#FF0032`; 30 and above restores the authored green. EXP preserves the authored orange fill.
- Threshold acknowledgement/reset is an explicit API reserved for future GameFlow. Reaching a threshold does not currently generate a Room or Resident, reveal Camera, pause, delay, or open Rewards.

Persistence, Reward catalogs/application, Target Selection, and final Game Over presentation are not established.

---

## Design Direction

Confirmed loop:

1. Resident Need resolved → EXP gained.
2. EXP Max → Level Up.
3. New Room generated and revealed.
4. Reward 3-choice presented; player picks 1.
5. Target Selection if needed.
6. Reward applied; game resumes.

Reward formulas, choice weights, effect magnitudes, exact Satisfaction gain/loss, EXP
scaling formula, and infinite scaling are **Open Decisions** pending balance tuning.

---

## Related Domains

- Resident / Demand: produces Satisfaction and EXP outcomes.
- Room Generation: is triggered by Level Up; provides RoomGenerated / RoomContentReady events.
- Camera Framing: performs Room Reveal zoom-out; provides CameraRevealCompleted event.
- Connection / Power: may receive capacity or equipment upgrades via Rewards.
- Infinite Mode: future GameFlow owns orchestration.

---

## Modification Cautions

- Do not invent balance values or progression formulas without a balance decision.
- Reward UI, presentation, and Target Selection require the Human Decision Gate.
- Do not add Rent, Currency, or separate Upgrade UI back without an explicit design decision.

---

## Narrow Search Order

1. `docs/decisions/infinity-progression-and-reward-loop.md` — authoritative design.
2. This map and the current Task.
3. Reward/progression sections of the concept PDF.
4. Related Domain Maps for integration boundaries.
5. Broaden only after a concrete implementation source exists.

---

## Planned, Not Established

Reward catalogs, upgrade application, Target Selection, GameFlow orchestration, save data,
and analytics are not established. Progression core and HUD binding have automated tests;
full player-facing flow still requires Human Verification.
