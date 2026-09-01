# Project H — Phase 1 Day 11 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 11
- 기준 커밋: `6e541d8a8e9eed35beaf5d17734641f3f144176e`
- 커밋 당시 메시지: `11`
- 주제: BattleScene 전투 배치 기반 구축

---

## 목표

10일차에서 저장한 4인 파티 편성을 실제 BattleScene의 전장 배치와 연결한다.

11일차에서는 전투 행동 자체보다 전투가 진행될 Scene 구조와 캐릭터 배치 기반을 우선 구축한다.

기본 흐름:

`Party 편성 → SaveData.PartyCharacterIds → BattlePartyRuntime → BattleDeploymentPlan → ALLY_0 ~ ALLY_3 → BattleScene 배치`

실제 타겟팅과 기본 공격은 12일차에서 연결한다.

---

## BattleScene 전용 구조

기존 Prototype 전투 화면을 전용 BattleScene 구조로 교체했다.

주요 구조:

- Main Camera
- Battle Background
- BattleWorld
- BattleFormation
- AllySlot 4개
- EnemySlot 5개
- SpawnedAllies
- Enemy Preview
- BattleHUD
- BattleController

전투 화면은 좌측 아군 / 우측 적군의 2D 횡스크롤형 자동 전투 배치를 기준으로 구성한다.

---

## BattleFormationAnchors

전투 캐릭터와 적군이 배치될 위치를 코드에 직접 종속시키지 않도록 전용 Anchor 계층을 추가했다.

아군:

- `AllySlot_0`
- `AllySlot_1`
- `AllySlot_2`
- `AllySlot_3`

적군:

- `EnemySlot_0`
- `EnemySlot_1`
- `EnemySlot_2`
- `EnemySlot_3`
- `EnemySlot_4`

`BattleFormationAnchors`가 각 Transform을 보관하며 Slot Index로 배치 위치를 조회한다.

이를 통해 이후 캐릭터 위치와 간격은 Scene의 Transform 조정만으로 수정할 수 있다.

---

## Party 순서와 전투 배치 연결

10일차에서 확정한 Party Slot 순서를 그대로 BattleScene의 Ally Slot에 연결했다.

연결 규칙:

- Party Slot 1 → `ALLY_0` → `AllySlot_0`
- Party Slot 2 → `ALLY_1` → `AllySlot_1`
- Party Slot 3 → `ALLY_2` → `AllySlot_2`
- Party Slot 4 → `ALLY_3` → `AllySlot_3`

Tank / Dealer / Healer 역할에 따라 강제로 전투 위치를 변경하지 않는다.

플레이어가 Party 화면에서 정한 순서를 그대로 전투 배치 순서로 사용한다.

---

## BattleDeploymentPlan

`BattlePartyRuntime`과 `BattleFormationAnchors` 사이를 연결하는 `BattleDeploymentPlan`을 추가했다.

역할:

1. 전투 Party 존재 여부 확인
2. Ally Anchor 개수 확인
3. Party Slot 순서 확인
4. 각 `BattleStats`와 대응 Anchor 연결
5. 실제 Scene Spawn에 사용할 배치 목록 생성

Anchor가 부족하거나 누락된 경우 명확한 오류를 반환하도록 구성했다.

---

## BattleUnitView

각 아군 전투 캐릭터를 BattleScene에 표시하기 위한 최소 View를 추가했다.

현재 표시 정보:

- Character Display Name
- Runtime ID
- Tank / Dealer / Healer
- Current HP
- Max HP
- HP Bar

실제 SD 캐릭터 전투 Sprite가 아직 없으므로 역할별 색상의 임시 캐릭터 표시를 사용한다.

이후 실제 캐릭터 Sprite와 Animation을 적용할 때 전투 데이터 계층을 수정하지 않고 View만 교체할 수 있도록 분리했다.

---

## BattleScreenController

BattleScene 전용 Controller를 추가했다.

초기화 흐름:

`GameManager → DataManager / SaveManager → CurrentSave → BattlePartyRuntime.TryCreate() → BattleDeploymentPlan.TryCreate() → BattleUnitView Spawn`

성공 시 현재 Party 인원만큼 `ALLY_n` GameObject를 생성한다.

각 Unit은:

- 대응 Ally Anchor 위치 적용
- Runtime Stats 연결
- HP 표시
- 하단 HUD 연결

순서로 초기화된다.

BattleScene을 Bootstrap 없이 직접 실행하거나 SaveData가 없을 경우 NullReference로 중단하지 않고 개발용 오류 메시지를 출력하도록 처리했다.

---

## Main Camera 및 전투 배경

BattleScene에 전용 Main Camera를 배치했다.

현재 설정:

- 이름: `Main Camera`
- Tag: `MainCamera`
- Orthographic Camera
- AudioListener 포함

기존 Prototype UI의 `bg_battle.png`를 전투 Background로 재사용한다.

신규 전투 배경 이미지 에셋은 이번 일차에 추가하지 않았다.

---

## Enemy Preview

11일차는 아군 배치가 핵심이므로 실제 Monster Runtime과 Enemy AI는 아직 구현하지 않는다.

대신 우측 전장에 임시 Enemy Preview 3개를 배치한다.

Enemy Anchor 자체는 최대 5개까지 준비해 이후 Wave 및 Monster 구성에 대응할 수 있도록 했다.

실제 적 AI는 14일차 범위에서 연결한다.

---

## 상단 Battle HUD

전투 화면 상단에 최소 상태 UI를 추가했다.

- `WAVE 1 / 3`
- 전투 경과 시간
- `MENU`
- 개발 상태 표시

전투 시간은 BattleScene 초기화 성공 후 증가한다.

실제 Wave 진행은 아직 구현하지 않았으며 현재는 `WAVE 1 / 3` 고정 표시다.

---

## 하단 4인 HUD

화면 하단에 4개의 캐릭터 HUD Slot을 추가했다.

각 HUD에는 다음 영역을 준비했다.

- Portrait 자리
- Character Name
- Level
- Current HP / Max HP
- HP Bar
- Skill Gauge 자리

11일차에서는 Skill Gauge의 UI 자리만 준비하고 값은 초기 상태로 유지한다.

실제 Battle HUD 완성은 16일차 범위다.

---

## AUTO 및 MENU

첨부 전투 방향에 맞추어 우측에 AUTO 버튼을 배치했다.

현재 AUTO는 실제 전투 동작을 변경하지 않고 ON / OFF 표시만 변경한다.

실제 자동 타겟팅과 기본 공격은 12일차부터 연결한다.

MENU에서는 현재 최소 기능으로:

- 전투 메뉴 표시
- 메뉴 닫기
- 던전 선택 화면으로 복귀

를 지원한다.

---

## EditMode Test

11일차에 다음 테스트를 추가했다.

### BattleFormationAnchorsTests

- 아군 Anchor 4개 순서 유지
- 적군 Anchor 5개 순서 유지
- 잘못된 Slot Index의 null 처리

### BattleDeploymentPlanTests

- Party 순서와 Ally Slot 순서 일치
- `ALLY_0 ~ ALLY_3` 배치 순서 확인
- 부족한 Ally Anchor에 대한 실패 처리

### Phase1Day11BattleSceneLayoutTests

- BattleScene 존재
- Main Camera 존재
- MainCamera Tag
- Orthographic Camera
- BattleScreenController 존재
- BattleFormationAnchors 존재
- Ally Anchor 4개
- Enemy Anchor 5개

를 검증한다.

---

## 최신 저장소 검토

기준 커밋:

`6e541d8a8e9eed35beaf5d17734641f3f144176e`

최신 원격 저장소에서 확인한 주요 상태:

- `Battle.unity`가 커밋에 포함됨
- `BattleDeploymentPlan` 추가
- `BattleFormationAnchors` 추가
- `BattleHudCardView` 추가
- `BattleScreenController` 추가
- `BattleUnitView` 추가
- `Phase1Day11Setup` 추가
- EditMode Test 3종 추가
- BattleScene의 `Main Camera`가 `MainCamera` Tag로 존재
- `BattleController`가 Scene에 존재
- `BattleScreenController`의 `formationAnchors`, `allyRoot`, `unitTemplate` 참조가 연결됨
- 하단 HUD Card 4개가 Controller에 연결됨
- Ally Anchor 4개가 연결됨
- Enemy Anchor 5개가 연결됨

GitHub Commit Status에는 별도의 CI Status가 등록되어 있지 않다.

따라서 원격 저장소의 파일 및 Scene 직렬화 구조에서 11일차 진행을 막는 명확한 문제는 확인하지 못했다.

단, GitHub 원격 검토만으로 Unity Editor 실제 Compile, EditMode Test Runner, Play Mode 실행 결과까지 검증할 수는 없다.

---

## 11일차 완료 범위

- 2D 횡스크롤형 BattleScene 구조
- Main Camera
- Battle Background
- 아군 Anchor 4개
- 적군 Anchor 5개
- Party 순서 기반 Ally 배치
- `BattlePartyRuntime`과 BattleScene 연결
- `BattleDeploymentPlan`
- `BattleUnitView`
- 하단 4인 HUD
- HP 표시 기반
- Wave / Time / Menu
- AUTO 표시 기반
- 임시 Enemy Preview
- Scene 구조 EditMode Test

다음 12일차에서는 현재 배치된 아군과 적에게 Targeting 및 기본 공격 흐름을 연결한다.

Phase 1 Day 11 — BattleScene 전투 배치 기반 구축.
