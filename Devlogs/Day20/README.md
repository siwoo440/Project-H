# Project H — Phase 1 Day 20 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 20
- 기준 원격 커밋: `69786e879ee7c93fa7073781b6cb27603ab94fe1`
- 기준 원격 커밋 메시지: `20`
- 주제: 초기 4인 패시브 및 공통 패시브 전투 시스템 구축

---

## 목표

19일차까지 구축한 Active Skill 시스템과 별도로 전투 이벤트 기반 Passive 시스템을 추가하고, 초기 4인 세레나·엘렌·릴리아·이브의 패시브를 실제 전투 흐름에 연결한다.

핵심 방향:

- 패시브는 Skill Block을 소비하지 않는다.
- 패시브는 Active Skill의 `SkillRequested`를 다시 발생시키지 않는다.
- 전투 Trigger를 공통 Context로 전달한다.
- 쿨다운·카운터·보호막·시간 제한 효과는 전투 Runtime에서 관리한다.
- 기존 Damage / Skill Runtime을 최대한 재사용한다.
- Scene 재진입 시 Runtime 상태가 남지 않도록 초기화한다.

---

## 공통 Passive Trigger

추가된 패시브 Trigger:

- `BattleStart`
- `BasicAttackHit`
- `BasicAttackMiss`
- `DamageTaken`
- `Death`
- `SkillUsed`
- `SkillEnhancementUsed`

`BattlePassiveEventContext`가 Trigger와 함께 다음 정보를 전달한다.

- Trigger 주체 `Actor`
- 상대 `Target`
- `BattleSkillRequest`
- 피해 등 수치 `Amount`

현재 실제 초기 4인 연결에는 BasicAttack Hit/Miss, DamageTaken, SkillUsed 계열 Trigger가 사용된다.

---

## BattlePassiveRuntimeState

패시브 전용 Runtime 상태 저장소를 추가했다.

관리 항목:

- 패시브 Cooldown
- 연속 행동 Counter
- Runtime ID별 Shield
- 시간 제한 Critical Chance Bonus
- 시간 제한 Defense Reduction

전투 종료 시 `ResetAll()`로 전체 상태를 제거한다.

### Cooldown

패시브별 고유 Key를 기준으로 재발동 제한 시간을 관리한다.

예:

`PASSIVE_SERENA_SHIELD:ALLY_0`

### Counter

Runtime ID와 Counter Key를 조합해 캐릭터별 누적 횟수를 관리한다.

이브:

`EVE_BASIC_HIT`

### Shield

보호막은 HP보다 먼저 피해를 흡수한다.

예:

`Shield 6 + Damage 10 → Shield 0 + HP Damage 4`

동일 대상에게 보호막을 다시 부여할 경우 현재 구현은 단순 누적 대신 더 큰 값이 유지된다.

---

## 세레나 패시브

캐릭터 ID:

`CH_SERENA`

발동 조건:

- 세레나가 파티에 존재하고 생존 중
- 피해를 받은 아군이 생존 중
- 해당 아군 HP가 최대 HP의 30% 이하
- 패시브 쿨다운 사용 가능

효과:

- 대상 최대 HP의 6% 보호막
- 최소 보호막 1
- 패시브 쿨다운 12초

예:

`Max HP 200 → Shield 12`

보호막은 `BattleActor.ApplyDamage()`에서 실제 HP 감소보다 먼저 적용된다.

---

## 엘렌 패시브

캐릭터 ID:

`CH_ELLEN`

발동 조건:

- 피해를 받은 대상이 엘렌
- 피해 후에도 생존
- HP 40% 이하
- 패시브 쿨다운 사용 가능

효과:

- Defense +20%
- 지속 10초
- 패시브 쿨다운 20초

기존 `BattleSkillRuntimeState.AddModifier()`의 `DefensePercent`를 재사용한다.

즉 패시브 전용 별도 방어 계산을 만들지 않고 기존 전투 Modifier 파이프라인에 연결했다.

---

## 릴리아 패시브

캐릭터 ID:

`CH_LILIA`

발동 조건:

- 릴리아가 스킬 사용
- 사용한 스킬이 광역 스킬로 판정됨

광역 판정 대상:

- `AllEnemies`
- `AllAllies`
- `LineEnemies`
- `NearbyEnemiesFromPrimary`

대표 `SkillData.TargetType`뿐 아니라 현재 강화도의 실제 `Effects[]`도 검사한다.

효과:

- 생존 적 전체 Defense -5%
- 지속 6초

`BattleDamageResolver`의 Physical Damage 방어 계산에 실제 적용된다.

계산 흐름:

`Skill Runtime Defense → Passive Defense Reduction → Physical Mitigation`

예:

`Effective Defense 100 → Passive -5% → Mitigation 95`

---

## 이브 패시브

캐릭터 ID:

`CH_EVE`

발동 조건:

- 이브 기본 공격 적중
- 5회 연속 적중

실패 조건:

- 기본 공격 Miss 발생 시 연속 적중 Counter 초기화

효과:

- Critical Chance +4%p
- 지속 10초
- 발동 후 연속 적중 Counter 0으로 초기화

예:

`기본 Crit 10% + Passive 4%p = 최종 14%`

---

## 기본 공격 Critical 연결

이브 패시브가 실제 전투 결과에 영향을 주도록 기본 공격에 Critical 판정을 연결했다.

흐름:

`Accuracy Hit Check → Critical Chance Check → Damage Multiplier → Damage Resolver`

최종 치명타율:

`BattleStats.CriticalRate + Passive Critical Bonus`

현재 프로토타입 기본 치명타 피해 배율:

`1.5x`

이 수치는 현재 20일차 코드에서 임시 공통값으로 사용한다.

치명타 발생 시 Debug Log:

`[Project H][CRIT]`

를 출력한다.

---

## 보호막 Damage 처리

`BattleActor.ApplyDamage()`의 처리 순서를 다음처럼 확장했다.

`Damage Result → Passive Shield Absorb → Remaining Damage → HP Damage`

보호막이 피해를 전부 흡수하면 HP는 감소하지 않는다.

보호막으로 일부만 흡수하면 잔여 피해만 실제 `TakeDamage()`에 전달한다.

보호막 흡수 시 Debug Log:

`[Project H][SHIELD]`

를 출력한다.

---

## DamageTaken Trigger

실제 HP 피해가 발생한 뒤:

`BattlePassiveEventContext.CreateDamageTaken()`

을 발생시킨다.

현재 이 Trigger로:

- 세레나 저체력 아군 보호막
- 엘렌 저체력 방어 증가

를 처리한다.

보호막으로 피해가 완전히 흡수되어 실제 HP Damage가 0이면 DamageTaken 패시브는 발생하지 않는다.

---

## BasicAttack Hit / Miss Trigger

기본 공격 명중 시:

`CreateBasicAttackHit()`

기본 공격 빗나감 시:

`CreateBasicAttackMiss()`

를 패시브 시스템에 전달한다.

현재 이브의 5회 연속 적중 카운터가 이 Trigger를 사용한다.

기본 공격의 기존 Accuracy 판정과 함께 동작한다.

---

## SkillUsed Trigger

`BattleSkillExecutor`가 Skill Effect 실행을 마친 뒤 패시브 시스템에 Skill Context를 전달한다.

강화도 1:

`SkillUsed`

강화도 2~3:

`SkillEnhancementUsed`

로 분류한다.

현재 릴리아의 광역 스킬 사용 패시브가 이 흐름에 연결되어 있다.

Active Skill Effect 실행과 패시브 Trigger는 분리되어 있으므로 패시브가 다시 Skill Block을 소비하지 않는다.

---

## BattlePassiveDriver

Battle Scene의 패시브 생명주기를 관리하는 Driver를 추가했다.

`Awake`

- `BattlePassiveSystem.Initialize()`
- 패시브 Runtime 초기화

`Start`

- `BattleStart` Trigger 발생

`OnDestroy`

- `BattlePassiveSystem.Shutdown()`
- 패시브 Runtime 상태 제거

Battle Scene의 `BattleController`에 `BattlePassiveDriver`가 연결되어 있다.

---

## Phase1Day20Setup

에디터 메뉴:

`Tools → Project H → Phase 1 → 20일차 패시브 시스템 설정 실행`

처리 내용:

- Battle Scene 열기
- `BattleController` 조회
- `BattleCombatRegistry` 확인
- `BattlePassiveDriver`가 없으면 추가
- Registry 연결
- Battle Scene 저장
- AssetDatabase 저장 및 Refresh

현재 최신 원격 Battle Scene에는 `BattlePassiveDriver` 컴포넌트가 이미 반영되어 있다.

---

## EditMode Test

20일차에서 추가된 주요 테스트 범위:

### BattlePassiveRuntimeStateTests

- 보호막이 HP보다 먼저 피해 흡수
- 보호막 소진 후 잔여 피해 계산
- 패시브 Cooldown 만료
- Critical Bonus 지속시간
- Defense Reduction 지속시간
- Defense Reduction의 Physical Damage Resolver 실제 반영

### BattlePassiveSystemTests

- 세레나 HP 30% 조건 보호막
- 세레나 최대 HP 6% 보호막
- 엘렌 HP 40% 조건
- 엘렌 Defense +20%
- 이브 5회 연속 적중
- 이브 Critical Chance +4%p
- Miss 후 연속 적중 Counter 초기화

### BattlePassiveAreaSkillTests

- 대표 TargetType가 단일이어도 실제 Enhancement Effect에 `AllEnemies`가 포함되면 광역 스킬로 판정

---

## 최신 원격 저장소 검수

확인한 최신 `main` 커밋:

`69786e879ee7c93fa7073781b6cb27603ab94fe1`

현재 커밋 메시지:

`20`

부모 커밋:

`b1356e15b2e42f287df9745c4109fa97dfe5acaa`

원격에서 확인한 주요 상태:

- Battle Scene에 `BattlePassiveDriver` 연결
- Shield → HP Damage 순서 반영
- DamageTaken Passive Trigger 연결
- BasicAttack Hit / Miss Trigger 연결
- 기본 공격 Critical 판정 연결
- Passive Defense Reduction의 Physical Damage 계산 연결
- SkillUsed / SkillEnhancementUsed Trigger 연결
- 세레나·엘렌·릴리아·이브 초기 패시브 구현
- 패시브 Runtime State 구현
- EditMode 패시브 테스트 파일 반영
- `Devlogs/Day20/README.md`는 아직 원격에 없음

GitHub Commit Status에는 등록된 CI Status가 없다.

따라서 원격 코드/Scene/테스트 파일 반영은 확인했지만 GitHub만으로 다음 항목의 실제 성공을 증명할 수는 없다.

- Unity 전체 Compile
- EditMode Test Runner 전체 0 Fail
- Play Mode 장시간 전투
- 세레나 보호막 실제 UI/연출
- 엘렌 Defense Modifier 체감
- 릴리아 광역 스킬 후 물리 피해 변화
- 이브 치명타 발동 빈도
- Scene 재진입 후 Runtime 완전 초기화

현재 원격 반영 상태에서 개발 일지 작성을 막는 추가 구조 문제는 확인되지 않았다.

---

## 20일차 완료 범위

- 공통 Passive Trigger Type
- Passive Event Context
- Passive Runtime State
- Passive Cooldown
- Passive Counter
- Passive Shield
- Passive Critical Bonus
- Passive Defense Reduction
- Battle Passive System
- Battle Passive Driver
- 세레나 초기 패시브
- 엘렌 초기 패시브
- 릴리아 초기 패시브
- 이브 초기 패시브
- 기본 공격 Critical 판정
- Shield 우선 Damage 처리
- Passive Defense Reduction 피해 계산 연결
- Skill / Enhancement Passive Trigger
- Battle Scene 패시브 Driver 연결
- Day20 Editor Setup
- Day20 EditMode Test

Phase 1 Day 20 — 초기 4인 패시브 및 공통 패시브 전투 시스템 구축.
