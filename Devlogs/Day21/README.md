# Project H — Phase 1 Day 21 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 21
- 기준 원격 커밋: `4a85b436199a72cacf3c979799bbef5c3776553f`
- 기준 원격 커밋 메시지: `21`
- 주제: 궁극기 게이지 시스템 구축 및 전투 시간 자동 충전

---

## 목표

20일차까지 구축한 기본 전투·스킬 블록·스킬 효과·패시브 흐름 위에 캐릭터별 궁극기 게이지 Runtime을 추가한다.

21일차 범위에서는 실제 궁극기 효과와 연출을 실행하지 않고, 다음 단계에서 궁극기를 연결할 수 있도록 게이지 축적·Ready 판정·소비 API·HUD 표시까지 기반을 완성하는 것을 목표로 한다.

핵심 방향:

- 궁극기 게이지는 캐릭터별로 독립 관리한다.
- 게이지 범위는 `0 ~ 100`으로 제한한다.
- 전투 Runtime 전용 상태로 관리하며 SaveData에는 저장하지 않는다.
- 성공한 블록 스킬 사용 시 현재 강화도의 `UltimateGaugeGain`을 적용한다.
- 강화도 1 / 2 / 3은 현재 실제 SkillData 기준 `+10 / +20 / +30`을 사용한다.
- 생존 중인 아군은 전투 시간 기준 1초마다 `+1`을 추가 획득한다.
- 게이지 100에 도달하면 Ready 상태가 된다.
- 사망한 캐릭터는 게이지가 100이어도 궁극기를 사용할 수 없다.
- 실제 궁극기 효과·애니메이션 실행은 21일차 범위에 포함하지 않는다.

---

## BattleUltimateGaugeRuntimeState

캐릭터별 궁극기 게이지를 전투 중에만 관리하는 공통 Runtime 상태 저장소를 추가했다.

최대 게이지:

`100`

저장 기준:

`CharacterId → Current Ultimate Gauge`

주요 API:

- `GetGauge(characterId)`
- `GetGaugeRatio(characterId)`
- `IsReady(characterId)`
- `CanUseUltimate(characterId, isAlive)`
- `AddGauge(characterId, amount)`
- `TryConsumeGauge(characterId, isAlive)`
- `ResetGauge(characterId)`
- `ResetAll()`

`AddGauge()`는 최대치를 넘어가는 값을 자동으로 100에 제한한다.

예:

`70 + 50 → 100`

캐릭터별 값은 서로 독립적이므로 한 캐릭터의 충전이 다른 캐릭터의 게이지에 영향을 주지 않는다.

---

## Ready 및 사용 가능 판정

게이지가 100 이상이면 `IsReady()`가 참이 된다.

실제 사용 가능 여부는 게이지만으로 결정하지 않고 생존 상태도 함께 확인한다.

판정 흐름:

`Alive && Gauge >= 100 → CanUseUltimate = true`

따라서 다음 경우는 사용 불가다.

- 게이지가 100 미만
- 캐릭터가 사망 상태

사망한 캐릭터가 게이지 100을 보유하고 있어도 게이지 자체를 즉시 제거하지 않고 사용만 차단한다.

---

## 궁극기 게이지 소비 준비

21일차에서는 실제 궁극기 효과를 실행하지 않지만 다음 단계 연결을 위해 소비 API를 준비했다.

`TryConsumeGauge(characterId, isAlive)`

동작:

- 생존 상태 확인
- 게이지 100 확인
- 조건 충족 시 해당 캐릭터 게이지 0으로 초기화
- 조건 미충족 시 게이지를 유지하고 실패 반환

예:

`Dead + Gauge 100 → Consume 실패 / Gauge 100 유지`

`Alive + Gauge 100 → Consume 성공 / Gauge 0`

이 API를 실제 궁극기 실행 직전에 연결할 수 있도록 구성했다.

---

## 블록 스킬 사용 시 게이지 획득

기존 `SkillData.SkillEnhancementData.UltimateGaugeGain` 값을 실제 전투 게이지에 연결했다.

처리 흐름:

`BattleSkillRequest`

→ `BattleSkillExecutor.TryExecute()` 성공 검증

→ 실제 Skill Effect 실행

→ Passive Trigger 처리

→ `BattleUltimateGaugeGainResolver.Resolve()`

→ 현재 강화도 `UltimateGaugeGain` 조회

→ `BattleUltimateGaugeRuntimeState.AddGauge()`

→ `SkillRequested` 완료 이벤트

현재 실제 세레나 SkillData를 사용하는 EditMode 테스트 기준 충전값:

- 강화도 1: `+10`
- 강화도 2: `+20`
- 강화도 3: `+30`

잘못된 요청 또는 SkillData가 없는 요청은 충전량 0으로 처리한다.

---

## 성공한 스킬 사용 지점 단일 연결

궁극기 게이지 획득은 `BattleSkillExecutor.TryExecute()`의 성공 흐름에 직접 한 번만 연결했다.

스킬 요청이 다음 조건에서 실패하면 게이지도 증가하지 않는다.

- 잘못된 `BattleSkillRequest`
- SkillData 없음
- Registry 없음
- 스킬 소유 캐릭터 사망 또는 미등록
- 스킬 소유 캐릭터 기절 상태

기존 Skill Block 소비 로직은 별도 Controller가 `TryExecute()` 성공 결과를 기준으로 처리하므로 21일차 게이지 시스템에서 블록을 추가 소비하지 않는다.

즉 궁극기 게이지를 위해 스킬 이벤트를 다시 실행하거나 Skill Block을 두 번 소비하는 경로를 만들지 않았다.

---

## 전투 시간 1초마다 +1 자동 충전

추가 요구사항으로 생존 아군이 전투 시간 1초마다 궁극기 게이지를 1씩 획득하도록 확장했다.

`BattleSkillRuntimeDriver`가 매 프레임 `Time.deltaTime`을 `BattleUltimateGaugeTickAccumulator`에 전달한다.

시간 누적기는 1초 단위 Tick을 계산한다.

예:

`0.25초 + 0.75초 → 1 Tick`

`추가 2초 → 2 Tick`

총 3초가 처리되면 생존 아군 각 캐릭터가 `+3`을 획득한다.

자동 충전 대상 조건:

- `BattleTeam.Ally`
- 전투 초기화 완료
- 생존 상태
- 유효한 `CharacterId`

자동 충전 제외 대상:

- 사망 아군
- 적군
- 미초기화 전투 객체
- 유효한 캐릭터 ID가 없는 객체

시간 충전 역시 `AddGauge()`를 사용하므로 100을 초과하지 않는다.

---

## 전투 배속 및 일시정지 연동

초당 충전은 `Time.deltaTime` 기반으로 처리한다.

따라서 기존 전투 시간 배율이 적용된 scaled time을 그대로 따른다.

현재 전투 시간 시스템 기준:

- `×1`에서는 일반 속도 충전
- `×1.5`에서는 전투 시간 진행에 맞춰 더 빠르게 충전
- `×2`에서는 전투 시간 진행에 맞춰 더 빠르게 충전
- `PAUSE`에서 `Time.timeScale = 0`이면 자동 충전도 진행되지 않음

별도의 현실 시간 타이머를 추가하지 않고 기존 전투 시간 흐름에 맞추는 구조다.

---

## BattleUltimateGaugeTickAccumulator

프레임 단위 `deltaTime`을 궁극기 게이지의 1초 단위 증가로 변환하는 전용 누적기를 추가했다.

주요 동작:

- 0 이하 `deltaTime` 무시
- 누적 시간이 1초 미만이면 Tick 0
- 1초 이상이면 완료된 정수 초 수 반환
- 여러 초가 한 번에 들어오면 누락 없이 여러 Tick 처리
- 처리 완료한 정수 초를 누적값에서 차감
- `Reset()`으로 잔여 누적 시간 초기화

프레임마다 직접 게이지를 증가시키지 않고 완료된 초 단위만 계산하도록 분리했다.

---

## BattleSkillRuntimeDriver 확장

기존 주기 피해와 시간 기반 Skill Runtime 갱신을 담당하던 Driver를 궁극기 게이지 시간 충전에도 재사용했다.

`Update()` 처리:

1. `BattleSkillRuntimeState.TickPeriodicEffects(Time.time)`
2. `TickUltimateGauge(Time.deltaTime)`

`TickUltimateGauge()`는 Registry에서 현재 살아있는 아군을 찾고 완료된 Tick 수만큼 각 캐릭터 게이지를 증가시킨다.

`BattleSkillExecutor.Configure()`에서 Registry 객체에 Driver가 없으면 자동으로 추가한다.

따라서 궁극기 게이지 시간 충전을 위해 별도 Scene 오브젝트나 Inspector 연결을 추가하지 않는다.

---

## HUD 궁극기 게이지 연동

기존 하단 캐릭터 프로필 카드의 궁극기 게이지 UI를 실제 Runtime 값에 연결했다.

`BattleHudCardView`는 다음 흐름으로 동작한다.

- `Bind()` 시 현재 캐릭터의 Runtime 게이지 비율 조회
- `BattleUltimateGaugeRuntimeState.GaugeChanged` 구독
- 자신의 `CharacterId`에 해당하는 변경만 반영
- 게이지 Fill 비율 갱신
- Ready 및 사망 상태 텍스트 갱신

표시 예:

`ULT 37%`

게이지 완충 및 생존:

`ULT READY`

게이지 완충 여부와 관계없이 사망:

`ULT 100% · DOWN`

사망 상태에서는 `IsUltimateReady`가 false가 되므로 UI 표시와 사용 가능 판정이 같은 생존 조건을 따른다.

---

## HUD 이벤트 정리

`BattleHudCardView`는 체력 변경 이벤트와 궁극기 게이지 변경 이벤트를 함께 관리한다.

`Bind()` 시 기존 연결을 먼저 해제하고 신규 Runtime 이벤트를 연결한다.

`OnDestroy()`에서도 다음 연결을 해제한다.

- `Stats.HealthChanged`
- `BattleUltimateGaugeRuntimeState.GaugeChanged`

Scene 재진입이나 HUD 재생성 시 이전 카드의 static 이벤트 연결이 남지 않도록 정리한다.

---

## Runtime 초기화

궁극기 게이지는 저장 데이터가 아니라 전투 한정 Runtime 값이다.

초기화 지점:

- `BattleSkillExecutor.Configure()`에서 신규 전투 시작 시 `ResetAll()`
- `BattleSkillRuntimeDriver.OnDestroy()`에서 전투 Runtime 종료 시 `ResetAll()`

`ResetAll()`은 저장된 캐릭터 ID별로 게이지 0 변경 이벤트도 발생시켜 이미 연결된 HUD가 있다면 0 상태로 동기화할 수 있도록 했다.

---

## 기존 전투 시스템 유지

21일차 구현은 기존 전투 구조의 책임을 가능한 한 유지했다.

유지한 범위:

- Skill Block 소비는 기존 Controller 흐름 유지
- 실제 Skill Effect 실행 흐름 유지
- Passive Trigger 흐름 유지
- 하단 프로필 카드 구조 유지
- 우측 Skill Block UI 구조 유지
- 19일차 몬스터 테스트 튜닝 관련 코드 변경 없음

궁극기 게이지 획득 때문에 별도의 Skill Block 소비나 Skill Effect 중복 실행 경로를 추가하지 않았다.

---

## EditMode Test

21일차 궁극기 게이지 관련 테스트 파일이 최신 원격 저장소에 반영되어 있다.

### BattleUltimateGaugeRuntimeStateTests

검증 범위:

- 캐릭터별 게이지 독립 저장
- 최대 100 Clamp
- Ready 판정
- 생존 여부를 포함한 `CanUseUltimate`
- 사망 상태 소비 차단
- 정상 소비 후 0 초기화
- 개별 Reset
- 전체 Reset

### BattleUltimateGaugeGainResolverTests

실제 `SK_SERENA_01.asset`을 로드해 다음 값을 검증한다.

- 강화도 1 → `10`
- 강화도 2 → `20`
- 강화도 3 → `30`
- 잘못된 요청 / SkillData 없음 → `0`

### BattleUltimateGaugeHudTests

검증 범위:

- Runtime 게이지 변경에 따른 HUD 비율 갱신
- 100 도달 시 Ready 상태
- `ResetAll()` 이후 HUD 0 동기화
- 사망 상태에서 게이지 100이어도 Ready false

### BattleUltimateGaugeTickAccumulatorTests

검증 범위:

- 1초 미만 누적
- 1초 완성 시 Tick 반환
- 여러 초 동시 처리
- Reset 후 누적값 초기화

### BattleUltimateGaugeTimedDriverTests

생존 아군·사망 아군·적군을 Registry에 함께 등록한 뒤 시간 Tick 결과를 검증한다.

테스트 흐름:

`0.25초 + 0.75초 + 2초 = 총 3초`

기대 결과:

- 생존 아군: 게이지 `3`
- 사망 아군: 게이지 `0`
- 적군: 게이지 `0`

이 테스트에서 `BattlePosition.Dealer`를 참조하기 위해 필요한 `ProjectH.Data` 네임스페이스 import도 최신 원격 코드에 반영되어 있다.

---

## 최신 원격 저장소 검수

확인한 최신 `main` 커밋:

`4a85b436199a72cacf3c979799bbef5c3776553f`

현재 커밋 메시지:

`21`

이전 Day 20 커밋:

`18c6728fa76f37718583a1e617c616651fd45d42`

원격에서 확인한 주요 상태:

- 캐릭터별 궁극기 게이지 Runtime 구현 반영
- 최대 게이지 100 Clamp 반영
- Ready / 생존 사용 가능 판정 반영
- 게이지 소비 및 Reset API 반영
- 성공한 블록 스킬의 강화도별 게이지 획득 반영
- 실제 세레나 SkillData 기반 `10 / 20 / 30` 테스트 반영
- 생존 아군 전투 시간 1초당 `+1` 자동 충전 반영
- 사망 아군 및 적군 자동 충전 제외 반영
- HUD Runtime 게이지 및 `ULT READY` 연동 반영
- 전투 종료 Runtime 초기화 반영
- 궁극기 게이지 관련 EditMode 테스트 파일 반영
- 이전 `BattlePosition` 참조 컴파일 오류의 `using ProjectH.Data;` 수정 반영
- `Devlogs/Day21/README.md`는 아직 원격에 없음

GitHub Commit Status에는 등록된 CI Status가 없다.

따라서 원격 코드와 테스트 파일의 구조 및 이전에 확인된 네임스페이스 오류 수정은 확인했지만 GitHub 정보만으로 다음 항목의 실제 성공을 증명할 수는 없다.

- Unity 프로젝트 전체 Compile 0 Error
- EditMode Test Runner 전체 0 Fail
- Play Mode 장시간 전투
- 실제 전투 배속별 장시간 게이지 축적 오차
- Scene 반복 진입 후 모든 Runtime 상태 완전 초기화

현재 원격 반영 상태를 검토한 범위에서는 Day 21 개발 일지 작성을 막는 추가 구조상 문제는 확인되지 않았다.

---

## 21일차 완료 범위

- 캐릭터별 궁극기 게이지 Runtime State
- 궁극기 게이지 최대치 100
- 게이지 Add / Ratio / Ready API
- 생존 조건 포함 궁극기 사용 가능 판정
- 게이지 소비 API 준비
- 개별 / 전체 게이지 초기화
- Skill Enhancement `UltimateGaugeGain` 연결
- 강화도 1 / 2 / 3 게이지 `+10 / +20 / +30`
- 성공한 스킬 사용 단일 게이지 충전 경로
- 생존 아군 전투 시간 1초당 `+1` 자동 충전
- 사망 아군 / 적군 자동 충전 제외
- 초 단위 Tick Accumulator
- 기존 Battle Skill Runtime Driver 확장
- HUD 궁극기 게이지 Runtime 연동
- `ULT READY` 표시
- 사망 상태 궁극기 사용 차단
- Runtime 종료 초기화
- Day 21 EditMode Test 추가
- Timed Driver 테스트 `BattlePosition` 네임스페이스 오류 수정

Phase 1 Day 21 — 궁극기 게이지 시스템 및 전투 시간 자동 충전 기반 구축.
