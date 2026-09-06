# Project H — Phase 1 Day 24 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 24
- 기준 원격 커밋: `f4fa2755cfcd5f3a3ce588fc67be15465977549c`
- 기준 원격 커밋 메시지: `24`
- 주제: 전투 결과 화면 및 기본 보상 산출 시스템 구축

---

## 목표

23일차까지 구축한 전투 조작과 자동 전투 흐름을 전투 종료 이후의 결과 확인 단계까지 연결한다.

24일차에서는 승리 또는 패배가 확정되면 현재 파티 상태를 전투 결과 데이터로 스냅샷하고, 기본 Gold·EXP 보상을 계산한 뒤 참고 이미지와 유사한 편성의 Runtime 결과 화면을 표시하도록 구성했다.

이번 단계에서는 결과와 보상을 화면에 산출·표시하는 데 집중하며, 실제 SaveData 지급과 저장 반영은 Day25로 분리한다.

핵심 방향:

- 기존 `BattleOutcome`의 Victory / Defeat 결과를 그대로 사용한다.
- 전투 종료 순간의 파티원 상태를 별도 결과 데이터로 복사한다.
- 승리 시 임시 기본 보상으로 Gold 120, EXP 80을 산출한다.
- 패배 및 비승리 상태에서는 Gold 0, EXP 0을 산출한다.
- 승리 결과는 별 3개, 패배 결과는 별 0개로 임시 표시한다.
- 최대 4명의 파티 캐릭터를 가로 카드 형태로 결과 화면에 표시한다.
- 각 카드에 이름, 레벨, 현재 HP / 최대 HP, 생존 또는 DOWN 상태를 표시한다.
- 전투 화면 위에 Runtime Overlay를 생성해 Scene/Prefab 추가 설정 없이 결과 UI를 표시한다.
- 결과 화면의 `다음` 버튼은 기존 던전 선택 복귀 흐름을 재사용한다.
- 실제 Gold·EXP 저장 반영은 이번 일차에서 처리하지 않는다.

---

## BattleResultData

전투 종료 시점의 결과를 UI와 이후 보상 지급 단계에서 공통으로 사용할 수 있도록 `BattleResultData`를 추가했다.

관리 정보:

- `Outcome`
- `Gold`
- `Experience`
- `StarCount`
- `Members`

`BattleResultData.Create()`는 현재 `BattlePartyRuntime.Members`를 입력받아 종료 시점 파티 상태를 별도의 스냅샷 목록으로 만든다.

따라서 결과 화면은 진행 중인 전투 객체를 직접 참조하지 않고 종료 순간에 확정된 결과 데이터를 기준으로 표시할 수 있다.

승리 시 `StarCount`는 현재 임시 규칙으로 3개를 사용하며, 패배 시 0개를 사용한다.

---

## BattleResultPartyMember

전투가 끝난 뒤 각 캐릭터의 상태가 추가 전투 처리에 의해 변하지 않도록 파티원별 결과 스냅샷을 추가했다.

저장 정보:

- `RuntimeId`
- `CharacterId`
- `DisplayName`
- `Level`
- `CurrentHp`
- `MaxHp`
- `IsAlive`
- `HealthRatio`

전투 종료 순간의 HP와 생존 상태를 그대로 복사하기 때문에 결과 화면에서 생존 캐릭터와 전투 불능 캐릭터를 구분해 표시할 수 있다.

`BattleResultPartyMember.Create()`에 null 전투 스탯이 전달되면 빈 결과를 생성하지 않고 null을 반환하도록 구성했다.

---

## 기본 보상 산출

`BattleRewardCalculator`에서 Day24용 임시 기본 보상을 계산한다.

현재 규칙:

- Victory: Gold `120`
- Victory: EXP `80`
- Defeat: Gold `0`
- Defeat: EXP `0`
- 그 외 비승리 상태: Gold `0`, EXP `0`

보상 수치는 `VictoryGold`, `VictoryExperience` 상수로 한 곳에 모아 두었다.

현재 단계의 보상은 결과 화면 표시용 산출값이며 SaveData, 인벤토리, 캐릭터 경험치에는 아직 반영하지 않는다.

실제 지급 및 저장 연결은 Day25에서 이 결과 데이터를 기반으로 확장할 예정이다.

---

## BattleResultOverlay

15일차의 단순 승패 Overlay를 24일차 결과 화면 구조로 확장했다.

화면은 별도 Scene 또는 Prefab을 요구하지 않고 `BattleResultOverlay.ShowRuntime()` 호출 시 Runtime으로 생성된다.

현재 주요 구성:

- 전체 화면 결과 배경
- 중앙 결과 패널
- `BATTLE RESULT` 보조 제목
- 승리 `WIN!` / 패배 `LOSE` 메인 제목
- 승리 별 `★ ★ ★` / 패배 빈 별 `☆ ☆ ☆`
- `EXP +값 / GOLD +값` 보상 요약 바
- `PARTY` 영역
- 최대 4개의 파티 결과 카드
- 우측 하단 `다음` 버튼

기존 전투 HUD 위에 표시되도록 Overlay Canvas의 Sorting Order를 높게 설정했다.

새 결과 Overlay를 다시 생성할 경우 기존 `ActiveOverlay`를 먼저 제거해 결과 화면이 중복으로 쌓이지 않도록 했다.

---

## 4인 파티 결과 카드

현재 프로젝트의 최대 파티 인원 `BattlePartyRuntime.MaxPartySize = 4`에 맞춰 결과 화면도 최대 4인까지만 표시한다.

각 캐릭터 카드 구성:

- 캐릭터 이름
- 임시 캐릭터 표시 영역
- 현재 레벨
- HP 게이지
- `HP 현재값 / 최대값`
- 생존 시 `ALIVE`
- 전투 불능 시 `DOWN`

생존 캐릭터는 녹색 계열 HP 표시를 사용하고, 전투 불능 캐릭터는 붉은 계열 상태 표시와 회색 임시 캐릭터 영역을 사용한다.

---

## 임시 캐릭터 표시 영역

현재 `CharacterData`에는 결과 화면에서 직접 사용할 캐릭터 Sprite 필드가 없기 때문에 Day24에서는 실제 스탠딩 이미지 대신 Runtime Placeholder를 사용한다.

현재 Placeholder는:

- 파티 슬롯별 서로 다른 임시 색상
- 전투 불능 시 회색 처리
- 캐릭터 이름의 첫 글자를 중앙에 표시

하는 방식으로 구성했다.

정식 캐릭터 Sprite 데이터가 추가되면 해당 Portrait 영역을 실제 이미지로 교체할 수 있도록 결과 카드 구조를 분리했다.

---

## BattleScreenController 결과 연결

기존 `BattleScreenController.HandleBattleOutcome()`의 승패 종료 흐름에 Day24 결과 데이터 생성을 연결했다.

처리 흐름:

`BattleOutcome Victory / Defeat`

→ `전투 진행 상태 종료`

→ `BattleTimeController Pause`

→ `기존 전투 UI 입력 잠금`

→ `BattleResultData.Create()`

→ `BattlePartyRuntime.Members` 종료 상태 스냅샷

→ `BattleResultOverlay.ShowRuntime()`

→ `결과 화면 표시`

기존 승패 시스템과 전투 행동 정지 구조를 유지하면서 결과 화면에 필요한 데이터만 새로 생성하도록 했다.

---

## 다음 버튼 및 복귀 흐름

결과 화면의 `다음` 버튼은 기존 `ReturnToDungeonSelect()`를 그대로 연결한다.

따라서 현재 흐름은 다음과 같다.

`전투 종료`

→ `결과 화면 확인`

→ `다음`

→ `전투 시간 배율 복원`

→ `DungeonSelect Scene 이동`

Day24에서는 버튼 클릭 시 보상을 실제 지급하지 않는다.

Day25에서 결과 보상 지급과 SaveData 저장 처리가 추가되면 결과 화면과 던전 복귀 사이에 실제 지급 단계를 연결할 수 있다.

---

## 테스트 추가

Day24 결과 데이터와 Runtime Overlay를 검증하기 위한 EditMode 테스트 코드를 추가했다.

`BattleResultDataTests`에서 확인하는 항목:

- 승리 시 임시 Gold 120 / EXP 80 계산
- 패배 시 Gold 0 / EXP 0 계산
- 4인 파티 결과 스냅샷 생성
- RuntimeId, 표시 이름, 레벨 보존
- 승리 별 3개 설정
- 전투 불능 캐릭터의 HP 0 / DOWN 상태 보존
- 생존 캐릭터의 종료 HP 보존
- 패배 별 0개 설정

`BattleResultOverlayTests`에서 확인하는 항목:

- 결과 데이터 기반 파티 카드 생성
- 생성 카드 수 확인
- 승리 제목 `WIN!` 확인
- 새로운 결과 Overlay 생성 시 기존 Overlay 교체
- 패배 제목 `LOSE` 확인

현재 저장소에는 이 테스트 코드가 포함되어 있으나, 원격 커밋에는 별도의 CI Status가 등록되어 있지 않으므로 이 개발 일지에서는 Unity Test Runner 통과 여부를 별도로 단정하지 않는다.

---

## 생성 파일

- `Assets/ProjectH/Scripts/Battle/BattleResultData.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultData.cs.meta`
- `Assets/ProjectH/Scripts/Battle/BattleResultPartyMember.cs`
- `Assets/ProjectH/Scripts/Battle/BattleResultPartyMember.cs.meta`
- `Assets/ProjectH/Scripts/Battle/BattleRewardCalculator.cs`
- `Assets/ProjectH/Scripts/Battle/BattleRewardCalculator.cs.meta`
- `Assets/ProjectH/Scripts/Battle/BattleReward.cs`
- `Assets/ProjectH/Scripts/Battle/BattleReward.cs.meta`
- `Assets/ProjectH/Tests/EditMode/BattleResultDataTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleResultDataTests.cs.meta`
- `Assets/ProjectH/Tests/EditMode/BattleResultOverlayTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleResultOverlayTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleResultOverlay.cs`
- `Assets/ProjectH/Scripts/Battle/BattleScreenController.cs`

---

## 삭제 파일

없음.

---

## Scene / Prefab 작업

Day24 결과 화면은 Runtime UI 생성 방식으로 구성했기 때문에 별도의 Scene 또는 Prefab 수동 수정은 요구하지 않는다.

현재 BattleScene의 승패 처리 코드가 `BattleResultData`와 `BattleResultOverlay`를 호출하며 결과 화면 전체를 Runtime으로 생성한다.

---

## Day24 완료 상태

이번 일차에서 연결된 플레이 흐름:

`전투 진행`

→ `Victory / Defeat 확정`

→ `남은 전투 행동 정지`

→ `결과 데이터 생성`

→ `기본 Gold / EXP 산출`

→ `4인 파티 종료 상태 스냅샷`

→ `WIN! / LOSE 결과 화면 표시`

→ `다음 버튼`

→ `DungeonSelect 복귀`

Day24의 범위는 전투 결과 표시와 기본 보상 산출까지이며 실제 보상 지급과 저장은 다음 일차 범위로 유지한다.

---

## 다음 개발 방향

Day25에서는 Day24에서 생성한 결과 데이터를 실제 진행 데이터에 연결한다.

예정 방향:

- 승리 보상 실제 지급
- Gold SaveData 반영
- EXP 지급 구조 연결
- 중복 지급 방지
- 결과 확정 후 저장
- 던전 선택 복귀와 지급 순서 정리
