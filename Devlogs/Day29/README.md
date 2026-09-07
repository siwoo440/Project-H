# Project H — Phase 1 Day 29 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 29
- 기준 원격 커밋: `bed93413825a2dd31befbbc00fb82db7f6f1df93`
- 기준 원격 커밋 메시지: `29`
- 이전 커밋: `63200c19a10f8d330e9d7c15eb77152bb77495cd`
- 주제: 캐릭터 레벨링·던전 클리어 진행도·순차 해금 및 영구 저장 연동

---

## 목표

Day28에서 DungeonId를 전투 컨텍스트로 고정하고 던전별 적 편성·능력치·보상·결과 데이터를 연결한 뒤, Day29에서는 전투 승리 결과가 실제 성장과 던전 진행으로 이어지도록 진행 시스템을 확장했다.

이번 일차의 핵심 방향:

- 전투 승리 EXP를 캐릭터의 실제 레벨 상승으로 연결한다.
- 레벨별 필요 EXP, 초과 EXP 이월, 다중 레벨업, 최대 레벨 처리를 구현한다.
- 전투 승리 시 해당 DungeonId의 클리어 상태를 영구 저장한다.
- `DG001` → `DG002` → `DG003` → `DG004` 순서로 다음 던전을 해금한다.
- 잠긴 던전은 선택과 전투 진입 단계에서 모두 차단한다.
- 던전 선택 화면에서 `LOCKED / AVAILABLE / CLEARED` 진행 상태를 표시한다.
- 기존 `SaveData` 구조와 Battle Scene / DungeonSelect Scene을 직접 재구성하지 않고 현재 런타임 구조에 연결한다.
- 레벨링, 던전 진행, 선택 차단, 전투 결과 연동을 EditMode 테스트로 검증할 수 있게 구성한다.

---

## 캐릭터 레벨링 시스템

`CharacterLevelProgression`을 추가해 전투에서 얻은 EXP를 현재 레벨과 잔여 경험치에 반영하도록 구성했다.

Day29 임시 성장 규칙:

| 항목 | 값 |
| --- | ---: |
| 시작 레벨 | Lv.1 |
| 최대 레벨 | Lv.20 |
| Lv.1 → 2 필요 EXP | 100 |
| 레벨당 필요 EXP 증가량 | +50 |
| 초과 EXP | 다음 레벨로 이월 |
| 최대 레벨 도달 후 EXP | 0으로 처리 |

필요 경험치 공식:

`100 + ((현재 레벨 - 1) × 50)`

예시:

- Lv.1 → 2: 100 EXP
- Lv.2 → 3: 150 EXP
- Lv.3 → 4: 200 EXP
- Lv.19 → 20: 1000 EXP
- Lv.20: 추가 필요 EXP 없음

`Apply()`는 현재 Level, 현재 Experience, 획득 EXP를 받아 반복적으로 레벨업 조건을 검사한다.

따라서 Lv.1 / 0 EXP 상태에서 380 EXP를 획득하면:

`Lv.1 / 380`
→ 100 EXP 소비
→ `Lv.2 / 280`
→ 150 EXP 소비
→ `Lv.3 / 130`

형태로 한 번의 전투에서 여러 레벨이 상승할 수 있다.

최대 Lv.20에 도달하면 남은 경험치는 현재 임시 규칙에 따라 0으로 처리한다.

---

## 전투 결과와 레벨 성장 연동

기존 `BattleResultCommitService`의 경험치 반영을 단순 누적 방식에서 성장 계산 방식으로 변경했다.

기존:

`현재 Experience + 전투 Experience`
→ `CharacterSaveData.Experience` 저장

Day29:

`현재 Level / Experience`
→ `CharacterLevelProgression.Apply()`
→ 최종 Level / Experience 계산
→ `CharacterSaveData.SetLevel()`
→ `CharacterSaveData.SetExperience()`

따라서 기존 `SaveData`의 `CharacterSaveData.Level`과 `CharacterSaveData.Experience` 필드를 그대로 사용하면서 별도 저장 데이터 구조 변경 없이 레벨링 결과를 영구 진행 데이터에 반영한다.

기존 Battle 결과 중복 반영 차단 구조도 유지되므로 동일 ResultId가 다시 처리될 때 성장 보상이 중복 적용되지 않는 기존 계약을 유지한다.

---

## 던전 클리어 영구 저장

`DungeonProgressSaveAdapter`를 추가했다.

던전 클리어 진행도는 기존 `SaveData.StoryFlags`를 이용해 다음 형태로 저장한다.

`SYS_DUNGEON_CLEAR:DG001`

주요 기능:

- `IsCleared()` — 해당 던전 클리어 여부 조회
- `MarkCleared()` — 최초 클리어 기록
- 이미 클리어된 DungeonId의 중복 플래그 생성 차단
- 지원하지 않는 DungeonId의 진행 저장 차단

따라서 `SaveData`에 새로운 직렬화 필드를 추가하지 않고 기존 저장 구조를 이용해 던전 진행도를 유지한다.

---

## 던전 순차 해금 정책

`DungeonProgressionPolicy`를 추가했다.

Day29 임시 순차 해금 규칙:

| 던전 | 해금 조건 |
| --- | --- |
| `DG001` | 기본 해금 |
| `DG002` | `DG001` 클리어 |
| `DG003` | `DG002` 클리어 |
| `DG004` | `DG003` 클리어 |

던전 진행 상태는 세 종류로 구분한다.

- `Locked`
- `Available`
- `Cleared`

예를 들어 신규 저장에서는 `DG001`만 `Available`이며 `DG002`~`DG004`는 `Locked` 상태다.

`DG001` 승리 후:

`DG001 · CLEARED`
→ `DG002 · AVAILABLE`
→ `DG003 · LOCKED`
→ `DG004 · LOCKED`

형태로 진행된다.

---

## 전투 승리와 던전 클리어 연동

`BattleResultCommitService.CommitOnce()`에서 승리 결과 처리 시 기존 Gold / EXP 처리와 함께 `result.DungeonId`를 클리어 상태로 기록하도록 연결했다.

승리 흐름:

`BattleResultData`
→ 승리 확인
→ Gold 반영
→ EXP 및 레벨 성장 반영
→ `DungeonProgressSaveAdapter.MarkCleared(result.DungeonId)`
→ 전투 ResultId 반영 완료 기록
→ 기존 `BattleScreenController`에서 `SaveCurrent()`

패배 흐름에서는 전투 결과 기록만 유지하며 던전 클리어 플래그는 생성하지 않는다.

따라서 패배한 던전으로 인해 다음 던전이 잘못 해금되지 않는다.

---

## 던전 선택 잠금 검사

기존 `DungeonSelectionRuntimeState`에 해금 검사 함수를 연결할 수 있는 구조를 추가했다.

추가된 핵심 상태:

- `unlockEvaluator`
- `SetUnlockEvaluator()`
- `IsUnlockedForSelection()`

`TrySelect()`에서는:

지원 DungeonId 확인
→ DungeonData 존재 확인
→ 해금 상태 확인
→ 선택 저장

순서로 검증한다.

`CanEnter()`에서도 선택 이후 해금 상태를 다시 검사한다.

따라서 이미 선택된 던전이라도 해금 판정이 변경되면 전투 진입을 다시 차단할 수 있다.

해금 검사 함수가 설정되지 않은 기존 테스트 및 기존 호출 경로에서는 기존 동작을 유지하도록 호환성을 보존했다.

---

## 던전 진행 UI 런타임 연동

`DungeonProgressUiRuntimePatch`를 추가했다.

DungeonSelect 씬이 로드되면 런타임으로 설치되며 현재 저장 진행도를 읽어 기존 던전 카드와 전투 진입 버튼 상태를 갱신한다.

표시 상태:

- `LOCKED`
- `AVAILABLE`
- `CLEARED`

적용 대상:

- 던전 카드 버튼 활성화 여부
- 던전 카드 배경 상태
- 카드 DungeonId 상태 문구
- 전투 시작 버튼 활성화 여부
- 상세 패널 진행 상태 문구

기존 `DungeonSelectScreenController`와 DungeonSelect Scene / Prefab 자체는 직접 수정하지 않는다.

잠긴 던전은 카드 버튼이 비활성화되며, 해금된 던전과 클리어된 던전만 선택 가능하도록 연결한다.

---

## CS0104 Object 모호성 수정

Day29 적용 과정에서 `DungeonProgressUiRuntimePatch.cs`의 `Object.FindFirstObjectByType<T>()` 호출 3곳에서 다음 컴파일 오류가 확인됐다.

`CS0104: 'Object' is an ambiguous reference between 'UnityEngine.Object' and 'object'`

원인은 파일에서 `System`과 `UnityEngine` 네임스페이스를 함께 사용하면서 `Object` 이름을 단독으로 참조한 것이다.

최신 원격 커밋에서는 세 호출을 모두 다음처럼 명시적으로 수정했다.

`UnityEngine.Object.FindFirstObjectByType<T>()`

수정 대상:

- 기존 진행 UI 패치 중복 설치 검사
- `DungeonSelectScreenController` 최초 조회
- 화면 컨트롤러 재조회

현재 최신 원격 파일에는 모호한 `Object.FindFirstObjectByType` 호출이 남아 있지 않다.

---

## 테스트 추가

Day29에서는 4개의 EditMode 테스트 파일을 추가했다.

### CharacterLevelProgressionTests

검증 내용:

- 임시 선형 필요 EXP 공식
- 경험치 부족 시 레벨 유지
- 초과 EXP 이월
- 기존 EXP와 신규 EXP 합산
- 한 번에 여러 레벨 상승
- Lv.20 도달 시 잔여 EXP 처리
- 이미 Lv.20인 캐릭터의 추가 EXP 처리

### DungeonProgressionTests

검증 내용:

- 신규 저장에서 DG001만 기본 해금
- DG001 클리어 시 DG002 해금
- 동일 던전 중복 클리어 기록 차단
- 순차 클리어에 따른 다음 던전 해금
- 미지원 DungeonId 진행 저장 차단

### DungeonSelectionUnlockTests

검증 내용:

- 해금 검사에서 거부한 던전 선택 차단
- 해금된 던전 선택 허용
- 선택 이후 해금 상태가 바뀌었을 때 `CanEnter()` 재검사 차단

### BattleResultProgressionIntegrationTests

검증 내용:

- 승리한 DungeonId가 클리어 상태로 저장됨
- 승리 후 다음 던전이 해금됨
- 패배 시 현재 던전이 클리어되지 않음
- 패배 시 다음 던전 잠금 유지

---

## 생성 파일

- `Assets/ProjectH/Scripts/Battle/CharacterLevelProgression.cs`
- `Assets/ProjectH/Scripts/Battle/CharacterLevelProgression.cs.meta`
- `Assets/ProjectH/Scripts/Battle/DungeonProgressSaveAdapter.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonProgressSaveAdapter.cs.meta`
- `Assets/ProjectH/Scripts/Battle/DungeonProgressionPolicy.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonProgressionPolicy.cs.meta`
- `Assets/ProjectH/Scripts/UI/DungeonProgressUiRuntimePatch.cs`
- `Assets/ProjectH/Scripts/UI/DungeonProgressUiRuntimePatch.cs.meta`
- `Assets/ProjectH/Tests/EditMode/BattleResultProgressionIntegrationTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleResultProgressionIntegrationTests.cs.meta`
- `Assets/ProjectH/Tests/EditMode/CharacterLevelProgressionTests.cs`
- `Assets/ProjectH/Tests/EditMode/CharacterLevelProgressionTests.cs.meta`
- `Assets/ProjectH/Tests/EditMode/DungeonProgressionTests.cs`
- `Assets/ProjectH/Tests/EditMode/DungeonProgressionTests.cs.meta`
- `Assets/ProjectH/Tests/EditMode/DungeonSelectionUnlockTests.cs`
- `Assets/ProjectH/Tests/EditMode/DungeonSelectionUnlockTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleResultCommitService.cs`
- `Assets/ProjectH/Scripts/UI/DungeonSelectionRuntimeState.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab 변경

없음.

Day29의 던전 진행 UI는 `DungeonProgressUiRuntimePatch`가 DungeonSelect 씬 로드 시 기존 화면 객체를 조회하고 상태를 덮어쓰는 방식으로 동작한다.

레벨링과 던전 클리어 진행도도 기존 `SaveData`, `BattleResultCommitService`, `BattleScreenController.SaveCurrent()` 흐름을 재사용하므로 Scene / Prefab의 직렬화 구조를 직접 변경하지 않는다.

---

## 최신 커밋 검토 상태

기준 원격 커밋 `bed93413825a2dd31befbbc00fb82db7f6f1df93`은 Day28 커밋 `63200c19a10f8d330e9d7c15eb77152bb77495cd`보다 1개 커밋 앞서 있다.

Day29 변경 범위:

- 총 18개 파일
- 신규 파일 16개
- 수정 파일 2개
- 삭제 파일 0개
- 추가 705줄
- 삭제 8줄

최신 원격 코드에서 이전에 확인된 `DungeonProgressUiRuntimePatch.cs`의 CS0104 Object 모호성 3곳은 `UnityEngine.Object` 명시 방식으로 반영되어 있다.

GitHub에는 현재 해당 커밋에 연결된 CI Status와 GitHub Actions 실행 기록이 없다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일, EditMode Test Runner 전체 통과, 실제 Play Mode에서의 전체 진행 성공을 단정하지 않는다.

최신 커밋 diff와 주요 변경 파일을 기준으로 정적 검토했을 때 Day29 개발 일지 작성을 막아야 할 명확한 차단 문제는 확인되지 않았다.

---

## Day29 완료 범위

이번 일차에서 코드상 연결된 흐름:

`DungeonSelect`
→ 신규 저장에서는 DG001만 해금
→ Battle 진입
→ 승리 결과 생성
→ Gold 지급
→ EXP 성장 계산
→ 다중 레벨업 및 잔여 EXP 반영
→ CharacterSaveData Level / Experience 저장
→ 승리 DungeonId 클리어 기록
→ 다음 DungeonId 해금
→ SaveCurrent()
→ DungeonSelect 복귀
→ `CLEARED / AVAILABLE / LOCKED` UI 반영

패배 시:

`BattleResultData`
→ ResultId 기록
→ Gold / EXP 미지급
→ DungeonId 미클리어
→ 다음 던전 잠금 유지

---

## 다음 확인 항목

Unity Editor에서 실제 적용 후 아래 항목은 실행 환경에서 추가 확인이 필요하다.

1. 전체 스크립트 컴파일
2. Day29 EditMode 테스트 실행
3. DG001 승리 후 DG002 해금 확인
4. DG002 → DG003 → DG004 순차 해금 확인
5. 저장 후 게임 재실행 시 던전 클리어 상태 유지 확인
6. 전투 EXP 획득 후 캐릭터 Level / Experience 저장 확인
7. 한 번의 큰 EXP 획득으로 다중 레벨업 확인
8. Lv.20 도달 시 Experience가 0으로 처리되는지 확인
9. 패배 시 다음 던전이 해금되지 않는지 확인
