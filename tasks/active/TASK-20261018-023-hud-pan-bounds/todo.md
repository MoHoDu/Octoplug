# Todo

## 이전 상단 HUD 수정
- [x] 구현/자동 검증 및 사용자 Human PASS. core61/61, 집중Pan22/22, 전체211/202 PASS/기존9 FAIL.
- [x] 후속 확장 계획 승인 및 Task/소유권 범위 갱신.

## 후속 개선
- [x] 상하단 UsableViewport와 초기/fixed-center fit 순수 계산·회귀(178/178 PASS).
- [x] delta-aware Pan 및 collapsed 안전구간 회귀.
- [x] HUD/Residents 교집합과 참조 통합 구현.
- [x] 초기 연속 이벤트 최신 bounds 수집 후 1회 fit 구현.
- [x] pre-Brain Lens/연속보정/Pan 순서와 동일 Lens 변환 구현.
- [x] Hint/Reveal 위치/완료 계약 및 invalid pending 처리 구현.
- [x] matching Editor 참조 wiring/저장, 참조-only diff와 order0<100 확인.
- [x] 순수178/178·집중61/61 PASS, 전체244/235 PASS/기존9 FAIL 비교.
- [ ] 최초 무입력/Lobby 진입, 상하단 실제입력, zoom뒤 tiny drag 검증.
- [ ] UI 우선/capture,aspect/resize,Hint/Reveal/touch회귀.
- [ ] compile/Console/fast/context/diff 및 임시설정 복원·Play종료.
- [x] Domain Map/handoff/log에 구현 및 검증 차단 상태 기록.
- [x] 재연결 후 Scene 비교 사본 동일 확인·저장 및 빈 테스트 Scene으로 복구.
- [x] 후속 개선 Human Play Check: 사용자 “문제가 잘 해결되었어”.
- [ ] 사용자 요청 checkpoint commit/push 및 local/remote HEAD 비교.

## 제외
- 현재 수정 checkpoint stage/commit/push만 명시 승인. 새 개선 자동 push/dev merge/원본 적용 없음.
- 환경 migration4개와 기존 폰트 glyph 데이터 변경은 보존·전달 제외.
- 선행 TASK022 Human대기와 기존9개 테스트 실패는 별도 유지.
