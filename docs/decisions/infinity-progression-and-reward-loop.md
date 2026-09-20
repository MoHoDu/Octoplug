# Design Decision: Infinity Mode Progression & Reward Loop

**Status:** Confirmed  
**Date:** 2026-09-20  
**Task:** TASK-20260920-009  
**Supersedes:** Any prior documents describing Rent, Currency, separate Upgrade Phase,
or "buy reward" mechanics.

---

## 1. Rent / Currency — Removed

There is no Rent system.  
There is no Currency.  
Rewards have no purchase cost.  
Upgrades are not gated by currency spend.

Any roadmap item or domain description referring to "월세", "재화", "Rent 획득", or
"Buy/purchase upgrade" describes a superseded design and must not be treated as
authoritative.

---

## 2. Unified Reward System (no separate Upgrade Phase)

There is no distinct Upgrade Phase or Upgrade UI.

Every upgrade is a Reward. The Reward System is the single place where the player
receives new capabilities.

### Reward trigger

Every Level Up presents exactly 3 Reward candidates. The player selects 1. That
selection ends the Reward Phase.

### Reward Pool

The pool contains items from all three categories:

| Category | Examples |
|---|---|
| Basic Reward | House Allowed Power +1, Wall Outlet 추가, Cable Length +, etc. |
| Basic Upgrade (formerly separate) | PowerStrip Allowed Power +1, PowerStrip Socket +1/+2/+3, etc. |
| Composite Reward | Any two effects from the above combined into one pick |

The Reward System implementation is **not in scope for TASK-20260920-009**.

---

## 3. Reward Target Selection

Some Rewards apply immediately on selection (e.g. House Allowed Power +1).

Others require the player to designate a target before the effect is applied.

Examples of target-required Rewards:
- 특정 PowerStrip Allowed Power +1
- 특정 PowerStrip Socket 추가
- 특정 Wall에 Wall Outlet 추가

### Target Selection Flow

```
Reward 3-choice displayed
→ Player picks 1 Reward
→ Reward popup hides
→ Non-selectable world objects dimmed
→ Player taps/clicks target
→ Reward applied
→ Reward Phase ends
→ Game resumes
```

Supported target types (planned, not all implemented at once):
- PowerStrip
- Product
- Room
- Wall
- Wall position

Target Selection implementation is **not in scope for TASK-20260920-009**.

---

## 4. Gameplay Pause During Reward Phase

Gameplay is paused for the entire duration of the Reward Phase, including Target
Selection.

Gameplay resumes only after the selected Reward is fully applied.

---

## 5. Satisfaction

**Range:** 0 – 100 (fixed).

| Resident Need result | Satisfaction change |
|---|---|
| Need satisfied | +amount (balance TBD) |
| Need unsatisfied | −amount (balance TBD) |

**Satisfaction = 0 → Game Over.**

Exact gain/loss amounts are an **Open Decision** pending balance tuning.

---

## 6. EXP / Level Progression

EXP accumulates from Resident Need outcomes.

When EXP reaches the level threshold, Level Progression fires.

The required EXP for the next level increases as the total room count grows.  
Exact formula and scaling constants are an **Open Decision** pending balance tuning.

---

## 7. Confirmed Level-Up Flow (authoritative sequence)

```
Resident Need resolved
→ EXP increases
→ EXP Max reached
→ New Room generated / unlocked
→ New Room's required Products / Wall Outlets generated
→ Camera auto Zoom Out to reveal new Room
→ Game Pause
→ 1-second wait
→ Reward 3-choice displayed
→ Player selects 1 Reward
→ Target Selection if required
→ Reward applied
→ Game resumes
```

**Critical ordering constraint:**  
Room Generation and Camera Reveal **precede** the Reward Phase.  
The sequence is: Room Generation → Camera Reveal → Reward.  
Reward must never precede Room Generation.

---

## 8. System Responsibility Split

### Room Generation is responsible for

- Room planning (RoomPlan, RoomBounds2D, candidate ordering)
- Hint room state (HintLocked / UnlockedGenerated)
- Promoting the stored hint plan on unlock
- Planning the next hint after promotion
- Door planning and application
- Room content generation hooks (Products, Wall Outlets inside room)
- Emitting Room Generation events for external orchestration

### Room Generation is NOT responsible for

- EXP calculation
- Level Up judgment
- Reward phase start
- Satisfaction calculation
- Game Over judgment

### Camera is responsible for

- Framing / zoom when a Room Reveal is requested
- Dynamic House bounds tracking for Pan and Zoom limits
- Hint framing (zoom-only, position-preserving)

### Camera is NOT responsible for

- Deciding when to reveal a room (that is GameFlow's trigger)

### Future GameFlow / GameManager is responsible for

```
EXP Max reached
→ Request Room Generation
→ Confirm Room content ready
→ Request Camera Reveal
→ Confirm Reveal complete
→ Pause game
→ Wait 1 second
→ Start Reward phase
→ Confirm Reward applied
→ Resume game
```

GameFlow implementation is **not in scope for TASK-20260920-009**.

---

## 9. Future GameFlow Hooks (boundary catalogue)

The following event/API surface must be available for GameFlow orchestration.
Exact names follow the production code style at implementation time; these are
intent labels, not final identifiers.

| Hook | Direction | Intent |
|---|---|---|
| `RoomGenerated` | Room Generation → GameFlow | A new room instance exists in the scene |
| `RoomUnlocked` | Room Generation → GameFlow | The hint room was promoted to unlocked |
| `RoomContentReady` | Room Generation → GameFlow | Products / Wall Outlets inside the room are ready |
| `NextHintCreated` | Room Generation → GameFlow | The next locked hint room has been planned and placed |
| `CameraRevealCompleted` | Camera → GameFlow | Zoom-out to show new room is finished |

GameFlow must be able to orchestrate the Level-Up sequence through these events/APIs
without polling the Scene hierarchy.

Room Generation and Camera must remain unaware of EXP, Level, Satisfaction, or Reward state.

---

## 10. Superseded Items

The following descriptions from prior documents no longer reflect the authoritative design:

| Prior description | Status |
|---|---|
| "Level up and receive rent based on residents" | **Superseded** — no rent |
| "Buy or upgrade a reward" | **Superseded** — Rewards are free on Level Up |
| "Upgrade Phase" / "Upgrade UI" | **Superseded** — absorbed into Reward System |
| "재화 소비를 통한 Upgrade" | **Superseded** — no currency |
| "Rent 획득" | **Superseded** — no rent |
| Roadmap item "Satisfaction / Level Up → Rent 획득" | **Superseded** |

Historical task logs (`tasks/done/`, `log.md` files) are preserved and not rewritten.
