# Project H — Phase 0 Day 4 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 0 / Day 4
- 기준 커밋: `c43355358d1c8cc114ad2f4b2186f8c8bbf889dd`
- 커밋 당시 메시지: `4`
- Unity: 6000.3.21f1

---

## 목표

프로젝트의 핵심 화면 흐름을 실제 Unity Scene과 Canvas UI로 구성하고, 앞서 구현한 DataManager와 SaveManager를 UI에서 사용할 수 있도록 연결한다.

이번 단계의 목적은 완성된 콘텐츠 구현이 아니라 다음 핵심 루프를 끊김 없이 순환할 수 있는 게임 화면 골격을 만드는 것이다.

`Bootstrap → Title → Lobby → Party → DungeonSelect → Battle → Result → Lobby`

---

## 완료 작업

### 핵심 Scene 구성

다음 Scene을 생성하고 Build Settings에 등록했다.

- `Bootstrap`
- `Title`
- `Lobby`
- `Party`
- `DungeonSelect`
- `Battle`
- `Result`

Bootstrap을 첫 번째 Scene으로 유지하고 이후 핵심 Scene을 순서대로 등록했다.

### Bootstrap 초기 진입

`BootstrapStartup`을 추가했다.

Bootstrap에서 GameManager 공통 초기화가 완료되면 Title Scene으로 이동한다.

기존 Bootstrap에는 다음 공통 시스템이 유지된다.

- `GameManager`
- `SceneLoader`
- `DataManager`
- `SaveManager`
- `BootstrapStartup`

### SceneLoader 확장

기존 `SceneLoader`를 확장했다.

- 씬 이름 공백 검증
- Build Settings 등록 여부 검증
- 비동기 Scene 전환
- 중복 Scene 전환 방지
- Scene Load 완료 후 전환 상태 해제
- 현재 Scene 재로드

핵심 Scene 이름은 `GameScenes`에서 공통 상수로 관리한다.

### Canvas UI 시스템

각 화면을 1920×1080 기준 Canvas로 구성했다.

Canvas Scaler 설정:

- `Scale With Screen Size`
- Reference Resolution `1920 × 1080`
- Match Width Or Height `0.5`

New Input System 기반 `InputSystemUIInputModule`을 사용하도록 구성했다.

### UI 디자인 방향

밝은 판타지 모바일 RPG 화면 구성을 기준으로 프로젝트 H용 프로토타입 UI를 제작했다.

주요 특징:

- 밝은 판타지 배경
- 크림색 반투명 패널
- 금색 테두리
- 파스텔 블루 / 핑크 핵심 버튼
- 둥근 카드형 UI
- 상단 자원 정보 영역
- 하단 메뉴 내비게이션
- 캐릭터 카드 및 전투 HUD
- 던전 노드형 선택 화면

실제 캐릭터 일러스트와 최종 UI 리소스가 없는 부분은 교체 가능한 Prototype 그래픽으로 구성했다.

### Title

Title Scene에 다음 기능을 연결했다.

- 새 게임
- 이어하기
- 게임 종료
- 저장 파일 존재 여부에 따른 이어하기 버튼 활성/비활성

새 게임:

`SaveManager.CreateNewGame() → Lobby`

이어하기:

`SaveManager.LoadCurrent() → Lobby`

### Lobby

Lobby에 다음 UI 골격을 구성했다.

- 상단 상태/자원 Bar
- 메인 캐릭터 일러스트 영역
- 현재 Chapter / Main Quest 표시
- Day / Time 표시
- Save 상태 표시
- 저장 버튼
- 하단 내비게이션

Day, Time, Chapter, Main Quest는 현재 SaveData에서 읽는다.

### Party

Party Scene에 다음 구조를 구성했다.

- 캐릭터 목록 영역
- 4인 Party 표시
- 캐릭터 이름
- 현재 Level 표시
- 던전 선택 이동
- 하단 내비게이션

파티 데이터는 SaveData의 `partyCharacterIds`와 CharacterSaveData를 사용하고, 캐릭터 표시 이름은 DataManager의 CharacterData에서 가져온다.

실제 캐릭터 교체 기능은 아직 구현하지 않았다.

### DungeonSelect

DungeonSelect Scene에서 `DG_LETICIA_FOREST` 데이터를 실제 DataManager에서 조회하도록 연결했다.

표시 정보:

- 던전 이름
- Region ID
- 권장 Level
- EXP 보상
- Gold 보상
- 던전 Node
- 전투 시작

현재 첫 번째 프로토타입 던전만 활성화하고 나머지 Node는 잠금 상태로 구성했다.

### Battle

Battle Scene에 향후 전투 시스템이 들어갈 UI 골격을 구성했다.

- Wave 표시
- Timer
- 아군 배치 영역
- 적 배치 영역
- 하단 4인 캐릭터 HUD
- HP Bar
- TP/게이지 Bar
- AUTO 버튼
- 임시 승리 버튼

실제 자동 전투, 타겟팅, 피해 계산, 스킬 시스템은 아직 연결하지 않았다.

`임시 승리 → Result`

흐름을 사용해 Scene Loop를 검증할 수 있도록 구성했다.

### Result

Result Scene에 다음 UI를 구성했다.

- VICTORY 표시
- 완료 던전 정보
- EXP 보상 표시
- Gold 보상 표시
- Lobby 복귀 버튼

현재 결과 화면은 보상 정보를 표시하며 실제 보상 지급은 이후 시스템에서 연결한다.

### 공통 화면 Controller

`PrototypeScreenController`를 추가했다.

주요 역할:

- 새 게임 / 이어하기
- Save 실행
- Scene 이동
- Title 상태 갱신
- Lobby SaveData 표시
- Party 데이터 표시
- DungeonData 표시
- Battle 프로토타입 정보 표시
- Result 보상 표시
- 게임 종료

### UI Prototype Art

`Assets/ProjectH/UI/Art/Prototype`에 교체 가능한 UI 그래픽 구조를 추가했다.

분류:

- `Backgrounds`
- `Frames`
- `Buttons`
- `Icons`

현재 파일은 UI 배치와 화면 흐름을 확인하기 위한 프로토타입 리소스다.

향후 최종 UI 이미지가 제작되면 동일 역할의 Sprite를 교체해 Scene 구조를 유지할 수 있도록 했다.

### Editor 자동 설정

`Phase0Day4Setup`을 추가했다.

메뉴:

`Tools > Project H > Phase 0 > 4일차 설정 실행`

실행 시 다음 작업을 자동으로 처리한다.

- UI Prototype Texture의 Sprite Import 설정
- Title Scene 생성
- Lobby Scene 생성
- Party Scene 생성
- DungeonSelect Scene 생성
- Battle Scene 생성
- Result Scene 생성
- Canvas 및 EventSystem 생성
- 각 화면 Controller와 Button 이벤트 연결
- BootstrapStartup 연결
- Build Settings 핵심 Scene 등록

### Assembly Definition

UI와 Input System 사용을 위해 Assembly Definition 참조를 갱신했다.

Runtime:

- `Unity.ugui`

Editor:

- `ProjectH.Runtime`
- `Unity.ugui`
- `Unity.InputSystem`

프로젝트 Package에는 Input System과 UGUI가 포함되어 있다.

### 테스트

`GameScenesTests`를 추가했다.

검증 항목:

- Bootstrap
- Title
- Lobby
- Party
- DungeonSelect
- Battle
- Result

핵심 Scene 이름이 서로 중복되지 않는지 확인한다.

---

## 검토 결과

최신 커밋 기준 정적 검토에서 Phase 0 Day 4 진행을 막는 명확한 구조 문제는 확인되지 않았다.

확인 항목:

- Bootstrap에 `BootstrapStartup` 연결 확인
- Bootstrap에서 Title로 이동하는 진입 흐름 확인
- 7개 핵심 Scene의 Build Settings 등록 확인
- `SceneLoader`의 중복 전환 및 Build Settings 검증 확인
- `PrototypeScreenController`의 DataManager / SaveManager 연결 확인
- Title 새 게임 / 이어하기 흐름 확인
- Lobby에서 SaveData를 사용하는 구조 확인
- Party에서 CharacterData와 CharacterSaveData를 함께 사용하는 구조 확인
- DungeonSelect와 Result에서 DungeonData를 사용하는 구조 확인
- Runtime / Editor Assembly의 UGUI 및 Input System 참조 확인
- 프로젝트 Package에 `com.unity.ugui`와 `com.unity.inputsystem` 존재 확인
- 핵심 Scene 이름 중복 검사용 EditMode 테스트 추가 확인

GitHub에는 해당 커밋에 대한 CI 상태 검사가 등록되어 있지 않다.

따라서 저장소 기준 구조 검토는 완료했지만 Unity Editor 실제 컴파일, Play Mode의 전체 화면 순환, Button 입력, EditMode Test Runner 성공까지 GitHub 상태만으로 자동 증명되지는 않는다.

---

## Day 4 완료 기준

- Bootstrap 실행 후 Title로 진입할 것
- Title에서 새 게임을 시작할 수 있을 것
- 저장 파일 존재 여부에 따라 이어하기 상태가 변경될 것
- Lobby에서 현재 SaveData 상태를 확인할 수 있을 것
- Lobby와 Party, DungeonSelect 사이를 이동할 수 있을 것
- DungeonSelect에서 프로토타입 DungeonData가 표시될 것
- Battle Scene의 전투 UI 골격이 표시될 것
- 임시 승리로 Result에 진입할 수 있을 것
- Result에서 Lobby로 복귀할 수 있을 것
- Scene 전환 중 GameManager, DataManager, SaveManager 상태가 유지될 것

Phase 0 Day 4 Scene/UI 골격 및 프로토타입 UI 구축.
