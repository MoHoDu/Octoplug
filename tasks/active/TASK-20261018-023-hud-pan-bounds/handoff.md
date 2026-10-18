# Handoff

## Status
- Current stage: checkpoint. 초기 fit/Residents_Area/manual zoom 연속보정에 사용자 “문제가 잘 해결되었어” Human PASS 및 현재 수정 push 명시 요청.
- 2026-10-19 기존 개선을 먼저 checkpoint한 뒤 별도 최소 Hint/Reveal 개선 예정. 새 개선 코드는 이 checkpoint에 포함하지 않는다.
- Branch: `task/TASK-20261018-023-hud-pan-bounds`; Base: `fcd460175811922c351236278dc05acbbf2d3377`.
- Worktree: `D:/github-worktrees/Octoplug/shared-wall-outlet`; 원본 `D:/github/Octoplug` 미변경.
- 선행 TASK021/022 checkpoint fcd4601만 origin/task/TASK-20261018-021-shared-wall-outlet에 push 완료. TASK023 stage/commit/push/dev merge 없음.

## 후속 구현 / 자산
- Core `PanBoundaryCalculator.cs`: 상하단 inset, delta-aware 입력, collapsed containment 및 최근접 안전좌표. 기존4/5인자 API 유지.
- 신규 `UsableViewport.cs`: 초기 fit/비대칭 중심, 위치 고정 zoom-out fit, invalid/impossible/overflow 처리. 관련 core 테스트 추가.
- Unity Pan: HUD/Residents 실제 screen 교집합 공유, raw pixel queue를 최신 virtual Lens로 변환. world-delta 즉시 API 유지.
- Unity Zoom: pre-Brain LateUpdate에서 초기 fit → Lens → manual 위치 smoothing → Pan → Reveal 완료. Hint/Reveal은 위치 보존, 불가능한 fit은 pending(유효 viewport의 Pan 복구는 허용).
- InputReader는 capture/우선순위 유지. Bridge는 시작 이벤트의 최신 visible bounds를 첫 tick까지 병합하고 이후 Hint 계약으로 복귀.
- Editor Pan/Zoom/Bridge 회귀 작성. 이 테스트의 내부 호출은 실제 Input Entry 증거가 아니다.
- `InfiniteMode.unity`: live Editor SerializedObject로 기존 topHudArea에 더해 bottomResidentsArea/Zoom.cameraPan 연결. 저장 직후 clean 및 zoom order0 < Brain order100 확인. 디스크 diff는 참조3줄만(이전 top1+후속2).
- docs camera-framing, TASK023, TASK008/018 한정 소유권 갱신.

## 후속 검증 결과
- 순수 CameraFraming 재실행178/178 PASS, 0 FAIL/0 skip(이전61 대비117 추가). 담당 strict build 경고0/오류0.
- matching Editor `verify-unity -Compile`: completed, 컴파일 오류 없음. 이 결과는 Editor 테스트 통과가 아니다.
- 재연결 후 집중 CameraFraming61/61 PASS. 전체244/235 PASS/9 FAIL/0 skip; 이전211/202/9 대비33개 추가 통과, 아래 기존9개 동일.
- 이전 run_tests timeout/저장 모달/Save 권한 거부 후 안전하게 취소했었다. 이번 승인 계획에서 복구: live SaveScene(saveAsCopy:true) scratch 사본과 디스크 Scene이 동일함을 git diff --no-index exit0으로 확인, 소유 Scene 저장 후 빈 임시 Scene에서 테스트 성공.
- 원래 Scene 디스크는 참조3줄만 유지. 강제종료/discard/raw YAML 변경 없음.
- 새 최초 무입력/Lobby 진입, 상하단 Plug 실제 입력, zoom 후 tiny/simultaneous drag의 자동 검증은 당시 미실행. 이를 소급 PASS 처리하지 않으며 이번 사용자 Human PASS와 구분한다.
- git diff --check PASS. 원본 status는 착수 시와 동일. 최종 fast/context 결과는 log 참조.

## 이전 단계 증거 (후속 개선의 검증이 아님)
- core61/61, 집중 Pan22/22 PASS; 전체211 중202 PASS/9 FAIL/0 skip. 직전191/182/9 대비 신규20건 통과·기존9건 동일.
- 실패2건: StarterPower_Validation(TV5 vs starter2~3), ScenariosAtoH_FullFlow(expected1/actual2).
- EditMode DontDestroyOnLoad 예외7건: PowerStrip SocketUpgradePreservesConnectedIdentities; ResidentDemand Coordinator_AssignmentEventsRefreshProductUseInfo / Coordinator_ReusesResidentOneAndCreatesLaterCardsOnceInStableOrder / UseInfo_BindsOneOrderedAuthoredPoolWithActivePriorityAndOneWarning; WallOutlet ConnectedTwoToThreeUpgradePreservesIdentitiesAndHouseUsage / InactiveSocketIsExcludedFromInteractionConnectionAndPower / RuntimeInitializedSocketIsCandidateAndReconnects.
- InputSystem 가상 Mouse → Dynamic Player Loop로 상단 wheel/Pan/TV preview/release 확인. S8 cameraY5.962963, 상단 소켓screenY745.68 < HUD하단880. length3, connected/powered=true.
- HUD Down 차단/capture 유지, 4:3 및 동시 wheel+Pan 확인. 물리 Touch는 미실행, ownership core 회귀는 통과.
- 이전 screenshot은 Overlay HUD가 빠져 경계 증거는 RectTransform screen 좌표 사용. 이전 Play 종료·검증 장치/설정 복원 완료.

## 환경 / 보존 / 다음 단계
- 격리 Editor PID55576/port7801, Unity6000.3.25f1 승인 검증 예외. 원본 PID56096/port7800 제어 안 함.
- 기존 환경 migration4개(URP global/manifest/lock/ProjectVersion)는 보존·전달 제외.
- 범위 밖 `Assets/02_Resources/Font/Pretendard-Medium SDF.asset`: 후속 착수 전529/0에서 최종2356/9(numstat)로 추가 변경 발견. 정확한 자동 갱신 원인 미확정. 직접 편집/되돌림/전달 금지, 별도 승인 필요.
- 새 runtime probe는 아직 실행하지 않아 이번 단계에서 추가한 임시 장치/화면/TimeScale 변경 없음.
- 다음: 검토된 파일만 checkpoint commit/push하고 local/remote HEAD 확인 후 새 Hint/Reveal 최소 framing 개선 착수. 환경4개/폰트 제외, dev merge 없음.
- 기존9실패는 범위 밖 예외로 남긴다. 새 후속 작업이 있어 Task는 active 유지. TASK021 Human PASS, TASK022 Human 대기 유지.
