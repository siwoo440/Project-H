# Project H — Phase 1 Day 19 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 19
- 기준 원격 커밋: `a05d172a32db1dcde9b763952c107d2a0f8445b5`
- 기준 원격 커밋 메시지: `19`
- 주제: 릴리아·이브 공격형 스킬 및 상태 효과 시스템 구축

---

## 목표

18일차까지 구축한 데이터 기반 Skill Effect 구조를 공격형 캐릭터까지 확장하고, 릴리아·이브의 Skill 1·2·3 총 6개 스킬을 실제 전투 효과와 연결한다.

19일차 핵심 범위:

- 공격력 계수 기반 직접 피해
- Physical / Magic / True 피해 타입 지원
- Multi Hit
- Periodic Damage / DoT
- 마법 저항 감소
- 명중률 감소
- 기절
- 직선 관통 대상
- 주 대상 주변 폭발 범위
- 릴리아 3개 실제 스킬
- 이브 3개 실제 스킬
- 공격형 스킬 관련 Runtime 상태
- 기본 공격 명중률과 Accuracy Debuff 연결
- 기절 중 이동·공격·AI·스킬 사용 차단
- 스킬 테스트용 몬스터 장기 전투 수치 조정
- 스킬 사용 시 `스킬!` 대신 실제 스킬명 표시

스킬 수치와 범위·확률·Hit 수·Tick 간격은 이후에도 `SkillData` Inspector에서 수정 가능한 데이터 기반 구조를 유지한다.

---

## SkillData 공격형 Effect 확장

19일차에서 `SkillEffectDefinition`을 확장했다.

추가 데이터:

- `DamageType`
- `Chance`
- `Interval`
- `Radius`
- `ExcludePrimary`

기존 데이터도 유지한다.

- `Kind`
- `TargetType`
- `Value`
- `Duration`
- `Count`
- `RequirePreviousSuccess`

추가된 `SkillEffectKind`:

- `DamageAttackRatio`
- `PeriodicDamageAttackRatio`
- `ResistanceReductionPercent`
- `AccuracyReductionPercent`
- `Stun`

추가된 `SkillTargetType`:

- `LineEnemies`
- `NearbyEnemiesFromPrimary`

추가된 `SkillDamageType`:

- `Physical`
- `Magic`
- `True`

18일차 Effect enum 값은 유지하여 기존 세레나·엘렌 SkillData 직렬화 호환성을 보존했다.

---

## 릴리아 Skill 1 — 아스트랄 스피어

SkillId:

`SK_LILIA_01`

역할:

단일 마법 피해 + 주기 마법 피해

| 강화도 | 즉시 마법 피해 | DoT |
| --- | ---: | ---: |
| 1 | Attack × 240% | Attack × 30% × 3 Tick |
| 2 | Attack × 300% | Attack × 40% × 3 Tick |
| 3 | Attack × 360% | Attack × 50% × 3 Tick |

DoT 기준:

- Tick 간격: 1초
- Tick 수: 3회
- 피해 타입: Magic
- 같은 스킬의 같은 DoT를 다시 사용하면 기존 Runtime DoT를 갱신

DoT는 제거 가능한 Debuff로 등록되어 이후 정화 시스템과 연결할 수 있다.

---

## 릴리아 Skill 2 — 별빛 폭우

SkillId:

`SK_LILIA_02`

역할:

생존 적 전체 마법 피해 + 마법 저항 감소

| 강화도 | 전체 마법 피해 | 마법 저항 감소 | 지속 |
| --- | ---: | ---: | ---: |
| 1 | Attack × 180% | -10% | 6초 |
| 2 | Attack × 225% | -12% | 6초 |
| 3 | Attack × 270% | -15% | 6초 |

마법 저항 감소는 `BattleSkillRuntimeState.GetEffectiveResistance()`를 통해 실제 Magic Damage 계산에 반영된다.

---

## 릴리아 Skill 3 — 카오스 오브

SkillId:

`SK_LILIA_03`

역할:

주 대상 강력한 마법 피해 + 마법 저항 감소 + 주변 폭발

| 강화도 | 주 대상 피해 | 마법 저항 감소 | 주변 폭발 피해 |
| --- | ---: | ---: | ---: |
| 1 | Attack × 320% | -15% | Attack × 90% |
| 2 | Attack × 400% | -18% | Attack × 115% |
| 3 | Attack × 480% | -20% | Attack × 140% |

마법 저항 감소 지속:

`6초`

주변 폭발 판정:

- 최초 `NearestEnemy`를 Primary Target으로 저장
- Primary 선택 시점의 X 위치를 Context로 유지
- 반경 `2.4`
- Primary Target 자체는 폭발 추가 피해에서 제외
- 범위 안의 다른 생존 적만 폭발 피해 적용

---

## 이브 Skill 1 — 연속 사격

SkillId:

`SK_EVE_01`

역할:

단일 물리 3연타

| 강화도 | 1Hit 피해 | Hit 수 |
| --- | ---: | ---: |
| 1 | Attack × 110% | 3 |
| 2 | Attack × 135% | 3 |
| 3 | Attack × 160% | 3 |

`Count` 값을 Multi Hit 횟수로 사용하므로 이후 다른 연타형 스킬에도 같은 Executor를 재사용할 수 있다.

대상이 Hit 도중 사망하거나 Registry에서 제거되면 남은 Hit를 중단한다.

---

## 이브 Skill 2 — 정령의 화살

SkillId:

`SK_EVE_02`

역할:

전방 직선 관통 물리 피해 + 확률 기절

| 강화도 | 직선 피해 | 기절 확률 | 기절 지속 |
| --- | ---: | ---: | ---: |
| 1 | Attack × 180% | 40% | 1초 |
| 2 | Attack × 225% | 50% | 1초 |
| 3 | Attack × 270% | 60% | 1초 |

`LineEnemies`는 현재 횡스크롤 전투 구조에 맞춰 전방에 있는 생존 적을 X 방향 거리순으로 선택한다.

전방 적이 없는 비정상 배치 상황에서는 생존 상대 전체를 안전 대체 대상으로 사용한다.

---

## 이브 Skill 3 — 엘프의 일제 사격

SkillId:

`SK_EVE_03`

역할:

전방 직선 3연타 + 명중률 감소 + 확률 기절

| 강화도 | 1Hit 피해 | Hit 수 | 명중 감소 | 기절 확률 |
| --- | ---: | ---: | ---: | ---: |
| 1 | Attack × 150% | 3 | -10% | 20% |
| 2 | Attack × 185% | 3 | -12% | 25% |
| 3 | Attack × 220% | 3 | -15% | 30% |

명중률 감소 지속:

`6초`

기절 지속:

`1초`

기절 확률은 3연타 각각이 아니라 별도 `Stun` Effect에서 한 번 판정한다.

---

## BattleSkillRuntimeState 확장

19일차에서 추가된 Runtime Modifier:

- `ResistanceReductionPercent`
- `AccuracyReductionPercent`
- `Stun`

추가된 Runtime 기능:

- 유효 마법 저항 계산
- 유효 명중률 계산
- 기절 상태 조회
- Periodic Damage 등록
- Periodic Damage Tick 처리
- 같은 출처 DoT 갱신
- Debuff 출처 기반 제거

마법 저항 감소:

`Base Resistance × (1 - Reduction)`

명중률 감소:

`Base Accuracy × (1 - Reduction)`

마법 저항 감소 상한과 명중 감소 상한을 적용하여 비정상 수치를 방지한다.

---

## BattleSkillRuntimeDriver

주기 피해 Runtime을 매 프레임 갱신하는 Driver를 추가했다.

역할:

`Time.time → BattleSkillRuntimeState.TickPeriodicEffects()`

BattleSkillExecutor 설정 시 현재 BattleCombatRegistry GameObject에 Driver가 없으면 자동으로 추가한다.

Battle 종료 시:

- Skill Runtime 상태 초기화
- Registry 참조 해제

를 수행한다.

---

## Damage / Accuracy 연동

### Magic Damage

`BattleDamageResolver`의 Magic Damage 계산이 스킬 Runtime의 실제 유효 Resistance를 사용하도록 연결했다.

흐름:

`Base Resistance → Resistance Reduction → Effective Resistance → Magic Damage`

### Accuracy

기본 공격에서 실제 명중 판정을 추가했다.

흐름:

`Base Accuracy → Accuracy Reduction → Effective Accuracy → Random Hit Check`

Miss 발생 시 Debug Log:

`[Project H][MISS]`

를 출력한다.

---

## Stun 행동 차단

기절 중인 전투 객체는 정상 행동을 진행하지 않는다.

아군/공통 기본 공격:

- 이동 중지
- 기본 공격 중지
- 공격 상태 Timer 진행 중지

적군:

- Enemy AI Target 판단 중지
- 기본 공격 중지

Skill Block:

- 기절 중 캐릭터의 스킬 실행 차단

기절 지속시간이 만료되면 기존 전투 흐름으로 복귀한다.

---

## Skill Effect Target Selector 확장

### LineEnemies

전방에 있는 생존 상대 전체를 선택하고 전진 방향 거리순으로 정렬한다.

주 용도:

- 이브 정령의 화살
- 이브 엘프의 일제 사격

### NearbyEnemiesFromPrimary

최초 선택한 Primary Target의 위치를 기준으로 주변 상대를 찾는다.

설정값:

- `Radius`
- `ExcludePrimary`

주 용도:

- 릴리아 카오스 오브 폭발

---

## 스킬 사용 실제 이름 표시

기존 스킬 사용 시 캐릭터 머리 위 행동 텍스트는 항상:

`스킬!`

로 표시됐다.

19일차 후속 수정에서 `BattleActionDebugText`에 실제 이름을 받을 수 있는 Overload를 추가했다.

현재 흐름:

`BattleSkillRequest → request.Skill.DisplayName → BattleActionDebugText`

예:

- 아스트랄 스피어
- 별빛 폭우
- 카오스 오브
- 연속 사격
- 정령의 화살
- 엘프의 일제 사격

스킬명이 비어 있는 예외 상황에서는 기존 `스킬!` 라벨로 fallback한다.

기본 공격과 궁극기는 기존 표시를 유지한다.

- 기본 공격: `공격!`
- 궁극기: `궁극기!`

---

## 테스트용 몬스터 전투 수치 조정

공격형 스킬과 DoT·Debuff를 충분히 확인할 수 있도록 현재 일반 몬스터 3종을 장시간 테스트용으로 임시 조정했다.

| 몬스터 | 기존 HP | Day19 테스트 HP | 기존 공격 | Day19 테스트 공격 |
| --- | ---: | ---: | ---: | ---: |
| 침식 병사 | 650 | 3250 | 55 | 11 |
| 침식된 늑대 | 420 | 2100 | 48 | 10 |
| 오염 식물 | 520 | 2600 | 42 | 8 |

정리:

- 최대 HP: 기존의 5배
- 공격력: 기존의 약 20%
- Defense 유지
- Resistance 유지
- AttackSpeed 유지
- AttackRange 유지
- MoveSpeed 유지

현재 값은 게임 최종 밸런스가 아니라 스킬 검증을 위한 임시 개발 수치다.

---

## Phase1Day19Setup

에디터 메뉴:

`Tools → Project H → Phase 1 → 19일차 릴리아-이브 스킬 및 테스트 몬스터 설정 실행`

처리 내용:

- 릴리아 Skill 1·2·3 실제 데이터 설정
- 이브 Skill 1·2·3 실제 데이터 설정
- 강화도 1·2·3 Effect 배열 설정
- Damage Type 설정
- Multi Hit 설정
- DoT Tick 설정
- Resistance Debuff 설정
- Accuracy Debuff 설정
- Stun Chance 설정
- Nearby Radius 설정
- 테스트용 몬스터 HP / Attack 설정
- 에셋 저장 및 Refresh

`SkillData`를 개별 조정한 뒤 이 Setup을 다시 실행하면 Day19 기준값으로 재설정되므로, 이후 밸런스 수정은 각 SkillData Inspector에서 관리한다.

---

## EditMode Test

19일차에서 추가된 주요 테스트 범위:

### BattleSkillRuntimeStateDay19Tests

- Resistance Reduction
- Effective Magic Resistance
- Accuracy Reduction
- Effective Accuracy
- Stun Duration / Expiration

### BattleSkillTargetSelectorDay19Tests

- `LineEnemies` 전방 거리순
- `NearbyEnemiesFromPrimary`
- Primary 제외
- 먼 대상 제외

### BattleSkillPeriodicDamageTests

- 첫 Tick 이전 체력 유지
- 지정 Tick 수만큼 피해 적용
- 3 Tick 누적 피해 확인

### BattleDay19MonsterTestTuningTests

- 침식 병사 테스트 HP / Attack
- 침식 늑대 테스트 HP / Attack
- 오염 식물 테스트 HP / Attack

### BattleDay19SkillDataTests

- 아스트랄 스피어 Magic Damage + DoT
- 엘프의 일제 사격 3Hit + Accuracy Down + Stun

### BattleActionDebugTextTests

- 기존 기본 공격 / 스킬 / 궁극기 fallback 라벨
- 실제 SkillData 이름 우선 표시
- 빈 스킬명 fallback
- 기본 공격에 Custom Label이 전달돼도 기존 라벨 유지

---

## 최신 원격 저장소 검수

확인한 최신 `main` 커밋:

`a05d172a32db1dcde9b763952c107d2a0f8445b5`

현재 커밋 메시지:

`19`

원격에서 확인한 주요 상태:

- 릴리아 Skill 1·2·3 실제 SkillData 반영
- 이브 Skill 1·2·3 실제 SkillData 반영
- 일반 몬스터 3종 테스트 수치 반영
- 공격형 Skill Effect 확장 반영
- 실제 스킬명 행동 텍스트 표시 반영
- 스킬명 fallback 처리 반영
- `Devlogs/Day19/README.md`는 아직 원격에 없음

GitHub Commit Status에는 등록된 CI Status가 없다.

따라서 현재 원격 코드와 에셋 반영 여부는 확인했지만 GitHub만으로 다음 항목을 증명할 수는 없다.

- Unity 전체 Compile 성공
- EditMode Test Runner 전체 0 Fail
- Play Mode 전체 회귀 테스트
- DoT 실제 Tick 체감
- Stun 실제 전투 행동 정지
- Accuracy Down 실제 Miss 빈도
- Line / Nearby 범위의 실제 화면 체감
- 스킬명 머리 위 표시의 실제 렌더링

---

## 19일차 완료 범위

- 공격력 계수 직접 피해
- Physical / Magic / True Skill Damage Type
- Multi Hit
- Periodic Damage / DoT
- Resistance Reduction
- Accuracy Reduction
- Stun
- LineEnemies
- NearbyEnemiesFromPrimary
- 릴리아 3개 실제 스킬
- 이브 3개 실제 스킬
- Magic Resistance 실제 피해 계산 연결
- Accuracy 실제 기본 공격 Hit/Miss 연결
- 기절 중 기본 공격·이동·Enemy AI·스킬 차단
- Periodic Damage Runtime Driver
- 제거 가능한 공격형 Debuff 연결
- 테스트용 몬스터 HP 5배
- 테스트용 몬스터 공격력 약 20%
- 실제 스킬명 행동 텍스트 표시
- Day19 Editor Setup
- Day19 EditMode Test

Phase 1 Day 19 — 릴리아·이브 공격형 스킬 및 상태 효과 시스템 구축.
