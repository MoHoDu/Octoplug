# Task Metadata

- **ID:** TASK-20261018-021
- **Title:** 공유 벽 콘센트 양방향 연결
- **Status:** active
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** connection-power
- **Base:** dev
- **Branch:** task/TASK-20261018-021-shared-wall-outlet
- **Started / Updated:** 2026-10-18
- **Current Stage:** verification
- **Current Skill:** integrate-unity-editor
- **Worktree:** D:/github-worktrees/Octoplug/shared-wall-outlet (독립 Library)

## Allowed Scope
- 공유 벽 양측 routing 및 Room3+ base-product 공유벽 도달 우선 생성 위치 보정, own-room fallback.
- 대상: routing 코드 3개, `RoomContentGenerationController.cs`, Power/ProductionRoomGeneration Editor 테스트.
- connection-power/room-generation Domain Map, Task 문서 및 TASK-018 한정 소유권 이관 기록.

## Do Not Modify
- 원본 workspace/Play 상태, Scene/Prefab/Collider/시각 효과, 전력·길이 수치, Telemetry, 방 형상·문 생성.
- TASK-008/019 및 TASK-018의 기타 범위; starter/redistribution 생성 정책은 유지.
- stage/commit/push/merge/원본 적용, 새 Editor 설치, 원본 미커밋 설정 복사 금지.

## AI Setup Allowed
- 후속 검증 시 독립 worktree의 임시 테스트 객체만; 게임 자산 직접 편집 없음.
- 설치된 6000.3.25f1 사용 예정(dev 6000.3.9f1); 자동 migration은 환경 변경으로 별도 기록·전달물 제외.

## Exclusive Assets
- TASK-018 → TASK-021 한정 이관: `Assets/01_Scripts/Power/Routing/CableRouteReachability.cs`, `Assets/01_Scripts/RoomGeneration.Unity/RoomContentGenerationController.cs`, `Assets/Tests/Editor/RoomGeneration/ProductionRoomGenerationControllerTests.cs`. Telemetry 불변.
- 아래 4개만 검증 Editor 자동 갱신에 조건부 예약(이 worktree 환경 전용):
- `ProjectSettings/ProjectVersion.txt`, `Assets/UniversalRenderPipelineGlobalSettings.asset`
- `Packages/manifest.json`, `Packages/packages-lock.json`
- 그 외 자산 자동 변경 또는 타 Task 소유 파일 수정 필요 시 먼저 보고·소유권 확인.

## Human Decisions
- 2026-10-18: 공유 벽 동일 Socket에 양쪽 방 직접 연결 승인; single occupancy·Door-only 유지.
- 후속 생성 위치 보정·shared 우선/own fallback·범위 정정·생성 코드/테스트 한정 이관 승인: `C:/Users/imvag/.claude/plans/magical-dazzling-kurzweil.md`.

## Open Decisions
- 없음. 후속 계획 승인: 길이 유지, Room3+ 공유벽 도달 위치 우선/불가능 시 own-room fallback, 자기방 동시 도달 필수 제외.

## Verification State
- 후속 생성 위치 수정 완료. 생성14/14·routing11/11 PASS; 전체164/173 PASS(9 FAIL). 실제 생성 TV length3/공유벽 경로1.442165 및 Player Loop 입력 연결 성공. 사용자 정상 동작 확인 완료. 후속 승인으로 범위 문구 정정 완료.
- Verification: HUMAN_PASS — 2026-10-18 사용자 “좋아 수정되었어.” 확인. Git 마무리 미요청으로 active 유지.
- 후속 TASK022에 CableRoutingController.cs와 connection-power Map 순차 수정 이관. 기존 worktree/branch 재사용 승인; 선행 변경 보존.
