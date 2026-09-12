# 프로젝트 H : 73일차 개발 로그

## 오늘 목표

72일차까지 **기능은 전부 갖췄지만 겉모습과 소리는 임시**였다. 배경은 코드로 그린 단색 그라데이션, 캐릭터는 색 실루엣, 소리는 아예 0개였다. 73일차는 **보고 들을 수 있는 상태**로 만들었다.

| 갈래 | 내용 |
| --- | --- |
| 🖼 **대화 배경 27종** | 밝은 일본 애니메이션풍 판타지로 직접 생성 |
| 👤 **캐릭터 초상화 12종** | 정사각 256×256, 캐릭터별 머리·눈·옷 색 |
| 🏘 **마을 카드 초상화** | 이름 글자 → 얼굴, 넘치면 가로 스크롤 (사용자 요청) |
| 🔊 **사운드 12개** | 효과음 9 · 배경음 3, 72일차 음량 설정과 연결 |
| ✍️ **폰트 자리** | `Resources/Fonts/GameFont`를 넣으면 전체 교체 |
| 📋 **리소스 점검 표** | 무엇이 들어왔고 무엇이 비었는지 에디터 메뉴로 출력 |

사용자와 정한 방식

1. 정식 리소스가 없으니 **그릴 수 있는 것은 직접 그려서** 넣는다
2. 전체 화풍은 **밝은 일본 애니메이션풍 판타지**
3. 마을 구역 카드의 캐릭터는 **정사각 초상화**, 칸을 넘으면 **가로 ScrollView**

## GitHub 기준

- 작업 시작 전 기준 커밋: `56a94c5` (`72일차 : 엔딩 4종…`)
- 브랜치: `main`

---

# Part 1. 대화 배경 27종

대사 229편을 훑어 **실제로 쓰이는 배경 키 27개**를 뽑고, 그 이름 그대로 PNG를 만들었다. 63일차에 만든 `DialogueArtFactory`가 `Resources/Dialogues/Backgrounds/{키}`를 먼저 찾으므로 **코드 수정 없이** 코드 그림을 대체한다.

| 분류 | 키 |
| --- | --- |
| 시간대 | `MORNING` `DAY` `EVENING` `NIGHT` |
| 마을 | `VILLAGE_PLAZA` `VILLAGE_MARKET` `VILLAGE_ONSEN` `VILLAGE_GUILD` `VILLAGE_INN` `INN_NIGHT` |
| 성역 | `SANCTUARY_PRACTICE` `CORRIDOR_MORNING` `GARDEN_POND` `CATHEDRAL_DUSK` |
| 지역 | `NOIR` `SILVARAN` `KARNIAN` `DESERT` `SEA_RIFT` `RUINS_CAMP` |
| 장소 | `OBSERVATORY_NIGHT` `ALCHEMY_LAB` `TEMPLE_EAST` `FORTRESS_YARD` `SOUTH_GATE` `BACK_ALLEY` `GUILD_HALL` |

- 1280×720 PNG · `.meta`에 `nPOTScale: 0` (67일차에 겪은 1024 축소 문제 방지)

## 화풍을 두 번 고친 과정

**1차** — 어둡고 채도가 낮아 실루엣 게임처럼 보였다. 게다가 실루엣이 화면 바닥까지 채워져 **깊이가 없었다**.

→ 그리는 순서를 `하늘 → 실루엣 → 바닥`으로 바꿔 바닥이 실루엣의 밑동을 덮게 했다.

**2차** — 사용자 요청으로 **밝은 애니메이션풍**으로 전면 재조정. 애니메이션 배경의 문법 네 가지를 규칙으로 넣었다.

| 규칙 | 이전 | 지금 |
| --- | --- | --- |
| 하늘 | 탁한 회청색 | 채도 높은 하늘색 → 지평선은 따뜻한 크림색 |
| **대기 원근** | 먼 산을 **어둡게** | 먼 것일수록 **하늘색 쪽으로 띄움** |
| 구름 | 없음 | **뭉게구름** — 아래는 연보라 그림자, 위는 흰 하이라이트 |
| 빛 | 없음 | 해·등불 **블룸**, 창문·나무 사이 **빛줄기** |
| 마감 | 비네트 0.42 · 그레인 0.025 | **0.14~0.3 · 0.012** |

---

# Part 2. 캐릭터 초상화 12종

256×256 정사각. 63일차에 정해 둔 **캐릭터 고유색**을 기준으로 머리·눈·옷 색을 잡고, 앞머리 모양을 3종(가운데 가르마 · 일자 · 사선)으로 나눠 실루엣이 겹치지 않게 했다.

`CharacterPortraitArt` — `Resources/Portraits/{캐릭터ID}` → 없으면 캐릭터 색 원. 던전 안내 아이콘·별하늘과 같은 교체 방식이다.

### 얼굴 비율을 한 번 고쳤다

처음 그린 얼굴은 **작고 좁은 데다 머리카락이 화면을 덮어** 화난 인상이었다. 다음을 고쳤다.

- 얼굴을 **둥근 계란형**으로 키우고 턱만 좁게
- 눈을 크게, 속눈썹은 **위쪽 곡선만** (일자 막대 → 곡선)
- 앞머리가 **눈 위에서 끝나도록** 높이 제한
- 어깨를 올려 가슴 위까지 보이게, 머리 하이라이트는 얇고 옅게

---

# Part 3. 마을 카드 초상화 (사용자 요청)

| 이전 | 지금 |
| --- | --- |
| `● 세레나, 엘렌, 릴리아…` 글자 | **정사각 초상화가 가로로 나란히** |
| 길어지면 잘림 | **넘치면 가로 스크롤** (`ScrollRect` + `RectMask2D`) |
| — | 맨 아래 `3명이 있습니다` / `영업 종료 (밤)` / `★ 새 동료 소식 1건 · …` |

- `preserveAspect`로 항상 정사각, 흰 테두리
- `raycastTarget = false` — **초상화를 눌러도 구역으로 들어간다**
- 갱신할 때마다 이전 초상화를 지우고 다시 배치, 스크롤은 항상 왼쪽부터

---

# Part 4. 사운드 (0개 → 12개)

| 분류 | 파일 |
| --- | --- |
| 효과음 9종 | `UI_CLICK` `UI_CONFIRM` `UI_CANCEL` `UI_PAGE` `GET_GOLD` `GET_ITEM` `BATTLE_HIT` `BATTLE_PERFECT` `LEVEL_UP` |
| 배경음 3곡 | `TITLE`(잔잔한 패드+아르페지오 16초) · `VILLAGE`(밝은 왈츠 12초) · `BATTLE`(8비트 리듬 8초) |

`AudioService` — 배경음 1채널 + 효과음 4채널(겹쳐 재생), 씬이 바뀌면 **배경음 자동 교체**.

| 항목 | 내용 |
| --- | --- |
| 음량 | **72일차 설정을 그대로 따름**. 설정을 바꾸면 `GameSettings.Changed`로 즉시 반영 |
| 버튼 소리 | `RuntimeUiKit.CreateButton` **한 곳**에 붙여 런타임 버튼 전체에 적용 |
| 파일이 없으면 | 조용히 넘어가 게임은 정상 동작 (정식 음원으로 덮어쓰면 교체) |

---

# Part 5. 폰트 · 리소스 점검 표

- `Resources/Fonts/GameFont.ttf`를 넣으면 `RuntimeUiKit.DefaultFont`가 그 폰트를 쓴다. 없으면 기존 기본 폰트
- **Tools → Project H → Phase 2 → 73일차 리소스 점검 표 출력** — 배경 27 · 초상화 12 · 스탠딩 12 · 배경음 3 · 효과음 9 · 폰트를 `[O] / [ ]`로 출력. 대사를 직접 읽어 배경 키를 뽑으므로 **대사를 추가하면 점검 항목도 자동으로 늘어난다**

---

## EditMode 테스트

**신규 4개 — `ResourceAndAudioTests`**

| 테스트 | 검증 |
| --- | --- |
| 배경 그림 | 대사가 쓰는 배경 키 **전부**에 PNG가 있다 |
| 초상화 | 12인 전원에게 초상화가 있고 **가로·세로가 같다**(정사각) |
| 배경음 매핑 | 타이틀 → `TITLE`, 전투 → `BATTLE`, 로비·마을 → `VILLAGE`, 부트스트랩 → 없음 |
| 소리 파일 | 배경음 3 · 효과음 9가 경로 규약대로 들어와 있고 이름이 겹치지 않는다 |

## 검증 결과

- `dotnet build ProjectH.Runtime.csproj` : 오류 0개 · 경고 0개
- `dotnet build ProjectH.Tests.EditMode.csproj` : 오류 0개
- `dotnet build ProjectH.Editor.csproj` : 오류 0개
- 생성 리소스 : 배경 27 · 초상화 12 · 소리 12 (전부 `.meta` 포함)

## 변경 파일 정리

| 구분 | 파일 |
| --- | --- |
| 신규 (그림) | `Resources/Dialogues/Backgrounds/*.png` 27장 · `Resources/Portraits/*.png` 12장 |
| 신규 (소리) | `Resources/Audio/Bgm/*.wav` 3개 · `Resources/Audio/Sfx/*.wav` 9개 |
| 신규 (코드) | `Core/AudioCatalog.cs` · `Core/AudioService.cs` · `UI/CharacterPortraitArt.cs` · `Editor/ResourceReportMenu.cs` |
| 신규 (테스트) | `ResourceAndAudioTests.cs` |
| 수정 | `UI/VillageScreenController.cs`(카드 초상화 줄) · `UI/RuntimeUiKit.cs`(폰트 교체 자리 · 버튼 클릭음) |

## 알아둘 점

- 그림·소리는 전부 **있으면 정식, 없으면 임시**로 동작한다. 정식 리소스가 준비되면 **같은 이름으로 덮어쓰기만** 하면 된다
- 배경 PNG의 `.meta`에는 반드시 `nPOTScale: 0`을 넣을 것 (없으면 1280 → 1024로 줄어든다)
- 애니메이션풍 배경을 추가할 때는 `하늘 → 실루엣 → 바닥` 순서와 **대기 원근(먼 것을 하늘색으로 띄우기)**을 지킬 것. 먼 것을 어둡게 하면 바로 칙칙해진다
- 버튼 소리는 `RuntimeUiKit.CreateButton`에서 한 번에 붙는다. 개별 버튼에 또 붙이면 두 번 난다
- 에디터 전용 스크립트는 `ProjectH.Editor.csproj`에 넣을 것 (register.py는 Runtime으로 보내므로 수동으로 옮겨야 한다)

## 다음 작업

- **Day74 최적화 · RC (마지막 일차)**
