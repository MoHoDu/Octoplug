# Plan

## Context / 승인
- 기존 상단 HUD Pan 수정 Human PASS 후, 최초 화면 fit·Residents_Area 제외·zoom 이후 첫 drag snap 제거를 요청받음.
- 2026-10-19 후속 계획 승인: `C:/Users/imvag/.claude/plans/magical-dazzling-kurzweil.md`.
- 같은 격리 worktree/branch에서 구현, 원본 미변경. Git 작업 없음. 기존 환경4개/폰트 변경 보존·제외.

## 구현
1. Pan의 기존 HUD world→screen→pixelRect 교집합을 상하단 공통화. optional bottomResidentsArea와 Zoom→Pan 참조를 명시 연결; 전역 서비스/이름 검색 없음.
2. 순수 UsableViewport helper에 top/bottom 비율·중앙fit·fixed-center fit 계산. null/disabled/outside는 해당 inset0, viewport0/전면가림은 대기.
3. 가용 Y offsets=[-S+2Sb,S-2St]. Pan Ymin=house.MinY-margin+S-2Sb, Ymax=house.MaxY+margin-S+2St. X 기존 유지.
4. 초기 Bridge 이벤트는 최신 unlocked/visible bounds로 병합. 시작 생성 및 Canvas layout 준비 후 최초 pre-render tick에 visible bounds(현재 Hint 포함)를 1회 fit/정렬.
5. S=max(minS,paddedWidth/(2aspect),paddedHeight/(2(1-t-b))); cameraY=boundsCenterY-S(b-t). Lens/target/velocity/range 함께 동기화. 이후 방 생성에는 자동 재중앙화 없음.
6. Zoom의 pre-Brain LateUpdate에서 layout/bootstrap→Lens smoothing→manual 경계복귀→수집된 Pan→Reveal 완료를 순차 처리. 실제 Brain executionOrder100보다 앞선 순서 검증; package/ProjectSettings 불변.
7. InputReader capture는 유지, production pixel delta를 모아 최신 Lens로 PanDeltaMapper 적용. 기존 public ApplyPanDelta(worldDelta) 즉시 계약 유지.
8. manual zoom 보정은 기존 unscaled smoothing 시간 재사용, 처음 범위 밖이어도 hard snap 없음. Lens가 먼저 정착하면 잔여 위치보정까지 마무리. Hint/Reveal 요청 시 위치복귀 중단.
9. 입력 dAllowed=Clamp(d,Min(0,L-current),Max(0,U-current)); 범위 밖 inward는 요청량만, outward/zero는0. collapsed에는 전체 콘텐츠가 보이는 containment 구간 보존; 안전하면 고정, 안전하지 않으면 inward 복구 허용.
10. 초기fit/manual 최소보정만 위치변경 허용. Hint/Reveal은 authoritative Cinemachine 중심으로 가용화면 fit, zoom-out-only/위치보존/완료이벤트 유지. invalid·불가능 fit은 유한현재값 유지 및 pending, 유효화면 복귀 때 재평가.
11. CameraMotionStarted는 매 smoothing tick마다 중복 발생시키지 않음. 기존 Tooltip dismiss 및 RevealCompleted→RewardDelay 유지.

## 수정 파일 / 소유권
- `Assets/01_Scripts/CameraFraming/PanBoundaryCalculator.cs`, 신규 `UsableViewport.cs`.
- `Assets/01_Scripts/CameraFraming.Unity/HouseCameraPanController.cs`, `HouseCameraZoomController.cs`, `CameraPanInputReader.cs`, `RoomGenerationCameraFramingBridge.cs`.
- 관련 `Tests/CameraFraming.Core.Tests/`, `Assets/Tests/Editor/CameraFraming/` 테스트 및 Unity 생성 .meta.
- `Assets/00_Scenes/Demo/InfiniteMode.unity`: 기존 상하단 UI 및 필요한 기존 카메라 controller 참조만 live Editor로 연결.
- TASK023 문서, TASK008/018 한정 소유권 meta, camera-framing map.
- 기존 DynamicZoomLimit/HintFramingCalculator API, UI layout/Raycast, renderer rect/Brain, RoomGeneration/GameFlow/Resident 알고리즘, 제품/밸런스 불변.

## Verification
- 기준: core61/61, 집중Pan22/22, 전체211 중202 PASS/동일9 FAIL. 이전 Human PASS와 새 개선 Human대기 분리.
- 최초 무입력 화면 및 Lobby→InfiniteMode, 초기 연속 Hint/Canvas 지연/재진입에서 최신 visible bounds 전체fit 확인.
- 상하단 비대칭,16:9/4:3,viewport/Canvas scale,null/inactive/한쪽/전면가림/resize/불가능 fixedfit 회귀.
- 실제 상하단 방 Plug drag→preview→release. 제품/케이블/UI변경 우회 금지.
- 양쪽 경계 zoom-in/out→1~2px tiny drag,zoom+drag,collapsed 진입/해제,Hint/resize 뒤 입력: 큰 추가변위 없음 확인.
- 실제 Input System→Player Loop, UI Down 차단/capture, touch/pinch억제, Reveal→RewardDelay 및 Tooltip 이벤트 검증.
- compile/Console/전체테스트/fast/context/diff, Scene clean/참조-only diff, 임시설정 복원 및 Play종료. Human Play 전 DONE 아님.
