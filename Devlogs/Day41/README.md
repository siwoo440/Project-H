# 프로젝트 H : 41일차 개발 로그

## 오늘 목표

41일차의 목표는 **게임 내 날짜와 Morning / Day / Evening / Night 시간대를 Runtime과 SaveData에 구축하는 것**이었다.

핵심 흐름은 다음과 같다.

```text
Day 1 Morning
↓ 시간 진행
Day 1 Day
↓ 시간 진행
Day 1 Evening
↓ 시간 진행
Day 1 Night
↓ 시간 진행
Day 2 Morning
```

그리고 게임을 저장하고 종료한 뒤 다시 실행해도 Day 번호와 현재 시간대가 그대로 복원되어야 한다.

## GitHub 기준

- 작업 시작 전 기준 커밋: `77be35e` (`40일차 : 기본 상점·Gold 경제 및 아이템 구매/판매 흐름 구축`)
- 브랜치: `main`
- Day40 → Day41 변경: 7개 파일 (신규 6개 · 수정 1개, 삭제 없음)

## 1. 기존 SaveData 구조 재사용

`SaveData`에는 이미 `currentDay`(`int`, 최소값 1 보정 포함)와 `currentTime`(`SaveTimeOfDay` enum) 필드가 존재했다. 다만 `currentTime`을 실제로 진행시키는 로직은 전혀 없는 표시 전용 필드였다.

Day41에서는 새 필드를 추가로 만들지 않고 이 기존 필드를 그대로 재사용했다.

## 2. 시간대 Enum 확장

`SaveTimeOfDay`를 기존 `Morning/Afternoon/Evening/Night` 4단계에서 기획 요구에 맞춰 `Morning/Day/Evening` 3단계로 먼저 정리했다가, 이후 요청에 따라 `Night`을 다시 추가해 최종적으로 `Morning → Day → Evening → Night` 4단계로 확정했다.

```csharp
public enum SaveTimeOfDay
{
    Morning = 0,
    Day = 1,
    Evening = 2,
    Night = 3
}
```

`EnsureDefaults()`에 `Enum.IsDefined` 검증을 추가해, 정의되지 않은 시간대 값이 저장되어 있으면 자동으로 `Morning`으로 복구하도록 했다.

## 3. 날짜/시간 진행 서비스 추가

`GoldCurrencyService`와 동일한 static 유틸리티 패턴으로 `GameTimeService`를 추가했다.

```text
GetCurrentDay(saveData)   // 현재 일차 조회
GetCurrentPhase(saveData) // 현재 시간대 조회
AdvanceTime(saveData)     // 시간대 한 단계 진행
```

`AdvanceTime()`의 진행 규칙은 다음과 같다.

```text
Morning → Day
Day     → Evening
Evening → Night
Night   → 다음 Day의 Morning (currentDay + 1)
```

SaveData 자체에는 UI나 게임 규칙을 넣지 않고, 진행 규칙은 이 서비스에만 두었다.

## 4. Lobby 표시 연동

`LobbyScreenViewData`가 이미 `DAY {day} · {time}` 형식으로 상태 텍스트를 구성하고 있었기 때문에, 화면 쪽 코드를 새로 만들지 않아도 시간대가 바뀌면 Lobby 상태 텍스트에 자동으로 반영된다.

## 5. 테스트용 시간 진행 버튼

`LobbyShopRuntimePatch`와 동일한 Runtime Patch 방식으로 `LobbyTimeAdvanceRuntimePatch`를 추가했다.

Lobby 씬 로드 시 TopBar의 빈 우측 공간에 "시간 진행" 버튼을 동적으로 생성하며, 클릭하면 다음 순서로 처리된다.

```text
시간 진행 버튼 클릭
↓
GameTimeService.AdvanceTime()
↓
SaveManager.SaveCurrent()
↓
LobbyScreenController.Refresh()
```

씬 파일(`Lobby.unity`)은 직접 수정하지 않고 기존 Day40 패턴을 그대로 따랐다.

## 6. 저장/로드 연동

`currentDay`, `currentTime` 모두 기존 `SaveData`/`SaveManager`/`JsonUtility` 저장 흐름을 그대로 사용하므로, 별도 연동 코드 없이 다음 흐름이 성립한다.

```text
Day 3 Evening 상태
↓ SaveCurrent()
게임 종료
↓
재실행 → LoadCurrent()
↓
Day 3 Evening 복원
```

## 7. 데이터 정규화

- `currentDay < 1` → `EnsureDefaults()`에서 1로 복구 (기존 로직 재사용)
- 정의되지 않은 `currentTime` 값 → `EnsureDefaults()`에서 `Morning`으로 복구 (신규 추가)

## 8. EditMode 테스트 추가

`GameTimeServiceTests`를 신규 작성했다.

- 새 게임 기본 Day가 1인지
- 새 게임 기본 시간대가 Morning인지
- Morning → Day
- Day → Evening
- Evening → Night
- Night → 다음 Day의 Morning
- 9회 반복 진행 시 순환이 정확한지 (Day 3 · Day 시간대 도달)
- 손상된 저장 데이터(`currentDay < 1`, 정의되지 않은 `currentTime`)의 기본값 보정 동작

Night 추가 직후 Unity Test Runner에서 `AdvanceTime_RepeatedSevenTimes_CyclesCorrectly`가 3단계 기준값(`Morning`)을 기대하던 것이 4단계 순환과 맞지 않아 실패했고, 해당 테스트를 4단계 기준 9회 반복 테스트로 교체해 수정했다.

## 9. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개
- Unity Editor의 실제 Play 모드 UI 동작과 EditMode Test Runner 실행 결과는 사용자 환경에서 직접 확인됨

## 10. 범위 제한 준수

기획서에서 후속 일정으로 명시한 다음 항목은 이번 Day41에 포함하지 않았다.

- 활력/Stamina 시스템, 행동 비용
- 던전 입장 시 시간 자동 소모, 이벤트 행동 비용
- 상점 날짜별 재고 갱신, 날짜별 랜덤 상점
- 호감도 일일 제한, 지역 침식도 시간 변화
- 캘린더 이벤트, 리듬 시스템

## 11. Day41 완료 정리

기존 `SaveManager`, `SaveData`, `GameManager`, `SceneLoader`, Lobby 구조, `ItemInventoryService`, `GoldCurrencyService`, Shop 시스템, Dungeon/Battle 시스템은 모두 그대로 유지했다.

`SaveData`에 이미 존재하던 `currentDay`/`currentTime` 필드를 재사용하고, 진행 규칙만 `GameTimeService`라는 별도 서비스로 분리했기 때문에, Day42의 활력/행동 비용 시스템을 이 위에 얹기 쉬운 구조로 남겨두었다.

## 다음 작업

- Day42 활력 및 행동 비용 시스템
- 행동 시 `GameTimeService.AdvanceTime()`을 소비하는 규칙 연결
- Lobby "시간 진행" 테스트 버튼을 실제 UI/UX로 다듬기
- Unity Test Runner에서 Day41 테스트 스위트 정기 회귀 확인
