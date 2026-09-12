using System.Collections; // 코루틴 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 게임 관리자·씬 이름·개발 기능 표시
using ProjectH.Data; // 캐릭터·던전 데이터 기능
using ProjectH.Dialogue; // 대화 파일 기능
using ProjectH.SaveSystem; // 저장·시간·활력 기능
using ProjectH.Dungeon; // 검은 균열 기능 (Day67 추가)
using ProjectH.Minigame; // 시장 놀이판 기능 (Day70 추가)
using ProjectH.Village; // 마을 구역·배치·행동·길드 의뢰 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 마을 화면 중복 방지
    public sealed class VillageScreenController : MonoBehaviour // 마을 화면 (Day62 신규 — 지도 5구역 → 구역 화면, 무작위 캐릭터 · 구역 행동 · 시간/일차 소비)
    {
        private static readonly Color BarColor = new Color(0.06f, 0.07f, 0.09f, 0.86f); // 상단 띠
        private static readonly Color CardColor = new Color(0.08f, 0.09f, 0.12f, 0.78f); // 구역 카드
        private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.10f, 0.88f); // 행동 패널
        private static readonly Color ActionColor = new Color(0.30f, 0.46f, 0.66f, 1f); // 행동 버튼 파랑
        private static readonly Color TimeColor = new Color(0.86f, 0.62f, 0.30f, 1f); // 시간 소비 버튼 주황
        private static readonly Color SubColor = new Color(0.26f, 0.28f, 0.34f, 1f); // 보조 버튼 회색
        private static readonly Color HintColor = new Color(0.82f, 0.84f, 0.88f, 1f); // 안내 글자
        private static readonly Color RecruitColor = new Color(0.36f, 0.62f, 0.40f, 1f); // 새 동료 소식 초록 (Day64 추가)
        private static readonly Color MinigameColor = new Color(0.62f, 0.42f, 0.26f, 1f); // 야시장 놀이판 갈색 (Day70 추가)
        private const float PanelViewHeight = 700f; // 행동 패널 기준 높이 (기존 비율 배치를 픽셀로 바꾸는 기준, Day67 추가)

        private readonly Dictionary<VillageZone, Button> zoneCards = new Dictionary<VillageZone, Button>(); // 지도 구역 카드
        private readonly Dictionary<VillageZone, Text> zoneCardPeople = new Dictionary<VillageZone, Text>(); // 카드 속 상태 글자 (닫힘 · 없음 · 새 동료 소식)
        private readonly Dictionary<VillageZone, RectTransform> zoneCardFaces = new Dictionary<VillageZone, RectTransform>(); // 카드 속 초상화 줄 (Day73 추가)
        private readonly Dictionary<VillageZone, ScrollRect> zoneCardScrolls = new Dictionary<VillageZone, ScrollRect>(); // 초상화 가로 스크롤 (Day73 추가)
        private readonly List<GameObject> zoneCardPortraits = new List<GameObject>(); // 만들어 둔 초상화 (갱신 때 제거)
        private readonly List<GameObject> zoneStandings = new List<GameObject>(); // 구역 화면 스탠딩
        private readonly List<GameObject> panelItems = new List<GameObject>(); // 행동 패널 동적 요소
        private VillageZone? currentZone; // 현재 구역 (null = 지도)
        private VillageZone? lastPanelZone; // 마지막으로 그린 구역 (스크롤 위치 유지용, Day67 추가)
        private string selectedCharacterId; // 선택 캐릭터
        private Transform canvasRoot; // 캔버스 루트
        private Image background; // 배경
        private Text titleText; // 제목
        private Text timeText; // 일차·시간대
        private Text resourceText; // 활력·결속 자원·골드
        private GameObject mapRoot; // 지도 화면
        private GameObject zoneRoot; // 구역 화면
        private RectTransform standingArea; // 스탠딩 영역
        private RectTransform panelContent; // 행동 패널 내용 (스크롤 내용물)
        private ScrollRect panelScroll; // 행동 패널 스크롤 (Day67 추가 — 버튼이 많아지면 아래로 내려서 봄)
        private float panelCursor; // 다음 줄이 들어갈 위치 (픽셀, 위에서부터)
        private Text statusText; // 결과 안내
        private Image fade; // 암전
        private CharacterGiftPanel giftPanel; // 선물 패널 (Day57 재사용)
        private bool busy; // 대화·암전 중 입력 막기

        private void Start() // 마을 화면 시작
        {
            EnsureEventSystem(); // UI 입력 시스템 보장
            BuildUi(); // UI 생성
            string rift = RiftService.Refresh(GetSave()); // 검은 균열 기한·발생 처리 (Day67 추가)
            if (!string.IsNullOrEmpty(rift)) Save(); // 균열 변화 저장
            SetStatus(string.IsNullOrEmpty(rift) ? "구역을 눌러 들어가 보세요. 시간대마다 캐릭터들이 다른 곳에 있어요." : rift); // 첫 안내 (균열 소식 우선)
            ShowMap(); // 지도부터
        }

        private void EnsureEventSystem() // UI EventSystem 보장
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>(); // 기존 EventSystem 조회

            if (eventSystem != null) // 기존 EventSystem 존재 확인
            {
                StandaloneInputModule legacyInputModule = eventSystem.GetComponent<StandaloneInputModule>(); // 구형 입력 모듈 조회

                if (legacyInputModule != null) // 구형 입력 모듈 존재 확인
                {
                    legacyInputModule.enabled = false; // 구형 입력 모듈 비활성화
                    Destroy(legacyInputModule); // 구형 입력 모듈 제거 예약
                }

                if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) // 신규 입력 모듈 존재 확인
                {
                    eventSystem.gameObject.AddComponent<InputSystemUIInputModule>(); // 신규 Input System UI 모듈 추가
                }

                return; // EventSystem 보정 완료
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 신규 EventSystem 생성
            eventSystemObject.transform.SetParent(transform, false); // 마을 화면 하위 연결
        }

        private void BuildUi() // 전체 UI 구성
        {
            GameObject canvasObject = new GameObject("VillageCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Runtime Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 화면 하위 연결
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 100; // 대화 화면(600) 아래
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 대응 방식
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간
            canvasRoot = canvasObject.transform; // 루트 저장
            background = CreateImage(canvasRoot, "Background", Color.white); // 배경
            background.raycastTarget = false; // 입력 통과
            Stretch(background.rectTransform); // 전체 확장
            BuildMap(); // 지도 화면
            BuildZone(); // 구역 화면
            BuildTopBar(); // 상단 바 (위에 그림)
            Image statusBox = CreateImage(canvasRoot, "StatusBox", new Color(0.05f, 0.06f, 0.08f, 0.82f)); // 안내 상자
            statusBox.raycastTarget = false; // 입력 통과
            SetRect(statusBox.rectTransform, new Vector2(0.01f, 0.015f), new Vector2(0.675f, 0.10f)); // 왼쪽 아래
            statusText = CreateText(statusBox.transform, "Status", string.Empty, 18, Color.white, FontStyle.Normal, TextAnchor.MiddleLeft).Wrap(); // 안내 글자
            Stretch(statusText.rectTransform, 14f); // 여백
            giftPanel = CharacterGiftPanel.Create(canvasRoot, OnGiftChanged, null); // 선물 패널 (보상 바로가기는 캐릭터 창에서)
            fade = CreateImage(canvasRoot, "Fade", new Color(0f, 0f, 0f, 0f)); // 암전 막
            fade.raycastTarget = false; // 평소엔 입력 통과
            Stretch(fade.rectTransform); // 전체
        }

        private void BuildTopBar() // 상단 바 : 뒤로 · 제목 · 일차/시간대 · 활력/결속/골드
        {
            Image bar = CreateImage(canvasRoot, "TopBar", BarColor); // 상단 띠
            SetRect(bar.rectTransform, new Vector2(0f, 0.915f), new Vector2(1f, 1f)); // 상단 배치
            Button back = CreateButton(bar.transform, "Back", "◀  뒤로", SubColor); // 뒤로 (구역 → 지도 → 로비)
            SetRect(back.GetComponent<RectTransform>(), new Vector2(0.01f, 0.14f), new Vector2(0.11f, 0.86f)); // 왼쪽
            back.onClick.AddListener(GoBack); // 뒤로 가기
            titleText = CreateText(bar.transform, "Title", "마을", 30, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            SetRect(titleText.rectTransform, new Vector2(0.125f, 0f), new Vector2(0.40f, 1f)); // 제목 배치
            timeText = CreateText(bar.transform, "Time", string.Empty, 24, TimeColor, FontStyle.Bold); // 일차·시간대
            SetRect(timeText.rectTransform, new Vector2(0.40f, 0f), new Vector2(0.60f, 1f)); // 가운데
            resourceText = CreateText(bar.transform, "Resources", string.Empty, 19, Color.white, FontStyle.Bold, TextAnchor.MiddleRight).BestFit(12); // 자원
            SetRect(resourceText.rectTransform, new Vector2(0.60f, 0f), new Vector2(0.985f, 1f)); // 오른쪽
        }

        private void BuildMap() // 지도 화면 : 구역 카드 5개
        {
            mapRoot = new GameObject("Map", typeof(RectTransform)); // 지도 루트
            mapRoot.transform.SetParent(canvasRoot, false); // 캔버스 하위
            Stretch((RectTransform)mapRoot.transform); // 전체
            Vector2[] mins = { new Vector2(0.37f, 0.40f), new Vector2(0.05f, 0.14f), new Vector2(0.68f, 0.14f), new Vector2(0.05f, 0.58f), new Vector2(0.68f, 0.58f) }; // 광장 · 시장 · 온천 · 여관 · 길드 위치

            foreach (VillageZoneInfo info in VillageZoneCatalog.All) // 구역 순회
            {
                VillageZone zone = info.Zone; // 구역 값
                Button card = CreateButton(mapRoot.transform, "Zone_" + zone, string.Empty, CardColor); // 구역 카드
                Vector2 min = mins[(int)zone]; // 왼쪽 아래
                SetRect(card.GetComponent<RectTransform>(), min, min + new Vector2(0.27f, 0.25f)); // 카드 크기
                AddOutline(card.gameObject, new Color(1f, 1f, 1f, 0.35f)); // 흰 테두리
                Text name = CreateText(card.transform, "Name", info.Name, 34, Color.white, FontStyle.Bold, TextAnchor.UpperLeft); // 구역 이름
                SetRect(name.rectTransform, new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.95f)); // 위
                Text desc = CreateText(card.transform, "Desc", info.Description, 15, HintColor, FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 설명
                SetRect(desc.rectTransform, new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.64f)); // 가운데
                Text people = CreateText(card.transform, "People", string.Empty, 15, TimeColor, FontStyle.Bold, TextAnchor.LowerLeft).BestFit(10); // 상태 글자
                SetRect(people.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.16f)); // 맨 아래
                BuildCardFaceStrip(card.transform, zone); // 초상화 줄 (Day73 — 이름 대신 얼굴, 넘치면 가로 스크롤)
                card.onClick.AddListener(() => EnterZone(zone)); // 구역 들어가기
                zoneCards[zone] = card; // 카드 저장
                zoneCardPeople[zone] = people; // 글자 저장
            }
        }

        private void BuildCardFaceStrip(Transform card, VillageZone zone) // 카드 안에 정사각 초상화가 가로로 늘어서는 줄 (넘치면 스크롤)
        {
            Image panel = CreateImage(card, "Faces", new Color(0f, 0f, 0f, 0f)); // 스크롤 바탕 (투명)
            SetRect(panel.rectTransform, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.44f)); // 설명 아래 · 상태 글자 위
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); // 보이는 영역 (넘친 초상화는 잘림)
            viewportObject.transform.SetParent(panel.transform, false); // 바탕 하위
            RectTransform viewport = (RectTransform)viewportObject.transform; // 영역 저장
            Stretch(viewport); // 바탕 전체
            GameObject contentObject = new GameObject("Content", typeof(RectTransform)); // 초상화가 쌓이는 루트
            contentObject.transform.SetParent(viewport, false); // 보이는 영역 하위
            RectTransform content = (RectTransform)contentObject.transform; // 루트 저장
            content.anchorMin = new Vector2(0f, 0f); // 왼쪽 기준
            content.anchorMax = new Vector2(0f, 1f); // 왼쪽 기준
            content.pivot = new Vector2(0f, 0.5f); // 왼쪽 고정
            content.offsetMin = new Vector2(0f, 0f); // 위아래 여백 없음
            content.offsetMax = new Vector2(0f, 0f); // 위아래 여백 없음
            ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>(); // 가로 스크롤
            scroll.content = content; // 내용 연결
            scroll.viewport = viewport; // 보이는 영역 연결
            scroll.horizontal = true; // 가로만
            scroll.vertical = false; // 세로 없음
            scroll.movementType = ScrollRect.MovementType.Clamped; // 끝에서 멈춤
            scroll.scrollSensitivity = 30f; // 휠 감도
            scroll.inertia = false; // 관성 없이 바로 멈춤
            zoneCardFaces[zone] = content; // 저장
            zoneCardScrolls[zone] = scroll; // 저장
        }

        private void RefreshCardFaces(VillageZone zone, List<string> present, bool open) // 카드 초상화 줄 채우기 (Day73 추가)
        {
            RectTransform content = zoneCardFaces[zone]; // 초상화 루트
            float size = ((RectTransform)content.parent).rect.height; // 한 변 = 줄 높이 (정사각)
            if (size <= 1f) size = 64f; // 첫 프레임 보호
            float step = size + 6f; // 초상화 간격
            int count = open ? present.Count : 0; // 표시 수

            for (int index = 0; index < count; index++) // 있는 사람 순회
            {
                string characterId = present[index]; // 캐릭터
                Image face = CreateImage(content, "Face_" + characterId, Color.white); // 초상화 칸
                face.sprite = CharacterPortraitArt.Get(characterId, out bool placeholder); // 초상화
                face.color = placeholder ? CharacterPortraitArt.GetPlaceholderTint(characterId) : Color.white; // 임시 그림은 캐릭터 색
                face.preserveAspect = true; // 정사각 비율 유지
                face.raycastTarget = false; // 카드 클릭이 먹히도록 입력 통과
                RectTransform rect = face.rectTransform; // 영역
                rect.anchorMin = new Vector2(0f, 0.5f); // 왼쪽 기준
                rect.anchorMax = new Vector2(0f, 0.5f); // 왼쪽 기준
                rect.pivot = new Vector2(0f, 0.5f); // 왼쪽 고정
                rect.sizeDelta = new Vector2(size, size); // 정사각
                rect.anchoredPosition = new Vector2(index * step, 0f); // 가로로 나란히
                AddOutline(face.gameObject, new Color(1f, 1f, 1f, 0.55f)); // 흰 테두리
                zoneCardPortraits.Add(face.gameObject); // 목록 등록
            }

            content.sizeDelta = new Vector2(Mathf.Max(0f, (count * step) - 6f), 0f); // 내용 너비 (넘치면 스크롤)
            if (zoneCardScrolls[zone] != null) zoneCardScrolls[zone].horizontalNormalizedPosition = 0f; // 항상 왼쪽부터
        }

        private void BuildZone() // 구역 화면 : 스탠딩 영역 + 오른쪽 행동 패널
        {
            zoneRoot = new GameObject("Zone", typeof(RectTransform)); // 구역 루트
            zoneRoot.transform.SetParent(canvasRoot, false); // 캔버스 하위
            Stretch((RectTransform)zoneRoot.transform); // 전체
            GameObject standingObject = new GameObject("Standings", typeof(RectTransform)); // 스탠딩 영역
            standingObject.transform.SetParent(zoneRoot.transform, false); // 구역 하위
            standingArea = (RectTransform)standingObject.transform; // 영역 저장
            SetRect(standingArea, new Vector2(0.01f, 0.10f), new Vector2(0.675f, 0.91f)); // 왼쪽 큰 영역
            Image panel = CreateImage(zoneRoot.transform, "ActionPanel", PanelColor); // 행동 패널
            SetRect(panel.rectTransform, new Vector2(0.69f, 0.015f), new Vector2(0.99f, 0.90f)); // 오른쪽
            AddOutline(panel.gameObject, new Color(1f, 1f, 1f, 0.25f)); // 테두리
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); // 보이는 영역 (밖으로 나간 버튼은 잘림)
            viewportObject.transform.SetParent(panel.transform, false); // 패널 하위
            RectTransform viewport = (RectTransform)viewportObject.transform; // 영역 저장
            Stretch(viewport, 14f); // 여백
            GameObject content = new GameObject("Content", typeof(RectTransform)); // 동적 내용 루트
            content.transform.SetParent(viewport, false); // 보이는 영역 하위
            panelContent = (RectTransform)content.transform; // 내용 저장
            panelContent.anchorMin = new Vector2(0f, 1f); // 위쪽 기준
            panelContent.anchorMax = new Vector2(1f, 1f); // 위쪽 기준
            panelContent.pivot = new Vector2(0.5f, 1f); // 위쪽 고정
            panelContent.offsetMin = new Vector2(0f, panelContent.offsetMin.y); // 좌우 여백 없음
            panelContent.offsetMax = new Vector2(0f, panelContent.offsetMax.y); // 좌우 여백 없음
            panelContent.sizeDelta = new Vector2(0f, PanelViewHeight); // 기본 높이
            panelScroll = panel.gameObject.AddComponent<ScrollRect>(); // 세로 스크롤
            panelScroll.content = panelContent; // 내용 연결
            panelScroll.viewport = viewport; // 보이는 영역 연결
            panelScroll.horizontal = false; // 가로 스크롤 없음
            panelScroll.vertical = true; // 세로 스크롤 사용
            panelScroll.movementType = ScrollRect.MovementType.Clamped; // 끝에서 멈춤
            panelScroll.scrollSensitivity = 40f; // 휠 감도
            panelScroll.inertia = false; // 관성 없이 바로 멈춤
        }

        private void ShowMap() // 지도 화면 표시
        {
            currentZone = null; // 지도 상태
            selectedCharacterId = null; // 선택 해제
            mapRoot.SetActive(true); // 지도 표시
            zoneRoot.SetActive(false); // 구역 숨김
            background.sprite = DialogueArtFactory.GetBackground("VILLAGE"); // 마을 전경
            RefreshAll(); // 갱신
        }

        private void EnterZone(VillageZone zone) // 구역 들어가기
        {
            if (busy) return; // 진행 중 입력 무시
            SaveData saveData = GetSave(); // 현재 저장

            if (!VillageZoneCatalog.IsOpen(zone, GameTimeService.GetCurrentPhase(saveData))) // 운영 시간 확인
            {
                SetStatus($"{VillageZoneCatalog.Get(zone).Name}은 밤에 문을 닫아요. 내일 아침에 다시 와 주세요."); // 안내
                return; // 중단
            }

            currentZone = zone; // 구역 저장
            List<string> present = VillagePresenceService.GetCharactersIn(saveData, zone); // 여기 있는 사람
            selectedCharacterId = present.Count > 0 ? present[0] : null; // 첫 사람 선택
            mapRoot.SetActive(false); // 지도 숨김
            zoneRoot.SetActive(true); // 구역 표시
            background.sprite = DialogueArtFactory.GetBackground(VillageZoneCatalog.Get(zone).BackgroundKey); // 구역 배경
            SetStatus(present.Count > 0 ? $"{JoinNames(present)}이(가) 있어요." : "지금은 아무도 없네요. 시간이 지나면 누군가 올지도 몰라요."); // 안내
            RefreshAll(); // 갱신
        }

        private void GoBack() // 뒤로 : 구역 → 지도 → 로비
        {
            if (busy) return; // 진행 중 입력 무시
            if (giftPanel.IsOpen) giftPanel.Toggle(); // 선물 패널 닫기

            if (currentZone.HasValue) // 구역 화면
            {
                ShowMap(); // 지도로
                return; // 종료
            }

            LoadScene(GameScenes.Lobby); // 로비로
        }

        private void RefreshAll() // 상단 바 · 지도 카드 · 구역 화면 갱신
        {
            SaveData saveData = GetSave(); // 현재 저장
            SaveTimeOfDay phase = GameTimeService.GetCurrentPhase(saveData); // 현재 시간대
            titleText.text = currentZone.HasValue ? $"마을 · {VillageZoneCatalog.Get(currentZone.Value).Name}" : "마을"; // 제목
            timeText.text = $"DAY {GameTimeService.GetCurrentDay(saveData)}  ·  {VillageActionService.GetPhaseLabel(phase)}"; // 일차·시간대
            resourceText.text = $"활력 {VitalityService.GetVitality(saveData)}/{SaveData.MaxVitality}    결속 자원 {BondService.GetResource(saveData)}/{BondCatalog.MaxResource}    ● {GoldCurrencyService.GetGold(saveData):N0} G"; // 자원

            foreach (GameObject portrait in zoneCardPortraits) Destroy(portrait); // 이전 초상화 제거 (Day73)
            zoneCardPortraits.Clear(); // 목록 비움

            foreach (VillageZoneInfo info in VillageZoneCatalog.All) // 지도 카드 순회
            {
                bool open = VillageZoneCatalog.IsOpen(info.Zone, phase); // 운영 여부
                List<string> present = VillagePresenceService.GetCharactersIn(saveData, info.Zone); // 있는 사람
                RefreshCardFaces(info.Zone, present, open); // 초상화 줄 (Day73 — 이름 대신 얼굴)
                int recruits = info.Zone == VillageZone.Guild ? RecruitService.GetPending(saveData).Count : 0; // 길드 새 동료 소식 수 (Day64)
                string status = !open ? "영업 종료 (밤)" : present.Count > 0 ? $"{present.Count}명이 있습니다" : "아무도 없음"; // 상태 글자
                zoneCardPeople[info.Zone].text = open && recruits > 0 ? $"★ 새 동료 소식 {recruits}건 · {status}" : status; // 카드 글자
                zoneCards[info.Zone].GetComponent<Image>().color = open ? CardColor : new Color(0.05f, 0.05f, 0.06f, 0.65f); // 닫힌 구역 어둡게
            }

            if (currentZone.HasValue) // 구역 화면
            {
                RefreshStandings(saveData); // 스탠딩
                RefreshPanel(saveData); // 행동 패널
            }
        }

        private void RefreshStandings(SaveData saveData) // 구역에 있는 캐릭터 스탠딩 (최대 2명, 누르면 선택)
        {
            foreach (GameObject item in zoneStandings) Destroy(item); // 이전 스탠딩 제거
            zoneStandings.Clear(); // 목록 비움
            List<string> present = VillagePresenceService.GetCharactersIn(saveData, currentZone.Value); // 있는 사람
            int count = Mathf.Min(2, present.Count); // 최대 2명

            for (int index = 0; index < count; index++) // 스탠딩 생성
            {
                string characterId = present[index]; // 캐릭터
                float left = count == 1 ? 0.25f : index * 0.50f; // 1명이면 가운데
                Button hit = CreateButton(standingArea, "Standing_" + characterId, string.Empty, new Color(1f, 1f, 1f, 0f)); // 누르는 영역
                SetRect(hit.GetComponent<RectTransform>(), new Vector2(left, 0f), new Vector2(left + 0.50f, 1f)); // 자리
                hit.onClick.AddListener(() => SelectCharacter(characterId)); // 선택
                Image image = CreateImage(hit.transform, "Image", Color.white); // 스탠딩
                image.sprite = DialogueArtFactory.GetStanding(characterId, null, out bool placeholder); // 정식 스탠딩 우선
                Color tint = placeholder ? DialogueArtFactory.GetCharacterTint(characterId) : Color.white; // 실루엣 색
                image.color = characterId == selectedCharacterId ? tint : Color.Lerp(tint, Color.black, 0.35f); // 선택한 사람 밝게
                image.preserveAspect = true; // 비율 유지
                image.raycastTarget = false; // 버튼이 입력 받음
                Stretch(image.rectTransform); // 영역 채움
                Text label = CreateText(hit.transform, "Name", GetName(characterId), 24, Color.white).Outlined(new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f)); // 이름
                SetRect(label.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.10f)); // 발밑
                zoneStandings.Add(hit.gameObject); // 목록 등록
            }
        }

        private void RefreshPanel(SaveData saveData) // 행동 패널 다시 그리기 (내용 높이에 맞춰 스크롤 범위 갱신, Day67 추가)
        {
            bool keepScroll = lastPanelZone == currentZone; // 같은 구역이면 보던 위치 유지
            float previous = panelScroll == null ? 1f : panelScroll.verticalNormalizedPosition; // 이전 스크롤 위치
            BuildPanelRows(saveData); // 줄 채우기
            panelContent.sizeDelta = new Vector2(0f, Mathf.Max(PanelViewHeight, panelCursor + 12f)); // 내용 높이 (넘치면 스크롤)
            if (panelScroll != null) panelScroll.verticalNormalizedPosition = keepScroll ? previous : 1f; // 구역이 바뀌면 맨 위부터
            lastPanelZone = currentZone; // 마지막으로 그린 구역 기록
        }

        private void BuildPanelRows(SaveData saveData) // 구역 행동 · 여기 있는 사람 · 선택 캐릭터 행동 줄 채우기
        {
            foreach (GameObject item in panelItems) Destroy(item); // 이전 요소 제거
            panelItems.Clear(); // 목록 비움
            panelCursor = 0f; // 위에서부터 다시
            VillageZone zone = currentZone.Value; // 현재 구역
            VillageZoneInfo info = VillageZoneCatalog.Get(zone); // 구역 정보
            float y = 1f; // 위에서부터 채움
            y = AddLabel(info.Name, 28, Color.white, FontStyle.Bold, y, 0.07f); // 구역 이름
            y = AddLabel(info.Description, 15, HintColor, FontStyle.Normal, y, 0.08f); // 설명
            y = AddZoneActions(saveData, zone, y); // 구역 행동
            List<string> present = VillagePresenceService.GetCharactersIn(saveData, zone); // 있는 사람
            y = AddLabel(present.Count > 0 ? "여기 있는 사람" : "여기 있는 사람 · 없음", 17, TimeColor, FontStyle.Bold, y - 0.015f, 0.045f); // 소제목

            foreach (string characterId in present) // 사람 순회
            {
                string captured = characterId; // 클릭용 복사
                string text = $"{GetName(characterId)}   ♥ {AffinityService.GetAffinity(saveData, characterId)}  · 결속 {BondService.GetLevel(saveData, characterId)}"; // 이름·호감도·결속
                y = AddButton(text, captured == selectedCharacterId ? TimeColor : SubColor, y, 0.055f, true, () => SelectCharacter(captured)); // 선택 버튼
            }

            if (string.IsNullOrEmpty(selectedCharacterId)) return; // 선택 없음
            y = AddLabel($"{GetName(selectedCharacterId)}와(과)", 17, TimeColor, FontStyle.Bold, y - 0.015f, 0.045f); // 소제목
            bool canTalk = DialogueService.CanTalk(saveData, selectedCharacterId, out string talkReason); // 대화 가능 여부
            y = AddButton(canTalk ? "대화하기  (시간 소비 없음)" : "대화하기  (이번 시간대 완료)", ActionColor, y, 0.06f, canTalk, Talk); // 일상 대화
            y = AddButton($"선물하기  (오늘 {GiftService.GetRemainingToday(saveData, selectedCharacterId)}회 남음)", ActionColor, y, 0.06f, true, OpenGift); // 선물

            if (zone == VillageZone.Inn) // 여관 : 특별한 밤
            {
                bool canInn = VillageActionService.CanStartInnEvent(saveData, selectedCharacterId, out string innReason); // 조건
                y = AddButton($"특별한 밤  (결속 자원 {VillageActionService.InnEventBondCost} · 다음 날까지)", new Color(0.52f, 0.30f, 0.52f, 1f), y, 0.06f, canInn, StartInnEvent); // 특별한 밤
                if (!canInn) y = AddLabel(innReason, 14, HintColor, FontStyle.Normal, y, 0.04f); // 조건 안내
            }
            else if (VillageActionService.HasZoneEvent(zone)) // 구역 이벤트
            {
                bool canEvent = VillageActionService.CanDoZoneEvent(saveData, selectedCharacterId, zone, out string eventReason); // 조건
                y = AddButton("함께 시간 보내기  (시간 1칸)", TimeColor, y, 0.06f, canEvent, StartZoneEvent); // 구역 이벤트
                if (!canEvent) y = AddLabel(eventReason, 14, HintColor, FontStyle.Normal, y, 0.04f); // 조건 안내
            }

            if (!canTalk) AddLabel(talkReason, 14, HintColor, FontStyle.Normal, y, 0.04f); // 대화 불가 안내
        }

        private float AddZoneActions(SaveData saveData, VillageZone zone, float y) // 구역 전용 행동 버튼
        {
            switch (zone) // 구역 분기
            {
                case VillageZone.Market: // 시장 : 상점 · 대장간 · 야시장 놀이판
                    y = AddButton("상점 가기", SubColor, y, 0.055f, true, () => LoadScene(GameScenes.Shop)); // 상점
                    y = AddButton("대장간 가기", SubColor, y, 0.055f, true, () => LoadScene(GameScenes.Blacksmith)); // 대장간
                    return AddButton($"야시장 놀이판  ({MinigameService.GetRemainingText(saveData)})", MinigameColor, y, 0.06f, MinigameService.GetRemainingPlays(saveData) > 0, OpenMinigame); // 놀이판 (Day70 추가)
                case VillageZone.Onsen: // 온천 : 목욕
                    return AddButton($"온천에 몸 담그기  (시간 1칸 · 활력 +{VillageActionService.OnsenVitality})", TimeColor, y, 0.06f, true, Bathe); // 목욕
                case VillageZone.Inn: // 여관 : 잠자기
                    bool canSleep = VillageActionService.CanSleep(saveData, out _); // 저녁·밤만
                    return AddButton(canSleep ? "방 잡고 잠자기  (다음 날 아침 · 활력 전체 회복)" : "방 잡고 잠자기  (저녁부터)", TimeColor, y, 0.06f, canSleep, Sleep); // 잠자기
                case VillageZone.Guild: // 길드 : 새 동료 소식 · 게시판 · 모험
                    y = AddRecruitActions(saveData, y); // 새 동료 합류 (Day64)
                    y = AddQuestActions(saveData, y); // 오늘의 길드 의뢰 (Day67)
                    return AddButton("모험 떠나기  (던전 선택)", SubColor, y, 0.055f, true, () => LoadScene(GameScenes.DungeonSelect)); // 던전
                default: // 광장
                    return y; // 구역 행동 없음
            }
        }

        private float AddRecruitActions(SaveData saveData, float y) // 길드 새 동료 소식 버튼 (대기 중 첫 동료 1명씩 · 개발용 전원 합류)
        {
            List<RecruitDefinition> pending = RecruitService.GetPending(saveData); // 조건을 채운 합류 대기 동료

            if (pending.Count > 0) // 소식 있음
            {
                RecruitDefinition first = pending[0]; // 첫 동료
                string more = pending.Count > 1 ? $"  (외 {pending.Count - 1}명)" : string.Empty; // 남은 수
                y = AddButton($"★ 새 동료 소식 · {GetName(first.CharacterId)}{more}", RecruitColor, y, 0.06f, true, () => StartRecruit(first.CharacterId)); // 합류 이야기
            }

            if (DevelopmentFeatures.Enabled && RecruitService.All.Count > CountOwnedRecruits(saveData)) // 개발용 : 아직 합류 안 한 동료가 있을 때만
            {
                y = AddButton("전원 합류  (개발용)", new Color(0.45f, 0.30f, 0.30f, 1f), y, 0.05f, true, RecruitAllForDebug); // 조건 무시 전원 합류
            }

            return y; // 다음 위치
        }

        private static int CountOwnedRecruits(SaveData saveData) // 이미 합류한 추가 동료 수
        {
            int count = 0; // 합류 수

            foreach (RecruitDefinition definition in RecruitService.All) // 정의 순회
            {
                if (saveData != null && saveData.HasCharacter(definition.CharacterId)) count++; // 보유
            }

            return count; // 합류 수 반환
        }

        private void StartRecruit(string characterId) // 합류 이야기 재생 → 끝까지 보면 동료 추가
        {
            if (busy) return; // 진행 중 입력 무시
            RecruitDefinition definition = RecruitService.Find(characterId); // 정의
            if (definition == null) return; // 정의 없음
            OpenDialogue(definition.ScriptId, runner => Finish(true, RecruitService.CompleteRecruit(GetSave(), characterId, runner, GetName(characterId)))); // 대화 → 합류
        }

        private void RecruitAllForDebug() // 개발용 전원 합류 (합류 이벤트 없이 바로)
        {
            int added = RecruitService.RecruitAllForDebug(GetSave()); // 합류
            Finish(added > 0, $"[개발용] 새 동료 {added}명이 바로 합류했습니다."); // 저장·안내
        }

        private float AddLabel(string text, int size, Color color, FontStyle style, float top, float height) // 패널 글자 한 줄 추가 후 다음 위치 반환
        {
            Text label = CreateText(panelContent, "Label", text, size, color, style, TextAnchor.MiddleLeft).Wrap(); // 글자
            PlaceRow(label.rectTransform, top, height, 0.008f); // 배치 (위에서부터 쌓기)
            panelItems.Add(label.gameObject); // 목록 등록
            return top - height - 0.008f; // 다음 위치
        }

        private float AddButton(string text, Color color, float top, float height, bool interactable, UnityEngine.Events.UnityAction action) // 패널 버튼 한 줄 추가 후 다음 위치 반환
        {
            Button button = CreateButton(panelContent, "Action", text, color); // 버튼
            PlaceRow((RectTransform)button.transform, top, height, 0.012f); // 배치 (위에서부터 쌓기)
            button.interactable = interactable; // 가능 여부
            button.onClick.AddListener(action); // 기능 연결
            panelItems.Add(button.gameObject); // 목록 등록
            return top - height - 0.012f; // 다음 위치
        }

        private void PlaceRow(RectTransform rect, float top, float height, float gap) // 비율로 받은 줄을 위에서부터 쌓아 배치 (Day67 — 스크롤 가능하도록 픽셀 배치)
        {
            float rowHeight = height * PanelViewHeight; // 줄 높이 (픽셀)
            float offset = Mathf.Max(0f, (1f - top) * PanelViewHeight); // 호출부가 비운 간격 (예: y - 0.015f)
            float y = Mathf.Max(panelCursor, offset); // 겹치지 않게 아래로
            rect.anchorMin = new Vector2(0f, 1f); // 위쪽 기준
            rect.anchorMax = new Vector2(1f, 1f); // 위쪽 기준
            rect.pivot = new Vector2(0.5f, 1f); // 위쪽 고정
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y); // 좌우 여백 없음
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y); // 좌우 여백 없음
            rect.sizeDelta = new Vector2(0f, rowHeight); // 줄 높이
            rect.anchoredPosition = new Vector2(0f, -y); // 위에서부터 내려오며 배치
            panelCursor = y + rowHeight + (gap * PanelViewHeight); // 다음 줄 위치
        }

        private void SelectCharacter(string characterId) // 캐릭터 선택
        {
            if (busy) return; // 진행 중 입력 무시
            selectedCharacterId = characterId; // 선택 저장
            if (giftPanel.IsOpen) giftPanel.Toggle(); // 선물 패널 닫기
            RefreshAll(); // 갱신
        }

        private void Talk() // 일상 대화 (Day58 대화 재사용, 시간 소비 없음)
        {
            SaveData saveData = GetSave(); // 현재 저장
            string characterId = selectedCharacterId; // 대화 캐릭터

            if (!DialogueService.CanTalk(saveData, characterId, out string reason)) // 가능 여부
            {
                SetStatus(reason); // 안내
                return; // 중단
            }

            string scriptId = DialogueService.GetTalkScriptId(characterId, GameTimeService.GetCurrentPhase(saveData)); // 시간대 대화 파일
            OpenDialogue(scriptId, runner => // 대화 열기
            {
                DialogueRewardResult result = DialogueService.CompleteTalk(GetSave(), characterId, runner); // 호감도 반영
                Finish(result.Applied, result.Message); // 저장·안내
            });
        }

        private void OpenMinigame() // 야시장 놀이판 열기 (Day70 추가 — 닫을 때 결과를 저장하고 화면 갱신)
        {
            if (busy) return; // 진행 중 입력 무시
            busy = true; // 입력 막기
            MinigameView.Open(GetSave(), GetData(), message => { busy = false; Finish(true, message); }); // 놀이판
        }

        private void OpenGift() // 선물 패널 열기 (Day57 재사용)
        {
            SaveData saveData = GetSave(); // 현재 저장
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(selectedCharacterId); // 캐릭터 진행
            if (character == null) return; // 캐릭터 없음
            giftPanel.Refresh(saveData, GetData(), character, GetName(selectedCharacterId)); // 선물 패널 갱신
            if (!giftPanel.IsOpen) giftPanel.Toggle(); // 열기
            giftPanel.transform.SetAsLastSibling(); // 맨 위
        }

        private void OnGiftChanged() // 선물 후 갱신
        {
            SaveData saveData = GetSave(); // 현재 저장
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(selectedCharacterId); // 캐릭터 진행
            if (character != null) giftPanel.Refresh(saveData, GetData(), character, GetName(selectedCharacterId)); // 패널 갱신
            RefreshAll(); // 화면 갱신
        }

        private void StartZoneEvent() // 구역 이벤트 (함께 시간 보내기)
        {
            SaveData saveData = GetSave(); // 현재 저장
            string characterId = selectedCharacterId; // 캐릭터
            VillageZone zone = currentZone.Value; // 구역

            if (!VillageActionService.CanDoZoneEvent(saveData, characterId, zone, out string reason)) // 조건
            {
                SetStatus(reason); // 안내
                return; // 중단
            }

            OpenDialogue(VillageActionService.GetZoneEventScriptId(characterId, zone), runner => Finish(true, VillageActionService.CompleteZoneEvent(GetSave(), characterId, zone, runner))); // 대화 → 호감도 · 시간 1칸
        }

        private void StartInnEvent() // 여관 특별한 밤 (진입 구조 : 조건 → 암전 → 대사 → 다음 날 아침)
        {
            SaveData saveData = GetSave(); // 현재 저장
            string characterId = selectedCharacterId; // 캐릭터

            if (!VillageActionService.CanStartInnEvent(saveData, characterId, out string reason)) // 조건
            {
                SetStatus(reason); // 안내
                return; // 중단
            }

            StartCoroutine(InnEventRoutine(characterId)); // 암전 연출
        }

        private IEnumerator InnEventRoutine(string characterId) // 암전 → 대사 → 밝아짐
        {
            busy = true; // 입력 막기
            fade.raycastTarget = true; // 뒤 클릭 차단
            yield return FadeTo(1f, 0.6f); // 암전
            bool finished = false; // 대화 종료 여부
            string message = string.Empty; // 결과 안내
            DialogueOverlayView view = DialogueOverlayView.Open(DialogueLibrary.Load(VillageActionService.GetInnEventScriptId(characterId)), runner => // 특별한 밤 대사
            {
                message = VillageActionService.CompleteInnEvent(GetSave(), characterId, runner); // 결속 자원 · 호감도 · 다음 날
                finished = true; // 종료 표시
            });

            if (view == null) // 대사 파일 없음
            {
                message = $"대사 파일을 찾을 수 없습니다. ({VillageActionService.GetInnEventScriptId(characterId)})"; // 안내
                finished = true; // 종료 처리
            }

            while (!finished) yield return null; // 대화 끝까지 대기
            Save(); // 저장
            RefreshAll(); // 다음 날 배치로 갱신
            SetStatus(message); // 결과 안내
            yield return FadeTo(0f, 0.8f); // 밝아짐
            fade.raycastTarget = false; // 입력 통과
            busy = false; // 입력 허용
        }

        private IEnumerator FadeTo(float alpha, float duration) // 암전 막 투명도 변화
        {
            float start = fade.color.a; // 시작 투명도

            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime) // 시간 진행
            {
                fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(start, alpha, time / duration)); // 보간
                yield return null; // 다음 프레임
            }

            fade.color = new Color(0f, 0f, 0f, alpha); // 최종값
        }

        private void Bathe() // 온천 목욕
        {
            VillageActionService.TryBathe(GetSave(), out string message); // 활력 + 시간 1칸
            Finish(true, message); // 저장·안내
        }

        private void Sleep() // 여관 잠자기
        {
            bool slept = VillageActionService.TrySleep(GetSave(), out string message); // 다음 날 아침
            if (slept) StartCoroutine(FlashBlack()); // 짧은 암전
            Finish(slept, message); // 저장·안내
        }

        private IEnumerator FlashBlack() // 잠자기 짧은 암전
        {
            fade.color = new Color(0f, 0f, 0f, 1f); // 바로 암전
            yield return FadeTo(0f, 0.9f); // 밝아짐
        }

        private float AddQuestActions(SaveData saveData, float y) // 오늘의 길드 의뢰 (Day67 신규 — 하루 3개 · 완료하면 보상 받기)
        {
            List<GuildQuestProgress> quests = GuildQuestService.GetToday(saveData); // 오늘의 의뢰
            if (quests.Count == 0) return y; // 의뢰 없음
            y = AddLabel($"오늘의 의뢰 ({GameTimeService.GetCurrentDay(saveData)}일차)", 17, TimeColor, FontStyle.Bold, y - 0.01f, 0.045f); // 소제목

            foreach (GuildQuestProgress quest in quests) // 의뢰 순회
            {
                string questId = quest.Definition.Id; // 클릭용 복사
                DungeonData dungeon = quest.Definition.Kind == GuildQuestKind.ClearDungeon && GetData() != null ? GetData().GetDungeon(quest.Definition.Target) : null; // 의뢰 던전
                string title = GuildQuestCatalog.GetTitle(quest.Definition, dungeon == null ? string.Empty : dungeon.DisplayName); // 제목
                string state = quest.Claimed ? "보상 받음" : quest.CanClaim ? "보상 받기" : $"{quest.Progress}/{quest.Definition.Required}"; // 상태
                y = AddButton($"{title}  ·  {state}", quest.CanClaim ? TimeColor : SubColor, y, 0.055f, quest.CanClaim, () => ClaimQuest(questId)); // 의뢰 줄
                y = AddLabel($"보상 {quest.Definition.RewardText}", 13, HintColor, FontStyle.Normal, y + 0.004f, 0.03f); // 보상 안내
            }

            bool bonus = GuildQuestService.CanClaimBonus(saveData); // 보너스 수령 가능
            return AddButton(bonus ? $"전부 완료 보너스 받기  ({GuildQuestCatalog.BonusGold}G · 결속 자원 {GuildQuestCatalog.BonusBondResource})" : saveData != null && saveData.QuestBoard.BonusClaimed ? "전부 완료 보너스 받음" : "전부 완료 보너스  (의뢰 3개 완료 시)", bonus ? ActionColor : SubColor, y, 0.05f, bonus, ClaimQuestBonus); // 보너스 줄
        }

        private void ClaimQuest(string questId) // 의뢰 보상 받기
        {
            Finish(true, GuildQuestService.TryClaim(GetSave(), GetData(), questId)); // 보상 · 저장 · 갱신
        }

        private void ClaimQuestBonus() // 전부 완료 보너스 받기
        {
            Finish(true, GuildQuestService.TryClaimBonus(GetSave())); // 보상 · 저장 · 갱신
        }

        private void OpenDialogue(string scriptId, System.Action<DialogueRunner> finished) // 대화 열기 (파일 없으면 안내)
        {
            if (DialogueOverlayView.Open(DialogueLibrary.Load(scriptId), finished) == null) // 대화 화면
            {
                SetStatus($"대사 파일을 찾을 수 없습니다. ({scriptId})"); // 안내
            }
        }

        private void Finish(bool changed, string message) // 행동 결과 저장·안내·갱신
        {
            if (changed) Save(); // 즉시 저장
            SetStatus(message); // 안내
            List<string> present = currentZone.HasValue ? VillagePresenceService.GetCharactersIn(GetSave(), currentZone.Value) : new List<string>(); // 시간이 흘러 바뀐 배치

            if (currentZone.HasValue && !VillageZoneCatalog.IsOpen(currentZone.Value, GameTimeService.GetCurrentPhase(GetSave()))) // 구역이 닫힘 (시장 밤)
            {
                ShowMap(); // 지도로
                SetStatus(message + "  (시장이 문을 닫았어요)"); // 안내
                return; // 종료
            }

            if (!present.Contains(selectedCharacterId)) selectedCharacterId = present.Count > 0 ? present[0] : null; // 떠난 캐릭터 선택 해제
            RefreshAll(); // 갱신
        }

        private void Save() // 즉시 저장
        {
            if (GameManager.Instance != null && GameManager.Instance.Save != null) GameManager.Instance.Save.SaveCurrent(); // 저장
        }

        private void SetStatus(string message) => statusText.text = message ?? string.Empty; // 안내 글자

        private void LoadScene(string sceneName) // 씬 이동
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                SetStatus("SceneLoader를 찾을 수 없습니다. Bootstrap 씬부터 실행해 주세요."); // 안내
                return; // 중단
            }

            GameManager.Instance.Scenes.LoadScene(sceneName); // 씬 로드
        }

        private string GetName(string characterId) // 캐릭터 이름
        {
            CharacterData data = GetData() == null ? null : GetData().GetCharacter(characterId); // 캐릭터 원본
            return data == null ? characterId : data.DisplayName; // 이름 반환
        }

        private string JoinNames(List<string> ids) // 이름 나열
        {
            List<string> names = new List<string>(); // 이름 목록
            foreach (string id in ids) names.Add(GetName(id)); // 이름 변환
            return string.Join(", ", names); // 쉼표로 연결
        }

        private static SaveData GetSave() => GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장 조회

        private static DataManager GetData() => GameManager.Instance == null ? null : GameManager.Instance.Data; // 데이터 관리자 조회

        private static Image CreateImage(Transform parent, string objectName, Color color) => RuntimeUiKit.CreateImage(parent, objectName, color); // 기본 Image 생성 (RuntimeUiKit 위임)

        private static Text CreateText(Transform parent, string objectName, string value, int fontSize, Color color, FontStyle fontStyle = FontStyle.Bold, TextAnchor alignment = TextAnchor.MiddleCenter) // 공통 UI 텍스트 생성 (RuntimeUiKit 위임)
        {
            return RuntimeUiKit.CreateText(parent, objectName, value, fontSize, color, fontStyle, alignment); // 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string objectName, string labelText, Color color) // 공통 UI 버튼 생성 (RuntimeUiKit 위임)
        {
            Button button = RuntimeUiKit.CreateButton(parent, objectName, color); // 버튼 생성
            Text label = CreateText(button.transform, "Label", labelText, 18, Color.white).BestFit(10); // 자동 크기 라벨
            Stretch(label.rectTransform, 6f); // 라벨 확장
            return button; // 버튼 반환
        }

        private static void AddOutline(GameObject target, Color color) // UI 외곽선 추가
        {
            Outline outline = target.GetComponent<Outline>(); // 기존 Outline 조회

            if (outline == null) // Outline 존재 확인
            {
                outline = target.AddComponent<Outline>(); // Outline 추가
            }

            outline.effectColor = color; // 외곽선 색상
            outline.effectDistance = new Vector2(2f, -2f); // 외곽선 두께
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) => RuntimeUiKit.SetRect(rect, anchorMin, anchorMax); // UI 앵커 영역 설정 (RuntimeUiKit 위임)

        private static void Stretch(RectTransform rect, float padding = 0f) => RuntimeUiKit.Stretch(rect, padding); // RectTransform 전체 확장 (RuntimeUiKit 위임)
    }
}
