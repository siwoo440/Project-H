# 프로젝트 H : 44일차 개발 로그

## 오늘 목표

44일차의 목표는 **지역별 침식도(0~100, 5단계)를 Runtime과 SaveData에 구축하고, 침식도가 높을수록 그 지역 던전의 적 스탯이 실제로 강해지도록 전투 계산에 연결하는 것**이었다.

핵심 흐름은 다음과 같다.

```text
지역 침식도 0~100
↓ 20%씩 5단계 분할
안정(0~19) → 균열(20~39) → 침식(40~59) → 위험(60~79) → 붕괴(80~100)
↓
등급이 높을수록 그 지역 던전의 적 HP·공격력·방어력·저항력 배율 증가
```

## GitHub 기준

- 작업 시작 전 기준 커밋: `a911171` (`43일차 : 캐릭터 호감도 5단계 시스템 및 캐릭터 화면 디버그 UI 구축`)
- 브랜치: `main`
- Day43 → Day44 변경: 7개 파일 (신규 4개 · 수정 3개, 삭제 없음)

## 1. SaveData에 지역별 침식도 목록 추가

캐릭터별 호감도가 `CharacterSaveData`에 붙었던 것과 달리, 침식도는 캐릭터가 아닌 **지역**(`DungeonData.RegionId`)마다 존재해야 한다. 기존 프로젝트에 지역을 나타내는 정적 데이터(`RegionData` 등)가 없어, 기존 `itemInventory`(ID+수량 목록) 패턴을 그대로 따라 `RegionErosionSaveData(regionId, erosionLevel)` 목록을 `SaveData`에 새로 추가했다.

```csharp
public const int MinErosion = 0;
public const int MaxErosion = 100;
[SerializeField] private string regionId;
[SerializeField] private int erosionLevel;
```

`EnsureDefaults()`에서 범위(0~100) 보정과 중복 지역 정규화(`NormalizeRegionErosion`)를 수행하며, `GetRegionErosion`/`SetRegionErosion`/`HasRegionErosion`으로 조회·변경한다.

## 2. 5단계 등급과 적 스탯 배율

`GoldCurrencyService`/`VitalityService`/`AffinityService`와 동일한 static 유틸리티 패턴으로 `RegionErosionService`를 추가했다.

```csharp
public enum RegionErosionTier
{
    Stable = 0,     // 0~19  : 안정  ×1.00
    Cracked = 1,    // 20~39 : 균열  ×1.10
    Eroded = 2,     // 40~59 : 침식  ×1.20
    Dangerous = 3,  // 60~79 : 위험  ×1.35
    Collapsed = 4   // 80~100 : 붕괴 ×1.50
}
```

`GetEnemyStatMultiplier(tier)`가 등급별 배율을 반환하고, `GetEnemyStatBonusPercent(saveData, regionId)`는 이를 `+20%` 같은 증가율(%) 문구로 변환한다.

## 3. 적 스탯 계산에 침식도 연결

기존 `BattleEnemyStatsFactory.Create()`는 이미 던전별 테스트 배율(`DungeonBattleTestProfile`)을 적용하고 있었다. 여기에 지역 침식도 배율을 곱연산으로 추가 적용했다.

```text
최종 적 HP/공격력/방어력/저항력
= 원본 스탯 × 던전별 배율 × 지역 침식도 배율
```

던전 ID로 `DungeonData.RegionId`를 조회해 해당 지역의 침식도 배율을 가져오며, 전역 관리자나 저장 데이터가 준비되지 않은 경우(직접 전투 테스트 씬 등)에는 배율 1.0(영향 없음)으로 안전하게 처리했다.

## 4. 지역 침식도가 던전마다 랜덤하게 정해지도록 확장

1차 구현에서는 침식도가 기본값 0에서 시작해 디버그 버튼으로만 바뀌었으나, 이후 요청에 따라 **던전마다 소속 지역의 침식도가 랜덤하게 자동 배정**되도록 확장했다.

```text
던전 선택 화면 최초 진입
↓
아직 등록되지 않은 지역마다 0~100 랜덤 침식도 1회 배정
↓
SaveCurrent()로 즉시 저장 (이후 재방문 시 재추첨되지 않음)
```

`RegionErosionService.TryInitializeRandomErosion()`이 핵심 로직이며, 이미 등록된 지역은 값을 그대로 유지하고 새 지역만 랜덤값을 배정한다. 혹시 던전 선택 화면을 거치지 않고 전투에 진입하는 경우를 대비해 `BattleEnemyStatsFactory.Create()`에서도 동일한 초기화를 안전망으로 한 번 더 시도한다.

## 5. 던전 선택 화면에 침식도 및 적 스탯 증가율 표시

기존 `DungeonSelectScreenController`의 DUNGEON DETAIL 패널에 침식도 전용 텍스트 행을 새로 추가했다.

```text
DUNGEON DETAIL
무너진 성역의 숲
REGION · REG_LETICIA
침식도 42/100 · 침식 · 적 스탯 +20%
```

REGION 텍스트 바로 아래, 기존 InfoBox 상단 여백을 살짝 줄여 확보했다. 하단에는 Day44 1차 구현 때 추가한 `침식도 -10`/`+10` 디버그 버튼을 그대로 유지해 수동 테스트가 가능하다.

## 6. EditMode 테스트 추가

`RegionErosionServiceTests`를 신규 작성했다.

- 미등록 지역 기본 침식도가 0인지
- 침식도 증가가 정상 반영되는지
- 최대값(100)/최소값(0) 초과·미만 입력이 정확히 보정되는지
- 5단계 등급 경계값 10개(0·19·20·39·40·59·60·79·80·100)가 모두 올바른 등급으로 변환되는지
- 등급이 높을수록 적 스탯 배율이 커지는지
- 서로 다른 지역의 침식도가 독립적으로 유지되는지
- 저장/로드 후에도 지역별 침식도와 등급이 유지되는지
- 미등록 지역 랜덤 초기화가 0~100 범위 안에서 이루어지는지
- 이미 등록된 지역은 재초기화(재추첨)되지 않는지
- `GetOrInitializeErosion`을 반복 호출해도 값이 유지되는지
- 침식도가 높을수록 적 스탯 증가율(%)이 커지는지 (0% → 50%)

## 7. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개
- Unity Editor의 실제 Play 모드 UI 동작과 EditMode Test Runner 실행 결과는 사용자 환경에서 직접 확인됨

## 8. 범위 제한 준수

이번 Day44는 **수치(0~100) + 5단계 등급 + 던전별 랜덤 배정 + 적 스탯 배율 연동 + 던전 선택 화면 표시**까지만 구현했다. 다음 항목은 아직 연결하지 않았다.

- 침식도 시간 경과 자동 증가/감소
- 침식도에 따른 보상·드롭 테이블 변화
- 지역 전용 화면/월드맵 UI
- 최종 UI 디자인 (현재는 Runtime 동작 검증용 디버그 버튼)

## 9. Day44 완료 정리

기존 `SaveManager`, `SaveData`, `DungeonData`, `BattleEnemyStatsFactory`, `DungeonBattleTestProfile`, Shop/Vitality/Affinity 시스템은 모두 그대로 유지했다. 침식도를 `SaveData`에 얇게 목록 하나로 얹고, 진행·배율 규칙은 `RegionErosionService`에 분리해두었기 때문에, 이후 침식도 자동 증가나 보상 연동 등도 이 서비스를 확장하는 형태로 이어갈 수 있는 구조로 남겨두었다.

## 다음 작업

- Day45 Dungeon Encounter 데이터 구축
- 침식도 시간 경과 자동 변화 규칙
- 침식도-보상/드롭 연동 설계
- Unity Test Runner에서 Day44 테스트 스위트 정기 회귀 확인
