# Project H — Phase 1 Day 17 개발 일지

- 날짜: 2026-09-06
- 단계: Phase 1 / Day 17
- 기준 원격 커밋: `700161c0bd6871b2a4f14c38a39c04688ce53952`
- 기준 원격 커밋 메시지: `17`
- 주제: 스킬 블록 공통 시스템 구축

---

## 목표

16일차까지 구축한 Battle HUD 위에 실제 캐릭터 스킬 구현을 연결할 수 있는 공통 스킬 구조를 만든다.

17일차 핵심 범위:

- `SkillData` 공통 데이터 구조
- 캐릭터별 Skill 1·2·3 연결
- 전투 중 스킬 블록 자동 생성
- 최대 7개 블록 Queue
- 블록 드래그 순서 변경
- 동일 `SkillId` 인접 블록 강화
- 강화도 1~3 판정
- 클릭한 강화 그룹 소비
- 스킬 사용 요청과 실제 효과 실행부 분리
- Battle HUD 스킬 블록 패널
- 캐릭터 사망 시 해당 캐릭터 블록 제거
- Pause / 전투 종료 시 블록 조작 차단
- 이후 18~19일차 캐릭터별 실제 스킬 효과를 연결할 기반 준비

17일차에서는 공통 스킬 흐름을 완성하고, 실제 캐릭터별 피해·회복·버프·디버프 효과는 이후 일차에서 연결한다.

---

## SkillData 공통 구조

새로운 `SkillData` ScriptableObject를 추가했다.

지원 대상 종류:

- `Self`
- `NearestEnemy`
- `LowestHpAlly`
- `AllEnemies`
- `AllAllies`

지원 효과 종류:

- `None`
- `Damage`
- `Heal`
- `Buff`
- `Debuff`
- `Shield`
- `Special`

현재 Day17에서 생성되는 임시 SkillData는 실제 효과를 아직 연결하지 않기 때문에 기본 `effectType`은 `None`이다.

각 SkillData는 다음 기본 정보를 가진다.

- Skill ID
- 소유 Character ID
- Skill Slot 1~3
- 표시 이름
- 설명
- Target Type
- Effect Type
- 강화도별 데이터

---

## 스킬 강화도 데이터

스킬 블록 강화도는 최대 3단계다.

`SkillEnhancementData`는 강화도별 다음 값을 보관한다.

- `PowerRatio`
- `FlatValue`
- `UltimateGaugeGain`

현재 공통 임시 기본값:

| 강화도 | Power Ratio | Flat Value | Ultimate Gauge Gain |
| --- | ---: | ---: | ---: |
| 1 | 1.00 | 0 | 10 |
| 2 | 1.25 | 0 | 20 |
| 3 | 1.60 | 0 | 30 |

실제 캐릭터별 스킬 밸런스는 개별 스킬 구현 단계에서 조정한다.

---

## 전체 캐릭터 Skill 1·2·3 데이터

`Phase1Day17Setup`은 전체 CharacterData를 검색하여 캐릭터마다 Skill 1·2·3을 생성하고 연결한다.

ID 규칙:

`SK_{CHARACTER}_{01~03}`

예:

- `SK_SERENA_01`
- `SK_SERENA_02`
- `SK_SERENA_03`
- `SK_ELLEN_01`
- `SK_ELLEN_02`
- `SK_ELLEN_03`

현재 프로젝트의 12인 CharacterData에 각 3개의 SkillData가 연결되는 구조다.

기존 SkillData가 이미 존재하면 다시 생성하지 않고 기존 에셋을 보존한다.

---

## BattleSkillBlock

전투 중 표시되는 하나의 스킬 블록을 `BattleSkillBlock`으로 분리했다.

블록은 다음 정보를 가진다.

- Runtime Block ID
- SkillData 참조
- Skill ID
- Character ID
- Skill Slot

실제 전투에서는 SkillData를 참조하고, EditMode 순수 테스트에서는 별도의 테스트 블록 생성 경로를 사용할 수 있도록 구성했다.

---

## BattleSkillBlockQueue

전투 중 현재 보유 중인 스킬 블록 순서를 관리한다.

지원 기능:

- 블록 추가
- 최대 보유 수 제한
- 블록 순서 이동
- 지정 범위 블록 소비
- 특정 캐릭터 블록 일괄 제거
- 전체 초기화

Day17 기본 최대 블록 수:

`7`

Queue가 가득 차면 추가 블록 생성을 중단한다.

---

## 스킬 블록 자동 생성

`BattleSkillBlockController`가 전투 진행 중 일정 주기로 신규 블록을 생성한다.

Day17 기본 생성 주기:

`1.5초`

블록 생성 후보는 현재 전투에 등록된 살아있는 아군 캐릭터의 Skill 1·2·3에서 수집한다.

생성 조건:

- 아군이어야 함
- 전투 가능한 상태여야 함
- 생존 상태여야 함
- CharacterData가 존재해야 함
- SkillData가 연결되어 있어야 함
- Skill ID가 유효해야 함

후보 중 하나를 랜덤으로 선택하여 Queue 마지막에 추가한다.

전투가 끝나거나 양쪽 중 한 팀의 생존자가 없어지면 블록 생성을 중단한다.

---

## 동일 SkillId 강화 시스템

스킬 블록의 강화도는 같은 `SkillId`가 서로 인접해 있는 개수로 판정한다.

예:

`A`

→ 강화도 1

`A A`

→ 강화도 2

`A A A`

→ 강화도 3

최대 강화도는 3이다.

같은 SkillId가 4개 이상 연속되는 경우 3개 단위로 강화 그룹을 나눈다.

예:

`A A A A`

→ `A A A` 강화도 3 그룹 + `A` 강화도 1 그룹

강화도 판정은 `BattleSkillChainResolver`가 담당한다.

---

## 블록 드래그 순서 변경

플레이어는 스킬 블록을 드래그하여 Queue 순서를 변경할 수 있다.

블록 이동 후에는 전체 강화 그룹을 다시 계산한다.

따라서 서로 떨어져 있던 같은 SkillId 블록을 인접하게 배치하면 강화도가 올라갈 수 있다.

이 구조를 통해 Day17 스킬 시스템의 핵심 플레이 흐름을 다음처럼 구성한다.

`블록 생성 → 순서 변경 → 같은 SkillId 연결 → 강화도 상승 → 사용`

---

## 스킬 블록 사용

블록을 클릭하면 해당 블록이 속한 강화 그룹을 조회한다.

사용 흐름:

1. 클릭한 블록의 강화 그룹 판정
2. 대표 블록에서 Skill ID / Character ID / Skill Slot 조회
3. 강화도와 소비 블록 수를 `BattleSkillRequest`로 생성
4. `BattleSkillExecutor`에 실행 요청
5. 요청 성공 시 해당 강화 그룹 블록 소비
6. Queue와 UI 재갱신

스킬 실행에 실패한 경우에는 블록을 소비하지 않는다.

---

## BattleSkillExecutor

Day17에서는 실제 스킬 효과를 직접 실행하지 않고 공통 요청 전달 계층을 만든다.

실행 전 확인:

- 요청 데이터 유효성
- Battle Registry 존재 여부
- 스킬 소유 캐릭터가 현재 아군인지
- 소유 캐릭터가 전투 가능한 상태인지
- 소유 캐릭터가 생존 중인지

조건을 통과하면:

- 기존 `BattleActionKind.Skill` 행동 표시
- `SkillRequested` 이벤트 발생
- Skill ID / Character ID / Slot / 강화도 Debug 로그 기록

실제 피해·회복·버프·디버프 효과는 18~19일차에서 `SkillRequested`를 기준으로 연결할 수 있도록 분리했다.

---

## 캐릭터 사망과 스킬 블록

Battle Registry에서 아군이 제외되면 해당 캐릭터 ID의 보유 스킬 블록을 Queue에서 모두 제거한다.

따라서 사망하거나 전투에서 제거된 캐릭터의 스킬 블록이 이후에도 남아 사용되는 상황을 방지한다.

또한 전투가 끝나면 스킬 블록 UI 입력을 비활성화한다.

---

## Pause 상태 연동

16일차에서 구축한 `Time.timeScale` 기반 Pause와 스킬 블록 조작을 연동했다.

다음 상태에서는 블록을 조작할 수 없다.

- `Time.timeScale == 0`
- 아군 전멸
- 적 전멸
- Battle Registry가 준비되지 않은 상태

Pause 해제 후 전투가 다시 진행되면 블록 조작이 가능하다.

---

## Battle HUD 스킬 블록 패널

BattleScene 하단 HUD에 `SkillBlockPanel`을 추가했다.

구성:

- `SKILL BLOCKS` 제목
- 현재 블록 수 `0 / 7`
- 7개 빈 슬롯 배경
- Runtime Block Layer
- 블록 조작 안내 문구

기존 4인 프로필 카드는 왼쪽 하단 영역에 재배치하고, 16일차에 준비해 둔 카드별 `SKILL / LOCKED` 임시 버튼은 숨긴다.

실제 스킬 입력은 중앙 공통 스킬 블록 패널을 사용한다.

---

## Phase1Day17Setup

현재 BattleScene과 전체 CharacterData를 수동으로 수정하지 않아도 되도록 Editor Setup을 추가했다.

메뉴:

`Tools → Project H → Phase 1 → 17일차 스킬 블록 시스템 설정 실행`

주요 처리:

- `Assets/ProjectH/Data/Skills` 폴더 확보
- 전체 CharacterData 검색
- 캐릭터별 Skill 1·2·3 SkillData 생성
- CharacterData에 SkillData 참조 연결
- BattleScene 열기
- Battle Registry 확보
- SkillBlockPanel 생성 및 배치
- 7개 슬롯 UI 구성
- 기존 4인 프로필 카드 위치 조정
- Day16 카드별 Skill Locked UI 숨김
- BattleSkillExecutor 연결
- BattleSkillBlockController 연결
- 최대 블록 7개 설정
- 생성 주기 1.5초 설정
- BattleScene 및 생성 에셋 저장

Setup 완료 로그의 흐름:

`Generate → Drag → Same SkillId → Enhancement 1~3 → Consume`

---

## Day17 HUD 배치

17일차에서는 기존 Battle HUD와 스킬 블록 패널이 겹치지 않도록 전용 HUD 배치 규칙을 추가했다.

관련 테스트에서는 프로필 카드 및 SkillBlockPanel의 RectTransform 배치값을 검증한다.

목표는 다음 영역을 분리하는 것이다.

- 왼쪽 하단: 4인 캐릭터 프로필
- 오른쪽 하단: Skill Blocks
- 상단: Wave / Time / Speed / Menu

---

## EditMode Test

17일차 커밋에는 다음 테스트가 포함되어 있다.

### BattleSkillBlockQueueTests

- 최대 블록 수 제한
- 블록 추가
- 블록 순서 이동
- 블록 소비
- 캐릭터 ID 기준 블록 제거

### BattleSkillChainResolverTests

- 동일 SkillId 인접 강화 판정
- 서로 다른 SkillId 그룹 분리
- 최대 강화도 3 제한
- 클릭 위치 기준 강화 그룹 조회

### BattleSkillRequestTests

- SkillId / CharacterId / SkillSlot 전달
- 강화도 전달
- 소비 블록 수 전달

### BattleDay17HudLayoutTests

- Day17 프로필 카드 배치
- SkillBlockPanel 배치
- HUD 영역 분리

### BattlePartyRuntimeTests 보정

Day17 TestRunner 검수 중 다음 테스트가 실패했다.

`TryCreate_FailsWhenPartyExceedsFourMembers`

원인은 `SaveData.CreateNewGame()`이 초기 보유 캐릭터를 5명 전달받아도 활성 파티에는 최대 4명만 넣기 때문에, 기존 테스트가 실제 5인 활성 파티를 만들지 못한 데 있었다.

또한 `BattlePartyRuntime.TryCreate()`에서 `SaveData.EnsureDefaults()`를 먼저 실행하면 손상된 5인 활성 파티가 정규화될 가능성이 있었다.

최종 보정:

- `TryCreate()`에서 `EnsureDefaults()` 전에 4인 초과 검사
- 보정 이후에도 4인 초과 안전 검사 유지
- 테스트에서 Reflection을 사용해 의도적인 비정상 5인 활성 파티를 주입
- 정상 저장 생성 로직과 비정상 저장 방어 테스트의 책임 분리

현재 원격 `main`에는 이 보정 코드와 테스트가 반영되어 있다.

---

## 최신 원격 저장소 검수

확인한 최신 `main` 커밋:

`700161c0bd6871b2a4f14c38a39c04688ce53952`

현재 커밋 메시지:

`17`

원격에서 확인한 주요 반영 항목:

- 전체 CharacterData Skill 참조
- SkillData 및 강화도 공통 구조
- 캐릭터별 Skill 1·2·3 임시 데이터
- BattleSkillBlock
- BattleSkillBlockQueue
- BattleSkillChain / Resolver
- BattleSkillRequest
- BattleSkillExecutor
- BattleSkillBlockController
- BattleSkillBlockPanel / View
- BattleScene SkillBlockPanel
- Phase1Day17Setup
- Day17 HUD Layout
- 관련 EditMode Test
- 5인 파티 비정상 SaveData 방어 보정

`BattlePartyRuntime.cs`에는 정규화 전 4인 초과 방어 코드가 존재하며, `BattlePartyRuntimeTests.cs`에도 의도적인 5인 활성 파티를 주입하는 수정 테스트가 반영되어 있다.

GitHub Commit Status에는 등록된 CI Status가 없다.

따라서 원격 파일 반영과 코드 구조는 확인했지만 다음 항목은 GitHub만으로 검증할 수 없다.

- Unity 실제 Compile 전체 결과
- Unity EditMode Test Runner 전체 최종 통과
- Play Mode에서 1.5초 블록 생성 체감
- 실제 Drag 조작
- 동일 SkillId 강화 표시
- 스킬 사용 후 블록 소비
- Pause / Resume 상태 입력 전환
- 캐릭터 사망 시 보유 블록 제거
- 실제 해상도별 HUD 렌더링

현재 원격에는 `Devlogs/Day17/README.md`가 아직 존재하지 않는다.

---

## 17일차 완료 범위

- SkillData 공통 구조
- Skill Target / Effect Type 정의
- 강화도 1~3 데이터
- 전체 12인 Skill 1·2·3 기반 데이터
- Skill Block Runtime 데이터
- 최대 7개 Queue
- 1.5초 자동 블록 생성
- 생존 아군 Skill 후보 수집
- Drag 순서 변경
- 동일 SkillId 인접 강화
- 강화도 최대 3
- 클릭 그룹 사용 및 소비
- SkillRequest / Executor 분리
- 실제 효과 연결 이벤트
- 캐릭터 사망 블록 제거
- Pause / 전투 종료 조작 차단
- Battle HUD SkillBlockPanel
- Day17 Editor Setup
- Day17 EditMode Test
- 5인 파티 비정상 저장 방어 테스트 보정

Phase 1 Day 17 — 스킬 블록 공통 시스템 구축.
