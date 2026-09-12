using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using System.Text; // 로그 문구 조립 기능
using ProjectH.Battle.Rhythm; // 원형 스프라이트 기능
using ProjectH.Core; // 저장 관리자 기능 (Day63 본 이야기 기록)
using ProjectH.Diary; // 일기장 기록 기능 (Day63)
using ProjectH.Dialogue; // 대화 진행 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 대화 화면 방지
    public sealed class DialogueOverlayView : MonoBehaviour // 미연시형 대화 화면 (Day58 신규 · Day62 목업 개편 — 2인 좌우 스탠딩 · 이름/소속 · 우상단 아이콘 메뉴 · 밝은 회색 창)
    {
        private const int SortingOrder = 600; // 캐릭터 화면(100) 위, 로딩창(1000) 아래
        private static readonly Color WindowColor = new Color(0.94f, 0.94f, 0.96f, 0.93f); // 대화창 밝은 회색
        private static readonly Color WindowLineColor = new Color(0.55f, 0.58f, 0.66f, 1f); // 대화창 윗선 회청색
        private static readonly Color BodyColor = new Color(0.13f, 0.14f, 0.18f, 1f); // 대사 글자 진회색
        private static readonly Color NamePlateColor = new Color(0.24f, 0.26f, 0.32f, 0.97f); // 이름표 차콜
        private static readonly Color AffiliationColor = new Color(0.78f, 0.80f, 0.86f, 1f); // 소속 글자 연회색
        private static readonly Color IconColor = new Color(0.10f, 0.11f, 0.14f, 0.62f); // 아이콘 원 배경
        private static readonly Color AutoOnColor = new Color(1f, 0.84f, 0.40f, 1f); // 자동 켜짐 금색
        private static readonly Color PanelColor = new Color(0.97f, 0.97f, 0.98f, 0.98f); // 설정·확인 창 흰 회색
        private static readonly Color ButtonColor = new Color(0.30f, 0.32f, 0.40f, 1f); // 창 버튼 차콜
        private static readonly Color SelectedColor = new Color(0.90f, 0.66f, 0.30f, 1f); // 선택 단계 주황

        private sealed class StandingView // 무대 위 캐릭터 한 명
        {
            public string CharacterId; // 캐릭터 ID
            public string Expression = string.Empty; // 현재 표정
            public Image Image; // 스탠딩 이미지
            public Text Label; // 임시 실루엣 이름·표정
            public DialogueStageSlot Slot; // 자리
        }

        private readonly List<Button> choiceButtons = new List<Button>(); // 현재 선택지 버튼
        private readonly List<StandingView> standings = new List<StandingView>(); // 무대 위 캐릭터
        private DialogueRunner runner; // 대화 진행기
        private Action<DialogueRunner> onFinished; // 종료 콜백
        private DialogueStageLayout layout; // 스탠딩 배치
        private string scriptId = string.Empty; // 대사 파일 ID (Day63 — 끝까지 보면 일기장에 기록)
        private Image backgroundImage; // 배경 (CG가 나오면 CG로 교체, Day63)
        private Sprite sceneBackground; // 대사 파일 원래 배경 (Day63)
        private bool cgActive; // CG 표시 중 (Day63 — 스탠딩 숨김)
        private GameObject uiRoot; // 숨김 대상 UI 묶음
        private RectTransform namePlate; // 이름표
        private Text nameText; // 이름
        private Text affiliationText; // 소속
        private Text bodyText; // 대사
        private Text continueMark; // 넘김 표시 ▼
        private Text autoGlyph; // 자동 아이콘 글자
        private RectTransform choiceRoot; // 선택지 영역
        private GameObject logPanel; // 로그 창
        private Text logText; // 로그 내용
        private ScrollRect logScroll; // 로그 스크롤
        private GameObject confirmPanel; // 건너뛰기 확인 창
        private bool settingsOpen; // 통합 설정 창 열림 여부 (Day72 — 대화 전용 설정 창을 대체)
        private string fullText = string.Empty; // 현재 대사 전체
        private float visibleChars; // 표시 중인 글자 수
        private bool isTyping; // 타자 효과 진행 여부
        private bool autoMode; // 자동 켜짐 여부
        private float autoTimer; // 자동 대기 시간
        private bool uiHidden; // 숨김 상태

        public static DialogueOverlayView Open(DialogueScript script, Action<DialogueRunner> finishedCallback) // 대화 화면 열기 (대화가 없으면 null)
        {
            if (script == null || script.Nodes.Count == 0) // 대화 확인
            {
                return null; // 열기 실패 반환
            }

            GameObject root = new GameObject("DialogueOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 대화 Canvas 생성
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay
            canvas.sortingOrder = SortingOrder; // 정렬 순서
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기준
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 중간
            DialogueOverlayView view = root.AddComponent<DialogueOverlayView>(); // 화면 컴포넌트 추가
            view.onFinished = finishedCallback; // 종료 콜백 저장
            view.runner = new DialogueRunner(script); // 진행기 생성
            view.layout = DialogueStageLayout.Build(script); // 좌우 배치 결정 (Day62)
            view.scriptId = script.Id; // 대사 파일 ID (Day63)
            view.Build(script); // 화면 구성
            view.ShowCurrent(); // 첫 대사 표시
            return view; // 화면 반환
        }

        private void Build(DialogueScript script) // 화면 구성
        {
            Image background = RuntimeUiKit.CreateImage(transform, "Background", Color.white); // 배경 생성
            background.sprite = DialogueArtFactory.GetBackground(script.Background); // 배경 이미지 적용
            background.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(background.rectTransform); // 전체 화면
            backgroundImage = background; // 배경 보관 (Day63 CG 교체용)
            sceneBackground = background.sprite; // 원래 배경 보관 (Day63)
            AddStanding(layout.Left, DialogueStageSlot.Left); // 왼쪽 스탠딩
            AddStanding(layout.Right, DialogueStageSlot.Right); // 오른쪽 스탠딩
            AddStanding(layout.Center, DialogueStageSlot.Center); // 가운데 스탠딩 (1인)

            Button clickCatcher = RuntimeUiKit.CreateButton(transform, "ClickCatcher", new Color(0f, 0f, 0f, 0f), false); // 전체 화면 클릭 영역 (투명)
            RuntimeUiKit.Stretch((RectTransform)clickCatcher.transform); // 전체 화면
            clickCatcher.onClick.AddListener(OnScreenClick); // 클릭 → 넘기기

            uiRoot = new GameObject("DialogueUi", typeof(RectTransform)); // 숨김 대상 묶음
            uiRoot.transform.SetParent(transform, false); // 부모 연결
            RuntimeUiKit.Stretch((RectTransform)uiRoot.transform); // 전체 화면
            BuildLocationTag(script); // 장소 표시
            BuildMessageWindow(); // 대화창·이름표
            BuildIconMenu(); // 우상단 아이콘 메뉴 (Day62)
            GameObject choiceObject = new GameObject("Choices", typeof(RectTransform)); // 선택지 영역 생성
            choiceObject.transform.SetParent(uiRoot.transform, false); // 부모 연결
            choiceRoot = (RectTransform)choiceObject.transform; // 영역 저장
            RuntimeUiKit.SetRect(choiceRoot, new Vector2(0.26f, 0.36f), new Vector2(0.74f, 0.86f)); // 화면 가운데 배치
            BuildLogPanel(); // 로그 창
            BuildConfirmPanel(); // 건너뛰기 확인 창
        }

        private void AddStanding(string characterId, DialogueStageSlot slot) // 스탠딩 한 명 배치 (빈 ID면 생략)
        {
            if (string.IsNullOrEmpty(characterId)) return; // 빈 자리
            StandingView view = new StandingView { CharacterId = characterId, Slot = slot }; // 참조 생성
            view.Image = RuntimeUiKit.CreateImage(transform, "Standing_" + slot, Color.white); // 스탠딩 이미지
            view.Image.preserveAspect = true; // 비율 유지
            view.Image.raycastTarget = false; // 입력 통과
            Vector2 min = slot == DialogueStageSlot.Left ? new Vector2(0.03f, 0f) : slot == DialogueStageSlot.Right ? new Vector2(0.57f, 0f) : new Vector2(0.32f, 0f); // 자리별 왼쪽 아래
            Vector2 max = slot == DialogueStageSlot.Left ? new Vector2(0.43f, 0.95f) : slot == DialogueStageSlot.Right ? new Vector2(0.97f, 0.95f) : new Vector2(0.68f, 0.97f); // 자리별 오른쪽 위
            RuntimeUiKit.SetRect(view.Image.rectTransform, min, max); // 배치 (대화창이 하체를 가림)
            view.Label = RuntimeUiKit.CreateText(view.Image.transform, "PlaceholderLabel", string.Empty, 26, new Color(0.20f, 0.16f, 0.26f, 0.9f)); // 임시 실루엣 이름·표정
            view.Label.supportRichText = true; // 글자 크기 태그 사용
            RuntimeUiKit.SetRect(view.Label.rectTransform, new Vector2(0f, 0.40f), new Vector2(1f, 0.56f)); // 가슴 높이 배치
            standings.Add(view); // 목록 등록
        }

        private void BuildLocationTag(DialogueScript script) // 좌상단 장소 표시
        {
            Image tag = RuntimeUiKit.CreateImage(uiRoot.transform, "LocationTag", new Color(0.10f, 0.11f, 0.14f, 0.55f)); // 반투명 띠
            tag.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(tag.rectTransform, new Vector2(0f, 0.925f), new Vector2(0.34f, 0.98f)); // 좌상단 배치
            string title = string.IsNullOrEmpty(script.Title) ? string.Empty : $"「{script.Title}」  "; // 제목 문구
            Text text = RuntimeUiKit.CreateText(tag.transform, "Text", $"{title}{script.Location}", 19, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft).Overflow(); // 장소 문구
            RuntimeUiKit.Stretch(text.rectTransform, 14f); // 여백 확장
        }

        private void BuildMessageWindow() // 하단 밝은 회색 대화창·이름표(이름·소속)·대사·넘김 표시
        {
            Image window = RuntimeUiKit.CreateImage(uiRoot.transform, "MessageWindow", WindowColor); // 대화창 생성
            window.raycastTarget = false; // 클릭은 전체 화면 영역이 받음
            RuntimeUiKit.SetRect(window.rectTransform, new Vector2(0.01f, 0.03f), new Vector2(0.99f, 0.29f)); // 하단 배치
            Image topLine = RuntimeUiKit.CreateImage(window.transform, "TopLine", WindowLineColor); // 윗선 생성
            topLine.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(topLine.rectTransform, new Vector2(0f, 0.985f), new Vector2(1f, 1f)); // 윗선 배치

            Image plate = RuntimeUiKit.CreateImage(uiRoot.transform, "NamePlate", NamePlateColor); // 이름표 생성
            plate.raycastTarget = false; // 입력 통과
            namePlate = plate.rectTransform; // 이름표 저장 (화자 쪽으로 이동)
            nameText = RuntimeUiKit.CreateText(plate.transform, "Name", string.Empty, ProjectH.Core.GameSettings.ScaleFontSize(24), Color.white, FontStyle.Bold, TextAnchor.MiddleLeft).BestFit(14); // 이름
            RuntimeUiKit.SetRect(nameText.rectTransform, new Vector2(0.07f, 0f), new Vector2(0.52f, 1f)); // 이름 왼쪽
            affiliationText = RuntimeUiKit.CreateText(plate.transform, "Affiliation", string.Empty, ProjectH.Core.GameSettings.ScaleFontSize(16), AffiliationColor, FontStyle.Normal, TextAnchor.MiddleRight).BestFit(10); // 소속
            RuntimeUiKit.SetRect(affiliationText.rectTransform, new Vector2(0.50f, 0f), new Vector2(0.94f, 1f)); // 소속 오른쪽

            bodyText = RuntimeUiKit.CreateText(window.transform, "Body", string.Empty, ProjectH.Core.GameSettings.ScaleFontSize(27), BodyColor, FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 대사 생성 (밝은 창 위 진회색)
            bodyText.lineSpacing = 1.15f; // 줄 간격
            RuntimeUiKit.SetRect(bodyText.rectTransform, new Vector2(0.035f, 0.10f), new Vector2(0.94f, 0.78f)); // 대사 배치
            continueMark = RuntimeUiKit.CreateText(window.transform, "ContinueMark", "▼", 22, WindowLineColor); // 넘김 표시 생성
            RuntimeUiKit.SetRect(continueMark.rectTransform, new Vector2(0.95f, 0.06f), new Vector2(0.985f, 0.26f)); // 오른쪽 아래 배치
        }

        private void BuildIconMenu() // 우상단 아이콘 메뉴 : 자동 · 건너뛰기 · 숨김 · 로그 · 설정 (목업 1번)
        {
            autoGlyph = CreateIconButton("Auto", "▶", "자동", 0, ToggleAuto); // 자동 넘기기
            CreateIconButton("Skip", "▶▶", "건너뛰기", 1, ConfirmSkip); // 건너뛰기 (확인 창)
            CreateIconButton("Hide", "◎", "숨김", 2, HideUi); // UI 숨김 (기획서 'UI 비활성화')
            CreateIconButton("Log", "≡", "로그", 3, OpenLog); // 대화 로그
            CreateIconButton("Settings", "⚙", "설정", 4, OpenSettings); // 통합 설정 창 (대사 속도·자동 넘김·글자 크기, Day72)
        }

        private Text CreateIconButton(string name, string glyph, string caption, int index, UnityEngine.Events.UnityAction action) // 원형 아이콘 버튼 + 아래 설명
        {
            float centerX = 0.735f + (index * 0.057f); // 오른쪽으로 나란히
            Button button = RuntimeUiKit.CreateButton(uiRoot.transform, "Icon_" + name, IconColor); // 원형 버튼
            button.GetComponent<Image>().sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // 원 모양
            RectTransform rect = (RectTransform)button.transform; // 버튼 영역
            rect.anchorMin = new Vector2(centerX, 0.945f); // 기준점
            rect.anchorMax = new Vector2(centerX, 0.945f); // 기준점
            rect.sizeDelta = new Vector2(54f, 54f); // 정원 크기
            Text text = RuntimeUiKit.CreateText(button.transform, "Glyph", glyph, 22, Color.white).BestFit(12); // 아이콘 글자
            RuntimeUiKit.Stretch(text.rectTransform, 8f); // 원 안
            Text label = RuntimeUiKit.CreateText(uiRoot.transform, "Caption_" + name, caption, 13, Color.white).Outlined(new Color(0f, 0f, 0f, 0.7f), new Vector2(1f, -1f)); // 아래 설명
            label.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(label.rectTransform, new Vector2(centerX - 0.028f, 0.878f), new Vector2(centerX + 0.028f, 0.908f)); // 원 아래
            button.onClick.AddListener(action); // 기능 연결
            return text; // 글자 반환
        }

        private void BuildLogPanel() // 로그 창 (지나간 대사 목록)
        {
            Image panel = RuntimeUiKit.CreateImage(transform, "LogPanel", new Color(0.06f, 0.07f, 0.09f, 0.92f)); // 어두운 전체 창 (뒤 클릭 차단)
            RuntimeUiKit.Stretch(panel.rectTransform); // 전체 화면
            Text title = RuntimeUiKit.CreateText(panel.transform, "Title", "LOG", 26, AffiliationColor, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            RuntimeUiKit.SetRect(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.50f, 0.97f)); // 제목 배치
            CreatePanelButton(panel.transform, "CloseLog", "닫기", new Vector2(0.84f, 0.905f), new Vector2(0.92f, 0.965f), CloseLog); // 닫기 버튼

            GameObject scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect)); // 스크롤 영역
            scrollObject.transform.SetParent(panel.transform, false); // 부모 연결
            scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f); // 옅은 바탕
            RuntimeUiKit.SetRect((RectTransform)scrollObject.transform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.88f)); // 스크롤 배치
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); // 뷰포트
            viewport.transform.SetParent(scrollObject.transform, false); // 부모 연결
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f); // 마스크용 바탕
            viewport.GetComponent<Mask>().showMaskGraphic = false; // 마스크 그래픽 숨김
            RuntimeUiKit.Stretch((RectTransform)viewport.transform, 12f); // 여백 확장
            logText = RuntimeUiKit.CreateText(viewport.transform, "Content", string.Empty, 21, Color.white, FontStyle.Normal, TextAnchor.UpperLeft); // 로그 글자
            logText.horizontalOverflow = HorizontalWrapMode.Wrap; // 가로 줄바꿈
            logText.verticalOverflow = VerticalWrapMode.Overflow; // 세로 확장
            logText.supportRichText = true; // 이름 색 표시
            RectTransform content = logText.rectTransform; // 내용 영역
            content.anchorMin = new Vector2(0f, 1f); // 위쪽 기준
            content.anchorMax = new Vector2(1f, 1f); // 가로 채움
            content.pivot = new Vector2(0.5f, 1f); // 위쪽 피벗
            content.offsetMin = Vector2.zero; // 여백 초기화
            content.offsetMax = Vector2.zero; // 여백 초기화
            logText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 내용 길이만큼 늘림
            logScroll = scrollObject.GetComponent<ScrollRect>(); // 스크롤 조회
            logScroll.viewport = (RectTransform)viewport.transform; // 뷰포트 연결
            logScroll.content = content; // 내용 연결
            logScroll.horizontal = false; // 가로 스크롤 끔
            logScroll.movementType = ScrollRect.MovementType.Clamped; // 범위 제한
            logScroll.scrollSensitivity = 30f; // 스크롤 감도
            logPanel = panel.gameObject; // 창 저장
            logPanel.SetActive(false); // 초기 숨김
        }

        private void BuildConfirmPanel() // 건너뛰기 확인 창 (기획서 '주의창-씬 넘기기')
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "SkipConfirm", new Color(0f, 0f, 0f, 0.55f)); // 뒤 화면 어둡게 (클릭 차단)
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체 화면
            Image box = RuntimeUiKit.CreateImage(dim.transform, "Box", PanelColor); // 확인 상자
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.33f, 0.38f), new Vector2(0.67f, 0.62f)); // 가운데 배치
            box.gameObject.AddComponent<Outline>().effectColor = WindowLineColor; // 회청색 외곽선
            Text message = RuntimeUiKit.CreateText(box.transform, "Message", "해당 씬을 건너뛰시겠습니까?\n<size=17>(일기장을 통해 다시 감상이 가능 · 선택지가 나오면 멈춰요)</size>", 22, BodyColor).Wrap(); // 안내 문구
            message.supportRichText = true; // 글자 크기 태그 사용
            RuntimeUiKit.SetRect(message.rectTransform, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.94f)); // 문구 배치
            CreatePanelButton(box.transform, "Yes", "네", new Vector2(0.10f, 0.10f), new Vector2(0.46f, 0.34f), RunSkip); // 건너뛰기 실행
            CreatePanelButton(box.transform, "No", "아니요", new Vector2(0.54f, 0.10f), new Vector2(0.90f, 0.34f), () => confirmPanel.SetActive(false)); // 창 닫기
            confirmPanel = dim.gameObject; // 창 저장
            confirmPanel.SetActive(false); // 초기 숨김
        }

        private static Button CreatePanelButton(Transform parent, string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action) // 창 버튼 생성
        {
            Button button = RuntimeUiKit.CreateButton(parent, "Button_" + name, ButtonColor); // 버튼 생성
            RuntimeUiKit.SetRect((RectTransform)button.transform, min, max); // 버튼 배치
            Text text = RuntimeUiKit.CreateText(button.transform, "Label", label, 19, Color.white).BestFit(11); // 버튼 글자
            RuntimeUiKit.Stretch(text.rectTransform, 4f); // 버튼 채움
            button.onClick.AddListener(action); // 기능 연결
            return button; // 버튼 반환
        }

        private void Update() // 타자 효과·자동·키보드 처리
        {
            if (runner == null) // 종료 확인
            {
                return; // 종료 후 처리 없음
            }

            float delta = Time.unscaledDeltaTime; // 일시정지와 무관한 시간

            if (isTyping) // 타자 효과 진행 확인
            {
                visibleChars += DialogueSettings.GetCharsPerSecond() * delta; // 설정한 대사 속도로 글자 증가
                int count = Mathf.Min(fullText.Length, Mathf.FloorToInt(visibleChars)); // 표시 글자 수

                if (count >= fullText.Length) // 대사 끝 확인
                {
                    CompleteTyping(); // 전체 표시
                }
                else // 진행 중 처리
                {
                    bodyText.text = fullText.Substring(0, count); // 일부 표시
                }
            }

            continueMark.color = new Color(WindowLineColor.r, WindowLineColor.g, WindowLineColor.b, 0.35f + (0.65f * Mathf.PingPong(Time.unscaledTime * 1.6f, 1f))); // ▼ 깜빡임
            bool modalOpen = logPanel.activeSelf || confirmPanel.activeSelf || settingsOpen; // 창 열림 여부 (설정은 별도 화면, Day72)

            if (autoMode && !isTyping && !modalOpen && !uiHidden && runner.Current != null && !runner.IsWaitingForChoice) // 자동 넘김 조건
            {
                autoTimer += delta; // 대기 누적

                if (autoTimer >= DialogueSettings.GetAutoDelay(fullText.Length)) // 설정한 자동 넘김 대기만큼 기다린 뒤
                {
                    Next(); // 다음 대사
                    return; // 이번 프레임 입력 처리 생략 (종료 직후 보호)
                }
            }

            Keyboard keyboard = Keyboard.current; // 키보드 조회

            if (keyboard != null && !modalOpen && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) // 스페이스·엔터 확인
            {
                OnScreenClick(); // 클릭과 같은 동작
            }
            else if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) // ESC 확인
            {
                if (logPanel.activeSelf) CloseLog(); // 로그 닫기
                confirmPanel.SetActive(false); // 확인 창 닫기 (설정 창은 자기 [닫기]로 닫는다)
            }
        }

        private void OnScreenClick() // 화면 클릭 처리
        {
            if (runner == null) // 종료 확인
            {
                return; // 무시
            }

            if (uiHidden) // 숨김 상태 확인
            {
                ShowUi(); // 다시 표시
                return; // 넘기지 않음
            }

            if (isTyping) // 타자 효과 중 확인
            {
                CompleteTyping(); // 대사 전체 표시
                return; // 넘기지 않음
            }

            if (runner.IsWaitingForChoice) // 선택지 대기 확인
            {
                return; // 선택지를 골라야 함
            }

            Next(); // 다음 대사
        }

        private void Next() // 다음 대사로 진행
        {
            runner.Advance(); // 진행기 넘김
            if (runner.IsFinished) Finish(); // 종료 처리
            else ShowCurrent(); // 새 대사 표시
        }

        private void ShowCurrent() // 현재 노드 표시
        {
            ClearChoices(); // 이전 선택지 제거
            DialogueNode node = runner.Current; // 현재 노드
            autoTimer = 0f; // 자동 대기 초기화

            if (node == null) // 노드 확인
            {
                Finish(); // 종료
                return; // 표시 중단
            }

            if (string.IsNullOrEmpty(node.Text)) // 선택지만 있는 노드 확인
            {
                CompleteTyping(); // 이전 대사를 그대로 두고 선택지 표시
                return; // 표시 종료
            }

            if (!string.IsNullOrEmpty(node.Cg)) ApplyCg(node.Cg); // 이 대사부터 CG 표시·해제 (Day63)
            StandingView speakerView = FindStanding(node.Speaker); // 무대 위 화자
            if (speakerView != null && !string.IsNullOrEmpty(node.Expression)) speakerView.Expression = node.Expression; // 표정 갱신
            RefreshStandings(node.Speaker); // 말하는 쪽 강조 (Day62)
            RefreshNamePlate(node.Speaker, speakerView); // 이름·소속·위치 (Day62)
            bodyText.fontStyle = node.Speaker == DialogueSpeakers.Narration ? FontStyle.Italic : FontStyle.Normal; // 나레이션은 기울임
            fullText = ResolveHeroName(node.Text); // 대사 저장 ({HERO}를 주인공 이름으로, Day68)
            visibleChars = 0f; // 타자 효과 시작
            bodyText.text = string.Empty; // 대사 비움
            isTyping = true; // 타자 효과 진행
            continueMark.gameObject.SetActive(false); // ▼ 숨김
        }

        private static string ResolveHeroName(string text) // 대사 속 {HERO}를 입력한 주인공 이름으로 교체 (Day68 추가)
        {
            ProjectH.SaveSystem.SaveData saveData = GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장
            return ProjectH.SaveSystem.HeroNameService.Apply(text, saveData); // 교체 결과 반환
        }

        private void CompleteTyping() // 대사 전체 표시 후 선택지·▼ 표시
        {
            isTyping = false; // 타자 효과 종료
            bodyText.text = fullText; // 전체 대사
            autoTimer = 0f; // 자동 대기 시작

            if (runner.IsWaitingForChoice) // 선택지 확인
            {
                ShowChoices(runner.Current.Choices); // 선택지 표시
                continueMark.gameObject.SetActive(false); // ▼ 숨김
            }
            else // 일반 대사 처리
            {
                continueMark.gameObject.SetActive(true); // ▼ 표시
            }
        }

        private StandingView FindStanding(string characterId) // 무대 위 캐릭터 조회
        {
            for (int index = 0; index < standings.Count; index++) // 목록 순회
            {
                if (standings[index].CharacterId == characterId) return standings[index]; // 일치
            }

            return null; // 무대에 없음
        }

        private void ApplyCg(string cgId) // CG 켜기·끄기 (Day63 추가 — CG가 있으면 화면 가득, 스탠딩 숨김)
        {
            Sprite cg = cgId == "-" ? null : RuntimeSpriteLoader.Load("Diary/CG/" + cgId); // CG 그림 ("-"은 끄기)
            cgActive = cg != null; // 표시 여부
            backgroundImage.sprite = cgActive ? cg : sceneBackground; // CG 또는 원래 배경
            backgroundImage.preserveAspect = false; // 화면 가득
        }

        private void RefreshStandings(string speaker) // 스탠딩 이미지·밝기 갱신 (말하는 사람 밝게, 듣는 사람 어둡게)
        {
            bool characterSpeaking = FindStanding(speaker) != null; // 무대 위 캐릭터가 말하는지

            for (int index = 0; index < standings.Count; index++) // 무대 순회
            {
                StandingView view = standings[index]; // 스탠딩
                view.Image.gameObject.SetActive(!cgActive); // CG 중에는 스탠딩 숨김 (Day63)
                view.Image.sprite = DialogueArtFactory.GetStanding(view.CharacterId, view.Expression, out bool placeholder); // 표정별 스탠딩
                Color tint = placeholder ? DialogueArtFactory.GetCharacterTint(view.CharacterId) : Color.white; // 임시 실루엣은 캐릭터 색
                bool speaking = view.CharacterId == speaker; // 이 캐릭터가 말하는지
                float dim = speaking ? 0f : characterSpeaking ? 0.45f : speaker == DialogueSpeakers.Narration && standings.Count == 1 ? 0f : 0.28f; // 듣는 쪽 0.45 · 주인공 대사 0.28 · 1인 나레이션 0
                view.Image.color = Color.Lerp(tint, Color.black, dim); // 밝기 적용
                view.Image.rectTransform.localScale = Vector3.one * (speaking || !characterSpeaking ? 1f : 0.96f); // 듣는 쪽 살짝 작게
                view.Label.gameObject.SetActive(placeholder); // 정식 아트가 있으면 글자 숨김
                view.Label.text = placeholder ? $"{DialogueSpeakerInfo.ResolveName(view.CharacterId)}\n<size=20>[{(string.IsNullOrEmpty(view.Expression) ? "기본" : view.Expression)}]</size>" : string.Empty; // 이름·표정 표시
            }
        }

        private void RefreshNamePlate(string speaker, StandingView speakerView) // 이름표 : 이름 · 소속 · 화자 쪽 위치
        {
            string speakerName = DialogueSpeakerInfo.ResolveName(speaker); // 이름
            namePlate.gameObject.SetActive(!string.IsNullOrEmpty(speakerName)); // 나레이션은 이름표 숨김
            nameText.text = speakerName; // 이름
            affiliationText.text = DialogueSpeakerInfo.ResolveAffiliation(speaker); // 소속
            bool right = speakerView != null && speakerView.Slot == DialogueStageSlot.Right; // 오른쪽 화자
            RuntimeUiKit.SetRect(namePlate, right ? new Vector2(0.72f, 0.29f) : new Vector2(0.025f, 0.29f), right ? new Vector2(0.975f, 0.35f) : new Vector2(0.28f, 0.35f)); // 화자 쪽 대화창 위
        }

        private void ShowChoices(IReadOnlyList<DialogueChoice> choices) // 선택지 버튼 표시
        {
            ClearChoices(); // 기존 선택지 제거
            float height = 0.17f; // 버튼 높이 비율
            float gap = 0.05f; // 버튼 간격 비율
            float total = (choices.Count * height) + ((choices.Count - 1) * gap); // 전체 높이
            float top = 0.5f + (total * 0.5f); // 세로 가운데 정렬 시작점

            for (int index = 0; index < choices.Count; index++) // 선택지 순회
            {
                int captured = index; // 클릭용 번호 복사
                Button button = RuntimeUiKit.CreateButton(choiceRoot, $"Choice_{index}", new Color(0.96f, 0.96f, 0.98f, 0.95f)); // 밝은 선택지 버튼
                float y = top - (index * (height + gap)); // 버튼 위치
                RuntimeUiKit.SetRect((RectTransform)button.transform, new Vector2(0f, y - height), new Vector2(1f, y)); // 버튼 배치
                button.gameObject.AddComponent<Outline>().effectColor = WindowLineColor; // 회청색 외곽선
                Text text = RuntimeUiKit.CreateText(button.transform, "Label", ResolveHeroName(choices[index].Text), ProjectH.Core.GameSettings.ScaleFontSize(24), BodyColor).BestFit(16); // 선택지 문구 (Day68 — 이름 반영)
                RuntimeUiKit.Stretch(text.rectTransform, 10f); // 여백 확장
                button.onClick.AddListener(() => Choose(captured)); // 선택 연결
                choiceButtons.Add(button); // 목록 등록
            }
        }

        private void ClearChoices() // 선택지 버튼 제거
        {
            for (int index = 0; index < choiceButtons.Count; index++) // 버튼 순회
            {
                if (choiceButtons[index] != null) Destroy(choiceButtons[index].gameObject); // 버튼 제거
            }

            choiceButtons.Clear(); // 목록 비움
        }

        private void Choose(int index) // 선택지 고르기
        {
            if (runner == null || !runner.Choose(index)) // 종료·선택 실패 확인
            {
                return; // 무시
            }

            if (runner.IsFinished) Finish(); // 종료 처리
            else ShowCurrent(); // 다음 대사 표시
        }

        private void ConfirmSkip() // 건너뛰기 확인 창 열기
        {
            if (runner == null || runner.IsWaitingForChoice) // 종료·선택지 대기 확인
            {
                return; // 선택지는 건너뛸 수 없음
            }

            confirmPanel.SetActive(true); // 확인 창 표시
            confirmPanel.transform.SetAsLastSibling(); // 최상단 표시
        }

        private void RunSkip() // 다음 선택지 또는 끝까지 건너뛰기
        {
            confirmPanel.SetActive(false); // 확인 창 닫기
            if (runner == null) return; // 종료 확인
            runner.SkipToChoiceOrEnd(); // 진행기 건너뛰기

            if (runner.IsFinished) // 종료 확인
            {
                Finish(); // 종료 처리
                return; // 처리 끝
            }

            ShowCurrent(); // 멈춘 대사 표시
            CompleteTyping(); // 타자 효과 없이 바로 표시
        }

        private void ToggleAuto() // 자동 켜기·끄기
        {
            autoMode = !autoMode; // 상태 전환
            autoTimer = 0f; // 대기 초기화
            autoGlyph.color = autoMode ? AutoOnColor : Color.white; // 켜짐 금색
        }

        private void OpenLog() // 로그 창 열기
        {
            if (runner == null) return; // 종료 확인
            StringBuilder builder = new StringBuilder(); // 로그 문구 조립기

            for (int index = 0; index < runner.Log.Count; index++) // 로그 순회
            {
                DialogueLogEntry entry = runner.Log[index]; // 로그 한 줄

                if (entry.IsChoice) // 선택지 기록 확인
                {
                    builder.Append($"<color=#FFD27A>▶ {ResolveHeroName(entry.Text)}</color>\n\n"); // 선택지 금색
                    continue; // 다음 줄
                }

                string name = DialogueSpeakerInfo.ResolveName(entry.Speaker); // 화자 이름
                if (!string.IsNullOrEmpty(name)) builder.Append($"<color=#C8CDD8><b>{name}</b></color>\n"); // 이름 연회색
                builder.Append($"{ResolveHeroName(entry.Text)}\n\n"); // 대사
            }

            logText.text = builder.ToString(); // 로그 적용
            logPanel.SetActive(true); // 창 표시
            logPanel.transform.SetAsLastSibling(); // 최상단 표시
            Canvas.ForceUpdateCanvases(); // 내용 크기 즉시 계산
            logScroll.verticalNormalizedPosition = 0f; // 최신 대사로 스크롤
        }

        private void CloseLog() // 로그 창 닫기
        {
            logPanel.SetActive(false); // 창 숨김
        }

        private void OpenSettings() // 통합 설정 창 열기 (Day72 — 대사 속도·자동 넘김을 여기서 바꾼다)
        {
            if (settingsOpen) return; // 이미 열림
            settingsOpen = true; // 열림 표시 (자동 넘김·키 입력 정지)
            SettingsView.Open(_ => settingsOpen = false); // 설정 화면
        }

        private void HideUi() // 숨김 : 대화창·메뉴 숨기기 (아무 곳이나 클릭하면 복귀)
        {
            uiHidden = true; // 숨김 상태
            uiRoot.SetActive(false); // UI 숨김
        }

        private void ShowUi() // 숨긴 UI 복귀
        {
            uiHidden = false; // 숨김 해제
            uiRoot.SetActive(true); // UI 표시
        }

        private void RecordSeen(DialogueRunner finished) // 본 이야기 기록 (Day63 — 다시 보기 해금, 새로 기록했을 때만 저장)
        {
            if (!finished.IsFinished || GameManager.Instance == null || GameManager.Instance.Save == null) return; // 중간 종료·저장 없음
            if (DiaryService.MarkDialogueSeen(GameManager.Instance.Save.CurrentSave, scriptId)) GameManager.Instance.Save.SaveCurrent(); // 기록 후 저장
        }

        private void Finish() // 대화 종료
        {
            if (runner == null) // 중복 종료 확인
            {
                return; // 무시
            }

            DialogueRunner finished = runner; // 종료된 진행기 보관
            runner = null; // 중복 종료 방지
            enabled = false; // Update 중지
            RecordSeen(finished); // 끝까지 봤으면 일기장에 기록 (Day63)
            onFinished?.Invoke(finished); // 결과 반영 콜백
            Destroy(gameObject); // 화면 제거
        }
    }
}
