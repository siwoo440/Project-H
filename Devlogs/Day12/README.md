# Project H — Phase 1 Day 12 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 12
- 기준 원격 커밋: `bc802139afb18bda80b931af3e8b86329616525c`
- 기준 원격 커밋 메시지: `12`
- 주제: 타겟팅 및 자동 기본 공격 기반 구축

---

## 목표

11일차에 BattleScene과 아군/적군 배치 기반을 구축한 뒤, 12일차에서는 전투 캐릭터가 자동으로 상대를 탐색하고 전진하여 기본 공격을 반복하는 전투 행동 기반을 구현한다.

전투 진행 방향은 2D 횡스크롤형 자동 전투를 기준으로 한다.

기본 흐름:

`Battle 시작 → 전방 상대 탐색 → 전진 → 공격 사거리에서 정지 → 기본 공격 → 공격 대기 → 상대 재확인 → 계속 전진/공격`

12일차에서는 실제 Damage 및 HP 감소를 적용하지 않고 공격 행동과 타겟팅까지 완성한다.

---

## 공통 전투 타입

아군과 적군이 같은 전투 로직을 사용할 수 있도록 공통 전투 타입을 추가했다.

주요 요소:

- `BattleTeam`
  - Ally
  - Enemy
- `BattleActionKind`
  - BasicAttack
  - Skill
  - Ultimate
- `BattleAttackState`
  - Idle
  - Approach
  - Attack
  - Cooldown
- `IBattleCombatantStats`

아군 `BattleStats`와 적군 `BattleEnemyStats`가 동일한 전투 행동 시스템에서 사용될 수 있도록 구성했다.

---

## CharacterData 전투 이동 수치

캐릭터가 실제 전장에서 움직이고 사거리 기반 공격을 수행할 수 있도록 다음 값을 추가했다.

- `AttackRange`
- `MoveSpeed`

기본 보정값:

- AttackRange: `1.6`
- MoveSpeed: `2.0`

기존 CharacterData Asset에서 신규 필드가 비정상적으로 0으로 들어온 경우 Day12 Setup이 기본값으로 보정한다.

실제 12인 캐릭터별 최종 수치는 이후 밸런스 단계에서 조정한다.

---

## 적군 Runtime

11일차의 단순 Enemy Preview를 실제 MonsterData 기반 Runtime으로 교체했다.

12일차 테스트 적군:

- `MON_CORRUPTED_SOLDIER`
- `MON_CORRUPTED_WOLF`
- `MON_POLLUTED_PLANT`

각 적은 MonsterData의 다음 값을 사용한다.

- MaxHp
- Attack
- Defense
- Resistance
- AttackSpeed
- AttackRange
- MoveSpeed

`BattleEnemyStatsFactory`가 MonsterData를 `BattleEnemyStats`로 변환한다.

---

## 전투 객체 Registry

전투 중 존재하는 아군과 적군을 한 곳에서 관리하도록 `BattleCombatRegistry`를 추가했다.

역할:

- 아군 등록
- 적군 등록
- 전투 객체 등록 해제
- 전체 초기화
- 현재 위치 기준 가장 가까운 상대 조회

아군과 적군의 기본 공격 Controller가 동일한 Registry를 사용한다.

---

## 타겟 선택

`BattleTargetSelector`를 통해 자동 타겟팅을 구현했다.

초기 구현 후 횡스크롤 전투 방식에 맞춰 타겟 기준을 보정했다.

최종 기준:

1. 생존한 상대만 대상
2. 같은 팀 제외
3. 자신의 전진 방향에 있는 상대 우선
4. 전방 상대 중 X축 거리가 가장 가까운 적 우선
5. 비정상적인 배치 상황에서는 가장 가까운 상대를 안전 대체 대상으로 사용

아군은 오른쪽, 적군은 왼쪽으로 진행한다.

---

## 전선 전진형 기본 공격

초기 12일차 구현은:

`접근 → 공격 → 원래 진형 위치 복귀`

방식이었다.

전투 방향을 재검토한 뒤 횡스크롤 자동전투에 맞춰 다음 방식으로 변경했다.

`전진 → 적 발견 → 공격 사거리에서 정지 → 공격 → 현재 위치 유지 → 반복 공격 → 적 제거 후 다시 전진`

따라서 공격 후 최초 AllySlot / EnemySlot 위치로 돌아가지 않는다.

캐릭터와 적군이 전선 전체를 계속 앞으로 밀어가는 느낌을 목표로 한다.

---

## 상대 통과 방지

이동은 자유로운 2D 추적이 아니라 X축 전선 이동을 기준으로 처리한다.

아군:

`ALLY → → → ENEMY`

적군:

`ALLY ← ← ← ENEMY`

상대의 X 위치에서 자신의 AttackRange만큼 떨어진 지점을 정지선으로 사용한다.

MoveSpeed가 매우 높더라도 한 프레임에 상대를 지나쳐 반대편으로 넘어가지 않도록 이동 위치를 정지선에서 제한한다.

캐릭터의 Y 좌표는 전투 연출용 Lane으로 유지한다.

---

## 공격 사거리

횡스크롤 전투 방식에 맞춰 공격 가능 여부는 주로 X축 거리로 판정한다.

따라서 캐릭터가 화면상 약간 위아래로 어긋나 있어도 Y 차이 때문에 가까운 적을 공격하지 못하는 문제를 방지한다.

근거리 캐릭터는 적 앞까지 이동하고, 사거리가 긴 캐릭터는 더 먼 위치에서 공격할 수 있다.

---

## AttackSpeed

기본 공격 주기는 기존 BattleStats의 AttackSpeed를 실제 행동 시간에 연결한다.

기본 계산:

`AttackInterval = 1 / AttackSpeed`

공격 후 현재 전선 위치에서 대기하고, 대기 시간이 끝나면 가장 가까운 상대를 다시 확인한다.

---

## 행동 디버그 텍스트

전투 행동을 빠르게 확인할 수 있도록 캐릭터와 적군 머리 위에 행동 디버그 텍스트를 추가했다.

표시:

- 기본 공격 → `공격!`
- 스킬 → `스킬!`
- 궁극기 → `궁극기!`

현재 실제 자동 전투에서는 기본 공격 시 `공격!`이 자동 표시된다.

스킬과 궁극기는 아직 실제 시스템이 없기 때문에 Debug Menu에서 텍스트 표시를 수동 확인할 수 있다.

Debug Menu:

- 공격! 표시
- 스킬! 표시
- 궁극기! 표시

향후 실제 Skill / Ultimate 구현 시 동일한 `BattleActionDebugText` API를 사용한다.

---

## 기본 공격 적중 Preview

12일차에서는 공격 동작을 확인할 수 있도록 적중 대상이 짧게 Flash되는 Preview를 추가했다.

현재 처리:

`공격! → 짧은 공격 시간 → Target Flash → AttackSpeed 대기`

아직 `TakeDamage()`를 호출하지 않는다.

실제 피해 공식과 HP 감소는 13일차에 연결한다.

---

## 전투 진형 Y 간격 축소

전투 레퍼런스처럼 같은 팀 캐릭터들이 하나의 덩어리로 보이도록 진형의 Y 간격을 크게 줄였다.

아군:

- AllySlot_0: `(-3.20, -0.25)`
- AllySlot_1: `(-3.50, -0.05)`
- AllySlot_2: `(-3.75, 0.20)`
- AllySlot_3: `(-4.00, 0.40)`

아군 Y 전체 범위:

`0.65`

적군:

- EnemySlot_0: `(3.10, -0.15)`
- EnemySlot_1: `(3.45, 0.15)`
- EnemySlot_2: `(3.75, 0.35)`
- EnemySlot_3: `(4.05, 0.50)`
- EnemySlot_4: `(4.35, 0.05)`

적군 Y 전체 범위:

`0.65`

전진 중에는 각 캐릭터의 Y 위치를 유지하고 X축으로 이동한다.

---

## BattleFormationLayout

진형 Y 간격 테스트를 추가하는 과정에서 EditMode Test가 Editor 전용 Namespace를 직접 참조하는 문제가 발견됐다.

발생 오류:

`CS0234: ProjectH.EditorTools namespace를 찾을 수 없음`

원인:

`BattleCompactFormationTests`가 `Assets/ProjectH/Scripts/Editor`의 `Phase1Day12CompactFormationSetup`을 직접 참조했다.

Unity의 Editor 전용 Assembly와 일반 EditMode Test Assembly 사이 참조 문제였다.

수정:

진형 좌표 적용 로직을 Runtime 영역의:

`ProjectH.Battle.BattleFormationLayout`

으로 분리했다.

최종 구조:

`BattleCompactFormationTests → BattleFormationLayout ← Phase1Day12CompactFormationSetup`

따라서 테스트와 Editor 메뉴가 동일한 Runtime 공통 로직을 사용하며, 테스트에서 `ProjectH.EditorTools`를 직접 참조하지 않는다.

---

## BattleScene Setup

12일차 Setup은 11일차 BattleScene을 통째로 다시 생성하지 않고 기존 Scene을 업그레이드하는 방식으로 구성했다.

주요 처리:

- CharacterData AttackRange / MoveSpeed 보정
- Enemy Preview 제거
- 실제 Enemy Runtime용 SpawnedEnemies 생성
- Ally BattleActor 추가
- Enemy BattleActor 추가
- BattleActionDebugText 생성
- BattleCombatRegistry 연결
- 기본 공격 Controller 연결
- Debug 행동 버튼 연결

별도의 진형 Y 조정 메뉴도 제공한다.

`Tools → Project H → Phase 1 → 12일차 전투 진형 Y 간격 축소`

이 메뉴는 BattleScene 전체를 재구성하지 않고 Formation Anchor 좌표만 수정한다.

---

## EditMode Test

12일차에 추가하거나 확장한 테스트 영역:

### BattleTargetSelectorTests

- 가장 가까운 생존 전방 적 선택
- 같은 팀 제외
- 죽은 적 제외
- 전방 적 우선
- 생존 상대 없음 처리

### BattleBasicAttackTimingTests

- AttackSpeed 역수 기반 공격 주기
- 잘못된 공격속도 최소값 보정

### BattleActionDebugTextTests

- BasicAttack → `공격!`
- Skill → `스킬!`
- Ultimate → `궁극기!`

### BattleEnemyStatsFactoryTests

- MonsterData 기반 Enemy Runtime 생성
- AttackSpeed 반영
- AttackRange 반영
- MoveSpeed 반영

### BattleStatsMobilityTests

- CharacterData AttackRange → BattleStats
- CharacterData MoveSpeed → BattleStats

### BattleForwardAdvanceTests

- 아군 X축 전진
- 적군 X축 전진
- AttackRange 앞 정지
- 상대 통과 방지
- Y Lane 유지
- X축 공격 사거리 판정

### BattleCompactFormationTests

- 아군 Y 범위 0.65
- 적군 Y 범위 0.65
- Runtime `BattleFormationLayout`을 통한 진형 좌표 적용

---

## 최신 원격 저장소 확인

확인한 최신 원격 커밋:

`bc802139afb18bda80b931af3e8b86329616525c`

메시지:

`12`

이 커밋에는 Day12 최초 타겟팅 / 기본 공격 / 행동 디버그 구현이 포함되어 있다.

다만 이후 진행한:

- 전선 전진형 이동 보정
- 상대 통과 방지
- 전방 X축 타겟 우선
- Y 간격 축소
- `BattleCompactFormationTests` Editor Assembly 참조 오류 수정
- `BattleFormationLayout` Runtime 분리

는 현재 원격 커밋 이후의 로컬 후속 수정이다.

따라서 Day12 최종 상태로 커밋할 때는 README만 추가하지 말고 위 후속 수정 파일까지 함께 Stage하여 기존 Day12 커밋을 amend해야 한다.

GitHub Commit Status에는 별도의 CI Status가 등록되어 있지 않다.

Unity Editor 실제 Compile / EditMode Test Runner / Play Mode 전체 실행 결과는 원격 저장소만으로 확인할 수 없다.

---

## 12일차 완료 범위

- CharacterData AttackRange / MoveSpeed
- 공통 BattleTeam / BattleActor
- BattleCombatRegistry
- 실제 Enemy Runtime
- 자동 타겟 선택
- 가장 가까운 전방 적 우선
- X축 전선 이동
- 상대 통과 방지
- 공격 사거리 정지
- AttackSpeed 기반 반복 공격
- 공격 후 원위치 미복귀
- `공격! / 스킬! / 궁극기!` 행동 디버그 텍스트
- 기본 공격 Target Flash
- 전투 진형 Y 간격 축소
- 진형 좌표 Runtime 공통화
- 관련 EditMode Test

13일차에서는 현재 기본 공격 적중 흐름에 Damage / Heal 계산과 실제 HP 변화를 연결한다.

Phase 1 Day 12 — 타겟팅 및 자동 기본 공격 기반 구축.
