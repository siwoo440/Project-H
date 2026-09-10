# 프로젝트 H : 45일차 개발 로그

## 오늘 목표

45일차의 목표는 **던전마다 코드에 하드코딩되어 있던 적 편성을 데이터(Dungeon Encounter)로 옮기고, 여러 웨이브로 순차 등장하는 구조를 실제 전투 루프에 연결하는 것**이었다.

핵심 흐름은 다음과 같다.

```text
던전 입장
↓
1웨이브 적군 등장
↓
1웨이브 전멸
↓
2웨이브 적군 등장 (WAVE 텍스트 갱신)
↓
...
↓
마지막 웨이브 전멸 → 최종 VICTORY
```

## GitHub 기준

- 작업 시작 전 기준 커밋: `ddea646` (`44일차 : 지역 침식도 5단계 시스템 및 적 스탯 배율 연동 구축`)
- 브랜치: `main`
- Day44 → Day45 변경: 14개 파일 (신규 6개 · 수정 8개, 삭제 없음)

## 1. 기존 구조 확인

던전별 적 편성은 `DungeonBattleFormationProfile.cs`에 DG001~DG004가 if문으로 하드코딩되어 있었고, `DungeonBattleFormationRuntimePatch`가 Reflection으로 `BattleScreenController.defaultEnemyIds`(단일 배열)에 주입하는 구조였다. 웨이브 개념도 없이 "한 번에 전부 등장"하는 단일 편성뿐이었다.

## 2. 인카운터 데이터 구조 추가

기존 `DungeonData.DropTable` 패턴을 그대로 따라 `DungeonEncounterWave`(몬스터 ID 목록 1개 웨이브)를 신설하고, `DungeonData`에 `encounterWaves` 목록 필드를 추가했다.

```csharp
[SerializeField] private List<DungeonEncounterWave> encounterWaves = new List<DungeonEncounterWave>();
public IReadOnlyList<DungeonEncounterWave> EncounterWaves => encounterWaves;
```

## 3. 해석 로직 분리 (테스트 가능한 순수 함수)

`DungeonEncounterResolver.ResolveWaves(dungeon, legacyFallback)`를 MonoBehaviour나 전역 관리자에 의존하지 않는 순수 static 함수로 작성했다.

```text
DungeonData에 encounterWaves가 있으면 → 그 웨이브 목록 그대로 사용
없으면 → 기존 DungeonBattleFormationProfile의 단일 편성을 1웨이브로 대체 (하위 호환)
```

## 4. Runtime 연결부 교체

`DungeonBattleFormationRuntimePatch`가 기존처럼 `defaultEnemyIds`(레거시 폴백용)를 주입한 뒤, 추가로 `DungeonEncounterResolver.ResolveWaves()` 결과를 `BattleScreenController.ConfigureEncounterWaves()`로 넘긴다. 전투 씬 로드 시 `SceneManager.sceneLoaded` 이벤트에서 `Start()` 이전에 처리되므로 기존 12일차 편성 주입 패턴과 동일한 타이밍으로 안전하게 동작한다.

## 5. 다중 웨이브 전투 루프 구현

`BattleScreenController`에 웨이브 상태(`resolvedWaves`, `currentWaveIndex`)를 추가하고, `SpawnEnemies()`가 전체 편성이 아닌 **현재 웨이브만** 스폰하도록 바꿨다.

```text
TryAdvanceWave()
↓
다음 웨이브 있는지 확인
↓
있으면: 이전 웨이브 잔여 오브젝트 정리 → 다음 웨이브 스폰 → WAVE 텍스트 갱신
없으면: false 반환 (최종 전투 종료로 이어짐)
```

`BattleOutcomeController.EvaluateNow()`가 "적 전멸 = 승리"로 판정하기 직전에 `screenController.TryAdvanceWave()`를 먼저 확인하도록 훅을 추가했다. 다음 웨이브가 있으면 승리 판정을 보류하고 전투를 계속 진행하며, 없으면 기존과 동일하게 최종 승리 처리로 넘어간다. 사망/승패 감시 로직 자체는 손대지 않고 승리 판정 지점 딱 한 곳만 가로챘다.

## 6. DG001~DG004 임의 웨이브 데이터 구성

기존 단일 편성 값을 참고해 임의로 2~3웨이브로 나누었다 (정확한 밸런스는 추후 조정 대상).

| 던전 | 웨이브 구성 |
| --- | --- |
| DG001 | 1웨이브: 늑대 1 · 2웨이브: 늑대 1 |
| DG002 | 1웨이브: 늑대 2 · 2웨이브: 병사 1 |
| DG003 | 1웨이브: 늑대 1 · 2웨이브: 병사 1 + 오염식물 1 · 3웨이브: 병사 1 |
| DG004 | 1웨이브: 늑대 2 · 2웨이브: 오염식물 2 · 3웨이브: 병사 2 |

각 웨이브의 동시 등장 수는 기존에 검증된 DG004 단일 편성(4마리)을 넘지 않도록 유지했다.

## 7. EditMode 테스트 추가

`DungeonEncounterResolverTests`를 신규 작성했다.

- 인카운터 데이터가 있으면 그 웨이브 목록을 그대로 사용하는지
- 인카운터 데이터가 없으면 레거시 단일 편성으로 대체되는지
- 던전 데이터와 레거시 편성이 모두 없으면 빈 목록을 반환하는지
- 빈 웨이브 항목(몬스터 ID 없음)은 결과에서 제외되는지

## 8. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개
- Unity Editor의 실제 Play 모드 다중 웨이브 진행과 EditMode Test Runner 실행 결과는 사용자 환경에서 직접 확인 예정

## 9. 범위 제한 준수

이번 Day45는 **인카운터 데이터 구조 + 다중 웨이브 전투 루프 + DG001~DG004 임의 데이터**까지만 구현했다. 다음 항목은 아직 다루지 않았다.

- 보스 전용 웨이브/연출 (Day46)
- 던전 클리어 기록 및 해금 (Day47)
- 웨이브별 정식 밸런스 수치 확정
- 웨이브 전환 시 연출(인트로 텍스트, 대기 시간 등)

## 10. Day45 완료 정리

기존 `BattleScreenController`의 아군 배치·사망/승패 감시·전투 결과 커밋 흐름, `DungeonBattleFormationProfile`(레거시 폴백), `DungeonBattleTestProfile`, `RegionErosionService` 연동은 모두 그대로 유지했다. 적 편성을 데이터로 옮기고 웨이브 진행 로직을 승리 판정 지점 한 곳에만 얇게 연결했기 때문에, 이후 보스 전용 웨이브나 웨이브별 연출을 추가하기 쉬운 구조로 남겨두었다.

## 다음 작업

- Day46 프로토타입 보스 구현
- 웨이브별 밸런스 수치 조정
- 웨이브 전환 연출 추가
- Unity Test Runner에서 Day45 테스트 스위트 및 실제 Play 모드 다중 웨이브 동작 확인
