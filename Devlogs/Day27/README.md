# Project H — Phase 1 Day 27 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 27
- 기준 원격 커밋: `cfa389b90597b38c32f7069642a379c117fba649`
- 기준 원격 커밋 메시지: `27`
- 주제: 던전별 전투 차등값·보상 연동 및 타이틀 화면 비주얼 개선

---

## 목표

26일차에서 `DungeonSelect`를 통해 `DG001`~`DG004` 중 하나를 선택하고 `Battle` 씬으로 진입하는 흐름을 구축한 뒤, 27일차에서는 선택한 던전이 실제 전투 수치와 승리 보상에 영향을 주도록 연결했다.

동시에 반복 테스트 시 각 던전의 차이를 빠르게 확인할 수 있도록 전투 디버그 오버레이를 추가하고, Title 화면의 배경과 UI 배치를 현재 프로젝트 방향에 맞게 정리했다.

이번 일차의 핵심 방향:

- `DG001`~`DG004`에 서로 다른 적 능력치 배율을 적용한다.
- 선택한 던전의 `DungeonData` 보상을 실제 승리 보상 계산에 사용한다.
- 던전별 전투 차이를 화면에서 즉시 확인할 수 있는 테스트 오버레이를 추가한다.
- 기존 몬스터 표시 이름은 변경하지 않아 기존 전투 테스트와 UI 의미를 유지한다.
- Title 화면에 신규 배경 이미지를 적용한다.
- Title의 메뉴 패널과 저장 상태 문구를 숨기고 버튼 크기를 정리한다.
- 게임 제목과 부제를 좌측 상단에 자연스럽게 배치한다.
- Scene / Prefab 직접 수정 없이 런타임 패치 방식으로 Title 화면을 적용한다.

---

## 던전별 전투 테스트 프로필

`DungeonBattleTestProfile`을 추가해 선택된 던전에 따라 적의 주요 능력치에 서로 다른 배율을 적용하도록 구성했다.

| 던전 | HP | ATK | DEF | RES |
| --- | ---: | ---: | ---: | ---: |
| `DG001` | x1.00 | x1.00 | x1.00 | x1.00 |
| `DG002` | x1.50 | x1.25 | x1.20 | x1.10 |
| `DG003` | x2.25 | x1.60 | x1.50 | x1.35 |
| `DG004` | x3.50 | x2.10 | x1.90 | x1.70 |

`DungeonBattleTestProfile.Get()`은 현재 선택된 던전 ID에 맞는 프로필을 반환한다.

직접 `Battle` 씬을 실행하거나 선택된 던전이 없는 경우에는 `DG001` 프로필을 기본값으로 사용한다.

---

## 적 전투 스탯에 던전 배율 적용

`BattleEnemyStatsFactory.Create()`가 `DungeonSelectionRuntimeState.SelectedDungeonId`를 확인하고 현재 던전의 테스트 프로필을 조회하도록 변경했다.

배율 적용 대상:

- 최대 HP
- 공격력
- 방어력
- 저항력

적 생성 흐름:

`MonsterData`

→ `현재 선택 DungeonId 확인`

→ `DungeonBattleTestProfile 조회`

→ `HP / ATK / DEF / RES 배율 계산`

→ `BattleEnemyStats 생성`

계산 결과는 최소값 보정을 거쳐 런타임 스탯에 적용한다.

공격 속도, 공격 사거리, 이동 속도, AI 유형 등 이번 일차 범위에 포함되지 않은 값은 기존 `MonsterData` 값을 그대로 사용한다.

---

## 몬스터 표시 이름 유지

초기 Day27 작업에서는 테스트 구분을 위해 몬스터 이름 뒤에 `[DG001]` 같은 던전 ID를 붙이는 방식이 사용되었지만, 기존 `BattleEnemyStatsFactoryTests`가 원본 `MonsterData.DisplayName` 유지 여부를 검증하고 있어 이를 제거했다.

현재 구조에서는:

`침식된 늑대`

→ 던전이 달라져도 표시 이름은 `침식된 늑대` 그대로 유지

던전별 차이는 몬스터 이름을 변형하지 않고 별도의 전투 디버그 오버레이에서 확인한다.

이로써 기존 표시 이름 계약을 유지하면서 던전별 전투 수치 차등 적용을 함께 사용할 수 있다.

---

## 던전별 실제 승리 보상 연동

기존 `BattleRewardCalculator`는 승리 시 고정값을 사용했다.

기본값:

- Gold: `120`
- EXP: `80`

Day27에서는 선택된 던전이 존재하고 데이터 시스템이 초기화되어 있으면 `DungeonData`에서 실제 보상값을 조회하도록 변경했다.

| 던전 | Gold | EXP |
| --- | ---: | ---: |
| `DG001` | 120 | 80 |
| `DG002` | 180 | 120 |
| `DG003` | 240 | 160 |
| `DG004` | 320 | 220 |

승리 보상 흐름:

`BattleOutcome.Victory`

→ `DungeonSelectionRuntimeState.SelectedDungeonId`

→ `GameManager.Instance.Data.GetDungeon()`

→ `DungeonData.RewardGold / RewardExp`

→ `BattleReward`

패배 또는 비종료 상태에서는 기존과 동일하게 보상 `0 / 0`을 반환한다.

직접 Battle 씬을 실행했거나 던전 데이터를 조회할 수 없는 경우에는 기존 기본 승리 보상 `120 Gold / 80 EXP`로 fallback한다.

---

## 전투 디버그 오버레이

`DungeonBattleDebugOverlay`를 추가했다.

`BeforeSceneLoad`에서 `SceneManager.sceneLoaded` 이벤트를 구독하고, `Battle` 씬이 로드되면 기존 오버레이 존재 여부를 확인한 뒤 런타임 오브젝트를 생성한다.

화면에는 다음 정보를 표시한다.

- 현재 선택된 던전 ID
- 던전 이름
- 적 HP 배율
- 적 공격력 배율
- 적 방어력 배율
- 적 저항력 배율
- 승리 Gold
- 승리 EXP

직접 Battle 씬을 실행한 경우에는 `DIRECT→DG001`로 표시해 던전 선택 흐름을 거치지 않았음을 구분한다.

이 오버레이는 Day27의 던전별 차등값을 빠르게 검증하기 위한 테스트 보조 UI다.

---

## Title 배경 리소스 추가

Title 화면용 배경 이미지가 다음 경로에 추가되었다.

`Assets/ProjectH/Resources/UI/TitleBackground.png`

`TitleScreenVisualPatch`는 `Resources.Load<Texture2D>("UI/TitleBackground")`로 이미지를 읽고 런타임 Sprite를 만들어 기존 `Background` Image에 적용한다.

배경은 화면 전체를 채우도록 설정하며 기존 Title 버튼 입력을 방해하지 않도록 Raycast 대상에서 제외한다.

---

## Title 화면 비주얼 정리

`TitleScreenVisualPatch`는 Title 씬 진입 시 기존 UI 오브젝트를 찾아 런타임에서 다음 변경을 적용한다.

### 메뉴 패널 제거

`TitleMenuPanel`의 Image와 Shadow 효과를 비활성화한다.

버튼 자체는 유지하며 기존 New Game / Continue / Quit 기능에는 영향을 주지 않는다.

### 저장 상태 문구 숨김

`Status` 오브젝트를 비활성화해 Title 화면의 저장 상태 안내 문구를 표시하지 않는다.

저장 데이터 존재 여부에 따른 이어하기 버튼 활성화 로직은 기존 `TitleScreenController`가 계속 담당한다.

### 게임 종료 버튼 크기 통일

`QuitButton`의 `RectTransform`과 사용 가능한 `LayoutElement` 값을 `NewGameButton` 기준으로 맞춘다.

따라서 게임 종료 버튼이 새 게임 버튼과 동일한 크기로 표시된다.

### 제목과 부제 재배치

`GameTitle`과 `Subtitle`을 좌측 상단 기준으로 재배치했다.

기준 해상도 `1920x1080`에서 사용하는 런타임 위치:

- `GameTitle`: `(120, -90)`
- `Subtitle`: `(124, -210)`

두 텍스트는 좌측 정렬을 사용한다.

배경 이미지의 주요 피사체와 우측 메뉴 버튼 영역을 피하면서 제목 정보를 좌측 상단에서 자연스럽게 읽을 수 있도록 배치했다.

---

## Scene / Prefab 비파괴 적용

Day27 커밋에는 Scene 또는 Prefab 파일 변경이 포함되어 있지 않다.

Title 관련 변경은 `TitleScreenVisualPatch`가 씬 로드 이후 다음 기존 오브젝트를 이름으로 찾아 적용한다.

- `Background`
- `TitleMenuPanel`
- `Status`
- `GameTitle`
- `Subtitle`
- `NewGameButton`
- `QuitButton`

따라서 기존 `Title.unity`의 버튼 이벤트와 `TitleScreenController` 연결을 직접 수정하지 않는다.

---

## 테스트 추가

`DungeonBattleTestProfileTests`를 추가했다.

### Day27Profiles_HaveDistinctEnemyValues

`DungeonSelectionRuntimeState.SupportedDungeonIds`를 순회하며 각 던전의:

- HP 배율
- ATK 배율
- DEF 배율
- RES 배율

조합이 모두 서로 다른지 검증한다.

### Day27DungeonAssets_HaveDistinctVisibleTestValues

`DG001`~`DG004`의 실제 `DungeonData` 에셋을 로드하고 다음 값 조합이 모두 서로 다른지 검증한다.

- 권장 레벨
- Gold 보상
- EXP 보상

또한 기존 `BattleEnemyStatsFactoryTests`가 기대하는 원본 몬스터 표시 이름을 유지하도록 구현을 정리했다.

---

## 생성 파일

- `Assets/ProjectH/Resources.meta`
- `Assets/ProjectH/Resources/UI.meta`
- `Assets/ProjectH/Resources/UI/TitleBackground.png`
- `Assets/ProjectH/Resources/UI/TitleBackground.png.meta`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleDebugOverlay.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleDebugOverlay.cs.meta`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleTestProfile.cs`
- `Assets/ProjectH/Scripts/Battle/DungeonBattleTestProfile.cs.meta`
- `Assets/ProjectH/Scripts/UI/TitleScreenVisualPatch.cs`
- `Assets/ProjectH/Scripts/UI/TitleScreenVisualPatch.cs.meta`
- `Assets/ProjectH/Tests/EditMode/DungeonBattleTestProfileTests.cs`
- `Assets/ProjectH/Tests/EditMode/DungeonBattleTestProfileTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Scripts/Battle/BattleEnemyStats.cs`
- `Assets/ProjectH/Scripts/Battle/BattleRewardCalculator.cs`

---

## 삭제 파일

없음.

---

## 현재 검토 상태

기준 원격 커밋 `cfa389b90597b38c32f7069642a379c117fba649`을 Day26 커밋 `3d68247a24cfcb17963ecce9a09b4d62ecbc50f4`와 비교한 결과, Day27은 1개 커밋으로 앞서 있으며 변경 파일은 던전 전투 차등값, 보상 연동, 테스트 오버레이, Title 비주얼 리소스와 런타임 패치에 집중되어 있다.

정적 검토에서는 이전 테스트 실패 원인이었던 몬스터 표시 이름 변형이 제거되어 `BattleEnemyStatsFactory.Create()`가 다시 원본 `monsterData.DisplayName`을 사용하고 있는 것을 확인했다.

`BattleRewardCalculator`는 선택된 던전 데이터를 조회할 수 있을 때 해당 보상을 사용하고, 직접 Battle 실행 또는 데이터 조회 불가 상황에서는 기존 기본값으로 fallback한다.

Title 변경은 Scene / Prefab을 직접 변경하지 않고 기존 오브젝트에 런타임으로 적용되는 구조다.

현재 원격 커밋에는 GitHub CI Status가 등록되어 있지 않다. 따라서 이 개발 일지에서는 Unity Editor 컴파일, EditMode Test Runner 전체 통과, 실제 Play Mode 실행 성공을 단정하지 않는다.

---

## Day27 완료 범위

이번 일차에서 코드상 연결된 흐름:

`DungeonSelect`

→ `DG001 ~ DG004 선택`

→ `Battle 진입`

→ `선택 DungeonId 기반 테스트 프로필 조회`

→ `적 HP / ATK / DEF / RES 차등 적용`

→ `전투 디버그 오버레이 표시`

→ `승리`

→ `선택 DungeonData의 Gold / EXP 조회`

→ `BattleReward 생성`

추가 Title 흐름:

`Title 씬 로드`

→ `신규 배경 적용`

→ `메뉴 패널 및 상태 문구 숨김`

→ `제목 / 부제 좌측 상단 배치`

→ `Quit 버튼 크기 통일`

Day27에서는 선택한 던전이 실제 전투 수치와 보상에 영향을 주는 구조를 연결하고, 반복 테스트에 필요한 시각적 구분과 Title 화면 비주얼 정리를 함께 완료했다.

---

## 다음 개발 방향

다음 단계에서는 현재 테스트용 배율 구조를 실제 콘텐츠 구성으로 확장하는 것이 적절하다.

예정 방향:

- 던전별 실제 적 편성 데이터 구축
- 선택된 던전 정보를 독립적인 Battle Context로 확정
- 던전별 적 종류와 출현 수 차등 적용
- 전투 결과에 DungeonId 기록
- 던전 클리어 기록 및 해금 조건 저장
- 최초 클리어 / 반복 클리어 보상 분리
- 현재 테스트용 전투 오버레이의 개발 환경 전용 처리 검토
- DungeonSelect → Battle → Result까지 PlayMode 회귀 테스트 추가
