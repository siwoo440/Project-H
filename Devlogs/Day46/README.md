# 프로젝트 H : 46일차 개발 로그

## 오늘 목표

46일차의 목표는 **코드에 자리만 있던 `EnemyAIType.Boss`를 실제로 채워 넣는 것**이었다. 프로토타입 보스 몬스터 하나를 새로 만들고, Day45에서 구축한 다중 웨이브 던전의 마지막 웨이브에 등장시키며, 보스 등장 연출과 전용 체력바까지 연결했다.

핵심 흐름은 다음과 같다.

```text
DG004 3웨이브 클리어
↓
4웨이브: 보스 단독 등장
↓
화면 붉은 번쩍임 + "보스 등장!" 중앙 문구
↓
화면 상단 중앙 보스 전용 체력바 표시
↓
보스 처치 → 체력바 자동 숨김 → 최종 VICTORY
```

## GitHub 기준

- 작업 시작 전 기준 커밋: `26c4b87` (`45일차 : Dungeon Encounter 데이터 및 다중 웨이브 전투 루프 구축`)
- 브랜치: `main`
- Day45 → Day46 변경: 7개 파일 (신규 4개 · 수정 3개, 삭제 없음)

## 1. 기존 구조 확인

`MonsterData.EnemyAIType`에는 이미 `Boss = 7`이 "확장 자리"로 존재했지만, `BattleEnemyTargetPolicy` 등 어디에서도 실제로 분기 처리되지 않아 사실상 `Normal`과 동일하게 동작하고 있었다.

## 2. 보스 몬스터 데이터 추가

`Assets/ProjectH/Data/Monsters/MON_BOSS_LETICIA.asset`를 신설했다. `AIType = Boss`, 이름 "침식의 파수꾼"이며, 기존 최강 몬스터인 부패 병사(HP 3,250)의 약 4.6배인 HP 15,000, 공격력·방어력도 크게 상향했다.

이 프로젝트의 `MonsterData`/`DungeonData`는 폴더 스캔이 아니라 `ProjectHDataCatalog.asset`에 개별 GUID로 명시 등록해야 `DataManager`가 인식한다는 점을 확인하지 못해, 1차 구현 직후 실제 플레이 중 `MonsterData not found: MON_BOSS_LETICIA` 오류가 발생했다. `ProjectHDataCatalog.asset`의 `monsters:` 목록에 보스 GUID를 추가해 해결했다.

## 3. 보스 웨이브 등록

Day45에서 만든 `DungeonEncounterWave` 구조를 그대로 사용해, DG004(심연의 관문)의 기존 3웨이브 뒤에 **보스 단독 4웨이브**를 추가했다. 새로운 데이터 타입을 만들 필요 없이 기존 웨이브 목록에 항목 하나만 추가하면 되는 구조였다.

## 4. 보스 등장 연출 및 상단 체력바

`BattleBossPresentationController`를 신설했다. `BattleScreenController.SpawnEnemies()`가 현재 웨이브에 `AIType.Boss` 몬스터가 있으면 자동으로 이 컨트롤러를 호출하도록 연결했다 (다중 웨이브 중 어느 시점에 보스가 나와도 동일하게 작동).

- 화면이 붉게 두 번 짧게 번쩍이고, 중앙에 "{보스 이름}\n보스 등장!" 문구가 페이드인 → 유지 → 페이드아웃
- 화면 상단 중앙에 보스 전용 체력바(이름 + 게이지 + 수치)가 표시되고, 보스가 데미지를 받을 때마다 `HealthChanged` 이벤트로 실시간 갱신되며, 보스가 쓰러지면 자동으로 숨겨짐

## 5. 체력바가 줄어들지 않는 문제 수정

1차 구현에서는 체력 게이지를 `Image.Type.Filled` + `fillAmount`로 구현했는데, 실제 플레이에서 피격을 받아도 게이지가 줄어들지 않는 문제가 발생했다.

원인은 `Sprite`를 지정하지 않은 `Filled` 타입 Image는 Unity가 채움 메시를 제대로 다시 그리지 못해 항상 꽉 찬 상태로 보이는 것이었다. 기존 `BattleHudCardView`, `BattleEnemyView`의 체력바는 애초에 이 문제를 피하려고 `fillAmount` 대신 **채움 이미지의 RectTransform 앵커(`anchorMax.x`)를 직접 축소하는 방식**을 쓰고 있었다.

`BattleBossPresentationController`의 체력바도 동일한 앵커 기반 방식으로 맞춰 문제를 해결했다.

## 6. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개
- 실제 Unity Play 모드에서 DG004 4웨이브 보스 등장 연출과 체력바 감소를 사용자가 직접 확인함
- UI 연출·코루틴 중심 기능 특성상 EditMode 자동 테스트는 추가하지 않음

## 7. 범위 제한 준수

이번 Day46은 **보스 몬스터 1종 + 보스 웨이브 등록 + 등장 연출 + 전용 체력바**까지만 구현했다. 다음 항목은 아직 다루지 않았다.

- 보스 전용 스킬/패턴/페이즈 전환
- 보스 처치 시 특별 보상/연출
- 보스 전용 처치 기록 (Day47 던전 클리어 기록/해금에서 다룰 가능성)

## 8. Day46 완료 정리

기존 `BattleScreenController`의 웨이브 전환 로직, `BattleOutcomeController`의 승패 판정, `DungeonEncounterResolver`, `RegionErosionService` 연동은 모두 그대로 유지했다. 보스 감지와 연출 트리거를 `SpawnEnemies()` 한 곳에만 얇게 연결했기 때문에, 이후 다른 던전에 다른 보스를 추가하거나 보스 전용 패턴을 확장하기 쉬운 구조로 남겨두었다.

## 다음 작업

- Day47 던전 클리어 기록 및 해금
- 보스 전용 패턴/스킬 설계
- 보스 처치 보상 차별화
- 보스 관련 EditMode 테스트 보강 (데이터 로드 검증 등)
