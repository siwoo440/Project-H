# Project H — Phase 1 Day 38 개발 일지

- 날짜: 2026-09-08
- 단계: Phase 1 / Day 38
- 개발 일지 추가 전 기준 커밋: `9040122f2d99aa8771c8905b0280fa3f650cd459`
- 기준 커밋 메시지: `38`
- 이전 커밋: `d344d5b872db8b0a43a44c4c1fbef976e35afada`
- 주제: 일반 아이템 인벤토리·사용 시스템 및 7열 ScrollView 가방 UI 구축

---

## 목표

Day37에서 ItemData 공통 정책과 Quest 타입을 확정한 뒤, Day38에서는 정적 아이템 데이터를 실제 SaveData와 연결해 일반 아이템의 보유 수량, 획득, 제거, 사용 흐름을 구현했다.

동시에 Lobby에서 접근 가능한 신규 Bag 씬을 추가하고, 가방 화면에 7열 Grid 기반 ScrollView와 우측 아이템 상세 패널을 구성했다.

```text
ItemData
→ ItemInventoryService
→ SaveData.ItemInventory
→ BagScreenController
→ 아이템 선택
→ 우측 상세 표시
→ Consumable 사용
→ 수량 감소
→ SaveCurrent()
```

---

## 일반 아이템 저장 구조

신규 `ItemStackSaveData`를 추가했다.

일반 아이템은 장비와 달리 고유 InstanceId를 만들지 않고 아이템 ID와 수량으로 저장한다.

```text
ItemStackSaveData
├─ ItemId
└─ Quantity
```

예:

```text
IT_POTION_SMALL × 5
IT_MATERIAL_001 × 12
IT_QUEST_KEY_001 × 1
```

장비는 기존 `EquipmentInstanceSaveData`와 `EquipmentInventory`를 계속 사용한다.

---

## SaveData ItemInventory

`SaveData`에 일반 아이템 인벤토리를 추가했다.

```text
SaveData
├─ Characters
├─ Party
├─ StoryFlags
├─ EquipmentInventory
└─ ItemInventory
```

기존 저장 파일에 ItemInventory가 없어도 `EnsureDefaults()`에서 빈 일반 아이템 인벤토리를 복원하는 구조를 사용한다.

---

## ItemInventoryService

신규 `ItemInventoryService`를 추가했다.

주요 기능:

```text
GetCount()
TryAdd()
TryRemove()
```

획득 시 SaveData, DataManager, ItemData, 수량, Equipment 타입 여부, MaxStack을 검증한다.

제거 시 보유 수량보다 많이 제거하지 못하도록 차단하며, 수량이 0이 되면 빈 스택을 인벤토리에서 제거한다.

장비는 일반 인벤토리에 넣지 않고 기존 EquipmentInventory를 사용한다.

---

## ItemUseService

신규 `ItemUseService`를 추가했다.

Day38 기준 직접 사용 가능한 타입은 `Consumable`이다.

```text
ItemData 조회
→ Consumable 확인
→ 보유 수량 확인
→ ItemInventoryService.TryRemove(..., 1)
→ 남은 수량 반환
```

Material과 Quest는 직접 사용하지 못하며 실패 시 수량을 유지한다.

현재 Day38의 사용 처리는 소비 아이템 1개 차감과 저장 흐름의 기반이다. 캐릭터 CurrentHp 같은 실제 효과 대상은 아직 일반화되어 있지 않아 물약 사용에 따른 HP 회복 효과 자체는 이번 범위에 포함하지 않았다.

---

## ItemData Icon

기존 `ItemData`에 선택적 `Sprite Icon` 필드를 추가했다.

아이콘 Sprite가 있으면 Bag 상세 상단의 일러스트 영역에 표시할 수 있고, 아이콘이 없어도 placeholder로 상세 정보를 확인할 수 있다.

---

## 신규 Bag 씬

`Bag.unity`를 추가하고 `GameScenes.Bag`을 등록했다.

`ProjectSettings/EditorBuildSettings.asset`에도 Bag 씬을 등록해 일반 씬 전환 경로에서 로드할 수 있도록 구성했다.

`BagSceneRuntimePatch`는 Bag 씬 로드 시 필요한 카메라와 `BagScreenController`를 Runtime에서 보장한다.

---

## 가방 UI

`BagScreenController`가 Runtime에서 가방 UI를 구성한다.

```text
┌──────────────────────────────────────────────┐
│                    가방                      │
├────────────────────────────┬─────────────────┤
│ [01][02][03][04][05][06][07]│ 아이템 일러스트 │
│ [08][09][10][11][12][13][14]│                 │
│ [15][16][17][18][19][20][21]├─────────────────┤
│ [22][23][24][25][26][27][28]│ 아이템 이름      │
│            ↓               │ 유형 / 등급      │
│        ScrollView          │ 보유 수량        │
│            ↓               │ 설명             │
│                            │ [사용]           │
└────────────────────────────┴─────────────────┘
```

가방 슬롯 규칙:

```text
가로 열 수      7
최소 슬롯 수   28
배치 방식       가로 우선 Grid
스크롤          세로 ScrollView
아이템 증가     7열 유지 후 아래로 자동 확장
```

오른쪽은 상단 일러스트, 하단 이름·유형·등급·수량·설명·사용 버튼 영역으로 나뉜다.

---

## Lobby 가방 버튼

기존 Lobby `BottomNavigation`에 Runtime 가방 버튼을 추가하도록 `LobbyScreenController`를 확장했다.

```text
Lobby
→ 가방 버튼
→ Bag
```

기존 하단 메뉴와 가방 버튼이 함께 표시되도록 내비게이션 배치 조정 코드도 포함한다.

---

## 테스트

신규 EditMode 테스트는 세 영역이다.

### ItemInventoryServiceTests

```text
Consumable 수량 누적
MaxStack 초과 차단
Equipment 일반 인벤토리 등록 차단
수량 0 도달 시 스택 제거
JSON 왕복 후 수량 유지
이전 SaveData의 ItemInventory 기본값 복원
```

### ItemUseServiceTests

```text
Consumable 사용 시 1개 감소
Material 직접 사용 차단
Quest 직접 사용 차단
미보유 Consumable 사용 차단
```

### BagScreenLayoutTests

```text
SlotColumnCount = 7
MinimumSlotCount = 28
```

---

## 생성 파일

Day38에서 신규 파일 18개를 추가했다.

```text
Assets/ProjectH/Scenes/Bag.unity
Assets/ProjectH/Scenes/Bag.unity.meta

Assets/ProjectH/Scripts/Save/ItemInventoryService.cs
Assets/ProjectH/Scripts/Save/ItemInventoryService.cs.meta
Assets/ProjectH/Scripts/Save/ItemStackSaveData.cs
Assets/ProjectH/Scripts/Save/ItemStackSaveData.cs.meta
Assets/ProjectH/Scripts/Save/ItemUseService.cs
Assets/ProjectH/Scripts/Save/ItemUseService.cs.meta

Assets/ProjectH/Scripts/UI/BagSceneRuntimePatch.cs
Assets/ProjectH/Scripts/UI/BagSceneRuntimePatch.cs.meta
Assets/ProjectH/Scripts/UI/BagScreenController.cs
Assets/ProjectH/Scripts/UI/BagScreenController.cs.meta

Assets/ProjectH/Tests/EditMode/BagScreenLayoutTests.cs
Assets/ProjectH/Tests/EditMode/BagScreenLayoutTests.cs.meta
Assets/ProjectH/Tests/EditMode/ItemInventoryServiceTests.cs
Assets/ProjectH/Tests/EditMode/ItemInventoryServiceTests.cs.meta
Assets/ProjectH/Tests/EditMode/ItemUseServiceTests.cs
Assets/ProjectH/Tests/EditMode/ItemUseServiceTests.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectH/Scripts/Core/GameScenes.cs
Assets/ProjectH/Scripts/Data/ItemData.cs
Assets/ProjectH/Scripts/Save/SaveData.cs
Assets/ProjectH/Scripts/UI/LobbyScreenController.cs
ProjectSettings/EditorBuildSettings.asset
```

---

## 삭제 파일

없음.

---

## 변경 규모

Day37 `d344d5b872db8b0a43a44c4c1fbef976e35afada` 대비 Day38 `9040122f2d99aa8771c8905b0280fa3f650cd459`:

```text
커밋 수       1
변경 파일     23
신규 파일     18
수정 파일      5
삭제 파일      0
```

---

## 최신 커밋 검토 상태

최신 GitHub `main`의 Day38 커밋과 관련 소스를 기준으로 정적 검토했다.

확인한 범위:

```text
Bag 씬 및 Build Settings 등록
GameScenes.Bag 등록
ItemStackSaveData 추가
SaveData.ItemInventory 추가
ItemInventoryService 획득/제거/MaxStack 처리
Equipment 일반 인벤토리 등록 차단
ItemUseService Consumable 1개 소비
Material / Quest 사용 차단
ItemData Icon 필드 추가
Bag 7열 Grid
최소 28칸
세로 ScrollView
우측 일러스트/상세 영역
Lobby 가방 진입 경로
일반 아이템 JSON 저장 회귀 테스트
가방 레이아웃 규칙 테스트
```

현재 GitHub Commit Status에는 상태 체크가 없고, 해당 커밋에 연결된 GitHub Actions workflow run도 없다.

따라서 Unity Editor 전체 컴파일, Bag 씬 실제 플레이 동작, EditMode TestRunner 전체 통과는 이 개발 일지에서 확정하지 않는다.

최신 소스 정적 검토에서는 개발 일지 작성을 중단해야 할 명확한 차단 문제를 확인하지 못했다.

---

## Day38 완료 범위

```text
ItemData
→ ItemInventoryService.TryAdd()
→ SaveData.ItemInventory
→ JSON 저장
→ Bag 씬
→ 7열 ScrollView
→ 아이템 선택
→ 이름 / 설명 / 수량 표시
→ Consumable 사용
→ ItemInventoryService.TryRemove(..., 1)
→ SaveCurrent()
```

다음 Day39에서는 이 일반 아이템 획득 API와 기존 장비 인스턴스 획득 API를 던전 보상과 연결해 드롭 테이블 시스템을 구축하는 단계로 이어진다.
