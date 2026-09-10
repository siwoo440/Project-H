# 프로젝트 H : 47일차 개발 로그

## 오늘 목표

47일차의 목표는 원래 "던전 클리어 기록 및 해금"이었다. 코드를 확인해 보니 클리어 기록/순차 해금은 이미 이전 작업에서 완성되어 있어, 대신 다음 세 가지를 진행했다.

1. 아직 없던 **던전 최고 별점 기록** 추가
2. 프로젝트 전반의 **불필요한 파일·중복 코드 정리(최적화)**
3. 실제 플레이 스크린샷에서 발견된 **UI 겹침 버그 수정**

## GitHub 기준

- 작업 시작 전 기준 커밋: `d27fcda` (`46일차 : 프로토타입 보스 몬스터 및 등장 연출·체력바 구축`)
- 브랜치: `main`

## 1. 던전 클리어 기록/해금 현황 확인

`DungeonProgressionPolicy`, `DungeonProgressSaveAdapter`, `DungeonProgressUiRuntimePatch`가 이미 존재했다 — 던전 클리어 시 `SYS_DUNGEON_CLEAR:` 플래그가 자동 저장되고, 순차 해금(DG001→DG002→DG003→DG004)과 LOCKED/AVAILABLE/CLEARED UI 표시까지 전부 동작 중이었다. Day45의 다중 웨이브·Day46의 보스도 "마지막 웨이브까지 이겨야 승리"로 설계되어 있어 자연스럽게 호환된다.

## 2. 던전 최고 별점 기록 추가

`SYS_DUNGEON_CLEAR:` 플래그와 동일한 패턴으로 `DungeonProgressSaveAdapter`에 `SYS_DUNGEON_STARS:{던전ID}:{별점}` 플래그 기반 최고 기록을 추가했다.

```csharp
GetBestStars(saveData, dungeonId)              // 최고 별점 조회
TrySetBestStars(saveData, dungeonId, stars)    // 더 높은 기록일 때만 갱신
```

`BattleResultCommitService`가 승리 시 클리어 플래그와 함께 별점도 반영하며, `DungeonProgressUiRuntimePatch`가 던전 선택 카드와 상세 문구에 `★★☆` 형태로 최고 기록을 표시한다. 회귀 테스트 6개를 `DungeonProgressionTests`에 추가했다.

## 3. 불필요한 파일 제거

- `Assets/Scenes/SampleScene.unity`: Unity 기본 템플릿 잔재. **EditorBuildSettings에 실제로 등록되어 빌드에 포함되고 있었음** — 제거
- `Assets/Settings/` 전체(URP 렌더러 애셋, 씬 템플릿): `GraphicsSettings.asset` 확인 결과 프로젝트는 Built-in RP를 사용 중이라(`m_CustomRenderPipeline: {fileID: 0}`) 미사용 상태였음 — 제거
- `EditorBuildSettings.asset`에서 SampleScene 항목도 함께 제거
- 총 1,000줄 이상 삭제

## 4. Runtime UI 헬퍼 중복 제거

`CreateImage`/`SetRect`/`Stretch`가 `BagScreenController`, `CharacterEquipmentScreenController`, `DungeonSelectScreenController`, `ShopScreenController`, `BattleResultOverlay`, `BattleBossPresentationController`, `BattleSkillBlockPanel` 7개 파일에 거의 동일하게 중복 구현되어 있었다. 한 줄씩 대조해 완전히 동일한 로직임을 확인한 뒤 `RuntimeUiKit` 공용 클래스로 추출하고, 각 파일은 위임(delegate) 형태로 교체했다.

```csharp
private static void Stretch(RectTransform rect) => RuntimeUiKit.Stretch(rect);
```

`CreateText`/`CreateButton`은 파일마다 `resizeTextForBestFit`, `overflow` 설정 등 실제 동작 차이가 있어 일부러 합치지 않았다 — 겉보기엔 비슷해도 억지로 합치면 미묘하게 UI 동작이 달라질 위험이 있었기 때문이다.

## 5. 실제 플레이에서 발견된 UI 겹침 버그 수정

사용자가 공유한 스크린샷 3장에서 아래 문제를 확인하고 원인을 특정해 수정했다.

### Battle 화면 상단 겹침
`DungeonBattleDebugOverlay`(개발용 던전 정보창)가 실제 전투 HUD(WAVE·상태·시간, y 0.91~0.985)와 같은 좌상단 자리에 더 높은 sortingOrder로 그려지고 있었다. 이 문제는 이번 세션 이전부터 있던 버그였다. 디버그 패널을 HUD 줄 아래로 이동해 해결했다.

### SKILL BLOCKS 패널 위 AUTO ON 버튼 겹침
11일차에 배치한 `AutoButton`이 17일차에 나중에 추가된 `SkillBlockPanel` 영역 안에 그대로 남아있었다. `Phase1Day11Setup.cs`(재구성용 원본 스크립트)와 실제 `Battle.unity` 씬 파일을 함께 수정해 AutoButton을 SkillBlockPanel 위쪽 빈 공간으로 옮겼다.

### Lobby Vitality/시간 진행 UI 디자인 불일치 및 겹침
Day41~42에 추가한 두 디버그 UI가 임의의 색상 사각형이라 Gold/Crystal 칩의 둥근 필(pill) 디자인과 이질적이었고, Vitality 패널은 캐릭터 초상화 프레임(HeroIllustrationFrame) 위에 겹쳐 있었다. Lobby 씬의 GoldChip 스프라이트를 런타임에 찾아 그대로 복사해 동일한 디자인을 적용하고, 초상화 프레임·PartySummary·Title/Save 버튼의 좌표를 전부 대조해 겹치지 않는 유일한 빈 공간(PartySummary 상단과 Title/Save 버튼 하단 사이)으로 재배치했다.

## 6. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개
- `dotnet build ProjectH.Editor.csproj` : 오류 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개
- UI 겹침 수정은 씬 파일의 앵커 좌표를 전부 대조해 수치상 겹치지 않음을 확인했으나, 실제 렌더링 결과는 사용자 환경에서 직접 확인 필요

## 7. Day47 완료 정리

기존 클리어/해금 시스템, Day41~46의 시간·활력·호감도·침식도·웨이브·보스 시스템은 전부 그대로 유지했다. 별점 기록은 얇은 확장으로 추가했고, 최적화 작업은 동작이 검증된 부분만 안전하게 통합했으며, UI 겹침 수정은 실제 좌표 계산에 근거해 최소 변경으로 처리했다.

## 다음 작업

- Day48 Phase 1 전체 게임 루프 통합 QA
- UI 겹침 수정 결과 실제 화면 확인
- `DungeonSelect.unity`/`Result.unity`에 남은 옛 UI 잔재 확인 및 정리 (보류 중)
