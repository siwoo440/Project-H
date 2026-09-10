# 프로젝트 H : 42일차 개발 로그

## 오늘 목표

42일차의 목표는 **Day41에서 만든 날짜/시간대 진행에 실제로 의미를 부여하는 활력(Vitality) 자원을 추가하는 것**이었다.

핵심 흐름은 다음과 같다.

```text
활력 소비 (테스트: -1 버튼)
↓
활력 부족 시 소비 차단
↓
시간 진행 (Night → 다음 날 Morning)
↓
활력 전체 회복
```

## GitHub 기준

- 작업 시작 전 기준 커밋: `9fa8081` (`41일차 : 날짜·Morning/Day/Evening/Night 시간대 Runtime 및 SaveData 구축`)
- 브랜치: `main`
- Day41 → Day42 변경: 9개 파일 (신규 6개 · 수정 3개, 삭제 없음)

## 1. SaveData에 활력 필드 추가

`GoldCurrencyService`가 기존 `SYS_BATTLE_GOLD:` 플래그를 재사용했던 것과 달리, 활력은 플래그로 표현할 대상이 없었기 때문에 `SaveData`에 `currentVitality` 필드를 새로 추가했다.

```csharp
public const int MaxVitality = 100; // Day42 임시 기본값, 추후 기획 수치로 조정
[SerializeField] private int currentVitality = MaxVitality;
```

최대값은 임시 기본값 100으로 두었고, 이후 기획 수치가 정해지면 이 상수 하나만 바꾸면 되도록 했다. `EnsureDefaults()`에서 `Mathf.Clamp(currentVitality, 0, MaxVitality)`로 항상 안전한 범위로 보정한다.

## 2. VitalityService 추가

`GoldCurrencyService`와 동일한 static 유틸리티 패턴으로 `VitalityService`를 추가했다.

```text
GetVitality(saveData)               // 현재 활력 조회
AddVitality(saveData, amount)       // 활력 증가 (최대값으로 자동 보정)
TrySpendVitality(saveData, amount)  // 활력 안전 소비 (부족 시 실패, 상태 보존)
RestoreFull(saveData)               // 활력 최대치 전체 회복
```

`TrySpendVitality`는 `GoldCurrencyService.TrySpendGold`와 동일하게 부족하면 차감하지 않고 실패만 반환해, 실패한 소비 시도가 저장 상태를 깨뜨리지 않도록 했다.

## 3. 날짜 진행과 활력 회복 연결

`GameTimeService.AdvanceTime()`이 Night에서 다음 날 Morning으로 넘어갈 때 `VitalityService.RestoreFull()`을 호출하도록 연결했다.

```text
Night
↓ AdvanceTime()
다음 Day의 Morning + 활력 전체 회복
```

SaveData 자체에는 이 규칙을 넣지 않고, 기존 Day41 방식대로 진행 규칙은 서비스 계층에만 두었다.

## 4. Lobby 활력 테스트 UI

`LobbyShopRuntimePatch`, `LobbyTimeAdvanceRuntimePatch`와 동일한 Runtime Patch 방식으로 `LobbyVitalityRuntimePatch`를 추가했다.

Lobby 씬 로드 시 SaveButton 왼쪽 여백에 `VITALITY · 현재/최대` 라벨과 `-1` / `+1` 테스트 버튼을 동적으로 생성한다.

```text
-1 버튼 클릭 → VitalityService.TrySpendVitality(1)
+1 버튼 클릭 → VitalityService.AddVitality(1)
↓ (둘 다 공통)
SaveManager.SaveCurrent()
↓
활력 라벨 즉시 갱신
```

시간 진행 버튼을 눌러 다음 날로 넘어갈 때도 활력 라벨이 즉시 갱신되도록 `LobbyTimeAdvanceRuntimePatch`에 `LobbyVitalityRuntimePatch.RefreshDisplay()` 호출을 한 줄 추가했다.

씬 파일(`Lobby.unity`)은 이번에도 직접 수정하지 않고 기존 Day40~41 패턴을 그대로 따랐다.

## 5. EditMode 테스트 추가

`VitalityServiceTests`를 신규 작성했다.

- 새 게임 기본 활력이 `MaxVitality`인지
- 활력 1 소비 성공 시 잔량 감소
- 활력 부족 시 소비 차단 및 기존 활력 유지
- 최대값을 초과하는 증가 요청이 `MaxVitality`로 보정되는지
- 시간을 4회 진행해 다음 날로 넘어가면 활력이 전체 회복되는지

## 6. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개
- Unity Editor의 실제 Play 모드 UI 동작과 EditMode Test Runner 실행 결과는 사용자 환경에서 직접 확인됨

## 7. 범위 제한 준수

이번 Day42는 사용자 요청에 따라 **기본값 + 테스트용 증감 버튼 + 날짜 진행 시 전체 회복**까지만 구현했다. 다음 항목은 아직 연결하지 않았다.

- 던전 입장 등 실제 행동에 대한 활력 소비 규칙
- 활력 수치(최대값/소비량) 최종 기획 확정
- 활력 부족 시 던전 진입 차단 등 게임플레이 제약
- 최종 UI 디자인 (현재는 Runtime 동작 검증용 임시 버튼)

## 8. Day42 완료 정리

기존 `SaveManager`, `SaveData`의 날짜/시간대 구조, `GoldCurrencyService`, `GameTimeService`, Shop 시스템은 모두 그대로 유지했다.

활력을 `GameTimeService`의 날짜 전환 지점에 얇게 연결만 해두었기 때문에, 이후 던전 입장이나 특정 행동에서 `VitalityService.TrySpendVitality()`를 호출하는 형태로 손쉽게 확장할 수 있는 구조로 남겨두었다.

## 다음 작업

- 던전 입장 등 실제 행동에 활력 소비 규칙 연결
- 활력 수치(최대값/소비량/회복량) 최종 기획 반영
- Day43 호감도 Runtime 및 단계 시스템
- Unity Test Runner에서 Day42 테스트 스위트 정기 회귀 확인
