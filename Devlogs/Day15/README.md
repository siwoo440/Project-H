# Project H — Phase 1 Day 15 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 15
- 기준 원격 커밋: `b2b602b4a99428d233b227ad8fcae3ebdbcf7fa8`
- 기준 원격 커밋 메시지: `15`
- 주제: 사망 및 승패 처리 시스템 구축

---

## 목표

14일차에서 몬스터 AI와 적군 사망 제외를 구현한 뒤, 15일차에서는 아군과 적군의 사망 처리를 공통화하고 팀별 생존 상태를 기반으로 실제 승리 / 패배를 판정한다.

핵심 전투 종료 흐름:

`HP 0 → BattleDeathHandler → Registry 제외 → 승패 재판정 → Victory / Defeat → 전투 행동 정지 → 결과 Overlay`

15일차에서는 정식 Result 보상 화면으로 이동하지 않고 BattleScene에서 임시 승패 Overlay를 표시한 뒤 DungeonSelect로 복귀할 수 있도록 구성한다.

---

## BattleOutcome

전투 전체 상태를 명시적으로 관리하도록 `BattleOutcome`을 추가했다.

상태:

- `Preparing`
- `Running`
- `Victory`
- `Defeat`

`BattleOutcomeEvaluator`는 현재 팀별 생존 수를 입력받아 승패를 계산한다.

판정 규칙:

- 아군 생존 수 `<= 0` → `Defeat`
- 적군 생존 수 `<= 0` → `Victory`
- 양쪽 모두 생존 → `Running`

동시 전멸과 같은 예외 상황에서는 현재 안전 규칙으로 `Defeat`을 우선한다.

---

## BattleCombatRegistry 이벤트

14일차의 `BattleCombatRegistry`에 `ActorUnregistered` 이벤트를 추가했다.

기존에는 사망한 전투 객체가 Registry에서 제거되기만 했지만, 15일차부터는 실제 제거가 성공했을 때:

`ActorUnregistered(actor)`

이벤트가 발생한다.

승패 시스템은 매 프레임 생존 수를 검사하지 않고 이 이벤트를 기준으로만 재평가한다.

중복 `Unregister()` 호출은 실제 제거가 발생하지 않으므로 추가 승패 이벤트를 만들지 않는다.

---

## BattleDeathHandler

14일차의 적군 전용 사망 구조를 확장하여 아군 / 적군 공통 `BattleDeathHandler`를 추가했다.

공통 사망 처리 순서:

1. HP 0 확인
2. 중복 사망 처리 차단
3. `BattleCombatRegistry.Unregister()`
4. `BattleBasicAttackController` 중지
5. 적군인 경우 `BattleEnemyBrain` 중지
6. 아군 / 적군 View에 `DOWN` 상태 표시
7. 약 `0.35초` 후 전장 GameObject 비활성화

사망한 캐릭터는 Registry에서 즉시 빠지므로 다음 타겟 후보와 팀별 생존 수에서 제외된다.

---

## 아군 사망 처리

14일차까지는 적군만 사망 시 화면에서 제외되는 구조였다.

15일차부터 아군도 동일하게:

`HP 0 → Registry 제외 → 기본 공격 정지 → DOWN 표시 → 전장 객체 숨김`

순서로 처리된다.

전장 캐릭터는 사라지지만 하단 HUD 카드는 남긴다.

HUD 표시:

`DOWN · 0 / MaxHp`

이를 통해 어떤 파티원이 전투 불능인지 계속 확인할 수 있다.

---

## BattleOutcomeController

전투 승패 감시와 종료 처리를 담당하는 `BattleOutcomeController`를 추가했다.

전투 시작 시:

`Preparing → Running`

으로 전환하고 `BattleCombatRegistry.ActorUnregistered` 이벤트를 구독한다.

전투 객체가 사망하여 Registry에서 제외될 때마다:

- `CountLiving(BattleTeam.Ally)`
- `CountLiving(BattleTeam.Enemy)`

를 조회한 뒤 `BattleOutcomeEvaluator`로 현재 상태를 판정한다.

최종 승패가 한 번 결정되면 감시를 중지하여 Victory / Defeat 중복 처리를 차단한다.

---

## Victory

마지막 적이 Registry에서 제외되면:

`Enemy Living Count = 0`

이 되어 `Victory`가 결정된다.

흐름:

`마지막 적 HP 0 → BattleDeathHandler → Registry 제외 → BattleOutcomeController → Victory`

이후 살아 있는 아군이 다음 타겟을 찾거나 계속 전진하지 않도록 전투 행동을 정지한다.

---

## Defeat

마지막 아군이 Registry에서 제외되면:

`Ally Living Count = 0`

이 되어 `Defeat`가 결정된다.

흐름:

`마지막 아군 HP 0 → BattleDeathHandler → Registry 제외 → BattleOutcomeController → Defeat`

아군 전멸 직후 추가 전투 행동이 진행되지 않는다.

---

## 전투 종료 행동 정지

Victory 또는 Defeat가 결정되면 현재 Registry에 남아 있는 생존 전투 객체를 순회하여 다음 동작을 중지한다.

- `BattleBasicAttackController`
- `BattleEnemyBrain`

또한 Battle 화면에서는:

- 전투 Timer 갱신 중지
- 기존 Battle UI 입력 잠금
- 기존 Menu 숨김
- 회복 Debug 비활성화
- 상태 텍스트를 최종 승패로 변경

처리한다.

마지막 적을 쓰러뜨린 뒤 아군이 계속 오른쪽으로 이동하는 현상을 막는 것이 핵심이다.

---

## BattleResultOverlay

정식 ResultScene 연결 전에 전투 종료를 확인할 수 있도록 Runtime 임시 Overlay를 추가했다.

Victory:

`VICTORY`

`모든 적을 쓰러뜨렸습니다.`

Defeat:

`DEFEAT`

`파티 전원이 전투 불능이 되었습니다.`

공통 버튼:

`던전 선택으로`

버튼을 누르면 기존 `BattleScreenController.ReturnToDungeonSelect()`를 통해 DungeonSelect Scene으로 복귀한다.

현재 Overlay는 Runtime에서 생성되므로 BattleScene에 별도 UI 오브젝트를 수동 배치할 필요가 없다.

---

## BattleScreenController

15일차 전투 종료 흐름을 기존 BattleScreenController에 연결했다.

주요 변경:

- `BattleOutcomeController` Runtime 생성 / 연결
- 아군 Spawn 시 `BattleDeathHandler` 추가
- 적군 Spawn 시 공통 `BattleDeathHandler` 추가
- 모든 전투 객체 Spawn 완료 후 승패 감시 시작
- 최종 승패 처리 진입점 `HandleBattleOutcome()` 추가
- 결과 Overlay 표시
- 승패 이후 Timer 및 UI 입력 중지
- 재초기화 / Scene 종료 시 승패 감시 안전 해제

기존 적군 전용 `BattleEnemyDeathHandler` 파일은 호환성을 위해 유지하지만 신규 Battle Spawn에서는 공통 `BattleDeathHandler`를 사용한다.

---

## 단일 테스트 Wave 표시

현재 실제 복수 Wave 진행 시스템은 아직 구현되지 않았으므로 Battle 화면의 표시를:

`WAVE 1 / 3`

에서:

`WAVE 1 / 1`

로 변경했다.

실제 여러 Wave의 순차 Spawn과 다음 Wave 진행은 이후 던전 전투 진행 구조에서 확장한다.

---

## EditMode Test

### BattleOutcomeEvaluatorTests

검증 항목:

- 양 팀 생존 → `Running`
- 적 전멸 → `Victory`
- 아군 전멸 → `Defeat`
- 동시 전멸 → `Defeat` 우선

### BattleOutcomeControllerTests

검증 항목:

- 마지막 적 Registry 제외 → Victory
- Victory 이벤트 1회만 발생
- 마지막 아군 Registry 제외 → Defeat

### BattleDeathHandlerTests

검증 항목:

- 아군 HP 0 처리
- 아군 Registry 제외
- 아군 기본 공격 중지
- 전장 아군 객체 숨김

---

## 최신 원격 저장소 검수

확인한 최신 원격 커밋:

`b2b602b4a99428d233b227ad8fcae3ebdbcf7fa8`

현재 메시지:

`15`

원격 코드에서 다음 항목을 확인했다.

- `BattleDeathHandler` 공통 사망 처리
- `BattleOutcome` / `BattleOutcomeEvaluator`
- `BattleOutcomeController`
- `BattleCombatRegistry.ActorUnregistered`
- 아군 / 적군 공통 사망 연결
- 승패 이후 생존 전투 행동 정지
- Runtime `BattleResultOverlay`
- 관련 EditMode Test

원격 코드 구조를 검토한 범위에서는 15일차 구현을 막는 명백한 연결 누락은 확인하지 못했다.

다만 GitHub Commit Status에는 CI 결과가 등록되어 있지 않다.

따라서 GitHub 코드 검수로 구조와 파일 반영은 확인했지만 다음 항목은 원격만으로 검증할 수 없다.

- Unity 실제 Compile
- Unity EditMode Test Runner 전체 통과
- Play Mode 실제 Victory / Defeat 동작
- Runtime Overlay 버튼 Scene 전환

위 항목은 Unity Editor 실행 결과로 최종 확인한다.

---

## 15일차 완료 범위

- 아군 / 적군 공통 사망 처리
- 사망 객체 Registry 즉시 제외
- 사망 객체 기본 공격 중지
- 사망 적군 AI 중지
- 아군 / 적군 DOWN 표시
- 전장 사망 객체 화면 제외
- 하단 아군 HUD DOWN 유지
- Registry 제외 이벤트
- 팀별 생존 수 기반 승리 판정
- 팀별 생존 수 기반 패배 판정
- 승패 중복 처리 차단
- 승패 후 모든 기본 공격 중지
- 승패 후 적군 AI 중지
- 전투 Timer 정지
- 기존 Battle UI 입력 잠금
- 임시 Victory / Defeat Overlay
- DungeonSelect 복귀 버튼
- `WAVE 1 / 1` 임시 표시
- 관련 EditMode Test

Phase 1 Day 15 — 사망 및 승패 처리 시스템 구축.
