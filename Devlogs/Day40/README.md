# 프로젝트 H : 40일차 개발 로그

## 오늘 목표

40일차의 목표는 **Day39에서 완성한 던전 보상 흐름의 Gold와 아이템을 실제 소비할 수 있도록 기본 상점과 구매·판매 경제 흐름을 구축하는 것**이었다.

핵심 흐름은 다음과 같다.

```text
던전 전투
↓
Gold / 아이템 획득
↓
Lobby 복귀
↓
상점 진입
↓
Gold로 아이템·장비 구매
또는 일반 아이템 판매
↓
Gold / 인벤토리 변경
↓
SaveCurrent()로 영구 저장
```

## GitHub 기준

- 개발 일지 추가 전 기준 커밋: `e881035bee5d826c731242edc7ce9f95c26fc366`
- 기준 커밋 메시지: `40`
- Day39 기준 커밋: `655fa69f40cc4b10ca1c23890c9fa9aec68f0ce6`
- 브랜치: `main`
- Day39 → Day40 변경: **26개 파일 변경 / 1,490줄 추가 / 삭제 없음**
- `Devlogs/Day40/README.md`: 기준 커밋에는 아직 없음

> 이 README를 기존 Day40 커밋에 `--amend`하면 최종 커밋 SHA는 변경된다.

## 1. 기본 상점 데이터 구조 추가

상점의 상품 구성을 코드에 고정하지 않고 데이터로 관리할 수 있도록 `ShopData`와 `ShopProductEntry`를 추가했다.

`ShopProductEntry`는 상품 ID, 대상 ItemId, 구매 가격, 판매 가격을 가진다. 판매 가격이 0이면 해당 상품은 판매 불가로 처리한다.

### 추가 파일

- `Assets/ProjectH/Scripts/Data/ShopData.cs`
- `Assets/ProjectH/Scripts/Data/ShopProductEntry.cs`
- `Assets/ProjectH/Resources/Shops/SHOP_LOBBY.asset`

각 신규 Unity 파일의 `.meta`와 신규 폴더 `.meta`도 함께 추가했다.

## 2. 기존 전투 Gold 저장 구조와 상점 연결

Day40에서는 `GoldCurrencyService`를 추가해 Gold 조회, 증가, 안전 차감을 공통 처리하도록 했다.

현재 프로젝트의 Gold는 기존 전투 보상 시스템과의 호환을 위해 `SYS_BATTLE_GOLD:` 시스템 플래그에 저장되고 있다. 새 서비스도 동일한 저장 규칙을 사용하므로 Day39까지 던전에서 획득한 Gold를 그대로 상점 잔액으로 사용할 수 있다.

```text
전투 보상
→ SYS_BATTLE_GOLD:500

상점 진입
→ GoldCurrencyService.GetGold()
→ 500 Gold

100 Gold 구매
→ GoldCurrencyService.TrySpendGold()
→ SYS_BATTLE_GOLD:400
```

Gold가 부족하면 차감하지 않고 실패하며 기존 잔액을 유지한다.

## 3. 구매 거래 흐름 구축

`ShopTransactionService.TryPurchase()`를 추가해 구매 검증과 실제 지급을 하나의 거래 흐름으로 묶었다.

구매 순서는 다음과 같다.

```text
상품 / ItemData 확인
↓
수량 및 총 가격 검증
↓
Gold 잔액 확인
↓
일반 아이템이면 MaxStack 확인
장비면 EquipmentData 확인
↓
Gold 차감
↓
아이템 또는 장비 지급
↓
지급 실패 시 Gold 환불
```

일반 아이템은 기존 `ItemInventoryService.TryAdd()`를 사용하므로 Day38 가방과 MaxStack 정책을 그대로 따른다.

Gold를 먼저 차감한 뒤 지급 단계에서 실패하더라도 구매 전 상태로 복구하도록 환불 처리를 넣었다.

## 4. 일반 아이템 판매 흐름 구축

`ShopTransactionService.TrySell()`을 통해 상점에 등록된 일반 아이템을 판매할 수 있도록 했다.

판매 시 다음 조건을 검증한다.

- 상품과 ItemData가 실제 존재하는지
- 거래 수량이 1 이상인지
- 상품의 판매 가격이 0보다 큰지
- 보유 수량이 판매 요청 수량 이상인지
- 판매 총액이 `int` 범위를 넘지 않는지

검증을 통과하면 기존 `ItemInventoryService.TryRemove()`로 수량을 감소시키고 판매 금액만큼 Gold를 지급한다.

## 5. 장비 구매는 고유 인스턴스로 지급

장비 상품은 일반 아이템 스택으로 넣지 않고 기존 `SaveData.TryCreateEquipmentInstance()`를 사용해 고유 장비 인스턴스로 생성한다.

여러 개를 한 번에 구매하다가 중간 생성에 실패하면 그 거래에서 이미 생성된 장비 인스턴스를 역순으로 제거하고 Gold도 환불하도록 구성했다.

Day40 기본 범위에서는 장비 판매는 지원하지 않는다. 장비는 개별 인스턴스 선택과 장착 상태 확인이 필요하므로 일반 아이템 판매와 분리했다.

## 6. 기본 상점 Runtime UI 추가

새 `Shop` 씬과 `ShopScreenController`를 추가했다.

상점 화면은 Runtime에서 UI를 만들며 다음 기능을 제공한다.

- 현재 Gold 표시
- 상점 이름 표시
- 상품 목록 표시
- 선택 상품 이름·설명·가격·보유량 표시
- 거래 수량 증감
- 구매 버튼
- 판매 버튼
- 거래 결과 상태 표시
- Lobby 복귀 버튼

`ShopSceneRuntimePatch`가 `Shop` 씬 로드 시 기본 카메라와 `ShopScreenController`를 자동 생성한다.

기본 상점 데이터는 `Resources/Shops/SHOP_LOBBY`에서 로드한다.

## 7. Lobby에서 상점 진입 연결

`LobbyShopRuntimePatch`를 추가해 Lobby 하단 내비게이션에 `상점` 버튼을 Runtime으로 생성한다.

상점 버튼을 누르면 `GameScenes.Shop`으로 이동한다.

`GameScenes.cs`에는 `Shop` 씬 이름을 추가했고 `ProjectSettings/EditorBuildSettings.asset`에도 `Assets/ProjectH/Scenes/Shop.unity`를 등록했다.

## 8. 임시 상점 상품 및 가격

현재 가격은 Day40 경제 흐름 확인용 임시값이다.

| 상품 ItemId | 구매 가격 | 판매 가격 | 비고 |
| --- | ---: | ---: | --- |
| `IT_POTION_SMALL` | 100 Gold | 40 Gold | 일반 소비 아이템 |
| `IT_MATERIAL_001` | 160 Gold | 60 Gold | 일반 재료 아이템 |
| `EQ_WEAPON_IRON` | 900 Gold | 판매 불가 | 고유 장비 인스턴스 지급 |
| `EQ_ARMOR_GUARD` | 1000 Gold | 판매 불가 | 고유 장비 인스턴스 지급 |

날짜별 재고 갱신과 본격적인 경제 밸런스는 후속 일정에서 확장할 수 있도록 현재는 고정 상품 구성으로 두었다.

## 9. 거래 후 즉시 저장 연결

상점 UI의 구매 또는 판매가 성공하면 `SaveCurrent()`를 바로 호출한다.

따라서 실제 플레이 흐름은 다음과 같이 이어진다.

```text
상점 구매 / 판매 성공
↓
Gold 변경
↓
인벤토리 변경
↓
SaveCurrent()
↓
Lobby / Bag 이동
↓
변경된 Gold와 아이템 상태 유지
```

저장에 실패한 경우에도 화면 상태 문구를 통해 거래 성공과 저장 실패를 구분해서 표시한다.

## 10. EditMode 테스트 추가

Day40에서는 Gold 차감과 기본 상점 거래의 핵심 규칙을 검증하는 테스트를 추가했다.

### `GoldCurrencyServiceTests`

- 충분한 Gold 차감 성공 및 잔액 감소
- Gold 부족 시 차감 실패 및 기존 잔액 유지

### `ShopTransactionServiceTests`

- 일반 아이템 구매 성공 시 Gold 차감 및 인벤토리 증가
- MaxStack 초과 구매 실패 시 Gold와 인벤토리 상태 유지
- 일반 아이템 판매 성공 시 인벤토리 감소 및 Gold 증가

현재 테스트는 일반 아이템 거래 중심이다. 장비 다중 구매 롤백, 판매 불가 상품, 총액 오버플로 등은 후속 회귀 테스트 보강 대상으로 남아 있다.

## 11. 검증 결과

GitHub 최신 `main`에서 Day39 커밋과 Day40 커밋의 변경 내용을 다시 검토했다.

정적 검토 기준으로 Day40 핵심 상점 흐름을 막는 명확한 오류는 확인되지 않았다.

확인된 사항:

- 최신 Day40 커밋은 Day39 커밋 바로 다음 1개 커밋
- Day40 변경 파일 26개, 삭제 파일 없음
- `Shop` 씬과 `.meta` 포함
- `Shop` 씬 Build Settings 등록 확인
- `Resources/Shops/SHOP_LOBBY.asset`과 Runtime 로드 경로 일치
- 기존 전투 Gold 플래그와 `GoldCurrencyService` 저장 규칙 일치
- 구매 전 잔액 및 MaxStack 검증 포함
- 일반 아이템 구매 실패 시 Gold 환불 처리 포함
- 장비 구매 실패 시 생성 인스턴스 롤백 및 Gold 환불 처리 포함
- 일반 아이템 판매 시 보유량 검증 포함
- 구매·판매 성공 후 `SaveCurrent()` 호출 확인
- Gold 및 기본 상점 거래 EditMode 테스트 추가
- `Devlogs/Day40/README.md`는 아직 원격 커밋에 없음

다만 최신 커밋에 GitHub Actions 상태 체크나 워크플로 실행 기록이 없어 **Unity 컴파일 및 EditMode Test Runner의 실제 통과 여부는 이번 검토만으로 확인할 수 없다.**

## 12. Day40 완료 정리

Day40에서는 Day39까지 구축한 던전 파밍 루프에 Gold 소비와 아이템 거래를 연결했다.

핵심적으로 던전 보상 → Gold 저장 → Lobby → Shop → 구매/판매 → 인벤토리 변경 → 저장의 기본 경제 순환이 이어졌다.

일반 아이템은 기존 가방 서비스를 그대로 사용하고 장비는 기존 고유 장비 인스턴스 구조를 사용했기 때문에, 앞선 Day36~39의 저장·인벤토리 구조를 교체하지 않고 상점 기능을 확장했다.

## 다음 작업

- Day41 날짜 및 시간대 Runtime / SaveData 구축
- 날짜 변경과 Lobby 표시 상태 연결
- 상점 재고를 날짜 시스템과 연결할 수 있는 확장 지점 정리
- 장비 구매 롤백 및 판매 불가 상품 회귀 테스트 보강
- Unity EditMode Test Runner에서 Day40 테스트 실행
- 실제 플레이를 기준으로 임시 구매·판매 가격 조정
