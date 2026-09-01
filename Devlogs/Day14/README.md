# Project H — Phase 1 Day 14 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 14
- 기준 원격 커밋: `e153708479c104ef8d2406b82ef32171adbd69e3`
- 기준 원격 커밋 메시지: `14`
- 주제: 적 AI 및 몬스터 사망 처리 시스템 구축

---

## 목표

13일차에서 실제 피해와 회복, HP 변화까지 연결한 뒤 14일차에서는 몬스터별로 서로 다른 타겟 판단을 수행하도록 적 AI 구조를 분리한다.

추가로 HP가 0이 된 몬스터가 전투 대상에 계속 남거나 화면에 유지되는 문제를 정리한다.

핵심 흐름:

`MonsterData → EnemyAIType → BattleEnemyBrain → Target Policy → BattleBasicAttackController`

사망 흐름:

`HP 0 → Registry 제외 → AI/공격 중지 → DOWN 표시 → 화면 제외`

---

## EnemyAIType

`MonsterData`에 적 행동 유형을 추가했다.

현재 정의:

- `Normal`
- `Rush`
- `Ranged`
- `Defensive`
- `Magic`
- `Assassin`
- `Elite`
- `Boss`

14일차에서 실제 행동이 연결된 유형은 `Normal`, `Rush`, `Ranged`다.

나머지 유형은 이후 상태이상, 스킬 AI, 정예 패턴, 보스 페이즈 시스템을 위한 확장 자리로 유지한다.

---

## 프로토타입 몬스터 AI 설정

현재 프로토타입 3종 몬스터에 AI 유형을 직접 지정했다.

### 침식 병사

`MON_CORRUPTED_SOLDIER`

- `aiType: Normal`

가장 가까운 생존 전방 아군을 공격하는 기본 전선형 AI다.

### 침식된 늑대

`MON_CORRUPTED_WOLF`

- `aiType: Rush`

후열을 희망 타겟으로 선택하는 돌격형 AI다.

다만 앞쪽 아군을 통과할 수 없도록 실제 공격 대상은 전선 차단 판정을 다시 거친다.

### 오염 식물

`MON_POLLUTED_PLANT`

- `aiType: Ranged`

가장 가까운 전선 대상을 선택하되 기존의 긴 `AttackRange`를 이용해 다른 몬스터보다 뒤쪽에서 공격한다.

---

## BattleEnemyTargetPolicy

적 AI의 타겟 판단을 기본 공격 Controller에서 분리했다.

### Normal

가장 가까운 생존 상대를 선택한다.

### Rush

전진 방향에서 가장 깊은 후열 상대를 희망 타겟으로 선택한다.

예:

`늑대 → 전방 아군 → 중간 아군 → 후열 아군`

Rush AI의 희망 타겟은 후열 아군이 될 수 있다.

### Ranged

가장 가까운 전선 상대를 선택한다.

실제 거리 유지는 별도 후퇴 AI가 아니라 몬스터가 가진 `AttackRange`와 `MoveSpeed`를 통해 자연스럽게 처리한다.

---

## BattleFrontBlockerResolver

Rush AI가 후열을 목표로 하더라도 앞의 캐릭터를 통과하지 않도록 전선 차단 판정을 추가했다.

흐름:

`희망 타겟 선택 → 공격자와 희망 타겟 사이의 생존 상대 검사 → 가장 앞 차단 상대를 실제 타겟으로 결정`

예:

`늑대 → 엘렌 → 세레나 → 이브`

희망 타겟:

`이브`

실제 공격 타겟:

`엘렌`

엘렌이 전투 불능이 되면 다음 타겟을 다시 판단한다.

이 방식으로 기존의 X축 전선형 이동과 상대 통과 금지 규칙을 유지한다.

---

## BattleEnemyBrain

적군의 판단을 전담하는 `BattleEnemyBrain`을 추가했다.

주요 역할:

- 현재 `EnemyAIType` 보관
- AI 유형별 희망 타겟 선택
- 전선 차단 반영
- 실제 공격 타겟 반환
- 개발용 타겟 상태 요약

구조:

`BattleEnemyBrain = 판단`

`BattleBasicAttackController = 이동 / 공격 행동`

으로 역할을 분리했다.

향후 암살형, 보스, 도발, 특수 패턴을 추가할 때 기본 공격 코드를 직접 복잡하게 만들지 않고 EnemyBrain과 Target Policy를 확장할 수 있다.

---

## 기본 공격 타겟 유지

기존에는 이동과 공격 주기 중 가장 가까운 상대를 반복해서 다시 선택하는 부분이 있었다.

14일차부터는 현재 타겟이 살아 있는 동안 기본적으로 같은 타겟을 유지한다.

흐름:

`Target 획득 → 접근 → 공격 → Cooldown → 같은 Target 공격`

현재 Target이 사망하거나 유효하지 않게 된 경우에만:

`새 Target 판단`

을 수행한다.

이를 통해 전투 중 캐릭터가 계속 타겟을 변경하는 현상을 줄였다.

---

## BattleCombatRegistry 확장

전투 객체 Registry에 다음 기능을 추가했다.

- `Contains()`
- `CountLiving()`

사망 몬스터가 실제 Registry에서 제외되었는지 확인할 수 있으며, 이후 15일차 승리/패배 판정에서도 팀별 생존 수 집계에 사용할 수 있는 기반을 마련했다.

---

## 몬스터 사망 제외

13일차에서는 HP 0이 되면 `IsAlive = false`가 되어 타겟 판정에서는 제외됐지만 GameObject 자체는 화면에 계속 남을 수 있었다.

14일차에 `BattleEnemyDeathHandler`를 추가했다.

사망 처리 순서:

1. Enemy HP가 0인지 확인
2. 사망 상태 중복 처리 방지
3. `BattleCombatRegistry.Unregister()`
4. `BattleBasicAttackController.enabled = false`
5. `BattleEnemyBrain.enabled = false`
6. Enemy View에 `DOWN` 상태 표시
7. 짧은 지연 후 GameObject 비활성화

사망 몬스터는 Registry에서 즉시 제외되므로 다른 캐릭터가 더 이상 해당 몬스터를 타겟으로 선택하지 않는다.

---

## 사망 몬스터 화면 처리

전투 불능 직후 몬스터가 바로 사라지기보다는 잠시 상태를 확인할 수 있도록 개발용 Preview를 유지한다.

표시 예:

`ENEMY_1 · RUSH · DOWN`

이후 약 `0.35초` 뒤 GameObject를 비활성화한다.

현재 단계에서는 정식 사망 Animation이나 Dissolve VFX를 사용하지 않는다.

---

## BattleEnemyView AI Debug

적군의 Runtime 표시 영역에 AI 유형을 함께 표시하도록 수정했다.

예:

- `ENEMY_0 · NORMAL`
- `ENEMY_1 · RUSH`
- `ENEMY_2 · RANGED`

사망 상태에서는:

- `ENEMY_1 · RUSH · DOWN`

형태로 확인할 수 있다.

정식 출시 UI가 아니라 개발 중 AI 동작 확인을 위한 Debug 표시다.

---

## BattleScreenController 연결

적군 생성 시 다음 Runtime Component를 자동으로 연결한다.

1. `BattleEnemyBrain`
2. `BattleBasicAttackController`
3. `BattleEnemyDeathHandler`

각 몬스터의 `MonsterData.AIType`이 `BattleEnemyStats`를 거쳐 EnemyBrain에 전달된다.

별도의 BattleScene 재구성 작업 없이 적 생성 시 Runtime에서 구성된다.

---

## EditMode Test

### BattleEnemyTargetPolicyTests

검증 항목:

- Normal이 가장 가까운 생존 아군 선택
- Rush가 후열을 희망 타겟으로 선택
- Rush의 희망 타겟 앞에 상대가 있으면 Front Blocker가 실제 타겟이 됨
- Ranged 기본 전선 타겟 선택

### BattleEnemyDeathHandlerTests

검증 항목:

- 적군 HP 0 처리
- Registry에서 사망 적군 제외
- 기본 공격 Controller 비활성화
- EnemyBrain 비활성화
- 사망 적군 GameObject 숨김

### MonsterAITypeAssetTests

프로토타입 몬스터 데이터 검증:

- 침식 병사 → `Normal`
- 침식된 늑대 → `Rush`
- 오염 식물 → `Ranged`

---

## 최신 원격 저장소 검수

확인한 최신 원격 커밋:

`e153708479c104ef8d2406b82ef32171adbd69e3`

현재 메시지:

`14`

원격 커밋 diff에서 다음 구현이 포함된 것을 확인했다.

- 몬스터 3종 `aiType` 데이터
- `BattleEnemyBrain`
- `BattleEnemyTargetPolicy`
- `BattleFrontBlockerResolver`
- `BattleEnemyDeathHandler`
- `BattleCombatRegistry` 확장
- 적군 AI를 사용하는 `BattleBasicAttackController`
- 적 Spawn 시 EnemyBrain / DeathHandler 연결
- Enemy AI / 사망 Debug 표시
- 관련 EditMode Test

GitHub Commit Status에는 등록된 CI 상태가 없다.

따라서 원격 diff 기준으로 14일차 구현 파일 구성과 연결은 확인했지만, GitHub만으로 Unity Editor 실제 Compile, EditMode Test Runner, Play Mode 전투 실행 성공 여부까지 검증할 수는 없다.

---

## 14일차 완료 범위

- `EnemyAIType` 기반
- Normal AI
- Rush AI
- Ranged AI
- AI 판단 / 행동 계층 분리
- 후열 희망 타겟
- 전방 상대 통과 방지
- 현재 타겟 유지
- 타겟 사망 시 재판단
- 몬스터 HP 0 감지
- 사망 몬스터 Registry 제외
- 사망 몬스터 공격 중지
- 사망 몬스터 AI 중지
- 사망 상태 Debug 표시
- 사망 몬스터 화면 제외
- 프로토타입 3종 AI 데이터 적용
- 관련 EditMode Test

15일차에서는 팀별 생존 상태를 기반으로 사망 처리, 전멸, 승리/패배 및 전투 종료 흐름을 확장한다.

Phase 1 Day 14 — 적 AI 및 몬스터 사망 처리 시스템 구축.
