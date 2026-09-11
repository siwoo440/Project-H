using System; // 콜백 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using System.Text; // 로그 문구 조립 기능
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Data; // 캐릭터 이름 조회 기능
using ProjectH.Dialogue; // 대화 진행 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 대화 화면 방지
    public sealed class DialogueOverlayView : MonoBehaviour // 미연시형 대화 화면 (Day58 신규 — 배경·스탠딩·하단 대화창·이름표·SKIP/AUTO/LOG/CLOSE)
    {
        private const int SortingOrder = 600; // 캐릭터 화면(100) 위, 로딩창(1000) 아래
        private const float CharsPerSecond = 45f; // 타자 효과 속도
        private static readonly Color WindowColor = new Color(0.25f, 0.17f, 0.36f, 0.80f); // 대화창 보라색
        private static readonly Color NamePlateColor = new Color(0.17f, 0.11f, 0.26f, 0.95f); // 이름표 진보라색
        private static readonly Color AccentColor = new Color(0.80f, 0.70f, 1f, 1f); // 연보라 강조색
        private static readonly Color MenuColor = new Color(0.86f, 0.80f, 0.98f, 1f); // 메뉴 글자색
        private static readonly Color AutoOnColor = new Color(1f, 0.84f, 0.40f, 1f); // AUTO 켜짐 금색

        private readonly List<Button> choiceButtons = new List<Button>(); // 현재 선택지 버튼
        private DialogueRunner runner; // 대화 진행기
        private Action<DialogueRunner> onFinished; // 종료 콜백
        private string standingCharacterId = string.Empty; // 스탠딩 캐릭터 ID
        private string currentExpression = string.Empty; // 현재 표정
        private Image standingImage; // 스탠딩 이미지
        private Text standingLabel; // 임시 실루엣 위 이름·표정
        private GameObject uiRoot; // CLOSE로 숨길 UI 묶음
        private GameObject namePlate; // 이름표
        private Text nameText; // 이름
        private Text bodyText; // 대사
        private Text continueMark; // 넘김 표시 ▼
        private Text autoLabel; // AUTO 버튼 글자
        private RectTransform choiceRoot; // 선택지 영역
        private GameObject logPanel; // LOG 창
        private Text logText; // LOG 내용
        private ScrollRect logScroll; // LOG 스크롤
        private GameObject confirmPanel; // SKIP 확인 창
        private string fullText = string.Empty; // 현재 대사 전체
        private float visibleChars; // 표시 중인 글자 수
        private bool isTyping; // 타자 효과 진행 여부
        private bool autoMode; // AUTO 켜짐 여부
        private float autoTimer; // AUTO 대기 시간
        private bool uiHidden; // CLOSE로 숨긴 상태

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
            view.standingCharacterId = script.CharacterId; // 스탠딩 캐릭터 저장
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
            standingImage = RuntimeUiKit.CreateImage(transform, "Standing", Color.white); // 스탠딩 생성
            standingImage.preserveAspect = true; // 비율 유지
            standingImage.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(standingImage.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.68f, 0.97f)); // 화면 중앙 하단 배치 (대화창이 하체를 가림)
            standingLabel = RuntimeUiKit.CreateText(standingImage.transform, "PlaceholderLabel", string.Empty, 26, new Color(0.20f, 0.16f, 0.26f, 0.9f)); // 임시 실루엣 이름·표정
            RuntimeUiKit.SetRect(standingLabel.rectTransform, new Vector2(0f, 0.40f), new Vector2(1f, 0.56f)); // 가슴 높이 배치
            standingImage.gameObject.SetActive(!string.IsNullOrEmpty(standingCharacterId)); // 스탠딩 캐릭터가 없으면 숨김

            Button clickCatcher = RuntimeUiKit.CreateButton(transform, "ClickCatcher", new Color(0f, 0f, 0f, 0f), false); // 전체 화면 클릭 영역 (투명)
            RuntimeUiKit.Stretch((RectTransform)clickCatcher.transform); // 전체 화면
            clickCatcher.onClick.AddListener(OnScreenClick); // 클릭 → 넘기기

            uiRoot = new GameObject("DialogueUi", typeof(RectTransform)); // 숨김 대상 묶음
            uiRoot.transform.SetParent(transform, false); // 부모 연결
            RuntimeUiKit.Stretch((RectTransform)uiRoot.transform); // 전체 화면
            BuildLocationTag(script); // 장소 표시
            BuildMessageWindow(); // 대화창·이름표
            BuildMenuBar(); // 하단 메뉴
            GameObject choiceObject = new GameObject("Choices", typeof(RectTransform)); // 선택지 영역 생성
            choiceObject.transform.SetParent(uiRoot.transform, false); // 부모 연결
            choiceRoot = (RectTransform)choiceObject.transform; // 영역 저장
            RuntimeUiKit.SetRect(choiceRoot, new Vector2(0.26f, 0.36f), new Vector2(0.74f, 0.86f)); // 화면 가운데 배치
            BuildLogPanel(); // LOG 창
            BuildConfirmPanel(); // SKIP 확인 창
        }

        private void BuildLocationTag(DialogueScript script) // 좌상단 장소 표시
        {
            Image tag = RuntimeUiKit.CreateImage(uiRoot.transform, "LocationTag", new Color(0.12f, 0.08f, 0.18f, 0.62f)); // 반투명 띠
            tag.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(tag.rectTransform, new Vector2(0f, 0.925f), new Vector2(0.34f, 0.98f)); // 좌상단 배치
            string title = string.IsNullOrEmpty(script.Title) ? string.Empty : $"「{script.Title}」  "; // 제목 문구
            Text text = RuntimeUiKit.CreateText(tag.transform, "Text", $"{title}{script.Location}", 19, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft).Overflow(); // 장소 문구
            RuntimeUiKit.Stretch(text.rectTransform, 14f); // 여백 확장
        }

        private void BuildMessageWindow() // 하단 대화창·이름표·대사·넘김 표시
        {
            Image window = RuntimeUiKit.CreateImage(uiRoot.transform, "MessageWindow", WindowColor); // 대화창 생성
            window.raycastTarget = false; // 클릭은 전체 화면 영역이 받음
            RuntimeUiKit.SetRect(window.rectTransform, new Vector2(0.01f, 0.05f), new Vector2(0.99f, 0.30f)); // 하단 배치
            Image topLine = RuntimeUiKit.CreateImage(window.transform, "TopLine", AccentColor); // 윗선 생성
            topLine.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(topLine.rectTransform, new Vector2(0f, 0.985f), new Vector2(1f, 1f)); // 윗선 배치

            Image plate = RuntimeUiKit.CreateImage(uiRoot.transform, "NamePlate", NamePlateColor); // 이름표 생성
            plate.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(plate.rectTransform, new Vector2(0.025f, 0.30f), new Vector2(0.20f, 0.355f)); // 대화창 왼쪽 위 배치
            Image plateLine = RuntimeUiKit.CreateImage(plate.transform, "Underline", AccentColor); // 이름표 아랫선
            plateLine.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(plateLine.rectTransform, new Vector2(0.06f, 0f), new Vector2(0.94f, 0.06f)); // 아랫선 배치
            nameText = RuntimeUiKit.CreateText(plate.transform, "Name", string.Empty, 24, Color.white).Overflow(); // 이름 생성
            RuntimeUiKit.Stretch(nameText.rectTransform); // 이름표 채움
            namePlate = plate.gameObject; // 이름표 저장

            bodyText = RuntimeUiKit.CreateText(window.transform, "Body", string.Empty, 27, Color.white, FontStyle.Normal, TextAnchor.UpperLeft).Wrap().Outlined(new Color(0f, 0f, 0f, 0.45f), new Vector2(1.5f, -1.5f)); // 대사 생성
            bodyText.lineSpacing = 1.15f; // 줄 간격
            RuntimeUiKit.SetRect(bodyText.rectTransform, new Vector2(0.035f, 0.10f), new Vector2(0.94f, 0.80f)); // 대사 배치
            continueMark = RuntimeUiKit.CreateText(window.transform, "ContinueMark", "▼", 22, AccentColor); // 넘김 표시 생성
            RuntimeUiKit.SetRect(continueMark.rectTransform, new Vector2(0.95f, 0.06f), new Vector2(0.985f, 0.26f)); // 오른쪽 아래 배치
        }

        private void BuildMenuBar() // 하단 메뉴 (SKIP·AUTO·LOG·CLOSE)
        {
            Image bar = RuntimeUiKit.CreateImage(uiRoot.transform, "MenuBar", new Color(0.14f, 0.09f, 0.22f, 0.88f)); // 메뉴 띠 생성
            bar.raycastTarget = false; // 입력 통과 (버튼만 입력)
            RuntimeUiKit.SetRect(bar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.045f)); // 화면 맨 아래 배치
            CreateMenuButton(bar.transform, "SKIP", 0.66f, ConfirmSkip); // 건너뛰기
            autoLabel = CreateMenuButton(bar.transform, "AUTO", 0.745f, ToggleAuto); // 자동 넘기기
            CreateMenuButton(bar.transform, "LOG", 0.83f, OpenLog); // 대화 로그
            CreateMenuButton(bar.transform, "CLOSE", 0.915f, HideUi); // 대화창 숨기기 (기획서 'UI 비활성화')
        }

        private Text CreateMenuButton(Transform parent, string label, float left, UnityEngine.Events.UnityAction action) // 메뉴 글자 버튼 생성
        {
            Button button = RuntimeUiKit.CreateButton(parent, $"Menu_{label}", new Color(1f, 1f, 1f, 0f)); // 투명 버튼
            RuntimeUiKit.SetRect((RectTransform)button.transform, new Vector2(left, 0f), new Vector2(left + 0.08f, 1f)); // 오른쪽부터 배치
            Text text = RuntimeUiKit.CreateText(button.transform, "Label", label, 17, MenuColor).Overflow(); // 메뉴 글자
            RuntimeUiKit.Stretch(text.rectTransform); // 버튼 채움
            button.onClick.AddListener(action); // 기능 연결
            return text; // 글자 반환
        }

        private void BuildLogPanel() // LOG 창 (지나간 대사 목록)
        {
            Image panel = RuntimeUiKit.CreateImage(transform, "LogPanel", new Color(0.05f, 0.03f, 0.09f, 0.90f)); // 어두운 전체 창 (뒤 클릭 차단)
            RuntimeUiKit.Stretch(panel.rectTransform); // 전체 화면
            Text title = RuntimeUiKit.CreateText(panel.transform, "Title", "LOG", 26, AccentColor, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            RuntimeUiKit.SetRect(title.rectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.50f, 0.97f)); // 제목 배치
            Button close = RuntimeUiKit.CreateButton(panel.transform, "CloseLog", new Color(0.30f, 0.22f, 0.42f, 0.95f)); // 닫기 버튼
            RuntimeUiKit.SetRect((RectTransform)close.transform, new Vector2(0.84f, 0.905f), new Vector2(0.92f, 0.965f)); // 닫기 배치
            Text closeLabel = RuntimeUiKit.CreateText(close.transform, "Label", "닫기", 18, Color.white); // 닫기 글자
            RuntimeUiKit.Stretch(closeLabel.rectTransform); // 버튼 채움
            close.onClick.AddListener(CloseLog); // 닫기 연결

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

        private void BuildConfirmPanel() // SKIP 확인 창
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "SkipConfirm", new Color(0f, 0f, 0f, 0.55f)); // 뒤 화면 어둡게 (클릭 차단)
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체 화면
            Image box = RuntimeUiKit.CreateImage(dim.transform, "Box", new Color(0.20f, 0.13f, 0.30f, 0.97f)); // 확인 상자
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.33f, 0.38f), new Vector2(0.67f, 0.62f)); // 가운데 배치
            box.gameObject.AddComponent<Outline>().effectColor = AccentColor; // 연보라 외곽선
            Text message = RuntimeUiKit.CreateText(box.transform, "Message", "해당 장면을 건너뛰시겠습니까?\n<size=17>선택지가 나오면 멈추고, 완료한 이야기는 다시 볼 수 있어요.</size>", 22, Color.white).Wrap(); // 안내 문구
            message.supportRichText = true; // 글자 크기 태그 사용
            RuntimeUiKit.SetRect(message.rectTransform, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.94f)); // 문구 배치
            CreateConfirmButton(box.transform, "네", new Vector2(0.10f, 0.10f), new Vector2(0.46f, 0.34f), RunSkip); // 건너뛰기 실행
            CreateConfirmButton(box.transform, "아니요", new Vector2(0.54f, 0.10f), new Vector2(0.90f, 0.34f), () => confirmPanel.SetActive(false)); // 창 닫기
            confirmPanel = dim.gameObject; // 창 저장
            confirmPanel.SetActive(false); // 초기 숨김
        }

        private static void CreateConfirmButton(Transform parent, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action) // 확인 창 버튼 생성
        {
            Button button = RuntimeUiKit.CreateButton(parent, $"Confirm_{label}", new Color(0.36f, 0.26f, 0.52f, 1f)); // 버튼 생성
            RuntimeUiKit.SetRect((RectTransform)button.transform, min, max); // 버튼 배치
            Text text = RuntimeUiKit.CreateText(button.transform, "Label", label, 20, Color.white); // 버튼 글자
            RuntimeUiKit.Stretch(text.rectTransform); // 버튼 채움
            button.onClick.AddListener(action); // 기능 연결
        }

        private void Update() // 타자 효과·AUTO·키보드 처리
        {
            if (runner == null) // 종료 확인
            {
                return; // 종료 후 처리 없음
            }

            float delta = Time.unscaledDeltaTime; // 일시정지와 무관한 시간

            if (isTyping) // 타자 효과 진행 확인
            {
                visibleChars += CharsPerSecond * delta; // 표시 글자 증가
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

            continueMark.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.35f + (0.65f * Mathf.PingPong(Time.unscaledTime * 1.6f, 1f))); // ▼ 깜빡임
            bool modalOpen = logPanel.activeSelf || confirmPanel.activeSelf; // 창 열림 여부

            if (autoMode && !isTyping && !modalOpen && !uiHidden && runner.Current != null && !runner.IsWaitingForChoice) // AUTO 넘김 조건
            {
                autoTimer += delta; // 대기 누적

                if (autoTimer >= 1.0f + (fullText.Length * 0.035f)) // 대사 길이만큼 기다린 뒤
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
                if (logPanel.activeSelf) CloseLog(); // LOG 닫기
                confirmPanel.SetActive(false); // 확인 창 닫기
            }
        }

        private void OnScreenClick() // 화면 클릭 처리
        {
            if (runner == null) // 종료 확인
            {
                return; // 무시
            }

            if (uiHidden) // CLOSE 상태 확인
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
            autoTimer = 0f; // AUTO 대기 초기화

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

            bool isStandingSpeaker = node.Speaker == standingCharacterId; // 스탠딩 캐릭터가 말하는지
            if (isStandingSpeaker && !string.IsNullOrEmpty(node.Expression)) currentExpression = node.Expression; // 표정 갱신
            RefreshStanding(isStandingSpeaker || node.Speaker == DialogueSpeakers.Narration); // 스탠딩 갱신 (주인공 대사 중에는 살짝 어둡게)
            string speakerName = ResolveSpeakerName(node.Speaker); // 이름 조회
            namePlate.SetActive(!string.IsNullOrEmpty(speakerName)); // 나레이션은 이름표 숨김
            nameText.text = speakerName; // 이름 적용
            bodyText.fontStyle = node.Speaker == DialogueSpeakers.Narration ? FontStyle.Italic : FontStyle.Normal; // 나레이션은 기울임
            fullText = node.Text; // 대사 저장
            visibleChars = 0f; // 타자 효과 시작
            bodyText.text = string.Empty; // 대사 비움
            isTyping = true; // 타자 효과 진행
            continueMark.gameObject.SetActive(false); // ▼ 숨김
        }

        private void CompleteTyping() // 대사 전체 표시 후 선택지·▼ 표시
        {
            isTyping = false; // 타자 효과 종료
            bodyText.text = fullText; // 전체 대사
            autoTimer = 0f; // AUTO 대기 시작

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

        private void RefreshStanding(bool bright) // 스탠딩 이미지·밝기 갱신
        {
            if (string.IsNullOrEmpty(standingCharacterId)) // 스탠딩 캐릭터 확인
            {
                return; // 갱신 생략
            }

            standingImage.sprite = DialogueArtFactory.GetStanding(standingCharacterId, currentExpression, out bool placeholder); // 표정별 스탠딩
            Color tint = placeholder ? DialogueArtFactory.GetCharacterTint(standingCharacterId) : Color.white; // 임시 실루엣은 캐릭터 색
            standingImage.color = bright ? tint : Color.Lerp(tint, Color.black, 0.28f); // 말하지 않을 때 어둡게
            standingLabel.gameObject.SetActive(placeholder); // 정식 아트가 있으면 글자 숨김
            standingLabel.text = placeholder ? $"{ResolveSpeakerName(standingCharacterId)}\n<size=20>[{(string.IsNullOrEmpty(currentExpression) ? "기본" : currentExpression)}]</size>" : string.Empty; // 이름·표정 표시
            standingLabel.supportRichText = true; // 글자 크기 태그 사용
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
                Button button = RuntimeUiKit.CreateButton(choiceRoot, $"Choice_{index}", new Color(0.22f, 0.14f, 0.32f, 0.92f)); // 선택지 버튼
                float y = top - (index * (height + gap)); // 버튼 위치
                RuntimeUiKit.SetRect((RectTransform)button.transform, new Vector2(0f, y - height), new Vector2(1f, y)); // 버튼 배치
                button.gameObject.AddComponent<Outline>().effectColor = AccentColor; // 연보라 외곽선
                Text text = RuntimeUiKit.CreateText(button.transform, "Label", choices[index].Text, 24, Color.white).BestFit(16); // 선택지 문구
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

        private void ConfirmSkip() // SKIP 확인 창 열기
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

        private void ToggleAuto() // AUTO 켜기·끄기
        {
            autoMode = !autoMode; // 상태 전환
            autoTimer = 0f; // 대기 초기화
            autoLabel.color = autoMode ? AutoOnColor : MenuColor; // 켜짐 표시
            autoLabel.text = autoMode ? "AUTO ●" : "AUTO"; // 켜짐 표시 문구
        }

        private void OpenLog() // LOG 창 열기
        {
            if (runner == null) return; // 종료 확인
            StringBuilder builder = new StringBuilder(); // 로그 문구 조립기

            for (int index = 0; index < runner.Log.Count; index++) // 로그 순회
            {
                DialogueLogEntry entry = runner.Log[index]; // 로그 한 줄

                if (entry.IsChoice) // 선택지 기록 확인
                {
                    builder.Append($"<color=#FFD27A>▶ {entry.Text}</color>\n\n"); // 선택지 금색
                    continue; // 다음 줄
                }

                string name = ResolveSpeakerName(entry.Speaker); // 화자 이름
                if (!string.IsNullOrEmpty(name)) builder.Append($"<color=#C9B6F2><b>{name}</b></color>\n"); // 이름 연보라
                builder.Append($"{entry.Text}\n\n"); // 대사
            }

            logText.text = builder.ToString(); // 로그 적용
            logPanel.SetActive(true); // 창 표시
            logPanel.transform.SetAsLastSibling(); // 최상단 표시
            Canvas.ForceUpdateCanvases(); // 내용 크기 즉시 계산
            logScroll.verticalNormalizedPosition = 0f; // 최신 대사로 스크롤
        }

        private void CloseLog() // LOG 창 닫기
        {
            logPanel.SetActive(false); // 창 숨김
        }

        private void HideUi() // CLOSE : 대화창·메뉴 숨기기 (아무 곳이나 클릭하면 복귀)
        {
            uiHidden = true; // 숨김 상태
            uiRoot.SetActive(false); // UI 숨김
        }

        private void ShowUi() // 숨긴 UI 복귀
        {
            uiHidden = false; // 숨김 해제
            uiRoot.SetActive(true); // UI 표시
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
            onFinished?.Invoke(finished); // 결과 반영 콜백
            Destroy(gameObject); // 화면 제거
        }

        private static string ResolveSpeakerName(string speaker) // 화자 이름 조회
        {
            if (speaker == DialogueSpeakers.Narration) return string.Empty; // 나레이션 이름 없음
            if (speaker == DialogueSpeakers.Hero) return DialogueSpeakers.HeroLabel; // 주인공 이름
            DataManager data = GameManager.Instance == null ? null : GameManager.Instance.Data; // 데이터 관리자 조회
            CharacterData character = data == null ? null : data.GetCharacter(speaker); // 캐릭터 원본 조회
            return character == null ? speaker : character.DisplayName; // 표시 이름 반환
        }
    }
}
