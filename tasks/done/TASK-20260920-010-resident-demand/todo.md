# Todo

## Completed

- [x] Import and validate enabled Demand rows, including room-15 two-Need entries.
- [x] Select only feasible Demands from gameplay-room Product UsageTypes with no fallback.
- [x] Preserve one-Product-per-Resident, Capacity, stable Waiting/FIFO, and USING/WAITING transitions.
- [x] Track authoritative per-resident Satisfaction/Patience and resolve success/failure exactly once.
- [x] Bind Resident Cards and the authored 20-slot Product UseInfo pool without icon cloning.
- [x] Add authoritative Global Satisfaction and success-only EXP from exact Demand row values.
- [x] Configure explicit RequiredEXP rows for RoomCount 1–15 with no runtime formula.
- [x] Bind Satisfaction/EXP HUD, including authored green at 30+ and `#FF0032` below 30.
- [x] Publish one-shot `SatisfactionDepleted` and `ExperienceThresholdReached` signals.
- [x] Preserve explicit GameFlow-owned threshold acknowledgement/reset API.
- [x] Pass official pure-core and Unity EditMode verification.
- [x] Record direct Human Verification PASS for player-facing Resident Demand and HUD behavior.
- [x] Update domain and task records.

## Deferred to TASK-011

- [ ] InfiniteMode progression GameFlow: Room promotion, geometry/content readiness, Camera reveal, EXP reset/RequiredEXP update, Resident +1, next Hint, Reward boundary, and GameOver boundary.
