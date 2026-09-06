# Project H — Phase 1 Day 23 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 23
- 기준 원격 커밋: `17446102f0fed0a1ce34a2e8ce267766959bc0f3`
- 기준 원격 커밋 메시지: `23`
- 주제: 캐릭터 수동 회피 이동·최근접 타겟 추적 및 선택 시각화 시스템 구축

---

## 목표

22일차까지 구축한 자동 전투 흐름에 플레이어가 직접 개입할 수 있는 수동 회피 이동을 추가한다.

23일차에서는 숫자키 `1~4`로 파티 캐릭터를 선택하고 우클릭한 전장 위치로 해당 캐릭터를 직접 이동시킨 뒤, 목적지 도착 후 다시 기존 자동 전투로 복귀하도록 구성했다.

수동 이동 이후에는 캐릭터의 현재 위치를 기준으로 가장 가까운 생존 몬스터를 다시 탐색하고, 선택 캐릭터와 이동 목적지를 쉽게 확인할 수 있도록 전장·HUD 윤곽 및 목적지 화살표 시각 효과도 추가했다.

핵심 방향:

- 숫자키 `1~4`로 파티 생성 순서의 캐릭터를 선택한다.
- 우클릭으로 선택 캐릭터의 X/Y 수동 이동 목적지를 지정한다.
- 수동 이동 중 해당 캐릭터의 자동 이동·기본 공격을 일시 중지한다.
- 목적지 도착 후 자동 전투를 즉시 재개한다.
- 여러 캐릭터에게 서로 다른 목적지를 연속으로 지정할 수 있게 한다.
- 수동 이동 후 현재 위치 기준 가장 가까운 생존 몬스터를 다시 추적한다.
- 좌우 방향과 관계없이 최근접 몬스터에게 접근할 수 있게 한다.
- 우클릭 목적지를 노란색 화살표로 표시한다.
- 숫자키로 선택된 캐릭터의 전장 바디와 HUD 초상화 영역에 금색 윤곽을 표시한다.
- 사망, 전투 불가, 시스템 제거 시 수동 이동 및 선택 시각 상태를 안전하게 해제한다.

---

## BattleManualMoveController

Day23의 수동 조작 핵심 관리자를 추가했다.

`BattleManualMoveController`는 `BattleCombatRegistry` 오브젝트에 Runtime으로 자동 연결되며, 각 아군 `BattleBasicAttackController`가 설정될 때 파티 슬롯 순서대로 등록된다.

따라서 현재 구조에서는 Scene 또는 Prefab에 Day23 전용 컴포넌트를 직접 배치할 필요가 없다.

주요 역할:

- 파티 최대 4인 슬롯 등록
- 숫자키 및 숫자패드 `1~4` 입력 처리
- 현재 선택 슬롯 관리
- 우클릭 입력 처리
- 화면 좌표를 전장 월드 좌표로 변환
- 캐릭터별 독립 목적지 저장
- X/Y 평면 수동 이동
- 목적지 도착 판정
- 자동 전투 일시 중지 및 복귀
- 목적지 화살표 표시 및 제거
- 선택 캐릭터 Runtime 상태 갱신

---

## 숫자키 캐릭터 선택

파티 생성 순서를 기준으로 다음 입력을 연결했다.

- `1`: 1번 파티 캐릭터
- `2`: 2번 파티 캐릭터
- `3`: 3번 파티 캐릭터
- `4`: 4번 파티 캐릭터

상단 숫자열과 숫자패드 입력을 모두 지원한다.

선택 가능한 대상은 생존 상태이며 현재 전투에 활성화된 아군으로 제한한다.

사망한 캐릭터를 선택하거나 기존 선택 캐릭터가 전투 불가 상태가 되면 현재 선택을 해제한다.

---

## Input System 연결

Day23 입력은 기존 Unity 구형 입력 API 대신 `UnityEngine.InputSystem`을 사용한다.

입력 구성:

- `Keyboard.current`
- `digit1Key ~ digit4Key`
- `numpad1Key ~ numpad4Key`
- `Mouse.current.rightButton`
- `Mouse.current.position`

초기 Day23 적용 과정에서 `UnityEngine.InputSystem` Namespace를 찾지 못하는 `CS0234` 오류가 발생했기 때문에 `ProjectH.Runtime.asmdef`의 Assembly 참조도 함께 수정했다.

현재 Runtime Assembly 참조:

- `Unity.ugui`
- `Unity.InputSystem`

패키지를 코드에서 사용하면서 Assembly Definition이 해당 Assembly를 참조하도록 정리했다.

---

## 우클릭 수동 이동

캐릭터를 선택한 뒤 전장 빈 공간을 우클릭하면 선택 캐릭터의 수동 이동이 시작된다.

처리 흐름:

`숫자키 선택`

→ `우클릭`

→ `ScreenPointToRay`

→ `캐릭터 Z축 기준 전투 Plane 교차`

→ `X/Y 목적지 저장`

→ `자동 전투 일시 중지`

→ `MoveTowards로 목적지 이동`

→ `목적지 도착`

→ `자동 전투 재개`

수동 이동은 기존 X축 자동 전진과 달리 X/Y 두 축을 모두 사용한다.

캐릭터의 기존 `Stats.MoveSpeed`를 이동 속도로 사용하며 별도 수동 회피 배율은 추가하지 않았다.

---

## 여러 캐릭터 독립 이동

한 캐릭터가 수동 이동 중이어도 다른 숫자키로 새로운 캐릭터를 선택해 별도의 목적지를 지정할 수 있다.

예시:

`1 → 우클릭 A`

`2 → 우클릭 B`

`3 → 우클릭 C`

각 슬롯은 별도의 목적지와 이동 상태를 보관하므로 여러 파티원이 동시에 서로 다른 위치로 이동할 수 있다.

같은 캐릭터가 이동 중 다시 우클릭되면 해당 슬롯의 목적지가 새로운 위치로 갱신된다.

---

## 수동 이동 중 자동 전투 정지

`BattleBasicAttackController`에 수동 이동 일시정지 상태를 추가했다.

추가 API:

- `BeginManualMove()`
- `EndManualMove()`
- `IsManualMoveSuspended`

수동 이동을 시작하면 기존 타겟과 공격 진행 상태를 초기화하고 자동 기본 공격 Controller의 Update 처리를 중지한다.

수동 이동이 끝나면 정지 상태를 해제하고 공격 상태를 `Idle`로 되돌려 현재 위치에서 타겟을 새로 탐색하도록 한다.

이를 통해 캐릭터가 목적지로 이동하는 동안 자동 전진이 수동 이동을 덮어쓰지 않도록 했다.

---

## 목적지 도착 후 자동 전투 복귀

목적지 도착 시 `EndManualMove()`가 호출되고 기존 자동 기본 공격 흐름으로 복귀한다.

복귀 직후 기존 타겟을 그대로 유지하지 않고 새 위치에서 타겟을 다시 획득한다.

따라서 패턴 회피를 위해 전장 위치가 크게 변경된 이후에도 이동한 위치를 기준으로 전투가 이어진다.

---

## 현재 위치 기준 최근접 몬스터 선택

수동 이동 이후 기존 전방 우선 타겟 정책 때문에 가까운 몬스터가 있어도 해당 몬스터에게 접근하지 않는 문제가 있어 `BattleTargetSelector`를 현재 위치 중심 정책으로 수정했다.

현재 타겟 선택 기준:

- 자신 제외
- 반대 팀
- 전투 초기화 완료
- 생존 상태
- 현재 캐릭터와의 가로 절대 거리가 가장 짧은 대상

기존처럼 전방에 있는 몬스터를 우선하지 않는다.

따라서 수동 이동으로 몬스터를 지나친 경우에도 뒤쪽에 있는 몬스터가 실제로 더 가깝다면 해당 몬스터를 선택할 수 있다.

---

## 아군 좌우 타겟 접근

기존 `BattleActor.MoveForwardToward()`는 팀의 전진 방향만 허용하기 때문에 뒤쪽의 최근접 몬스터를 선택해도 해당 위치로 이동할 수 없었다.

이를 위해 아군 자동 전투에 `BattleActor.MoveToward()`를 추가했다.

`MoveToward()`는:

- 현재 X 위치와 타겟 X 위치의 부호를 계산한다.
- 좌우 어느 방향이든 타겟 쪽으로 이동한다.
- 공격 사거리만큼 떨어진 정지선을 계산한다.
- `Mathf.MoveTowards()`로 해당 정지선까지 접근한다.

적군 AI는 기존 `MoveForwardToward()` 흐름을 유지하고, 아군만 방향과 무관한 최근접 타겟 추적을 사용한다.

---

## 우클릭 목적지 화살표

수동 이동 명령을 내린 위치를 확인할 수 있도록 슬롯별 목적지 화살표를 Runtime으로 생성한다.

구성:

- `LineRenderer` 사용
- 노란색 표시
- 화살표 몸통과 양쪽 날개를 5개 점으로 구성
- 전장 캐릭터보다 앞에 보이도록 높은 Sorting Order 사용
- `Sprites/Default` Shader 기반 Runtime Material 사용

화살표는 이동 명령 성공 시 표시되고 다음 상황에서 숨겨진다.

- 목적지 도착
- 이동 취소
- 캐릭터 사망 또는 전투 불가
- 이전 전투 슬롯 정리

각 파티 슬롯마다 독립 `LineRenderer`를 보관하므로 여러 캐릭터가 동시에 이동할 때 각자의 목적지 화살표를 유지할 수 있다.

---

## UI 위 우클릭 차단

`EventSystem.current.IsPointerOverGameObject()`를 확인해 HUD, 버튼 등 UI 위에서 발생한 우클릭은 수동 이동 명령으로 처리하지 않는다.

이를 통해 전투 UI 조작과 전장 이동 명령의 입력 충돌을 줄였다.

---

## 스턴 및 사망 처리

수동 이동 명령 시 현재 캐릭터가 스턴 상태면 이동 명령을 차단한다.

이미 이동 중인 캐릭터가 스턴되면 스턴이 해제될 때까지 현재 위치를 유지하고 목적지 이동을 진행하지 않는다.

이동 중 사망하거나 전투 불가 상태가 되면:

- 수동 이동 상태 해제
- 목적지 화살표 숨김
- 자동 공격 Controller 잠금 해제
- 현재 선택 캐릭터라면 선택 상태 및 윤곽 해제

를 수행한다.

---

## BattleSelectionRuntimeState

숫자키 선택 상태를 전장 캐릭터와 HUD가 함께 사용할 수 있도록 별도 Runtime 상태를 추가했다.

관리 정보:

- `SelectedRuntimeId`
- `SelectionChanged`
- `IsSelected(runtimeId)`
- `SetSelected(runtimeId)`
- `Clear()`
- `ResetAll()`

같은 캐릭터를 반복 선택할 때는 중복 변경 이벤트를 발생시키지 않는다.

Play Mode 진입 시 `SubsystemRegistration` 단계에서 선택 상태를 초기화해 이전 실행의 static 상태가 남지 않도록 했다.

---

## 전장 캐릭터 선택 윤곽

`BattleActor`의 바디 이미지에 Runtime `UnityEngine.UI.Outline`을 추가해 현재 선택된 아군을 표시한다.

현재 효과:

- 금색 계열 윤곽
- 바디 Image에 적용
- 선택된 생존 아군만 활성화
- 다른 캐릭터를 선택하면 기존 윤곽 자동 해제
- 선택 캐릭터 사망 시 자동 해제

`BattleSelectionRuntimeState.SelectionChanged` 이벤트를 구독해 선택 상태가 바뀔 때 즉시 시각 상태를 갱신한다.

---

## HUD 초상화 선택 윤곽

`BattleHudCardView`도 동일한 선택 Runtime 이벤트를 구독한다.

현재 프로젝트의 HUD 초상화는 실제 Sprite Image가 아니라 `portraitText` 기반 임시 초상화이므로 현재 단계에서는 해당 초상화 Graphic에 Runtime `Outline`을 적용한다.

동작:

- 선택 캐릭터와 동일한 Runtime ID의 HUD 카드 윤곽 활성화
- 다른 캐릭터 선택 시 이전 HUD 윤곽 해제
- 사망 캐릭터 윤곽 해제
- HUD 제거 시 선택 이벤트 구독 안전 해제

향후 실제 초상화 Image로 교체되더라도 동일한 Graphic 선택 표시 방향으로 확장할 수 있는 구조다.

---

## EditMode Test

Day23 기능을 검증하기 위한 EditMode 테스트 코드가 원격 저장소에 반영되어 있다.

### BattleManualMoveControllerTests

검증 코드 범위:

- 파티 생성 순서와 숫자키 슬롯 연결
- 수동 이동 중 목적지 화살표 표시
- 도착 후 목적지 화살표 숨김
- X/Y 목적지 도착
- 수동 이동 중 자동 전투 정지
- 도착 후 자동 전투 복귀
- 복귀 후 `Idle` 상태 확인
- 여러 아군의 독립 목적지 이동
- 여러 아군의 독립 목적지 화살표 표시
- 이동 중 사망 시 이동 취소
- 사망 취소 후 화살표 숨김
- 사망 후 자동 공격 잠금 해제
- 사망 캐릭터 재선택 차단

### BattleTargetSelectorTests

검증 코드 범위:

- 현재 위치 기준 가장 가까운 생존 적 선택
- 같은 팀 제외
- 사망 적 제외
- 뒤쪽 적이라도 실제 거리가 더 가까우면 해당 적 선택
- 생존 상대가 없으면 `null` 반환

### BattleForwardAdvanceTests

기존 전진 이동 테스트에 방향과 무관한 아군 접근 동작에 대한 검증 범위를 추가했다.

기존 적군 전진 정지선과 Lane 유지 규칙도 함께 유지한다.

### BattleSelectionHighlightTests

검증 코드 범위:

- 슬롯 선택 시 `SelectedRuntimeId` 저장
- 선택 캐릭터 전장 바디 윤곽 활성화
- 선택 캐릭터 HUD 초상화 윤곽 활성화
- 다른 슬롯 선택 시 이전 전장 윤곽 해제
- 다른 슬롯 선택 시 이전 HUD 윤곽 해제
- 신규 선택 캐릭터와 HUD 윤곽 활성화
- 선택 캐릭터 사망 후 선택 Runtime 상태 해제
- 사망 후 전장 및 HUD 윤곽 해제

---

## 생성 및 수정 파일

수정 Runtime:

- `Assets/ProjectH/Scripts/Battle/BattleActor.cs`
- `Assets/ProjectH/Scripts/Battle/BattleBasicAttackController.cs`
- `Assets/ProjectH/Scripts/Battle/BattleHudCardView.cs`
- `Assets/ProjectH/Scripts/Battle/BattleTargetSelector.cs`
- `Assets/ProjectH/Scripts/ProjectH.Runtime.asmdef`

신규 Runtime:

- `Assets/ProjectH/Scripts/Battle/BattleManualMoveController.cs`
- `Assets/ProjectH/Scripts/Battle/BattleSelectionRuntimeState.cs`

수정 EditMode Test:

- `Assets/ProjectH/Tests/EditMode/BattleForwardAdvanceTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleTargetSelectorTests.cs`

신규 EditMode Test:

- `Assets/ProjectH/Tests/EditMode/BattleManualMoveControllerTests.cs`
- `Assets/ProjectH/Tests/EditMode/BattleSelectionHighlightTests.cs`

신규 C# 파일에는 대응하는 Unity `.meta` 파일도 함께 추가했다.

---

## 검수 상태

Day22 기준 커밋 `61eb9ce971c1be2931504199e4a02cc4c3ca9b2a`와 최신 Day23 커밋 `17446102f0fed0a1ce34a2e8ce267766959bc0f3`을 비교해 Day23 변경 파일이 하나의 커밋에 반영되어 있는 것을 확인했다.

원격 코드에서 다음 항목을 확인했다.

- `BattleManualMoveController` 수동 이동 및 입력 코드 반영
- `Unity.InputSystem` Runtime Assembly 참조 반영
- 수동 이동 중 자동 전투 일시정지 및 복귀 코드 반영
- 현재 위치 기준 최근접 타겟 선택 코드 반영
- 아군 좌우 타겟 접근 코드 반영
- 우클릭 목적지 `LineRenderer` 화살표 반영
- `BattleSelectionRuntimeState` 반영
- 전장 캐릭터 `Outline` 반영
- HUD 초상화 `Outline` 반영
- 관련 EditMode 테스트 코드 반영

GitHub Commit Status에는 별도 CI 상태가 등록되어 있지 않았다.

따라서 이 개발 일지는 원격 소스와 테스트 코드 반영 상태를 기준으로 작성했으며, Unity Editor 전체 Compile 및 Test Runner 실행 성공을 별도로 주장하지 않는다.

현재 원격의 `Devlogs/Day23/README.md`는 존재하지 않아 이번 ZIP에서 새로 추가한다.

---

## Day 23 완료 상태

22일차까지의 자동 전투 중심 흐름에 23일차부터 플레이어 직접 위치 제어가 추가됐다.

현재 흐름:

`1~4 숫자키 캐릭터 선택`

→ `전장 캐릭터 + HUD 초상화 금색 윤곽 표시`

→ `전장 우클릭`

→ `목적지 화살표 표시`

→ `해당 캐릭터 자동 전투 일시 중지`

→ `X/Y 수동 이동`

→ `목적지 도착 및 화살표 제거`

→ `자동 전투 재개`

→ `현재 위치 기준 최근접 생존 몬스터 재탐색`

→ `좌우 방향과 무관하게 공격 사거리까지 자동 접근`

이를 통해 향후 보스 패턴, 장판, 범위 공격 등 위치 기반 전투 기믹을 캐릭터 단위로 직접 회피할 수 있는 기본 조작 기반이 마련됐다.
