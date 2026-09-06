# Project H — Phase 1 Day 18 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 18
- 기준 원격 커밋: `d0ec60eea0e92d518bcde4367e188da8f8adb643`
- 기준 원격 커밋 메시지: `18`
- 주제: 세레나·엘렌 스킬 및 강화 효과 시스템 구축

---

## 목표

17일차에서 완성한 스킬 블록 시스템을 실제 전투 효과와 연결하고, 세레나·엘렌의 Skill 1·2·3 총 6개 스킬을 강화도 1·2·3에 따라 실제로 동작하도록 구현한다.

핵심 규칙:

- `SkillId`는 스킬 종류를 의미한다.
- 동일 `SkillId` 블록의 인접 개수는 해당 스킬의 강화도 1·2·3을 결정한다.
- 강화도는 다른 스킬로 변환되는 개념이 아니다.
- 스킬 효과와 수치는 `SkillData`에서 수정 가능하게 유지한다.
- 실제 궁극기 게이지 충전은 21일차 범위로 남긴다.

---

## 데이터 기반 Skill Effect 구조

기존 `SkillData`의 강화도 데이터에 실제 효과 목록을 추가했다.

강화도별 데이터:

- `PowerRatio`
- `FlatValue`
- `UltimateGaugeGain`
- `Effects[]`

각 `SkillEffectDefinition`은 다음 정보를 가진다.

- `Kind`
- `TargetType`
- `Value`
- `Duration`
- `Count`
- `RequirePreviousSuccess`

현재 지원 Effect:

- `HealMaxHpPercent`
- `CleanseDebuff`
- `DefensePercent`
- `DamageReductionPercent`
- `HealingReceivedPercent`
- `Taunt`
- `CounterChance`

따라서 스킬 수치와 대부분의 효과 조합은 C# 하드코딩 대신 `SkillData` Inspector에서 변경할 수 있다.

---

## 세레나 Skill 1 — 정화의 빛

SkillId:

`SK_SERENA_01`

역할:

생존 아군 전체 회복 + 제거 가능한 약화 효과 정화

| 강화도 | 전체 회복 | 정화 |
| --- | ---: | ---: |
| 1 | 최대 HP 10% | 1개 |
| 2 | 최대 HP 14% | 1개 |
| 3 | 최대 HP 18% | 최대 2개 |

현재 상태이상 전체 시스템은 아직 후속 일정이므로, 제거 가능한 Debuff 등록/제거 API를 먼저 마련했다.

제거할 Debuff가 없는 경우 정화는 0개 제거로 종료된다.

---

## 세레나 Skill 2 — 치유 기도

SkillId:

`SK_SERENA_02`

역할:

현재 HP 비율이 가장 낮은 생존 아군 1명을 집중 회복하고 방어력을 증가시킨다.

| 강화도 | 회복 | 방어 증가 | 지속 |
| --- | ---: | ---: | ---: |
| 1 | 최대 HP 18% | +10% | 5초 |
| 2 | 최대 HP 24% | +15% | 5초 |
| 3 | 최대 HP 30% | +20% | 5초 |

한 스킬 실행 중 같은 `TargetType`의 후속 효과는 최초 선택한 대상을 유지한다.

예를 들어 치유 기도로 최저 HP 대상이 회복되어 HP 순위가 바뀌더라도 방어 증가 효과는 처음 선택한 같은 대상에게 적용된다.

---

## 세레나 Skill 3 — 성유의 가호

SkillId:

`SK_SERENA_03`

역할:

생존 아군 전체 회복 + 받는 회복량 증가 + 정화

| 강화도 | 전체 회복 | 받는 회복량 증가 | 정화 | 지속 |
| --- | ---: | ---: | ---: | ---: |
| 1 | 최대 HP 12% | +8% | 1개 | 6초 |
| 2 | 최대 HP 16% | +12% | 1개 | 6초 |
| 3 | 최대 HP 20% | +15% | 최대 2개 | 6초 |

받는 회복량 증가 Modifier는 기존 `BattleHealingResolver`에 연결했다.

---

## 엘렌 Skill 1 — 방어 태세

SkillId:

`SK_ELLEN_01`

역할:

자신의 방어력과 최종 피해 감소율 증가

| 강화도 | 방어 증가 | 피해 감소 | 지속 |
| --- | ---: | ---: | ---: |
| 1 | +20% | 10% | 6초 |
| 2 | +30% | 15% | 6초 |
| 3 | +40% | 20% | 6초 |

방어력 증가와 피해 감소를 별도 단계로 처리한다.

피해 계산 흐름:

`기본 Defense → Defense Modifier → 방어 계산 → Damage Reduction → 최종 HP 감소`

피해 감소에는 완전 무적 상태를 피하기 위한 Runtime 상한이 존재한다.

---

## 엘렌 Skill 2 — 도발

SkillId:

`SK_ELLEN_02`

역할:

생존 적 전체의 공격 목표를 엘렌 쪽으로 유도하고, 자신에게 방어 증가와 조건부 회복을 적용한다.

| 강화도 | 도발 | 자기 회복 | 방어 증가 |
| --- | ---: | ---: | ---: |
| 1 | 2.5초 | 최대 HP 3% | +10% |
| 2 | 3.5초 | 최대 HP 5% | +15% |
| 3 | 4.5초 | 최대 HP 7% | +20% |

자기 회복은 앞선 도발 효과가 실제로 적용됐을 때만 실행된다.

Enemy AI는 기존 타겟 정책보다 활성 도발을 먼저 확인한다.

도발 중:

`활성 Taunt → 엘렌 Desired Target → 기존 FrontBlocker 규칙 → 실제 타겟`

도발이 끝나면 기존 Enemy AI 타겟 정책으로 복귀한다.

---

## 엘렌 Skill 3 — 철의 맹세

SkillId:

`SK_ELLEN_03`

역할:

자신의 방어·피해 감소를 크게 높이고 피격 시 반격 확률 부여

| 강화도 | 방어 증가 | 피해 감소 | 반격 확률 | 지속 |
| --- | ---: | ---: | ---: | ---: |
| 1 | +30% | 15% | 50% | 6초 |
| 2 | +40% | 20% | 70% | 6초 |
| 3 | +50% | 25% | 90% | 6초 |

현재 반격 피해는 엘렌의 Attack 100%를 사용하는 물리 피해로 계산한다.

반격 처리 중 다시 반격이 발생하는 재귀 상황을 막는 Runtime 잠금을 포함한다.

---

## BattleSkillRuntimeState

스킬에서 발생하는 시간 제한 상태를 공통 Runtime으로 관리한다.

현재 관리 대상:

- Defense Percent
- Damage Reduction Percent
- Healing Received Percent
- Counter Chance
- Taunt

Modifier는 전투 시간에 따라 만료된다.

같은 스킬의 같은 효과를 다시 사용하면 동일 효과가 무한 중첩되지 않고 최신 수치와 지속시간으로 갱신된다.

다른 스킬에서 부여된 별도 효과는 독립적으로 공존할 수 있다.

---

## BattleSkillEffectTargetSelector

실제 스킬 효과용 공통 대상 선택기를 추가했다.

현재 지원:

- `Self`
- `NearestEnemy`
- `LowestHpAlly`
- `AllEnemies`
- `AllAllies`

`LowestHpAlly`는 생존 아군 중 현재 HP 비율이 가장 낮은 대상을 선택한다.

---

## BattleSkillEffectExecutor

`BattleSkillRequest`에서 전달된 `SkillId`와 `EnhancementLevel`을 기반으로 해당 `SkillData` 강화도의 `Effects[]`를 읽어 실행한다.

흐름:

`BattleSkillRequest → SkillData → Enhancement → Effects[] → Target Selector → Effect 적용`

적용 가능한 효과는 공통 Executor를 통해 처리하므로 이후 캐릭터 스킬도 같은 파이프라인을 재사용할 수 있다.

---

## 기존 전투 시스템 연결

### BattleDamageResolver

- 스킬 기반 Defense Modifier 적용
- 스킬 기반 Damage Reduction 적용

### BattleHealingResolver

- Healing Received Modifier 적용
- 사망 대상 일반 회복 차단 유지

### BattleCombatRegistry

- Runtime ID로 BattleActor를 찾는 조회 기능 추가
- Taunt 및 Counter에서 사용

### BattleEnemyBrain

- 활성 Taunt가 존재하면 기존 AI 정책보다 우선 처리
- 기존 FrontBlocker 규칙은 유지

### BattleActor

- 피격 후 생존 중이고 Counter Modifier가 활성화돼 있으면 반격 판정
- 기존 Damage/Healing 표시 구조 유지

---

## 실제 스킬 데이터

17일차의 임시 스킬 이름을 실제 세레나·엘렌 스킬로 교체했다.

세레나:

- `SK_SERENA_01` — 정화의 빛
- `SK_SERENA_02` — 치유 기도
- `SK_SERENA_03` — 성유의 가호

엘렌:

- `SK_ELLEN_01` — 방어 태세
- `SK_ELLEN_02` — 도발
- `SK_ELLEN_03` — 철의 맹세

각 에셋은 강화도별 실제 `Effects[]`를 보유한다.

---

## Phase1Day18Setup

에디터 메뉴:

`Tools → Project H → Phase 1 → 18일차 세레나-엘렌 스킬 설정 실행`

기능:

- 세레나 Skill 1·2·3 실제 이름/설명 적용
- 엘렌 Skill 1·2·3 실제 이름/설명 적용
- 강화도별 효과 수치 재설정
- Target Type / Effect Type 설정
- 21일차 연결용 `UltimateGaugeGain` 데이터 유지

스킬을 수동으로 조정한 이후 Setup을 다시 실행하면 Day18 기준값으로 재설정될 수 있으므로, 이후 개별 밸런스 수정 시에는 SkillData Inspector 값을 직접 관리한다.

---

## TestRunner 컴파일 오류 보정

Day18 작업 중 `BattleSkillEffectTargetSelectorTests`에서 다음 Compile 오류가 발생했다.

`Argument 1: cannot convert from 'ProjectH.Battle.BattleActor' to 'string'`

원인:

Unity/NUnit 환경에서 컬렉션 멤버 검증에 사용한 `Does.Contain(BattleActor)`가 문자열 제약으로 해석됐다.

수정:

- `Does.Contain(owner)` → `Has.Member(owner)`
- `Does.Contain(ally)` → `Has.Member(ally)`
- `Does.Not.Contain(enemy)` → `Has.No.Member(enemy)`

실제 스킬 Runtime 로직은 변경하지 않고 테스트 Assertion만 현재 NUnit 환경에 맞게 수정했다.

---

## SkillBlockPanel MissingReference 보정

Play Mode 또는 BattleScene 종료 중 다음 오류가 발생했다.

`MissingReferenceException: BattleSkillBlockView has been destroyed but you are still trying to access it`

발생 흐름:

`BattleScreenController.OnDestroy → ClearSpawnedCombatants → Registry.Unregister → HandleActorUnregistered → BattleSkillBlockPanel.Refresh`

원인:

`BattleSkillBlockPanel.views`가 Unity에서 이미 Destroy된 `BattleSkillBlockView` 참조를 보유한 상태에서 `Refresh()`가 `view.gameObject`에 접근했다.

보정:

- `lifecycleBlocked` 상태 추가
- `OnDisable()` 이후 Runtime UI 갱신 차단
- `Refresh()` 시작 시 파괴된 View 참조 정리
- Destroy된 View에 대한 null 방어
- 종료 중 Runtime View 재생성 차단
- Drag / Click / SetInteractable도 종료 상태에서 차단

회귀 테스트:

- Destroy된 Runtime View 참조를 가진 상태에서 `Refresh()` 호출
- 비활성화된 Panel에서 `Refresh()` 호출
- 종료 중 Runtime View 재생성 방지

---

## EditMode Test

Day18에서 추가 또는 수정된 주요 테스트 범위:

- Skill Effect Definition 직렬화 값
- Runtime Modifier 만료
- 받는 회복량 배율
- Damage Reduction 상한
- Taunt 만료
- LowestHpAlly 선택
- AllAllies 대상 범위
- BattleSkillBlockPanel Destroyed View 생명주기
- Panel 비활성 상태 Refresh 안전성

---

## 최신 원격 저장소 검수

확인한 최신 `main` 커밋:

`d0ec60eea0e92d518bcde4367e188da8f8adb643`

현재 커밋 메시지:

`18`

원격에서 확인한 주요 상태:

- 세레나·엘렌 6개 실제 SkillData 반영
- 데이터 기반 강화도별 Effect 구조 반영
- Damage / Healing / Taunt / Counter Runtime 연결
- Target Selector Test의 NUnit Assertion 수정 반영
- BattleSkillBlockPanel의 Destroyed View 생명주기 보정 반영
- 관련 Lifecycle 회귀 테스트 반영
- `Devlogs/Day18/README.md`는 아직 원격에 없음

GitHub Commit Status에는 등록된 CI Status가 없다.

따라서 원격 코드 반영 여부는 확인했지만, GitHub만으로 아래 항목의 실제 통과를 증명할 수는 없다.

- Unity 전체 Compile
- EditMode Test Runner 전체 0 Fail
- Play Mode 전체 회귀 테스트
- 세레나·엘렌 6개 스킬의 실제 전투 체감
- Taunt / Counter의 실제 전투 연출
- Scene 종료 시 MissingReferenceException 재현 여부

---

## 18일차 완료 범위

- 데이터 기반 Skill Effect 구조
- 강화도별 Effects 배열
- 공통 Skill Target Selector
- 공통 Skill Effect Executor
- 시간 제한 Modifier Runtime
- Defense 증가
- Damage Reduction
- Healing Received 증가
- Taunt
- Counter
- Cleanse 확장 API
- 세레나 Skill 1·2·3 실제 효과
- 엘렌 Skill 1·2·3 실제 효과
- 기존 Damage / Healing / Enemy AI 연결
- Inspector 기반 스킬 수치 수정 구조
- Day18 Editor Setup
- Target Selector NUnit Compile 보정
- SkillBlockPanel Destroyed View 생명주기 보정
- 관련 EditMode 회귀 테스트

Phase 1 Day 18 — 세레나·엘렌 스킬 및 강화 효과 시스템 구축.
