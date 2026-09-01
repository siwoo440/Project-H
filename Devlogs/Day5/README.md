# Project H — Phase 0 Day 5 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 0 / Day 5
- 기준 커밋: `704594c92d9bd34b11230b5964046fe9215e409b`
- 커밋 당시 메시지: `5`
- Unity: 6000.3.21f1

---

## 목표

향후 스토리, 캐릭터 이벤트, 던전 해금, 조건부 UI와 각종 진행 분기를 코드에 직접 하드코딩하지 않고 확장할 수 있도록 이벤트 조건, StoryFlag, EventBus, 개발용 상태 확인 도구의 기반을 구축한다.

실제 이벤트 내용이 확정된 이후에도 `EventDefinition` 에셋과 조건 조합을 추가하는 방식으로 확장할 수 있도록 구성한다.

---

## 완료 작업

### StoryFlag 저장 구조

`SaveData`에 `storyFlags` 목록을 추가했다.

구현 기능:

- `HasStoryFlag()`
- `SetStoryFlag()`
- `RemoveStoryFlag()`
- `StoryFlags`
- `EnsureDefaults()`

StoryFlag는 SaveData의 일부로 JSON에 저장되므로 저장 후 재실행 및 Load 과정에서도 유지될 수 있는 구조다.

기존 저장 파일에 StoryFlag 필드가 없더라도 `EnsureDefaults()`에서 빈 목록을 보완하도록 구성했다.

### EventCondition

공통 이벤트 조건 구조를 추가했다.

현재 지원 조건:

- `Always`
- `StoryFlag`
- `DayAtLeast`
- `DayAtMost`
- `ChapterEquals`
- `CharacterLevelAtLeast`
- `HasSaveData`

조건 그룹 방식:

- `All`
- `Any`

조건 판정은 개별 이벤트 구현 코드가 아니라 `EventConditionEvaluator`에서 공통 처리한다.

### EventConditionEvaluator

이벤트 실행 가능 여부를 공통으로 판정하는 평가기를 추가했다.

평가 결과는 단순한 true / false뿐 아니라 조건 실패 또는 성공 사유도 반환한다.

예:

- StoryFlag 기대값과 현재값
- 현재 Day와 최소/최대 Day
- 현재 Chapter
- 캐릭터 실제 Level
- SaveData 존재 여부

이를 통해 이벤트가 실행되지 않는 원인을 Debug 도구에서 즉시 확인할 수 있다.

### EventDefinition

이벤트 정보를 ScriptableObject 에셋으로 관리할 수 있도록 `EventDefinition`을 추가했다.

주요 항목:

- Event ID
- Display Name
- Condition Group
- Conditions

Unity Create 메뉴를 통해 새로운 이벤트 정의 에셋을 추가할 수 있다.

실제 스토리 및 캐릭터 이벤트가 확정되면 이벤트별 별도 코드 대신 EventDefinition 에셋을 추가하고 조건을 설정하는 방식으로 확장할 수 있다.

### Event Catalog

`ProjectHEventCatalog`를 추가했다.

EventManager가 전체 EventDefinition을 직접 검색하지 않고 Catalog를 통해 등록된 이벤트를 관리한다.

Editor 메뉴를 통해 `Assets/ProjectH/Data/Events/Definitions` 아래의 EventDefinition을 다시 수집해 Catalog를 재구성할 수 있도록 했다.

5일차 Setup 재실행 시 기존 Catalog에 등록된 실제 이벤트를 보존하면서 Prototype 이벤트를 갱신하도록 구성했다.

### EventManager

Bootstrap 공통 시스템에 `EventManager`를 추가했다.

주요 기능:

- EventDefinition ID Registry
- 중복 Event ID 검증
- 빈 Event ID 검증
- EventDefinition null 검증
- StoryFlag 조회
- StoryFlag 활성화 / 비활성화
- EventDefinition 조회
- 이벤트 조건 평가
- 현재 사용 가능한 이벤트 목록 조회

GameManager에서 다음 순서로 초기화된다.

1. DataManager
2. SaveManager
3. EventManager
4. GameManager 초기화 완료

### EventBus

시스템 간 직접 참조를 줄이기 위한 `ProjectHEventBus`를 추가했다.

현재 신호:

- `StoryFlagChangedEvent`
- `SaveLifecycleEvent`

Save Lifecycle:

- `NewGameCreated`
- `Saved`
- `Loaded`
- `Deleted`

이 구조는 이후 Battle, Affinity, Quest, Item 등 다른 시스템 이벤트를 동일한 방식으로 확장할 수 있다.

### SaveManager 연동

기존 SaveManager를 EventBus 및 StoryFlag 저장 구조와 연결했다.

추가 처리:

- 새 게임 생성 이벤트 발행
- 저장 완료 이벤트 발행
- Load 완료 이벤트 발행
- 저장 삭제 이벤트 발행
- Load 시 `EnsureDefaults()` 처리
- StoryFlag ID 중복 검증
- 빈 StoryFlag ID 검증
- Debug 상태 로그에 Flag 개수 표시

### Prototype Event

조건 시스템 확인용으로 다음 두 이벤트를 추가했다.

`EV_DEBUG_ALWAYS`

- 항상 활성

`EV_DEBUG_SERENA_DAY3`

조건:

- `STORY_SERENA_JOINED = true`
- `CurrentDay >= 3`
- `CH_SERENA Level >= 2`

실제 이벤트가 확정되면 `EV_DEBUG_` 에셋은 제거할 수 있다.

### Editor 자동 설정

`Phase0Day5Setup`을 추가했다.

메뉴:

`Tools > Project H > Phase 0 > 5일차 설정 실행`

실행 시:

- 이벤트 데이터 폴더 생성
- Prototype EventDefinition 생성
- Event Catalog 생성 또는 갱신
- Bootstrap에 EventManager 추가
- Event Catalog 연결

이벤트 카탈로그 재구성 메뉴:

`Tools > Project H > Event > 이벤트 카탈로그 재구성`

### Debug State Monitor

개발 중 현재 게임 상태와 이벤트 조건을 빠르게 확인하기 위한 `ProjectHDebugWindow`를 추가했다.

메뉴:

`Tools > Project H > Debug > State Monitor`

확인 항목:

- 현재 Scene
- GameManager 초기화 상태
- DataManager 초기화 상태
- SaveManager 초기화 상태
- EventManager 초기화 상태
- Save 파일 존재 여부
- Save Path
- Day
- Time
- Chapter
- Main Quest
- StoryFlag 목록
- EventDefinition 개수
- 현재 실행 가능한 Event 개수
- 개별 Event 조건 평가 사유
- Character / Monster / Dungeon / Item 개수
- Data Validation 오류 수
- Event Validation 오류 수

Debug 창에서 StoryFlag ID를 직접 입력해 ON / OFF할 수 있다.

### StoryFlag Debug 수정

초기 구현에서는 활성 StoryFlag 목록을 `foreach`로 순회하는 동안 OFF 버튼이 눌리면 원본 목록을 즉시 변경할 가능성이 있었다.

이를 수정해:

1. OFF 대상 Flag ID를 임시 변수에 저장
2. StoryFlag 순회를 종료
3. 순회 종료 후 `RemoveStoryFlag()` 실행

순서로 변경했다.

이를 통해 StoryFlag Debug 목록 순회 도중 컬렉션이 변경되는 구조를 제거했다.

### EditMode 테스트

다음 테스트를 추가했다.

`StoryFlagTests`

- StoryFlag 추가
- StoryFlag 조회
- StoryFlag 제거
- JSON 직렬화 / 역직렬화 후 Flag 유지

`EventConditionEvaluatorTests`

- ALL 조건 전체 성공
- Day 조건 실패 및 실패 사유 반환
- ANY 조건 중 하나 성공
- StoryFlag 조건
- 캐릭터 Level 조건

---

## 검토 결과

최신 커밋 기준 정적 검토에서 Phase 0 Day 5 진행을 막는 명확한 구조 문제는 추가로 확인되지 않았다.

확인 항목:

- Bootstrap에 EventManager 및 Event Catalog 연결
- GameManager의 EventManager 초기화 연결
- SaveData의 StoryFlag 저장 구조
- 이전 SaveData 기본값 보완 구조
- EventDefinition / Event Catalog 구조
- Event ID Registry와 기본 Validation
- EventCondition의 All / Any 평가 구조
- 조건 평가 실패 사유 반환
- StoryFlag와 Save Lifecycle EventBus 발행
- Event Catalog 자동 재구성 메뉴
- Debug State Monitor
- StoryFlag Debug OFF 처리의 순회 후 삭제 방식
- StoryFlag 및 EventCondition EditMode 테스트 코드

GitHub에는 해당 커밋의 CI 상태 검사가 등록되어 있지 않다.

따라서 저장소 정적 코드와 에셋 연결은 확인했지만 Unity Editor 실제 컴파일, EditMode Test Runner, Play Mode에서의 Flag 저장/복원과 Debug Window 동작 성공 여부까지 GitHub 상태만으로 증명되지는 않는다.

---

## Day 5 완료 기준

- Bootstrap에서 EventManager가 초기화될 것
- Event Catalog가 EventManager에 연결될 것
- EventDefinition을 ID로 조회할 수 있을 것
- StoryFlag를 ON / OFF할 수 있을 것
- StoryFlag가 SaveData에 저장될 것
- 저장 후 Load 과정에서 StoryFlag가 복원될 수 있을 것
- All / Any 조건을 평가할 수 있을 것
- Day / Chapter / Character Level / StoryFlag 조건을 평가할 수 있을 것
- 조건 실패 사유를 확인할 수 있을 것
- Debug State Monitor에서 Save / Data / Flag / Event 상태를 확인할 수 있을 것
- EventDefinition 추가 후 Catalog를 다시 구성할 수 있을 것
- Debug Flag OFF 처리 중 원본 컬렉션을 순회 중 직접 변경하지 않을 것

Phase 0 Day 5 이벤트 조건, StoryFlag, EventBus 및 개발 Debug 기반 구축.
