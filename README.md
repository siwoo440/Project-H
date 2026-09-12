# 프로젝트 H

Unity로 만드는 **리듬 전투 + 동료 육성 RPG**입니다.

주인공은 다른 세계로 소환된 용사입니다. 12명의 동료와 함께 대륙에 번지는 **침식**을 막고, 마지막에는 천 년 전 **이름을 지워 가둔 존재**와 마주합니다. 낮에는 던전에서 리듬으로 싸우고, 밤에는 마을에서 동료와 시간을 보냅니다.

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity (Input System 패키지 사용) |
| 언어 | C# — 모든 기능 줄에 한국어 주석 |
| 규모 | 런타임 스크립트 **314개** · 테스트 **140개** · 대사 **229편** |
| 진행 | 1~74일차 완료 (일차별 기록은 [`Devlogs/`](Devlogs)) |

---

## 1. 어떤 게임인가

```text
타이틀 ─ 새 게임 ─ 이름 입력
   │
   └─ 로비 ──┬─ 모험 지도 ─ 던전 선택 ─ 전투(리듬) ─ 결과
             ├─ 마을 ─ 광장·시장·온천·여관·길드
             ├─ 상점 · 대장간 · 가방 · 캐릭터 · 파티
             └─ 일기장 (다시 보기 · 세계관 · 몬스터)
   ESC ─ 저장/불러오기 · 설정 · 타이틀
```

| 축 | 내용 |
| --- | --- |
| **전투** | 리듬 판정(Perfect/Good/Miss) · 스킬 블록 · 궁극기 컷인 · 보스 페이즈 · 속성 · 상태이상 |
| **육성** | 레벨 · 장비 5칸(강화 +5 / 초월 ★3) · 룬 15종 5칸 · 전용 장비 12종 |
| **관계** | 호감도 · 결속 5단계 · 선물 · 개인 이벤트 · 마을 구역 이벤트 |
| **진행** | 일차/시간대(아침·점심·저녁·밤) · 활력 · 17던전 · 검은 균열 · 길드 의뢰 |
| **이야기** | 프롤로그 + 챕터 1~6 · 엔딩 4종 · 회차(뉴게임+) |

---

## 2. 폴더 구조

```text
Assets/ProjectH/
├─ Scripts/
│  ├─ Core/        게임 관리자 · 씬 이름 · 설정 · 회차 기록 · 소리 · 로그
│  ├─ Data/        ScriptableObject 정의 (캐릭터 · 몬스터 · 던전 · 아이템 · 스킬)
│  ├─ Save/        저장 데이터와 규칙 (룬 · 장비 · 결속 · 호감도 · 슬롯)
│  ├─ Battle/      전투 전체 (리듬 · 스킬 · 보스 · 밸런스 계산기)
│  ├─ Dungeon/     던전 진행 · 지역 · 검은 균열
│  ├─ Village/     마을 구역 · 배치 · 길드 의뢰 · 합류
│  ├─ Story/       챕터 · 최종장 · 엔딩
│  ├─ Dialogue/    대사 파일 해석 · 진행기
│  ├─ Diary/       일기장 · 세계관
│  ├─ Minigame/    야시장 놀이판 5종
│  ├─ Shop/        상점 회전 · 거래
│  ├─ UI/          모든 화면 (대부분 코드로 생성)
│  └─ Editor/      에디터 도구 (밸런스 표 · 리소스 점검 표)
├─ Data/           실제 데이터 에셋 (.asset)
├─ Resources/      런타임에 불러오는 그림 · 소리 · 대사
└─ Tests/EditMode/ 테스트 140개
```

---

## 3. 자주 하는 작업

### 대사 추가

1. `Assets/ProjectH/Resources/Dialogues/{ID}.json` 작성
2. 화자는 `CH_*`(동료) · `NPC_*` · `HERO` · `NARRATION`
3. `{HERO}`는 주인공 이름으로 자동 치환
4. 선택지에 `"flag": "..."`를 넣으면 스토리 플래그가 남는다 (엔딩 분기에 사용)
5. `.json.meta`는 `TextScriptImporter`로 만들 것

### 던전 추가

1. `Data/Dungeons/DG0NN.asset` 생성 후 `ProjectHDataCatalog`에 등록
2. `DungeonSelectionRuntimeState.SupportedDungeonIds`에 ID 추가
3. `DungeonProgressionPolicy.GetPreviousDungeonId`에 해금 순서 추가
4. `DungeonBattleTestProfile`에 적 배율 추가
5. **`rewardExp`는 반드시 `60 + 20 × 권장레벨`** (테스트가 공식으로 검사)
6. `AdventureRegionCatalog`의 지역에 넣을 것

### 그림 · 소리 교체

정식 리소스는 **같은 이름으로 덮어쓰기만** 하면 됩니다. 없으면 코드로 그린 임시 그림·무음으로 동작합니다.

| 종류 | 경로 |
| --- | --- |
| 대화 배경 | `Resources/Dialogues/Backgrounds/{배경키}.png` |
| 캐릭터 스탠딩 | `Resources/Dialogues/Standing/{캐릭터ID}.png` |
| 정사각 초상화 | `Resources/Portraits/{캐릭터ID}.png` |
| 궁극기 컷인 | `Resources/UltimateCutIns/{캐릭터ID}.png` |
| 배경음 · 효과음 | `Resources/Audio/Bgm/{키}.wav` · `Resources/Audio/Sfx/{키}.wav` |
| 폰트 | `Resources/Fonts/GameFont.ttf` |

> PNG의 `.meta`에는 반드시 **`nPOTScale: 0`**을 넣으세요. 없으면 1280 크기가 1024로 줄어듭니다.

---

## 4. 에디터 도구

`Tools → Project H → Phase 2`

| 메뉴 | 하는 일 |
| --- | --- |
| 67일차 밸런스 표 출력 | 던전별 예상 처치 시간 · 버티는 시간 · 여유 비율 |
| 73일차 리소스 점검 표 출력 | 그림 · 소리 · 폰트가 들어왔는지 `[O]/[ ]`로 표시 |

---

## 5. 개발 규칙

프로젝트 전체가 지키는 약속입니다. 새 코드도 여기에 맞춰 주세요.

| 규칙 | 이유 |
| --- | --- |
| 기능 줄마다 **한국어 주석** | 74일치 코드를 나중에 읽기 위해 |
| 화면은 **코드로 생성**(`RuntimeUiKit`) | 씬 파일 충돌을 피하려고 |
| 줄이 늘어나는 패널은 **ScrollRect + 픽셀 쌓기** | 비율 배치는 줄이 늘면 반드시 넘친다 |
| 키 입력은 **`Keyboard.current`** | 이 프로젝트는 Input System이라 구형 `Input`은 예외를 던진다 |
| 골드 차감은 **`TrySpendGold`** | `AddGold`는 음수를 무시한다 |
| 안내 로그는 **`GameLog.Info`** | 출시 빌드에서 호출과 문자열 조립이 통째로 빠진다 |
| 회차를 넘겨 남길 값은 **`PlayerProfile`** | `SaveData`는 한 회차 안에서만 유효 |
| 설정 값은 **`GameSettings` 한 곳** | 화면과 기능이 따로 저장하면 어긋난다 |
| 새 스크립트는 **`.meta`를 함께 만들 것** | Unity가 먼저 만들면 GUID가 어긋난다 |

---

## 6. 아직 임시인 것

기능은 전부 동작하지만, 아래는 **코드로 그린 임시 리소스**입니다.

| 항목 | 지금 상태 | 필요한 것 |
| --- | --- | --- |
| 캐릭터 스탠딩 | 색 실루엣 | 12인 × 표정 7종 |
| 궁극기 컷인 | 스탠딩 재사용 | 12장 |
| CG · 정지 컷신 | 테스트 1장 | 개인 1화 · 결속 5단계 · 특별한 밤 |
| 배경음 · 효과음 | 코드로 합성한 간이 음원 | 정식 음원 |
| 폰트 | Unity 기본 | 한글 폰트 1종 |
| 대화 배경 · 초상화 | **코드로 그린 애니메이션풍**(사용 가능) | 원하면 정식 일러스트 |

---

## 7. 테스트

```bash
# 로컬에서 컴파일만 확인
dotnet build ProjectH.Runtime.csproj
dotnet build ProjectH.Tests.EditMode.csproj
dotnet build ProjectH.Editor.csproj
```

실제 테스트는 Unity의 **Test Runner → EditMode**에서 실행합니다. 출시 전 점검은 `ReleaseCandidateTests`가 담당합니다 — 새 게임부터 최종장·엔딩 4종까지 한 번도 막히지 않는지, 17던전이 순서대로 열리는지, **깨진 저장에서 복구되는지**를 확인합니다.
