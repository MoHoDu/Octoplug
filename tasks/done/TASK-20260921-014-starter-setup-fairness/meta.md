---
id: TASK-20260921-014
slug: starter-setup-fairness
status: COMPLETED
domain:
  - room-generation
  - power-connection
started: 2026-09-21
completed: 2026-09-21
---

## Objective
Implement early game setup rules to ensure fairness in procedural content generation, including safe placement of starter products (Fan, TV), preventing door/outlet collision, and validation of power feasibility.

## Context
Google Sheet balance updates provided a "초기 구성" (starter setup) tab. Products frequently spawn where they cannot reach a power outlet, overlap with existing doors, or exceed the starting power capacity.

## Boundaries
### Allowed Scope
- RoomContentGenerationController.cs
- ProductionRoomGenerationController.cs
- RoomPlanner.cs
- DoorPlanningOptions.cs
- DefaultRoomContentBalance.cs

### Do Not Modify
- HousePowerBudget default configuration/logic (except querying)
- Prefab TV/Fan/AirConditioner power consumption directly in prefabs
- Other progression logic

### Exclusive Assets
- None

## Status / Handoff
HUMAN_VERIFY_REQUIRED
