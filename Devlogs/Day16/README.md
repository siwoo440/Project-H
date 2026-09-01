# Project H — Phase 1 Day 16 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 16
- 기준 원격 커밋: `557b8f611c9b8970b203a88979be43eb1745d16b`
- 기준 원격 커밋 메시지: `a`
- 주제: Battle HUD 및 전투 조작 UI 구축

---

## 목표

11~15일차에서 구축한 전투 시스템을 플레이어가 실제로 읽고 조작할 수 있도록 Battle HUD를 정리한다.

16일차 핵심 범위:

- 상단 Battle HUD 정리
- 전투 배속 `×1 / ×1.5 / ×2`
- MENU와 전투 Pause 연동
- 하단 4인 HUD 카드 확장
- HP 상태 `HP OK / LOW / DANGER / DOWN`
- Skill 자리 UI 준비
- Ultimate Gauge 자리 UI 준비
- 개발용 Debug UI 분리
- Runtime ID / Enemy AI Debug 기본 숨김
- SpeedButton 시각 스타일 보정
- SpeedButton 단일 Text 상태 표시
- TimePanel 상단 중앙 정렬

실제 Skill 발동은 17일차, 실제 Ultimate Gauge 충전은 이후 Ultimate 단계에서 연결한다.

---

## BattleHudHealthState

전투 HUD에서 캐릭터의 현재 위험도를 숫자만으로 판단하지 않아도 되도록 체력 상태를 구분했다.

상태:

- `Normal`
- `Low`
- `Danger`
- `Down`

현재 판정 기준:

- HP 50% 초과 → `HP OK`
- HP 25% 초과 ~ 50% 이하 → `LOW`
- HP 1 ~ 25% → `DANGER`
- HP 0 → `DOWN`

HP Bar 색상만으로 상태를 구분하지 않고 텍스트 상태도 함께 표시할 수 있는 구조로 준비했다.

---

## BattleHudCardView 확장

기존 하단 캐릭터 카드는 이름, 레벨, HP, 임시 Portrait, 빈 Gauge 정도만 표시했다.

16일차에서는 4인 HUD 카드를 다음 정보 구조로 확장했다.

- 캐릭터 이름
- 레벨
- 임시 Portrait 영역
- 현재 HP / Max HP
- HP Bar
- 체력 상태 문구
- Ultimate Gauge
- Ultimate 퍼센트 문구
- Skill 버튼 자리
- 전투 불능 `DOWN` 상태

현재 Skill 버튼은:

`SKILL / LOCKED`

상태로 표시하며 실제 입력은 비활성화한다.

17일차 Skill 공통 시스템에서 실제 Skill 사용 상태와 Cooldown을 연결한다.

---

## Ultimate Gauge UI 자리

16일차에서는 실제 궁극기 충전 로직을 구현하지 않는다.

HUD 카드에 다음 구조만 준비했다.

`Gauge Background → Gauge Fill → ULT 0%`

`BattleHudCardView.SetUltimatePreview()`를 통해 0~1 범위의 UI 값을 받을 수 있도록 했고, 값은 `Clamp01`로 보정한다.

현재 전투 시작 시 Ultimate Gauge는 `0%`다.

실제 Gauge 충전은 이후 Ultimate Gauge 단계에서 연결한다.

---

## BattleTimeController

전투 배속과 Pause를 `BattleScreenController` 내부에 직접 넣지 않고 별도 `BattleTimeController`로 분리했다.

지원 배속:

- `×1`
- `×1.5`
- `×2`

속도 버튼을 누르면:

`×1 → ×1.5 → ×2 → ×1`

순서로 순환한다.

실제 적용은 `Time.timeScale`을 사용한다.

---

## MENU Pause

전투 중 MENU를 열면:

`Time.timeScale = 0`

으로 전투가 Pause된다.

MENU의 `계속하기`를 통해 메뉴를 닫으면 기존 선택 배속으로 다시 전투가 진행된다.

Scene 이동 및 BattleController 종료 시에는 항상:

`Time.timeScale = 1`

로 복원하여 다음 Scene까지 배속이나 Pause 상태가 남지 않도록 처리했다.

---

## AUTO 의미 유지

기존 전투 구조와 동일하게 기본 공격은 항상 자동이다.

AUTO 상태는 향후 다음 기능을 제어하기 위한 설정으로 유지한다.

- Skill 자동 사용
- Ultimate 자동 사용

`BattleScreenController.IsAutoEnabled`를 통해 이후 Skill 시스템이 AUTO 설정을 읽을 수 있도록 했다.

---

## BattleDebugPanel

개발 중 사용하던 Debug 요소를 실제 Battle HUD와 분리했다.

기본 전투 화면에서는 Debug UI를 숨긴다.

Debug 영역에는 기존 개발 기능을 모을 수 있도록 구성했다.

예:

- BattleStatus
- 공격 Debug
- Skill Debug
- Ultimate Debug
- 회복 Debug
- Runtime ID
- Enemy AI Type

MENU 안의 `DEBUG` 버튼으로 개발 패널 표시를 전환할 수 있다.

---

## Runtime ID / Enemy AI 표시 분리

기존 전투에서는 캐릭터 위에 다음과 같은 개발 정보가 항상 표시될 수 있었다.

- `ALLY_0`
- `ENEMY_1 · RUSH`

16일차부터는 이러한 Runtime Debug 정보는 기본적으로 숨긴다.

`BattleDebugPanel` 활성 상태에서만 다시 표시하도록 `BattleUnitView`와 `BattleEnemyView`에 Debug 표시 전환 기능을 추가했다.

실제 이름과 HP는 일반 전투 정보로 계속 유지한다.

---

## Phase1Day16Setup

현재 BattleScene을 수동으로 하나씩 수정하지 않아도 되도록 Editor Setup을 추가했다.

메뉴:

`Tools → Project H → Phase 1 → 16일차 Battle HUD 설정 실행`

주요 적용 내용:

- 상단 SpeedButton 생성 / 재설정
- BattleTimeController 연결
- BattleDebugPanel 구성
- 기존 Debug 버튼 전용 패널 이동
- 4인 HUD 카드 확장
- Skill 자리 생성
- Ultimate Gauge 문구 생성
- 체력 상태 문구 생성
- Runtime Debug 정보 기본 숨김
- MENU 문구 정리
- BattleScreenController Day16 참조 연결
- BattleScene 저장

최신 원격 커밋에는 `Battle.unity` 변경도 포함되어 있어 현재 Day16 Setup 적용 결과가 원격에 반영된 상태다.

---

## SpeedButton 시각 수정

초기 16일차 구현에서 SpeedButton이 Sprite가 없는 기본 Unity Image로 생성되어 흰색 사각형처럼 보이는 문제가 있었다.

이를 수정하여 기존 `MENU` 버튼을 스타일 원본으로 사용한다.

복사 대상:

- Sprite
- Override Sprite
- Image Type
- Preserve Aspect
- Fill Center
- Image Color
- Material
- Button Transition
- ColorBlock
- SpriteState
- Animation Trigger
- Text Font / Style / Size / Color / Alignment

따라서 SpeedButton은 MENU와 같은 HUD 계열의 시각 스타일을 사용한다.

---

## SpeedButton 단일 Text

Setup을 여러 번 실행한 뒤 SpeedButton에 Text가 중복되어:

`PAUSE`

뒤에 이전 글자가 겹쳐 보이는 문제가 있었다.

`BattleHudButtonLabelUtility`를 추가하여 SpeedButton 하위 Text를 항상 하나만 유지한다.

동작:

1. 기존 Text 목록 조회
2. 첫 번째 Text 하나 유지
3. 추가 Text GameObject 제거
4. 남은 Text를 `Label`로 통일
5. 버튼 전체 영역에 맞춰 배치
6. 같은 Text 하나에 현재 상태 표시

하나의 Text가 다음 모든 상태를 담당한다.

- `×1`
- `×1.5`
- `×2`
- `PAUSE`

---

## TimePanel 중앙 정렬

기존 TimePanel은 과거 Battle HUD 좌표를 유지하고 있어 오른쪽 상단에서 SpeedButton과 가까운 위치에 있었다.

기존 위치:

- X `0.80 ~ 0.885`
- Y `0.925 ~ 0.982`

16일차 최종 수정에서는 TimePanel을 화면 상단 중앙으로 이동했다.

최종 위치:

- X `0.455 ~ 0.545`
- Y `0.920 ~ 0.985`

중심 X는 정확히 `0.5`이며 SpeedButton / MENU와 같은 상단 높이 범위를 사용한다.

상단 HUD 개념:

`WAVE ........ TIME ........ SPEED · MENU`

형태로 정리한다.

`BattleTopHudLayout.ApplyTimePanel()`을 통해 Day16 Setup을 다시 실행해도 같은 중앙 위치가 적용된다.

---

## Battle HUD 상단 구성

현재 상단 플레이 UI는 다음 정보를 기준으로 구성한다.

- `WAVE 1 / 1`
- 전투 시간
- 전투 배속
- MENU

현재 실제 복수 Wave 진행은 아직 구현되지 않았으므로 `WAVE 1 / 1`을 유지한다.

여러 Wave의 실제 Spawn / 진행은 이후 Dungeon 전투 진행 시스템에서 연결한다.

---

## EditMode Test

16일차에 추가된 테스트 범위는 다음과 같다.

### BattleHudHealthStateTests

- Normal 체력 구간
- Low 체력 구간
- Danger 체력 구간
- Down 상태
- 상태별 표시 문구

### BattleSpeedRulesTests

- `×1 → ×1.5`
- `×1.5 → ×2`
- `×2 → ×1`
- 지원 배속 보정
- 배속 표시 문구

### BattleHudCardViewTests

- 초기 Ultimate Gauge `0`
- 초기 정상 체력 상태
- Ultimate Ratio 범위 보정
- HP 변경 시 HealthState 갱신

### BattleHudButtonStyleTests

- MENU 버튼 시각 스타일 복사
- Image 색상 / 타입
- Button Transition
- ColorBlock
- TargetGraphic 연결

### BattleHudButtonLabelUtilityTests

- SpeedButton 하위 중복 Text 제거
- Text 한 개만 유지
- 동일 Text에 상태 문자열 적용

### BattleTopHudLayoutTests

- TimePanel X 중앙 정렬
- TimePanel 상단 동일 높이
- 중앙 X `0.5`
- RectTransform Offset 초기화

---

## 최신 원격 저장소 검수

확인한 최신 `main` 커밋:

`557b8f611c9b8970b203a88979be43eb1745d16b`

현재 커밋 메시지:

`a`

최신 커밋에는 다음이 포함되어 있다.

- `Battle.unity`
- `BattleDebugPanel`
- `BattleHudButtonLabelUtility`
- `BattleHudButtonStyle`
- `BattleHudCardView` 확장
- `BattleHudState`
- `BattleTimeController`
- `BattleTopHudLayout`
- `BattleScreenController` Day16 변경
- `BattleUnitView` / `BattleEnemyView` Debug 표시 제어
- `Phase1Day16Setup`
- Day16 관련 EditMode Test

원격 코드에서 SpeedButton의 단일 Text 처리와 MENU 스타일 복사, TimePanel 중앙 정렬 코드와 관련 테스트가 포함된 것을 확인했다.

GitHub Commit Status에는 등록된 CI Status가 없다.

따라서 원격 파일 반영과 코드 구조는 확인했지만 다음 항목은 GitHub만으로 검증할 수 없다.

- Unity 실제 Compile
- Unity EditMode Test Runner 전체 통과
- Play Mode에서 실제 배속 전환
- MENU Pause / Resume 실제 동작
- 해상도별 HUD 최종 렌더링

현재 원격에는 `Devlogs/Day16/README.md`가 아직 존재하지 않는다.

---

## 16일차 완료 범위

- Battle HUD 상단 구조 정리
- 4인 캐릭터 HUD 카드 확장
- HP 상태 표시
- DOWN 상태 유지
- Skill UI 자리
- Ultimate Gauge UI 자리
- 전투 배속 `×1 / ×1.5 / ×2`
- MENU Pause / Resume
- Scene 이동 시 TimeScale 복원
- AUTO 이후 확장 API
- BattleDebugPanel
- Runtime Debug 정보 기본 숨김
- SpeedButton MENU 스타일 적용
- SpeedButton 단일 Text 상태 표시
- TimePanel 상단 중앙 정렬
- Day16 Editor Setup
- 관련 EditMode Test

Phase 1 Day 16 — Battle HUD 및 전투 조작 UI 구축.
