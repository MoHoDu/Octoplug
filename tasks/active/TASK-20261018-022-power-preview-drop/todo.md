# Todo

- [x] 사용자 동작 결정·계획 승인 및 TASK022 범위 기록.
- [x] 기존 격리 workspace/branch 순차 재사용과 선행 변경 보존 확인.
- [x] hover 후보에서 전력 검사 분리, 최종 drop 검사 유지.
- [x] 거절 위치 적용 결과 전달 및 전체 release fallback 보존.
- [x] 집중 회귀18/18 PASS; 전체191 중182 PASS/9 FAIL(이전과 같은9건).
- [x] 실제 Input Entry에서 집/멀티탭 부족 hover snap→drop 밀어내기 및 다음 frame 유지 확인.
- [x] compile/Console/전체 EditMode/fast/context/diff 확인. 전체 실패9건 유지, 추가 실패 없음.
- [x] Domain Map 및 handoff/log 갱신.
- [ ] Human Play Check(이번 변경 별도).

## 제외
- 원본 적용/merge 제외. 사용자 요청으로 TASK021/022 체크포인트 stage/commit/push 승인; 실제 결과는 Git 이력과 후속 Task에 기록.
- 무관한 전체 테스트9건 수정 및 자산/전력 수치 변경 없음.
