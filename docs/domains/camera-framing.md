# Camera Framing Domain

## Responsibility

Own the 2D orthographic Camera's manual zoom, empty-world drag Pan, dynamic House-based limits, and one-shot zoom-only framing that keeps a newly generated Hint Room visible without moving the Camera.

## Engine-Independent Core

`Assets/01_Scripts/CameraFraming/` is a `noEngineReferences` calculation library. It references `Octoplug.RoomGeneration` only for `RoomBounds2D` and `Point2D`. `Tests/CameraFraming.Core.Tests/` compiles the same sources outside Unity.

Current rules:

- `ZoomRange` is an inclusive positive Orthographic Size range.
- `DynamicZoomLimit` derives maximum manual zoom-out from the union of actual generated Room bounds, actual Camera aspect, and a caller-provided margin. It never uses room-count or fixed-coordinate assumptions.
- `ZoomInputMapper` maps wheel/touchpad scroll and pinch distance deltas; device reading remains outside the core.
- `CameraDesignReference` records 1920x1080 / 16:9 as the tuning reference only. Runtime calculations always use actual `Camera.aspect`.
- `HintFramingCalculator` fits the complete Hint min/max bounds plus margin at the current Camera center, never moves the Camera, never forces zoom-in, and has no unlocked-House upper clamp.
- `PanDeltaMapper` converts pointer pixel displacement into opposite Camera world displacement using current Orthographic Size, actual aspect, viewport dimensions, and sensitivity.
- `PanBoundaryCalculator`는 실제 House bounds + margin과 가용 viewport에서 Camera 중심 범위를 계산한다. 상단 t/하단 b, Orthographic Size S이면 Y 범위는 `[house.MinY-margin+S-2*S*b, house.MaxY+margin-S+2*S*t]`. X는 기존과 같다. 범위 밖의 수동 입력은 요청 delta만큼 안쪽으로 복구하며 순간 clamp하지 않는다. collapsed 축도 집 전체를 담는 containment 구간을 보존하고 이미 안전하면 현재 좌표를 유지한다. 기존 4/5인자 overload를 보존한다.
- `UsableViewport`는 상하단 비율의 유효성, 중앙 정렬 초기 fit, 위치 고정 zoom-out fit을 계산하는 순수 helper다. 전면 가림/불가능한 고정 중심 fit은 명시적인 실패로 반환하며 무한·음수 Lens를 만들지 않는다.
- `PanGestureTracker` latches ownership as `CameraPan` or `GameplayBlocked` at Pointer Down. `MultiTouchSuppressed` prevents Pan from resuming from a remaining pinch finger; all touches must release before a new Pan can begin.

## Unity Integration

The production integration is wired fresh in `Assets/00_Scenes/Demo/InfiniteMode.unity`. The isolated `RoomGenerationTest.unity` scene remains reference-only and is not a production copy target.

### Zoom and Hint framing

- `CameraZoomInputReader` reads `Mouse.current.scroll` and two-finger `Touchscreen` pinch through the New Input System. Its Inspector fields are `desktopScrollZoomSensitivity` (Mouse Wheel + Windows touchpad scroll, which the Input System cannot reliably tell apart, so they intentionally share one field) and `pinchZoomSensitivity` (independent, mobile pinch only); both default to `0.2`.
- `HouseCameraZoomController` exclusively owns `CinemachineCamera.Lens.OrthographicSize`, including smoothing, the fixed minimum size `5`, dynamic House maximum, and full-bounds Hint framing.
- A new Hint may require more zoom-out than the unlocked-House maximum because it is not yet in that union. Hint framing widens the effective range rather than clipping the Hint; later House recomputation preserves the larger current target so promotion does not implicitly zoom in.
- 자동 Hint/Reveal은 다음 Hint까지 포함하되 추가 여백 없이 raw room bounds를 가용 화면에 담는 최소 크기만 요청한다. 이미 보이면 추가 축소하지 않는다. 초기 fit/수동 최대 범위/Pan의 margin2는 별도 유지하며, `max(target,current Lens)`로 같은 프레임의 먼 Hint 목표가 가까운 Reveal에 취소되지 않게 한다.

### Pan ownership and movement

- `CameraPanInputReader.Awake()` corrects Unity's Editor Play Mode default (`InputSystem.settings.editorInputBehaviorInPlayMode`), which otherwise gates pointer button state behind Game View OS focus while mouse-wheel scroll bypasses that gate — the exact reason empty-world Mouse Drag could silently do nothing while Zoom still worked in the same session. The override is applied in memory only, guarded by `#if UNITY_EDITOR`, writes no `ProjectSettings` asset, and has no effect in a build.
- `CameraPanInteractionResolver` adapts Camera Pan to the authoritative production `PointerInteractionResolver`. It requests Empty World capture only on Pointer Down; the production resolver applies UI, Plug, Product, Socket, eligible PowerStrip Head, and explicit-blocker priority.
- Broad Room floor/wall/door colliders do not block empty-world Pan.
- `CameraPanInputReader` separately owns Mouse and single-touch Pan input. It reads `panSensitivity` live, latches Down ownership until Up, suppresses emulated Mouse while touch is active, and yields completely to pinch during multitouch.
- `HouseCameraPanController` moves only the `CinemachineCamera` transform. It applies the latest cached "visible house" bounds (see below), the **current** Lens size, the **current** output aspect, and `panBoundaryMargin` — recomputed fresh on every single Pan request, never cached as a fixed world-space range. Zoom In/Out therefore widens/narrows the reachable range immediately, with no Room Generation event required. Main Camera is never arbitrarily transformed.
- `HouseCameraPanController.topHudArea`/`bottomResidentsArea`는 InfiniteMode의 기존 `Canvas/HUD_Area`/`Canvas/Residents_Area`에 연결한다. world corners→screen→`outputCamera.pixelRect` 교집합으로 상하단 가림을 구하고 Zoom도 같은 결과를 사용한다. 각 UI 미지정/비활성/Canvas disabled/해당 가장자리 비겹침은 inset0; viewport0/전면 가림은 적용을 보류한다. 고정 HUD 픽셀 크기는 사용하지 않는다.
- TASK023 후속 승인: 시작 생성과 Canvas layout 준비 후 최신 visible-house를 가용 화면에 1회 fit/정렬한다. 이후 수동 zoom에는 기존 smoothing 시간으로 최근접 안전 위치까지 연속 보정하되 drag delta에는 지연을 추가하지 않는다. Hint/Reveal/resize 자체는 위치 보존; 범위 밖에서도 다음 작은 drag를 큰 clamp로 바꾸지 않는다.
- Zoom이 pre-Brain LateUpdate를 구동하여 최신 Lens→manual 보정→수집된 pixel Pan 변환/적용→Reveal 완료 순서를 보장한다. 기존 world-delta public 호출은 즉시 적용한다. Hint/Reveal은 유효한 가용 화면에서 위치 고정·zoom-out-only target을 구하고, 불가능한 fit은 pending으로 보관한다.

### Room Generation bridge

- `RoomGenerationCameraFramingBridge` is the sole Room Generation → Camera connection.
- `ProductionRoomGenerationController.NextHintCreated`에서 unlocked-only bounds는 dynamic max에, unlocked+Hint visible bounds는 Pan에 전달한다. 최초 tick 전 이벤트는 최신 visible bounds의 초기 fit으로 병합하고, 이후 이벤트는 위치 고정 Hint framing으로 전달한다.
- 실제 `GameFlowManager`가 `RequestRoomReveal(RoomPlacement)`를 사용하고 완료 후 RewardDelay로 진행한다. Bridge는 zoom-only 요청을 전달하고 유효한 fit의 smoothed target에 도달했을 때만 `CameraRevealCompleted`를 전달한다(이미 보이면 즉시).
- Room Generation remains unaware of Camera, Cinemachine, Zoom, or Pan.

## Verification Evidence

### TASK023 최소 자동 framing (2026-10-19, Human 확인 대기)
- 기존 개선은7082cfa push 완료. 새 자동 Hint/Reveal margin0은 core178/178·집중 Editor71/71 PASS, 전체254/245 PASS/기존9 FAIL. Scene 변경 없음.
- 실제 연속 생성에서 Room3 목표27→23.82353(new0 계산과 일치), 이미 보이는 Room4는23.82353 유지. 중심 불변·완료1회→RewardDelay 확인. 생성/보상 setup 호출과 실제 Mouse wheel/Pan/tiny drag/UI 차단 증거는 TASK023 handoff에 구분.

### TASK023 후속 (2026-10-19, 사용자 Human PASS)
- 초기 fit/상하단 UI/manual zoom 연속보정 구현, core178/178·집중 Editor61/61 PASS 및 Unity compile completed. 전체244/235 PASS/기존9 FAIL. Scene 참조3줄만 변경, Zoom order0 < Brain order100 확인.
- 저장 모달/CLI timeout 후 재연결, live 비교 사본과 디스크 동일 확인·저장 및 빈 테스트 Scene으로 복구. 사용자가 해결 확인 및 checkpoint push 요청. 당시 미실행 자동 Input Entry를 소급 PASS로 주장하지 않는다. 범위 밖 폰트 변경과 실패9건은 TASK023 handoff 참조.

### TASK023 이전 상단 수정 (2026-10-18, 이후 사용자 Human PASS)
- 순수 CameraFraming 61/61 PASS, 집중 Unity Pan 22/22 PASS. 전체 EditMode 211 중202 PASS/9 FAIL: 직전191/182/9 대비 추가20건 통과, 기존9건 동일. 최종 컴파일 up_to_date.
- 실제 Input System → Player Loop 경로로 상단 방 확대/Pan, TV preview→release→powered, HUD Down 차단 및 Pan capture의 HUD 통과 유지 확인. 16:9/4:3와 동시 wheel+Pan에서 최신 Lens 경계 적용 확인.
- 터치 소유권/멀티터치 후 전체 release 억제는 순수 회귀 통과. 이번 물리 터치 장치 확인은 미실행. 자세한 증거·범위 밖 에셋 변경은 TASK023 handoff 참조.

### TASK006 당시 기록
- Camera Framing pure suite: 46 passing tests; strict build has 0 warnings/errors.
- Room Generation regression suite: 66 passing tests.
- Actual Input System mouse state events verified empty-world Pan direction and latched ownership in Play Mode.
- Runtime probes verified gameplay-blocked crossing, dynamic expanded-House ranges, exact boundary clamps, and Hint framing with unchanged panned X/Y.
- Unity recompilation and a clean Play Mode restart reported zero Console errors. Existing copied-scene warnings are documented in the Task handoff.
- Unity discovered zero project tests (`NO_PROJECT_TESTS`); pure tests and focused runtime probes are recorded separately.
- Human Play Check (Task 006, DONE): the user confirmed PASS on Camera Pan feel, Zoom In reaching House/Hint edges via the boundary fix, Zoom/Hint framing, and Room Generation/Hint/Door functionality. Physical Touch/Pinch feel was exercised as part of that same sign-off.

## Design Direction

The concept document does not define detailed Camera feel. Production `InfiniteMode.unity` now owns the verified Zoom/Hint/Pan adapters and an external Room Reveal completion boundary. Future GameFlow orchestration and cinematic behavior remain deferred.

### Camera Reveal (confirmed 2026-09-20)

On Level Up, the Camera must automatically zoom out to show the newly generated Room to
the player before the Reward Phase begins.

- Camera Reveal is a zoom-out operation (similar in mechanism to Hint framing, but
  triggered by the GameFlow rather than by Room Generation's HintRoomCreated event).
- Camera does **not** decide when to reveal; it responds to a request from future
  `GameManager` / `GameFlowController`.
- Camera must emit a `CameraRevealCompleted` event (exact name at implementation time)
  so GameFlow knows when to proceed to the Reward Phase.
- Camera is **not** responsible for EXP, Level Up judgment, Satisfaction, or Reward.

See: `docs/decisions/infinity-progression-and-reward-loop.md` section 7 and 8.

## Related Domains

- [Room Generation](room-generation.md) supplies `NextHintCreated` and actual `RoomLayout`/`RoomPlacement` bounds.
- Infinite Mode owns the production Camera hierarchy and fresh component/reference wiring.
- `Octoplug.Power.Input.PointerInteractionResolver` is the sole production pointer-ownership authority. `CameraPanInteractionResolver` is only its screen-to-world adapter for Empty World capture.

## Modification Cautions

- Preserve Pointer Down ownership through release; never transfer an object-started gesture to Pan or an empty-world Pan to gameplay while crossing boundaries.
- Preserve priority: UI → Plug → Socket/PowerStrip → Product → explicit gameplay → empty-world Pan.
- Preserve pinch priority and full-sequence multitouch suppression; Pan and pinch must not run simultaneously.
- Do not turn broad Room colliders into Pan blockers.
- Hint framing must remain zoom-only and position-preserving.
- Do not merge Pan ownership into Zoom input/ownership without a new approved design decision.
- Do not reintroduce a Room Generation dependency on Camera code.
- `com.unity.cinemachine` is user-installed; do not reinstall it or change its pinned version.

## Narrow Search Order

1. 현재 active Task와 이 map (HUD Pan 수정: TASK023, 초기 Pan 기록: TASK006).
2. `Assets/01_Scripts/CameraFraming/` and `Assets/01_Scripts/CameraFraming.Unity/`.
3. [Room Generation](room-generation.md) for `HintRoomCreated` and actual bounds.
4. Interaction-domain files only when explicit hit semantics must be verified.
5. Broaden only when evidence is insufficient.

## Open Decisions

- **`Assets/00_Scenes/Demo/RoomGenerationTest.unity` is not a production copy target.** Production integration is wired fresh against `InfiniteMode.unity`; retain the test scene only as historical verification reference.
- 최초 fit 및 manual zoom의 최근접 안전 위치 보정은 TASK023 후속 승인 범위다. 그 밖의 자동 위치 변경은 새 결정이 필요하며 Hint/Room Reveal은 zoom-only/위치 보존을 유지한다.
- Cinematic/session-level framing beyond the external Room Reveal request/completion boundary.
