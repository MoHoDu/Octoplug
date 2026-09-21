---
id: TASK-20260922-015
slug: balance-sync-redistribution
status: IMPLEMENTATION
domain:
  - room-generation
  - power-connection
  - resident-demand
started: 2026-09-22
completed: 
---

## Objective
Implement a Google Sheet to Unity Local Balance Sync system and use it to drive Room-wide Product Redistribution.

## Boundaries
### Allowed Scope
- Editor tools for Google Sheet CSV download and parsing
- ScriptableObject local balance assets
- Production controllers to consume the balance assets
- Product redistribution logic upon room progression

### Do Not Modify
- Implement Telemetry
- Alter actual gameplay logic outside of balance consumer injection

### Exclusive Assets
- Assets/04_Data/Balance/*.asset (New)
