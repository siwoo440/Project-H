# Project H — Phase 1 Day 37 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 37
- 개발 일지 추가 전 기준 커밋: `4719fc4349b74f73e1a099eddbe9729fc81b0d4b`
- 기준 커밋 메시지: `37`
- 이전 커밋: `0d28935e97563179bd61479defc57c27853fe817`
- 주제: 아이템 데이터 정책·퀘스트 타입·검증 체계 구축 및 캐릭터 장비 UI 배치 정리

---

## 목표

Day37에서는 기존 `ItemData`를 공통 아이템 기반으로 정식화하고, 일반 아이템과 장비 아이템의 ID 규칙 및 MaxStack 정책을 데이터 초기화 단계에서 검증하도록 확장했다.

동시에 Day36에서 구성한 Character 장비 관리 화면의 배치를 조정하여 장비 목록과 장비 상세/비교 영역의 가독성을 개선했다.

핵심 범위:

```text
ItemData 공통 구조 유지
→ ItemType.Quest 추가
→ ID 정책 정의
→ MaxStack 정책 정의
→ DataManager 초기화 검증 연결
→ 퀘스트 아이템 샘플 등록
→ EditMode 검증 테스트 추가
→ 기존 장비 테스트 fixture를 최신 정책에 맞게 보정
```

---

## ItemType Quest 추가

기존 `ItemType` 숫자를 변경하지 않고 마지막에 `Quest = 5`를 추가했다.

```text
Consumable = 0
Material   = 1
Equipment  = 2
Gift       = 3
Special    = 4
Quest      = 5
```

기존 ScriptableObject 에셋의 enum 직렬화 값이 변하지 않도록 기존 0~4 값은 유지했다.

---

## ItemData ID 정책

신규 `ItemDataPolicy`를 추가했다.

아이템 유형별 기본 ID 정책:

```text
일반 아이템
Consumable / Material / Gift / Special / Quest
→ IT_

장비 아이템
Equipment
→ EQ_
```

예:

```text
IT_POTION_SMALL
IT_MATERIAL_001
IT_QUEST_KEY_001

EQ_WEAPON_TRAINING
EQ_ARMOR_TRAINING
```

ID가 정책과 맞지 않으면 `DataManager.Initialize()` 단계에서 ValidationErrors에 기록한다.

---

## MaxStack 정책

공통 아이템은 `MaxStack >= 1`을 요구한다.

장비 아이템은 Day33부터 고유 `EquipmentInstanceSaveData` 단위로 관리하므로 다음 정책을 적용했다.

```text
ItemType.Equipment
→ MaxStack = 1
```

잘못된 장비 데이터가 등록되면:

```text
[Item] 장비 MaxStack은 1이어야 합니다
```

형태의 검증 오류를 기록한다.

---

## DataManager 아이템 검증 연결

기존 데이터 초기화 흐름:

```text
Character
Monster
Dungeon
Item
Equipment
→ Equipment 연결 검증
```

Day37 이후:

```text
Character
Monster
Dungeon
Item
Equipment
→ ItemDataPolicy 검증
→ Equipment 연결 검증
```

`ValidateItemDefinitions()`에서 Catalog의 ItemData를 순회하고 `ItemDataPolicy.Validate()`를 실행한다.

기존 `DataRegistry`의 빈 ID/중복 ID 검증과 함께 사용되므로 ID 정책과 공통 레지스트리 정책을 분리해서 유지한다.

---

## 퀘스트 아이템 샘플

신규 아이템:

```text
ID
IT_QUEST_KEY_001

이름
낡은 조사실 열쇠

Type
Quest

Grade
Common

MaxStack
1
```

해당 ItemData를 `ProjectHDataCatalog` Items 목록에 등록했다.

Day37에서는 퀘스트 아이템의 사용 효과나 문 열기 기능까지 구현하지 않고 정적 데이터 등록·조회 범위만 다룬다.

---

## ItemDataSystemTests

신규 `ItemDataSystemTests`를 추가했다.

검증 범위:

```text
기존 ItemType 0~4 값 유지
Quest = 5 추가 확인
퀘스트 샘플 Catalog 등록 및 GetItem() 조회
일반 아이템 IT_ 접두사 검증
장비 EQ_ 접두사 검증
장비 MaxStack = 1 검증
정상 Material 데이터 초기화 검증
```

의도적으로 잘못된 ItemData를 사용하는 테스트는 기존 장비 검증 테스트와 같은 방식으로 테스트 범위에서 Unity Logger를 잠시 비활성화하여 Console 오류 노이즈를 억제한다.

---

## 기존 장비 TestRunner 회귀 수정

Day37의 `Equipment MaxStack = 1` 정책을 추가한 뒤 기존 장비 테스트 fixture가 `ItemData` 기본값인 MaxStack 99를 그대로 사용하면서 장비 관련 테스트가 연쇄 실패했다.

발생한 주요 증상:

```text
DataManager 초기화 실패
→ CharacterEquipmentService.TryEquip() 실패
→ 장착 슬롯 비어 있음
→ 장착/해제/교체 테스트 연쇄 실패
→ JSON RoundTrip에서도 빈 InstanceId 저장
```

최종 커밋에서는 테스트 fixture를 실제 장비 정책에 맞게 수정했다.

`CharacterEquipmentServiceTests`:

```text
Equipment ItemData
→ maxStack = 1
```

`EquipmentDataTests`:

```text
Equipment
→ maxStack = 1

그 외 테스트 ItemData
→ maxStack = 99
```

프로덕션의 `ItemDataPolicy` 검증 규칙은 약화하지 않고 테스트 데이터만 최신 규칙에 맞췄다.

---

## Character 장비 화면 배치 변경

Day36 장비 관리 기능은 그대로 유지하면서 요청한 UI 위치를 조정했다.

캐릭터 장비 슬롯:

```text
변경 전
우측 위   신발
우측 아래 장갑

변경 후
우측 위   장갑
우측 아래 신발
```

장비 관리 영역:

```text
변경 전

[보유 장비]     [장비 상세]
                [변경 전 → 변경 후]


변경 후

[장비 상세]     [보유 장비]
[변경 전 → 변경 후] [장비 목록]
```

장비 목록을 오른쪽으로 이동하고, 선택 장비 상세 및 능력치 비교 영역을 왼쪽으로 이동했다.

장비 장착·교체·해제, 미리보기 계산, 저장 로직 자체는 변경하지 않았다.

---

## 생성 파일

- `Assets/ProjectH/Data/Items/IT_QUEST_KEY_001.asset`
- `Assets/ProjectH/Data/Items/IT_QUEST_KEY_001.asset.meta`
- `Assets/ProjectH/Scripts/Data/ItemDataPolicy.cs`
- `Assets/ProjectH/Scripts/Data/ItemDataPolicy.cs.meta`
- `Assets/ProjectH/Tests/EditMode/ItemDataSystemTests.cs`
- `Assets/ProjectH/Tests/EditMode/ItemDataSystemTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset`
- `Assets/ProjectH/Scripts/Data/DataEnums.cs`
- `Assets/ProjectH/Scripts/Data/DataManager.cs`
- `Assets/ProjectH/Scripts/UI/CharacterEquipmentScreenController.cs`
- `Assets/ProjectH/Tests/EditMode/CharacterEquipmentServiceTests.cs`
- `Assets/ProjectH/Tests/EditMode/EquipmentDataTests.cs`

---

## 삭제 파일

없음.

---

## 최신 커밋 변경 규모

Day36 커밋 `0d28935e97563179bd61479defc57c27853fe817` 대비 Day37 최신 커밋 `4719fc4349b74f73e1a099eddbe9729fc81b0d4b`:

```text
커밋 수       1
변경 파일     12
신규 파일      6
수정 파일      6
삭제 파일      0
```

---

## 최신 커밋 검토 상태

최신 GitHub `main` 기준으로 정적 검토했다.

확인 내용:

```text
ItemType.Quest = 5 추가
기존 ItemType 0~4 값 유지
ItemDataPolicy 추가
IT_ / EQ_ ID 정책 추가
Equipment MaxStack = 1 정책 추가
DataManager 아이템 정책 검증 연결
IT_QUEST_KEY_001 Catalog 등록
ItemDataSystemTests 추가
CharacterEquipmentServiceTests maxStack=1 반영
EquipmentDataTests 유형별 maxStack fixture 반영
신발/장갑 위치 교환 반영
장비 목록 오른쪽 이동 반영
상세/변경 전후 비교 영역 왼쪽 이동 반영
```

현재 GitHub Commit Status와 연결된 GitHub Actions workflow run은 없다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일이나 EditMode TestRunner 전체 통과를 확정하지 않는다.

다만 이전에 보고된 Day37 장비 테스트 실패의 원인이었던 테스트용 Equipment `maxStack=99` 상태는 최신 커밋에서 `1`로 수정되어 있고, 최신 소스 정적 검토에서 개발 일지 작성을 중단해야 할 추가적인 명확한 차단 문제는 확인하지 못했다.

---

## Day37 완료 범위

현재 아이템 데이터 흐름:

```text
ItemData
→ ItemType / Grade / MaxStack
→ ItemDataPolicy
→ ID / Stack 검증
→ ProjectHDataCatalog
→ DataManager.Initialize()
→ GetItem(ID)
```

다음 Day38에서는 이 정적 ItemData를 실제 SaveData 기반 아이템 인벤토리와 연결하여 수량 저장, 획득, 소비, 사용 기능을 구현하는 단계로 이어진다.
