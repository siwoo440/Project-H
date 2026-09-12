using System.Collections; // 코루틴 기능
using System.Collections.Generic; // 목록 자료형
using System.Text; // 문자열 조립 기능
using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.Data; // 장비·아이템 데이터 기능
using ProjectH.SaveSystem; // 저장·강화 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 대장간 화면 중복 방지
    public sealed class BlacksmithScreenController : MonoBehaviour // 대장간 화면 (Day61 신규 — 왼쪽 강화 작업대, 오른쪽 대장장이·대사 또는 장비 선택 목록)
    {
        private const int ListColumns = 4; // 장비 목록 열 수
        private const int ListRows = 3; // 장비 목록 행 수
        private const int ListPerPage = ListColumns * ListRows; // 한 페이지 장비 수
        private static readonly Color PanelColor = new Color(0.06f, 0.05f, 0.06f, 0.86f); // 어두운 패널 색
        private static readonly Color EmberColor = new Color(1f, 0.55f, 0.22f, 1f); // 불씨 주황
        private static readonly Color GoldColor = new Color(1f, 0.82f, 0.36f, 1f); // 골드 글자 색
        private static readonly Color TabOffColor = new Color(0.24f, 0.21f, 0.22f, 1f); // 일반 탭 색

        private sealed class EquipmentCardView // 장비 선택 카드 참조
        {
            public GameObject Root; // 카드 루트
            public Image Icon; // 아이콘
            public Text Name; // 이름
            public Text Owner; // 장착자
            public string InstanceId; // 표시 중인 장비
        }

        private readonly List<EquipmentCardView> equipmentCards = new List<EquipmentCardView>(); // 장비 카드 12칸
        private readonly Button[] scrollButtons = new Button[3]; // 주문서 등급 버튼 C·B·A
        private string selectedInstanceId; // 선택 장비
        private ScrollGrade selectedGrade = ScrollGrade.C; // 선택 주문서 등급
        private bool transcendMode; // 초월 모드 여부
        private int listPage; // 장비 목록 페이지
        private int lineSeed; // 대사 변주 값
        private Text goldText; // 골드 표시
        private Button enhanceTab; // 강화 탭
        private Button transcendTab; // 초월 탭
        private Image ownerFrame; // 장착자 초상 틀
        private Image ownerPortrait; // 장착자 초상
        private Text ownerText; // 장착자 이름
        private Image equipmentIcon; // 장비 칸
        private Text equipmentName; // 장비 이름
        private Image materialIcon; // 주문서·재료 칸
        private Text materialText; // 재료 수량
        private Text stageText; // +N >>> +N+1
        private Text effectText; // 강화 내용
        private Text chanceText; // 성공 확률
        private Text costText; // 비용
        private Button actionButton; // 강화하기·초월하기
        private Text actionLabel; // 버튼 글자
        private Image flash; // 결과 번쩍임
        private Text dialogueText; // 대장장이 대사
        private GameObject listPanel; // 장비 선택 목록
        private Text listPageText; // 목록 페이지
        private Button listPrev; // 이전 페이지
        private Button listNext; // 다음 페이지

        private void Start() // 대장간 화면 시작
        {
            EnsureEventSystem(); // UI 입력 시스템 보장
            BuildUi(); // UI 생성
            Say(NpcLineKind.Greeting, null); // 입장 인사
            listPanel.SetActive(true); // 처음엔 장비 선택 목록 (인사 대사 유지)
            SetMode(false); // 강화 모드로 시작 (화면 갱신 포함)
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
            eventSystemObject.transform.SetParent(transform, false); // 대장간 화면 하위 연결
        }

        private void BuildUi() // 전체 UI 구성
        {
            GameObject canvasObject = new GameObject("BlacksmithCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Runtime Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 화면 하위 연결
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 100; // 정렬 순서
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 대응 방식
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간
            Image background = CreateImage(canvas.transform, "Background", Color.white); // 대장간 배경
            background.sprite = DialogueArtFactory.GetBackground("BLACKSMITH"); // 화로 배경 (정식 배경 우선)
            background.raycastTarget = false; // 입력 통과
            Stretch(background.rectTransform); // 전체 확장
            BuildTopBar(background.transform); // 상단 바
            BuildBlacksmith(background.transform); // 오른쪽 대장장이
            BuildWorkbench(background.transform); // 왼쪽 작업대
            BuildEquipmentList(background.transform); // 오른쪽 장비 선택 목록 (대장장이 위)
        }

        private void BuildTopBar(Transform parent) // 상단 바 : 로비로·제목·골드 (로비 하단 '대장간' 버튼으로 진입)
        {
            Image bar = CreateImage(parent, "TopBar", new Color(0.04f, 0.03f, 0.03f, 0.90f)); // 상단 띠
            SetRect(bar.rectTransform, new Vector2(0f, 0.915f), new Vector2(1f, 1f)); // 상단 배치
            Button backButton = CreateButton(bar.transform, "BackButton", "◀  로비", TabOffColor, Color.white); // 로비 복귀 버튼
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.01f, 0.14f), new Vector2(0.11f, 0.86f)); // 왼쪽 배치
            backButton.onClick.AddListener(() => LoadScene(GameScenes.Lobby)); // 로비 이동
            Text title = CreateText(bar.transform, "Title", "대장간", 30, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            SetRect(title.rectTransform, new Vector2(0.125f, 0f), new Vector2(0.40f, 1f)); // 제목 배치
            goldText = CreateText(bar.transform, "Gold", "● 0 G", 26, GoldColor, FontStyle.Bold, TextAnchor.MiddleRight); // 골드 표시
            SetRect(goldText.rectTransform, new Vector2(0.70f, 0f), new Vector2(0.985f, 1f)); // 골드 배치
        }

        private void BuildBlacksmith(Transform parent) // 오른쪽 대장장이 스탠딩 + 대사 상자
        {
            NpcProfile npc = NpcLineCatalog.Blacksmith; // 대장장이 프로필
            Image standing = CreateImage(parent, "Blacksmith", Color.white); // 스탠딩
            standing.sprite = DialogueArtFactory.GetStanding(npc.Id, null, out bool placeholder); // 정식 스탠딩 우선 · 없으면 실루엣
            standing.color = placeholder ? DialogueArtFactory.GetCharacterTint(npc.Id) : Color.white; // 실루엣이면 구릿빛
            standing.preserveAspect = true; // 비율 유지
            standing.raycastTarget = false; // 입력 통과
            SetRect(standing.rectTransform, new Vector2(0.60f, 0.20f), new Vector2(0.98f, 0.91f)); // 오른쪽 배치
            Image box = CreateImage(parent, "DialogueBox", new Color(0.05f, 0.04f, 0.04f, 0.92f)); // 대사 상자
            SetRect(box.rectTransform, new Vector2(0.52f, 0.02f), new Vector2(0.99f, 0.24f)); // 하단 배치
            AddOutline(box.gameObject, new Color(1f, 0.55f, 0.22f, 0.75f)); // 불씨 테두리
            Image plate = CreateImage(parent, "NamePlate", EmberColor); // 이름표
            SetRect(plate.rectTransform, new Vector2(0.53f, 0.215f), new Vector2(0.72f, 0.265f)); // 상자 위 왼쪽
            Text plateText = CreateText(plate.transform, "Name", NpcLineCatalog.FormatSpeaker(npc), 20, new Color(0.12f, 0.05f, 0.02f, 1f)).BestFit(12); // 이름·소속
            Stretch(plateText.rectTransform, 4f); // 이름표 확장
            dialogueText = CreateText(box.transform, "Line", string.Empty, 21, Color.white, FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 대사
            SetRect(dialogueText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.80f)); // 대사 배치
        }

        private void BuildWorkbench(Transform parent) // 왼쪽 강화 작업대
        {
            Image panel = CreateImage(parent, "Workbench", PanelColor); // 작업대 패널
            SetRect(panel.rectTransform, new Vector2(0.01f, 0.02f), new Vector2(0.505f, 0.90f)); // 왼쪽 배치
            AddOutline(panel.gameObject, new Color(1f, 0.55f, 0.22f, 0.35f)); // 불씨 테두리
            enhanceTab = CreateButton(panel.transform, "EnhanceTab", "강화  +0~+5", TabOffColor, Color.white); // 강화 탭
            SetRect(enhanceTab.GetComponent<RectTransform>(), new Vector2(0.03f, 0.915f), new Vector2(0.30f, 0.98f)); // 탭 배치
            enhanceTab.onClick.AddListener(() => SetMode(false)); // 강화 모드
            transcendTab = CreateButton(panel.transform, "TranscendTab", "초월  ★1~★3", TabOffColor, Color.white); // 초월 탭
            SetRect(transcendTab.GetComponent<RectTransform>(), new Vector2(0.31f, 0.915f), new Vector2(0.58f, 0.98f)); // 탭 배치
            transcendTab.onClick.AddListener(() => SetMode(true)); // 초월 모드
            Button pickButton = CreateButton(panel.transform, "PickButton", "장비 선택", new Color(0.36f, 0.30f, 0.26f, 1f), Color.white); // 장비 선택 목록 열기
            SetRect(pickButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.915f), new Vector2(0.97f, 0.98f)); // 오른쪽 위
            pickButton.onClick.AddListener(() => ShowList(true)); // 목록 열기

            ownerFrame = CreateImage(panel.transform, "OwnerFrame", new Color(0.12f, 0.10f, 0.10f, 1f)); // 장착자 초상 틀
            SetRect(ownerFrame.rectTransform, new Vector2(0.04f, 0.60f), new Vector2(0.25f, 0.89f)); // 왼쪽 위
            ownerFrame.gameObject.AddComponent<RectMask2D>(); // 상반신만 보이게 자르기
            ownerPortrait = CreateImage(ownerFrame.transform, "Portrait", Color.white); // 장착자 초상
            ownerPortrait.preserveAspect = true; // 비율 유지
            ownerPortrait.raycastTarget = false; // 입력 통과
            SetRect(ownerPortrait.rectTransform, new Vector2(-0.35f, -1.05f), new Vector2(1.35f, 1.02f)); // 크게 놓고 위쪽만 노출
            ownerText = CreateText(panel.transform, "OwnerText", string.Empty, 16, new Color(0.85f, 0.82f, 0.78f, 1f)).BestFit(10); // 장착자 이름
            SetRect(ownerText.rectTransform, new Vector2(0.03f, 0.555f), new Vector2(0.26f, 0.60f)); // 초상 아래

            equipmentIcon = ItemIconView.Create(panel.transform, "EquipmentSlot"); // 장비 칸
            PlaceSquare(equipmentIcon.rectTransform, new Vector2(0.43f, 0.76f), 118f); // 가운데 위
            Button equipmentHit = CreateButton(equipmentIcon.transform, "Hit", string.Empty, new Color(1f, 1f, 1f, 0f), Color.white); // 칸 클릭 → 목록
            Stretch(equipmentHit.GetComponent<RectTransform>()); // 칸 전체
            equipmentHit.onClick.AddListener(() => ShowList(true)); // 목록 열기
            flash = CreateImage(equipmentIcon.transform, "Flash", new Color(1f, 1f, 1f, 0f)); // 결과 번쩍임
            flash.raycastTarget = false; // 입력 통과
            Stretch(flash.rectTransform, -6f); // 칸보다 조금 크게
            equipmentName = CreateText(panel.transform, "EquipmentName", string.Empty, 19, Color.white).BestFit(11); // 장비 이름
            SetRect(equipmentName.rectTransform, new Vector2(0.28f, 0.555f), new Vector2(0.58f, 0.62f)); // 칸 아래

            Text plus = CreateText(panel.transform, "Plus", "+", 40, new Color(1f, 1f, 1f, 0.5f)); // 칸 사이 기호
            SetRect(plus.rectTransform, new Vector2(0.57f, 0.70f), new Vector2(0.63f, 0.82f)); // 가운데
            materialIcon = ItemIconView.Create(panel.transform, "MaterialSlot"); // 주문서·재료 칸
            PlaceSquare(materialIcon.rectTransform, new Vector2(0.77f, 0.76f), 100f); // 오른쪽 위
            materialText = CreateText(panel.transform, "MaterialText", string.Empty, 17, new Color(0.90f, 0.86f, 0.80f, 1f)).BestFit(10); // 재료 수량
            SetRect(materialText.rectTransform, new Vector2(0.62f, 0.62f), new Vector2(0.96f, 0.675f)); // 칸 아래
            string[] grades = { "C", "B", "A" }; // 주문서 등급

            for (int index = 0; index < scrollButtons.Length; index++) // 등급 버튼 생성
            {
                ScrollGrade grade = (ScrollGrade)index; // 등급 값
                scrollButtons[index] = CreateButton(panel.transform, "Scroll_" + grades[index], grades[index], TabOffColor, Color.white); // 등급 버튼
                SetRect(scrollButtons[index].GetComponent<RectTransform>(), new Vector2(0.63f + (index * 0.112f), 0.56f), new Vector2(0.73f + (index * 0.112f), 0.615f)); // 칸 아래 한 줄
                scrollButtons[index].onClick.AddListener(() => SelectGrade(grade)); // 등급 선택
            }

            stageText = CreateText(panel.transform, "Stage", string.Empty, 44, EmberColor).Outlined(new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f)); // +N >>> +N+1
            SetRect(stageText.rectTransform, new Vector2(0.03f, 0.445f), new Vector2(0.97f, 0.545f)); // 가운데 줄
            Image effectBox = CreateImage(panel.transform, "EffectBox", new Color(1f, 1f, 1f, 0.05f)); // 강화 내용 상자
            SetRect(effectBox.rectTransform, new Vector2(0.04f, 0.215f), new Vector2(0.96f, 0.435f)); // 가운데 아래
            Text effectTitle = CreateText(effectBox.transform, "Title", "강화 내용", 18, EmberColor, FontStyle.Bold, TextAnchor.UpperLeft); // 제목
            SetRect(effectTitle.rectTransform, new Vector2(0.03f, 0.78f), new Vector2(0.97f, 0.98f)); // 상자 위
            effectText = CreateText(effectBox.transform, "Lines", string.Empty, 18, Color.white, FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 능력치 변화
            SetRect(effectText.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.78f)); // 상자 안
            chanceText = CreateText(panel.transform, "Chance", string.Empty, 26, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft).BestFit(14); // 성공 확률
            SetRect(chanceText.rectTransform, new Vector2(0.04f, 0.135f), new Vector2(0.55f, 0.205f)); // 확률 배치
            costText = CreateText(panel.transform, "Cost", string.Empty, 18, GoldColor, FontStyle.Bold, TextAnchor.MiddleRight).BestFit(11); // 비용
            SetRect(costText.rectTransform, new Vector2(0.45f, 0.135f), new Vector2(0.96f, 0.205f)); // 비용 배치
            actionButton = CreateButton(panel.transform, "Action", "강화하기", new Color(0.78f, 0.36f, 0.12f, 1f), Color.white); // 실행 버튼
            SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.20f, 0.025f), new Vector2(0.80f, 0.12f)); // 하단 가운데
            actionLabel = actionButton.GetComponentInChildren<Text>(); // 버튼 글자
            actionLabel.fontSize = 28; // 큰 글자
            actionButton.onClick.AddListener(RunAction); // 강화·초월 실행
        }

        private void BuildEquipmentList(Transform parent) // 오른쪽 장비 선택 목록
        {
            Image panel = CreateImage(parent, "EquipmentList", new Color(0.05f, 0.04f, 0.04f, 0.94f)); // 목록 패널
            SetRect(panel.rectTransform, new Vector2(0.52f, 0.275f), new Vector2(0.99f, 0.90f)); // 대사 상자 위
            AddOutline(panel.gameObject, new Color(1f, 0.55f, 0.22f, 0.5f)); // 불씨 테두리
            listPanel = panel.gameObject; // 패널 저장
            Text title = CreateText(panel.transform, "Title", "강화할 장비 선택", 22, EmberColor, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            SetRect(title.rectTransform, new Vector2(0.03f, 0.90f), new Vector2(0.70f, 0.985f)); // 위 왼쪽
            Button close = CreateButton(panel.transform, "Close", "닫기", TabOffColor, Color.white); // 닫기
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.84f, 0.905f), new Vector2(0.98f, 0.98f)); // 위 오른쪽
            close.onClick.AddListener(() => ShowList(false)); // 목록 닫기
            GameObject grid = new GameObject("Grid", typeof(RectTransform)); // 카드 격자
            grid.transform.SetParent(panel.transform, false); // 패널 하위
            RectTransform gridRect = grid.GetComponent<RectTransform>(); // 격자 영역
            SetRect(gridRect, new Vector2(0.02f, 0.11f), new Vector2(0.98f, 0.89f)); // 격자 배치

            for (int index = 0; index < ListPerPage; index++) // 카드 생성
            {
                equipmentCards.Add(BuildEquipmentCard(gridRect, index)); // 카드 추가
            }

            listPrev = CreateButton(panel.transform, "Prev", "◀", TabOffColor, Color.white); // 이전 페이지
            SetRect(listPrev.GetComponent<RectTransform>(), new Vector2(0.02f, 0.02f), new Vector2(0.10f, 0.095f)); // 왼쪽 아래
            listPrev.onClick.AddListener(() => ChangeListPage(-1)); // 이전
            listPageText = CreateText(panel.transform, "Page", "1 / 1", 18, Color.white); // 페이지
            SetRect(listPageText.rectTransform, new Vector2(0.10f, 0.02f), new Vector2(0.22f, 0.095f)); // 가운데
            listNext = CreateButton(panel.transform, "Next", "▶", TabOffColor, Color.white); // 다음 페이지
            SetRect(listNext.GetComponent<RectTransform>(), new Vector2(0.22f, 0.02f), new Vector2(0.30f, 0.095f)); // 오른쪽
            listNext.onClick.AddListener(() => ChangeListPage(1)); // 다음
        }

        private EquipmentCardView BuildEquipmentCard(RectTransform grid, int index) // 장비 카드 한 칸
        {
            int column = index % ListColumns; // 열
            int row = index / ListColumns; // 행
            float width = 1f / ListColumns; // 칸 너비
            float height = 1f / ListRows; // 칸 높이
            EquipmentCardView card = new EquipmentCardView(); // 카드 참조
            Button body = CreateButton(grid, "Card_" + index, string.Empty, new Color(0.15f, 0.12f, 0.12f, 1f), Color.white); // 카드 본체
            SetRect(body.GetComponent<RectTransform>(), new Vector2((column * width) + 0.008f, 1f - ((row + 1) * height) + 0.012f), new Vector2(((column + 1) * width) - 0.008f, 1f - (row * height) - 0.012f)); // 격자 배치
            body.onClick.AddListener(() => SelectEquipment(card.InstanceId)); // 장비 선택
            card.Root = body.gameObject; // 루트 저장
            card.Icon = ItemIconView.Create(body.transform, "Icon"); // 아이콘
            PlaceSquare(card.Icon.rectTransform, new Vector2(0.5f, 0.64f), 64f); // 위 가운데
            card.Name = CreateText(body.transform, "Name", string.Empty, 15, Color.white).BestFit(9); // 이름
            SetRect(card.Name.rectTransform, new Vector2(0.03f, 0.17f), new Vector2(0.97f, 0.34f)); // 이름 배치
            card.Owner = CreateText(body.transform, "Owner", string.Empty, 13, new Color(0.80f, 0.76f, 0.70f, 1f), FontStyle.Normal).BestFit(8); // 장착자
            SetRect(card.Owner.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.17f)); // 아래
            return card; // 카드 반환
        }

        private void ShowList(bool visible) // 장비 선택 목록 열기·닫기 (닫으면 대장장이가 보임)
        {
            listPanel.SetActive(visible); // 표시 전환
            if (visible) Say(NpcLineKind.SelectEquipment, null); // 선택 안내 대사
            RefreshView(); // 갱신
        }

        private void ChangeListPage(int delta) // 목록 페이지 이동
        {
            listPage += delta; // 페이지 변경
            RefreshView(); // 갱신
        }

        private void SetMode(bool transcend) // 강화·초월 모드 전환
        {
            transcendMode = transcend; // 모드 저장
            enhanceTab.GetComponent<Image>().color = transcend ? TabOffColor : EmberColor; // 강화 탭 강조
            transcendTab.GetComponent<Image>().color = transcend ? EmberColor : TabOffColor; // 초월 탭 강조
            RefreshView(); // 갱신
        }

        private void SelectEquipment(string instanceId) // 장비 선택
        {
            if (string.IsNullOrEmpty(instanceId)) return; // 빈 칸 무시
            selectedInstanceId = instanceId; // 선택 저장
            AutoPickGrade(); // 보유 주문서 등급 자동 선택
            ShowList(false); // 목록 닫기
            EquipmentInstanceSaveData instance = GetSave() == null ? null : GetSave().FindEquipmentInstance(instanceId); // 선택 장비
            if (instance != null && instance.EnhanceLevel >= EquipmentUpgradeCatalog.MaxEnhanceLevel && instance.TranscendStage < EquipmentUpgradeCatalog.MaxTranscendStage) SetMode(true); // +5면 초월 모드로
        }

        private void SelectGrade(ScrollGrade grade) // 주문서 등급 선택
        {
            selectedGrade = grade; // 등급 저장
            RefreshView(); // 갱신
        }

        private void AutoPickGrade() // 보유한 가장 낮은 등급 주문서 선택 (없으면 C)
        {
            EquipmentData equipment = GetSelectedEquipment(out _); // 선택 장비 원본
            selectedGrade = ScrollGrade.C; // 기본 C
            if (equipment == null) return; // 장비 없음

            for (int index = 0; index < scrollButtons.Length; index++) // C→B→A 순회
            {
                if (ItemInventoryService.GetCount(GetSave(), EquipmentUpgradeCatalog.GetScrollItemId(equipment.Slot, (ScrollGrade)index)) > 0) // 보유 확인
                {
                    selectedGrade = (ScrollGrade)index; // 등급 선택
                    return; // 종료
                }
            }
        }

        private EquipmentData GetSelectedEquipment(out EquipmentInstanceSaveData instance) // 선택 장비 원본·인스턴스
        {
            SaveData saveData = GetSave(); // 현재 저장
            instance = saveData == null || string.IsNullOrEmpty(selectedInstanceId) ? null : saveData.FindEquipmentInstance(selectedInstanceId); // 인스턴스
            return instance == null || GetData() == null ? null : GetData().GetEquipment(instance.EquipmentId); // 원본
        }

        private void RefreshView() // 화면 갱신
        {
            SaveData saveData = GetSave(); // 현재 저장
            DataManager dataManager = GetData(); // 데이터 관리자
            goldText.text = $"● {GoldCurrencyService.GetGold(saveData):N0} G"; // 골드
            RefreshList(saveData, dataManager); // 장비 목록
            EquipmentData equipment = GetSelectedEquipment(out EquipmentInstanceSaveData instance); // 선택 장비
            RefreshOwner(saveData, dataManager, instance); // 장착자 초상
            ItemIconView.Apply(equipmentIcon, equipment == null ? null : dataManager.GetItem(equipment.Id), dataManager); // 장비 칸
            equipmentName.text = equipment == null ? "장비를 선택하세요" : EquipmentUpgradeCatalog.FormatName(equipment.DisplayName, instance); // 장비 이름

            for (int index = 0; index < scrollButtons.Length; index++) // 등급 버튼
            {
                scrollButtons[index].gameObject.SetActive(!transcendMode); // 초월 모드에서는 숨김
                scrollButtons[index].GetComponent<Image>().color = index == (int)selectedGrade ? EmberColor : TabOffColor; // 선택 강조
            }

            if (equipment == null) // 장비 미선택
            {
                ItemIconView.Apply(materialIcon, null, dataManager); // 빈 재료 칸
                materialText.text = transcendMode ? "같은 장비" : "강화 주문서"; // 안내
                stageText.text = "—"; // 단계 없음
                effectText.text = "오른쪽 목록 또는 장비 칸을 눌러 강화할 장비를 고르세요."; // 안내
                chanceText.text = string.Empty; // 확률 없음
                costText.text = string.Empty; // 비용 없음
                SetAction(false, transcendMode ? "초월하기" : "강화하기"); // 비활성
                return; // 종료
            }

            if (transcendMode) RefreshTranscend(saveData, dataManager, equipment, instance); // 초월 화면
            else RefreshEnhance(saveData, dataManager, equipment, instance); // 강화 화면
        }

        private void RefreshEnhance(SaveData saveData, DataManager dataManager, EquipmentData equipment, EquipmentInstanceSaveData instance) // 강화 화면 갱신
        {
            string scrollId = EquipmentUpgradeCatalog.GetScrollItemId(equipment.Slot, selectedGrade); // 선택 주문서
            int scrollCount = ItemInventoryService.GetCount(saveData, scrollId); // 보유 주문서
            ItemIconView.Apply(materialIcon, dataManager.GetItem(scrollId), dataManager); // 주문서 칸
            materialText.text = $"{(EquipmentUpgradeCatalog.IsWeaponScrollSlot(equipment.Slot) ? "무기" : "방어구")} 주문서 {selectedGrade}  ×{scrollCount}"; // 주문서 수량
            int level = instance.EnhanceLevel; // 현재 강화

            if (level >= EquipmentUpgradeCatalog.MaxEnhanceLevel) // 최대 강화
            {
                stageText.text = $"+{level}  (MAX)"; // 최대 표시
                effectText.text = instance.TranscendStage < EquipmentUpgradeCatalog.MaxTranscendStage ? "최대 강화에 도달했습니다. 초월 탭에서 ★를 올릴 수 있어요." : "최대 강화·최대 초월 장비입니다."; // 안내
                chanceText.text = string.Empty; // 확률 없음
                costText.text = string.Empty; // 비용 없음
                SetAction(false, "강화하기"); // 비활성
                return; // 종료
            }

            float current = EquipmentUpgradeCatalog.GetStatMultiplier(level, instance.TranscendStage); // 현재 배율
            float next = EquipmentUpgradeCatalog.GetStatMultiplier(level + 1, instance.TranscendStage); // 다음 배율
            stageText.text = $"+{level}   >>>   +{level + 1}"; // 단계 변화
            effectText.text = BuildStatChange(equipment, current, next); // 능력치 변화
            float chance = EquipmentUpgradeCatalog.GetSuccessChance(level, selectedGrade, instance.FailStreak); // 성공 확률
            string streak = instance.FailStreak > 0 ? $"  <size=16>(실패 보정 +{instance.FailStreak * EquipmentUpgradeCatalog.FailStreakBonus * 100f:0}%)</size>" : string.Empty; // 실패 보정
            chanceText.text = $"성공 확률  <color=#FFB347>{chance * 100f:0}%</color>{streak}"; // 확률 표시
            int gold = EquipmentUpgradeCatalog.GetEnhanceGold(level); // 비용
            costText.text = $"● {gold:N0} G  ·  주문서 1장\n<size=14>실패 시 주문서만 소모</size>"; // 비용 표시
            SetAction(scrollCount > 0 && GoldCurrencyService.GetGold(saveData) >= gold, "강화하기"); // 재료 충족 시 활성
        }

        private void RefreshTranscend(SaveData saveData, DataManager dataManager, EquipmentData equipment, EquipmentInstanceSaveData instance) // 초월 화면 갱신
        {
            bool unique = UniqueEquipmentCatalog.IsUnique(instance.EquipmentId); // 전용 장비 여부 (Day70 — 같은 장비 대신 결속 단계로 초월)
            EquipmentInstanceSaveData material = unique ? instance : EquipmentUpgradeService.FindTranscendMaterial(saveData, instance); // 재료 장비 (전용 장비는 재료가 필요 없음)
            int candidates = unique ? 0 : CountMaterials(saveData, instance); // 재료 후보 수
            ItemIconView.Apply(materialIcon, candidates > 0 ? dataManager.GetItem(equipment.Id) : null, dataManager); // 재료 칸
            int bondLevel = unique ? BondService.GetLevel(saveData, UniqueEquipmentCatalog.GetOwnerId(instance.EquipmentId)) : 0; // 주인의 결속 단계
            int requiredBond = UniqueEquipmentCatalog.GetRequiredBondLevel(instance.TranscendStage); // 필요한 결속 단계
            materialText.text = unique ? $"전용 장비 · 결속 {requiredBond}단계 (현재 {bondLevel}단계)" : $"같은 장비 (미장착)  ×{candidates}"; // 재료 표시
            int stage = instance.TranscendStage; // 현재 초월

            if (stage >= EquipmentUpgradeCatalog.MaxTranscendStage) // 최대 초월
            {
                stageText.text = $"★{stage}  (MAX)"; // 최대 표시
                effectText.text = "최고 초월(★3) 장비입니다."; // 안내
                chanceText.text = string.Empty; // 확률 없음
                costText.text = string.Empty; // 비용 없음
                SetAction(false, "초월하기"); // 비활성
                return; // 종료
            }

            float current = EquipmentUpgradeCatalog.GetStatMultiplier(instance.EnhanceLevel, stage); // 현재 배율
            float next = EquipmentUpgradeCatalog.GetStatMultiplier(0, stage + 1); // 초월 후 배율 (+0 초기화)
            stageText.text = $"★{stage}   >>>   ★{stage + 1}"; // 단계 변화
            string warning = instance.EnhanceLevel < EquipmentUpgradeCatalog.MaxEnhanceLevel ? $"\n<color=#FF8A7A>+5까지 강화해야 초월할 수 있습니다. (현재 +{instance.EnhanceLevel})</color>" : "\n강화 단계는 +0으로 돌아가지만, 기본 능력치가 올라 다시 강화하면 더 강해집니다."; // 조건 안내
            effectText.text = $"기본 능력치 ×{EquipmentUpgradeCatalog.GetTranscendMultiplier(stage):0.0} → ×{EquipmentUpgradeCatalog.GetTranscendMultiplier(stage + 1):0.0}\n{BuildStatChange(equipment, current, next)}{warning}"; // 능력치 변화
            chanceText.text = "성공 확률  <color=#FFB347>100%</color>"; // 초월은 확정
            int gold = EquipmentUpgradeCatalog.GetTranscendGold(stage); // 비용
            costText.text = unique ? $"● {gold:N0} G  ·  결속 {requiredBond}단계" : $"● {gold:N0} G  ·  같은 장비 1개"; // 비용 표시
            bool materialReady = unique ? bondLevel >= requiredBond : material != null; // 재료 충족 여부
            SetAction(instance.EnhanceLevel >= EquipmentUpgradeCatalog.MaxEnhanceLevel && materialReady && GoldCurrencyService.GetGold(saveData) >= gold, "초월하기"); // 조건 충족 시 활성
        }

        private void RefreshOwner(SaveData saveData, DataManager dataManager, EquipmentInstanceSaveData instance) // 장착자 초상 갱신
        {
            CharacterSaveData owner = instance == null ? null : CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 장착 캐릭터
            ownerPortrait.enabled = owner != null; // 장착자 있을 때만 초상

            if (owner == null) // 미장착
            {
                ownerText.text = instance == null ? string.Empty : "보관 중"; // 상태
                return; // 종료
            }

            ownerPortrait.sprite = DialogueArtFactory.GetStanding(owner.CharacterId, null, out bool placeholder); // 초상
            ownerPortrait.color = placeholder ? DialogueArtFactory.GetCharacterTint(owner.CharacterId) : Color.white; // 실루엣 색
            CharacterData data = dataManager == null ? null : dataManager.GetCharacter(owner.CharacterId); // 캐릭터 원본
            ownerText.text = $"{(data == null ? owner.CharacterId : data.DisplayName)} 장착 중"; // 장착자 이름
        }

        private void RefreshList(SaveData saveData, DataManager dataManager) // 장비 선택 목록 갱신 (장착 중 먼저, 강화 높은 순)
        {
            List<EquipmentInstanceSaveData> list = new List<EquipmentInstanceSaveData>(); // 표시 목록
            if (saveData != null) list.AddRange(saveData.EquipmentInventory); // 보유 장비
            list.RemoveAll(item => item == null); // 빈 항목 제거
            list.Sort((a, b) => SortKey(saveData, b).CompareTo(SortKey(saveData, a))); // 정렬
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(list.Count / (float)ListPerPage)); // 페이지 수
            listPage = Mathf.Clamp(listPage, 0, pageCount - 1); // 페이지 보정
            listPageText.text = $"{listPage + 1} / {pageCount}"; // 페이지 표시
            listPrev.interactable = listPage > 0; // 이전 가능
            listNext.interactable = listPage < pageCount - 1; // 다음 가능

            for (int index = 0; index < equipmentCards.Count; index++) // 카드 순회
            {
                EquipmentCardView card = equipmentCards[index]; // 카드
                int listIndex = (listPage * ListPerPage) + index; // 목록 번호
                EquipmentInstanceSaveData instance = listIndex < list.Count ? list[listIndex] : null; // 장비
                card.Root.SetActive(instance != null); // 빈 칸 숨김
                if (instance == null) continue; // 다음 칸
                EquipmentData equipment = dataManager == null ? null : dataManager.GetEquipment(instance.EquipmentId); // 원본
                card.InstanceId = instance.InstanceId; // 장비 저장
                ItemIconView.Apply(card.Icon, dataManager == null ? null : dataManager.GetItem(instance.EquipmentId), dataManager); // 아이콘
                card.Name.text = equipment == null ? instance.EquipmentId : EquipmentUpgradeCatalog.FormatName(equipment.DisplayName, instance); // 이름
                CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 장착자
                CharacterData ownerData = owner == null || dataManager == null ? null : dataManager.GetCharacter(owner.CharacterId); // 장착자 원본
                card.Owner.text = owner == null ? "보관 중" : $"{(ownerData == null ? owner.CharacterId : ownerData.DisplayName)} 장착"; // 장착자
                card.Root.GetComponent<Image>().color = instance.InstanceId == selectedInstanceId ? new Color(0.45f, 0.22f, 0.10f, 1f) : new Color(0.15f, 0.12f, 0.12f, 1f); // 선택 강조
            }
        }

        private static int SortKey(SaveData saveData, EquipmentInstanceSaveData instance) // 정렬 점수 (장착 1000 + 초월 100 + 강화 10)
        {
            int equipped = CharacterEquipmentService.IsEquipped(saveData, instance.InstanceId) ? 1000 : 0; // 장착 우선
            return equipped + (instance.TranscendStage * 100) + (instance.EnhanceLevel * 10); // 점수
        }

        private static int CountMaterials(SaveData saveData, EquipmentInstanceSaveData target) // 초월 재료 후보 수
        {
            int count = 0; // 개수

            foreach (EquipmentInstanceSaveData other in saveData.EquipmentInventory) // 보유 장비 순회
            {
                if (other != null && other != target && other.EquipmentId == target.EquipmentId && !CharacterEquipmentService.IsEquipped(saveData, other.InstanceId)) count++; // 같은 장비·미장착
            }

            return count; // 개수 반환
        }

        private static string BuildStatChange(EquipmentData equipment, float current, float next) // 능력치 변화 문구 (공격력 +10 → +11)
        {
            if (equipment.StatOptions == null || equipment.StatOptions.Count == 0) return "추가 능력치 없음"; // 옵션 없음
            StringBuilder builder = new StringBuilder(); // 문구 조립

            for (int index = 0; index < equipment.StatOptions.Count; index++) // 옵션 순회
            {
                EquipmentStatOption option = equipment.StatOptions[index]; // 옵션
                if (index > 0) builder.Append('\n'); // 줄바꿈
                builder.Append(CharacterEquipmentScreenController.GetStatDisplayName(option.StatType)); // 능력치 이름
                builder.Append("  ").Append(CharacterEquipmentScreenController.FormatStatValue(option.StatType, option.Value * current)); // 현재 값
                builder.Append("  →  <color=#FFB347>").Append(CharacterEquipmentScreenController.FormatStatValue(option.StatType, option.Value * next)).Append("</color>"); // 다음 값
            }

            return builder.ToString(); // 문구 반환
        }

        private void SetAction(bool interactable, string label) // 실행 버튼 상태
        {
            actionButton.interactable = interactable; // 활성 여부
            actionLabel.text = label; // 글자
        }

        private void RunAction() // 강화 또는 초월 실행
        {
            SaveData saveData = GetSave(); // 현재 저장
            DataManager dataManager = GetData(); // 데이터 관리자
            EquipmentData equipment = GetSelectedEquipment(out EquipmentInstanceSaveData instance); // 선택 장비
            if (equipment == null) return; // 장비 없음

            if (transcendMode) // 초월
            {
                bool done = EquipmentUpgradeService.TryTranscend(saveData, dataManager, instance.InstanceId, out string transcendMessage); // 초월 실행
                Say(done ? NpcLineKind.TranscendSuccess : NpcLineKind.NeedMaterial, transcendMessage); // 결과 대사
                if (done) Flash(new Color(0.75f, 0.55f, 1f, 1f)); // 보랏빛 번쩍임
                FinishAction(done); // 저장·갱신
                return; // 종료
            }

            EnhanceOutcome outcome = EquipmentUpgradeService.TryEnhance(saveData, dataManager, instance.InstanceId, EquipmentUpgradeCatalog.GetScrollItemId(equipment.Slot, selectedGrade), out string message); // 강화 실행
            Say(outcome == EnhanceOutcome.Success ? NpcLineKind.EnhanceSuccess : outcome == EnhanceOutcome.Failed ? NpcLineKind.EnhanceFail : NpcLineKind.NeedMaterial, message); // 결과 대사
            if (outcome != EnhanceOutcome.Rejected) Flash(outcome == EnhanceOutcome.Success ? new Color(1f, 0.85f, 0.35f, 1f) : new Color(1f, 0.25f, 0.2f, 1f)); // 성공 금색 · 실패 붉은색
            FinishAction(outcome != EnhanceOutcome.Rejected); // 저장·갱신
        }

        private void FinishAction(bool changed) // 결과 저장·갱신
        {
            if (changed && GameManager.Instance != null && GameManager.Instance.Save != null) GameManager.Instance.Save.SaveCurrent(); // 상태 변경 시 즉시 저장
            if (changed) AutoPickGrade(); // 주문서가 떨어졌으면 다른 등급으로
            RefreshView(); // 갱신
        }

        private void Flash(Color color) // 장비 칸 번쩍임
        {
            StopAllCoroutines(); // 이전 연출 중단
            StartCoroutine(FlashRoutine(color)); // 연출 시작
        }

        private IEnumerator FlashRoutine(Color color) // 번쩍임 연출 (0.5초 동안 사라짐)
        {
            for (float time = 0f; time < 0.5f; time += Time.unscaledDeltaTime) // 0.5초
            {
                flash.color = new Color(color.r, color.g, color.b, 0.85f * (1f - (time / 0.5f))); // 점점 투명
                yield return null; // 다음 프레임
            }

            flash.color = new Color(color.r, color.g, color.b, 0f); // 완전 투명
        }

        private void Say(NpcLineKind kind, string detail) // 대장장이 대사 + 시스템 안내
        {
            lineSeed++; // 변주
            string line = NpcLineCatalog.GetLine(NpcLineCatalog.Blacksmith, kind, lineSeed); // 대사
            dialogueText.text = string.IsNullOrEmpty(detail) ? line : $"{line}\n<color=#B9B2A8><size=17>{detail}</size></color>"; // 대사 + 안내
        }

        private void LoadScene(string sceneName) // 씬 이동
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                Say(NpcLineKind.NeedMaterial, "SceneLoader를 찾을 수 없습니다."); // 안내
                return; // 중단
            }

            GameManager.Instance.Scenes.LoadScene(sceneName); // 씬 로드
        }

        private static void PlaceSquare(RectTransform rect, Vector2 anchor, float size) // 기준점 중심 정사각 배치
        {
            rect.anchorMin = anchor; // 기준점
            rect.anchorMax = anchor; // 기준점
            rect.anchoredPosition = Vector2.zero; // 중심
            rect.sizeDelta = new Vector2(size, size); // 크기
        }

        private static SaveData GetSave() => GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장 조회

        private static DataManager GetData() => GameManager.Instance == null ? null : GameManager.Instance.Data; // 데이터 관리자 조회

        private static Image CreateImage(Transform parent, string objectName, Color color) => RuntimeUiKit.CreateImage(parent, objectName, color); // 기본 Image 생성 (RuntimeUiKit 위임)

        private static Text CreateText(Transform parent, string objectName, string value, int fontSize, Color color, FontStyle fontStyle = FontStyle.Bold, TextAnchor alignment = TextAnchor.MiddleCenter) // 공통 UI 텍스트 생성 (RuntimeUiKit 위임)
        {
            return RuntimeUiKit.CreateText(parent, objectName, value, fontSize, color, fontStyle, alignment); // 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string objectName, string labelText, Color color, Color labelColor) // 공통 UI 버튼 생성 (RuntimeUiKit 위임)
        {
            Button button = RuntimeUiKit.CreateButton(parent, objectName, color); // 버튼 생성
            Text label = CreateText(button.transform, "Label", labelText, 20, labelColor).BestFit(11); // 자동 크기 라벨
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
