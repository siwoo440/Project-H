# 프로젝트 H : 43일차 개발 로그

## 오늘 목표

43일차의 목표는 **캐릭터 개개인을 향한 호감도(0~100)와 5단계 등급을 Runtime과 SaveData에 구축하는 것**이었다.

핵심 흐름은 다음과 같다.

```text
캐릭터별 호감도 0~100
↓ 20%씩 5단계 분할
낯섦(0~19) → 안면(20~39) → 호감(40~59) → 신뢰(60~79) → 유대(80~100)
```

## GitHub 기준

- 작업 시작 전 기준 커밋: `2618f45` (`42일차 : 활력 자원 시스템 및 날짜 진행 연동 구축`)
- 브랜치: `main`
- Day42 → Day43 변경: 6개 파일 (신규 4개 · 수정 2개, 삭제 없음)

## 1. CharacterSaveData에 호감도 필드 추가

Gold/활력과 달리 호감도는 캐릭터마다 별도로 존재해야 하므로, 전역 `SaveData`가 아닌 기존 `CharacterSaveData`(레벨·경험치·장비를 이미 담고 있는 클래스)에 필드를 추가했다.

```csharp
public const int MinAffinity = 0;
public const int MaxAffinity = 100;
[SerializeField] private int affinity;
```

`EnsureDefaults()`에서 `Mathf.Clamp(affinity, MinAffinity, MaxAffinity)`로 항상 안전한 범위를 유지하도록 했다.

## 2. 5단계 등급 Enum과 AffinityService 추가

`GoldCurrencyService`/`VitalityService`와 동일한 static 유틸리티 패턴으로 `AffinityService`를 추가했다.

```csharp
public enum AffinityTier
{
    Stranger = 0,      // 0~19  : 낯섦
    Acquaintance = 1,  // 20~39 : 안면
    Friendly = 2,      // 40~59 : 호감
    Trusted = 3,       // 60~79 : 신뢰
    Bonded = 4         // 80~100 : 유대
}
```

```text
GetAffinity(saveData, characterId)      // 현재 호감도 조회
AddAffinity(saveData, characterId, d)   // 호감도 증감 (음수 입력 시 감소, 0~100 자동 보정)
GetAffinityTier(saveData, characterId)  // 현재 등급 조회
ResolveTier(affinity)                   // 수치 → 등급 변환 (20 단위 구간 계산)
```

미보유 캐릭터 ID로 조회/변경을 시도하면 0을 반환하고 아무 것도 변경하지 않도록 안전 처리했다.

## 3. 테스트 UI 위치 결정: Lobby가 아닌 캐릭터 화면

처음에는 Day41~42와 동일한 Runtime Patch 방식으로 Lobby에 호감도 테스트 버튼을 추가했으나, 호감도는 캐릭터마다 다른 값이라는 점에서 Lobby(전역 상태 화면)보다 **이미 캐릭터별로 이전/다음(◀ / ▶) 탐색이 가능한 캐릭터 장비 화면**이 훨씬 자연스러운 위치라고 판단해 이동했다.

`LobbyAffinityRuntimePatch`는 제거하고, 대신 `CharacterEquipmentScreenController`의 화면 최하단 여백(기존 UI와 겹치지 않는 빈 공간)에 작은 디버그 바를 추가했다.

```text
호감도 · 62/100 · 신뢰    [호감도 -10]  [호감도 +10]
```

`◀`/`▶`로 캐릭터를 바꾸면 `Refresh()`가 해당 캐릭터의 호감도를 자동으로 다시 계산해서 보여준다. 버튼을 누르면 `AffinityService.AddAffinity()` → `SaveManager.SaveCurrent()` → `Refresh()` 순으로 즉시 반영·저장된다.

씬 파일은 수정하지 않았다 — 이 화면은 애초에 Day36부터 전체 UI를 코드에서 Runtime으로 생성하는 구조라, 기존 `BuildCharacterPanel`/`BuildEquipmentPanel` 흐름에 `BuildAffinityDebugBar` 호출 한 줄만 추가했다.

## 4. EditMode 테스트 추가

`AffinityServiceTests`를 신규 작성했다.

- 새 캐릭터 기본 호감도가 0인지
- 호감도 증가가 정상 반영되는지
- 최대값(100)/최소값(0) 초과·미만 입력이 정확히 보정되는지
- 미보유 캐릭터에 대한 변경 시도가 무시되는지
- 5단계 등급 경계값 10개(0·19·20·39·40·59·60·79·80·100)가 모두 올바른 등급으로 변환되는지
- 저장/로드 후에도 캐릭터별 호감도와 등급이 유지되는지

## 5. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개
- Unity Editor의 실제 Play 모드 UI 동작과 EditMode Test Runner 실행 결과는 사용자 환경에서 직접 확인됨

## 6. 범위 제한 준수

이번 Day43은 **수치(0~100) + 5단계 등급 + 저장 + 캐릭터 화면 디버그 버튼**까지만 구현했다. 다음 항목은 아직 연결하지 않았다.

- 호감도 등급에 따른 실제 대사/이벤트 분기
- 호감도 일일 증가 제한
- 특정 행동(선물, 대화 등)과 호감도 자동 연동
- 최종 UI 디자인 (현재는 Runtime 동작 검증용 임시 버튼)

## 7. Day43 완료 정리

기존 `SaveManager`, `SaveData`, `CharacterSaveData`의 레벨/경험치/장비 구조, `GoldCurrencyService`, `GameTimeService`, `VitalityService`는 모두 그대로 유지했다.

호감도를 `CharacterSaveData`에 얇게 필드 하나로 얹고 진행 규칙은 `AffinityService`에 분리해두었기 때문에, 이후 이벤트/선물 시스템에서 `AffinityService.AddAffinity()`를 호출하는 형태로 손쉽게 확장할 수 있는 구조로 남겨두었다.

## 다음 작업

- Day44 지역 침식도 Runtime 구축
- 호감도 등급별 이벤트/대사 분기 설계
- 호감도 일일 증가 제한 규칙 반영
- Unity Test Runner에서 Day43 테스트 스위트 정기 회귀 확인
