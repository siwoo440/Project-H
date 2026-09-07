# Project H — Phase 1 Day 31 개발 일지

- 날짜: 2026-09-07
- 단계: Phase 1 / Day 31
- 개발 일지 추가 전 기준 커밋: `9c137f2e842d3217f79cd3877b04cc4caff35f98`
- 기준 커밋 메시지: `31`
- 이전 커밋: `912b5c649726c24b4f0e08f22a18df7677da0290`
- 주제: 성장 결과 UI 표시, 저장 회귀 검증, 중복 보상 방지 검증

---

## 목표

Day29에서 캐릭터 EXP와 레벨 저장을 연결하고 Day30에서 저장 레벨이 다음 전투의 Runtime Stats로 다시 계산되도록 정리했다.

Day31에서는 전투 종료 직후 실제로 적용된 성장 결과를 결과 화면에 표시하고, 저장 후 다시 불러온 상황에서도 레벨·EXP와 중복 보상 방지 기록이 유지되는지 검증 가능한 구조를 보강했다.

이번 일차의 핵심 방향:

- 전투 결과 반영 전 캐릭터 레벨과 EXP를 기록한다.
- 실제 레벨업 계산 결과를 결과 파티원 데이터에 연결한다.
- 결과 화면에 레벨 상승 전후와 획득 EXP를 표시한다.
- 단일 레벨업, 다중 레벨업, 최대 레벨 표시를 구분한다.
- 패배 시 성장 결과가 생성되지 않는 기존 규칙을 유지한다.
- 동일한 전투 결과 ID가 재처리되어도 EXP가 중복 지급되지 않는지 검증한다.
- SaveData JSON 직렬화·역직렬화 후에도 레벨, EXP, 전투 결과 처리 기록이 유지되는지 검증한다.

---

## 실제 성장 결과 기록

`BattleResultCommitService`에서 승리 보상을 반영할 때 캐릭터의 성장 적용 전 상태를 먼저 저장한다.

기록 항목:

- 성장 전 레벨
- 성장 전 EXP
- 이번 전투 획득 EXP
- 성장 후 레벨
- 성장 후 잔여 EXP
- 상승 레벨 수
- 최대 레벨 도달 여부

계산 흐름:

`CharacterSaveData`
→ 성장 전 Level / EXP 기록
→ `CharacterLevelProgression.Apply()`
→ SaveData의 Level / EXP 갱신
→ `BattleResultPartyMember.ApplyGrowthResult()`
→ 결과 화면용 성장 정보 보존

이 방식으로 전투 종료 시 생성된 기존 파티 스냅샷의 레벨만 표시하는 것이 아니라, 실제 저장 데이터에 적용된 성장 결과를 결과 화면에서 사용할 수 있게 했다.

---

## BattleResultPartyMember 성장 정보 확장

`BattleResultPartyMember`에 전투 보상 적용 결과를 보관하는 속성을 추가했다.

추가된 주요 정보:

- `HasGrowthResult`
- `GrowthStartLevel`
- `GrowthStartExperience`
- `GrowthEndLevel`
- `GrowthEndExperience`
- `GainedExperience`
- `LevelsGained`
- `ReachedMaxLevel`

기존 `Level`, HP, 생존 상태 등 전투 종료 스냅샷은 유지한다.

따라서 전투 중 사용한 레벨 정보와 전투 종료 후 저장에 반영된 성장 정보를 분리해 관리한다.

---

## 결과 화면 성장 표시

`BattleResultOverlay`의 파티원 카드에 성장 결과 표시를 추가했다.

레벨업이 발생한 경우 예시:

`Lv. 1 → Lv. 2`

`LEVEL UP! · EXP +80`

여러 레벨이 한 번에 상승한 경우:

`LEVEL UP ×2 · EXP +350`

레벨업 없이 EXP만 획득한 경우:

`EXP +80 · 120 / 150`

최대 레벨에 도달한 경우 레벨 문구에 `MAX`를 표시한다.

성장 정보가 없는 결과는 기존 방식대로 전투 스냅샷의 `Lv.` 표시만 사용한다.

---

## 전투 종료 처리 순서

현재 `BattleScreenController.HandleBattleOutcome()`의 처리 순서는 다음과 같다.

`BattleResultData.Create()`
→ 현재 SaveData 조회
→ `BattleResultCommitService.CommitOnce()`
→ 반영 성공 시 `SaveCurrent()`
→ `BattleResultOverlay.ShowRuntime()`

결과 Overlay가 생성되기 전에 실제 보상 반영이 먼저 수행되므로, Day31에서 `BattleResultPartyMember`에 연결한 성장 결과를 같은 전투의 결과 화면에서 바로 표시할 수 있다.

---

## 저장 회귀 검증

`BattleResultProgressionIntegrationTests`를 보강해 SaveData의 JSON 왕복 저장 상황을 검증하도록 구성했다.

검증 흐름:

`SaveData 생성`
→ 승리 결과 반영
→ Level / EXP 저장
→ `JsonUtility.ToJson()`
→ `JsonUtility.FromJson<SaveData>()`
→ `EnsureDefaults()`
→ 캐릭터 Level / EXP 재확인
→ 동일 ResultId 재반영 시도
→ 중복 지급 차단 확인

이 테스트는 현재 `SaveManager`에서 사용하는 Unity `JsonUtility` 기반 저장 방식과 동일한 직렬화 흐름을 사용한다.

---

## 중복 보상 방지 검증

동일한 `ResultId`를 가진 전투 결과를 두 번 반영하는 테스트를 추가했다.

첫 번째 처리:

`CommitOnce() == true`

두 번째 처리:

`CommitOnce() == false`

두 번째 처리 후에도 캐릭터 Level과 EXP가 첫 번째 처리 직후 값에서 변하지 않는지 확인하도록 구성했다.

또한 JSON 저장·불러오기 이후에도 `BattleProgressSaveAdapter`의 전투 결과 처리 기록이 유지되어 동일 결과의 재지급을 차단하는지 확인한다.

---

## 테스트 보강

### BattleResultProgressionIntegrationTests

기존 승리·패배 던전 진행 테스트에 다음 검증을 추가했다.

- 패배 시 성장 결과 미생성
- 승리 시 실제 적용된 성장 결과 기록
- 저장 캐릭터 최종 Level / EXP와 결과 성장 데이터 일치
- 동일 ResultId의 EXP 중복 지급 차단
- JSON 저장·불러오기 후 Level / EXP 유지
- JSON 저장·불러오기 후 ResultId 중복 처리 방지 유지

### BattleResultOverlayTests

성장 결과가 반영된 승리 결과를 Overlay에 표시했을 때 다음 UI 요소를 확인하는 테스트를 추가했다.

- 파티원 카드 생성
- `Level` Text 존재
- `Growth` Text 존재
- 레벨 상승 전후 문구
- 획득 EXP 및 LEVEL UP 문구

---

## 생성 파일

없음.

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleResultCommitService.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultOverlay.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultPartyMember.cs`
- `Assets/ProjectH/Tests/EditMode/BattleResultOverlayTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleResultProgressionIntegrationTests.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab 변경

없음.

이번 일차는 기존 Runtime 결과 화면과 저장/성장 데이터 흐름을 코드에서 연결하는 작업이므로 Scene 또는 Prefab 파일을 수정하지 않는다.

---

## 최신 커밋 변경 규모

Day30 커밋 `912b5c649726c24b4f0e08f22a18df7677da0290`과 Day31 구현 커밋 `9c137f2e842d3217f79cd3877b04cc4caff35f98` 비교:

- 총 5개 파일 수정
- 신규 파일 0개
- 삭제 파일 0개
- 추가 182줄
- 삭제 8줄
- Day30 대비 1개 커밋 앞섬

---

## 최신 커밋 검토 상태

Day31 구현 커밋의 diff와 현재 전투 종료 처리 순서를 기준으로 정적 검토했다.

확인한 사항:

- 실제 성장 계산 후 `BattleResultPartyMember`에 성장 전후 값이 연결된다.
- `BattleScreenController`는 결과 Overlay 표시 전에 `CommitOnce()`를 수행한다.
- 결과 화면은 실제 반영된 성장 결과가 있을 때만 `Growth` 표시를 추가한다.
- 패배 결과에는 성장 결과가 생성되지 않는다.
- 동일 ResultId의 중복 EXP 지급을 차단하는 테스트가 포함되어 있다.
- JSON 직렬화·역직렬화 후 Level / EXP와 결과 처리 기록을 재확인하는 테스트가 포함되어 있다.
- Scene / Prefab 변경은 없다.

GitHub Commit Status와 GitHub Actions workflow run은 현재 연결된 기록이 없다.

따라서 이 개발 일지에서는 Unity Editor 전체 컴파일 또는 EditMode Test Runner 전체 통과를 단정하지 않는다.

최신 커밋의 변경 내용과 호출 순서를 기준으로 정적 검토했을 때, 개발 일지 작성을 중단해야 할 명확한 차단 문제는 확인되지 않았다.

---

## Day31 완료 범위

현재 성장 결과 흐름:

`Battle 종료`
→ 결과 데이터 생성
→ 저장 캐릭터 성장 전 Level / EXP 기록
→ 승리 EXP 계산
→ CharacterSaveData Level / EXP 변경
→ 성장 결과를 BattleResultPartyMember에 연결
→ 즉시 SaveCurrent
→ 결과 Overlay 생성
→ 레벨 상승 전후 / EXP / MAX 표시

Day31에서는 성장 결과 UI와 저장 회귀 검증을 정리했다.

장비 데이터와 장비 슬롯 구조는 다음 일차인 Day32 범위로 넘긴다.
