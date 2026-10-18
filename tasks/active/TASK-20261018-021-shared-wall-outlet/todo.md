# Todo

## 준비 / Routing
- [x] 양측 동일 Socket 정책 승인, 독립 worktree/Library 및 환경 migration 4개 분리.
- [x] 양측 국소 경로 비교와 후보/preview/drop/reconnect 포인터 판정 통일.
- [x] single occupancy·Door-only·외벽 거부·길이 제한 유지, routing11/11 PASS.
- [x] 후속 계획 승인으로 문서 전용 범위 오기 정정 및 TASK018 생성 코드/테스트 한정 이관.

## 생성 위치 후속
- [x] 실제 생성 TV length3 vs shared 경로3.131505 실패 확인; 이전 근거리 임시 TV 검증 누락 기록.
- [x] Room3+ shared 우선/own fallback 구현, starter·redistribution 정책 유지.
- [x] 길이·스냅 수치·Scene/Prefab 변경 없이 기존 footprint/충돌/rollback 재사용.
- [x] 실제 생성 TV 및 shared 없음/점유/비활성/비공유/유효 위치 없음 회귀 추가.
- [x] 두 Domain Map 및 Task 계획 갱신.

## Verification
- [x] Refresh/compile 오류 없음.
- [x] ProductionRoomGeneration14/14 PASS; 전체 EditMode164/173 PASS,9 FAIL,0 skipped.
- [x] 잔여 실패: SFX EditMode7, StarterPower1, GameFlow1. 수정 전 baseline 미실행; 기존 실패라고 확정하지 않음.
- [x] InfiniteMode 실제 Room3 TV origin(-0.625,4.125), length3, shared 경로1.442165/후보 성공. 위치·길이 변경 없이 검사.
- [x] 실제 InputSystem→Player Loop→PlugDragInput→preview→drop: 생성 TV 후면 연결·전력, 분리, 기존 Fan 전면 연결·재드래그 복귀 성공. Human 시각 확인과 구분.
- [x] 최종 verify-unity -Compile PASS(up_to_date/clean/stopped), runtime Console 새 오류0, fast PASS/context WARN(hard 미만)/diff --check PASS.
- [x] Human Play Check: 2026-10-18 사용자 “좋아 수정되었어.” 정상 동작 확인. 세부 조작별 결과는 별도 진술되지 않음.
- [ ] 원본 적용: 별도 승인 필요, 미적용.

## 제외
- 무관한 전체 테스트9건 수정, TASK019 압축, 두 번째 버그 및 튜토리얼 미착수.
- 원본 파일/Play 제어·stage/commit/push/merge 없음.
