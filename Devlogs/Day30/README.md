# Project H — Phase 1 Day 30 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 30
- 기준 원격 커밋: `cd67e318ef56b2e2c8893a543824c7fd4f45dbe0`
- 기준 원격 커밋 메시지: `31`
- 이전 커밋: `c234c7a33d250ebb1d14cb0abfb0023671f79170`
- 주제: 레벨 기반 기본 스탯 성장 공식 확정 및 Runtime Stats 재계산

---

## 목표

Day29에서 전투 보상 EXP가 실제 캐릭터 레벨과 저장 데이터에 반영되도록 연결한 뒤, Day30에서는 저장된 캐릭터 레벨이 다음 전투의 실제 전투 스탯으로 이어지도록 성장 계산 흐름을 정리했다.

이번 일차의 핵심 방향:

- Lv.1~Lv.20 범위의 기본 스탯 성장 공식을 확정한다.
- 레벨당 +5% 선형 성장 규칙을 적용한다.
- 저장된 `CharacterSaveData.Level`을 전투 진입 시 다시 읽어 Runtime Stats를 생성한다.
- HP, 공격력, 방어력, 저항력을 레벨 성장 대상으로 사용한다.
- 공격속도, 명중률, 치명타율, 공격 사거리, 이동속도는 현재 성장 대상에서 제외한다.
- 최대 레벨을 초과한 비정상 저장값은 Lv.20 성장치로 제한한다.
- 성장 공식과 저장 레벨 기반 Runtime Stats 재생성을 EditMode 테스트로 검증할 수 있게 구성한다.

---

## 레벨 기반 기본 스탯 성장 공식

`BattleGrowthFormula`를 Day30 기본 성장 공식으로 정리했다.

현재 규칙:

| 항목 | 값 |
| --- | ---: |
| 최소 성장 레벨 | Lv.1 |
| 최대 성장 레벨 | Lv.20 |
| Lv.1 배율 | 1.00 |
| 레벨당 성장 | +5% |
| Lv.20 배율 | 1.95 |

성장 배율 공식:

`1 + ((현재 레벨 - 1) × 0.05)`

예시:

| 레벨 | 성장 배율 |
| ---: | ---: |
| Lv.1 | 1.00 |
| Lv.5 | 1.20 |
| Lv.10 | 1.45 |
| Lv.20 | 1.95 |

`NormalizeLevel()`을 추가해 성장 계산에 사용하는 레벨을 Lv.1~Lv.20 범위로 제한한다.

따라서 저장 데이터에 Lv.99 같은 비정상 값이 들어 있어도 Runtime Stats에는 Lv.20 성장치만 적용된다.

---

## 성장 대상 스탯

`BattleStatsFactory`에서 다음 기본 전투 스탯에 레벨 성장 공식을 적용한다.

- Max HP
- Attack
- Defense
- Resistance

계산 흐름:

`CharacterData.BaseStat`
→ `BattleGrowthFormula.ScaleStat()`
→ 레벨 성장 적용
→ `BattleStats` 생성

현재 성장 비대상 스탯:

- AttackSpeed
- Accuracy
- CriticalRate
- AttackRange
- MoveSpeed

이 수치들은 현재 `CharacterData`의 원본 값을 그대로 Runtime Stats에 전달한다.

---

## SaveData Level → Runtime Stats 재계산

`BattleStatsFactory.CreateCharacter(CharacterData, CharacterSaveData, runtimeId)`는 저장 데이터의 현재 `Level`을 읽어 레벨 기반 생성 함수로 전달한다.

흐름:

`CharacterSaveData.Level`
→ `BattleGrowthFormula.NormalizeLevel()`
→ HP / Attack / Defense / Resistance 성장 계산
→ 새 `BattleStats` 생성

전투 파티 생성은 `BattlePartyRuntime.TryCreate()`에서 저장된 파티 캐릭터를 다시 조회하고 각 캐릭터의 `CharacterSaveData`를 사용해 새 Runtime Stats를 만든다.

따라서 Day29에서 전투 승리 후 캐릭터 레벨이 저장되면, 이후 새 전투 파티를 생성할 때 상승한 레벨 기준으로 전투 스탯이 다시 계산되는 구조다.

---

## 최대 레벨 상한

Day29의 `CharacterLevelProgression.MaxLevel`과 Day30의 성장 공식 최대 레벨을 동일하게 연결했다.

`BattleGrowthFormula.MaxLevel = CharacterLevelProgression.MaxLevel`

현재 최대 레벨은 Lv.20이다.

이 방식으로 레벨링 시스템과 전투 성장 시스템이 서로 다른 최대 레벨 값을 별도로 관리하지 않도록 했다.

---

## Serena 기준 성장 예시

현재 Serena 기본 데이터:

| 스탯 | Lv.1 | Lv.5 | Lv.20 |
| --- | ---: | ---: | ---: |
| HP | 2200 | 2640 | 4290 |
| Attack | 180 | 216 | 351 |
| Defense | 120 | 144 | 234 |

Lv.5는 1.20배, Lv.20은 1.95배 성장치를 사용한다.

Runtime Stats는 정수 스탯이므로 `Mathf.RoundToInt()`를 이용해 최종 값을 계산한다.

---

## 테스트 추가 및 보강

### BattleGrowthFormulaTests

신규 테스트 파일을 추가했다.

검증 내용:

- 최소·최대 레벨 범위 보정
- Lv.1 / Lv.5 / Lv.20 성장 배율
- 최대 레벨 초과 입력 상한
- 정수 스탯 성장 계산
- 음수 기본 스탯의 0 보정

### BattleStatsFactoryTests

기존 테스트를 보강했다.

검증 내용:

- Lv.1에서 CharacterData 기본값 유지
- 기본 Resistance 유지
- Lv.5 성장치 적용
- Lv.20 초과 저장 레벨이 Lv.20으로 제한됨
- 공격속도·명중률·치명타율·공격 사거리·이동속도는 성장하지 않음
- Runtime Stats 변경이 CharacterData 원본 에셋을 수정하지 않음

### BattlePartyRuntimeTests

저장 레벨 변경 후 Runtime Stats 재생성 테스트를 추가했다.

검증 흐름:

`Lv.1 SaveData`
→ 첫 Runtime Party 생성
→ Serena SaveData를 Lv.5로 변경
→ Runtime Party 재생성
→ Lv.5 HP / Attack / Defense 확인

이를 통해 기존 Runtime Stats 객체를 수정하는 방식이 아니라, 전투 파티 생성 시 현재 저장 레벨을 기준으로 새 스탯을 계산하는 흐름을 확인하도록 구성했다.

---

## 생성 파일

- `Assets/ProjectH/Tests/EditMode/BattleGrowthFormulaTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleGrowthFormulaTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleGrowthFormula.cs`
- `Assets/ProjectH/Scripts/Battle/BattleStatsFactory.cs`
- `Assets/ProjectH/Tests/EditMode/BattlePartyRuntimeTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleStatsFactoryTests.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab 변경

없음.

Day30은 기존 데이터와 전투 생성 흐름 안에서 성장 계산을 확정하는 작업이므로 Scene, Prefab, CharacterData 에셋 자체를 수정하지 않는다.

---

## 최신 커밋 변경 규모

Day29 커밋 `c234c7a33d250ebb1d14cb0abfb0023671f79170`과 비교한 Day30 변경 범위:

- 총 6개 파일
- 신규 파일 2개
- 수정 파일 4개
- 삭제 파일 0개
- 추가 107줄
- 삭제 5줄

현재 원격 최신 커밋 메시지는 `31`이지만 변경 내용은 Day30의 레벨 기반 기본 스탯 성장 작업이다.

---

## 최신 커밋 검토 상태

기준 원격 커밋 `cd67e318ef56b2e2c8893a543824c7fd4f45dbe0`은 Day29 커밋보다 1개 커밋 앞서 있다.

정적 코드 검토에서 확인한 사항:

- `BattleGrowthFormula`의 최대 성장 레벨은 `CharacterLevelProgression.MaxLevel`과 연결되어 있다.
- 레벨 입력은 `NormalizeLevel()`에서 Lv.1~Lv.20으로 제한된다.
- HP, Attack, Defense, Resistance는 동일한 성장 공식으로 계산된다.
- 저장 기반 `BattleStatsFactory`는 현재 `CharacterSaveData.Level`을 사용한다.
- 최대 레벨 초과 성장과 성장 비대상 Utility Stats에 대한 테스트가 추가되어 있다.
- 파티 재생성 시 저장 레벨 변경이 새 Runtime Stats에 반영되는 테스트가 추가되어 있다.

GitHub Commit Status와 GitHub Actions workflow run은 현재 연결된 기록이 없다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일 및 실제 EditMode Test Runner 전체 통과를 단정하지 않는다.

최신 커밋 diff와 변경 파일을 기준으로 정적 검토했을 때 Day30 개발 일지 작성을 막아야 할 명확한 차단 문제는 확인되지 않았다.

---

## Day30 완료 범위

현재 코드상 성장 흐름:

`Battle Victory`
→ Day29 EXP 지급
→ CharacterSaveData.Level 상승 및 저장
→ 다음 Battle Party 생성
→ 현재 CharacterSaveData.Level 조회
→ 레벨 범위 정규화
→ 기본 HP / Attack / Defense / Resistance 성장 계산
→ 새 BattleStats 생성
→ 실제 전투 Runtime Stats 사용

현재 Day30 범위에서는 레벨업 결과 화면의 별도 연출 및 성장 전후 비교 UI는 포함하지 않는다.
