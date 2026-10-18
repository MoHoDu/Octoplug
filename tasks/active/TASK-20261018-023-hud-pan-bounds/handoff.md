# Handoff

## Status
- Current stage: verification. 새 최소 자동 Hint/Reveal 구현·자동 검증 완료, Human 확인 대기. 기존 초기 fit/Residents_Area/manual 연속보정은 사용자 Human PASS.
- 선행 카메라 checkpoint `7082cfa72f094c52a63184d5e676b2adabac7a14` push 및 local/remote HEAD 일치 확인 완료. 이후 사용자 명시 요청으로 새 margin0 변경 commit/push 및 dev 대상 PR 통합 진행.
- Branch: `task/TASK-20261018-023-hud-pan-bounds`; Worktree: `D:/github-worktrees/Octoplug/shared-wall-outlet`. 원본/dev merge 없음.
- TASK021 Human PASS, TASK022 Human대기 유지. 선행 checkpoint fcd4601도 별도 branch에 push 완료.

## 새 개선 / 승인 경계
- 사용자 선택은 **다음 Hint까지 포함**. production `HouseCameraZoomController.TryApplyFixedCenterFraming`만 margin0 적용, tooltip 정정.
- 초기 fit/dynamic max/Pan margin2, 위치 보존·zoom-out-only·smoothing, max(target,current), invalid/impossible pending 및 완료1회 계약 유지.
- 이미 커진 화면을 자동 zoom-in하지 않는다. 먼 Hint 전체 표시 때문에 필요한 축소는 여전히 발생한다.
- Zoom/Bridge Editor 테스트2개: 기존4개 fixture/기대값 보정, 신규10case. UsableViewport/Bridge/RoomGeneration/GameFlow/Scene/UI/Package/ProjectSettings 구현 불변.

## 새 개선 검증
- Unity compile completed. core178/178, 집중 Editor71/71 PASS. 전체254/245 PASS/9 FAIL/0skip: 직전244/235/9에 새10개 통과, 아래 기존9실패 동일.
- fresh InfiniteMode Play:1920x1080, 상하단 inset0.1851852, visible[-6,-3,12,9], S12.70588/중심(3,3)이 기존 margin2 initial-fit 계산과 일치.
- DebugAddExp→실제 GameFlow/생성: Room3 다음 Hint[-4,-12,4,-3], old padding2 목표27, new0/실제target23.82353. 정착 후 완료1회→RewardDelay→AwaitingReward.
- 연속 Room4 생성: 승격방/다음 Hint[-16,-6,-4,3]가 이미 보여 S23.82353 그대로(기존 padded계산27). 완료1회/같은 보상 흐름. 두 생성 모두 중심(3,3) 불변.
- 이미 보이는 seed에 Hint+Reveal 재요청: size/target23.82353 유지, motion0/completion1/중심 불변.
- 위 생성/보상 종료 setup은 내부 API 호출이며 실제 UI 입력 검증이 아니다. unfocused Loop가 처음 정지해 보상 확인1회 실패 후 runInBackground 임시 true로 실제 Player Loop 정착 확인.
- 실제 InputSystem 가상 Mouse→Dynamic Player Loop: wheel23.82353→8 및8→12, 상·하단 방향 drag/capture 확인. S8 상단 Pan Y5.962963 경계 도달, 하단 방향은 매86.4px마다1.28world 이동.
- zoom-out 후1px 교대 drag는 약0.02222world 이동(2*S/1080), 큰 추가 snap 없음. 내부 보상 종료 setup 직후 첫 drag는 무이동이었고 release 후 재시도 정상; 첫 capture 원인은 미계측.
- 물리 Touch/모든 aspect·Lobby 진입·상하단 Plug 재연결은 이번 범위에서 미실행. 이전 Human PASS와 자동 회귀 결과를 이 실입력 증거로 소급하지 않음.
- HUD 시작 drag는 EventSystem capture/카메라 불변. Console runtime 오류0(기존 hatching/RW007 경고 유지), 장치0/background=false/TimeScale1 복원·Play종료, InfiniteMode clean. fast PASS/context Strict WARN(하드 초과 없음)/diff PASS. 세부 log 참조.

## 기존 개선 checkpoint 증거
- Core PanBoundaryCalculator/UsableViewport, Unity Pan/Zoom/Input/Bridge의 상하단 가용 화면·초기fit·pre-Brain 연속 보정 구현.
- `InfiniteMode.unity`는 기존 checkpoint에서 참조3줄만 변경(topHudArea/bottomResidentsArea/Zoom.cameraPan). Zoom order0<Brain100 검증. 이번 개선 Scene 디스크 diff 없음.
- 당시 core178/178, 집중61/61, 전체244/235 PASS/기존9 FAIL. 저장 모달 후 live 비교 사본과 디스크 동일 확인·저장, 빈 Scene에서 테스트 복구. 강제종료/discard/raw YAML 없음.
- 당시 미실행 최초/Lobby/상하단 Plug/tiny drag 자동 검증을 사용자 Human PASS로 소급 대체하지 않음.
- 이전 상단 수정: core61/61, 집중Pan22/22, 전체211/202 PASS/9 FAIL. InputSystem→wheel/Pan/TV preview/release/powered, HUD Down 차단/capture,4:3/동시 wheel+Pan 확인.

## 기존9실패 (범위 밖)
- StarterPower_Validation: TV power5 vs starter2~3. ScenariosAtoH_FullFlow: expected1/actual2.
- EditMode DontDestroyOnLoad 예외7개: PowerStrip SocketUpgradePreservesConnectedIdentities; ResidentDemand Coordinator_AssignmentEventsRefreshProductUseInfo / Coordinator_ReusesResidentOneAndCreatesLaterCardsOnceInStableOrder / UseInfo_BindsOneOrderedAuthoredPoolWithActivePriorityAndOneWarning; WallOutlet ConnectedTwoToThreeUpgradePreservesIdentitiesAndHouseUsage / InactiveSocketIsExcludedFromInteractionConnectionAndPower / RuntimeInitializedSocketIsCandidateAndReconnects.

## 환경 / 보존 / 다음 단계
- 격리 Editor PID55576/port7801, Unity6000.3.25f1 승인 예외. 원본 PID56096/port7800 제어 안 함.
- 환경 migration4개(URP global/manifest/lock/ProjectVersion)와 폰트 `Pretendard-Medium SDF.asset` 변경2356/9는 보존·전달 제외. 폰트 자동 갱신 원인 미확정; 직접 편집/되돌림 없음.
- 사용자 확인: 새 세션에서 방 생성 시 다음 Hint는 보이되 불필요하게 더 작아지지 않는지 확인. 새 개선 Human 확인 전 DONE 아님.
- 2026-10-19 “여기까지 수정 내용도 commit하고 push해줘. 그 다음 PR을 통해 통합하자.”로 Git 전달 승인. 기존9실패·TASK022/최소framing Human대기는 PR에 공개하고 DONE 처리 안 함. 원본 checkout/환경/폰트는 보존.
