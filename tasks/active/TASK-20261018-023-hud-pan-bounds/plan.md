# Plan

## Context / 승인
- 초기 fit/Residents_Area/manual zoom 연속보정은 사용자 Human PASS. 선행 checkpoint `7082cfa72f094c52a63184d5e676b2adabac7a14` push 및 local/remote HEAD 일치 확인 완료.
- 2026-10-19 새 요청: 방 생성 시 과도한 축소 개선. 사용자는 자동 화면 대상에 **다음 Hint까지 포함** 선택, 최소 여백 계획 승인.
- 승인 계획: `C:/Users/imvag/.claude/plans/magical-dazzling-kurzweil.md`.
- 동일 격리 worktree/branch 유지. 환경4개/폰트 보존·제외. 구현 뒤 사용자 추가 요청으로 현재 변경 commit/push 및 dev PR 통합 승인. 원본 checkout 적용 없음.

## 구현
1. 자동 Hint/Reveal의 fixed-center fit에만 margin0 적용. 초기 fit/dynamic max/Pan margin2는 유지.
2. raw room bounds가 이미 가용 화면에 보이면 추가 축소 없음. 일부 가려지면 현재 중심에서 전체가 보이는 최소 size만 요청.
3. 위치 보존·zoom-out-only·smoothing 유지. 이미 커진 size를 자동 확대해 되돌리지 않는다.
4. `max(target,current Lens)` baseline 유지: 같은 프레임의 먼 다음 Hint 목표를 가까운 승격방 Reveal이 취소하지 않음.
5. invalid/impossible fit pending 및 queued Pan 뒤 Reveal 완료 재검사/완료1회 계약 유지.
6. `houseFramingMargin` tooltip을 초기 fit/수동 범위 용도로 정정. 새 serialized 필드/상태/Scene 설정 없음.

## 파일 / 소유권
- production: `Assets/01_Scripts/CameraFraming.Unity/HouseCameraZoomController.cs`만.
- Editor 테스트: `HouseCameraZoomControllerTests.cs`, `RoomGenerationCameraFramingBridgeTests.cs`.
- TASK023 문서 및 camera-framing map. 기존 TASK008/018 한정 이관 범위 유지.
- UsableViewport 수식/Bridge/RoomGeneration/GameFlow/Scene/UI/Package/ProjectSettings 변경 없음.

## PR 검토 후속 (2026-10-19 사용자 승인)
- Pan 큐가 비어 있지 않으면 Reveal 즉시 완료를 보류하고 기존 pre-Brain tick에서 Pan 적용 후 재판정한다. 큐가 없으면 기존 즉시 완료 유지.
- Zoom/Pan 코드와 Editor 회귀만 수정. 가려짐/가려지지 않음·smoothing·상쇄 입력을 검증하고 기존9실패와 Human대기는 별도 유지한다.
- 수정 전 회귀 실패→수정 후 집중/전체 검증, 코드 재검토 후 현재 PR에 commit/push 및 dev 병합. 환경4개/폰트·원본 workspace 제외.

## Verification
- 기준: core178/178, 집중 Editor61/61, 전체244/235 PASS/기존9 FAIL. 새 개선 Human 확인 별도.
- 이미 보임/padding만 화면 밖/상하단 부분 가림/반복 요청/비영 smoothing/먼 Hint→가까운 Reveal 같은 프레임 회귀.
- 기존 impossible pending fixture가 margin0에서도 실제 zoom을 요구하도록 bounds 보정. 초기/dynamic/Pan margin2 유지 회귀.
- 빈 임시 Scene에서 compile/집중/전체 Editor tests, core 재실행. 기존 실패9건은 수정 범위 밖.
- live Play에서 fresh 초기 진입·연속 실제 방 승격 경로의 bounds/중심/Lens/inset 측정, old margin2 대비 new0 비교. manual zoom/Pan 및 Reveal→RewardDelay 확인.
- 내부 setup 호출과 실제 InputSystem 입력 증거 구분. 검증 후 임시설정 복원/Play종료, Scene checkpoint 대비 불변 확인.
- fast/context/diff 검사 및 handoff 기록. 필요한 Hint 표시를 위한 축소는 유지됨을 안내, 새 개선 Human 확인 대기.
