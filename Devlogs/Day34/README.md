# Project H — Phase 1 Day 34 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 34
- 개발 일지 추가 전 기준 커밋: `27c70c491e51c44ab1172f9cd4c68f6105236382`
- 기준 커밋 메시지: `34`
- 이전 커밋: `ce470805a94ddf7e2c8281de7f3a87b2859cab83`
- 주제: 캐릭터별 장비 착용·해제·교체, 임시 캐릭터 장비 화면, Lobby 진입 연결, Input System 및 카메라 회귀 수정

---

## 목표

Day33에서 플레이어가 장비를 고유 인스턴스 단위로 획득·보유·저장할 수 있는 장비 인벤토리를 구축했다.

Day34에서는 해당 장비 인스턴스를 실제 캐릭터에게 장착하고, 장착 상태를 저장하며, 임시 캐릭터 상세 화면에서 장비 선택·장착·교체·해제를 직접 확인할 수 있는 흐름을 구축했다.

이번 일차의 핵심 목표:

- 캐릭터별 Weapon / Armor 장착 상태 저장
- 보유 장비 인스턴스를 이용한 장착·해제·교체
- 동일 장비 인스턴스의 캐릭터 간 중복 착용 방지
- 장착 중인 장비의 인벤토리 제거 방지
- 장착 상태 JSON 저장·불러오기 유지
- 임시 `Character` 씬 추가
- Lobby의 기존 하단 `캐릭터` 버튼과 Character 씬 연결
- 캐릭터 장비 Runtime UI 구성
- 테스트용 장비 데이터 생성 및 Catalog 등록
- Input System 전용 설정과 충돌하던 Legacy UI 입력 모듈 제거
- 카메라 없는 Character 씬의 `No cameras rendering` 상태 보완

---

## 캐릭터 장비 저장 구조

신규 `CharacterEquipmentSaveData`를 추가했다.

현재 지원하는 장비 슬롯:

- Weapon
- Armor

캐릭터 저장 구조는 다음과 같이 확장되었다.

```text
CharacterSaveData
├─ CharacterId
├─ Level
├─ Experience
└─ Equipment
   ├─ WeaponInstanceId
   └─ ArmorInstanceId
```

장비 슬롯에는 원본 `EquipmentId`가 아니라 Day33에서 생성한 고유 `InstanceId`를 저장한다.

따라서 같은 종류의 장비를 여러 개 보유하더라도 실제로 어떤 장비 한 개를 착용했는지 구별할 수 있다.

---

## CharacterEquipmentService

신규 `CharacterEquipmentService`를 추가하여 장착 로직을 SaveData와 UI에서 분리했다.

주요 기능:

- `TryEquip()`
- `TryUnequip()`
- `GetEquippedInstance()`
- `FindEquippedCharacter()`
- `IsEquipped()`

### 장착

장비 인스턴스의 `EquipmentId`를 통해 `DataManager.GetEquipment()`로 원본 장비를 조회한다.

이후 `EquipmentData.Slot`에 따라 Weapon 또는 Armor 슬롯에 자동으로 장착한다.

### 교체

동일 슬롯에 기존 장비가 있을 때 새 장비를 착용하면 슬롯의 `InstanceId`를 새 장비로 교체한다.

기존 장비 인스턴스는 인벤토리에 그대로 남는다.

### 중복 착용 방지

다른 캐릭터가 이미 사용 중인 동일 `InstanceId`는 추가 캐릭터가 동시에 장착할 수 없도록 차단했다.

---

## 장착 장비 인벤토리 제거 방지

기존 Day33의 `TryRemoveEquipmentInstance()`는 인벤토리에 해당 인스턴스가 존재하면 제거할 수 있었다.

Day34에서는 제거 전에 모든 캐릭터의 장착 상태를 확인한다.

장착 중이면 제거를 거부한다.

```text
장착 중 장비
→ TryRemoveEquipmentInstance()
→ 제거 거부
→ 인벤토리 유지
```

이를 통해 장착 슬롯이 존재하지 않는 장비 인스턴스를 참조하는 상태를 예방한다.

---

## 저장 호환

`CharacterSaveData.EnsureDefaults()`를 추가하여 이전 저장 데이터에 장착 정보가 존재하지 않아도 빈 `CharacterEquipmentSaveData`를 복원하도록 구성했다.

SaveData의 기존 버전은 그대로 유지한다.

장착 상태는 기존 `SaveManager`의 `JsonUtility` 저장/불러오기 흐름에 포함된다.

---

## Character 씬

신규 씬:

`Assets/ProjectH/Scenes/Character.unity`

`GameScenes.Character`를 추가하고 `EditorBuildSettings`에도 Character 씬을 등록했다.

Character 씬 자체는 최소 구조로 두고 실제 Day34 장비 UI는 Runtime에서 생성하는 프로토타입 형태로 구성했다.

---

## 임시 캐릭터 장비 화면

`CharacterEquipmentScreenController`를 추가했다.

현재 화면에서 확인할 수 있는 기능:

- 캐릭터 전환
- 현재 레벨 표시
- Weapon 슬롯
- Armor 슬롯
- 준비 중인 추가 장비 슬롯 표시
- 보유 장비 목록
- 장비 이름 및 등급 표시
- 장비 능력치 옵션 표시
- 장착
- 교체
- 해제
- 장비 변경 직후 저장
- 테스트 장비 지급

상단의 `스탯 / 장비 / 룬 / 스킬 / 프로필` 중 Day34에서는 장비 탭만 실제 기능을 제공하며 나머지는 프로토타입 상태다.

---

## 테스트용 장비 데이터

장비 화면과 장착 흐름을 실제로 확인할 수 있도록 임시 장비 4종을 추가했다.

### 무기

`EQ_WEAPON_TRAINING`
- 훈련용 검
- Attack +20

`EQ_WEAPON_IRON`
- 철제 검
- Attack +35
- CriticalRate +2%

### 방어구

`EQ_ARMOR_TRAINING`
- 훈련용 갑옷
- MaxHp +120
- Defense +15

`EQ_ARMOR_GUARD`
- 수호자 갑옷
- MaxHp +200
- Defense +25
- Resistance +10

각 장비는 `ItemData`와 `EquipmentData`를 함께 생성하고 `ProjectHDataCatalog`의 Items / Equipments 목록에 등록했다.

Day35 전이므로 이 장비 옵션은 장비 상세 화면에는 표시되지만 아직 실제 `BattleStats`에는 적용하지 않는다.

---

## Lobby 캐릭터 버튼 연결

초기 Day34 구현에서는 Character 씬 진입을 위해 Lobby 우측 상단에 별도 Runtime 버튼을 만들었다.

실제 Lobby에는 이미 하단 내비게이션에 `캐릭터` 버튼이 존재하므로 최종 구조에서는 신규 버튼 생성을 제거했다.

현재 흐름:

```text
Lobby
→ BottomNavigation
→ 기존 캐릭터 버튼
→ LobbyScreenController.GoCharacter()
→ GameScenes.Character
```

`LobbyScreenController`가 기존 하단 버튼 중 라벨이 `캐릭터`인 버튼을 찾아 Runtime에서 `GoCharacter()` 이벤트를 연결한다.

이전 Runtime 버튼 이름인 `CharacterSceneEntryButton`이 남아 있을 경우 제거하는 정리 처리도 포함했다.

---

## Input System 회귀 수정

프로젝트의 UI 입력은 신규 Input System을 사용한다.

초기 Character 화면 구현에서는 EventSystem이 없을 경우 Legacy `StandaloneInputModule`을 생성하여 다음 예외가 발생했다.

```text
InvalidOperationException:
You are trying to read Input using the UnityEngine.Input class,
but you have switched active Input handling to Input System package
```

최종 Day34에서는 Character 화면도 `InputSystemUIInputModule`을 사용하도록 변경했다.

기존 EventSystem에 `StandaloneInputModule`이 존재하면:

- Legacy 모듈 비활성화
- Legacy 모듈 제거
- `InputSystemUIInputModule`이 없다면 추가

EventSystem 자체가 없다면 처음부터:

```text
EventSystem
+
InputSystemUIInputModule
```

형태로 생성한다.

---

## Character 씬 카메라 회귀 수정

초기 Character 씬에는 Camera가 없어 Game View에 다음 안내가 표시되었다.

```text
Display 1
No cameras rendering
```

Character 화면은 `ScreenSpaceOverlay` Canvas이므로 UI 자체는 Camera 없이도 표시될 수 있지만, Game View 경고 상태를 제거하고 정상적인 씬 구성을 유지하기 위해 Character 진입 시 카메라 존재를 확인하도록 보완했다.

카메라가 없는 경우 `CharacterCamera`를 Runtime에서 생성한다.

설정:

- MainCamera 태그
- 위치 `(0, 0, -10)`
- SolidColor clear
- Character 화면 배경과 유사한 색상
- Orthographic

---

## EditMode 테스트

신규 `CharacterEquipmentServiceTests`를 추가했다.

주요 검증 대상:

- Weapon 인스턴스가 Weapon 슬롯에 저장되는지 확인
- 동일 슬롯 새 장비 장착 시 기존 장비가 교체되는지 확인
- 동일 InstanceId를 다른 캐릭터가 동시에 착용하지 못하는지 확인
- Armor 장비가 Armor 슬롯에 자동 장착되는지 확인
- 해제 후 장비 인스턴스가 인벤토리에 유지되는지 확인
- 장착 중인 장비 인스턴스 제거가 차단되는지 확인
- JSON 저장·불러오기 후 Weapon / Armor InstanceId가 유지되는지 확인

---

## 생성 파일

주요 신규 요소:

- `Assets/ProjectH/Scenes/Character.unity`
- `Assets/ProjectH/Scenes/Character.unity.meta`
- `Assets/ProjectH/Scripts/Save/CharacterEquipmentSaveData.cs`
- `Assets/ProjectH/Scripts/Save/CharacterEquipmentSaveData.cs.meta`
- `Assets/ProjectH/Scripts/Save/CharacterEquipmentService.cs`
- `Assets/ProjectH/Scripts/Save/CharacterEquipmentService.cs.meta`
- `Assets/ProjectH/Scripts/UI/CharacterEquipmentScreenController.cs`
- `Assets/ProjectH/Scripts/UI/CharacterEquipmentScreenController.cs.meta`
- `Assets/ProjectH/Scripts/UI/CharacterSceneRuntimePatch.cs`
- `Assets/ProjectH/Scripts/UI/CharacterSceneRuntimePatch.cs.meta`
- `Assets/ProjectH/Tests/EditMode/CharacterEquipmentServiceTests.cs`
- `Assets/ProjectH/Tests/EditMode/CharacterEquipmentServiceTests.cs.meta`
- 테스트용 ItemData 4종 및 `.meta`
- 테스트용 EquipmentData 4종 및 `.meta`
- `Assets/ProjectH/Data/Equipments.meta`

---

## 수정 파일

- `Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset`
- `Assets/ProjectH/Scripts/Core/GameScenes.cs`
- `Assets/ProjectH/Scripts/Save/SaveData.cs`
- `Assets/ProjectH/Scripts/UI/LobbyScreenController.cs`
- `ProjectSettings/EditorBuildSettings.asset`

---

## 삭제 파일

없음.

---

## 최신 커밋 변경 규모

Day33 커밋 `ce470805a94ddf7e2c8281de7f3a87b2859cab83` 대비 Day34 최신 커밋 `27c70c491e51c44ab1172f9cd4c68f6105236382`:

- 1개 커밋 앞섬
- 총 34개 파일 변경
- 신규 Character 씬 추가
- 장비 장착 저장/서비스 추가
- Character 장비 Runtime UI 추가
- 테스트 장비 데이터 4종 추가
- 장착 서비스 EditMode 테스트 추가
- Lobby 기존 캐릭터 버튼 연결
- Input System 회귀 수정
- Character Camera 보완

---

## 검토 상태

최신 GitHub 커밋에서 다음 최종 코드 상태를 확인했다.

- `CharacterEquipmentScreenController`가 `UnityEngine.InputSystem.UI`를 참조한다.
- EventSystem 생성 시 `InputSystemUIInputModule`을 사용한다.
- 기존 `StandaloneInputModule`이 있으면 비활성화·제거하도록 처리한다.
- `CharacterSceneRuntimePatch`는 Lobby 신규 버튼을 만들지 않는다.
- Character 씬에 Camera가 없으면 `CharacterCamera`를 생성한다.
- `LobbyScreenController`는 기존 하단 `캐릭터` 버튼을 찾아 `GoCharacter()`에 연결한다.
- 최신 Day34 커밋은 Day33 대비 단일 커밋으로 구성되어 있다.

현재 GitHub Commit Status와 GitHub Actions workflow run 기록은 없다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일, PlayMode 실행, EditMode TestRunner 전체 통과를 확정하지 않는다.

사용자 환경에서 확인된 Input System 예외와 `No cameras rendering` 문제를 대상으로 한 수정 코드는 최신 커밋에 반영된 상태이며, 최신 소스 정적 검토에서 개발 일지 작성을 중단해야 할 추가적인 명확한 차단 문제는 확인되지 않았다.

---

## Day34 완료 범위

현재 흐름:

```text
Lobby 기존 캐릭터 버튼
→ Character 씬
→ 캐릭터 선택
→ 테스트 장비 획득
→ 장비 선택
→ 장착 / 교체 / 해제
→ CharacterSaveData에 InstanceId 저장
→ SaveCurrent
→ 저장 불러오기 후 장착 상태 복원
```

Day35에서는 현재 장착된 Weapon / Armor의 `EquipmentStatOption`을 `BattleStatsFactory`의 Runtime Stats 계산에 반영하는 단계로 이어진다.
