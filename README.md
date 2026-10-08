# 프로젝트 H

Unity로 만드는 **리듬 전투 + 동료 육성 RPG**입니다.

주인공은 다른 세계로 소환된 용사입니다. 12명의 동료와 함께 대륙에 번지는 **침식**을 막고, 마지막에는 천 년 전 **이름을 지워 가둔 존재**와 마주합니다. 낮에는 던전에서 리듬으로 싸우고, 밤에는 마을에서 동료와 시간을 보냅니다.

| 항목 | 내용 |
| --- | --- |
| 엔진 | Unity (Input System 패키지 사용) |
| 언어 | C# — 모든 기능 줄에 한국어 주석 |
| 규모 | 런타임 스크립트 **324개** · 테스트 **146개** · 대사 **229편** |
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
└─ Tests/EditMode/ 테스트 146개
```

---

## 3. 자주 하는 작업

### 대사 추가

1. `Assets/ProjectH/Resources/Dialogues/{ID}.json` 작성
2. 화자는 `CH_*`(동료) · `NPC_*` · `HERO` · `NARRATION`
3. `"background"`에는 **그 장소의 배경 키**를 쓴다. 분위기가 비슷하다고 다른 장소의 키를 빌려 쓰면 엉뚱한 그림이 나온다 (새 장소면 키를 새로 만들고 `DialogueArtFactory`에 대체 배경을 적어 둔다)
4. `{HERO}`는 주인공 이름으로 자동 치환
5. 선택지에 `"flag": "..."`를 넣으면 스토리 플래그가 남는다 (엔딩 분기에 사용)
6. 대사 줄의 `"expression"`에는 표정 이름을 적는다 (`기본` · `미소` · `진지` · `부끄러움` · `놀람` · `슬픔` 등). **새 이름은 `ExpressionCatalog`에 먼저 등록할 것**
7. `.json.meta`는 `TextScriptImporter`로 만들 것

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
| 화면 배경 | `Resources/Dialogues/Backgrounds/SHOP.png` · `BLACKSMITH.png` · `VILLAGE.png` |
| 캐릭터 스탠딩 | `Resources/Dialogues/Standing/{캐릭터ID}_{표정}.png` (없는 표정은 `_normal`, 그것도 없으면 `{캐릭터ID}.png`) |
| 정사각 초상화 | `Resources/Portraits/{캐릭터ID}.png` (없으면 스탠딩에서 얼굴을 잘라 쓴다) |
| 궁극기 컷인 | `Resources/UltimateCutIns/{캐릭터ID}.png` (가로 3:2 그림 · 세로 가운데 띠만 보인다) |
| 전투 SD 그림 | `Resources/BattleUnits/{캐릭터ID}.png` · `{몬스터ID}.png` (아군은 없으면 스탠딩, 몬스터는 없으면 색 상자) |
| 전투 배경 | `Resources/Dialogues/Backgrounds/BATTLE_{지역}.png` (없으면 비슷한 대화 배경으로 대신) |
| 모험 지도 지역 아이콘 | `Resources/Map/Regions/{지역ID}.png` (없으면 색 원과 글자) |
| 배경음 · 효과음 | `Resources/Audio/Bgm/{키}.wav` · `Resources/Audio/Sfx/{키}.wav` |
| 폰트 | `Resources/Fonts/GameFont.ttf` |

> PNG의 `.meta`에는 반드시 **`nPOTScale: 0`**을 넣으세요. 없으면 1280 크기가 1024로 줄어듭니다.

**표정 파일 이름은 영어 7종**입니다. 대사에는 한글 이름을 적고, `ExpressionCatalog`가 둘을 이어 줍니다.

| 파일 이름의 표정 | 대사에 적는 이름 |
| --- | --- |
| `normal` | 기본 |
| `smile` | 미소 · 흥미 |
| `serious` | 진지 · 각오 · 생각 · 경계 |
| `shy` | 부끄러움 |
| `surprised` | 놀람 · 당황 |
| `sad` | 슬픔 · 상처 |
| `angry` | 화남 · 분노 |

스탠딩은 `{ID}_{표정}` → `{ID}_normal` → `{ID}` → 임시 실루엣 순서로 찾습니다. 표정 그림이 일부만 있어도 나머지는 기본 표정으로 나옵니다.

### 캐릭터가 보이는 크기 바꾸기

| 바꾸고 싶은 것 | 고칠 곳 |
| --- | --- |
| 대화 화면의 캐릭터 크기 | `DialogueStandingFrame.Scale` (1 = 전신 · 1.4 = 허벅지까지 · 1.7 = 허리까지) |
| 초상화에 머리가 들어오는 범위 | `StandingFaceCatalog.PortraitScale` (얼굴 높이의 몇 배를 자를지) |
| 궁극기 컷인의 얼굴 크기 | `UltimateCutInView.FaceHeightInBand` |
| 전투 유닛 그림이 차지하는 영역 | `BattleUnitArt.SpriteAnchorMin` · `SpriteAnchorMax` |
| 보스가 일반 몬스터보다 커 보이는 배율 | `BattleUnitArt.BossScale` |
| 전투 배경을 어둡게 덮는 정도 | `BattleBackgroundCatalog.DimAlpha` |
| 전투 유닛 움직임의 폭과 빠르기 | `BattleUnitMotionMath`의 상수 (대기 · 공격 · 피격 · 쓰러짐) |
| 전용 컷인 그림의 표시 너비 | `UltimateCutInView.WideArtWidth` |

초상화와 컷인은 `StandingFaceCatalog`의 **얼굴 위치표**를 씁니다. 스탠딩을 새로 받아 얼굴 위치가 달라지면 이 표의 값을 다시 재야 합니다.

---

## 4. 에디터 도구

`Tools → Project H → Phase 2`

| 메뉴 | 하는 일 |
| --- | --- |
| 67일차 밸런스 표 출력 | 던전별 예상 처치 시간 · 버티는 시간 · 여유 비율 |
| 73일차 리소스 점검 표 출력 | 그림 · 소리 · 폰트가 들어왔는지 `[O]/[ ]`로 표시 (표정 84칸 · 화면 배경 3곳 포함) |

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
| 대사 표정 이름은 **`ExpressionCatalog`에 등록** | 등록하지 않은 이름은 기본 표정으로 나오고 테스트가 실패한다 |
| 전체 화면 배경은 **`BackgroundFit.Apply`** | 그냥 늘리면 16:9가 아닌 화면에서 그림이 찌그러진다 (자식이 없는 배경에만) |
| 스탠딩을 바꾸면 **`StandingFaceCatalog`를 다시 잴 것** | 초상화와 컷인이 이 표로 얼굴을 찾는다. 값이 어긋나면 얼굴이 잘린다 |
| 전투 연출은 **바디 그림만** 움직일 것 | 유닛의 실제 위치는 사거리와 이동 계산에 쓰인다. 위치를 흔들면 전투 결과가 달라진다 |

---

## 6. 아직 임시인 것

기능은 전부 동작합니다. 그림은 75일차부터 정식 일러스트로 바꾸는 중입니다.

| 항목 | 지금 상태 | 필요한 것 |
| --- | --- | --- |
| 캐릭터 스탠딩 | **정식 일러스트 84장**(1024×1536 · 12인 × 표정 7종) | 완료 |
| 궁극기 컷인 | 정식 스탠딩을 재사용 | 전용 그림 12장 |
| CG · 정지 컷신 | 테스트 1장 | 개인 1화 · 결속 5단계 · 특별한 밤 |
| 배경음 · 효과음 | 코드로 합성한 간이 음원 | 정식 음원 |
| 폰트 | Unity 기본 | 한글 폰트 1종 |
| 대화 배경 | **정식 일러스트 33장**(1672×941) | 완료 |
| 화면 배경 | **정식 일러스트 3장** (상점 · 대장간 · 마을 지도) | 완료 |
| 초상화 | 스탠딩에서 얼굴을 잘라 사용 | 완료 (원하면 전용 그림) |
| NPC 스탠딩 | **정식 일러스트 4장** (상점 주인 · 대장장이 · 그림자 · 아르카이) | 완료 |
| 전투 아군 그림 | **정식 SD 12장**(1024×1024) | 완료 |
| 전투 몬스터 그림 | **정식 SD 34장** (보스 14 · 일반 20) | 완료 |
| 전투 배경 | 지역별로 그 지역의 대화 배경을 사용 | 원하면 전용 배경 8장 |
| 모험 지도 지역 아이콘 | **8개 지역 적용** · 바다는 색 원 | 바다 1개 |

---

## 7. 테스트

```bash
# 로컬에서 컴파일만 확인
dotnet build ProjectH.Runtime.csproj
dotnet build ProjectH.Tests.EditMode.csproj
dotnet build ProjectH.Editor.csproj
```

실제 테스트는 Unity의 **Test Runner → EditMode**에서 실행합니다. 출시 전 점검은 `ReleaseCandidateTests`가 담당합니다 — 새 게임부터 최종장·엔딩 4종까지 한 번도 막히지 않는지, 17던전이 순서대로 열리는지, **깨진 저장에서 복구되는지**를 확인합니다.
