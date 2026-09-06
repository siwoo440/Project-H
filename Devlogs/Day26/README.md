# Project H — Phase 1 Day 26 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 26
- 기준 원격 커밋: `f46751ca0169227fd5360895ef20eaabd56dc591`
- 기준 원격 커밋 메시지: `26`
- 주제: 던전 선택 UI·데이터 연결 및 전투 진입 시스템 구축

---

## 목표

25일차에서 전투 결과 보상을 실제 저장 데이터에 반영하는 흐름까지 연결한 뒤, 26일차에서는 전투에 진입하기 전 플레이어가 출격할 던전을 선택할 수 있는 화면과 런타임 선택 상태를 구축했다.

이번 일차의 핵심 방향:

- Day26에서 사용할 던전 데이터 4종을 실제 `DungeonData` 에셋으로 추가한다.
- 추가한 던전을 `ProjectHDataCatalog`에 등록한다.
- 지원 던전 목록과 현재 선택 상태를 `DungeonSelectionRuntimeState`에서 관리한다.
- `DungeonSelect` 씬 진입 시 런타임 UI를 자동으로 구성한다.
- 던전 카드 선택 결과를 상세 정보 패널과 전투 시작 버튼 상태에 즉시 반영한다.
- 유효한 던전이 선택된 경우에만 `Battle` 씬으로 진입하도록 제한한다.
- 로비 복귀와 전투 진입을 기존 `SceneLoader` 흐름에 연결한다.
- 일반적인 `Bootstrap → Title → Lobby → DungeonSelect` 전환에서도 화면 컨트롤러가 설치되도록 씬 로드 이벤트 기반 초기화를 사용한다.

---

## 던전 데이터 4종 추가

Day26에서 선택 가능한 던전으로 `DG001`부터 `DG004`까지 4개의 `DungeonData` 에셋을 추가했다.

| ID | 이름 | 지역 | 권장 레벨 | Gold | EXP |
| --- | --- | --- | ---: | ---: | ---: |
| `DG001` | 무너진 성역의 숲 | `REG_LETICIA` | 1 | 120 | 80 |
| `DG002` | 성역 외곽 폐허 | `REG_LETICIA` | 3 | 180 | 120 |
| `DG003` | 침식된 회랑 | `REG_BORDER` | 5 | 240 | 160 |
| `DG004` | 심연의 관문 | `REG_ABYSS` | 8 | 320 | 220 |

각 에셋은 다음 정보를 가진다.

- 던전 ID
- 표시 이름
- 지역 ID
- 권장 레벨
- 예상 Gold 보상
- 예상 EXP 보상

---

## ProjectHDataCatalog 등록

추가한 `DG001`~`DG004`를 `Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset`의 던전 목록에 등록했다.

따라서 런타임 화면은 별도의 하드코딩된 표시 데이터가 아니라 `GameManager.Instance.Data`를 통해 실제 등록된 `DungeonData`를 조회한다.

지원 ID가 목록에 존재하더라도 실제 데이터가 없으면 해당 던전 카드는 선택할 수 없도록 구성되어 있다.

---

## DungeonSelectionRuntimeState

던전 선택 화면과 전투 진입 사이에서 현재 선택 상태를 유지하기 위한 `DungeonSelectionRuntimeState`를 추가했다.

지원 던전 ID:

- `DG001`
- `DG002`
- `DG003`
- `DG004`

주요 기능:

- `SupportedDungeonIds`: Day26 지원 던전 목록 제공
- `SelectedDungeonId`: 현재 선택된 던전 ID 제공
- `TrySelect()`: 지원 여부와 실제 데이터 존재 여부를 확인한 뒤 선택 저장
- `CanEnter()`: 현재 선택 상태로 전투 진입이 가능한지 검증
- `IsSupportedDungeonId()`: Day26 지원 던전 ID 여부 확인
- `Clear()`: 현재 선택 해제
- `ResetAll()`: 테스트 및 상태 초기화
- `SelectionChanged`: 선택 변경 시 UI에 전달되는 이벤트

플레이 세션이 새로 시작될 때 `SubsystemRegistration` 단계에서 선택 ID와 이벤트 구독 상태를 초기화한다.

---

## DungeonSelectScreenController

`DungeonSelectScreenController`를 추가해 `DungeonSelect` 씬의 화면을 런타임에서 구성하도록 했다.

화면 구성:

- 전체 배경
- 상단 `SELECT DUNGEON` 헤더
- 로비 복귀 버튼
- 던전 카드 목록 패널
- 던전 상세 정보 패널
- 전투 시작 버튼

런타임 Canvas는 `1920x1080`을 기준 해상도로 사용하고 화면 크기에 맞춰 스케일되도록 구성했다.

---

## 씬 전환 이후 런타임 컨트롤러 설치

던전 선택 화면은 프로젝트의 일반적인 시작 흐름에서도 생성되어야 한다.

현재 초기화 구조:

`BeforeSceneLoad`

→ `SceneManager.sceneLoaded` 이벤트 구독

→ 씬 로드 완료마다 씬 이름 확인

→ `DungeonSelect`이면 기존 컨트롤러 존재 여부 확인

→ 없을 때만 `DungeonSelectScreenRuntime` 생성

이 방식으로 최초 실행 씬이 `DungeonSelect`가 아니더라도 이후 `Bootstrap → Title → Lobby → DungeonSelect`처럼 씬을 이동했을 때 컨트롤러를 설치할 수 있다.

이벤트 등록 시 기존 핸들러를 먼저 제거한 뒤 다시 등록해 중복 구독 가능성을 줄였으며, 컨트롤러 생성 전에도 `FindFirstObjectByType<DungeonSelectScreenController>()`로 중복 생성을 방지한다.

---

## 던전 카드 선택

지원 던전 ID를 순회해 던전 카드 버튼을 생성한다.

각 카드에는 다음 정보가 표시된다.

- 던전 ID
- 던전 이름
- 권장 레벨

실제 `DungeonData`를 찾을 수 없는 경우에는 해당 카드를 비활성화하고 데이터 누락 상태를 표시한다.

유효한 카드를 누르면 `DungeonSelectionRuntimeState.TrySelect()`를 통해 선택을 저장하며, 선택 변경 결과에 따라 카드 색상과 외곽선을 갱신한다.

---

## 상세 정보 패널

선택된 던전이 없을 때는 기본 안내 상태를 표시한다.

유효한 던전을 선택하면 실제 `DungeonData`를 기반으로 다음 정보를 표시한다.

- 던전 이름
- 지역 ID
- 권장 레벨
- 예상 EXP
- 예상 Gold
- 출격 준비 상태

선택 정보와 실제 데이터가 모두 유효할 때만 전투 시작 버튼을 활성화한다.

---

## 전투 진입

전투 시작 버튼을 눌렀을 때 다시 `DungeonSelectionRuntimeState.CanEnter()`로 선택 상태를 검증한다.

전투 진입 조건:

- 선택된 던전 ID가 존재할 것
- Day26 지원 던전일 것
- 실제 `DungeonData`가 존재할 것
- `GameManager`와 `SceneLoader`가 준비되어 있을 것

조건을 만족하면 다음 흐름으로 이동한다.

`DungeonSelect`

→ `던전 선택`

→ `전투 시작 버튼 활성화`

→ `GameManager.Instance.Scenes.LoadScene(GameScenes.Battle)`

→ `Battle`

---

## 로비 복귀

상단의 로비 버튼은 기존 씬 로더를 이용해 `Lobby` 씬으로 복귀한다.

`GameManager` 또는 `SceneLoader`가 준비되지 않은 상태에서는 씬 이동을 시도하지 않고 상세 상태 텍스트에 오류를 표시한다.

---

## 테스트 추가

Day26 기능을 검증하기 위한 EditMode 테스트 2종을 추가했다.

### DungeonSelectionRuntimeStateTests

검증 항목:

- 지원 던전 ID가 `DG001`~`DG004` 순서인지 확인
- 초기 상태에서 선택이 비어 있고 전투 진입이 차단되는지 확인
- 유효한 지원 던전 선택 저장 확인
- 미지원 던전 선택 차단 확인
- 실제 데이터가 없는 던전 선택 차단 확인
- 다른 던전 선택 시 현재 선택 교체 확인
- 선택 후 데이터가 사라진 경우 전투 진입 차단 확인

### DungeonSelectionDataAssetTests

검증 항목:

- `DG001`~`DG004` 에셋 존재 확인
- 각 에셋의 ID 일치 확인
- 표시 이름 존재 확인
- 권장 레벨이 0보다 큰지 확인
- `ProjectHDataCatalog`에 4개 던전이 모두 등록되어 있는지 확인

현재 원격 커밋에는 GitHub CI Status가 등록되어 있지 않다. 따라서 이 개발 일지에서는 Unity Test Runner와 실제 Play Mode 실행 성공을 단정하지 않는다.

---

## 생성 파일

- `Assets/ProjectH/Data/Dungeons/DG001.asset`
- `Assets/ProjectH/Data/Dungeons/DG001.asset.meta`
- `Assets/ProjectH/Data/Dungeons/DG002.asset`
- `Assets/ProjectH/Data/Dungeons/DG002.asset.meta`
- `Assets/ProjectH/Data/Dungeons/DG003.asset`
- `Assets/ProjectH/Data/Dungeons/DG003.asset.meta`
- `Assets/ProjectH/Data/Dungeons/DG004.asset`
- `Assets/ProjectH/Data/Dungeons/DG004.asset.meta`
- `Assets/ProjectH/Scripts/UI/DungeonSelectScreenController.cs`
- `Assets/ProjectH/Scripts/UI/DungeonSelectScreenController.cs.meta`
- `Assets/ProjectH/Scripts/UI/DungeonSelectionRuntimeState.cs`
- `Assets/ProjectH/Scripts/UI/DungeonSelectionRuntimeState.cs.meta`
- `Assets/ProjectH/Tests/EditMode/DungeonSelectionDataAssetTests.cs`
- `Assets/ProjectH/Tests/EditMode/DungeonSelectionDataAssetTests.cs.meta`
- `Assets/ProjectH/Tests/EditMode/DungeonSelectionRuntimeStateTests.cs`
- `Assets/ProjectH/Tests/EditMode/DungeonSelectionRuntimeStateTests.cs.meta`

---

## 수정 파일

- `Assets/ProjectH/Data/Database/ProjectHDataCatalog.asset`

---

## 삭제 파일

없음.

---

## Scene / Prefab 작업

Day26 커밋에는 Scene 또는 Prefab 파일 변경이 포함되어 있지 않다.

던전 선택 화면은 `DungeonSelectScreenController`가 런타임에 Canvas와 UI 오브젝트를 생성하는 방식이며, 씬 로드 이벤트를 통해 `DungeonSelect` 진입 시 컨트롤러를 자동 설치한다.

---

## 현재 검토 상태

기준 원격 커밋 `f46751ca0169227fd5360895ef20eaabd56dc591`을 이전 Day25 커밋과 비교한 결과 Day26 변경은 던전 데이터, 데이터 카탈로그, 던전 선택 상태, 런타임 UI, EditMode 테스트에 집중되어 있다.

정적 코드 검토에서는 이전에 문제가 되었던 `AfterSceneLoad` 1회 초기화 방식 대신 `BeforeSceneLoad`에서 `SceneManager.sceneLoaded`를 구독하는 구조가 반영된 것을 확인했다.

다만 원격 커밋에 CI Status가 없고 현재 검토 환경에서는 Unity Editor의 컴파일, EditMode Test Runner, 실제 `Bootstrap → Title → Lobby → DungeonSelect → Battle` 플레이 흐름을 실행하지 않았으므로 런타임 성공 여부는 별도 Unity 검증 대상이다.

---

## Day26 완료 범위

이번 일차에서 코드상 연결된 흐름:

`Bootstrap / Title / Lobby`

→ `DungeonSelect 씬 로드`

→ `DungeonSelectScreenController 자동 설치`

→ `던전 카드 4종 구성`

→ `실제 DungeonData 조회`

→ `던전 선택`

→ `상세 정보 갱신`

→ `전투 시작 버튼 활성화`

→ `Battle 씬 이동`

Day26에서는 전투에 진입하기 전 필요한 던전 선택 데이터와 선택 UI, 유효성 검증, 씬 이동 연결까지 구현 범위에 포함되었다.

---

## 다음 개발 방향

다음 단계에서는 선택된 던전 정보를 실제 전투 구성과 진행 데이터에 더 깊게 연결할 수 있다.

예정 방향:

- 선택된 던전 ID를 Battle 초기화 데이터에 전달
- 던전별 적 편성 및 난이도 데이터 연결
- 던전별 실제 보상값을 전투 결과 산출에 연결
- 던전 클리어 기록과 해금 조건 저장
- 최초 클리어 / 반복 클리어 보상 분리
- 던전 선택 화면의 실제 아트 리소스와 연출 적용
- PlayMode 기반 DungeonSelect 진입 회귀 테스트 추가
