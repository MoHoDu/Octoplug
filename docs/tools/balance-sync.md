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
   동기화된 데이터는 즉시 로컬 에셋(Assets/02_Resources/Resources/Balance/GameBalanceArchive.asset)으로 갱신되며, Play 버튼을 눌러 테스트할 수 있습니다.

### 런타임 생성 규칙
- Starter Room 1/2는 `초기 구성`에 따라 각각 Fan/TV 한 개와 Wall Outlet 한 개를 생성합니다.
- Room 3+의 기본 Product는 `제품 등장 풀`에서 현재 RoomCount와 Weight를 적용해 정확히 한 개를 선택합니다.
- `방 구성`의 Product count 열은 현재 기본 Product 생성 수량으로 사용하지 않습니다. RoomConfig는 방 크기·가중치·Wall Outlet 소켓 범위에 사용됩니다.
- `제품 추가 규칙`의 Redistribution은 기본 Content 생성 후 별도로 실행됩니다. 추가 개수 `n`을 결정한 뒤 `n`개의 서로 다른 방을 선택하고 방마다 최대 한 개를 추가합니다.
- 필수 기본 Product 또는 Wall Outlet을 만들 수 없으면 해당 방 promotion을 취소하고 기존 locked hint를 유지합니다.

### 주의 사항
- 구글 시트만 수정한다고 해서 게임에 자동으로 반영되지 않습니다. **반드시 에디터에서 Sync를 진행해야 합니다.**
- Sync 중 오류(네트워크 에러, 유효성 검사 실패)가 발생하면, 기존 정상 데이터가 유지되며 덮어쓰지 않습니다.
- runtime/build는 Google Sheet에 직접 접속하지 않고 동기화된 local asset만 사용합니다.
