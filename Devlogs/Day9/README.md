# Project H — Phase 1 Day 9 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 9
- 기준 커밋: `5576ba90421b17933de332c611c6a4170caf744f`
- 커밋 당시 메시지: `9`
- 주제: Title / Lobby 실제 게임 흐름 구축

---

## 목표

Phase 0에서 하나의 `PrototypeScreenController`가 담당하던 화면 흐름 중 Title과 Lobby를 실제 Phase 1 전용 Controller로 분리한다.

Title에서는 새 게임 / 이어하기 / 종료와 저장 상태에 따른 버튼 제어를 담당하고, Lobby에서는 현재 SaveData를 기반으로 Day, 시간, Chapter, Main Quest, 저장 상태와 현재 4인 파티를 표시하도록 구성한다.

기본 흐름:

`Bootstrap → Title → New Game / Continue → Lobby`

Lobby 기본 이동:

`Lobby → Party / DungeonSelect / Title`

---

## TitleScreenController

Title 화면을 `PrototypeScreenController`에서 분리해 `TitleScreenController`가 전담하도록 변경했다.

주요 기능:

- 새 게임 시작
- 이어하기
- 게임 종료
- 저장 데이터 존재 여부 확인
- 이어하기 버튼 활성 / 비활성
- 새 게임 / 이어하기 중 중복 클릭 방지
- 저장 Load 실패 상태 표시
- Bootstrap 미실행 상태 안내
- 저장 생명주기 이벤트 구독

화면 전환 중에는 `isTransitioning`을 사용해 버튼 입력을 잠근다.

새 게임 흐름:

`NewGame → SaveManager.CreateNewGame → Lobby`

이어하기 흐름:

`Continue → SaveManager.LoadCurrent → Lobby`

저장 데이터가 없거나 Load에 실패하면 Lobby로 이동하지 않고 Title에 실패 상태를 표시한다.

---

## TitleScreenViewState

Title의 순수 표시 상태 계산을 Controller에서 분리했다.

저장 없음:

- 상태: 새로운 여정을 시작해 주세요.
- Continue 비활성

저장 있음:

- 상태: 저장된 여정이 있습니다.
- Continue 활성

이를 통해 저장 존재 여부에 따른 UI 상태를 Scene 없이 EditMode에서 검증할 수 있도록 했다.

---

## LobbyScreenController

Lobby 화면도 기존 `PrototypeScreenController`에서 분리해 `LobbyScreenController`가 담당하도록 변경했다.

표시 데이터:

- Current Day
- Current Time
- Current Chapter
- Current Main Quest
- Save 상태
- 현재 Party 인원
- Party Character 이름
- Party Character Level

주요 기능:

- 현재 진행 저장
- Lobby 상태 새로고침
- Party Scene 이동
- DungeonSelect Scene 이동
- Title Scene 이동
- Scene 전환 중 중복 클릭 방지
- SaveLifecycleEvent 기반 자동 UI 갱신

SaveData가 없는 상태에서 Lobby에 직접 진입하면 진행 버튼을 잠그고 Title로 돌아갈 수 있도록 구성했다.

---

## LobbyScreenViewData

Lobby의 표시 문자열과 진행 가능 여부를 Controller에서 분리했다.

정상 SaveData가 있는 경우 다음 형태의 데이터를 생성한다.

```text
DAY 1 · Morning

CHAPTER_01
MAIN_001

PARTY · 4/4
1. 세레나  Lv.1
2. 엘렌  Lv.1
3. 릴리아  Lv.1
4. 이브  Lv.1
```

Character ID를 `DataManager`로 조회해 실제 표시 이름을 사용하고, `CharacterSaveData`에서 Level을 읽는다.

CharacterData가 누락된 경우에도 ID를 대체 표시해 화면 갱신 중 NullReference가 발생하지 않도록 처리한다.

---

## SaveLifecycleEvent 연동

Title과 Lobby 모두 기존 `ProjectHEventBus`의 `SaveLifecycleEvent`를 구독한다.

대상 이벤트:

- NewGameCreated
- Saved
- Loaded
- Deleted

Controller 활성화 시 이벤트를 구독하고 비활성화 시 구독을 해제한다.

저장 상태가 변경되면 현재 SaveManager / SaveData 상태를 다시 읽어 화면을 갱신한다.

Scene 전환 중 발생한 이벤트는 불필요한 UI 갱신을 막기 위해 무시한다.

---

## Title Scene 변경

`Title.unity`에서 기존 `PrototypeScreenController`를 제거하고 `TitleScreenController`를 연결했다.

연결 대상:

- Status Text
- NewGameButton
- ContinueButton
- QuitButton

각 버튼의 기존 Persistent Listener를 정리하고 새 Controller 메서드에 다시 연결했다.

---

## Lobby Scene 변경

`Lobby.unity`에서 기존 `PrototypeScreenController`를 제거하고 `LobbyScreenController`를 연결했다.

기존 요소 연결:

- RuntimeStatus
- MissionBody
- SaveState
- SaveButton
- Nav_로비
- Nav_파티
- Nav_모험

추가 UI:

- `PartySummary`
- `TitleButton`

`PartySummary`는 현재 4인 파티 이름과 Level을 표시하며, `TitleButton`은 Lobby에서 Title로 돌아가는 용도로 사용한다.

기존 UI 이미지를 재사용하므로 9일차에서 별도의 신규 이미지 에셋은 추가하지 않았다.

---

## Phase1Day9Setup

9일차 Scene 마이그레이션을 자동화하는 Editor 도구를 추가했다.

메뉴:

`Tools > Project H > Phase 1 > 9일차 Title-Lobby 설정 실행`

처리 내용:

1. Title Scene 열기
2. 기존 PrototypeScreenController 제거
3. TitleScreenController 추가
4. Title 버튼 이벤트 재연결
5. Lobby Scene 열기
6. 기존 PrototypeScreenController 제거
7. LobbyScreenController 추가
8. PartySummary 생성 또는 기존 요소 재사용
9. TitleButton 생성 또는 기존 요소 재사용
10. Lobby 버튼 이벤트 재연결
11. Scene 저장

동일 메뉴를 다시 실행해도 기존 `PartySummary`와 `TitleButton`을 검색해 재사용하도록 구성했다.

---

## PrototypeScreenController 범위

9일차 이후 Title과 Lobby는 전용 Controller로 분리됐다.

현재 `PrototypeScreenController`는 다음 Phase 0 프로토타입 화면에서 계속 사용한다.

- Party
- DungeonSelect
- Battle
- Result

Party는 10일차 편성 시스템 구현 시 별도 Controller로 분리하는 방향이다.

---

## EditMode 테스트

9일차에 다음 테스트를 추가했다.

### TitleScreenViewStateTests

검증 항목:

- 저장 데이터 없음 → Continue 비활성
- 저장 데이터 있음 → Continue 활성
- 저장 상태에 따른 안내 문구 생성

### LobbyScreenViewDataTests

검증 항목:

- SaveData 없음 → Lobby 진행 잠금
- SaveData 없음 → Title 이동 안내
- 초기 4인 Party 표시
- CharacterData의 DisplayName 사용
- CharacterSaveData Level 표시
- Day 표시
- 저장 상태 표시

---

## 최신 저장소 검토

Day 8 커밋과 Day 9 커밋을 비교해 실제 반영 파일을 확인했다.

주요 변경:

- `Title.unity` 수정
- `Lobby.unity` 수정
- `TitleScreenController` 추가
- `TitleScreenViewState` 추가
- `LobbyScreenController` 추가
- `LobbyScreenViewData` 추가
- `Phase1Day9Setup` 추가
- Title 화면 EditMode 테스트 추가
- Lobby 화면 EditMode 테스트 추가

최신 Scene에는 Title / Lobby의 전용 Controller 참조와 버튼 이벤트가 저장된 상태다.

저장소 코드 및 Scene 직렬화 구조를 기준으로 9일차 진행을 막는 명확한 추가 문제는 확인하지 못했다.

GitHub Commit Status에는 별도의 CI 상태 검사가 등록되어 있지 않다.

따라서 Unity Editor 실제 Compile 성공 및 EditMode Test Runner 통과 여부까지 GitHub 상태만으로 자동 검증된 것은 아니다.

---

## Day 9 완료 기준

- TitleScreenController 분리
- LobbyScreenController 분리
- Title 저장 상태 표시 구조 분리
- 저장 유무에 따른 Continue 제어
- 새 게임 / 이어하기 중복 클릭 방지
- Load 실패 안내
- Lobby Day / Time / Chapter / Quest 표시
- Lobby 현재 Party 4인 이름 / Level 표시
- Lobby Save 상태 표시
- Lobby → Party / DungeonSelect / Title 이동 연결
- SaveLifecycleEvent 기반 UI Refresh
- Title / Lobby Scene에 새 Controller 실제 연결
- Title / Lobby 표시 데이터 EditMode 테스트 코드 존재

Phase 1 Day 9 — Title / Lobby 실제 게임 흐름 구축.
