# InfinityMode Demo Roadmap

## Goal

- InfinityMode에서 핵심 코어 루프를 플레이 가능한 상태로 완성
- 최종 목표: 요구 → 전력 연결 → 만족도 → 레벨업 → 방 확장

## Current

- [x] Grid 기반 8방향 Cable Routing
- [x] Plug Drag
- [x] Socket 연결/해제
- [x] Wall Outlet 연결
- [x] House Power Validation
- [x] Powered / Power Flow
- [x] ProductInfo / Tooltip
- [ ] PowerStrip
- [ ] Resident Demand
- [ ] Satisfaction / Level Up
- [ ] Room Generation Integration
- [ ] Reward
- [ ] Demo Polish

## Execution Order

### 1. PowerStrip
- Head Drag
- Room 내부 이동 제한
- Wall Outlet → PowerStrip 연결
- Product → PowerStrip Socket 연결
- PowerStrip Allowed Power

### 2. Resident Demand
- 요구 생성
- UsageType 연결
- 대기 / 사용 상태
- 요구 성공 / 실패

### 3. Satisfaction / Level Up
- 만족도 증감
- Level Gauge
- Level Up
- Rent 획득

### 4. Merge Room Generation Core
- branch: feat/infinity-room-generation-core
- pure logic 검증 후 merge
- Scene / Prefab 변경 없는지 확인
- Main Grid와 호환 확인

### 5. Room Generation Integration
- HintLocked Room
- Level Up → Unlock
- Room prefab 생성
- DoorPlan 적용
- Grid rebuild
- 다음 Hint 생성

### 6. Reward
- Reward UI
- Upgrade 선택
- 대상 선택
- Cable Length / Power / Socket 강화

### 7. Demo Polish
- Settings
- Result
- Best Score
- Sound
- Deferred visual TODO

## Parallel Work

### Main Worktree
branch: feat/infinity-power-connection

- PowerStrip
- Resident
- Satisfaction / Level Up

### Room Generation Worktree
branch: feat/infinity-room-generation-core

- Room placement core
- Door planning
- Locked / Unlocked state

### Merge Point
Satisfaction / Level Up 완료 직후 Room Generation Core merge