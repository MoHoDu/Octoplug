# Human Setup Required

기본적으로 다음은 사람이 준비한다.

- Scene Hierarchy
- Prefab 구조
- UI 배치
- Art/Graphic 배치
- 시각적 위치/크기

단, 현재 Task에서 명시적으로 AI에게 위임된 항목은 예외다.

## AI Setup Allowed

Task에 `AI Setup Allowed`로 지정된 항목은
AI가 직접 생성/수정할 수 있다.

이 경우에도:

- 기존 구조를 최대한 재사용
- 기획 미결정 사항은 Human Decision Gate
- 불필요한 재설계 금지
- 작업 후 생성/수정한 Scene/Prefab/UI를 명확히 보고

Task에 위임 표시가 없는 경우,
AI가 디자인/배치 판단까지 대신해야 한다면
Human Setup Required를 발동한다.