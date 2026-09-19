# Runtime Interaction Validation

다음 기능은 자동 로직 테스트만으로 완료 처리하지 않는다.

- Click
- Drag
- Drop
- Touch
- Pointer Input
- Scene Reference
- Inspector Reference
- UI Button Interaction
- Runtime GameObject 연결
- Prefab Instance 연결

검증 단계:

1. Logic Test
2. Runtime Integration Check
3. 실제 Input Entry 확인
4. Human Play Check

다음과 같은 테스트는 실제 사용자 입력 검증으로 간주하지 않는다.

- Reflection으로 메서드 직접 호출
- 내부 함수를 직접 실행
- 상태값을 강제로 주입
- 실제 Pointer를 거치지 않은 합성 테스트

이런 테스트는 Logic Test로만 기록한다.

실제 조작이 필요한 기능은 사람이 Unity PlayMode에서 직접 확인하기 전까지
Task Status를 DONE으로 변경하지 않는다.

권장 상태:

IMPLEMENTING
→ AUTO_VERIFIED
→ HUMAN_VERIFY_REQUIRED
→ DONE