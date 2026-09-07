# Project H — Phase 1 Day 32 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 32
- 개발 일지 추가 전 기준 커밋: `925365ae8a792ed28daef322c6cfe3c1d6cb2f0a`
- 기준 커밋 메시지: `32`
- 이전 커밋: `e6305d3d8ab398be93bf32e0f30c24a10626dd23`
- 주제: 장비 데이터, 장비 슬롯, 장비 능력치 옵션, 카탈로그 등록 및 검증 구조

---

## 목표

Day31까지 캐릭터 경험치·레벨 성장 결과와 저장 회귀 검증을 정리했다.

Day32에서는 이후 장비 인벤토리, 착용/해제, Runtime Stats 반영을 구현하기 전에 장비 자체를 표현할 수 있는 정적 데이터 구조를 만든다.

이번 일차의 핵심 방향:

- 기존 `ItemData`를 장비의 공통 아이템 정보로 재사용한다.
- 장비 전용 슬롯을 `Weapon`, `Armor`로 구분한다.
- 장비가 증가시킬 수 있는 전투 능력치 종류를 정의한다.
- 장비 한 개에 여러 능력치 옵션을 등록할 수 있게 한다.
- `ProjectHDataCatalog`에서 장비 데이터를 함께 관리한다.
- `DataManager`에서 장비 ID 조회와 연결 유효성 검증을 수행한다.
- 잘못된 장비 데이터가 등록되었을 때 초기화를 거부하고 검증 오류를 남긴다.
- EditMode 테스트에서 정상 등록과 주요 오류 상황을 검증한다.

---

## 장비 슬롯 및 능력치 종류

`DataEnums.cs`에 Day32 장비 시스템용 열거형을 추가했다.

### EquipmentSlot

현재 장비 슬롯:

- `Weapon`
- `Armor`

Day32에서는 이후 시스템에 필요한 최소 슬롯만 정의하고, 장신구 등 추가 슬롯은 아직 확장하지 않는다.

### EquipmentStatType

장비 옵션에서 사용할 수 있는 능력치 종류:

- `MaxHp`
- `Attack`
- `Defense`
- `Resistance`
- `AttackSpeed`
- `Accuracy`
- `CriticalRate`
- `AttackRange`
- `MoveSpeed`

기존 캐릭터 및 전투 스탯 계층에서 사용 중인 주요 수치와 대응할 수 있도록 구성했다.

---

## EquipmentStatOption

신규 `EquipmentStatOption` 구조체를 추가했다.

장비 옵션 하나는 다음 두 값으로 구성된다.

`EquipmentStatType`
→ 어떤 능력치를 변경하는지 지정

`Value`
→ 해당 능력치에 적용할 수치 저장

예시:

`Attack +20`

`Defense +7`

`CriticalRate +0.03`

Day32에서는 옵션 데이터만 정의하며, 실제 전투 Runtime Stats에 더하는 처리는 이후 Day35 범위로 남긴다.

---

## EquipmentData

신규 `EquipmentData` ScriptableObject를 추가했다.

`EquipmentData`는 장비 이름이나 등급을 별도로 중복 저장하지 않고 기존 `ItemData`를 기반 데이터로 참조한다.

구조:

`ItemData`
→ ID
→ 표시 이름
→ 설명
→ ItemType
→ ItemGrade

`EquipmentData`
→ 기반 `ItemData`
→ `EquipmentSlot`
→ `EquipmentStatOption` 목록

장비 ID는 기반 `ItemData.Id`를 그대로 사용한다.

따라서 장비와 인벤토리 아이템의 ID가 서로 달라지는 문제를 피하고, 기존 아이템 데이터 구조를 유지한 채 장비 전용 정보만 확장한다.

---

## 장비 옵션 합산

`EquipmentData.GetStatValue()`를 추가했다.

같은 종류의 옵션이 여러 개 존재하면 모두 합산한다.

예:

`Attack +20`
+
`Attack +5.5`
=
`Attack +25.5`

해당 장비에 요청한 능력치 옵션이 없으면 `0`을 반환한다.

---

## ProjectHDataCatalog 확장

기존 `ProjectHDataCatalog`에 장비 목록을 추가했다.

기존 목록:

- Character
- Monster
- Dungeon
- Item

Day32 추가:

- Equipment

기존 Monster 및 기타 데이터 목록은 유지한 상태에서 장비 데이터만 확장했다.

이를 통해 프로젝트의 기존 데이터 카탈로그 흐름을 깨지 않고 장비를 같은 방식으로 등록할 수 있다.

---

## DataManager 장비 Registry

`DataManager`에 `DataRegistry<EquipmentData>`를 추가했다.

추가된 주요 기능:

- `EquipmentCount`
- `GetEquipment(string id)`
- 장비 Registry 초기화
- 장비와 ItemData 연결 검증

정상적인 장비 데이터는 기존 데이터들과 함께 초기화되고 ID를 통해 조회할 수 있다.

---

## 장비 연결 검증

장비가 카탈로그에 등록되었더라도 기반 `ItemData` 연결이 잘못되어 있으면 데이터 초기화를 완료하지 않는다.

검증 항목:

- `EquipmentData`에 `ItemData`가 연결되어 있는지 확인
- 연결된 `ItemData.Type`이 `ItemType.Equipment`인지 확인
- 장비가 참조하는 `ItemData`가 Catalog의 `Items`에도 등록되어 있는지 확인
- 동일 ID라 하더라도 Catalog에 등록된 것과 다른 `ItemData` 인스턴스를 참조하지 않는지 확인
- 장비 ID 중복 여부 확인

문제가 발견되면 `ValidationErrors`에 내용을 남기고 기존 `DataManager` 방식대로 오류 로그를 출력한 뒤 초기화를 완료하지 않는다.

---

## 기존 데이터 구조 회귀 수정

Day32 구현 과정에서 기존 데이터 정의를 장비 구조에 맞춰 전체 교체하면 프로젝트의 기존 참조가 깨지는 문제가 확인되었다.

따라서 최종 Day32 구조에서는 기존 프로젝트의 다음 요소를 유지했다.

- 기존 `CharacterJob`
- 기존 `BattlePosition`
- 기존 `CharacterRole`
- 기존 `ItemType`
- 기존 `ItemGrade`
- 기존 `MonsterData` 카탈로그 및 DataManager Registry

장비에 필요한 enum과 Registry만 기존 구조 위에 추가하는 방식으로 정리했다.

이로써 기존 전투, 파티, 캐릭터 데이터 코드가 사용하는 타입을 유지하면서 Day32 장비 구조를 확장했다.

---

## EditMode 테스트

신규 `EquipmentDataTests`를 추가했다.

검증 범위:

### EquipmentStatOption_StoresTypeAndValue

장비 옵션에 능력치 종류와 값이 정상 보관되는지 확인한다.

### EquipmentData_UsesBackingItemAndSumsMatchingStats

기반 `ItemData`의 ID, 이름, 등급을 장비가 재사용하는지 확인한다.

동일 능력치 옵션이 여러 개일 때 합산되는지도 검증한다.

### DataManager_RegistersAndFindsEquipment

정상 장비를 Catalog에 등록한 뒤 `DataManager.GetEquipment()`로 다시 조회할 수 있는지 확인한다.

### Initialize_NonEquipmentBackingItemReportsValidationError

`ItemType.Equipment`가 아닌 아이템을 장비 원본으로 연결하면 초기화를 거부하고 오류를 기록하는지 확인한다.

### Initialize_UnregisteredBackingItemReportsValidationError

장비가 참조하는 기반 `ItemData`가 Catalog의 Items에 등록되어 있지 않으면 오류를 기록하는지 확인한다.

### Initialize_DuplicateEquipmentIdReportsValidationError

동일 장비 ID가 중복 등록되면 Registry 검증 오류가 발생하는지 확인한다.

---

## TestRunner 오류 로그 처리

오류 상황을 검증하는 테스트는 `DataManager`가 의도적으로 `Debug.LogError()`를 출력한다.

Unity TestRunner는 예상하지 않은 Error 로그를 테스트 실패로 처리하므로, 다음 세 검증 테스트에 `LogAssert.Expect()`를 추가했다.

- 비장비 ItemData 연결
- Items 미등록 ItemData 연결
- 장비 ID 중복

테스트가 단순히 오류 로그를 무시하는 것이 아니라 예상한 정확한 오류 메시지가 발생하는지 함께 검증하도록 구성했다.

---

## 생성 파일

- `Assets/ProjectH/Scripts/Data/EquipmentData.cs`
- `Assets/ProjectH/Scripts/Data/EquipmentData.cs.meta`
- `Assets/ProjectH/Scripts/Data/EquipmentStatOption.cs`
- `Assets/ProjectH/Scripts/Data/EquipmentStatOption.cs.meta`
- `Assets/ProjectH/Tests/EditMode/EquipmentDataTests.cs`
- `Assets/ProjectH/Tests/EditMode/EquipmentDataTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Data/DataEnums.cs`
- `Assets/ProjectH/Scripts/Data/DataManager.cs`
- `Assets/ProjectH/Scripts/Data/ProjectHDataCatalog.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab 변경

없음.

Day32는 정적 장비 데이터와 데이터 조회/검증 계층을 구축하는 작업이므로 Scene, Prefab, Inspector 전용 Runtime UI 변경은 포함하지 않는다.

---

## SaveData / 전투 스탯 변경

없음.

Day32에서는 장비를 소유하거나 착용하는 저장 구조를 추가하지 않는다.

또한 장비 능력치를 `BattleStats`에 실제로 적용하지 않는다.

관련 범위는 다음 일차로 분리한다.

- Day33: 장비 인벤토리 및 저장
- Day34: 장비 착용·해제·교체 및 저장
- Day35: 장비 능력치 Runtime Stats 반영
- Day36: 장비 관리 UI

---

## 최신 커밋 변경 규모

Day31 커밋 `e6305d3d8ab398be93bf32e0f30c24a10626dd23`과 Day32 구현 커밋 `925365ae8a792ed28daef322c6cfe3c1d6cb2f0a` 비교:

- Day31 대비 1개 커밋 앞섬
- 총 9개 파일 변경
- 기존 파일 수정 3개
- 신규 파일 6개
- 삭제 파일 0개
- `DataEnums.cs`: +19
- `DataManager.cs`: +57 / -1
- `ProjectHDataCatalog.cs`: +2
- `EquipmentData.cs`: +41
- `EquipmentStatOption.cs`: +21
- `EquipmentDataTests.cs`: +170
- 신규 `.meta`: 3개

---

## 최신 커밋 검토 상태

최신 Day32 커밋의 diff와 현재 저장소 파일을 기준으로 정적 검토했다.

확인한 사항:

- 기존 `BattlePosition`, `CharacterRole` 등 기존 프로젝트 enum이 유지된다.
- 기존 `MonsterData` Catalog/Registry가 유지된다.
- 장비용 `EquipmentSlot`, `EquipmentStatType`만 추가된다.
- `EquipmentData`는 `IDataRecord`를 구현한다.
- 장비 Registry는 기존 `DataRegistry<T>` 방식으로 동작한다.
- 장비 기반 아이템 검증은 기존 `ItemData.Type` 속성을 사용한다.
- 정상 장비 조회 테스트가 포함되어 있다.
- 잘못된 장비 연결과 중복 ID 검증 테스트가 포함되어 있다.
- 오류 로그 검증 테스트에는 `LogAssert.Expect()`가 등록되어 있다.
- Scene / Prefab / SaveData / BattleStats 변경은 없다.

현재 GitHub Commit Status에는 실행된 상태 체크가 없으며, 연결된 GitHub Actions workflow run도 확인되지 않는다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일 또는 EditMode Test Runner 전체 통과를 단정하지 않는다.

최신 커밋 코드 기준 정적 검토에서 개발 일지 작성을 중단해야 할 명확한 차단 문제는 확인되지 않았다.

---

## Day32 완료 범위

현재 장비 데이터 흐름:

`ItemData 생성`
→ `ItemType.Equipment` 지정
→ `EquipmentData`에서 ItemData 연결
→ Weapon / Armor 슬롯 지정
→ 장비 능력치 옵션 등록
→ `ProjectHDataCatalog`의 Items / Equipments 등록
→ `DataManager.Initialize()`
→ 장비 연결 유효성 검사
→ `GetEquipment(id)`로 조회

Day32에서는 장비 시스템의 정적 데이터 기반을 구축했다.

다음 Day33에서는 이 장비 데이터를 실제 플레이어가 보유할 수 있도록 장비 인벤토리와 SaveData 저장 구조를 연결하는 단계로 넘어간다.
