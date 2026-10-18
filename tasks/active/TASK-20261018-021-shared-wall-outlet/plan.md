# Plan

## 승인 및 원인
- 2026-10-18: 동일 공유벽 Socket 양측 연결 승인. single occupancy·일반 Door-only 유지.
- 후속 사용자 선택: 생성 위치 보정. 실제 상단 Room3 TV length3에 공유벽 경로3.131505로 길이가 부족했다.
- 승인 원문: `C:/Users/imvag/.claude/plans/magical-dazzling-kurzweil.md`.
- 임시 근거리 TV 검증만으로 실제 생성 문제를 해결했다고 판단하지 않는다.

## Routing
1. terminal 앞·뒤 국소 접근 셀에 각각 A*, 실제 렌더링 경로가 짧은 쪽 선택; 동률 전면.
2. 후보·preview·drop·최근 분리 fallback에 Grid 기반 포인터 측 판정 사용.
3. Socket은 마지막 endpoint일 뿐 방간 통과 지점이 아니다. 외벽 Unknown 접근 거부, Wall 상태 불변.

## 생성 위치 후속 수정
1. Room3+ base-product만 기존 공유벽 Socket 도달 위치 우선 탐색. starter·redistribution은 자기방 전면 유지.
2. RoomGeometry와 owner registry로 실제 공유벽을 찾고 구간·법선 offset·ApproachDirection·active/free를 검사.
3. 기존 exhaustive placement callback의 footprint/Walkable/reservation/overlap 조건 그대로 사용.
4. 공유벽 경로는 새 방 측 종점 접근 및 초기 케이블 길이를 검사한다.
5. 공유벽 유효 배치가 없으면 자기방 terminalFrontOnly fallback. 양쪽 콘센트 동시 도달은 요구하지 않는다.
6. 길이·스냅 수치·기존 제품 위치·방 형상·문 생성·전력 조건 변경 없음. 실패 시 기존 finalize/rollback 유지.

## 범위 / 소유권
- Routing 3개 코드, RoomContentGenerationController.cs, Power/ProductionRoomGeneration Editor tests, 두 Domain Map 및 Task 문서.
- TASK018 routing helper와 생성 코드/테스트 2개를 이번 수정에 한정 이관. Telemetry/registry/planner 동작 불변.
- 후속 승인에 meta의 잘못된 문서 전용 범위 정정 포함; 정정 완료.
- 독립 worktree `D:/github-worktrees/Octoplug/shared-wall-outlet`, branch `task/TASK-20261018-021-shared-wall-outlet`.
- 모든 Unity 명령에 정확한 프로젝트 경로 지정. 원본 파일·Play 제어·stage/commit/push/merge/원본 적용 없음.
- Scene/Prefab/Collider/밸런스 자산 변경 없음. 환경 migration 4개(ProjectVersion/URP global/manifest/lock)는 전달물 제외.
- dev6000.3.9f1, 격리 검증6000.3.25f1. 새 설치/원본 미커밋 설정 복사 없음.

## Verification
- 실제 생성 Room3 TV, length3 유지, 위치 변경 없이 공유벽 candidate→commit→power→disconnect/reconnect 검사.
- 공유 소켓 없음/점유/비활성/비공유벽/배치 공간 없음 fallback, starter own-front, Room4 및 rollback 회귀.
- 기존 routing11 cases(4방향·양측·sub-cell·길이·점유·외벽·Door-only) 유지.
- 생성 테스트의 SFX 출력만 기존 verification override로 격리; production Audio 미수정.
- 실제 InfiniteMode 생성 TV 및 Input Entry는 별도 검증. 함수 직접 호출은 실제 입력 PASS가 아니다.
- compile/Console delta/전체 EditMode/fast/context/diff 검증. 실패·미실행·환경 차이 그대로 기록.
- 사용자 실제 드래그 확인 전 HUMAN_VERIFY_REQUIRED; 원본 적용은 별도 승인 후. 두 번째 버그/튜토리얼 제외.
