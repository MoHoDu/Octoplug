# 밸런스 수정 방법

1. Google Sheet 수정
   (문서 링크: https://docs.google.com/spreadsheets/d/1kJR8Hx0QeLGeE-k7W3pLSuOmaEALF7YuoeO7orozj14/edit)

2. Unity 에디터 메뉴 열기
   Octoplug -> Balance -> Open Balance Sync Window

3. 검증
   Validate Google Sheets 버튼을 눌러 문법 오류나 논리적 오류가 없는지 확인합니다.

4. 밸런스 동기화 (적용)
   오류가 없다면 Sync Latest Balance 버튼을 클릭합니다.
   - [BalanceSync] SUCCESS 로그가 콘솔에 뜨면 완료된 것입니다.

5. 플레이 테스트
   동기화된 데이터는 즉시 로컬 에셋(Assets/02_Resources/Balance/GameBalanceArchive.asset)으로 갱신되며, Play 버튼을 눌러 테스트할 수 있습니다.

### 주의 사항
- 구글 시트만 수정한다고 해서 게임에 자동으로 반영되지 않습니다. **반드시 에디터에서 Sync를 진행해야 합니다.**
- Sync 중 오류(네트워크 에러, 유효성 검사 실패)가 발생하면, 기존 정상 데이터가 유지되며 덮어쓰지 않습니다.
