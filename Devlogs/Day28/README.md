# Project H — Phase 1 Day 28 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 28
- 기준 원격 커밋: `306cc2e71124442337e17114bc71601ecdbd2bf4`
- 기준 원격 커밋 메시지: `28`
- 주제: 전투 컨텍스트 고정·던전별 실제 적 편성 및 결과 데이터 연동

---

## 목표

27일차에서 `DG001`~`DG004` 선택에 따라 적 능력치 배율과 승리 보상이 달라지도록 연결한 뒤, 28일차에서는 선택된 던전 정보를 전투 시작 시점에 하나의 전투 컨텍스트로 확정하고 전투 전체가 동일한 DungeonId를 사용하도록 구조를 정리했다.

동시에 기존 Battle 씬의 적 생성 구조를 그대로 활용하면서 던전마다 실제 등장 적 수와 조합이 달라지도록 편성 데이터를 추가했다.

이번 일차의 핵심 방향:

- `Battle` 진입 시 선택된 DungeonId를 전투 컨텍스트로 확정한다.
- 전투 도중 던전 선택 상태가 변경되어도 현재 전투의 DungeonId는 유지한다.
- 직접 `Battle` 씬을 실행한 경우 `DG001`을 안전한 기본값으로 사용한다.
- `DG001`~`DG004`에 서로 다른 실제 적 편성을 적용한다.
- 적 능력치 배율, 승리 보상, 전투 결과가 동일한 전투 컨텍스트를 사용하게 한다.
- 전투 결과 데이터에 DungeonId를 포함한다.
- 기존 Battle Scene / Prefab을 직접 수정하지 않고 런타임에서 기존 적 생성 입력값을 연결한다.
- 던전별 적 편성과 전투 컨텍스트를 검증하는 EditMode 테스트를 추가한다.

---

## 전투 컨텍스트 고정

`BattleContextRuntimeState`를 추가했다.

기존 구조에서는 필요한 시점마다 `DungeonSelectionRuntimeState.SelectedDungeonId`를 직접 조회했지만, Day28부터는 Battle 진입 시 DungeonId를 한 번 확정하고 현재 전투가 끝날 때까지 해당 값을 기준으로 동작한다.

주요 상태:

- `DungeonId`: 확정된 현재 전투 DungeonId
- `CurrentDungeonId`: 현재 전투에서 실제 사용할 DungeonId
- `HasContext`: 전투 컨텍스트 확정 여부
- `UsedDirectFallback`: 직접 Battle 실행으로 기본값을 사용했는지 여부
- `DefaultDungeonId`: `DG001`

전투 컨텍스트 흐름:

`DungeonSelect`

→ `DungeonSelectionRuntimeState.SelectedDungeonId`

→ `Battle` 씬 로드

→ `BattleContextRuntimeState.Capture()`

→ 현재 전투 DungeonId 확정

→ 적 편성 / 능력치 / 보상 / 결과 데이터가 동일한 DungeonId 사용

직접 `Battle` 씬을 실행하거나 지원하지 않는 DungeonId가 전달되면 `DG001`을 기본값으로 사용한다.

---

## 던전별 실제 적 편성

`DungeonBattleFormationProfile`을 추가해 던전별 실제 등장 적 수와 몬스터 조합을 정의했다.

| 던전 | 적 수 | 편성 |
| --- | ---: | --- |
| `DG001` | 1 | `MON_CORRUPTED_WOLF` |
| `DG002` | 2 | `MON_CORRUPTED_SOLDIER`, `MON_CORRUPTED_WOLF` |
| `DG003` | 3 | `MON_CORRUPTED_SOLDIER`, `MON_CORRUPTED_WOLF`, `MON_POLLUTED_PLANT` |
| `DG004` | 4 | `MON_CORRUPTED_SOLDIER`, `MON_CORRUPTED_WOLF`, `MON_POLLUTED_PLANT`, `MON_CORRUPTED_SOLDIER` |

`CreateEnemyIds()`는 내부 편성 배열을 그대로 노출하지 않고 복사본을 반환하도록 구성했다.

지원하지 않는 DungeonId가 들어오면 `DG001` 편성을 기본값으로 반환한다.

---

## 기존 Battle 적 생성 구조에 런타임 편성 연결

`DungeonBattleFormationRuntimePatch`를 추가했다.

기존 `BattleScreenController`는 `defaultEnemyIds` 배열을 기준으로 적을 생성하고 있었기 때문에 Battle Scene 또는 Prefab을 다시 구성하지 않고 해당 입력값만 전투 시작 전에 던전별 편성으로 교체하는 방식을 사용했다.

적 편성 연결 흐름:

`BeforeSceneLoad`

→ `SceneManager.sceneLoaded` 구독

→ `Battle` 씬 로드 감지

→ 전투 DungeonId 확정

→ `DungeonBattleFormationProfile.Get()`

→ 현재 던전의 적 ID 배열 생성

→ 기존 `BattleScreenController.defaultEnemyIds`에 적용

→ 기존 `SpawnEnemies()` 흐름이 해당 편성으로 적 생성

기존 전투 생성 코드와 전투 AI, 공격, 사망, 승패 판정 구조는 그대로 유지한다.

---

## 적 능력치 계산을 전투 컨텍스트 기준으로 변경

`BattleEnemyStatsFactory.Create()`가 더 이상 현재 던전 선택 상태를 직접 읽지 않고 `BattleContextRuntimeState.CurrentDungeonId`를 사용하도록 변경했다.

따라서 Day27에서 추가한 던전별:

- HP 배율
- ATK 배율
- DEF 배율
- RES 배율

은 현재 전투에 확정된 DungeonId를 기준으로 계산된다.

전투가 시작된 이후 다른 DungeonId가 선택 상태에 들어가더라도 이미 진행 중인 전투의 적 능력치 기준은 바뀌지 않는다.

몬스터 이름, 공격 속도, 공격 사거리, 이동 속도, AI 유형 등 기존 전투 데이터 계약은 유지한다.

---

## 승리 보상을 전투 컨텍스트 기준으로 변경

`BattleRewardCalculator`에 DungeonId를 직접 전달할 수 있는 계산 경로를 추가했다.

기본 `Calculate(BattleOutcome)` 호출은 현재 `BattleContextRuntimeState.CurrentDungeonId`를 사용한다.

승리 시:

`현재 BattleContext DungeonId`

→ `GameManager.Instance.Data.GetDungeon()`

→ `DungeonData.RewardGold / RewardExp`

→ `BattleReward`

패배 또는 비종료 상태에서는 기존과 동일하게 보상 `0 / 0`을 반환한다.

DungeonData를 사용할 수 없는 직접 Battle 테스트 상황에서는 기존 기본 승리 보상인 `120 Gold / 80 EXP`를 유지한다.

---

## 전투 결과 데이터에 DungeonId 추가

`BattleResultData`에 `DungeonId`를 추가했다.

기존 코드에서 사용하던 `Create()` 오버로드는 유지하면서 내부적으로 현재 전투 컨텍스트 DungeonId를 포함한 결과를 생성한다.

추가된 생성 경로에서는 DungeonId를 명시적으로 전달할 수도 있다.

전투 결과 데이터 구성:

- `ResultId`
- `DungeonId`
- `Outcome`
- `Gold`
- `Experience`
- `StarCount`
- `Members`

이를 통해 전투가 끝난 이후에도 결과가 어느 던전에서 발생했는지 결과 스냅샷 자체에서 확인할 수 있다.

---

## 전투 디버그 오버레이 갱신

기존 `DungeonBattleDebugOverlay`를 현재 전투 컨텍스트 기준으로 갱신했다.

표시 정보:

- 현재 전투 DungeonId
- 직접 Battle 실행 여부
- 던전 이름
- 현재 던전 적 수
- HP 배율
- ATK 배율
- DEF 배율
- RES 배율
- 승리 Gold
- 승리 EXP

적 수는 `ENEMIES 1`~`ENEMIES 4` 형태로 표시된다.

직접 Battle 씬을 실행한 경우에는 `DIRECT→DG001` 형태로 표시한다.

---

## 테스트 추가

Day28에서는 전투 컨텍스트, 실제 적 편성, 결과 DungeonId를 검증하는 EditMode 테스트를 추가했다.

### BattleContextRuntimeStateTests

검증 내용:

- `DG002`를 전투 컨텍스트로 확정한 뒤 선택 상태가 `DG004`로 바뀌어도 현재 전투는 `DG002`를 유지한다.
- 던전 선택 없이 직접 Battle 실행 상황을 가정하면 `DG001` fallback을 사용한다.

### DungeonBattleFormationProfileTests

검증 내용:

- `DG001`~`DG004`의 적 수가 각각 `1 / 2 / 3 / 4`로 구분된다.
- 각 편성에서 사용하는 MonsterData 에셋이 실제 프로젝트에 존재한다.

### BattleResultDungeonContextTests

검증 내용:

- 명시적으로 전달한 DungeonId가 `BattleResultData`에 저장된다.
- 지원하지 않는 DungeonId는 `DG001`로 보정된다.
- 패배 결과에서는 Gold와 EXP가 0이다.

---

## 생성 파일

- `Assets/ProjectH/Scripts/Battle/BattleContextRuntimeState.cs`
- `Assets/ProjectH/Scripts/Battle/BattleContextRuntimeState.cs.meta`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleFormationProfile.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleFormationProfile.cs.meta`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleFormationRuntimePatch.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleFormationRuntimePatch.cs.meta`
- `Assets/ProjectH/Tests/EditMode/BattleContextRuntimeStateTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleContextRuntimeStateTests.cs.meta`
- `Assets/ProjectH/Tests/EditMode/BattleResultDungeonContextTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleResultDungeonContextTests.cs.meta`
- `Assets/ProjectH/Tests/EditMode/DungeonBattleFormationProfileTests.cs`
- `Assets/ProjectH/Tests/EditMode/DungeonBattleFormationProfileTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleEnemyStats.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultData.cs`
- `Assets/ProjectH/Scripts/Battle/BattleRewardCalculator.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleDebugOverlay.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab 변경

없음.

Day28의 적 편성은 기존 `BattleScreenController.defaultEnemyIds` 입력값을 Battle 씬 로드 시 런타임으로 교체하는 방식이므로 기존 Battle Scene과 Prefab의 직렬화 구조를 직접 변경하지 않는다.

---

## 현재 검토 상태

기준 원격 커밋 `306cc2e71124442337e17114bc71601ecdbd2bf4`은 Day27 커밋 `8d86ece0b01e6f30e0ff1ce3621be03bda4d278c`보다 1개 커밋 앞서 있다.

Day28 변경 범위는 총 16개 파일이며:

- 신규 파일 12개
- 수정 파일 4개
- 삭제 파일 0개

로 구성되어 있다.

정적 검토에서 확인한 사항:

- `BattleScreenController`의 기존 `defaultEnemyIds` 필드가 유지되어 런타임 편성 패치 대상과 일치한다.
- `DG001`~`DG004` 편성은 현재 Battle 씬의 적 배치 슬롯 범위 안에서 구성된다.
- 편성에 사용하는 몬스터 ID는 기존 프로젝트 MonsterData 에셋을 사용한다.
- 기존 Battle 결과 생성 호출을 유지할 수 있도록 `BattleResultData.Create()` 오버로드 호환성을 유지한다.
- Day27의 던전별 적 능력치 배율과 DungeonData 보상 구조를 제거하지 않고 전투 컨텍스트 기준으로 연결한다.
- Scene / Prefab 변경은 포함되어 있지 않다.

현재 원격 커밋에는 GitHub CI Status가 등록되어 있지 않다.

따라서 이 개발 일지에서는 Unity Editor 컴파일, EditMode Test Runner 전체 통과, 실제 Play Mode 전투 성공을 단정하지 않는다. 최신 커밋 diff 기준으로는 Day28 개발 일지를 막아야 할 명확한 정적 차단 문제는 확인되지 않았다.

---

## Day28 완료 범위

이번 일차에서 코드상 연결된 흐름:

`DungeonSelect`

→ `DG001 ~ DG004 선택`

→ `Battle 진입`

→ `BattleContextRuntimeState`에 DungeonId 확정

→ `DungeonBattleFormationProfile`에서 던전별 적 편성 조회

→ 기존 Battle 적 생성 입력에 편성 적용

→ 확정 DungeonId 기준 적 능력치 배율 적용

→ 확정 DungeonId 기준 승리 보상 계산

→ `BattleResultData`에 DungeonId 포함

→ 전투 디버그 오버레이에 DungeonId / 적 수 / 배율 / 보상 표시

Day28에서는 던전 선택값을 단순히 Battle 씬으로 전달하는 수준에서 한 단계 확장해, 하나의 전투가 시작부터 결과까지 동일한 DungeonId를 기준으로 처리되도록 전투 컨텍스트를 고정했다.

또한 던전별 능력치 차이뿐 아니라 실제 적 수와 조합까지 달라지도록 연결해 `DG001`~`DG004`가 전투 구성에서도 구분되도록 했다.
