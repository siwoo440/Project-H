# 프로젝트 H : 56일차 개발 로그

## 오늘 목표

56일차는 두 부분으로 진행했다.

1. **호감도 단계 보상 및 이벤트 조건** — Phase 3의 첫 콘텐츠
2. **프로젝트 전체 최적화** — 흩어진 구조 정리와 중복 제거

로드맵상 Day56은 원래 **Phase 2 전투·던전 통합 QA**였다. Day48 때와 마찬가지로 사용자 요청에 따라 QA 일차는 건너뛰고, 다음 콘텐츠를 이 번호로 당겨왔다. 이후 일정이 하나씩 앞당겨져 전체 일정은 **Day88**에서 끝난다. 회귀 검증은 매 일차 Test Runner가 대신하고 있고, 실제로 Day55에서 테스트 간 상태 누수 버그를 그렇게 잡았다.

## GitHub 기준

- 작업 시작 전 기준 커밋: `b7e30d9` (`55일차 : 가로형 노드 던전 탐험·이벤트/함정/휴식 노드, 로딩창·전투 진입 터널 연출 및 테스트 상태 누수 수정`)
- 브랜치: `main`

---

# Part 1. 호감도 단계 보상 및 이벤트 조건

## 1. 배경

Day43에서 캐릭터마다 호감도(0~100)를 만들고 20 단위로 다섯 단계를 나눴다.

```text
낯섦(0) → 안면(20) → 호감(40) → 신뢰(60) → 유대(80)
```

그런데 캐릭터 창에 숫자와 단계 이름만 보일 뿐, **호감도를 올려도 달라지는 게 없었다.** Day56은 단계에 의미를 붙인다.

## 2. 보상은 버튼으로 직접 받기

처음 계획은 단계에 도달하면 **자동 지급**이었다. 사용자 요청으로 **직접 받는 방식**으로 바꿨다.

- 캐릭터 창 하단에 **`호감도 보상`** 버튼 추가
- 누르면 **단계별 보상 버튼 4개**(안면 · 호감 · 신뢰 · 유대)가 있는 패널이 열림

| 상태 | 조건 | 표시 |
| --- | --- | --- |
| 잠김 | 아직 도달하지 못함 | 회색, 누를 수 없음 |
| 받기 | 도달했지만 받지 않음 | **금색**, 누르면 지급 |
| 받음 | 이미 받음 | 연두색, 누를 수 없음 |

**전투 보너스도 받은 단계부터 적용**하도록 맞췄다. 도달만 해도 효과가 들어가면 보상 버튼의 의미가 흐려지기 때문이다.

## 3. 보상 구성

`AffinityRewardCatalog` 한 파일에서 수치를 조정한다.

| 단계 | 골드 | 아이템 | 전투 보너스 |
| --- | --- | --- | --- |
| 안면 (20) | 100 | 재료 ×2 | 체력 +2% |
| 호감 (40) | 200 | 회복 물약 ×1 | 공격력 +2% |
| 신뢰 (60) | 300 | 회복 물약 ×2 | 체력 +2% · 공격력 +2% |
| 유대 (80) | 500 | 철제 무기 ×1 | **궁극기 25로 시작** |

네 단계를 모두 받으면 체력 +4%, 공격력 +4%에 궁극기 게이지 25로 전투를 시작한다.

## 4. 수령 기록 저장

`CharacterSaveData`에 `affinityRewardClaimMask`를 추가해 **단계마다 1비트**를 기록한다.

다른 시스템처럼 스토리 플래그(`SYS_...`)를 쓰지 않은 이유가 있다. 전투 보너스를 계산하는 `BattleStatsFactory`는 `CharacterSaveData`만 받는다. 수령 기록이 캐릭터 저장 안에 있어야 팩토리가 추가 인자 없이 보너스를 계산할 수 있다.

필드가 없던 기존 세이브는 0(아무것도 받지 않음)으로 시작하므로 **예전 저장 파일도 그대로 열린다.**

## 5. 수령 처리

`AffinityRewardService`

```csharp
public static AffinityRewardState GetState(SaveData save, string characterId, AffinityTier tier)
public static bool TryClaim(SaveData save, DataManager data, string characterId, AffinityTier tier, out string message)
```

- 한 번에 여러 단계를 넘으면(예: 15 → 65) **해당 단계가 모두 받기 상태**가 된다
- 받은 뒤 호감도가 떨어져도 **보상과 보너스는 회수하지 않는다**
- 골드는 `GoldCurrencyService`, 아이템은 Day45 드롭 지급 서비스를 재사용했다
- **아이템을 지급할 수 없으면 골드도 주지 않고 상태를 그대로 둔다** — 절반만 받는 상황 방지

## 6. 전투 보너스 연결

- `BattleStatsFactory`가 체력·공격력에 보너스 비율을 곱한다. 보너스가 0이면 계산 결과가 **기존과 완전히 같다**
- 유대 보상의 **궁극기 시작 게이지**는 `BattleSkillRuntimeDriver`에서 전투 시작 시 한 번 넣는다. Day55 함정·휴식과 같은 방식이라 게이지 초기화에 지워지지 않는다
- 캐릭터 창의 현재 능력치에도 **받는 즉시 반영**된다

## 7. 이벤트 해금 조건 (신규)

앞으로 만들 대화·개인 이벤트는 아무 때나 볼 수 있으면 안 된다. 조건을 판정하는 틀을 먼저 만들었다.

| 조건 | 예시 |
| --- | --- |
| 호감도 단계 이상 | 세레나 신뢰 이상 |
| 일차 이상 | 5일차 이후 (Day41) |
| 시간대 | 저녁에만 (Day41) |
| 던전 클리어 | 성역 외곽 폐허 클리어 |
| 스토리 플래그 | 특정 이야기 진행 후 |

`GameEventConditionEvaluator.Evaluate()`는 모든 조건을 AND로 판정하되, **첫 번째 미충족에서 멈추지 않고 사유를 전부 모은다.** 그래서 잠긴 이벤트에 `[잠김] 신뢰 이상 · 5일차 이후`처럼 한 번에 무엇이 부족한지 보여줄 수 있다.

## 8. 개인 이벤트 목록

초기 4인 × 2화를 등록했다. 해금 조건만 가진 목록이고, 실제 이야기는 대화 시스템 일차에 연결한다.

| 캐릭터 | 1화 | 2화 |
| --- | --- | --- |
| 세레나 | 「성역의 기도」 호감 | 「오래된 약속」 신뢰 · 5일차 이후 |
| 엘렌 | 「기사의 아침 훈련」 호감 · 아침 | 「부러진 검」 신뢰 · DG002 클리어 |
| 릴리아 | 「별을 세는 밤」 호감 · 밤 | 「금지된 서가」 신뢰 · DG003 클리어 |
| 이브 | 「정령의 속삭임」 호감 | 「비 오는 날의 약속」 신뢰 · 3일차 이후 · 저녁 |

보상표와 이벤트 목록은 에셋이 아니라 **코드 목록**으로 만들었다. 에셋으로 만들면 Day46처럼 데이터 카탈로그에 따로 등록해야 하고, 개인 이벤트는 대화 데이터가 생기면 그쪽으로 옮기는 게 자연스럽기 때문이다.

---

# Part 2. 프로젝트 전체 최적화

코드를 전수 점검했다. 스크립트 175개, 약 3만 줄(테스트 8,300줄 포함) 규모다.

결론부터 말하면 **"느려서" 고칠 곳은 거의 없었다.** `Find` 계열 전역 탐색 30여 곳은 모두 씬 로드나 버튼 클릭 때 한 번만 실행되고, 매 프레임 도는 코드도 현재 규모에서는 부담이 없었다. 대신 **구조가 흩어져서 버그가 나기 쉬운 곳**이 있었고, 실제로 Day52와 Day55에 그 때문에 버그가 났다. 그래서 정리 위주로 진행했다.

## 1. 전투 정적 상태 초기화 통합

전투 상태를 담는 static 저장소 7개의 초기화 코드가 **세 파일에 흩어져** 있었다.

| 저장소 | 시작 시 초기화 | 종료 시 초기화 |
| --- | --- | --- |
| 스킬·상태이상 | `BattleSkillExecutor` | `BattleSkillRuntimeDriver` |
| 궁극기 게이지 | `BattleSkillExecutor` | `BattleSkillRuntimeDriver` |
| 속성 | — | `BattleSkillRuntimeDriver` |
| 흐트러짐 | — | `BattleSkillRuntimeDriver` |
| 패시브 | `BattlePassiveSystem` | `BattlePassiveSystem` |
| 캐릭터 선택 | — | `BattleManualMoveController` |

새 시스템을 만들 때마다 "어디서 초기화해야 하지?"를 매번 새로 판단해야 했고, 두 번 틀렸다.

- **Day52**: 전투 시작 시점에 초기화했더니 `Start()` 순서가 보장되지 않아 속성 등록이 지워질 뻔했다
- **Day55**: 종료 시점에만 초기화해서 테스트 사이로 속성이 새어 나가 5개 테스트가 실패했다

`BattleRuntimeStates`를 만들어 **한 곳**에 모았다.

```csharp
BattleRuntimeStates.BeginBattle(registry);   // 전투 중 효과만 초기화
BattleRuntimeStates.EndBattle();             // 전투 범위 전부 초기화
BattleRuntimeStates.ResetAll();              // 테스트 SetUp·TearDown 진입점
```

규칙도 클래스 맨 위에 문서로 남겼다.

- 전투 중 효과(스킬·상태이상, 게이지)는 **시작 시** 초기화
- 등록형(속성, 흐트러짐)은 **시작 시 지우지 않는다** — 스탯 생성과 순서 경합이 있어서. 대신 스탯 생성자가 잔여 상태를 정리한다
- 선택 상태는 `ResetAll`이 이벤트 구독까지 지우므로 전투 중에는 **`Clear`만** 쓴다

테스트 4개 파일의 여러 줄짜리 초기화 7묶음도 `BattleRuntimeStates.ResetAll()` 한 줄로 바꿨다.

## 2. 중복 UI 생성 함수 통합

`CreateText` · `CreateLabel` · `CreateButton`을 각자 구현한 파일이 **런타임 11개**였다. Day47에 `CreateImage` · `SetRect` · `Stretch`는 합쳤지만 텍스트·버튼은 파일마다 설정이 달라 보류했는데, 그 뒤로 6개가 더 생겼다.

비교해 보니 기본 설정은 전부 같고 **추가 옵션만** 달랐다.

| 옵션 | 쓰는 파일 |
| --- | --- |
| 넘침 허용 | 전투 뷰 4개 · 지도 · 로딩창 |
| 줄바꿈 | 던전 선택 · 캐릭터 버튼 |
| 자동 크기 맞춤 | 보스 연출 · 결과창 · 가방 · 상점 |
| 외곽선 | 흐트러짐 · 리듬 · 지도 |

그래서 **기본 함수 + 체인 옵션** 방식으로 통합했다.

```csharp
RuntimeUiKit.CreateText(parent, "Label", text, 20, color).Overflow().Outlined(outlineColor, new Vector2(2f, -2f));
```

각 파일의 설정을 그대로 옮겨 **화면은 달라지지 않는다.** 캐릭터 창 버튼은 원래 `targetGraphic`을 연결하지 않아 눌러도 색이 변하지 않았는데, 이 동작도 옵션(`assignTargetGraphic: false`)으로 보존했다. 기본 폰트도 매번 조회하지 않고 **한 번만 불러와 캐시**한다. 11개 파일에서 **174줄**이 줄었다.

## 3. 리플렉션 제거

`DungeonBattleFormationRuntimePatch`가 `BattleScreenController`의 private 필드를 **문자열 이름으로 찾아** 값을 넣고 있었다(Day28 코드). 필드 이름을 바꾸면 컴파일 오류 없이 조용히 동작이 멈추는 구조다. 공개 함수 `ConfigureDefaultEnemies()`를 만들어 교체했다.

## 4. `.slnx` git 추적 해제

`.gitignore`가 `*.sln` · `*.csproj`는 제외했지만 `.slnx`는 빠져 있었다. `dotnet build`나 Unity가 파일 순서를 바꿀 때마다 변경사항으로 잡혀서, 지금까지 커밋 전마다 수동으로 되돌려야 했다. `*.slnx`를 추가하고 추적을 해제했다. 로컬 파일은 그대로 남는다.

## 5. 큰 파일 분리

| 파일 | 변경 | 비고 |
| --- | --- | --- |
| `CharacterEquipmentScreenController` | 1,019 → **881줄** | 호감도 보상 패널을 `CharacterAffinityRewardPanel`로 분리 |
| `SaveData.cs` | 1,023 → **872줄** | `CharacterSaveData` · `PartyPresetSaveData` · `RegionErosionSaveData` · `SaveTimeOfDay`를 각자 파일로 |
| `BattleSkillRuntimeState` | 658 → **240줄** | `partial` 클래스로 도발 · 상태이상 수집 · 정화 · 주기 피해 파일 분리 |

코드 내용은 한 줄도 바꾸지 않고 **위치만 옮겼다.** `SaveData` 클래스 본체(약 860줄)는 세이브 호환성과 직결되는 곳이라 이번에는 나누지 않았다. 호감도 패널을 먼저 뗀 이유는, 다음 일차의 선물 UI와 이후 대화 UI가 붙을 자리이기 때문이다.

## 6. 씬 Runtime Patch 공통화

로비 상점 · 활력 · 시간 진행, 지도, 로딩 등 **씬 로드 시 UI를 주입하는 파일 12개**가 똑같은 코드를 반복하고 있었다.

- `RuntimeInitializeOnLoadMethod` 등록
- 중복 구독 방지 (`registered` 플래그 방식 또는 `-=` 후 `+=` 방식, 두 가지가 섞여 있음)
- 씬 이름 비교

`SceneRuntimePatch.Register(씬 이름, 콜백)`을 만들어 셋을 한 곳에서 처리한다. 각 파일은 이제 한 줄로 등록하고, 콜백은 해당 씬에서만 호출된다. 12개 파일에서 **114줄**이 줄었다.

## 7. 디버그 기능 분리

디버그 버튼 9종이 출시 빌드에도 그대로 들어가 있었다. 클래스를 `#if`로 빌드에서 빼면 **씬에 저장된 컴포넌트 참조가 깨지므로**, 클래스는 두고 **출시 빌드에서만 숨기는** `DevelopmentFeatures.HideInRelease()`를 만들었다. 에디터와 Development Build에서는 지금처럼 모두 보인다.

숨겨지는 기능: 전투 디버그 패널 버튼 · 체력 회복 · 던전 테스트 정보 오버레이, 로비 활력 ±1, 던전 선택 침식도 ±10, 캐릭터 창 호감도 ±10 · 테스트 장비 지급

## 8. 옛 Editor 스크립트 정리

Day1~20에 씬을 만들 때 쓴 셋업 스크립트 17개(약 4,900줄)를 `Editor/Legacy/`로 옮겼다. 씬은 이미 만들어져 있지만 다시 만들 때 필요할 수 있어서 지우지 않았다. `git mv`로 `.meta`까지 함께 옮겨서 GUID와 메뉴 명령이 그대로 유지된다.

## 9. 추가 — 효과 목록 정리 개선

`IsStunned` 같은 조회는 **호출될 때마다** 전체 효과 목록을 훑어 만료된 것을 지우고 있었다. 기본 공격 · 적 AI · 이동 · 보스가 매 프레임 호출한다.

가장 빨리 끝나는 효과의 시각(`nextExpiryAt`)을 기억해 두고, **그 시각 전에는 순회를 건너뛰게** 했다.

```csharp
if (now < nextExpiryAt)
{
    return;   // 아직 만료된 효과가 하나도 없음
}
```

건너뛰는 경우는 원래도 지울 게 없는 경우뿐이라 **결과는 기존과 완전히 같다.** 효과를 추가하거나 연장할 때는 기록을 앞당기기만 하고, 제거는 기록을 늦추는 방향으로만 작용한다. 그래서 기록된 시각이 실제 가장 이른 만료 시각보다 늦어지는 일이 없다. 연장된 효과가 이전 만료 시각에 잘못 지워지지 않는지까지 테스트로 확인했다.

## 10. 최적화 결과

| 항목 | 효과 |
| --- | --- |
| 초기화 통합 | 초기화 버그 재발 방지, 규칙 문서화 |
| UI 함수 통합 | −174줄, 폰트 캐시 |
| 리플렉션 제거 | 이름 변경 시 조용히 깨지던 위험 제거 |
| `.slnx` 추적 해제 | 빌드마다 생기던 변경사항 제거 |
| 큰 파일 분리 | 1,019 / 1,023 / 658줄 파일 축소 |
| Runtime Patch 공통화 | −114줄 |
| 디버그 분리 | 출시 빌드 정리 |
| Editor 정리 | 폴더 정리 |
| 효과 정리 개선 | 매 프레임 조회 비용 감소 |

---

## EditMode 테스트

**Part 1 — 신규 14개**
- `AffinityRewardTests` 7개: 도달 전 잠김 · 수령과 골드 지급 · 중복 수령 차단 · 다단계 도달 · 호감도 감소 후 유지 · **받은 단계만 보너스** · 보너스 없을 때 기존 스탯 동일
- `GameEventConditionTests` 7개: 빈 조건 통과 · 호감도 · 일차 · 시간대 · 클리어 · 플래그 · 여러 조건 결합과 사유 수집 · 개인 이벤트 목록 구성

**Part 2 — 신규 5개**
- `BattleRuntimeStatesTests` 3개: 통합 초기화 · **전투 시작 초기화가 등록형 상태를 지우지 않는지** · 효과 정리 최적화가 기존 결과와 같은지
- `SceneRuntimePatchTests` 2개: 중복 등록 무시 · 잘못된 입력 무시

## 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Editor.csproj` : 오류 0개 · 경고 0개
- 추적 파일 74개 변경, **+385 / −987줄**

## 변경 파일 정리

**Part 1 — 호감도 보상**

| 구분 | 파일 |
| --- | --- |
| 신규 | `Save/AffinityRewardCatalog.cs` · `AffinityRewardService.cs` · `GameEventCondition.cs` · `CharacterEventCatalog.cs` |
| 신규 | `UI/CharacterAffinityRewardPanel.cs` · `Tests/EditMode/AffinityRewardTests.cs` |
| 수정 | `Save/SaveData.cs`(수령 기록) · `Save/AffinityService.cs`(단계 이름 공용화) |
| 수정 | `Battle/BattleStatsFactory.cs` · `BattleSkillRuntimeDriver.cs` · `UI/CharacterEquipmentScreenController.cs` |

**Part 2 — 최적화**

| 구분 | 파일 |
| --- | --- |
| 신규 | `Battle/BattleRuntimeStates.cs` · `Core/SceneRuntimePatch.cs` · `Core/DevelopmentFeatures.cs` |
| 신규(분리) | `Save/CharacterSaveData.cs` · `PartyPresetSaveData.cs` · `RegionErosionSaveData.cs` · `SaveTimeOfDay.cs` |
| 신규(분리) | `Battle/BattleSkillRuntimeState.Taunt.cs` · `.StatusEffects.cs` · `.Cleanse.cs` · `.Periodic.cs` |
| 수정 | `UI/RuntimeUiKit.cs` + UI 함수 통합 11개 파일 |
| 수정 | 씬 Runtime Patch 12개 파일 · 디버그 기능 6개 파일 |
| 수정 | `.gitignore` (`*.slnx`), 테스트 5개 파일 |
| 이동 | `Editor/Phase*Setup*.cs` 17개 → `Editor/Legacy/` |
| 추적 해제 | `Project-H.slnx` |

## 다음 작업

- Day57 선물 시스템 — 선호도 · 호감도 증가량 · 일일 제한
- 호감도 디버그 버튼을 선물 시스템으로 대체
- `SaveData` 본체 분리는 세이브 구조 변경이 필요한 시점에 함께 검토
