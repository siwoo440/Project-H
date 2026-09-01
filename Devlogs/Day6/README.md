# Project H — Phase 0 Day 6 검수 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 0 / Day 6
- 검수 기준 커밋: `61991091cd19756e343efa47b0c145cb5a0a3fda`
- 기준 커밋 제목: `5일차 : 이벤트 조건 및 디버그 기반 구축`
- 목적: Phase 0 전체 기반 통합 검수 및 Phase 1 진입 전 구조 확인

---

## 목표

1~5일차 동안 구축한 Core, Data, Save, Scene/UI, Event 시스템을 한 번에 검토하고 Phase 1 개발을 시작하기 전에 구조적으로 막히는 문제가 없는지 확인한다.

6일차는 새로운 콘텐츠 구현보다 다음 흐름이 서로 충돌하지 않고 연결되는지 확인하는 데 집중한다.

`Data Load → New Game → Scene Flow → State Change → StoryFlag → Save → Load → State Restore`

---

## 검수 대상

### Core

- `GameManager`
- `SceneLoader`
- `BootstrapStartup`
- Bootstrap의 `DontDestroyOnLoad` 유지 구조
- Manager 중복 생성 여부
- Manager 초기화 순서

현재 의도된 초기화 순서:

1. DataManager
2. SaveManager
3. EventManager
4. GameManager 초기화 완료

### Data

검수 대상 데이터:

- CharacterData
- MonsterData
- DungeonData
- ItemData
- ProjectHDataCatalog
- DataRegistry
- DataManager Validation

현재 Phase 0 기준 샘플 데이터:

- Characters: 4
- Monsters: 3
- Dungeons: 1
- Items: 2

검수 항목:

- 빈 ID
- 중복 ID
- null 데이터
- Catalog 누락
- Registry 생성 실패
- 정적 데이터 조회 실패

### Save

검수 대상:

- SaveData
- CharacterSaveData
- SaveManager
- `save_001.json`
- `saveVersion`
- Party 데이터
- Character Level / Experience
- StoryFlag

기본 회귀 시나리오:

1. 새 게임 생성
2. Day를 7로 변경
3. `CH_SERENA`를 Lv.5 / Exp 350으로 변경
4. `STORY_SERENA_JOINED` 활성화
5. Save
6. Load
7. 변경값 비교

확인 대상:

- Day 유지
- Time 유지
- Party 유지
- Character Level 유지
- Experience 유지
- StoryFlag 유지
- SaveVersion 유지

기존 저장 파일에 StoryFlag 목록이 없는 경우 `EnsureDefaults()`를 통해 빈 목록으로 보정되는 구조도 함께 확인한다.

### Scene / UI

핵심 Scene:

- Bootstrap
- Title
- Lobby
- Party
- DungeonSelect
- Battle
- Result

기본 화면 흐름:

`Bootstrap → Title → Lobby → Party → DungeonSelect → Battle → Result → Lobby`

검수 항목:

- Scene 파일 존재
- Build Settings 등록
- Bootstrap이 첫 번째 Scene인지 확인
- Scene 전환 중 Manager 유지
- 중복 Manager 생성 여부
- Canvas 존재
- EventSystem 존재
- `PrototypeScreenController` 연결
- Button Event 연결

현재 Battle은 실제 전투 구현이 아닌 화면 골격이며 `임시 승리`를 통해 Result로 이동하는 Phase 0 검증용 흐름이다.

### Event / StoryFlag

검수 대상:

- EventCondition
- EventConditionEvaluator
- EventDefinition
- ProjectHEventCatalog
- EventManager
- ProjectHEventBus
- StoryFlag

현재 지원 조건:

- Always
- StoryFlag
- DayAtLeast
- DayAtMost
- ChapterEquals
- CharacterLevelAtLeast
- HasSaveData

조건 그룹:

- All
- Any

검수용 이벤트:

`EV_DEBUG_ALWAYS`

- 항상 활성

`EV_DEBUG_SERENA_DAY3`

- `STORY_SERENA_JOINED = true`
- `CurrentDay >= 3`
- `CH_SERENA Level >= 2`

조건 실패 시 단순 false가 아니라 실패 이유 문자열을 반환하는지도 확인한다.

---

## Debug State Monitor 검수

메뉴:

`Tools > Project H > Debug > State Monitor`

확인 대상:

- 현재 Scene
- GameManager 상태
- DataManager 상태
- SaveManager 상태
- EventManager 상태
- Save 파일 존재 여부
- Save Path
- Day
- Time
- Chapter
- Main Quest
- StoryFlag 목록
- EventDefinition 개수
- 활성 Event 개수
- Event 조건 평가 결과
- Data Validation 오류 수
- Event Validation 오류 수

StoryFlag ON / OFF 기능도 검수한다.

초기 구현에서 활성 StoryFlag 목록을 순회하는 동안 원본 목록을 직접 변경할 가능성이 있었으나, 이후 수정에서 OFF 대상 ID를 저장한 뒤 반복 종료 후 `RemoveStoryFlag()`를 실행하도록 변경했다.

---

## EditMode 테스트 검수

현재 Phase 0에서 확인할 테스트 묶음:

- `DataRegistryTests`
- `SaveDataTests`
- `GameScenesTests`
- `StoryFlagTests`
- `EventConditionEvaluatorTests`

주요 확인 내용:

### DataRegistryTests

- 정상 ID 조회
- 중복 ID 검출
- 빈 ID 검출

### SaveDataTests

- 새 게임 기본값
- 초기 Party 생성
- JSON 직렬화 / 역직렬화
- Day / Level / Exp 복원
- 존재하지 않는 Character ID 조회

### GameScenesTests

- 핵심 Scene 이름 중복 여부

### StoryFlagTests

- StoryFlag 추가
- StoryFlag 조회
- StoryFlag 제거
- JSON Round Trip 후 Flag 유지

### EventConditionEvaluatorTests

- ALL 조건 성공
- 실패 조건 Reason 반환
- ANY 조건 성공
- StoryFlag 조건
- Day 조건
- Character Level 조건

---

## 수동 통합 검수 순서

### 1. Bootstrap

`Bootstrap.unity`를 열고 `[ProjectH] Bootstrap`을 확인한다.

필수 구성:

- SceneLoader
- DataManager
- SaveManager
- EventManager
- GameManager
- BootstrapStartup

DataManager에는 Data Catalog, EventManager에는 Event Catalog가 연결되어 있어야 한다.

### 2. Play 시작

Bootstrap에서 Play한다.

Console에서 다음 시스템들의 초기화 실패 로그가 없는지 확인한다.

- DataManager
- SaveManager
- EventManager
- GameManager

### 3. Title

- 새 게임 버튼 동작
- 저장 파일이 없을 때 이어하기 비활성
- 저장 파일이 있을 때 이어하기 활성

### 4. Lobby

새 게임 후 다음 정보를 확인한다.

- Day 1
- Morning
- 현재 Chapter
- 현재 Main Quest

### 5. Party

초기 4인 Party 표시 확인:

- Serena
- Ellen
- Lilia
- Eve

### 6. DungeonSelect

`DG_LETICIA_FOREST` 데이터 표시를 확인한다.

- Dungeon 이름
- Region
- Recommended Level
- Reward EXP
- Reward Gold

### 7. Battle

- Battle Scene 진입
- 기본 HUD 표시
- 4인 Party HUD 영역
- Enemy 영역
- 임시 승리 버튼 동작

### 8. Result

- Victory 표시
- Dungeon 정보
- EXP 표시
- Gold 표시
- Lobby 복귀

### 9. StoryFlag

State Monitor에서:

`STORY_SERENA_JOINED`

를 ON으로 변경한다.

Flag 목록에 표시되는지 확인하고 OFF 후 정상적으로 사라지는지 확인한다.

### 10. Event Condition

`EV_DEBUG_SERENA_DAY3`를 기준으로 조건 실패 및 성공 상태를 확인한다.

초기 상태:

- Day 1
- Serena Lv.1

결과:

- 조건 실패

테스트 진행도 적용 후:

- Day 7
- Serena Lv.5
- `STORY_SERENA_JOINED = true`

결과:

- 조건 성공

### 11. Save / Load

테스트 진행 상태를 저장한다.

확인 상태:

- Day 7
- Serena Lv.5
- Exp 350
- StoryFlag 활성

Play 종료 후 다시 실행하고 Load한다.

동일한 상태가 복원되는지 확인한다.

---

## Phase 0 Gate 기준

Phase 1 진행 전 다음 조건을 목표로 한다.

| 영역 | Gate 기준 |
|---|---|
| Core | Manager 누락 및 중복 문제 없음 |
| Data | 기본 데이터 Validation 오류 없음 |
| Save | 새 게임 / Save / Load 흐름 유지 |
| Scene | 핵심 7개 Scene 순환 가능 |
| UI | 필수 Canvas / Controller / EventSystem 연결 |
| Event | EventDefinition 및 조건 평가 가능 |
| StoryFlag | Save / Load 후 상태 유지 |
| Debug | 현재 상태와 조건 실패 원인 확인 가능 |
| Tests | 기존 EditMode 테스트에서 구조 문제 확인 |

---

## 현재 검수 상태

GitHub 기준으로 1~5일차 구현과 5일차 StoryFlag Debug 수정사항까지 저장소에 반영되어 있다.

최신 기준 커밋:

`61991091cd19756e343efa47b0c145cb5a0a3fda`

`5일차 : 이벤트 조건 및 디버그 기반 구축`

GitHub에는 해당 커밋에 대한 CI 상태 검사가 등록되어 있지 않다.

따라서 저장소의 코드 및 에셋 구조를 기준으로 검수 항목을 정리했으며, Unity Editor에서의 실제 Compile 성공, EditMode Test Runner 결과, 전체 Play Mode 순환 결과는 수동 검수 항목으로 남긴다.

---

## Day 6 결론

6일차는 Phase 0에서 만든 개별 시스템을 다시 확장하기보다 전체 기반을 한 번에 확인하는 통합 QA 단계로 사용한다.

중점 확인 흐름:

`Bootstrap → Data → Save → Scene/UI → Event/Flag → Save/Load`

Phase 1 진입 전 최종 목표는 새로운 기능 수를 늘리는 것이 아니라 이후 전투, 파티 편성, 스킬, 적 AI 등의 구현을 시작했을 때 기반 시스템 문제로 개발이 중단되지 않도록 하는 것이다.

Phase 0 Day 6 통합 검수 과정 기록.
