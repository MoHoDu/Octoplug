# Implementation Plan: InfiniteMode Progression GameFlow

## 1. GameFlow State Machine
- Create `GameFlowManager` (MonoBehaviour) handling authoritative states:
  - `Playing`
  - `RoomProgression`
  - `CameraReveal`
  - `RewardDelay`
  - `AwaitingReward`
  - `GameOver`
- Implement robust state transitions to prevent overlap or re-entry into progression if already processing.

## 2. Dependencies & Subscriptions
- Subscribe to `SessionProgressState.ExperienceThresholdReached`.
- Subscribe to `SessionProgressState.SatisfactionDepleted`.
- Reference/Find existing singletons or inject dependencies:
  - `RoomGenerationManager` (for `PromoteHintToRoom`)
  - `ResidentDemandController` (for `AddResident`)
  - `HouseCameraPanController` / `HouseCameraZoomController` / `CameraRevealRequest` mechanism.
  - `SessionProgressState` (for acknowledging/resetting EXP threshold).

## 3. The Level Up Flow Sequence
When `ExperienceThresholdReached` fires:
1. Validate state is `Playing`. Transition to `RoomProgression`.
2. Acknowledge/reset EXP to prepare the next cycle (carry over logic uses SessionProgressState if available, or just retain `CurrentEXP -= RequiredEXP`). Set new `RequiredEXP` based on new `RoomCount`.
3. Fetch the current Hint Room.
4. Promote the Hint Room using existing Room API (expects `RoomUnlocked`, `RoomContentReady`, `NextHintCreated` events/flow). Ensure Wall Outlets and Products spawn based on Room config.
5. Add exactly one Resident (`ResidentDemandController.AddResident()`).
6. Transition to `CameraReveal`.
7. Request Camera Reveal targeting the new Room bounds.
8. Await `CameraRevealCompleted` signal.
9. Transition to `RewardDelay`.
10. Trigger Game Pause (Time.timeScale = 0 or custom pause).
11. Wait for 1 second unscaled time (`WaitForSecondsRealtime(1f)`).
12. Transition to `AwaitingReward`.
13. Emit `RewardPhaseRequested` event.

## 4. The Reward Boundary
- Provide `CompleteRewardPhase()` API for future use.
- When called:
  - Resume Game (Time.timeScale = 1).
  - Transition state back to `Playing`.

## 5. GameOver Flow Sequence
When `SatisfactionDepleted` fires:
1. Check if Satisfaction == 0.
2. Transition to `GameOver` state.
3. Ignore future EXP thresholds or demand updates (stop gameplay loop).
4. Emit `GameOverRequested` event.

## 6. Debug / Inspector Setup
- Add `[Header("Debug")]` and read-only fields/properties in `GameFlowManager` to track: current state, current EXP, required EXP, room count, etc.
- Add ContextMenu or Inspector button to trigger `CompleteRewardPhase()` manually.

## 7. Automated Tests
- Create `GameFlowManagerTests`.
- Use existing Unity Test Framework (EditMode or PlayMode depending on time dependencies). Since there is a 1-second unscaled delay, PlayMode tests with `Time.timeScale` manipulation might be needed, or we mock the Coroutine runner/time.
- Test scenarios A-H as defined in the prompt.
