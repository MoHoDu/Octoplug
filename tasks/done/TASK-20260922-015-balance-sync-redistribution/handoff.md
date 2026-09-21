## Implemented
- Google Sheet 9개를 Editor에서만 download/parse/validate하고, 모두 유효할 때만 `Assets/02_Resources/Resources/Balance/GameBalanceArchive.asset`을 원자적으로 갱신합니다.
- Runtime/build는 `BalanceRegistry`의 local archive만 사용하며 Google에 접속하지 않습니다.
- Reward의 미지원 `AddWallOutlet` 행은 zero-effect record를 만들지 않고 제외합니다.
- inactive staged Product도 authored `BoxCollider2D` bounds로 placement/reservation을 계산하며, Product/Product 및 Product/PowerStrip 양의 면적 겹침을 막습니다.

## Base Room Content
- 모든 방은 기본 Product 정확히 1개와 Wall Outlet 정확히 1개가 필수입니다.
- Starter Room 1/2는 `초기 구성`의 Fan/TV를 유지합니다.
- Room 3+는 `제품 등장 풀`에서 enabled, positive weight, min-room, prefab, house-power 조건을 만족하는 Product 하나를 가중 선택합니다.
- 선택 타입을 배치할 수 없으면 같은 방에서 그 타입을 제거하고 남은 pool을 재선택하며, 첫 성공 후 종료합니다.
- `방 구성`의 Product count 열은 runtime 기본 생성 수량으로 사용하지 않습니다. RoomConfig는 선택 가중치와 Wall Outlet socket 범위 등에 사용됩니다.

## Transaction / Redistribution
- Product 또는 Wall Outlet 생성 실패 시 해당 호출에서 만든 equipment를 모두 rollback합니다.
- Promotion 실패 시 이전 `RoomGenerationState`와 같은 locked hint를 복원하고 unlock/readiness/next-hint event를 발행하지 않습니다.
- Redistribution은 기본 Content 성공 후 별도로 실행합니다.
- `n`개 distinct room을 Product 수 오름차순, 면적 내림차순, RoomId 순으로 선택하고 각 방에 최대 하나를 추가합니다.
- 물리 제약으로 배치할 수 없는 방은 shortfall을 기록하며 다른 방으로 전가하지 않습니다.

## Automated Verification
- Unity compilation: PASS, error 0.
- Balance registry: 3/3 PASS.
- Placement/redistribution: 16/16 PASS.
- Production room-generation integration: 4/4 PASS (Starter 두 방 exact-one 포함).
- `scripts/verify-fast.ps1`: PASS (context warnings only).
- `git diff --check`: whitespace error 없음. Line-ending warnings만 존재합니다.

## Human Verification
- 2026-09-22 사용자 Play Mode 검증 PASS: 변경 사항 관련 에러 없음.
- Starter/Room 3~5 기본 Content, redistribution, placement/reachability/power 동작을 승인했습니다.

## Status
DONE — 자동 검증과 사용자 Play Mode 검증을 통과했습니다.
