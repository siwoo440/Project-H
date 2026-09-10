# 프로젝트 H : 51일차 개발 로그

## 오늘 목표

51일차의 목표는 **상태이상 시스템 확장 — 지속피해·행동제한·버프·디버프 공통화**다.

코드를 확인해 보니 상태이상의 **재료는 이미 다 있었다.** 기절·마법 저항 감소·피해 감소·지속 피해·도발이 전부 동작하고 있었다. 문제는 이것들이 서로 **남남**이었다는 점이다.

| 효과 | 저장 위치 |
| --- | --- |
| 기절 · 저항 감소 · 방어 증가 · 피해 감소 | `modifiers` (TimedModifier 목록) |
| 지속 피해 | `periodicDamages` (PeriodicDamageEntry 목록) |
| 도발 | `taunts` (TauntEntry 목록) |

세 창고가 따로 놀았기 때문에 결정적인 문제가 있었다.

> **적이 지금 무슨 상태인지 화면에서 알 수가 없다.**

기절했는지, 독에 걸렸는지, 저항이 깎였는지 전부 내부 숫자로만 처리되고 플레이어에게는 아무것도 보이지 않았다. 세레나 궁극기의 정화도 무엇을 지웠는지 알 수 없었다.

Day51은 이 세 창고를 **"상태이상"이라는 하나의 개념으로 묶고**, 그것을 화면에 띄운다.

## GitHub 기준

- 작업 시작 전 기준 커밋: `209e1cc` (`50일차 : 리듬 콤보·HUD 확장 및 판정 등급 궁극기 효과 연동`)
- 브랜치: `main`
- Day51 변경: 13개 파일 (신규 6개 · 수정 7개, 삭제 없음)

## 1. 상태이상 카탈로그 (신규)

`BattleStatusEffectCatalog`를 순수 조회 전용 static 클래스로 만들었다. 상태이상 13종을 정의하고, 각각의 표시 정보를 한곳에서 관리한다.

| 디버프 (8종) | 버프 (5종) |
| --- | --- |
| 화상(화) · 중독(독) · 기절(기) · 침묵(침) | 방어 증가(방) · 피해 감소(막) |
| 둔화(둔) · 저항 감소(저) · 실명(맹) · 도발(도) | 회복 증가(치) · 반격 증가(반) · 공격 증가(공) |

색상은 4계열로 나눴다.

- **지속 피해** — 주황 (화상 · 중독)
- **행동 제한** — 보라 (기절 · 침묵 · 도발)
- **일반 디버프** — 붉은색 (둔화 · 저항 감소 · 실명)
- **버프** — 방어 계열 청록 / 그 외 파랑

핵심은 **변환 함수**다. 기존 `BattleRuntimeModifierKind`와 주기 피해의 `BattleDamageType`을 상태이상 ID로 바꿔주기 때문에, 기존 코드를 전혀 고치지 않고도 이미 걸려 있는 효과들이 그대로 표시된다.

```csharp
public static BattleStatusEffectId FromModifierKind(BattleRuntimeModifierKind kind)
public static BattleStatusEffectId FromPeriodicDamageType(BattleDamageType damageType)  // 마법→화상, 그 외→중독
```

## 2. 신규 상태이상 3종

기존 `BattleRuntimeModifierKind`에 세 종류를 이어 붙였다. **기존 값 번호는 건드리지 않았다** (0~6 유지).

```csharp
Silence = 7,                        // 스킬 사용 불가 침묵
AttackSpeedReductionPercent = 8,    // 공격 속도 비율 감소 둔화
AttackPercent = 9                   // 공격력 비율 증가
```

화상·중독은 새 구조를 만들지 않고 **기존 `AddPeriodicDamage`를 그대로 재활용**했다. 이미 검증된 주기 피해 로직을 재작성하지 않기 위해서다. 피해 종류가 마법이면 화상, 그 외면 중독으로 분류된다.

## 3. 상태이상 수집 API

`BattleSkillRuntimeState.CollectStatusEffects()` — 세 창고를 한 번에 훑어 하나의 목록으로 반환한다.

```csharp
public static void CollectStatusEffects(string runtimeId, List<BattleStatusEffectSnapshot> buffer, float nowSeconds = -1f)
```

설계에서 신경 쓴 부분이 두 가지 있다.

**첫째, 버퍼 재사용.** UI가 주기적으로 호출하기 때문에 `List`를 매번 새로 만들면 GC 부담이 생긴다. 호출자가 넘긴 버퍼를 `Clear()` 후 채우는 방식으로 할당을 없앴다.

**둘째, 동일 종류 병합.** 저항 감소가 서로 다른 출처로 두 번 걸리면 칩이 두 개 뜨는 게 아니라 **하나의 칩에 중첩 수 `저2`** 로 표시된다. 지속시간은 가장 늦게 끝나는 쪽을 쓴다.

```csharp
float longest = Mathf.Max(existing.RemainingSeconds, remainingSeconds);
buffer[index] = new BattleStatusEffectSnapshot(id, longest, existing.StackCount + 1);
```

지속 피해의 잔여 시간은 `다음 Tick까지 남은 시간 + (남은 Tick 수 - 1) × Tick 간격`으로 계산했다.

## 4. 소비 지점 연결

신규 3종을 실제로 동작시키기 위해 전투 핵심 경로 세 곳에 손을 댔다.

| 상태이상 | 연결 위치 |
| --- | --- |
| 침묵 | `BattleSkillExecutor` — 기절 체크 바로 아래 |
| 둔화 | `BattleBasicAttackTiming.GetInterval(공격속도, 감소율)` 오버로드 신설 |
| 공격력 증가 | `BattleDamageResolver.Resolve` — 원본 위력에 배율 적용 |

여기서 가장 조심한 것은 **회귀 방지**다. 세 곳 모두 효과가 걸려 있지 않으면 배율 1.0 / 감소율 0이 되어 **이전 계산과 결과가 완전히 동일**하다.

```csharp
// 공격력 버프가 없으면 attackMultiplier == 1f 이므로 기존 계산과 동일
float attackMultiplier = BattleSkillRuntimeState.GetAttackMultiplier(request.Attacker.RuntimeId);
int rawPower = Mathf.Max(0, Mathf.RoundToInt(request.Power * attackMultiplier));
```

기존 `GetInterval(공격속도)` 시그니처도 그대로 남겨서 Day11에 작성한 `BattleBasicAttackTimingTests`가 수정 없이 통과한다.

둔화는 `MaxAttackSpeedReduction = 0.70f` 상한을 걸었다. 중첩으로 100%에 도달하면 적이 완전히 멈춰버리기 때문이다.

## 5. 상태이상 칩 UI (신규)

`BattleStatusEffectStripView` — 런타임 생성 전용 뷰다. 씬 파일을 전혀 건드리지 않는다.

칩 하나의 구성은 다음과 같다.

- 분류 색상 배경
- 축약 문구 (중첩 시 `저2` 형태)
- 하단 잔여 시간 바 (기준 10초 대비 비율)

최대 5개까지 표시하고 초과분은 `+n`으로 표시한다. 갱신은 매 프레임이 아니라 **0.1초 간격**으로 제한했다.

잔여 시간 바는 `Image.Type.Filled` + `fillAmount`가 아니라 **`anchorMax.x` 확장 방식**으로 만들었다. Day46에서 보스 체력바가 줄어들지 않던 원인이 스프라이트 없는 Filled 타입이었기 때문에, 같은 함정을 반복하지 않았다.

무한 지속 효과(엘렌 궁극기의 전투 종료까지 유지되는 버프)는 잔여 시간 바가 가득 찬 상태로 유지된다.

## 6. 표시 위치

요청받은 대로 아군과 적군의 배치를 다르게 했다.

| 대상 | 위치 | 부착 방식 |
| --- | --- | --- |
| 아군 | **초상화 위** | `EnsurePortraitButton()`에서 초상화 RectTransform의 자식으로 `AttachAbove` |
| 적군 | **적 아래** | `Bind()`에서 적군 월드 Canvas의 자식으로 `AttachBelow` |

부모 앵커를 기준으로 바깥 방향 피벗을 잡았기 때문에 기존 레이아웃을 밀어내지 않는다.

```csharp
stripRect.anchorMin = new Vector2(0f, above ? 1f : 0f);
stripRect.anchorMax = new Vector2(1f, above ? 1f : 0f);
stripRect.pivot     = new Vector2(0.5f, above ? 0f : 1f);
```

배치 검증은 씬 YAML을 읽어서 계산했다. HUD 카드는 화면 y `0.015~0.235`, 그 안에서 초상화는 카드의 `0.39~0.94`이므로 초상화 상단은 화면 y 약 `0.222`다. 칩 높이 26px를 얹어도 `0.248` 정도라, Day47에서 정리한 SkillBlockPanel 영역(`y 0.02~0.215`)과 겹치지 않는다.

적군 월드 Canvas는 `180×250` 단위이므로 칩 5개(`5×26 + 4×3 = 142`)가 가로 폭 안에 들어간다.

모든 칩과 텍스트는 `raycastTarget = false`로 설정했다. Day49에서 초상화 버튼이 동작하지 않던 원인이 raycast 설정이었던 만큼, 상태이상 표시가 전투 입력을 가로채지 않도록 확실히 막았다.

## 7. 정화 확장

기존 `RemoveDebuffs`는 **등록된 출처만** 제거했다. 즉 `RegisterRemovableDebuff` 호출을 빼먹은 효과는 정화되지 않았다.

이걸 두 단계로 바꿨다.

1. 기존처럼 등록된 출처를 등록 순서대로 제거 (부분 정화 순서 보존)
2. 남은 개수만큼 **카탈로그가 디버프로 분류한 효과**를 추가로 제거

```csharp
removed += RemoveUnregisteredDebuffs(runtimeId, count - removed);
```

기존 동작을 대체하지 않고 **뒤에 덧붙인** 이유는, `BattleSkillEffectExecutor`의 부분 정화(예: "디버프 2개 제거")가 등록 순서에 의존하기 때문이다. 순서를 유지하면서 누락만 보완했다.

이제 개별 효과마다 `RegisterRemovableDebuff`를 빼먹는 실수가 원천 차단된다.

## 8. EditMode 테스트

`BattleStatusEffectTests` 신규 **10개**를 작성했다.

- 카탈로그 전 항목이 표시 문구·축약 문구·버프/디버프 분류를 갖는지
- **모든 `BattleRuntimeModifierKind`가 상태이상으로 매핑되는지** (열거형에 종류를 추가하고 카탈로그 갱신을 잊으면 즉시 실패)
- 활성 Modifier 수집과 잔여 시간 계산
- 만료된 효과가 목록에서 빠지는지
- 동일 종류 병합 및 중첩 수 계산
- 침묵 상태 조회 (지속 중 / 만료 후)
- 둔화의 공격 주기 반영 및 70% 상한
- 공격력 버프 배율
- 정화가 디버프만 지우고 버프는 남기는지

두 번째 항목은 일종의 **안전장치**다. 앞으로 Day52(속성), Day53(브레이크)에서 Modifier 종류가 늘어날 텐데, 카탈로그 등록을 잊으면 테스트가 바로 잡아낸다.

## 9. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Editor.csproj` : 오류 0개 · 경고 0개
- 기존 `BattleBasicAttackTimingTests` / `BattleSkillRuntimeStateDay19Tests` / `BattleUltimateEffectTests`는 시그니처와 계산 결과가 유지되어 수정 없이 통과

씬(`Battle.unity`) 수정은 필요하지 않았다.

## 10. 범위 제한

이번 Day51은 **시스템과 UI까지만** 구현했다. 신규 3종(침묵·둔화·공격력 증가)을 실제로 거는 스킬은 아직 없다.

현재 게임에서 확인 가능한 것은 **기존 효과들이 칩으로 표시되는 것**이다.

- 이브 궁극기 → 적 아래에 보라색 「기」
- 릴리아 궁극기 → 적 아래에 붉은 「저」
- 엘렌 궁극기 → 아군 초상화 위에 청록 「막」「방」
- 스킬 지속 피해 → 주황 「화」 또는 「독」
- 도발 스킬 → 보라 「도」

신규 3종은 Day52 이후 스킬·보스 패턴 데이터에서 사용될 예정이다.

## 11. 변경 파일 정리

| 구분 | 파일 |
| --- | --- |
| 신규 | `Battle/BattleStatusEffectCatalog.cs` (+ `.meta`) |
| 신규 | `Battle/BattleStatusEffectStripView.cs` (+ `.meta`) |
| 신규 | `Tests/EditMode/BattleStatusEffectTests.cs` (+ `.meta`) |
| 수정 | `Battle/BattleSkillRuntimeState.cs` |
| 수정 | `Battle/SkillBlock/BattleSkillExecutor.cs` |
| 수정 | `Battle/BattleBasicAttackController.cs` |
| 수정 | `Battle/BattleBasicAttackTiming.cs` |
| 수정 | `Battle/BattleDamageResolver.cs` |
| 수정 | `Battle/BattleHudCardView.cs` |
| 수정 | `Battle/BattleEnemyView.cs` |

## 다음 작업

- Day52 속성·약점·저항 시스템 — 공격 속성·저항·약점 배율 및 UI 피드백
- 신규 3종(침묵·둔화·공격력 증가)을 실제 스킬 데이터에 연결
- 실제 플레이 감각에 따른 칩 크기 및 표시 개수 조정
