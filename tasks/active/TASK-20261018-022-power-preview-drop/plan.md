# Plan

## Context
- 집/멀티탭 전력이 부족하면 후보 탐색 단계에서 제외되어 드래그 스냅도 되지 않는다.
- 사용자 결정: preview는 허용, release에서만 현재 전력을 검사해 초과 시 기존처럼 밀어낸다.

## Approach
1. TryFindNearestSocket 및 recentlyDetachedSocket fallback의 조기 전력 검사만 제거.
2. active/free·거리·접근측·경로·길이·graph 검사는 유지, 가장 가까운 preview 후보를 release에서도 판정.
3. 최종 TryValidateGraphConnection→TryValidate→Connect를 유지. preview는 pairing/power/사용량/SFX 불변.
4. TryConnectToNearbySocket bool 연결 성공 계약 유지 + out bool rejectionPositionApplied.
5. ApplyPowerRejectFallbackPosition/RejectConnection은 실제 위치 적용 성공 여부 반환. ResolveDragEnd는 성공한 밀어내기만 일반 fallback 덮어쓰기에서 보호.
6. 밀어내기 불가 시 일반 fallback 유지. no-drift 연결 유지/이동 후 release 분리 정책 불변.

## Files / Boundaries
- 수정: Assets/01_Scripts/Power/Cable/CableRoutingController.cs.
- 신규: Assets/Tests/Editor/Power/SocketPreviewBudgetTests.cs 및 Editor 생성 meta.
- 문서: connection-power Map, TASK022 문서, TASK021 사용자 PASS/순차 소유권 기록.
- 참조 재사용: PowerValidationService, PlugSocketConnection, UnityVerificationFixture, SFX verification override. 수정 제외.
- TASK021 작업 공간·branch 순차 재사용 승인; 원본/commit/push/자산·전력 수치 변경 없음.

## Verification
- 집/직접·상위 멀티탭/부하가 있는 멀티탭 전력 부족 preview 및 release 거절.
- preview 순수성, 가까운 부족 소켓 선택, release 최신 예산·점유·활성 상태 재검사.
- 전체 drag-end 이후 밀어내기 위치/경로 유지, 불가 시 fallback, 정상 연결/no-drift/이동 후 거절.
- 기존 graph/길이/외벽/Door-only/공유벽·생성 회귀.
- 실제 InputSystem/Player Loop 입력 검증과 API/Reflection Logic Test는 구분.
- compile/Console/관련·전체 EditMode/fast/context/diff. 기존 기준164/173과 비교, 실패 그대로 보고.
- 자동 검증 후 HUMAN_VERIFY_REQUIRED. 사람 확인 전 DONE/원본 적용 없음.
