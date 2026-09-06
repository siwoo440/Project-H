# Project H — Phase 1 Day 25 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 25
- 기준 원격 커밋: `9d57581209d65253de5b0d25fe52e26976047884`
- 기준 원격 커밋 메시지: `25`
- 주제: 전투 보상 지급·중복 방지 및 영구 저장 시스템 구축

---

## 목표

24일차에서 구축한 전투 결과 화면과 기본 Gold·EXP 산출 결과를 실제 진행 데이터에 연결한다.

25일차에서는 승리 결과가 확정되는 순간 보상을 현재 SaveData에 반영하고 즉시 저장하며, 동일한 전투 결과가 다시 처리되어도 보상이 중복 지급되지 않도록 전투별 고유 결과 ID와 반영 기록을 추가했다.

핵심 방향:

- 전투 시작마다 고유 `ResultId`를 생성한다.
- 전투 종료 시 Day24의 `BattleResultData`에 동일한 `ResultId`를 포함한다.
- 승리 시 Gold 120을 실제 진행 데이터에 누적한다.
- 승리 시 전투 참가 캐릭터에게 EXP 80을 실제 저장 데이터에 누적한다.
- 패배 시 Gold와 EXP는 지급하지 않는다.
- 동일한 `ResultId`는 한 번만 반영한다.
- 결과 반영 직후 `SaveManager.SaveCurrent()`를 호출해 JSON 저장 파일에 기록한다.
- 기존 결과 Overlay와 `다음` 버튼의 던전 선택 복귀 흐름은 유지한다.

---

## BattleResultData 결과 ID 확장

기존 `BattleResultData`에 전투 결과를 고유하게 식별하기 위한 `ResultId`를 추가했다.

관리 정보:

- `ResultId`
- `Outcome`
- `Gold`
- `Experience`
- `StarCount`
- `Members`

기존 `BattleResultData.Create(outcome, battleMembers)` 호출은 내부에서 `Guid.NewGuid().ToString("N")`으로 새로운 결과 ID를 생성한다.

전투 화면처럼 이미 전투별 ID를 관리하는 곳에서는 `BattleResultData.Create(outcome, battleMembers, resultId)` 오버로드를 사용해 동일한 ID를 명시적으로 전달할 수 있도록 구성했다.

---

## BattleProgressSaveAdapter

현재 `SaveData`에는 별도의 Gold 필드와 전투 결과 처리 이력 전용 컬렉션이 없기 때문에 기존 저장 구조를 깨지 않고 Day25 기능을 연결하기 위한 호환 계층을 추가했다.

현재 사용하는 시스템 플래그 형식:

- Gold 저장: `SYS_BATTLE_GOLD:<누적값>`
- 결과 반영 기록: `SYS_BATTLE_RESULT:<ResultId>`

`GetGold()`는 저장된 Gold 시스템 플래그를 조회해 현재 누적 Gold를 반환한다.

`AddGold()`는 기존 Gold 플래그를 제거한 뒤 새 누적값을 하나의 시스템 플래그로 다시 저장한다. 합산 과정은 `long`으로 계산하고 최종값은 `int.MaxValue`를 넘지 않도록 보정한다.

`HasBattleResultCommit()`은 특정 `ResultId`가 이미 처리되었는지 확인한다.

`MarkBattleResultCommitted()`는 결과 처리가 끝난 `ResultId`를 시스템 플래그로 기록한다.

이 방식은 현재 저장 포맷을 확장하지 않고 Day25 보상을 영구 저장하기 위한 임시 호환 구조다. 추후 정식 재화 필드와 전투 진행 데이터가 `SaveData`에 추가되면 해당 전용 필드로 이전할 수 있다.

---

## BattleResultCommitService

전투 결과를 실제 저장 데이터에 한 번만 반영하는 `BattleResultCommitService`를 추가했다.

`CommitOnce()` 처리 흐름:

`SaveData / BattleResultData / ResultId 검증`

→ `동일 ResultId 반영 여부 확인`

→ `Victory이면 Gold 반영`

→ `Victory이면 참가 캐릭터 EXP 반영`

→ `ResultId 처리 완료 기록`

→ `반영 성공 반환`

이미 처리된 `ResultId`가 다시 전달되면 즉시 false를 반환해 같은 전투 보상이 중복 지급되지 않도록 한다.

패배 결과도 보상은 지급하지 않지만 결과 ID 자체는 처리 완료로 기록한다. 따라서 같은 패배 결과가 반복 호출되어도 다시 처리되지 않는다.

---

## Gold 실제 지급

Day24에서 표시용으로만 계산하던 승리 Gold 120을 Day25부터 실제 진행 데이터에 반영한다.

현재 흐름:

`BattleRewardCalculator Victory Gold 120`

→ `BattleResultData.Gold`

→ `BattleResultCommitService.CommitOnce()`

→ `BattleProgressSaveAdapter.AddGold()`

→ `SYS_BATTLE_GOLD:<누적값>` 저장

패배 결과는 Gold를 추가하지 않는다.

---

## 참가 캐릭터 EXP 실제 지급

승리 결과의 EXP 80을 전투 결과에 포함된 참가 캐릭터의 `CharacterSaveData.Experience`에 실제 누적한다.

처리 조건:

- 승리 결과일 것
- 결과 EXP가 0보다 클 것
- 결과 파티원 정보가 존재할 것
- `CharacterId`가 유효할 것
- 현재 SaveData에 보유 캐릭터로 존재할 것
- 동일 CharacterId가 결과 목록에서 중복 지급되지 않을 것

EXP 합산도 오버플로를 방지하기 위해 `long`으로 먼저 계산한 뒤 최대 `int.MaxValue`로 제한한다.

현재 Day25에서는 경험치 누적까지만 처리하며 레벨업 요구 경험치 곡선이나 자동 레벨 상승 규칙은 추가하지 않는다.

---

## 전투 결과 중복 지급 방지

전투 시작 시 `BattleScreenController.InitializeBattle()`에서 새로운 `currentBattleResultId`를 생성한다.

이 ID는 해당 전투가 끝날 때까지 유지된다.

전투 종료 시:

`currentBattleResultId`

→ `BattleResultData.ResultId`

→ `BattleResultCommitService.CommitOnce()`

→ `SYS_BATTLE_RESULT:<ResultId>` 기록

순서로 전달된다.

같은 종료 처리가 다시 호출되더라도 이미 같은 결과 ID가 저장되어 있으면 보상 반영을 중단한다.

---

## BattleScreenController 저장 연결

기존 `HandleBattleOutcome()`에 실제 보상 반영과 저장 단계를 추가했다.

현재 처리 흐름:

`Victory / Defeat 확정`

→ `전투 행동 정지`

→ `ResultId 보정`

→ `BattleResultData 생성`

→ `현재 SaveData 조회`

→ `BattleResultCommitService.CommitOnce()`

→ `반영 성공 시 SaveManager.SaveCurrent()`

→ `BattleResultOverlay 표시`

→ `다음 버튼`

→ `DungeonSelect 복귀`

보상은 결과 화면의 `다음` 버튼을 누를 때 지급하는 방식이 아니라 전투 결과가 확정되는 순간 바로 반영·저장된다.

따라서 결과 화면을 본 뒤 Scene이 변경되기 전에 저장 처리가 끝나도록 구성했다.

---

## 기존 결과 화면과의 연결

24일차의 `BattleResultOverlay` 구조는 수정하지 않는다.

결과 화면은 동일하게:

- 승패 결과
- 별 표시
- EXP 표시
- Gold 표시
- 파티 종료 상태
- 다음 버튼

을 보여 준다.

Day25에서 달라진 점은 화면에 표시되던 보상 수치가 이제 실제 SaveData에도 반영된다는 점이다.

---

## 테스트 추가

`BattleResultCommitServiceTests`를 추가해 Day25의 핵심 저장 반영 규칙을 검증하도록 구성했다.

현재 테스트 항목:

- 승리 결과 첫 반영 성공
- 동일 승리 결과 두 번째 반영 차단
- 승리 Gold 120 실제 누적 확인
- 참가 캐릭터 EXP 80 실제 누적 확인
- 결과 반영 기록 저장 확인
- 패배 Gold 0 확인
- 패배 EXP 0 확인
- 패배 결과 처리 완료 기록 확인
- 저장 데이터에 없는 참가 캐릭터 EXP 지급 제외
- 미보유 참가자가 있어도 계정 Gold 지급 유지
- 빈 ResultId 결과 반영 차단
- 빈 ResultId의 Gold·EXP 미지급 확인

현재 원격 커밋에는 별도의 CI Status가 등록되어 있지 않으므로 이 개발 일지에서는 Unity Test Runner 통과 여부를 단정하지 않는다.

---

## 생성 파일

- `Assets/ProjectH/Scripts/Battle/BattleProgressSaveAdapter.cs`
- `Assets/ProjectH/Scripts/Battle/BattleProgressSaveAdapter.cs.meta`
- `Assets/ProjectH/Scripts/Battle/BattleResultCommitService.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultCommitService.cs.meta`
- `Assets/ProjectH/Tests/EditMode/BattleResultCommitServiceTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleResultCommitServiceTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleResultData.cs`
- `Assets/ProjectH/Scripts/Battle/BattleScreenController.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab 작업

Day25의 보상 지급과 저장 연결은 기존 전투 코드 및 SaveData 구조를 이용하므로 별도의 Scene 또는 Prefab 수동 수정은 요구하지 않는다.

전투 종료 시 `BattleScreenController`가 Runtime에서 현재 저장 데이터를 조회하고 보상을 반영한 뒤 즉시 저장한다.

---

## 현재 임시 저장 구조

Day25 시점에는 `SaveData`에 정식 Gold 필드가 없기 때문에 Gold와 전투 결과 처리 이력은 `StoryFlags`에 시스템 전용 접두사를 사용해 저장한다.

현재 구분:

- 일반 스토리 플래그: 기존 문자열 플래그
- Day25 Gold: `SYS_BATTLE_GOLD:`
- Day25 결과 이력: `SYS_BATTLE_RESULT:`

일반 스토리 플래그와 충돌 가능성을 줄이기 위해 `SYS_BATTLE_` 전용 접두사를 사용한다.

향후 정식 재화·던전 진행 저장 구조를 추가하는 시점에는 이 임시 호환 계층을 정식 필드 기반 구조로 교체하는 것이 적합하다.

---

## Day25 완료 상태

이번 일차에서 연결된 플레이 흐름:

`전투 시작`

→ `전투별 ResultId 생성`

→ `전투 진행`

→ `Victory / Defeat 확정`

→ `결과 데이터 생성`

→ `중복 결과 여부 확인`

→ `승리 Gold 실제 지급`

→ `승리 참가 캐릭터 EXP 실제 지급`

→ `결과 처리 ID 기록`

→ `SaveCurrent 즉시 실행`

→ `결과 화면 표시`

→ `다음 버튼`

→ `DungeonSelect 복귀`

Day25에서 Day24의 표시용 보상 흐름이 실제 진행 데이터 저장까지 연결되었다.

---

## 다음 개발 방향

다음 단계에서는 임시 보상 저장 구조를 실제 게임 진행 시스템으로 확장할 수 있다.

예정 방향:

- `SaveData` 정식 Gold / 재화 필드 추가
- 경험치 요구량 데이터와 레벨업 처리
- 레벨업 시 능력치 성장 반영
- 던전 및 스테이지 클리어 기록 저장
- 스테이지별 보상 데이터 분리
- 최초 클리어와 반복 클리어 보상 구분
- 결과 화면에서 실제 누적 Gold 및 성장 결과 표시
