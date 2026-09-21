## Balance Architecture Audit
기존 Default*Balance.cs 클래스들이 하드코딩된 상태로 프로덕션에 남아 있던 구조를 모두 파악했으며, 실제 런타임 소비처들이 이를 참조하지 않도록 완전히 분리했습니다. 대신 GameBalanceArchive라는 단일 ScriptableObject 구조를 도입하여 모든 밸런스 데이터를 통합 관리합니다.

## Local Balance Assets
Assets/02_Resources/Balance/GameBalanceArchive.asset 경로에 Git으로 추적되는 Local Data Asset이 저장되도록 구현했습니다. 게임은 시작 시 BalanceRegistry를 통해 이 에셋을 불러옵니다.

## Editor Sync Menu
Octoplug -> Balance -> Open Balance Sync Window 메뉴를 추가했습니다. 비개발자가 한눈에 진행 상황과 유효성 검사 결과를 파악할 수 있는 간단한 인터페이스(Validate / Sync 버튼 포함)를 제공합니다.

## Sheet Validation
CSV 파싱 후 헤더 이름 누락 여부, 정수/실수 파싱, MinRoomCount, Weight, 소켓 수(1~5) 등 지침에 명시된 엄격한 스키마 검증(Schema Validation)을 수행합니다. 공식 Area / (CurrentProductCount + 1)도 지원되는지 검사합니다.

## Atomic Sync
All-or-Nothing 방식(Atomic)을 적용했습니다. 9개의 시트를 모두 다운로드하고 파싱 및 유효성 검증을 마친 후, 단 1개의 행(Row)이라도 오류가 있으면 로컬 에셋을 전혀 갱신하지 않고 기존 정상 데이터를 보존합니다.

## Runtime Consumer Migration
RoomContentGenerationController, ResidentDemandController, SessionProgressConfig, RewardSystemController 내부의 호출을 모두 수정하여, 하드코딩 테이블 대신 새로 작성한 Mapper (DemandAuthoringMapper, RoomContentBalanceMapper 등)를 통해 Sync된 로컬 데이터만 읽도록 변경했습니다.

## Product Redistribution Rules
RoomContentGenerationController에 RoomUnlocked 이벤트 구독을 추가하여, 새로운 방이 열릴 때 현재 RoomCount에 맞는 제품 추가 규칙 행을 찾아 지정된 개수(Min~Max)만큼 추가 제품 배치를 시도합니다.

## Product Pool
현재 RoomCount 이상인 제품 등장 풀에서 가중치(Weight) 기반으로 제품을 무작위로 선택하며, 현재 집의 허용 전력량(HouseAllowedPower)을 초과하는 제품은 후보 풀에서 제외(Feasibility check)합니다.

## Room Selection
추가 제품을 배치할 방은 Area / (CurrentProductCount + 1) 공식에 따라 가중치를 계산해 확률적으로 선택됩니다. DistinctRoomPerProduct = TRUE일 경우, 이번 배치 사이클 내에서 같은 방에 중복 생성되지 않도록 필터링합니다.

## Placement / Reachability
추가 제품은 일반 생성 로직과 동일하게 방 내부, 문/콘센트 간섭 방지, 기존 WallOutlet 및 PowerStrip과 현재 케이블 길이 이내에서 닿을 수 있는 유효한 경로(Cable Reachability)가 보장되는 위치에만 생성됩니다. 실패 시 재시도하며 불가능할 경우 조용히 경고를 남깁니다.

## Automated Verification
- 모든 런타임 소비처가 Local Asset 구조로 마이그레이션되었음을 확인했습니다.
- 에디터 내 동기화 로직에 예외 처리를 촘촘하게 적용했습니다.
- pwsh -File scripts/verify-fast.ps1 검증을 통과했습니다.

## Human Verification
HUMAN_VERIFY_REQUIRED
유니티 에디터에서 다음을 직접 확인해 주세요.
1. Octoplug -> Balance -> Open Balance Sync Window 열기
2. **Validate Google Sheets** 클릭 후 콘솔에 [BalanceSync] VALID가 뜨는지 확인
3. **Sync Latest Balance** 클릭 후 SUCCESS가 뜨는지 확인 (최초 1회 실행 필수)
4. Play 모드 실행 후, 4번째 혹은 5번째 방을 열 때 제품이 추가로(1개) 재분배(Redistribution)되어 배치되는지 관찰
5. 추가된 제품이 기존 방의 잉여 공간에 안전하게 배치되고, 케이블로 전원에 연결 가능한 상태인지 확인

## Usage Guide
비개발자를 위한 간략한 가이드 문서를 docs/tools/balance-sync.md에 작성했습니다. 에디터 메뉴 진입 방법과 주의 사항이 포함되어 있습니다.

## TASK Status
HUMAN_VERIFY_REQUIRED
