# Project H — Phase 1 Day 36 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 36
- 개발 일지 추가 전 기준 커밋: `c9d401f153994d69ff0338c3d2ffca223f0f85f8`
- 기준 커밋 메시지: `36`
- 이전 커밋: `d6928301557c98ef23f2fb318c89cd5739a80677`
- 주제: 캐릭터 장비 관리 UI 정식화 및 장착 전후 Runtime Stats 미리보기

---

## 목표

Day35에서 장착 중인 Weapon / Armor의 능력치를 실제 전투 Runtime Stats에 반영했다.

Day36에서는 현재 Character 씬의 임시 장비 화면을 장비 관리 화면으로 확장하고, 실제 저장 데이터를 변경하지 않은 상태에서 선택 장비를 장착·교체·해제했을 때의 최종 능력치 변화를 미리 계산하여 표시한다.

핵심 흐름:

```text
현재 장착 상태
→ 현재 BattleStats 계산
→ 보유 장비 선택
→ 대상 슬롯만 가상 교체
→ 예상 BattleStats 계산
→ 현재 / 변경 후 능력치 비교
→ 실제 장착·교체·해제 실행
→ SaveCurrent()
```

---

## BattleEquipmentStatCalculator 가상 슬롯 계산

기존 `BattleEquipmentStatCalculator.TryCalculate()`는 현재 저장된 장착 상태를 기준으로 Weapon / Armor 보정값을 계산한다.

Day36에서는 신규 API를 추가했다.

```text
TryCalculateWithSlotOverride()
```

이 API는 실제 `CharacterEquipmentSaveData`를 수정하지 않고 계산 시점에 특정 슬롯의 인스턴스 ID만 임시로 덮어쓴 것으로 간주한다.

예:

```text
현재
Weapon = EQI_TRAINING
Armor  = EQI_ARMOR

미리보기
Weapon = EQI_IRON
Armor  = EQI_ARMOR
```

실제 저장 데이터는 그대로 유지하면서 철제 검을 장착했을 때의 장비 보정값을 계산할 수 있다.

공통 계산은 `TryCalculateInternal()`로 통합했다.

---

## CharacterEquipmentPreview

신규 `CharacterEquipmentPreview`를 추가했다.

미리보기 결과에 다음 정보를 보관한다.

- 현재 최종 BattleStats
- 변경 후 예상 BattleStats
- 변경 대상 EquipmentSlot
- 선택한 장비 InstanceId
- 기존 장착 장비 InstanceId
- 해제 동작 여부
- 교체 동작 여부
- 최종 액션 라벨

액션 라벨은 현재 상태에 따라 다음 중 하나로 결정된다.

```text
빈 슬롯 + 장비 선택
→ 장착

다른 장비가 있는 슬롯 + 장비 선택
→ 교체

현재 장착 중인 장비 선택
→ 해제
```

---

## CharacterEquipmentPreviewService

신규 `CharacterEquipmentPreviewService`를 추가했다.

장비 목록에서 장비를 선택했을 때 실제 저장을 수정하지 않고 현재 능력치와 변경 후 예상 능력치를 생성한다.

계산 순서:

```text
SaveData
→ CharacterSaveData 조회
→ CharacterData 조회
→ 선택 EquipmentInstanceSaveData 조회
→ EquipmentData 조회
→ 다른 캐릭터 장착 여부 확인
→ 현재 BattleEquipmentStatBonus 계산
→ 대상 슬롯 가상 교체 Bonus 계산
→ BattleStatsFactory로 현재/예상 Stats 생성
→ CharacterEquipmentPreview 반환
```

다른 캐릭터가 이미 착용 중인 장비는 기존 Day34 장착 규칙과 동일하게 미리보기 단계에서도 차단한다.

---

## 실제 SaveData 무변경

Day36에서 가장 중요한 조건은 장비를 클릭하는 것만으로 실제 저장 데이터가 바뀌지 않는 것이다.

잘못된 흐름:

```text
장비 선택
→ 실제 장착
→ Stats 계산
→ 원상복구
```

Day36 구현:

```text
장비 선택
→ 실제 SaveData 유지
→ 계산용 슬롯만 Override
→ 예상 Stats 생성
```

실제 장착 상태는 사용자가 장착/교체/해제 버튼을 실행할 때 기존 `CharacterEquipmentService`를 통해 변경한다.

---

## CharacterEquipmentScreenController 확장

기존 Day34 Character 장비 Runtime UI를 Day36 장비 관리 화면으로 확장했다.

주요 변경:

- Day36 장비 관리 화면 명칭으로 정리
- 캐릭터 현재 최종 능력치 영역 추가
- 장비 상세 영역 개선
- 현재 장비와 선택 장비 상태 표시
- 선택 장비 기준 장착 / 교체 / 해제 액션 표시
- 장비 변경 후 예상 능력치 영역 추가
- 장비 선택 시 능력치 변화량 표시
- 다른 캐릭터가 장착 중인 장비 액션 차단
- 장착 변경 후 기존 `SaveManager.SaveCurrent()` 저장 흐름 유지

현재 Character 씬의 Runtime 생성 방식과 Lobby 진입 구조는 그대로 유지한다.

---

## 능력치 비교 대상

Day35에서 실제 Runtime Stats에 연결한 9종 능력치를 Day36 비교 화면에서도 동일한 계산식으로 사용한다.

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

따라서 UI용 별도 공식이 아니라 실제 전투에 사용하는 `BattleStatsFactory` 결과를 비교한다.

목표는 다음과 같은 형태로 장착 전후 차이를 확인하는 것이다.

```text
공격력
200 → 215  (+15)

최대 HP
2320 → 2320  (0)

치명타율
5% → 7%  (+2%p)
```

---

## 장착 / 교체 / 해제 판정

선택한 장비의 상태에 따라 미리보기 액션을 결정한다.

### 장착

대상 슬롯이 비어 있을 때:

```text
빈 Weapon
+
철제 검 선택
→ 장착
```

### 교체

대상 슬롯에 다른 장비가 있을 때:

```text
훈련용 검 장착 중
+
철제 검 선택
→ 교체
```

### 해제

현재 장착 중인 장비 자체를 선택했을 때:

```text
훈련용 검 장착 중
+
훈련용 검 선택
→ 해제
```

---

## 다른 캐릭터 장착 장비 처리

Day34에서 동일한 장비 인스턴스를 여러 캐릭터가 동시에 착용할 수 없도록 한 규칙을 Day36 미리보기에서도 유지한다.

```text
엘렌
→ EQI_SHARED 장착 중

세레나
→ EQI_SHARED 선택

결과
→ 미리보기 생성 거부
→ 다른 캐릭터 장착 중 안내
```

실제 장착 서비스와 미리보기 서비스가 같은 소유권 규칙을 사용한다.

---

## EditMode 테스트 추가

신규 `CharacterEquipmentPreviewTests`를 추가했다.

포함된 검증:

- 무기 교체 시 예상 공격력이 계산되는지 확인
- 교체 미리보기 중 실제 WeaponInstanceId가 변경되지 않는지 확인
- 현재 장착 장비 선택 시 해제 미리보기가 생성되는지 확인
- 빈 슬롯에 장비 선택 시 `장착` 액션이 생성되는지 확인
- 다른 캐릭터가 장착한 장비는 미리보기가 차단되는지 확인
- `TryCalculateWithSlotOverride()`가 실제 장비 저장을 변경하지 않는지 확인
- Weapon 가상 교체 중 기존 Armor 보정이 유지되는지 확인

---

## 생성 파일

- `Assets/ProjectH/Scripts/Battle/CharacterEquipmentPreview.cs`
- `Assets/ProjectH/Scripts/Battle/CharacterEquipmentPreview.cs.meta`
- `Assets/ProjectH/Scripts/Battle/CharacterEquipmentPreviewService.cs`
- `Assets/ProjectH/Scripts/Battle/CharacterEquipmentPreviewService.cs.meta`
- `Assets/ProjectH/Tests/EditMode/CharacterEquipmentPreviewTests.cs`
- `Assets/ProjectH/Tests/EditMode/CharacterEquipmentPreviewTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleEquipmentStatCalculator.cs`
- `Assets/ProjectH/Scripts/UI/CharacterEquipmentScreenController.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab / SaveData 변경

없음.

Character 씬은 기존 `CharacterSceneRuntimePatch`가 `CharacterEquipmentScreenController`를 Runtime에서 생성하는 구조를 그대로 사용한다.

SaveData 스키마도 변경하지 않는다.

Day36의 미리보기 기능은 저장 구조를 확장하는 것이 아니라 기존 장착 정보를 읽고 계산 단계에서만 가상 슬롯 값을 사용하는 방식이다.

---

## 최신 커밋 변경 규모

Day35 커밋 `d6928301557c98ef23f2fb318c89cd5739a80677` 대비 Day36 커밋 `c9d401f153994d69ff0338c3d2ffca223f0f85f8`:

- 1개 커밋 앞섬
- 총 8개 파일 변경
- 신규 파일 6개
- 수정 파일 2개
- 삭제 파일 0개

파일별 주요 변경량:

```text
BattleEquipmentStatCalculator.cs        +25 / -4
CharacterEquipmentPreview.cs            +26
CharacterEquipmentPreviewService.cs     +87
CharacterEquipmentScreenController.cs   +427 / -281
CharacterEquipmentPreviewTests.cs       +127
```

---

## 최신 커밋 검토 상태

최신 GitHub `main`의 Day36 커밋과 관련 소스를 기준으로 정적 검토했다.

확인한 내용:

- 현재 장착 계산과 가상 슬롯 계산이 공통 계산 경로를 사용한다.
- 가상 슬롯 계산은 실제 `CharacterEquipmentSaveData.SetInstanceId()`를 호출하지 않는다.
- 선택 장비의 원본 `EquipmentData`와 슬롯을 기준으로 예상 능력치를 계산한다.
- 현재/예상 능력치는 모두 기존 `BattleStatsFactory`를 사용한다.
- 현재 장착 장비를 선택하면 해제 미리보기로 판정한다.
- 빈 슬롯이면 장착, 다른 장비가 있으면 교체로 판정한다.
- 다른 캐릭터가 사용 중인 장비는 미리보기 생성 단계에서 차단한다.
- 관련 EditMode 테스트 파일이 최신 커밋에 포함되어 있다.
- Day36 커밋에는 Scene, Prefab, SaveData 스키마 변경이 없다.

현재 GitHub Commit Status에는 실행된 상태 체크가 없고, 해당 커밋에 연결된 GitHub Actions workflow run도 없다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일, Character 씬 실제 UI 동작, EditMode TestRunner 전체 통과를 확정하지 않는다.

최신 소스 정적 검토에서는 개발 일지 작성을 중단해야 할 명확한 차단 문제를 확인하지 못했다.

---

## Day36 구현 범위

현재 장비 관리 흐름:

```text
Lobby
→ Character 씬
→ 캐릭터 선택
→ 보유 장비 선택
→ 현재 최종 Stats 확인
→ 장착 후 예상 Stats 미리보기
→ 장착 / 교체 / 해제
→ UI 갱신
→ SaveCurrent()
→ 전투 진입 시 동일 BattleStatsFactory 계산 사용
```

다음 Day37에서는 기획 일정에 따라 장비 관리 UI를 더 확장하기보다 ItemData를 기반으로 한 아이템 데이터 구조와 일반 아이템 시스템 방향으로 넘어간다.
