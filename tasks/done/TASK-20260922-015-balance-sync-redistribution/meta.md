---
id: TASK-20260922-015
slug: balance-sync-redistribution
status: DONE
domain:
  - room-generation
  - power-connection
  - resident-demand
started: 2026-09-22
completed: 2026-09-22
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
- Assets/02_Resources/Resources/Balance/GameBalanceArchive.asset (New, synchronized local runtime balance)

## Human Decisions
- 2026-09-22: 모든 새 방은 기본 Product 정확히 1개와 Wall Outlet 정확히 1개를 생성한다.
- 2026-09-22: Starter Room 1/2는 `초기 구성`의 Fan/TV를 유지한다.
- 2026-09-22: Room 3+의 기본 Product는 `방 구성`의 Product count가 아니라 `제품 등장 풀`에서 정확히 하나 선택한다.
- 2026-09-22: Redistribution은 기본 Content 이후 별도 단계이며, 결정된 `n`개의 서로 다른 방에 각각 최대 하나씩 추가한다.

## Open Decisions
- 플레이 모드에서 Room 1~5의 exact-one 기본 Content와 Redistribution을 사람이 최종 검증해야 한다.
