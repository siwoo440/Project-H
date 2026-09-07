# Project H — Phase 1 Day 35 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 35
- 개발 일지 추가 전 기준 커밋: `0a78c1346160638f8e8321b3f04a683cd90dee0f`
- 기준 커밋 메시지: `35`
- 이전 커밋: `986049f07cca2fa0f976c9525d0beb52ac571c0e`
- 주제: 장착 장비 능력치의 Runtime Stats 반영 및 장비 검증 테스트 로그 정리

---

## 목표

Day32에서 장비 데이터와 슬롯/옵션 구조를 만들고, Day33에서 장비 인벤토리와 고유 인스턴스 저장을 추가했으며, Day34에서 캐릭터별 장착·해제·교체와 Character 씬 장비 UI를 연결했다.

Day35에서는 캐릭터가 실제로 착용한 Weapon / Armor의 능력치 옵션을 전투 Runtime Stats에 반영한다.

최종 계산 흐름:

```text
CharacterData 기본 스탯
→ Day30 레벨 성장 적용
→ Day34 장착 장비 조회
→ Weapon / Armor 옵션 합산
→ BattleStats 생성
```

---

## BattleEquipmentStatBonus

신규 `BattleEquipmentStatBonus`를 추가했다.

지원하는 장비 전투 보정값:

- MaxHp
- Attack
- Defense
- Resistance
- AttackSpeed
- Accuracy
- CriticalRate
- AttackRange
- MoveSpeed

장비가 없거나 기존 호환 API를 사용하는 경우 `BattleEquipmentStatBonus.Empty`를 사용한다.

이를 통해 기존 `BattleStatsFactory.CreateCharacter()` 호출부를 유지하면서 장비 적용 경로를 추가했다.

---

## BattleEquipmentStatCalculator

신규 `BattleEquipmentStatCalculator`를 추가했다.

캐릭터 저장 데이터에 기록된 장착 인스턴스를 기준으로 다음 흐름을 수행한다.

```text
CharacterSaveData.Equipment
→ WeaponInstanceId / ArmorInstanceId
→ SaveData.FindEquipmentInstance()
→ EquipmentInstanceSaveData.EquipmentId
→ DataManager.GetEquipment()
→ EquipmentData.StatOptions
→ BattleEquipmentStatBonus
```

Weapon과 Armor 두 슬롯의 옵션을 합산한다.

다음 비정상 상태는 계산 실패로 처리한다.

- 장착 인스턴스가 SaveData 인벤토리에 존재하지 않음
- EquipmentData가 DataManager에 등록되어 있지 않음
- 저장된 슬롯과 EquipmentData.Slot이 일치하지 않음

손상된 저장 상태를 조용히 무시하지 않고 전투 파티 생성 단계에서 오류를 반환하도록 구성했다.

---

## BattleStatsFactory 확장

기존 레벨 성장 계산을 유지하면서 장비 보정값을 추가로 받을 수 있는 오버로드를 추가했다.

기존 호출:

```text
CreateCharacter(characterData, saveData, runtimeId)
CreateCharacter(characterData, level, runtimeId)
```

위 API는 계속 사용할 수 있으며 장비 보정값이 없는 기존 동작을 유지한다.

Day35 신규 경로:

```text
CreateCharacter(
    characterData,
    saveData,
    runtimeId,
    equipmentBonus
)
```

최종 정수 스탯 계산 예:

```text
성장 MaxHp + 장비 MaxHp
성장 Attack + 장비 Attack
성장 Defense + 장비 Defense
성장 Resistance + 장비 Resistance
```

비성장 스탯은 CharacterData 값에 장비 값을 합산한다.

```text
AttackSpeed + 장비 AttackSpeed
Accuracy + 장비 Accuracy
CriticalRate + 장비 CriticalRate
AttackRange + 장비 AttackRange
MoveSpeed + 장비 MoveSpeed
```

최종 범위 보정은 기존 `BattleStats` 생성자가 담당한다.

---

## BattlePartyRuntime 연결

실제 전투 파티 생성 시 각 캐릭터마다 장착 장비 보정값을 계산하도록 `BattlePartyRuntime`을 수정했다.

기존:

```text
CharacterData
+
CharacterSaveData.Level
→ BattleStatsFactory
```

Day35 이후:

```text
CharacterData
+
CharacterSaveData.Level
+
장착 Weapon / Armor
→ BattleEquipmentStatCalculator
→ BattleStatsFactory
→ 최종 BattleStats
```

따라서 Character 씬에서 장비를 교체한 후 다음 전투에 진입하면 새 장비 능력치 기준으로 Runtime Stats가 다시 생성된다.

---

## 레벨 성장과 장비 합산 예시

훈련용 장비를 착용한 세레나 Lv.5 기준 테스트 값:

```text
레벨 성장 후
MaxHp 2640
Attack 216
Defense 144

훈련용 장비
MaxHp +120
Attack +20
Defense +15

최종 Runtime Stats
MaxHp 2760
Attack 236
Defense 159
```

Day30 레벨 성장 결과를 먼저 계산한 뒤 장비의 고정 보정값을 더한다.

---

## 전체 장비 능력치 지원

Day32에서 정의한 9종 `EquipmentStatType`을 전부 Runtime Stats 계산 경로에 연결했다.

```text
MaxHp
Attack
Defense
Resistance
AttackSpeed
Accuracy
CriticalRate
AttackRange
MoveSpeed
```

명중률과 치명타율은 최종 `BattleStats`에서 0~1 범위로 보정된다.

AttackRange와 MoveSpeed 등도 기존 `BattleStats`의 최소값 보정 규칙을 그대로 사용한다.

---

## EditMode 테스트 추가

신규 `BattleEquipmentStatsTests`를 추가했다.

검증 범위:

- 장착 Weapon + Armor 옵션 합산
- 장착 장비가 실제 BattlePartyRuntime에 반영되는지 확인
- Lv.5 성장 스탯과 장비 옵션 동시 합산
- 9종 EquipmentStatType 전체 반영
- 존재하지 않는 장착 인스턴스의 계산 실패

실제 ProjectHDataCatalog와 세레나 CharacterData 에셋을 로드하는 EditMode 테스트 구조를 사용한다.

---

## 기존 EquipmentDataTests 로그 정리

장비 데이터 유효성 테스트 세 개는 의도적으로 잘못된 데이터를 생성한다.

대상 시나리오:

- 장비가 아닌 ItemData 연결
- Items에 등록되지 않은 ItemData 연결
- 중복 Equipment ID

`DataManager`가 해당 상태를 정상적으로 검출하면 `Debug.LogError()`가 발생하기 때문에 TestRunner 실행 시 Unity Console에 테스트용 빨간 오류 로그가 남았다.

프로덕션의 `DataManager.LogValidationErrors()`는 유지하고, 해당 세 테스트에서만 `InitializeWithoutErrorLogs()`를 사용하도록 수정했다.

처리 흐름:

```text
기존 Unity Logger 상태 저장
→ 테스트 중 Logger 비활성화
→ DataManager.Initialize()
→ ValidationErrors 기록
→ finally에서 Logger 상태 복원
→ Assert로 검증 오류 내용 확인
```

따라서 실제 게임의 장비 검증 오류 로그 기능은 유지하면서 테스트가 의도적으로 발생시키는 Console 오류 로그만 억제한다.

---

## 생성 파일

- `Assets/ProjectH/Scripts/Battle/BattleEquipmentStatBonus.cs`
- `Assets/ProjectH/Scripts/Battle/BattleEquipmentStatBonus.cs.meta`
- `Assets/ProjectH/Scripts/Battle/BattleEquipmentStatCalculator.cs`
- `Assets/ProjectH/Scripts/Battle/BattleEquipmentStatCalculator.cs.meta`
- `Assets/ProjectH/Tests/EditMode/BattleEquipmentStatsTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleEquipmentStatsTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattlePartyRuntime.cs`
- `Assets/ProjectH/Scripts/Battle/BattleStatsFactory.cs`
- `Assets/ProjectH/Tests/EditMode/EquipmentDataTests.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab / SaveData 변경

없음.

Day35는 Day34에서 저장한 장비 장착 상태를 실제 전투 스탯 계산에 연결하는 단계이므로 Character 씬, Prefab, SaveData 스키마를 추가 변경하지 않는다.

---

## 최신 커밋 변경 규모

Day34 커밋 `986049f07cca2fa0f976c9525d0beb52ac571c0e` 대비 Day35 커밋 `0a78c1346160638f8e8321b3f04a683cd90dee0f`:

- 1개 커밋 앞섬
- 총 9개 파일 변경
- 신규 파일 6개
- 수정 파일 3개
- 삭제 파일 0개

주요 변경량:

- `BattleEquipmentStatBonus.cs`: +29
- `BattleEquipmentStatCalculator.cs`: +145
- `BattlePartyRuntime.cs`: +6 / -1
- `BattleStatsFactory.cs`: +27 / -6
- `BattleEquipmentStatsTests.cs`: +127
- `EquipmentDataTests.cs`: +18 / -7

---

## 최신 커밋 검토 상태

최신 GitHub `main`의 Day35 커밋과 관련 소스를 기준으로 정적 검토했다.

확인 사항:

- 장비 보정값 계산기가 Weapon / Armor 슬롯을 모두 조회한다.
- 인벤토리 인스턴스와 EquipmentData 연결 상태를 검증한다.
- 장비 슬롯 불일치 상태를 전투 생성 실패로 처리한다.
- 기존 BattleStatsFactory API를 유지하면서 장비 보정 오버로드를 추가했다.
- 레벨 성장 결과와 장비 수치를 합산한다.
- 최종 수치 범위 보정은 기존 BattleStats 규칙을 재사용한다.
- BattlePartyRuntime에서 실제 전투 캐릭터 생성 전에 장비 보정값을 계산한다.
- 장비 Runtime Stats EditMode 테스트가 추가되어 있다.
- 기존 장비 검증 테스트의 의도적 Error 로그는 테스트 범위에서만 억제되며 실제 DataManager 로그 기능은 유지된다.

현재 GitHub Commit Status에는 실행된 상태 체크가 없고, 해당 커밋에 연결된 GitHub Actions workflow run도 없다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일 또는 EditMode TestRunner 전체 통과를 확정하지 않는다.

최신 소스 정적 검토에서는 개발 일지 작성을 중단해야 할 명확한 차단 문제를 확인하지 못했다.

---

## Day35 완료 범위

현재 장비 전투 적용 흐름:

```text
Character 씬에서 장비 장착
→ CharacterSaveData에 InstanceId 저장
→ 전투 파티 생성
→ 장착 Weapon / Armor 조회
→ EquipmentData 옵션 합산
→ 레벨 성장 Runtime Stats 계산
→ 장비 보정 합산
→ BattleStats 생성
→ 실제 전투 사용
```

다음 Day36에서는 현재 임시 Character 씬의 장비 UI를 정식 장비 관리 UI로 발전시키고, 장착 전후 능력치 비교 및 장비 목록/상세 표시를 다듬는 단계로 이어진다.
