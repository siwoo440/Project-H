# Project H — Phase 1 Day 8 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 8
- 기준 커밋: `76270355be7a90a8003240dcec7ae386a3c2b371`
- 커밋 당시 메시지: `8`
- 주제: 전투 Runtime Stats 기반 구축

---

## 목표

7일차에 구축한 12인 CharacterData와 기존 CharacterSaveData를 전투에서 직접 사용할 수 있는 Runtime 데이터로 변환하는 기반을 만든다.

원본 ScriptableObject와 저장 데이터를 전투 중 직접 변경하지 않고, 전투가 시작될 때 별도의 BattleStats를 생성해 HP 변화, 사망 상태, 레벨 보정 등을 처리할 수 있도록 구조를 분리한다.

기본 흐름:

`CharacterData + CharacterSaveData → BattleStatsFactory → BattleStats`

파티 단위 흐름:

`SaveData.PartyCharacterIds → DataManager → BattlePartyRuntime`

---

## BattleGrowthFormula

프로토타입 레벨 성장 공식을 추가했다.

현재 성장률:

`1 + (Level - 1) × 0.05`

예:

- Lv.1 = 100%
- Lv.5 = 120%
- Lv.10 = 145%

현재 성장 공식을 적용하는 스탯:

- Max HP
- Attack
- Defense

AttackSpeed, Accuracy, CriticalRate는 현재 CharacterData 값을 그대로 사용한다.

이 공식은 최종 밸런스가 아니라 전투 Runtime 구조 검증을 위한 프로토타입 공식이다.

---

## BattleStats

한 전투에서 사용하는 캐릭터 Runtime 상태 객체를 추가했다.

주요 데이터:

- RuntimeId
- CharacterId
- DisplayName
- Position
- Level
- MaxHp
- CurrentHp
- Attack
- Defense
- AttackSpeed
- Accuracy
- CriticalRate
- IsAlive
- HealthRatio

CharacterData의 BaseHp 같은 원본 데이터를 변경하지 않고, BattleStats의 CurrentHp만 전투 중 변경한다.

---

## HP Runtime 처리

BattleStats에 다음 기본 상태 변경 기능을 추가했다.

### TakeDamage

이미 계산이 완료된 피해량을 현재 HP에 적용한다.

- 음수 피해는 0으로 처리
- CurrentHp가 0 미만으로 내려가지 않음
- 실제 적용된 피해량 반환

### Heal

이미 계산이 완료된 회복량을 현재 HP에 적용한다.

- 음수 회복은 0으로 처리
- CurrentHp가 MaxHp를 초과하지 않음
- 실제 적용된 회복량 반환

### SetCurrentHp

CurrentHp를 직접 설정하되:

`0 ≤ CurrentHp ≤ MaxHp`

범위를 유지한다.

### RestoreFullHp

CurrentHp를 MaxHp까지 복구한다.

### IsAlive

`CurrentHp > 0`

이면 생존 상태로 판단한다.

실제 피해 공식, 방어 공식, 회복 공식은 아직 포함하지 않는다. 8일차는 계산된 숫자를 Runtime 상태에 적용하는 계층까지만 담당한다.

---

## BattleStatsFactory

CharacterData와 CharacterSaveData를 BattleStats로 변환하는 Factory를 추가했다.

저장 데이터 기반 생성:

`CreateCharacter(CharacterData, CharacterSaveData, RuntimeId)`

레벨 직접 지정 생성:

`CreateCharacter(CharacterData, Level, RuntimeId)`

처리 내용:

1. CharacterData 존재 확인
2. CharacterSaveData 존재 확인
3. Character ID 일치 확인
4. 최소 레벨 보정
5. 레벨 성장 배율 계산
6. HP / Attack / Defense 성장 적용
7. BattleStats 생성

CharacterData와 CharacterSaveData의 Character ID가 다르면 예외를 발생시켜 잘못된 Runtime 캐릭터 생성을 방지한다.

---

## Static / Save / Runtime 분리

캐릭터 상태를 다음 세 계층으로 분리했다.

### CharacterData

변하지 않는 캐릭터 원본 데이터.

예:

- Base HP
- Base Attack
- Base Defense
- Position
- Attack Speed
- Accuracy

### CharacterSaveData

플레이어의 영구 진행 데이터.

현재 주요 데이터:

- Character ID
- Level
- Experience

### BattleStats

한 번의 전투에서만 사용하는 Runtime 상태.

예:

- Max HP
- Current HP
- 계산된 Attack
- 계산된 Defense
- IsAlive

이를 통해 전투 중 HP가 감소해도 CharacterData 에셋이나 SaveData가 직접 변경되지 않는다.

---

## BattlePartyRuntime

저장된 파티 구성을 실제 전투용 Runtime Party로 만드는 컨테이너를 추가했다.

최대 파티 인원:

`4`

생성 흐름:

1. SaveData의 PartyCharacterIds 확인
2. DataManager에서 CharacterData 조회
3. SaveData에서 CharacterSaveData 조회
4. BattleStatsFactory로 BattleStats 생성
5. Runtime Party에 추가

Runtime ID:

- `ALLY_0`
- `ALLY_1`
- `ALLY_2`
- `ALLY_3`

현재 초기 파티 기준:

- CH_SERENA
- CH_ELLEN
- CH_LILIA
- CH_EVE

가 Runtime Party로 생성될 수 있는 구조다.

---

## BattlePartyRuntime Validation

파티 생성 시 다음 예외 상태를 방어한다.

- DataManager 없음
- DataManager 미초기화
- SaveData 없음
- 파티 인원 0명
- 파티 인원 4명 초과
- 빈 Character ID
- 중복 Character ID
- CharacterData 조회 실패
- CharacterSaveData 조회 실패

실패 시 예외를 무조건 발생시키기보다 `TryCreate`가 false와 실패 사유 문자열을 반환하도록 구성했다.

---

## Runtime Party 조회 기능

BattlePartyRuntime에 다음 조회 기능을 추가했다.

### FindByRuntimeId

예:

`ALLY_0`

같은 전투 인스턴스 ID로 캐릭터를 조회한다.

### FindByCharacterId

예:

`CH_SERENA`

같은 원본 캐릭터 ID로 전투 캐릭터를 조회한다.

### RestoreAll

전투 파티 전체의 CurrentHp를 MaxHp로 복원한다.

---

## Runtime Stats Preview

전투 Scene 구현 전에 12인 Runtime Stats 결과를 확인할 수 있는 Editor Window를 추가했다.

메뉴:

`Tools > Project H > Phase 1 > 8일차 Runtime Stats Preview`

기능:

- Preview Level 1~100 조정
- 전체 CharacterData 순회
- Runtime Stats 생성
- Position 표시
- Level 표시
- HP 표시
- Attack 표시
- Defense 표시
- Attack Speed 표시
- Accuracy 표시
- Critical Rate 표시

이를 통해 BattleScene과 실제 캐릭터 GameObject가 없어도 성장 공식과 CharacterData 연결 상태를 확인할 수 있다.

---

## EditMode 테스트

8일차에 다음 테스트 파일을 추가했다.

### BattleStatsTests

검증 항목:

- 과잉 피해 시 HP가 0에서 멈추는지 확인
- HP 0에서 IsAlive가 false인지 확인
- 과잉 회복 시 MaxHp를 초과하지 않는지 확인
- 음수 Damage 무시
- 음수 Heal 무시
- RestoreFullHp 동작
- 전체 회복 후 생존 상태 복원

### BattleStatsFactoryTests

검증 항목:

- Lv.1 Runtime Stats가 CharacterData 기본값과 일치
- Lv.5 성장 배율 120%
- 세레나 Lv.5 HP = 2640
- 세레나 Lv.5 Attack = 216
- 세레나 Lv.5 Defense = 144
- BattleStats 변경이 CharacterData 원본을 수정하지 않음

### BattlePartyRuntimeTests

검증 항목:

- 초기 4인 Runtime Party 생성
- Party Count = 4
- 첫 Runtime ID = ALLY_0
- 첫 캐릭터 = CH_SERENA
- 마지막 캐릭터 = CH_EVE
- 존재하지 않는 Character ID 실패
- 5인 이상 파티 생성 실패

---

## 8일차 신규 파일

전투 Runtime:

- `Assets/ProjectH/Scripts/Battle/BattleGrowthFormula.cs`
- `Assets/ProjectH/Scripts/Battle/BattleStats.cs`
- `Assets/ProjectH/Scripts/Battle/BattleStatsFactory.cs`
- `Assets/ProjectH/Scripts/Battle/BattlePartyRuntime.cs`

Editor:

- `Assets/ProjectH/Scripts/Editor/BattleRuntimePreviewWindow.cs`

Tests:

- `Assets/ProjectH/Tests/EditMode/BattleStatsTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleStatsFactoryTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattlePartyRuntimeTests.cs`

---

## 검토 결과

최신 GitHub 커밋 기준으로 8일차 변경사항을 다시 확인했다.

확인 항목:

- Battle Runtime 폴더 및 스크립트 존재
- BattleGrowthFormula의 레벨당 5% 프로토타입 성장 공식
- BattleStats의 CurrentHp / MaxHp 분리
- HP Clamp 처리
- IsAlive 및 HealthRatio 제공
- BattleStatsFactory의 CharacterData + CharacterSaveData 연결
- Character ID 불일치 방어
- BattlePartyRuntime의 4인 제한
- DataManager와 SaveData를 이용한 Runtime Party 생성
- ALLY_0 형식 Runtime ID 생성
- 중복 / 누락 Character 방어
- Runtime Stats Preview Editor Window 존재
- BattleStatsTests 존재
- BattleStatsFactoryTests 존재
- BattlePartyRuntimeTests 존재

저장소 정적 검토 기준으로 8일차 진행을 막는 명확한 구조 문제는 추가로 확인되지 않았다.

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 Unity Editor 실제 컴파일 성공과 EditMode Test Runner 통과 여부는 GitHub 상태만으로 확인할 수 없다.

---

## 현재 범위에서 제외

8일차에서는 다음 기능을 구현하지 않는다.

- Character GameObject Spawn
- BattleScene 실제 배치
- 자동 타겟팅
- 기본 공격
- Damage 계산 공식
- Defense 계산 공식
- Enemy AI
- Skill
- Ultimate
- Battle HUD 연결

이 기능들은 이후 Phase 1 일차에서 Runtime Stats를 기반으로 단계적으로 연결한다.

---

## Day 8 완료 기준

- CharacterData와 CharacterSaveData를 BattleStats로 변환 가능
- Lv.1 원본 스탯 반영 가능
- 레벨 성장 수치 계산 가능
- CurrentHp가 CharacterData와 분리됨
- 피해 / 회복 / HP Clamp 가능
- HP 0 생존 판정 가능
- 초기 4인 Runtime Party 생성 구조 존재
- 잘못된 파티 데이터 Validation 존재
- 12인 Runtime Stats Preview 가능
- Runtime 관련 EditMode 테스트 코드 존재

Phase 1 Day 8 — 전투 Runtime Stats 기반 구축.
