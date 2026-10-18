# Todo

## 기존 개선 checkpoint
- [x] 상단 HUD 및 초기 fit/Residents_Area/manual 연속보정 구현, 사용자 Human PASS.
- [x] Scene 저장 차단 복구: live 비교 사본 동일 확인·저장 및 빈 테스트 Scene 전환.
- [x] core178/178, 집중61/61, 전체244/235 PASS/기존9 FAIL.
- [x] 사용자 요청 checkpoint `7082cfa` commit/push 및 local/remote HEAD 일치.
- 이전 미실행 최초/Lobby/상하단 Plug/tiny drag 자동 검증은 Human PASS로 소급 대체하지 않음(handoff 참조).

## 최소 자동 Hint/Reveal 개선
- [x] 다음 Hint 포함 정책 확인 및 계획 승인.
- [x] 자동 fixed-fit margin0 및 초기/수동 범위 tooltip 정정.
- [x] 기존4개 기대값/fixture 보정, 신규10개 Editor 회귀 추가.
- [x] Unity compile completed, core178/178 및 집중 Editor71/71 PASS.
- [x] 전체 Editor254/245 PASS/동일9실패 비교.
- [x] fresh 초기 진입/연속 실제 승격/최소fit/위치 보존/Reveal→RewardDelay runtime 확인.
- [x] 수동 zoom/Pan 실제 InputSystem 회귀 및 임시설정 복원·Play종료.
- [x] Domain Map/handoff/log, fast PASS/context Strict WARN(하드 초과 없음)/diff PASS 및 Scene 불변 확인.
- [ ] 새 최소 자동 줌 개선 Human Play Check.

## 제외
- 현재 변경 commit/push 및 dev PR 통합은 사용자 명시 승인. 원본 checkout 적용/범위 밖 변경은 제외.
- 환경 migration4개와 기존 폰트 glyph 데이터 변경 보존·전달 제외.
- 선행 TASK022 Human대기와 기존9개 테스트 실패는 별도 유지.
