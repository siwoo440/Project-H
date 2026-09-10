# 프로젝트 H : 52일차 개발 로그

## 오늘 목표

52일차의 목표는 **속성·약점·저항 시스템 — 공격 속성·저항·약점 배율 및 UI 피드백**이다.

지금까지 전투에는 피해 "종류"가 물리 / 마법 / 고정 세 가지뿐이었다. 그런데 이건 **방어력을 볼지 저항력을 볼지**를 정하는 값이라, 사실상 계산 규칙일 뿐 전략이 되지 못했다. 누구를 데려가도 똑같았다.

Day52는 여기에 **다른 축을 하나 더** 겹친다.

```text
[지금까지]  물리 / 마법 / 고정                       → 방어력이냐 저항력이냐

[Day52]     물리 / 마법 / 고정                       → 방어력이냐 저항력이냐
              ×
            무 / 불 / 물 / 풀 / 빛 / 어둠            → 약점이냐 저항이냐
```

즉 "리리아의 마법 공격"이 아니라 **"리리아의 불 속성 마법 공격"** 이 된다.

## GitHub 기준

- 작업 시작 전 기준 커밋: `8a14729` (`51일차 : 상태이상 시스템 공통화 및 아군·적군 상태이상 HUD 구축`)
- 브랜치: `main`
- Day52 변경: 27개 파일 (신규 6개 · 수정 21개, 삭제 없음)

## 1. 상성 설계

속성은 요청받은 대로 **불 · 물 · 풀** 삼각 순환에 **빛 · 어둠**을 얹는 구조로 잡았다.

```text
불 → 풀 → 물 → 불          (삼각 순환)
빛 → 어둠                   (일방향)
빛 / 어둠 → 무속성          (무속성은 빛·어둠에만 약점)
```

| 공격 \ 방어 | 무 | 불 | 물 | 풀 | 빛 | 어둠 |
| --- | --- | --- | --- | --- | --- | --- |
| **무** | — | — | — | — | — | — |
| **불** | — | — | 저항 | **약점** | — | — |
| **물** | — | **약점** | — | 저항 | — | — |
| **풀** | — | 저항 | **약점** | — | — | — |
| **빛** | **약점** | — | — | — | — | **약점** |
| **어둠** | **약점** | — | — | — | 저항 | — |

| 상수 | 값 |
| --- | --- |
| `WeakMultiplier` | 1.50 |
| `ResistMultiplier` | 0.75 |
| `NeutralMultiplier` | 1.00 |

빛과 어둠은 **일방향**이다. 빛이 어둠을 이기고, 어둠이 빛을 공격하면 저항당한다. 대신 어둠은 무속성을 찌를 수 있어서 나름의 자리를 갖는다.

## 2. 상성 계산 구현

상성표를 6×6 배열로 하드코딩하지 않고, **"각 속성이 우위를 가지는 대상 1개"** 만 정의한 뒤 역방향을 자동으로 저항 처리하는 방식으로 짰다.

```csharp
private static BattleElement GetStrongTarget(BattleElement element)
{
    switch (element)
    {
        case BattleElement.Fire:  return BattleElement.Grass;   // 불 > 풀
        case BattleElement.Grass: return BattleElement.Water;   // 풀 > 물
        case BattleElement.Water: return BattleElement.Fire;    // 물 > 불
        case BattleElement.Light: return BattleElement.Dark;    // 빛 > 어둠 (일방향)
        default:                  return BattleElement.None;    // 어둠·무속성은 우위 대상 없음
    }
}
```

`Evaluate`는 이 함수를 두 번 호출한다. 공격 속성의 우위 대상이 방어 속성이면 약점, 방어 속성의 우위 대상이 공격 속성이면 저항이다. 항목이 늘어나도 이 한 줄만 추가하면 된다.

어둠이 `default`로 빠지는 덕분에 "빛 → 어둠"이 자동으로 일방향이 된다.

## 3. 무속성 규칙 (기존 콘텐츠 보호)

여기가 이번 일차에서 가장 신경 쓴 부분이다.

무속성 처리를 **공격 쪽과 방어 쪽에서 다르게** 정의했다.

```csharp
if (attackElement == BattleElement.None)   // 무속성 공격
{
    return BattleElementAffinity.Neutral;  // 언제나 상성 없음
}

if (defenderElement == BattleElement.None) // 무속성 방어
{
    return IsLightOrDark(attackElement) ? Weak : Neutral;  // 빛·어둠에만 약점
}
```

**무속성 공격이 항상 Neutral**인 것이 핵심이다. Day52 이전에 만든 모든 콘텐츠는 속성이 없으므로 공격도 무속성이고, 따라서 **기존 전투 결과가 단 하나도 바뀌지 않는다.** 이 성질을 테스트로 못 박아 뒀다.

## 4. 데이터 계층 분리

기존 프로젝트는 `SkillDamageType`(데이터) → `BattleDamageType`(전투)처럼 두 계층의 열거형을 분리해 쓰고 있다. 이 관례를 따라 `DataEnums.cs`에 `ElementType`을 추가했다.

두 열거형의 순서가 어긋나면 단순 캐스팅 변환이 조용히 망가지므로, **항목 수와 이름 순서를 검사하는 테스트**를 넣었다.

```csharp
Assert.That((int)(BattleElement)dataElement, Is.EqualTo((int)dataElement));
Assert.That(Enum.GetName(typeof(ElementType), dataElement),
            Is.EqualTo(Enum.GetName(typeof(BattleElement), (BattleElement)dataElement)));
```

`CharacterData` · `MonsterData` · `SkillEffectDefinition`에 `element` 필드를 추가했다. `ScriptableObject`에 `[SerializeField]`를 더한 것이므로, 기존 `.asset`은 필드가 없으면 자동으로 0(무속성)이 되어 재생성이 필요 없다.

## 5. Runtime 속성 저장소 (신규)

`IBattleCombatantStats` 인터페이스에는 **손대지 않았다.**

이 인터페이스는 EditMode 테스트 3곳(`BattleForwardAdvanceTests` / `BattleOutcomeControllerTests` / `BattleTargetSelectorTests`)이 직접 구현하고 있어서, 멤버를 추가하면 전부 컴파일 에러가 난다.

대신 Day51의 상태이상과 같은 **RuntimeId 기반 조회 계층**을 만들었다.

```csharp
public static void Register(string runtimeId, BattleElement element);
public static BattleElement GetElement(string runtimeId);              // 미등록이면 None
public static BattleElement ResolveAttackElement(BattleElement requested, string attackerRuntimeId);
```

등록은 `BattleStatsFactory.CreateCharacter`와 `BattleEnemyStatsFactory.Create`에서 스탯 생성과 동시에 이뤄진다.

## 6. 피해 계산 연결

`BattleDamageRequest`와 `BattleDamageResult`에 속성 정보를 추가하되, **기존 생성자를 무속성으로 위임하는 호환 생성자**로 남겼다.

```csharp
public BattleDamageRequest(IBattleCombatantStats attacker, IBattleCombatantStats target, BattleDamageType type, int power)
    : this(attacker, target, type, power, BattleElement.None)
{
}
```

덕분에 현재 `Resolve(new BattleDamageRequest(...))`를 호출하는 모든 지점 — 궁극기, 기본 공격, 반격, 지속 피해 — 이 수정 없이 동작한다.

적용 순서는 다음과 같다.

```text
원본 위력 → 공격력 버프(Day51) → 방어/저항 차감 → 피해 감소 → 속성 배율
```

속성을 마지막에 둔 것은 의도적이다. "약점 1.5배"가 최종 피해에 그대로 반영되어야 체감이 명확하다.

## 7. 공격 속성 상속

공격 속성이 지정되지 않으면(`None`) **시전자의 속성을 상속**하도록 했다.

```csharp
public static BattleElement ResolveAttackElement(BattleElement requestedElement, string attackerRuntimeId)
{
    return requestedElement != BattleElement.None ? requestedElement : GetElement(attackerRuntimeId);
}
```

이 규칙 하나로 궁극기와 기본 공격이 자동으로 속성을 갖게 됐다. 리리아 궁극기는 불, 이브 궁극기는 물이 된다. `BattleUltimateEffectExecutor`를 전혀 수정하지 않고도 속성이 붙었다.

스킬은 `SkillEffectDefinition.element`로 개별 지정이 가능하고, 비워 두면 마찬가지로 시전자 속성을 따른다.

## 8. UI 피드백

**피해 숫자** — `BattleFloatingValueText`에 상성 인자를 받는 오버로드를 추가했다.

| 상성 | 표시 |
| --- | --- |
| 약점 | 금색 · **1.3배 크기** · `-123 약점!` |
| 저항 | 회색 · `-45 저항` |
| 없음 | 기존과 동일한 붉은색 · `-78` |

**속성 칩** — 새 UI 클래스를 만들지 않고 Day51의 `BattleStatusEffectStripView` 칩 생성 코드를 **재사용**했다. `ShowElementChip()`을 추가해서 적 아래 상태이상 칩 왼쪽에 고정 표시한다. 무속성 몬스터는 칩이 뜨지 않는다.

칩 색상은 속성별로 구분했다 — 불 주황 / 물 파랑 / 풀 초록 / 빛 연노랑 / 어둠 보라.

## 9. 초기화 시점 함정

처음에는 전투 시작 시점인 `BattleSkillExecutor.Configure()`에서 `BattleElementRuntimeState.ResetAll()`을 호출하도록 짰다. Day51까지의 다른 Runtime 상태들이 전부 거기서 초기화되기 때문이다.

그런데 호출 경로를 따라가 보니 문제가 있었다.

```text
BattleSkillBlockController.Start()  →  executor.Configure(registry)  →  ResetAll()
BattleScreenController              →  SpawnAllies / SpawnEnemies    →  Register()
```

**서로 다른 MonoBehaviour의 `Start()` 실행 순서는 보장되지 않는다.** 스탯 생성이 먼저 일어나면 등록된 속성이 초기화로 전부 지워져, 모든 유닛이 무속성이 되는 버그가 발생할 수 있었다.

초기화를 전투 **종료** 시점(`BattleSkillRuntimeDriver.OnDestroy`)으로 옮겼다. 속성 등록은 RuntimeId를 키로 덮어쓰기 때문에 전투 시작 시 초기화가 필요 없고, 전투 종료 시 정리만 해주면 누적도 막을 수 있다.

## 10. 초기 속성 배정

프로토타입 캐릭터 4인과 몬스터 4종에 `.asset` YAML을 직접 수정해 속성을 넣었다.

| 캐릭터 | 속성 | 몬스터 | 속성 |
| --- | --- | --- | --- |
| 세레나 | 빛 | 침식의 파수꾼(보스) | 어둠 |
| 릴리아 | 불 | 침식된 병사 | 불 |
| 이브 | 물 | 침식된 늑대 | 물 |
| 엘렌 | 풀 | 오염된 식물 | 풀 |

세레나(빛)가 보스(어둠)의 약점을 찌르는 구도라 바로 체감된다. 나머지는 삼각 순환이 전부 성립하도록 배치했다. 수치와 마찬가지로 이후 조정 가능한 기본값이다.

## 11. EditMode 테스트

`BattleElementTests` 신규 **11개**를 작성했다.

- 삼각 순환 정방향(불→풀, 풀→물, 물→불)이 약점인지
- 역방향이 저항인지
- 빛→어둠이 약점이고 어둠→빛이 저항인지 (일방향 검증)
- 무속성 방어가 빛·어둠에만 약점이고 불·물·풀에는 상성 없음인지
- **무속성 공격이 모든 방어 속성에 대해 상성 없음인지** (기존 전투 결과 불변 보장)
- 같은 속성끼리 상성 없음인지
- 배율 값 (1.5 / 0.75 / 1.0)
- 전체 속성의 표시 문구·축약 문구 정의
- Runtime 등록·조회 및 미등록 시 무속성 반환
- 공격 속성 상속 (미지정 시 시전자, 명시 시 우선)
- `ElementType`과 `BattleElement`의 항목 수·이름 순서 일치

## 12. 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Editor.csproj` : 오류 0개 · 경고 0개
- `IBattleCombatantStats`를 직접 구현하는 테스트 3곳은 인터페이스를 건드리지 않아 수정 없이 통과
- 기존 피해 계산 테스트는 모두 무속성 공격이므로 결과 불변

씬(`Battle.unity`) 수정은 필요하지 않았다.

## 13. 변경 파일 정리

| 구분 | 파일 |
| --- | --- |
| 신규 | `Battle/BattleElement.cs` (+ `.meta`) |
| 신규 | `Battle/BattleElementRuntimeState.cs` (+ `.meta`) |
| 신규 | `Tests/EditMode/BattleElementTests.cs` (+ `.meta`) |
| 수정 | `Battle/BattleDamageResolver.cs` · `BattleFloatingValueText.cs` · `BattleActor.cs` |
| 수정 | `Battle/BattleStatsFactory.cs` · `BattleEnemyStats.cs` · `BattleEnemyView.cs` |
| 수정 | `Battle/BattleStatusEffectStripView.cs` · `BattleSkillRuntimeDriver.cs` |
| 수정 | `Battle/SkillBlock/BattleSkillEffectExecutor.cs` |
| 수정 | `Data/DataEnums.cs` · `CharacterData.cs` · `MonsterData.cs` · `SkillData.cs` |
| 데이터 | 캐릭터 4개 · 몬스터 4개 `.asset` 속성 지정 |

## 다음 작업

- Day53 브레이크 게이지 시스템 — Break 누적·파괴·경직·추가 피해 Window
- 약점 적중 시 브레이크 게이지 추가 누적 연계 검토
- 던전별 몬스터 속성 편성 및 실제 플레이 기반 배율(1.5 / 0.75) 조정
