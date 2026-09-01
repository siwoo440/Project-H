# Project H — Phase 1 Day 13 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 13
- 기준 원격 커밋: `97773a72c9fc8d064160dceb0165cada7be4613e`
- 기준 원격 커밋 메시지: `13`
- 주제: 피해 / 회복 시스템 구축

---

## 목표

12일차에서 구축한 자동 타겟팅과 기본 공격 흐름에 실제 HP 변화를 연결한다.

기본 전투 흐름:

`전진 → 가장 가까운 적 탐색 → 공격 사거리 정지 → 공격! → 피해 계산 → 실제 HP 감소 → HP UI 갱신 → 다음 공격`

회복 흐름:

`회복 요청 → 회복 가능량 계산 → 실제 HP 증가 → HP UI 갱신`

13일차에서는 실제 Damage / Heal을 구현하고, HP 0 대상은 행동 및 타겟 후보에서 제외한다.

사망 연출, 승리/패배와 Wave 종료는 15일차 범위로 남긴다.

---

## BattleDamageType

향후 기본 공격과 스킬이 공통 피해 시스템을 사용할 수 있도록 피해 종류를 구분했다.

- `Physical`
- `Magic`
- `True`

현재 기본 공격은 `Physical` 피해로 처리한다.

물리 피해는 Defense, 마법 피해는 Resistance를 사용하고 True 피해는 방어 수치를 무시한다.

---

## BattleDamageResolver

기본 공격 Controller가 HP를 직접 계산하지 않도록 피해 계산을 별도 Resolver로 분리했다.

기본 물리 피해 공식:

`Damage = Max(1, Attack - Defense)`

예:

`Attack 30 - Defense 12 = 18 Damage`

Defense가 Attack보다 높은 경우에도 최소 1 피해를 보장한다.

Magic 피해:

`Magic Power - Resistance`

True 피해:

`Power 그대로 적용`

13일차 기본 공격은 랜덤 명중률과 치명타를 아직 적용하지 않아 피해 계산을 디버깅하기 쉽게 유지한다.

---

## 실제 기본 공격 Damage 연결

12일차 기본 공격은 다음 흐름이었다.

`공격! → Target Flash`

13일차에서는 다음으로 변경했다.

`공격! → BattleDamageResolver.ResolveBasicAttack() → BattleActor.ApplyDamage() → TakeDamage()`

따라서 아군과 적군의 기본 공격이 실제 CurrentHp를 감소시킨다.

공격 후에는 12일차에서 구축한 전선 전진형 전투 구조를 그대로 유지한다.

---

## 변경 가능한 전투 스탯

아군과 적군 모두 같은 피해/회복 시스템을 사용할 수 있도록 공통 계약을 확장했다.

- `IBattleCombatantStats`
  - 전투 수치 조회
- `IBattleMutableCombatantStats`
  - `TakeDamage()`
  - `Heal()`
- `IBattleResistanceStats`
  - `Resistance`

`BattleStats`와 `BattleEnemyStats`는 변경 가능한 전투 스탯을 구현한다.

---

## Character Resistance Runtime 연결

CharacterData의 기존 `BaseResistance`를 BattleStats Runtime에 연결했다.

캐릭터 생성 시 현재 레벨에 맞춰 Resistance 성장 수치를 계산하고 BattleStats에 저장한다.

이를 통해 이후 마법 공격과 마법 스킬이 공통 DamageResolver를 사용할 수 있다.

---

## HealthChanged 이벤트

아군 `BattleStats`와 적군 `BattleEnemyStats`에 체력 변경 이벤트를 추가했다.

피해 또는 회복으로 실제 HP가 변경되었을 때만 `HealthChanged`가 발생한다.

연결 대상:

- `BattleUnitView`
- `BattleEnemyView`
- `BattleHudCardView`

따라서 매 프레임 HP를 확인하지 않고 실제 체력 변화가 발생했을 때만 UI를 갱신한다.

---

## HP Bar 실시간 갱신

피해 발생 시:

`TakeDamage → HealthChanged → View.Refresh`

회복 발생 시:

`Heal → HealthChanged → View.Refresh`

방식으로 동작한다.

갱신되는 정보:

- 전장 아군 HP 수치
- 전장 아군 HP Bar
- 적군 HP 수치
- 적군 HP Bar
- 하단 캐릭터 HUD HP
- 하단 캐릭터 HUD HP Bar

---

## BattleHealingResolver

일반 회복 계산을 Damage와 분리했다.

회복 규칙:

1. 음수 회복량 차단
2. 현재 손실 HP 확인
3. MaxHp를 초과하지 않도록 실제 회복량 제한
4. HP 0 대상은 일반 회복 불가

예:

`60 / 100 + 25 → 85 / 100`

`90 / 100 + 25 → 100 / 100`

HP가 0인 전투 불능 대상은 일반 Heal로 부활하지 않는다.

부활은 이후 별도 기능으로 구현한다.

---

## 피해 / 회복 숫자 표시

개발 중 전투 결과를 바로 확인할 수 있도록 `BattleFloatingValueText`를 추가했다.

피해:

`-18`

회복:

`+25`

기존 행동 디버그 텍스트:

- `공격!`
- `스킬!`
- `궁극기!`

와 함께 사용할 수 있도록 분리했다.

피해 숫자는 붉은 계열, 회복 숫자는 초록 계열의 임시 개발용 Text로 표시한다.

정식 Damage Font, 이동 Animation, Critical 표시 등은 이후 Battle Feedback 단계에서 다룬다.

---

## BattleActor 체력 적용

`BattleActor`에 실제 피해와 회복 적용 진입점을 추가했다.

### ApplyDamage

- Runtime ID 일치 확인
- 변경 가능한 Stats인지 확인
- 실제 `TakeDamage()` 호출
- 피해 Flash
- 피해 숫자 표시

### ApplyHealing

- 대상 생존 여부 확인
- Runtime ID 일치 확인
- 실제 `Heal()` 호출
- 회복 숫자 표시

잘못된 Target Runtime ID에는 체력 변경을 적용하지 않는다.

---

## HP 0 처리

CurrentHp가 0이 되면 `IsAlive = false`가 된다.

12일차 타겟 시스템과 기본 공격 Controller가 이미 생존 상태를 확인하므로 HP 0 대상은:

- 기본 공격 행동 중지
- 새로운 공격 대상에서 제외

된다.

13일차에서는 전투 불능 상태까지만 처리한다.

아직 구현하지 않는 부분:

- 사망 Animation
- 전투 캐릭터 제거
- 승리
- 패배
- 전멸
- Wave 종료

위 기능은 15일차에서 처리한다.

---

## 회복 Debug

`BattleHealthDebugController`를 추가했다.

Battle Menu의 `아군 +25 회복` 버튼을 통해:

- 생존 중이고
- HP가 감소한
- 첫 번째 아군

을 찾아 실제로 25 회복하도록 구성했다.

회복 가능한 피해 아군이 없으면 상태 메시지로 표시한다.

---

## Phase1Day13Setup

BattleScene에 13일차 개발용 표시 및 Debug UI를 적용하는 Editor Setup을 추가했다.

메뉴:

`Tools → Project H → Phase 1 → 13일차 피해-회복 시스템 설정 실행`

적용 내용:

- 아군 `FloatingValueText`
- 적군 `FloatingValueText`
- `BattleHealthDebugController`
- `아군 +25 회복` Debug 버튼
- 관련 Scene 참조 연결

---

## EditMode Test

### BattleDamageResolverTests

- Physical 피해에 Defense 적용
- Magic 피해에 Resistance 적용
- True 피해 방어 무시
- 최소 1 피해 보장

### BattleHealingResolverTests

- MaxHp 초과 회복 차단
- HP 0 대상 일반 회복 차단

### BattleActorHealthIntegrationTests

- 실제 Damage 적용
- CurrentHp 감소
- HealthChanged 발생
- 생존 대상 회복
- 일반 회복에 의한 부활 차단

---

## 최신 원격 저장소 검수

확인한 최신 원격 커밋:

`97773a72c9fc8d064160dceb0165cada7be4613e`

메시지:

`13`

원격 커밋에서 확인한 주요 내용:

- `BattleActor.ApplyDamage()` / `ApplyHealing()` 추가
- 기본 공격의 실제 `BattleDamageResolver` 연결
- `BattleDamageType` 및 변경 가능한 전투 Stats 계약 추가
- `BattleDamageResolver` 추가
- `BattleHealingResolver` 추가
- `BattleFloatingValueText` 추가
- `BattleHealthDebugController` 추가
- `Phase1Day13Setup` 추가
- Day13 EditMode Test 추가

GitHub Commit Status에는 별도 CI Status가 등록되어 있지 않다.

### Scene 반영 확인 사항

현재 원격 `Battle.unity`에는 Day13 Setup이 추가하는 다음 직렬화 요소가 확인되지 않았다.

- `floatingValueText` 참조
- `BattleHealthDebugController`

즉 Day13 소스와 테스트는 원격에 올라와 있지만, 최신 원격 Scene은 아직 Day13 Setup 실행 결과가 저장된 상태가 아니다.

Day13 최종 커밋 전 Unity에서 다음 메뉴를 한 번 실행해야 한다.

`Tools → Project H → Phase 1 → 13일차 피해-회복 시스템 설정 실행`

실행 후 `Battle.unity`가 변경되면 해당 Scene과 이 README를 함께 기존 Day13 커밋에 amend한다.

Unity Editor 실제 Compile / EditMode Test Runner / Play Mode 결과는 원격 저장소 검수만으로 확인할 수 없다.

---

## 13일차 완료 범위

- Physical / Magic / True Damage 기반
- 기본 공격 실제 피해 적용
- Defense / Resistance 반영
- 최소 1 Damage
- 아군 / 적군 CurrentHp 변화
- HealthChanged 이벤트
- 전장 HP Bar 실시간 갱신
- 하단 HUD HP 실시간 갱신
- 일반 Heal
- MaxHp 초과 회복 방지
- 일반 Heal 부활 방지
- 피해 `-숫자` 표시
- 회복 `+숫자` 표시
- HP 0 행동 중지
- HP 0 타겟 제외
- 회복 Debug 기능
- 관련 EditMode Test

Phase 1 Day 13 — 피해 / 회복 시스템 구축.
