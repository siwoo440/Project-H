# 프로젝트 H : 50일차 개발 로그

## 오늘 목표

로드맵의 **"콤보 및 리듬 HUD"** 와 **"리듬 판정과 전투 효과 연동"** 두 항목을 하나의 일차로 합쳐서 진행했다.

Day48~49에서 만든 리듬 챌린지는 사실상 **보여주기용**이었다. 궁극기를 누르면 효과가 먼저 터지고 그 다음에 원이 떴기 때문에, 잘 누르든 다 놓치든 결과가 똑같았다. 판정 결과는 로그에만 남았다.

50일차는 이 리듬 게임을 **실제 게임성**으로 바꾸는 날이다. 핵심은 **궁극기 실행 순서를 뒤집는 것**이다.

```text
[Day49까지]
초상화 클릭 → 궁극기 효과 발동 → 리듬 챌린지 (장식)

[Day50]
초상화 클릭 → 게이지 소모 → 전투 일시정지 → 리듬 챌린지
→ 성적으로 위력 배율 계산 → 그 배율로 궁극기 효과 발동 → 전투 재개
```

## GitHub 기준

- 작업 시작 전 기준 커밋: `13e8ae7` (`49일차 : 리듬 Perfect/Good/Miss 판정 확장 및 원형 UI·박자 설계 적용`)
- 브랜치: `main`
- Day50 변경: 10개 파일 (신규 4개 · 수정 6개, 삭제 없음)

## 1. 콤보 추적 (신규)

`RhythmComboTracker`를 MonoBehaviour가 아닌 **순수 클래스**로 만들었다. EditMode 테스트에서 Unity 런타임 없이 바로 검증하기 위해서다.

```csharp
public int Register(RhythmHitResult result)
{
    if (RhythmCircleJudge.IsHit(result))   // Perfect 또는 Good
    {
        CurrentCombo++;
        MaxCombo = Mathf.Max(MaxCombo, CurrentCombo);
    }
    else                                    // Miss
    {
        CurrentCombo = 0;
    }

    return CurrentCombo;
}
```

Miss가 나면 `CurrentCombo`는 0이 되지만 `MaxCombo`는 그대로 보존된다. 최종 성적 평가에는 `MaxCombo`를 쓰기 때문이다.

콤보 구간별 색상도 여기서 관리한다.

| 콤보 | 색상 |
| --- | --- |
| 0 ~ 4 | 흰색 |
| 5 ~ 9 | 하늘색 |
| 10 이상 | 금색 |

`RhythmChallengeResult`에는 `MaxCombo`와 `IsFullCombo`(Miss가 하나도 없는 상태)를 추가했다. 기존 3인자 생성자는 **최고 콤보를 성공 판정 수로 간주해 위임하는 호환 생성자**로 남겨서, Day49에서 작성한 테스트가 그대로 통과한다.

## 2. 리듬 HUD 확장

`UltimateRhythmChallengeView`에 세 가지를 추가했다.

- **콤보 카운터** (우상단) — 2연속부터 표시되고, 갱신될 때마다 1.35배로 튀었다가 0.12초에 걸쳐 원래 크기로 돌아오는 punch 연출
- **중앙 판정 문구** — 개별 원 위에 뜨는 팝업과 별개로, 가장 최근 판정을 화면 중앙에 크게 표시 (0.5초 유지)
- **진행 게이지** (하단) — 몇 번째 원까지 판정됐는지 표시

진행 게이지는 `Image.Type.Filled` + `fillAmount`가 아니라 **`anchorMax.x` 확장 방식**으로 구현했다. Day46에서 보스 체력바가 안 줄어들던 원인이 바로 스프라이트 없는 Filled 타입이었기 때문에, 같은 함정을 반복하지 않았다.

종료 시에는 요약 문구에 위력 배율이 함께 표시되고, 중앙에 `FULL COMBO` 또는 `MAX n COMBO`가 뜬다.

## 3. 시간 기준 전환 (`deltaTime` → `unscaledDeltaTime`)

5번 항목에서 챌린지 동안 전투를 일시정지시키는데, `BattleTimeController.SetPaused(true)`는 `Time.timeScale = 0f`을 적용한다. 이 상태에서 `Time.deltaTime`은 항상 0이 되므로 **챌린지 자체가 같이 얼어붙는다.**

그래서 View의 모든 시간 누적을 `Time.unscaledDeltaTime`으로 교체했다.

- 챌린지 전체 경과 시간 (원 등장 타이밍·박자 표시)
- 개별 원의 축소 진행 시간
- 콤보 punch 연출과 중앙 판정 문구 표시 시간

## 4. 위력 배율 계산 (신규)

`RhythmPowerScaler`는 정확도(Perfect 1점 · Good 0.5점)를 위력 배율로 바꾼다.

```csharp
float accuracy = Mathf.Clamp01(result.Accuracy);
float multiplier = Mathf.Lerp(MinMultiplier, AccuracyTopMultiplier, accuracy); // 0.60 ~ 1.40

if (result.IsFullCombo)
{
    multiplier += FullComboBonus;   // +0.10
}

return Mathf.Clamp(multiplier, MinMultiplier, MaxMultiplier);   // 0.60 ~ 1.50
```

| 성적 | 배율 |
| --- | --- |
| 전부 Miss | 0.60 |
| 절반 성공 | 약 0.90 |
| 전부 Good + 풀콤보 | 1.10 |
| 전부 Perfect + 풀콤보 | 1.50 |

풀콤보 보너스를 정확도와 **분리한** 이유는, "정확도는 낮아도 하나도 놓치지 않았다"는 성취를 따로 보상하기 위해서다.

## 5. 궁극기 실행 흐름 반전

이번 일차에서 가장 구조적으로 큰 변경이다. `BattleUltimateExecutor.TryExecute()`가 검증·게이지 소비·효과 실행을 한 번에 처리하고 있었는데, 이걸 두 단계로 분리했다.

```csharp
// 1단계 : 검증 + 게이지 소모까지만 (효과 미실행)
public static BattleUltimateBeginResult TryBeginUltimate(string characterId, BattleCombatRegistry registry)

// 2단계 : 앞 단계에서 확보한 정보로 배율을 적용해 실제 효과 실행
public static BattleUltimateExecutionResult ExecuteUltimateEffect(BattleUltimateBeginResult begin, float powerMultiplier)

// 기존 시그니처는 배율 1.0으로 위 둘을 순차 호출하는 호환 래퍼로 유지
public static BattleUltimateExecutionResult TryExecute(string characterId, BattleCombatRegistry registry)
```

호환 래퍼를 남긴 덕분에 `BattleUltimateExecutorTests`와 `BattleUltimateEffectTests`의 기존 8개 호출부가 전혀 수정 없이 동작한다.

`BattleUltimateBeginResult`는 사용자 액터와 Registry 참조를 들고 있다가 2단계에 넘긴다. 챌린지가 진행되는 3~5초 동안 상황이 바뀔 수 있으므로, 2단계 진입 시 **사용자 생존 여부와 전투 종료 여부를 다시 검증**한다. 이미 전투가 끝났다면 효과를 적용하지 않고 실패 메시지를 반환한다. (게이지는 이미 소비된 상태이므로 이 경우 손해지만, 죽은 뒤 궁극기가 터지는 것보다는 자연스럽다.)

## 6. 효과에 배율 적용

`BattleUltimateEffectExecutor.Execute(...)`에 `float powerMultiplier = 1f` 기본 인자를 추가하고, 4인의 계수에 곱했다.

| 캐릭터 | 항목 | 0.60배 | 1.00배 | 1.50배 |
| --- | --- | --- | --- | --- |
| 리리아 | 마법 피해 계수 | 1.56 | 2.60 | 3.90 |
| 이브 | 물리 피해 계수 | 1.32 | 2.20 | 3.30 |
| 세레나 | 회복 / 부활 체력 | 15% | 25% | 37.5% |
| 엘렌 | 파티 피해감소 / 자기 방어 | 12% / 30% | 20% / 50% | 30% / 75% |

배율을 그냥 곱하기만 하면 밸런스가 깨지는 항목이 있어서 상한을 걸었다.

- `MaxHealRatio = 0.60f` — 세레나 회복·부활이 최대 체력의 60%를 넘지 않도록
- `MaxPartyDamageReduction = 0.35f` — 엘렌 파티 피해감소가 무적에 가까워지지 않도록

지속시간(릴리아 저항 감소 10초, 이브 기절 1초)과 저항 감소 비율은 배율 대상에서 **의도적으로 제외**했다. 리듬 성적이 위력에만 영향을 주고 지속시간까지 늘리면 체감 편차가 지나치게 커지기 때문이다.

## 7. 전투 일시정지 연동

원을 누르는 동안 적이 계속 공격하면 리듬에 집중할 수 없으므로, 챌린지 동안 전투를 멈춘다.

```csharp
BattleUltimateBeginResult begin = BattleUltimateExecutor.TryBeginUltimate(Stats.CharacterId);
// ...
pendingUltimate = begin;
ultimateChallengeActive = true;
PauseBattleForChallenge();
UltimateRhythmChallengeView.Show(begin.UltimateName, HandleRhythmChallengeCompleted);
```

`Time.timeScale`이 0으로 남는 사고를 막기 위해 세 겹의 방어를 넣었다.

1. 콜백 본문 전체를 `try` / `finally`로 감싸고 `finally`에서 재개
2. 챌린지 도중 HUD 카드가 파괴되면 `OnDestroy`에서도 재개
3. **사용자가 이미 일시정지 버튼을 눌러둔 상태였다면** 복구 대상에서 제외 — 챌린지가 끝났다고 사용자의 일시정지를 멋대로 풀어버리지 않는다

`ultimateChallengeActive` 플래그로 챌린지 진행 중 초상화 재클릭도 차단한다.

## 8. EditMode 테스트

`UltimateRhythmChallengeTests`를 12개에서 **18개**로 확장했다.

- 연속 성공 시 콤보 증가 (Good도 콤보를 유지하는지 포함)
- Miss 시 콤보 초기화 및 `MaxCombo` 보존
- 풀콤보 판별
- 전부 Perfect → 최대 배율 1.50
- 전부 Miss → 최저 배율 0.60
- 배율 범위 및 성적 순서 (전부 Miss < 혼합 < 전부 Good < 전부 Perfect)

마지막 테스트를 작성하면서 함정을 하나 발견했다. 처음에 혼합 성적을 `(Perfect 2, Good 1, Miss 1)`로 잡았는데, 이 경우 정확도 0.625 → 배율 1.10이 되어 **"전부 Good + 풀콤보"(1.00 + 0.10 = 1.10)와 정확히 같아진다.** 풀콤보 보너스가 정확도 차이를 상쇄한 것이다. 혼합 성적을 `(1, 1, 2)`로 낮춰 의도한 순서 관계를 검증하도록 수정했다.

이 자체는 버그가 아니라 설계 의도대로다. "정확도는 높지만 하나 놓친 플레이"와 "정확도는 낮아도 다 맞춘 플레이"를 비슷하게 평가하겠다는 것이다.

## 9. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Editor.csproj` : 오류 0개 · 경고 0개
- 기존 `BattleUltimateExecutorTests` / `BattleUltimateEffectTests`는 호환 래퍼를 사용하므로 기대값 변화 없음

씬(`Battle.unity`) 수정은 필요하지 않았다. 추가된 HUD가 전부 챌린지 View의 런타임 생성 요소이기 때문이다.

## 10. 변경 파일 정리

| 구분 | 파일 |
| --- | --- |
| 신규 | `Battle/Rhythm/RhythmComboTracker.cs` (+ `.meta`) |
| 신규 | `Battle/Rhythm/RhythmPowerScaler.cs` (+ `.meta`) |
| 수정 | `Battle/Rhythm/RhythmChallengeResult.cs` |
| 수정 | `Battle/Rhythm/UltimateRhythmChallengeView.cs` |
| 수정 | `Battle/BattleUltimateExecutor.cs` |
| 수정 | `Battle/BattleUltimateEffectExecutor.cs` |
| 수정 | `Battle/BattleHudCardView.cs` |
| 수정 | `Tests/EditMode/UltimateRhythmChallengeTests.cs` |

## 다음 작업

- Day51 상태이상 시스템 확장 — 지속피해·행동제한·버프·디버프 공통화
- 실제 플레이 감각에 따른 배율 범위(0.60 ~ 1.50) 조정
- 궁극기 연출 중 리듬 성적에 따른 이펙트 차등화 (정식 이펙트 적용 시점에 검토)
