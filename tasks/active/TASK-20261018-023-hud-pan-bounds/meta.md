# Task Metadata

- **ID:** TASK-20261018-023
- **Title:** 상하단 UI 가용 화면과 연속 카메라 보정
- **Status:** active
- **Owner:** MoHoDu
- **Agent:** Claude Code
- **Domain:** camera-framing; infinite-mode
- **Base:** dev 계보의 checkpoint `fcd460175811922c351236278dc05acbbf2d3377`
- **Branch:** task/TASK-20261018-023-hud-pan-bounds
- **Started:** 2026-10-18
- **Updated:** 2026-10-19
- **Current Stage:** verification
- **Current Skill:** integrate-unity-editor
- **Worktree:** D:/github-worktrees/Octoplug/shared-wall-outlet (독립 Library 순차 재사용)

## Allowed Scope
- 승인 새 후속: 다음 Hint까지 포함하는 자동 Hint/Reveal의 margin만0으로 변경. 초기 fit/dynamic max/Pan margin2와 위치 보존·zoom-out-only 유지.
- Unity Zoom/Pan 및 관련 Editor 테스트: 2026-10-19 사용자 요청으로 PR 검토 P2(queued Pan 전 Reveal 즉시 완료)를 수정·검증 후 commit/push·PR 병합. 기존 UsableViewport 수식/Bridge/Scene/GameFlow 구현 변경 없음.
- camera-framing map, Task 문서와 TASK008/018 한정 소유권 기록. 상세 파일·검증은 plan.md.

## Do Not Modify
- 2026-10-19 사용자 명시 요청으로 현재 수정 commit/push 및 PR을 통한 dev 통합 허용. 원본·다른 Editor, 범위 밖 파일 변경/reset/force 금지.
- HUD/주민 카드 layout·크기·Raycast, 입력 capture/우선순위, 렌더 rect, Brain/Package/ProjectSettings 유지.
- 제품 위치/길이/밸런스, RoomGeneration/GameFlow 알고리즘, 기존 실패9건 유지.
- 기존 환경 migration4개와 폰트 에셋 변경 보존·전달 제외. raw Unity YAML 수정 금지.

## AI Setup Allowed
- 정확한 격리 Editor의 Scene 참조 wiring, 임시 검증 객체/가상 입력 허용; 검증 후 복원.

## Exclusive Assets
- TASK008에서 Camera PanBoundaryCalculator/UsableViewport 및 Unity Pan/Zoom/InputReader/Bridge와 해당 테스트를 이번 개선에 한정 순차 이관.
- TASK018에서 InfiniteMode.unity의 기존 HUD_Area/Residents_Area와 필요한 기존 카메라 controller 참조 연결만 이관. 다른 Scene/Telemetry 범위 유지, 동시 편집 금지.

## Human Decisions
- 기존 상단 HUD 수동 Pan 수정: 사용자 “잘 수정된 것 확인했어.” Human PASS.
- 2026-10-19 후속 계획 승인: 초기 1회 fit/정렬, 하단 Residents_Area 반영, manual zoom 연속 최소 보정과 첫 drag snap 제거.
- 기존 개선 Human PASS 및 checkpoint push 완료(7082cfa). 새 자동 줌은 “다음 Hint까지 포함” 선택/계획 승인: 추가 여백만 제거, 위치 보존 유지. 이후 현재 변경의 Git 전달도 명시 승인.

## Open Decisions
- 차단 결정 없음. 계획 밖 player-facing 정책이 필요하면 구현 전에 중단·확인.

## Verification State
- 이전 수정: core61/61, 집중 Pan22/22, 전체211/202 PASS/9 FAIL. Scene clean, 사용자 Human PASS.
- 후속 core178/178, 집중 Editor61/61 PASS; 전체244/235 PASS/기존9 FAIL. compile completed/참조3줄 diff. Scene 비교 동일 확인 후 저장, 테스트 차단 해제.
- Verification: HUMAN_VERIFY_REQUIRED(최소 Hint/Reveal). PR#1 P2 수정: red4/4→집중76/76, core178/178·RoomGeneration66/66 PASS; 전체259/250 PASS/동일9 FAIL. 후속 수정·검증·병합 사용자 승인, Human PASS로 소급하지 않음.
