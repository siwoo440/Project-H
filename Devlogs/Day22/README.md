# Project H — Phase 1 Day 22 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 22
- 기준 원격 커밋: `21d6c74328ac78cbdf87f2b112ff5e8d4e7d8e72`
- 기준 원격 커밋 메시지: `22`
- 주제: 초기 4인 궁극기 발동 및 고유 효과 시스템 구축

---

## 목표

21일차까지 구축한 캐릭터별 궁극기 게이지 Runtime을 실제 전투 행동으로 연결한다.

22일차에서는 게이지 100 및 생존 조건을 만족한 캐릭터가 HUD에서 궁극기를 발동하고, 게이지를 0으로 소비한 뒤 초기 4인의 고유 효과를 실행할 수 있도록 궁극기 전용 실행 흐름을 구축했다.

핵심 방향:

- 일반 Skill Block 실행과 궁극기 실행 경로를 분리한다.
- 궁극기 사용 조건은 생존 상태와 게이지 100을 함께 확인한다.
- 사용 실패 시 게이지를 유지한다.
- 사용 성공 시 게이지를 0으로 소비한다.
- 초기 4인 캐릭터의 궁극기 효과를 전투 Runtime에 연결한다.
- 기존 공통 피해·회복·Modifier·상태이상 시스템을 재사용한다.
- HUD의 `ULT READY` 상태를 실제 입력으로 연결한다.
- 세레나 궁극기용 전투불능 아군 부활 흐름을 추가한다.

---

## BattleUltimateExecutor

궁극기 전용 실행 관리자를 추가했다.

`BattleUltimateExecutor.TryExecute(characterId)`는 현재 Scene의 `BattleCombatRegistry`를 조회한 뒤 실제 실행 함수로 전달한다.

실행 검증 순서:

1. CharacterId 확인
2. BattleCombatRegistry 확인
3. 초기 4인 지원 여부 확인
4. 전투 진행 상태 확인
5. 생존한 궁극기 사용자 조회
6. `CanUseUltimate()`로 게이지 100 및 생존 조건 확인
7. `TryConsumeGauge()`로 게이지 소비
8. `BattleActionKind.Ultimate` 행동 표시
9. 캐릭터별 궁극기 효과 실행

사용 조건을 만족하지 못하면 실패 결과를 반환하고 게이지는 유지한다.

정상 사용 시:

`Gauge 100 → Ultimate Execute → Gauge 0`

궁극기는 별도 Executor를 사용하므로 Skill Block 소비 경로를 호출하지 않는다.

---

## 최신 Unity 객체 조회 API 적용

궁극기 실행 시 현재 Scene의 `BattleCombatRegistry`를 찾는 코드는 최신 Unity API를 사용하도록 정리했다.

기존 경고 발생 API:

`Object.FindObjectOfType<BattleCombatRegistry>()`

현재 코드:

`Object.FindFirstObjectByType<BattleCombatRegistry>()`

이를 통해 `CS0618` obsolete 경고가 발생하던 호출을 최신 API로 교체했다.

---

## BattleUltimateEffectExecutor

초기 4인 캐릭터의 궁극기 효과를 전용 실행기로 분리했다.

지원 캐릭터:

- `CH_SERENA`
- `CH_ELLEN`
- `CH_LILIA`
- `CH_EVE`

궁극기 이름:

- 세레나: `성광의 심판`
- 엘렌: `기사의 결의`
- 릴리아: `별의 낙인`
- 이브: `정령 폭우`

기존 `BattleDamageResolver`, `BattleHealingResolver`, `BattleSkillRuntimeState` 등의 공통 전투 기능을 재사용해 별도의 중복 전투 계산 계층을 만들지 않았다.

---

## 세레나 — 성광의 심판

세레나 궁극기는 아군 생존 지원과 부활을 담당한다.

현재 구현 효과:

- 생존 아군 전체 최대 HP의 25% 회복
- 등록된 제거 가능 Debuff 전체 제거
- 전투불능 아군 1명 부활
- 부활 체력은 최대 HP의 25%

회복은 기존 `BattleHealingResolver`를 사용한다.

정화는 `BattleSkillRuntimeState.RemoveDebuffs()`를 사용한다.

부활 대상은 전투불능 상태의 아군 `BattleDeathHandler`를 찾아 처리하며, 부활 후 전장 객체 활성화와 Registry 재등록까지 수행한다.

---

## BattleDeathHandler 부활 확장

세레나 궁극기를 위해 기존 공통 사망 처리기에 부활 기능을 추가했다.

추가 상태 및 API:

- `AllyStats`
- `CanRevive`
- `TryRevive(reviveHp)`

부활 처리 흐름:

- 전투불능 아군 여부 확인
- 기존 숨김 코루틴 중단
- GameObject 활성화
- 체력 복원
- Ally View 재연결
- Battle Registry 재등록
- 기본 공격 Controller 재활성화
- 체력 변경 이벤트 재연결

사망 후 실행 중이던 숨김 코루틴은 별도 참조로 관리해 부활 시 안전하게 중단할 수 있도록 했다.

---

## 엘렌 — 기사의 결의

엘렌 궁극기는 파티 생존력 강화 효과로 구현했다.

현재 구현 효과:

- 생존 아군 전체 피해 감소 20%
- 엘렌 자신의 방어력 50% 증가
- 현재 구현에서는 전투 종료까지 유지

피해 감소는 `BattleRuntimeModifierKind.DamageReductionPercent`를 사용한다.

자기 방어 증가는 `BattleRuntimeModifierKind.DefensePercent`를 사용한다.

궁극기 전용 SourceKey를 사용해 Runtime Modifier 출처를 분리했다.

---

## 릴리아 — 별의 낙인

릴리아 궁극기는 전체 마법 피해와 마법 저항 감소를 함께 적용한다.

현재 구현 효과:

- 생존 적군 전체 공격력 260% 마법 피해
- 피해 후 생존한 적군의 마법 저항 20% 감소
- 마법 저항 감소 지속시간 10초

전체 피해 중 적 사망으로 Registry가 변경될 수 있으므로 생존 적군 목록을 스냅샷으로 만든 뒤 순회하도록 구성했다.

마법 저항 감소는 제거 가능한 Debuff로 등록된다.

---

## 이브 — 정령 폭우

이브 궁극기는 전체 물리 피해와 행동 제어 효과를 적용한다.

현재 구현 효과:

- 생존 적군 전체 공격력 220% 물리 피해
- 피해 후 생존한 적군 1초 기절

기절은 `BattleRuntimeModifierKind.Stun`을 사용하며 제거 가능한 Debuff로 등록한다.

릴리아와 동일하게 전체 피해 도중 Registry 변경에 영향을 받지 않도록 대상 스냅샷을 사용한다.

---

## HUD 궁극기 입력 연결

기존 `BattleHudCardView`의 궁극기 텍스트를 실제 입력으로 연결했다.

Runtime에서 궁극기 텍스트 GameObject에 `Button`이 없으면 자동으로 추가한다.

HUD 상태:

- 게이지 100 미만: 궁극기 입력 비활성화
- 게이지 100 + 생존: `ULT READY` 및 입력 활성화
- 사망 상태: 궁극기 입력 비활성화

클릭 시 `TryUseUltimate()`가 실행되고 현재 HUD에 연결된 캐릭터 ID를 `BattleUltimateExecutor`에 전달한다.

궁극기 사용으로 게이지가 0이 되면 기존 `GaugeChanged` 이벤트를 통해 HUD 표시도 다시 0 상태로 갱신된다.

`OnDestroy()`에서는 Runtime으로 연결한 궁극기 클릭 이벤트를 해제한다.

---

## 기존 전투 흐름 유지

22일차 구현은 기존 일반 스킬과 패시브 구조를 최대한 유지했다.

유지한 범위:

- 기존 Skill Block 소비 흐름
- 기존 일반 Skill Effect 실행 흐름
- 기존 Passive Trigger 흐름
- 기존 궁극기 게이지 축적 및 100 Clamp
- 기존 전투 시간 기반 초당 게이지 충전
- 기존 피해·회복 계산기
- 기존 Runtime Modifier 및 Debuff 시스템

궁극기는 `BattleUltimateExecutor`로 분리되어 일반 스킬 요청 이벤트를 다시 발생시키거나 Skill Block을 소비하지 않는다.

---

## EditMode Test

22일차 궁극기 관련 EditMode 테스트 파일이 원격 저장소에 추가되어 있다.

### BattleUltimateExecutorTests

검증 코드 범위:

- 게이지 99에서 궁극기 사용 실패
- 사용 실패 후 게이지 유지
- 릴리아 게이지 100에서 정상 사용
- 정상 사용 후 게이지 0
- 전체 적군에 릴리아 피해 적용
- 사망 캐릭터 궁극기 사용 차단
- 사망 상태 실패 후 게이지 유지

### BattleUltimateEffectTests

검증 코드 범위:

- 엘렌 아군 전체 피해 감소 20%
- 엘렌 자기 방어력 50% 증가
- 릴리아 공격력 260% 마법 피해
- 릴리아 마법 저항 20% 감소
- 이브 공격력 220% 전체 피해
- 이브 전체 적군 기절
- 세레나 아군 최대 HP 25% 회복
- 세레나 Debuff 정화
- 세레나 전투불능 아군 25% 체력 부활
- 부활 아군 Registry 복귀

### BattleUltimateHudUseTests

HUD 궁극기 입력과 게이지 Ready 상태를 검증하기 위한 테스트 파일을 추가했다.

---

## 생성 및 수정 파일

수정:

- `Assets/ProjectH/Scripts/Battle/BattleDeathHandler.cs`
- `Assets/ProjectH/Scripts/Battle/BattleHudCardView.cs`

신규 Runtime:

- `Assets/ProjectH/Scripts/Battle/BattleUltimateEffectExecutor.cs`
- `Assets/ProjectH/Scripts/Battle/BattleUltimateExecutor.cs`

신규 EditMode Test:

- `Assets/ProjectH/Tests/EditMode/BattleUltimateEffectTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleUltimateExecutorTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleUltimateHudUseTests.cs`

신규 C# 파일에는 대응하는 Unity `.meta` 파일을 함께 추가했다.

---

## 검수 상태

기준 원격 커밋에서 Day22 궁극기 실행 코드, 초기 4인 효과, HUD 입력, 부활 처리, EditMode 테스트 소스가 반영되어 있는 것을 확인했다.

`BattleUltimateExecutor`의 Scene Registry 조회는 `Object.FindFirstObjectByType<BattleCombatRegistry>()`를 사용하고 있어 앞서 발생한 `CS0618` obsolete 경고 수정도 원격에 반영되어 있다.

GitHub Commit Status에는 별도 CI 상태가 등록되어 있지 않았다.

따라서 이 개발 일지는 원격 소스와 테스트 코드의 반영 상태를 기준으로 작성했으며, Unity Editor 전체 Compile 및 Test Runner 실행 성공을 별도로 주장하지 않는다.

---

## Day 22 완료 상태

21일차에 준비한 궁극기 게이지가 22일차에서 실제 전투 행동으로 연결됐다.

현재 흐름:

`게이지 충전`

→ `100 / ULT READY`

→ `HUD 궁극기 입력`

→ `생존 및 전투 상태 검증`

→ `게이지 100 소비`

→ `캐릭터 고유 궁극기 효과 실행`

→ `GaugeChanged`

→ `HUD 0 상태 복귀`

초기 4인 궁극기의 실제 효과까지 전투 Runtime에 연결되어 다음 단계에서는 궁극기 연출, 데이터화, 추가 캐릭터 확장 및 세부 밸런스 조정으로 이어갈 수 있는 상태다.
