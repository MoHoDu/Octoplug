# Task Checklist

## Setup
- [x] Create `GameFlowManager` script and define States enum.
- [x] Create `RewardPhaseRequested` and `GameOverRequested` events/actions.
- [x] Integrate into `InfiniteMode` Demo scene (Human verification passed).

## Implementation
- [x] **State Machine:** Implement Playing, RoomProgression, CameraReveal, RewardDelay, AwaitingReward, GameOver states.
- [x] **GameOver Flow:** Handle Satisfaction 0 → GameOver transition.
- [x] **EXP Threshold Flow:** Subscribe to `ExperienceThresholdReached`. Ensure single-fire execution.
- [x] **Room Progression:** Wire up `Hint Promote` logic on level up using existing Room API.
- [x] **Resident Addition:** Call `AddResident()` safely exactly once per level up.
- [x] **Camera Reveal:** Integrate existing Camera Reveal mechanism.
- [x] **Pause / Delay:** Implement unscaled 1-second pause delay.
- [x] **Reward Boundary:** Implement `CompleteRewardPhase()`.
- [x] **EXP Cycle Reset:** Acknowledge threshold, set next cycle `RequiredEXP`.
- [x] **Demand Cooldown:** Ensure demand generates again after 6 seconds cooldown.
- [x] **Input Lock:** Block world interactions during Reward/GameOver and dismiss Tooltip.
- [x] **Progression Source:** Replace temporary RequiredEXP with `DefaultRequiredExperience.cs`.

## Verification
- [x] Write `GameFlowManagerTests` for scenarios A-H.
- [x] Verify Demo Scene play flow manually (Human Verification PASS).
- [x] Validate standard regression (Power, Demand, Room, Camera).
