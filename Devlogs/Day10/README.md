# Project H — Phase 1 Day 10 개발 일지

- 날짜: 2026-09-01
- 단계: Phase 1 / Day 10
- 기준 커밋: `802b0eeb65b0c7d8c2cc3bdc99a8c65f6a9c58d6`
- 커밋 당시 메시지: `10`
- 주제: 파티 편성 및 캐릭터 선택 시스템 구축

---

## 목표

기존 Prototype Party 화면을 실제 4인 파티 편성 화면으로 교체하고, 보유 캐릭터 중 원하는 캐릭터를 선택해 슬롯을 변경할 수 있는 편성 시스템을 구축한다.

기본 흐름:

`Lobby → Party → 4인 편성 → 캐릭터 슬롯 선택 → Character Select Popup → 캐릭터 교체 → 편성 확정 → SaveData → BattlePartyRuntime`

---

## 4인 파티 편성

Party 화면에 최대 4명의 캐릭터를 배치하는 전용 편성 UI를 구축했다.

각 슬롯은 다음 정보를 사용한다.

- Character ID
- Character Display Name
- Character Level
- Portrait 표시 영역
- 편성 Slot 순서

현재 저장된 Party 순서가 그대로 전투의 `ALLY_0 ~ ALLY_3` 순서로 연결된다.

---

## 캐릭터 선택 Popup

메인 Party Slot을 클릭하면 보유 캐릭터 선택 Popup이 열린다.

Popup 주요 기능:

- 현재 편집 Slot 표시
- 보유 캐릭터 카드 목록
- 캐릭터 이름
- Level
- Tank / Dealer / Healer 역할 표시
- 현재 Slot 캐릭터 표시
- 다른 Slot 편성 중 표시
- 캐릭터 선택
- Slot 비우기
- 취소

보유 캐릭터 목록은 `SaveData.Characters`를 기준으로 생성하므로 전체 CharacterData가 12개 존재해도 실제 획득하지 않은 캐릭터는 선택 목록에 나타나지 않는다.

---

## 역할 필터

캐릭터 선택 Popup에 다음 역할 필터를 추가했다.

- 전체
- 탱커
- 딜러
- 힐러

캐릭터의 `BattlePosition`을 기준으로 목록을 필터링한다.

메인 Party Slot에서는 사용자가 직접 정리한 현재 Scene 디자인에 따라 역할 배지와 역할 텍스트를 제거했지만, 캐릭터 선택 Popup의 역할 표시는 유지한다.

---

## 중복 편성 방지

같은 파티에 동일 캐릭터를 두 번 편성할 수 없도록 처리했다.

다른 Slot에 이미 포함된 캐릭터는 Popup에서:

`● 편성 중`

상태로 표시되고 선택할 수 없다.

현재 편집 중인 Slot에 이미 존재하는 캐릭터는:

`✓ 현재 선택`

으로 표시된다.

---

## 빈 Slot

Party는 1~4명으로 구성할 수 있다.

기존 캐릭터 Slot을 비우면 뒤쪽 캐릭터가 앞으로 정렬된다.

최소 1명의 캐릭터는 반드시 유지하도록 제한해 완전히 빈 Party가 저장되지 않도록 했다.

---

## PartyEditState

화면에서 캐릭터를 선택할 때마다 SaveData를 직접 수정하지 않도록 임시 편집 계층을 추가했다.

구조:

`SaveData → PartyEditState → 사용자 편집 → 편성 확정 → SaveData 반영 → SaveCurrent`

따라서 편집 중인 변경사항과 실제 저장된 Party 상태가 분리된다.

---

## 편성 Preset

4개의 파티 Preset을 지원하도록 SaveData를 확장했다.

- 편성 #1
- 편성 #2
- 편성 #3
- 편성 #4

각 Preset은 최대 4개의 Character ID를 보관한다.

선택한 Preset이 현재 활성 Party가 되며 기존 `PartyCharacterIds`와 동기화된다.

이를 통해 기존 Battle Runtime 코드가 별도 변경 없이 현재 선택 Party를 사용할 수 있다.

---

## 기존 Save 호환

기존 저장 파일에는 Party Preset 정보가 존재하지 않으므로 `EnsureDefaults()`에서 기본값을 생성한다.

기존 `PartyCharacterIds`를 기준으로 4개의 Preset을 구성하고, 첫 번째 Preset을 초기 선택 상태로 사용한다.

현재 Save Version은 기존 버전을 유지한다.

---

## Party Scene 디자인

Party Scene은 밝은 판타지 모바일 RPG UI 방향으로 구성했다.

구조:

- 상단 편성 Header
- 편성 설명
- Lobby 이동
- Help
- 재화 표시
- Main Camera
- 4개 Party Slot
- 편성 #1 ~ #4
- 편성 확정
- 던전 선택
- Character Select Popup

기존 Project H Prototype UI 이미지를 재사용하며 신규 이미지 에셋은 추가하지 않았다.

---

## Camera 및 Layout 수정

초기 Day 10 Party Scene에는 Camera가 생성되지 않아 Game View에서:

`Display 1 / No cameras rendering`

문구가 표시됐다.

이후 Scene 자동 생성기에 Main Camera 생성을 추가했다.

현재 Main Camera:

- 이름: `Main Camera`
- Tag: `MainCamera`
- Orthographic
- AudioListener 포함

또한 주요 UI 정렬을 보정했다.

- 상단 설명 중앙 정렬
- Party 상태 문구 중앙 정렬
- 4인 Slot 간격 조정
- Level / 이름 영역 정렬
- 편성 #1 ~ #4 버튼 중앙 그룹 배치
- Preset 상태 문구 중앙 배치
- 편성 확정 / 던전 선택 버튼 중앙 배치
- Popup 역할 Filter 중앙 배치
- 공통 Text의 Geometry Alignment 적용

---

## 현재 Party Scene 수동 정리

최신 Party Scene에서는 메인 4개 Party Slot의 일부 장식 요소를 수동으로 제거했다.

제거된 참조:

- `roleBadge`
- `roleText`
- `hintText`

현재 4개 `PartySlotView`에서 위 필드는 모두 null 상태다.

남아 있는 핵심 참조:

- `slotButton`
- `portraitText`
- `levelText`
- `nameText`

`PartySlotView`는 Text 참조를 null 검사 후 갱신하고, `roleBadge`도 null 검사 후 처리하도록 작성되어 있어 현재 삭제 상태가 Runtime NullReference를 직접 발생시키는 구조는 아니다.

따라서 메인 편성 화면에서 역할 배지와 하단 교체 안내 문구를 표시하지 않는 현재 디자인은 기능적으로 유지 가능하다.

Character Select Popup의 `PartyCharacterCardView`에는 Role Badge / Role Text / 상태 Text가 그대로 연결되어 있어 캐릭터 선택 시 역할 정보는 계속 확인할 수 있다.

### Scene 재구성 주의

`Phase1Day10Setup`은 현재도 메인 Slot의 Role Badge / Role Text / Hint Text를 생성한다.

따라서:

`Tools > Project H > Phase 1 > 10일차 파티 편성 Scene 재구성`

메뉴를 다시 실행하면 수동으로 제거한 요소가 다시 생성된다.

현재 수동 정리 상태를 유지하려면 해당 메뉴를 다시 실행한 뒤 동일 요소를 다시 제거하거나, 이후 Setup 스크립트의 생성 규칙을 현재 Scene 디자인과 맞춰 정리해야 한다.

---

## EditMode Test

Day 10에 다음 테스트를 추가했다.

### PartyPresetSaveDataTests

- 6인 보유 상태에서 초기 4인 Party 구성
- 4개 Preset 생성
- 중복 캐릭터 방지
- 미보유 캐릭터 방지
- Preset 선택과 Active Party 동기화
- 기존 Save의 Preset 마이그레이션

### PartyEditStateTests

- Slot 캐릭터 교체
- 동일 Party 중복 캐릭터 방지
- Slot 제거
- Party 압축
- 편집 상태 SaveData 반영

### PartyRosterFilterTests

- 전체
- Tank
- Dealer
- Healer

역할 필터 동작을 검증한다.

### PartyPresetBattleRuntimeTests

편성 화면에서 저장한 Party 순서가 `BattlePartyRuntime`에서 동일한 순서로 생성되는지 확인한다.

### Phase1Day10PartySceneLayoutTests

- Party Scene의 Main Camera 존재
- MainCamera Tag
- 편성 #1 ~ #4 중앙 배치
- Preset Label 중앙 정렬

을 검증한다.

---

## 최신 저장소 검토

기준 커밋:

`802b0eeb65b0c7d8c2cc3bdc99a8c65f6a9c58d6`

현재 저장소에서 확인한 핵심 상태:

- Day 10 Party 편성 코드 존재
- Party Scene 존재
- Main Camera 존재
- MainCamera Tag 존재
- PartyScreenController의 핵심 Serialized Reference 연결
- Party Slot 4개 연결
- Preset Button 4개 연결
- Character Select Popup 연결
- 역할 Filter 4개 연결
- Clear / Cancel Button 연결
- Main Party Slot의 Role Badge / Role Text / Hint Text는 의도적으로 제거된 상태
- 해당 null 참조를 PartySlotView가 안전하게 처리
- Character Select Popup의 역할 표시 요소는 유지
- GitHub Commit Status에 별도의 CI 검사 없음

저장소 정적 구조 기준으로 Day 10 진행을 막는 명확한 기능 문제는 확인하지 못했다.

다만 Unity Editor 실제 Compile, EditMode Test Runner, Play Mode 전체 편성 흐름은 GitHub 상태만으로 자동 검증할 수 없다.

---

## Day 10 완료 기준

- PartyScreenController 구축
- 최대 4인 Party 편성
- 보유 캐릭터 선택 Popup
- Tank / Dealer / Healer Filter
- 동일 캐릭터 중복 편성 방지
- 빈 Slot 및 최소 1인 규칙
- PartyEditState 기반 임시 편집
- 편성 #1 ~ #4 Preset
- 편성 확정 및 SaveCurrent
- 기존 Save Preset 보정
- BattlePartyRuntime과 Party 순서 연동
- Party Scene Main Camera 추가
- Party UI 중앙 정렬 보정
- 현재 수동 정리된 Party Slot 디자인 기능 호환 확인

Phase 1 Day 10 — 파티 편성 및 캐릭터 선택 시스템 구축.
