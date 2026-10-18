# Task Metadata

- **ID:** TASK-20261018-022
- **Title:** 전력 부족 드래그 스냅과 드롭 판정 분리
- **Status:** active
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** connection-power
- **Base:** dev + TASK021 미커밋 수정
- **Branch:** task/TASK-20261018-021-shared-wall-outlet (승인된 순차 재사용 예외)
- **Started / Updated:** 2026-10-18
- **Current Stage:** verification
- **Current Skill:** integrate-unity-editor
- **Worktree:** D:/github-worktrees/Octoplug/shared-wall-outlet (기존 독립 Library 재사용)

## Allowed Scope
- CableRoutingController.cs의 hover 후보 전력 검사 분리 및 release 거절 위치 보존.
- SocketPreviewBudgetTests.cs/.meta 신규, connection-power Map, TASK021 Human PASS 및 Task 문서.

## Do Not Modify
- 원본 파일/Play, merge/원본 적용, Scene/Prefab/밸런스/길이/스냅 수치.
- 2026-10-18 사용자 요청으로 TASK021/022 체크포인트 stage/commit/push 허용. Human PASS/DONE 선언은 아님.
- PowerValidationService, PlugSocketConnection, Audio/Telemetry/생성 코드 및 선행 수정 내용.
- 무관한 기존 테스트9건, 다른 Task 범위.

## AI Setup Allowed
- 격리 Editor의 임시 검증 객체·InputSystem 가상 Mouse, 종료 시 원상복구.

## Exclusive Assets
- CableRoutingController.cs와 connection-power Map은 TASK021 후속으로 순차 수정.
- 게임 serialized asset 변경 없음. 선행 환경 migration4개는 보존/전달물 제외; 추가 변경은 중단·보고.

## Human Decisions
- 2026-10-18: 전력 부족이어도 hover snap 허용, drop 시 전력 판정 및 기존 밀어내기 유지.
- 후속 계획 승인: C:/Users/imvag/.claude/plans/magical-dazzling-kurzweil.md.
- TASK021 정상 동작 확인은 이전 수정의 PASS이며 이번 변경의 PASS는 아님.

## Open Decisions
- 없음.

## Verification State
- 구현 완료. 신규18/18·공유벽11/11·생성14/14 PASS. 전체191 중182 PASS/9 FAIL(이전과 같은9건). 실제 집/멀티탭 Input Entry PASS.
- Verification: HUMAN_VERIFY_REQUIRED
