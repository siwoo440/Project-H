# 프로젝트 H : 53일차 개발 로그

## 오늘 목표

53일차의 목표는 로드맵상 **브레이크 게이지 시스템 — 누적·파괴·경직·추가 피해 Window**다. 이름은 **흐트러짐 게이지**로 정했다.

지금까지 적을 상대하는 방법은 하나뿐이었다. **체력을 깎는 것.** Day52에서 속성을 넣었지만, 그것도 결국 "체력을 더 빨리 깎는다"에 머물렀다.

Day53은 적에게 **두 번째 체력바**를 붙인다.

```text
체력바       ■■■■■■■■□□   ← 0이 되면 사망
흐트러짐     ■■■■□□□□□□   ← 가득 차면 흐트러짐
```

적을 때릴 때마다 이 게이지가 조금씩 차고, 가득 차는 순간 적이 흐트러진다.

- 4초간 **완전히 행동 불능** (경직)
- 그동안 받는 피해가 **1.6배** (추가 피해 Window)
- 화면 번쩍임과 `흐트러짐!` 연출

이제 "언제 화력을 쏟아부을지"를 고르는 게임이 된다. 궁극기를 아무 때나 쓰는 게 아니라 **흐트러뜨린 직후에 몰아넣는 것**이 최선이 된다. Day50의 리듬 배율과 Day52의 속성 약점이 여기서 하나로 합쳐진다.

## GitHub 기준

- 작업 시작 전 기준 커밋: `be07f27` (`52일차 : 불·물·풀 및 빛·어둠 속성 상성 시스템과 약점·저항 UI 피드백 구축`)
- 브랜치: `main`
- Day53 변경: 15개 파일 (신규 8개 · 수정 6개, 삭제 없음) + 개발 일지

## 1. 흐트러짐 Runtime 저장소 (신규)

`BattleDisarrayRuntimeState`는 Day51 상태이상, Day52 속성과 같은 **RuntimeId 기반 static 저장소** 패턴을 따른다. 세 시스템이 같은 모양이라 읽는 사람이 한 번만 익히면 된다.

| 상수 | 값 | 설명 |
| --- | --- | --- |
| `BaseDisarrayPerHit` | 10 | 1회 타격 기본 누적량 |
| `WeakHitMultiplier` | **2.0** | 약점 적중 시 누적 배율 |
| `ResistHitMultiplier` | 0.5 | 저항 적중 시 누적 배율 |
| `DisarrayedSeconds` | 4.0 | 흐트러짐 유지 시간 |
| `DisarrayedDamageMultiplier` | 1.6 | 흐트러진 대상이 받는 피해 배율 |
| `MaxGrowthPerDisarray` | 1.25 | 흐트러질 때마다 최대치 증가율 |
| `MinMaxGauge` | 60 | 최대치 하한 |

최대치는 **최대 체력에 비례**시켰다.

```csharp
return Mathf.Max(MinMaxGauge, Mathf.RoundToInt(maxHp / (float)MaxHpDivisor));   // max(60, 최대체력 / 8)
```

체력 800 잡몹은 최대치 100이고, 체력 16000 보스는 2000이다. 보스와 잡몹이 같은 횟수로 흐트러지면 어색하기 때문이다.

## 2. Day52 속성 연계

이번 일차의 핵심이다. **약점 속성으로 때리면 흐트러짐이 2배로 쌓인다.**

```csharp
public static int GetHitAmount(BattleElementAffinity affinity)
{
    switch (affinity)
    {
        case BattleElementAffinity.Weak:   return 20;   // 약점 2배
        case BattleElementAffinity.Resist: return 5;    // 저항 절반
        default:                           return 10;   // 기본
    }
}
```

체력 800 몬스터 기준으로 비교하면 차이가 확연하다.

| 공격 | 누적량 | 흐트러짐까지 |
| --- | --- | --- |
| 약점 속성 | 20 | **5회** |
| 상성 없음 | 10 | 10회 |
| 저항 속성 | 5 | 20회 |

Day52에서는 약점이 "피해 1.5배"에 그쳤다. 이제는 **"두 배 빨리 흐트러뜨리고, 흐트러진 동안 1.6배를 더 받는다"** 가 된다. 약점을 찌르는 순간 1.5 × 1.6 = **2.4배**의 피해 창이 열리는 셈이다. 속성 선택이 처음으로 진짜 전략이 됐다.

누적은 Day52에서 `BattleDamageResult`에 추가해 둔 `Affinity`를 그대로 읽는다. 속성 판정을 두 번 하지 않는다.

## 3. 무한 흐트러짐 방지

흐트러짐이 너무 강하면 적을 영원히 묶어둘 수 있다. 세 겹으로 막았다.

1. **흐트러짐 중에는 추가 누적을 무시한다.** 경직 시간을 무한히 연장할 수 없다.
2. **해제되면 게이지가 0으로 초기화된다.** 흐트러짐 직후 바로 다시 흐트러지지 않는다.
3. **흐트러질 때마다 최대치가 1.25배 증가한다.** 100 → 125 → 156 → 195… 로 점점 어려워진다.

해제 처리는 별도 타이머 없이 **조회 시점에 시각을 확인하는 지연 방식**으로 구현했다.

```csharp
private static void RefreshRecovery(DisarrayEntry entry, float now)
{
    if (!entry.Disarrayed || now < entry.RecoverAt) return;

    entry.Disarrayed = false;
    entry.Current = 0;
    entry.Max = Mathf.RoundToInt(entry.Max * MaxGrowthPerDisarray);
}
```

`IsDisarrayed`, `GetGaugeRatio`, `AddDisarray` 등 모든 조회가 먼저 이 함수를 거친다. Update 루프가 필요 없고, 테스트에서 `nowSeconds`를 넘겨 시간을 자유롭게 제어할 수 있다.

## 4. 경직 — 기존 기절 재사용

흐트러진 적을 행동 불능으로 만들 때 **새 게이트를 만들지 않았다.**

현재 `BattleSkillRuntimeState.IsStunned` 체크가 이미 네 곳에 들어가 있다.

- `BattleBasicAttackController` — 기본 공격
- `BattleEnemyBrain` — 적 AI
- `BattleManualMoveController` — 이동
- `BattleSkillExecutor` — 스킬

흐트러질 때 `Stun` Modifier를 `"DISARRAY:STAGGER"` 출처 키로 등록하면 이 네 곳이 **전부 자동으로 동작**한다.

```csharp
BattleSkillRuntimeState.AddModifier(Stats.RuntimeId, BattleRuntimeModifierKind.Stun, 1f,
    BattleDisarrayRuntimeState.DisarrayedSeconds, BattleDisarrayRuntimeState.DisarraySourceKey);
```

새로 짠 코드 없이 경직이 완성됐고, 회귀 위험도 최소다. Day51 상태이상 칩에도 보라색 「기」가 자동으로 뜬다.

## 5. 누적 처리 위치

누적은 `BattleActor.ApplyDamage`에서 **실제 피해가 들어간 직후**에 한다. 보호막에 전부 흡수되거나 피해가 0이면 앞에서 이미 반환되므로 누적되지 않는다.

두 가지 경우를 의도적으로 제외했다.

**아군** — 흐트러짐 게이지는 적 스탯 생성 시에만 등록된다. 아군은 미등록이라 `IsRegistered` 체크에서 자연히 빠진다. 아군까지 흐트러지면 조작 불능 시간이 길어져 답답해지고, 아군 HUD에는 Day51 상태이상 칩으로 이미 공간이 빠듯하다.

**지속 피해** — 공격자 RuntimeId가 비어 있는 피해는 누적하지 않는다. 화상·중독을 걸어두고 방치하면 저절로 흐트러지는 상황을 막기 위해서다.

## 6. 피해 계산 연결

`BattleDamageResolver.Resolve`의 맨 마지막에 흐트러짐 배율을 추가했다. 이제 전체 피해 파이프라인은 다음과 같다.

```text
원본 위력
  → 공격력 버프       (Day51)
  → 방어 / 저항 차감
  → 피해 감소
  → 속성 배율         (Day52)
  → 흐트러짐 배율     (Day53)
```

흐트러지지 않은 대상은 배율 1.0이 반환되어 **기존 계산과 결과가 완전히 같다.** Day51·52와 같은 원칙이다.

## 7. 게이지 UI (신규)

`BattleDisarrayGaugeView` — 런타임 생성 전용. 씬 파일을 건드리지 않는다.

적 **체력바 배경의 자식**으로 붙여서 체력바 바로 아래에 얇게(7px) 표시된다. 체력바 "채움"이 아니라 "배경"을 기준으로 잡아야 체력이 줄어도 게이지 폭이 유지된다.

| 상태 | 표시 |
| --- | --- |
| 평상시 (0~80%) | 노란색 |
| 임박 (80% 이상) | 노랑↔주황 **깜빡임** |
| 흐트러짐 중 | 붉은색 가득 + `흐트러짐 2.4s` 잔여 시간 |

채움은 이번에도 `anchorMax.x` 방식이다. Day46 보스 체력바에서 스프라이트 없는 `Image.Type.Filled`가 갱신되지 않던 함정을 계속 피해 가고 있다. 갱신은 0.1초 간격, 시간 기준은 `unscaledDeltaTime`이다 (Day50 리듬 챌린지 일시정지 중에도 표시 유지).

## 8. 흐트러짐 연출 (신규)

`BattleDisarrayPresentation` — 흐트러지는 순간 화면이 주황색으로 0.18초 번쩍이고, 중앙에 `침식된 늑대 흐트러짐!` 문구가 0.55초 유지된 뒤 0.25초에 걸쳐 사라진다.

Day46 보스 등장 연출(`BattleBossPresentationController`)과 구조는 비슷하지만 **별도 컨트롤러로 분리했다.** 보스 전용 컨트롤러에 흐트러짐 책임까지 얹으면 보스가 없는 전투에서도 보스 컨트롤러를 생성해야 하기 때문이다.

Canvas 정렬 순서는 560으로 잡았다. 일반 HUD보다 위, Day50 리듬 챌린지(600)보다는 아래다. 궁극기 도중 흐트러짐이 발생해도 리듬 원을 가리지 않는다.

## 9. 보스 체력바 연동

Day46의 상단 보스 체력바 아래에도 같은 게이지를 붙였다. `BattleDisarrayGaugeView`를 그대로 재사용했기 때문에 코드는 부착 함수 하나만 추가됐다.

보스는 최대치가 크고(체력 15000 기준 1875) 흐트러질 때마다 1.25배씩 늘어나므로, 보스전에서는 **약점 속성 캐릭터를 편성하느냐가 공략의 핵심**이 된다. Day52에서 보스를 어둠 속성으로, 세레나를 빛 속성으로 배정해 둔 것이 여기서 의미를 갖는다.

## 10. 초기화 시점

Day52에서 겪은 교훈을 그대로 적용했다. 서로 다른 MonoBehaviour의 `Start()` 순서가 보장되지 않기 때문에, 전투 시작 시점에 초기화하면 스탯 생성과 경합해 등록이 지워질 수 있다.

흐트러짐 등록은 RuntimeId를 키로 **덮어쓰기** 방식이라 전투 시작 시 초기화가 필요 없다. 초기화는 전투 종료 시점(`BattleSkillRuntimeDriver.OnDestroy`)에만 둔다.

같은 RuntimeId를 다시 등록하면 누적·최대치·발생 횟수가 전부 초기화되는 것을 테스트로 확인했다.

## 11. EditMode 테스트

`BattleDisarrayTests` 신규 **12개**를 작성했다.

- 최대 체력 비례 최대치 산출과 하한 적용
- 미등록 대상(아군)의 안전 기본값 — 게이지 0, 흐트러짐 아님, 누적 무시, 추가 피해 없음
- 속성 상성별 누적량 (10 / 20 / 5)
- **약점 적중이 5회 만에 흐트러짐에 도달하는지** (상성 없음은 10회)
- 최대치 도달 시 흐트러짐 발생과 게이지 100% 고정
- 흐트러짐 중 추가 누적 무시
- 추가 피해 배율 1.6 적용과 해제 후 1.0 복귀
- 4초 경과 후 해제 및 게이지 초기화
- 최대치가 100 → 125 → 156으로 누적 증가하는지
- 잔여 시간 계산
- 누적 비율 표시 (25% → 60%)
- 재등록 시 전체 상태 초기화

모든 테스트는 `nowSeconds`를 명시해 Unity 시간에 의존하지 않는다.

## 12. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Editor.csproj` : 오류 0개 · 경고 0개
- 흐트러지지 않은 대상의 피해 배율이 1.0이므로 기존 피해 계산 테스트 결과 불변

씬(`Battle.unity`) 수정은 필요하지 않았다.

## 13. 변경 파일 정리

| 구분 | 파일 |
| --- | --- |
| 신규 | `Battle/BattleDisarrayRuntimeState.cs` (+ `.meta`) |
| 신규 | `Battle/BattleDisarrayGaugeView.cs` (+ `.meta`) |
| 신규 | `Battle/BattleDisarrayPresentation.cs` (+ `.meta`) |
| 신규 | `Tests/EditMode/BattleDisarrayTests.cs` (+ `.meta`) |
| 수정 | `Battle/BattleActor.cs` — 실제 피해 시 누적·경직·연출 |
| 수정 | `Battle/BattleDamageResolver.cs` — 흐트러짐 추가 피해 배율 |
| 수정 | `Battle/BattleEnemyStats.cs` — 적 스탯 생성 시 등록 |
| 수정 | `Battle/BattleEnemyView.cs` — 체력바 아래 게이지 부착 |
| 수정 | `Battle/BattleBossPresentationController.cs` — 보스 체력바 아래 게이지 부착 |
| 수정 | `Battle/BattleSkillRuntimeDriver.cs` — 전투 종료 시 초기화 |

## 다음 작업

- Day54 보스 페이즈 및 패턴 시스템 — HP·시간·**흐트러짐** 기반 페이즈 전환·패턴 스케줄러
- 흐트러짐 발생 시 궁극기 게이지 보너스 등 추가 보상 검토
- 실제 플레이 기반 수치 조정 (누적량 10 / 경직 4초 / 추가 피해 1.6배 / 최대치 증가 1.25배)
