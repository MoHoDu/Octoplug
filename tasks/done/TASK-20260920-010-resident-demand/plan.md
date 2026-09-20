# Plan

## Completed Approach

1. Import and validate Demand balance rows, then generate only feasible resident Demands from active gameplay-room Product UsageTypes.
2. Keep connection, power, compatibility, Capacity, stable ordering, and FIFO authoritative in the Resident Demand controller.
3. Present per-resident Need/Satisfaction/Patience and per-Product usage snapshots through authored UI without cloning icons.
4. Apply resolved outcomes to one authoritative session progression state: exact row Satisfaction deltas, success-only EXP, 0–100 clamp, and one-shot depletion/threshold events.
5. Consume explicit RoomCount→RequiredEXP configuration (Room 1=20 through Room 15=160) with no runtime formula.
6. Bind authoritative normalized Satisfaction/EXP to the existing HUD, preserving authored colors except the approved `<30` Satisfaction danger state.
7. Wire serialized assets only through the matching isolated Unity Editor and verify pure core, Unity EditMode, production Play Mode, and repository checks.

## Preserved Boundaries

- Resident Demand core never mutates UI.
- Threshold and depletion events publish conditions only; TASK-010 does not orchestrate Room generation, Resident addition, Camera reveal, pause/delay, Reward, Result, or Restart.
- `AcknowledgeExperienceThreshold(newRoomCount, resetExperience)` remains the explicit future GameFlow boundary.
- Full progression orchestration continues in TASK-011.
