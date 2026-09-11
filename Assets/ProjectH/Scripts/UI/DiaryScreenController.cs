using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 궁극기 컷인 기능
using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.Data; // 캐릭터·몬스터·던전 데이터 기능
using ProjectH.Dialogue; // 대사 파일 기능
using ProjectH.Diary; // 일기장 목록·해금 기능
using ProjectH.SaveSystem; // 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 일기장 화면 중복 방지
    public sealed class DiaryScreenController : MonoBehaviour // 일기장 화면 (Day63 신규 — 목업 11번 : 왼쪽 페이지 목록 · 오른쪽 페이지 설명, 시나리오 · CG·컷신 · 몬스터 정보 · 세계관)
    {
        private const int RowsPerPage = 11; // 왼쪽 페이지 한 쪽 줄 수
        private static readonly Color LeatherColor = new Color(0.22f, 0.14f, 0.10f, 1f); // 가죽 표지
        private static readonly Color PaperColor = new Color(0.96f, 0.93f, 0.85f, 1f); // 종이
        private static readonly Color InkColor = new Color(0.20f, 0.15f, 0.12f, 1f); // 잉크
        private static readonly Color FadedInk = new Color(0.52f, 0.46f, 0.40f, 1f); // 흐린 잉크
        private static readonly Color RowColor = new Color(0.90f, 0.86f, 0.76f, 1f); // 줄 배경
        private static readonly Color RowSelectedColor = new Color(0.98f, 0.80f, 0.52f, 1f); // 선택 줄
        private static readonly Color TabOnColor = new Color(0.96f, 0.93f, 0.85f, 1f); // 선택 책갈피
        private static readonly Color TabOffColor = new Color(0.46f, 0.30f, 0.20f, 1f); // 일반 책갈피
        private static readonly Color ActionColor = new Color(0.52f, 0.30f, 0.18f, 1f); // 실행 버튼

        public static string ReturnScene = GameScenes.Lobby; // 닫기 시 돌아갈 씬 (타이틀에서 열면 Title)

        private enum DiaryTab // 일기장 탭
        {
            Scenario = 0, // 시나리오
            Gallery = 1, // CG · 컷신
            Monster = 2, // 몬스터 정보
            Glossary = 3 // 세계관
        }

        private const int GallerySlotCount = 6; // 오른쪽 그림 칸 수 (가로 2 × 세로 3)

        private sealed class GalleryItem // CG · 컷신 한 장
        {
            public string Title; // 제목
            public string Caption; // 칸 아래 글자
            public bool Unlocked; // 열림 여부
            public Sprite Sprite; // 그림
            public string StandingId; // 임시 CG 실루엣 캐릭터
            public Color Tint = Color.white; // 그림 색 (임시 컷인 실루엣)
            public string Question; // 재생 질문
            public System.Action Play; // 재생 동작
            public bool IsStill; // 정지 컷신 여부 (true = 크게 보기 · 사진 아이콘 / false = 재생 질문 · 동영상 아이콘)
        }

        private sealed class GallerySlot // 그림 칸 한 개
        {
            public Button Button; // 누르기
            public Image Thumb; // 그림
            public Image Standing; // 실루엣
            public Text Caption; // 아래 글자
            public Text Lock; // 잠김 표시
            public Outline Outline; // 고른 칸 테두리
            public Image Badge; // 오른쪽 위 사진·동영상 아이콘
        }

        private sealed class DiaryRow // 왼쪽 페이지 한 줄 (탭마다 내용이 다름)
        {
            public string Text; // 줄 글자
            public bool Unlocked; // 열림 여부
            public System.Action ShowDetail; // 오른쪽 페이지 채우기
        }

        private readonly Button[] tabButtons = new Button[4]; // 책갈피
        private readonly List<Button> rowButtons = new List<Button>(); // 줄 버튼
        private readonly List<DiaryRow> rows = new List<DiaryRow>(); // 현재 탭 줄 목록
        private int page; // 현재 쪽
        private int selectedRow = -1; // 선택 줄
        private Text pageText; // 쪽 표시
        private Text leftTitle; // 왼쪽 페이지 제목
        private Text detailTitle; // 오른쪽 제목
        private Text detailSub; // 오른쪽 부제
        private Text detailBody; // 오른쪽 본문
        private Image preview; // 오른쪽 그림
        private Image previewStanding; // 그림 위 캐릭터 실루엣
        private Button actionButton; // 다시 보기 · 크게 보기 · 재생
        private Text actionLabel; // 버튼 글자
        private System.Action action; // 버튼 동작
        private DiaryImageViewer imageViewer; // 크게 보기 창 (휠 확대·축소, Day63 추가)
        private GameObject galleryRoot; // CG · 컷신 2×3 칸 (Day63 추가)
        private readonly List<GallerySlot> gallerySlots = new List<GallerySlot>(); // 그림 칸 6개
        private readonly List<GalleryItem> galleryItems = new List<GalleryItem>(); // CG · 컷신 항목
        private int galleryPage; // 그림 칸 쪽
        private int galleryFocus = -1; // 왼쪽 목록에서 고른 항목
        private Text galleryPageText; // 그림 칸 쪽 표시

        private void Start() // 일기장 시작
        {
            EnsureEventSystem(); // UI 입력 보장
            BuildUi(); // 화면 구성
            SelectTab(DiaryTab.Scenario); // 시나리오부터
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
            eventSystemObject.transform.SetParent(transform, false); // 화면 하위 연결
        }

        private void BuildUi() // 가죽 표지 위 펼친 책 두 쪽 + 책갈피 탭
        {
            GameObject canvasObject = new GameObject("DiaryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Canvas
            canvasObject.transform.SetParent(transform, false); // 화면 하위
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay
            canvas.sortingOrder = 100; // 대화(600)·컷인 아래
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 스케일러
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기준
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간
            Transform root = canvasObject.transform; // 루트
            Image leather = CreateImage(root, "Leather", LeatherColor); // 가죽 표지
            Stretch(leather.rectTransform); // 전체
            Button close = CreateButton(root, "Close", "◀  닫기", new Color(0.36f, 0.24f, 0.16f, 1f), Color.white); // 닫기
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.015f, 0.905f), new Vector2(0.115f, 0.975f)); // 왼쪽 위
            close.onClick.AddListener(Close); // 돌아가기
            Text title = CreateText(root, "Title", "일기장", 32, new Color(0.98f, 0.88f, 0.66f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            SetRect(title.rectTransform, new Vector2(0.13f, 0.90f), new Vector2(0.30f, 0.98f)); // 제목 배치
            string[] names = { "시나리오", "CG · 컷신", "몬스터 정보", "세계관" }; // 책갈피 이름

            for (int index = 0; index < tabButtons.Length; index++) // 책갈피 생성
            {
                DiaryTab tab = (DiaryTab)index; // 탭 값
                tabButtons[index] = CreateButton(root, "Tab_" + tab, names[index], TabOffColor, InkColor); // 책갈피
                SetRect(tabButtons[index].GetComponent<RectTransform>(), new Vector2(0.36f + (index * 0.155f), 0.875f), new Vector2(0.505f + (index * 0.155f), 0.955f)); // 위쪽에 나란히
                tabButtons[index].onClick.AddListener(() => SelectTab(tab)); // 탭 선택
            }

            Image leftPage = CreateImage(root, "LeftPage", PaperColor); // 왼쪽 페이지
            SetRect(leftPage.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.497f, 0.875f)); // 왼쪽
            Image rightPage = CreateImage(root, "RightPage", PaperColor); // 오른쪽 페이지
            SetRect(rightPage.rectTransform, new Vector2(0.503f, 0.04f), new Vector2(0.97f, 0.875f)); // 오른쪽
            Image spine = CreateImage(root, "Spine", new Color(0.70f, 0.62f, 0.50f, 1f)); // 책 가운데 접힌 선
            SetRect(spine.rectTransform, new Vector2(0.496f, 0.04f), new Vector2(0.504f, 0.875f)); // 가운데
            BuildLeftPage(leftPage.transform); // 목록 쪽
            BuildRightPage(rightPage.transform); // 설명 쪽
            imageViewer = DiaryImageViewer.Create(root); // 크게 보기 창 (휠 확대·축소 · 재생 질문)
        }

        private void BuildLeftPage(Transform page) // 왼쪽 페이지 : 제목 + 줄 목록 + 쪽 넘김
        {
            leftTitle = CreateText(page, "LeftTitle", string.Empty, 22, InkColor, FontStyle.Bold, TextAnchor.MiddleLeft); // 제목
            SetRect(leftTitle.rectTransform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.98f)); // 위

            for (int index = 0; index < RowsPerPage; index++) // 줄 생성
            {
                int captured = index; // 클릭용 번호
                Button row = CreateButton(page, "Row_" + index, string.Empty, RowColor, InkColor); // 줄 버튼
                float top = 0.90f - (index * 0.072f); // 위치
                SetRect(row.GetComponent<RectTransform>(), new Vector2(0.04f, top - 0.062f), new Vector2(0.96f, top)); // 배치
                Text label = row.GetComponentInChildren<Text>(); // 줄 글자
                label.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
                label.fontStyle = FontStyle.Normal; // 보통 굵기
                row.onClick.AddListener(() => SelectRow(captured)); // 줄 선택 (쪽 번호는 SelectRow에서 계산)
                rowButtons.Add(row); // 목록 등록
            }

            Button prev = CreateButton(page, "Prev", "◀", TabOffColor, Color.white); // 이전 쪽
            SetRect(prev.GetComponent<RectTransform>(), new Vector2(0.04f, 0.02f), new Vector2(0.14f, 0.07f)); // 왼쪽 아래
            prev.onClick.AddListener(() => ChangePage(-1)); // 이전
            pageText = CreateText(page, "Page", "1 / 1", 17, FadedInk, FontStyle.Bold); // 쪽 표시
            SetRect(pageText.rectTransform, new Vector2(0.15f, 0.02f), new Vector2(0.35f, 0.07f)); // 가운데
            Button next = CreateButton(page, "Next", "▶", TabOffColor, Color.white); // 다음 쪽
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.36f, 0.02f), new Vector2(0.46f, 0.07f)); // 오른쪽
            next.onClick.AddListener(() => ChangePage(1)); // 다음
        }

        private void BuildRightPage(Transform page) // 오른쪽 페이지 : 그림 · 제목 · 부제 · 본문 · 버튼
        {
            preview = CreateImage(page, "Preview", Color.white); // 그림
            preview.preserveAspect = true; // 비율 유지
            preview.raycastTarget = false; // 입력 통과
            SetRect(preview.rectTransform, new Vector2(0.06f, 0.50f), new Vector2(0.94f, 0.96f)); // 위쪽
            previewStanding = CreateImage(preview.transform, "Standing", Color.white); // 실루엣
            previewStanding.preserveAspect = true; // 비율 유지
            previewStanding.raycastTarget = false; // 입력 통과
            SetRect(previewStanding.rectTransform, new Vector2(0.30f, 0f), new Vector2(0.70f, 1f)); // 가운데
            detailTitle = CreateText(page, "DetailTitle", string.Empty, 30, InkColor, FontStyle.Bold, TextAnchor.UpperLeft).BestFit(16); // 제목
            detailSub = CreateText(page, "DetailSub", string.Empty, 17, new Color(0.60f, 0.36f, 0.20f, 1f), FontStyle.Bold, TextAnchor.UpperLeft).Wrap(); // 부제
            detailBody = CreateText(page, "DetailBody", string.Empty, 18, InkColor, FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 본문
            detailBody.lineSpacing = 1.2f; // 줄 간격
            actionButton = CreateButton(page, "Action", string.Empty, ActionColor, Color.white); // 실행 버튼
            SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.60f, 0.03f), new Vector2(0.94f, 0.11f)); // 오른쪽 아래
            actionLabel = actionButton.GetComponentInChildren<Text>(); // 버튼 글자
            actionButton.onClick.AddListener(() => action?.Invoke()); // 실행
            BuildGalleryGrid(page); // CG · 컷신 그림 칸 (Day63 추가)
        }

        private void BuildGalleryGrid(Transform page) // 가로 2 × 세로 3 그림 칸 + 쪽 넘김
        {
            GameObject grid = new GameObject("GalleryGrid", typeof(RectTransform)); // 칸 묶음
            grid.transform.SetParent(page, false); // 오른쪽 페이지
            RectTransform gridRect = (RectTransform)grid.transform; // 영역
            SetRect(gridRect, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.98f)); // 쪽 넘김 위

            for (int index = 0; index < GallerySlotCount; index++) // 칸 생성
            {
                int column = index % 2; // 열 (0·1)
                int row = index / 2; // 행 (0·1·2)
                int captured = index; // 클릭용 번호
                GallerySlot slot = new GallerySlot(); // 칸 참조
                slot.Button = CreateButton(gridRect, "Slot_" + index, string.Empty, new Color(0.30f, 0.24f, 0.20f, 1f), Color.white); // 칸 버튼 (어두운 액자)
                SetRect(slot.Button.GetComponent<RectTransform>(), new Vector2((column * 0.5f) + 0.015f, 1f - ((row + 1) / 3f) + 0.01f), new Vector2(((column + 1) * 0.5f) - 0.015f, 1f - (row / 3f) - 0.01f)); // 격자 배치
                slot.Outline = slot.Button.gameObject.AddComponent<Outline>(); // 고른 칸 테두리
                slot.Outline.effectDistance = new Vector2(3f, -3f); // 두께
                slot.Thumb = CreateImage(slot.Button.transform, "Thumb", Color.white); // 그림
                slot.Thumb.preserveAspect = true; // 비율 유지
                slot.Thumb.raycastTarget = false; // 버튼이 입력 받음
                SetRect(slot.Thumb.rectTransform, new Vector2(0.03f, 0.20f), new Vector2(0.97f, 0.97f)); // 위쪽
                slot.Standing = CreateImage(slot.Thumb.transform, "Standing", Color.white); // 실루엣
                slot.Standing.preserveAspect = true; // 비율 유지
                slot.Standing.raycastTarget = false; // 입력 통과
                SetRect(slot.Standing.rectTransform, new Vector2(0.30f, 0f), new Vector2(0.70f, 1f)); // 가운데
                slot.Lock = CreateText(slot.Button.transform, "Lock", "?", 48, new Color(1f, 1f, 1f, 0.35f), FontStyle.Bold); // 잠김 표시
                SetRect(slot.Lock.rectTransform, new Vector2(0f, 0.20f), new Vector2(1f, 1f)); // 그림 자리
                slot.Caption = CreateText(slot.Button.transform, "Caption", string.Empty, 14, new Color(0.96f, 0.92f, 0.84f, 1f), FontStyle.Bold).BestFit(9); // 아래 글자
                SetRect(slot.Caption.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.20f)); // 아래
                slot.Badge = CreateImage(slot.Button.transform, "Badge", Color.white); // 오른쪽 위 아이콘 (사진 · 동영상)
                slot.Badge.raycastTarget = false; // 입력 통과
                RectTransform badgeRect = slot.Badge.rectTransform; // 아이콘 영역
                badgeRect.anchorMin = Vector2.one; // 오른쪽 위 기준
                badgeRect.anchorMax = Vector2.one; // 오른쪽 위 기준
                badgeRect.pivot = Vector2.one; // 오른쪽 위 기준점
                badgeRect.anchoredPosition = new Vector2(-6f, -6f); // 모서리 안쪽
                badgeRect.sizeDelta = new Vector2(38f, 38f); // 크기
                slot.Button.onClick.AddListener(() => OpenGallerySlot(captured)); // 크게 보기
                gallerySlots.Add(slot); // 목록 등록
            }

            Button prev = CreateButton(page, "GalleryPrev", "◀", TabOffColor, Color.white); // 이전 쪽
            SetRect(prev.GetComponent<RectTransform>(), new Vector2(0.30f, 0.02f), new Vector2(0.40f, 0.075f)); // 아래
            prev.onClick.AddListener(() => ChangeGalleryPage(-1)); // 이전
            galleryPageText = CreateText(page, "GalleryPage", "1 / 1", 17, FadedInk, FontStyle.Bold); // 쪽 표시
            SetRect(galleryPageText.rectTransform, new Vector2(0.40f, 0.02f), new Vector2(0.60f, 0.075f)); // 가운데
            Button next = CreateButton(page, "GalleryNext", "▶", TabOffColor, Color.white); // 다음 쪽
            SetRect(next.GetComponent<RectTransform>(), new Vector2(0.60f, 0.02f), new Vector2(0.70f, 0.075f)); // 아래
            next.onClick.AddListener(() => ChangeGalleryPage(1)); // 다음
            galleryRoot = new GameObject("GalleryRoot", typeof(RectTransform)); // 켜고 끄기 묶음
            galleryRoot.transform.SetParent(page, false); // 오른쪽 페이지
            Stretch((RectTransform)galleryRoot.transform); // 전체
            grid.transform.SetParent(galleryRoot.transform, false); // 칸 묶음 이동
            prev.transform.SetParent(galleryRoot.transform, false); // 이전 버튼 이동
            galleryPageText.transform.SetParent(galleryRoot.transform, false); // 쪽 표시 이동
            next.transform.SetParent(galleryRoot.transform, false); // 다음 버튼 이동
            galleryRoot.SetActive(false); // 처음엔 숨김
        }

        private void SelectTab(DiaryTab tab) // 탭 선택 → 줄 목록 다시 만들기
        {
            page = 0; // 첫 쪽
            selectedRow = -1; // 선택 없음

            for (int index = 0; index < tabButtons.Length; index++) // 책갈피 색
            {
                tabButtons[index].GetComponent<Image>().color = index == (int)tab ? TabOnColor : TabOffColor; // 선택 책갈피는 종이색
            }

            rows.Clear(); // 목록 비움
            galleryItems.Clear(); // 그림 항목 비움
            galleryPage = 0; // 그림 첫 쪽
            galleryFocus = -1; // 고른 그림 없음
            if (tab == DiaryTab.Scenario) BuildScenarioRows(); // 시나리오
            else if (tab == DiaryTab.Gallery) BuildGalleryRows(); // CG · 컷신
            else if (tab == DiaryTab.Monster) BuildMonsterRows(); // 몬스터
            else BuildGlossaryRows(); // 세계관
            int unlocked = 0; // 열린 수
            foreach (DiaryRow row in rows) if (row.Unlocked) unlocked++; // 개수 세기
            leftTitle.text = $"{new[] { "시나리오", "CG · 컷신", "몬스터 정보", "세계관" }[(int)tab]}   {unlocked} / {rows.Count}"; // 제목 + 수집률
            RefreshRows(); // 줄 표시
            bool gallery = tab == DiaryTab.Gallery; // 그림 칸 탭 여부
            SetDetailVisible(!gallery); // 그림 칸 탭에서는 설명 글자 숨김
            galleryRoot.SetActive(gallery); // 그림 칸 표시
            if (gallery) RefreshGallery(); // 그림 칸 채우기
            else ClearDetail(tab == DiaryTab.Glossary || tab == DiaryTab.Monster ? "왼쪽에서 단어를 고르면 설명이 여기에 적힙니다." : "왼쪽에서 항목을 고르세요."); // 오른쪽 비움
        }

        private void BuildScenarioRows() // 시나리오 줄 (분류 · 캐릭터 · 제목)
        {
            SaveData saveData = GetSave(); // 현재 저장

            foreach (DiaryScenarioEntry entry in DiaryCatalog.Scenarios) // 이야기 순회
            {
                DiaryScenarioEntry captured = entry; // 클릭용 복사
                bool seen = DiaryService.IsDialogueSeen(saveData, entry.ScriptId); // 본 이야기
                string head = $"[{DiaryCatalog.GetCategoryLabel(entry.Category)}] {GetName(entry.CharacterId)}"; // 앞부분
                rows.Add(new DiaryRow { Text = seen ? $"{head} · {entry.Title}" : $"{head} · ???", Unlocked = seen, ShowDetail = () => ShowScenario(captured, seen) }); // 줄 추가
            }
        }

        private void BuildGalleryRows() // CG · 컷신 줄 + 그림 칸 항목 (Day63 — 오른쪽 2×3 칸)
        {
            SaveData saveData = GetSave(); // 현재 저장
            if (DevelopmentFeatures.Enabled) AddTestCg(); // 개발용 테스트 CG를 1번 칸에 (출시 빌드에서는 없음)

            foreach (DiaryCgEntry cg in DiaryCatalog.Cgs) // CG 순회
            {
                bool seen = DiaryService.IsDialogueSeen(saveData, cg.ScriptId); // 해금
                DialogueScript script = DialogueLibrary.Load(cg.ScriptId); // 연결 이야기
                string title = script == null ? cg.Id : script.Title; // 제목
                Sprite art = RuntimeSpriteLoader.Load("Diary/CG/" + cg.Id); // 정식 CG
                GalleryItem item = new GalleryItem // 그림 항목
                {
                    Title = title, // 제목
                    Caption = seen ? $"{GetName(cg.CharacterId)} · {title}" : "???", // 칸 글자
                    Unlocked = seen, // 열림
                    Sprite = art != null ? art : script == null ? null : DialogueArtFactory.GetBackground(script.Background), // 정식 CG 또는 이야기 배경
                    StandingId = art == null ? cg.CharacterId : null, // 임시 CG는 실루엣 겹침
                    Question = "CG를 재생하겠습니까?", // 재생 질문
                    Play = script == null ? null : (System.Action)(() => DialogueOverlayView.Open(script, null)) // 네 → CG 장면 재생 (보상 없음)
                };
                AddGalleryItem(item, seen ? $"CG · {GetName(cg.CharacterId)} · {title}" : $"CG · {GetName(cg.CharacterId)} · ???"); // 줄 + 칸
            }

            foreach (string characterId in DiaryCatalog.Starters) // 궁극기 컷신
            {
                string captured = characterId; // 클릭용 복사
                bool owned = saveData != null && saveData.FindCharacter(characterId) != null; // 동료로 합류
                string ultimate = BattleUltimateEffectExecutor.GetUltimateName(characterId); // 궁극기 이름
                Sprite cutIn = DialogueArtFactory.GetCutIn(characterId, out bool placeholder); // 컷인 그림
                int bond = owned ? saveData.FindCharacter(characterId).BondLevel : 0; // 결속 단계 (5단계 금빛 연출)
                GalleryItem item = new GalleryItem // 그림 항목
                {
                    Title = $"「{ultimate}」", // 제목
                    Caption = owned ? $"{GetName(characterId)} · 「{ultimate}」" : "???", // 칸 글자
                    Unlocked = owned, // 열림
                    Sprite = cutIn, // 컷인 그림
                    Tint = placeholder ? DialogueArtFactory.GetCharacterTint(characterId) : Color.white, // 임시 실루엣 색
                    Question = "컷신을 재생하겠습니까?", // 재생 질문
                    Play = () => UltimateCutInView.Show(UltimateCutInCatalog.Build(captured, GetName(captured), ultimate, bond), null) // 네 → 궁극기 컷신
                };
                AddGalleryItem(item, owned ? $"컷신 · {GetName(characterId)} · 궁극기 「{ultimate}」" : "컷신 · ??? · ???"); // 줄 + 칸
            }
        }

        private void AddTestCg() // 개발용 테스트 칸 2개 : CG(재생 질문) + 정지 컷신(크게 보기) — 항상 열림
        {
            DialogueScript script = DialogueLibrary.Load("DIARY_TEST_CG"); // 테스트 장면
            GalleryItem item = new GalleryItem // 그림 항목
            {
                Title = "테스트 CG", // 제목
                Caption = "테스트 · 노을 CG (개발용)", // 칸 글자
                Unlocked = true, // 항상 열림
                Sprite = RuntimeSpriteLoader.Load("Diary/CG/CG_TEST"), // 테스트 그림 (기본 Texture로 가져온 PNG)
                Question = "CG를 재생하겠습니까?", // 재생 질문
                Play = script == null ? null : (System.Action)(() => DialogueOverlayView.Open(script, null)) // 네 → CG가 나오는 테스트 장면
            };
            AddGalleryItem(item, "CG · 테스트 · 노을 CG (개발용)"); // 줄 + 칸
            GalleryItem still = new GalleryItem // 개발용 정지 컷신 (Resources/Diary/Stills/STILL_TEST.png — 크게 보기 테스트)
            {
                Title = "테스트 컷신", // 제목
                Caption = "테스트 · 밤의 성역 (정지 컷신)", // 칸 글자
                Unlocked = true, // 항상 열림
                Sprite = RuntimeSpriteLoader.Load("Diary/Stills/STILL_TEST"), // 정지 그림
                IsStill = true // 사진 아이콘 · 크게 보기
            };
            AddGalleryItem(still, "컷신(정적) · 테스트 · 밤의 성역 (개발용)"); // 줄 + 칸
        }

        private void AddGalleryItem(GalleryItem item, string rowText) // 왼쪽 줄과 오른쪽 칸을 같은 순서로 추가
        {
            int index = galleryItems.Count; // 항목 번호
            galleryItems.Add(item); // 칸 항목
            rows.Add(new DiaryRow { Text = rowText, Unlocked = item.Unlocked, ShowDetail = () => FocusGallery(index) }); // 줄 (누르면 그 칸이 있는 쪽으로)
        }

        private void RefreshGallery() // 현재 쪽 그림 칸 6개 채우기
        {
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(galleryItems.Count / (float)GallerySlotCount)); // 쪽 수
            galleryPage = Mathf.Clamp(galleryPage, 0, pageCount - 1); // 쪽 보정
            galleryPageText.text = $"{galleryPage + 1} / {pageCount}"; // 쪽 표시

            for (int index = 0; index < gallerySlots.Count; index++) // 칸 순회
            {
                GallerySlot slot = gallerySlots[index]; // 칸
                int itemIndex = (galleryPage * GallerySlotCount) + index; // 항목 번호
                GalleryItem item = itemIndex < galleryItems.Count ? galleryItems[itemIndex] : null; // 항목
                slot.Button.gameObject.SetActive(item != null); // 빈 칸 숨김
                if (item == null) continue; // 다음 칸
                slot.Thumb.gameObject.SetActive(item.Unlocked && item.Sprite != null); // 열린 그림만 표시
                slot.Thumb.sprite = item.Sprite; // 그림
                slot.Thumb.color = item.Tint; // 색
                slot.Standing.gameObject.SetActive(item.Unlocked && !string.IsNullOrEmpty(item.StandingId)); // 임시 CG 실루엣
                if (slot.Standing.gameObject.activeSelf) ApplyStanding(slot.Standing, item.StandingId); // 실루엣 적용
                slot.Lock.gameObject.SetActive(!item.Unlocked); // 잠김 표시
                slot.Caption.text = item.Caption; // 아래 글자
                slot.Badge.sprite = item.IsStill ? DiaryBadgeArt.Photo : DiaryBadgeArt.Video; // 사진 = 크게 보는 정지 컷신 · 동영상 = 재생되는 CG·컷신
                slot.Badge.color = item.Unlocked ? Color.white : new Color(1f, 1f, 1f, 0.45f); // 잠긴 칸은 흐리게
                slot.Outline.effectColor = itemIndex == galleryFocus ? new Color(1f, 0.78f, 0.36f, 1f) : new Color(0f, 0f, 0f, 0.35f); // 고른 칸 금테
            }
        }

        private void ChangeGalleryPage(int delta) // 그림 칸 쪽 넘기기
        {
            galleryPage += delta; // 쪽 변경
            RefreshGallery(); // 표시
        }

        private void FocusGallery(int itemIndex) // 왼쪽 목록에서 고른 항목의 칸으로 이동
        {
            galleryFocus = itemIndex; // 고른 항목
            galleryPage = itemIndex / GallerySlotCount; // 그 칸이 있는 쪽
            RefreshGallery(); // 표시
        }

        private void OpenGallerySlot(int slotIndex) // 칸 누르기 → 전체 화면 크게 보기 (+ 재생 질문)
        {
            int itemIndex = (galleryPage * GallerySlotCount) + slotIndex; // 항목 번호
            if (itemIndex >= galleryItems.Count) return; // 빈 칸
            GalleryItem item = galleryItems[itemIndex]; // 항목
            galleryFocus = itemIndex; // 고른 칸
            RefreshGallery(); // 금테
            if (!item.Unlocked) return; // 잠긴 칸은 열지 않음
            if (item.IsStill) imageViewer.OpenImage(item.Sprite, item.StandingId, item.Tint); // 정지 컷신 → 전체 화면 크게 보기 (휠 확대 · 닫기)
            else if (item.Play != null) imageViewer.AskPlay(item.Question, item.Play); // CG·컷신 → 가운데 "재생하겠습니까?"만
        }

        private void BuildMonsterRows() // 몬스터 줄 (만난 몬스터만 이름 공개)
        {
            SaveData saveData = GetSave(); // 현재 저장
            DataManager dataManager = GetData(); // 데이터

            foreach (string monsterId in GetMonsterIds(dataManager)) // 몬스터 순회
            {
                MonsterData monster = dataManager.GetMonster(monsterId); // 원본
                if (monster == null) continue; // 누락 제외
                bool seen = saveData != null && saveData.HasSeenMonster(monsterId); // 만남 여부
                bool boss = monster.BossPattern != null; // 보스 여부
                rows.Add(new DiaryRow { Text = seen ? $"{(boss ? "[보스] " : string.Empty)}{monster.DisplayName}" : "???", Unlocked = seen, ShowDetail = () => ShowMonster(monster, seen) }); // 줄 추가
            }
        }

        private void BuildGlossaryRows() // 세계관 단어 줄 (분류 순)
        {
            SaveData saveData = GetSave(); // 현재 저장

            foreach (GlossaryEntry entry in WorldGlossaryCatalog.All) // 단어 순회
            {
                GlossaryEntry captured = entry; // 클릭용 복사
                bool open = entry.IsUnlocked(saveData); // 열림 여부
                rows.Add(new DiaryRow { Text = $"[{WorldGlossaryCatalog.GetCategoryLabel(entry.Category)}] {(open ? entry.Term : "???")}", Unlocked = open, ShowDetail = () => ShowGlossary(captured, open) }); // 줄 추가
            }
        }

        private void RefreshRows() // 현재 쪽 줄 표시
        {
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(rows.Count / (float)RowsPerPage)); // 쪽 수
            page = Mathf.Clamp(page, 0, pageCount - 1); // 쪽 보정
            pageText.text = $"{page + 1} / {pageCount}"; // 쪽 표시

            for (int index = 0; index < rowButtons.Count; index++) // 줄 순회
            {
                int rowIndex = (page * RowsPerPage) + index; // 전체 번호
                bool exists = rowIndex < rows.Count; // 줄 존재
                rowButtons[index].gameObject.SetActive(exists); // 없으면 숨김
                if (!exists) continue; // 다음
                Text label = rowButtons[index].GetComponentInChildren<Text>(); // 줄 글자
                label.text = rows[rowIndex].Text; // 글자
                label.color = rows[rowIndex].Unlocked ? InkColor : FadedInk; // 잠긴 줄 흐리게
                rowButtons[index].GetComponent<Image>().color = rowIndex == selectedRow ? RowSelectedColor : RowColor; // 선택 강조
            }
        }

        private void ChangePage(int delta) // 쪽 넘기기
        {
            page += delta; // 쪽 변경
            RefreshRows(); // 표시
        }

        private void SelectRow(int indexOnPage) // 줄 선택 → 오른쪽 페이지
        {
            int rowIndex = (page * RowsPerPage) + indexOnPage; // 전체 번호
            if (rowIndex >= rows.Count) return; // 빈 줄
            selectedRow = rowIndex; // 선택 저장
            RefreshRows(); // 강조
            rows[rowIndex].ShowDetail?.Invoke(); // 오른쪽 페이지 채우기
        }

        private void ShowScenario(DiaryScenarioEntry entry, bool seen) // 시나리오 설명 + 다시 보기
        {
            DialogueScript script = DialogueLibrary.Load(entry.ScriptId); // 대사 파일
            SetPreview(script == null ? null : DialogueArtFactory.GetBackground(script.Background), seen ? entry.CharacterId : null); // 배경 + 실루엣

            if (!seen) // 아직 안 본 이야기
            {
                SetDetail("???", $"{DiaryCatalog.GetCategoryLabel(entry.Category)} · {GetName(entry.CharacterId)}", "아직 보지 않은 이야기입니다. 게임에서 끝까지 보면 여기서 다시 볼 수 있어요.", null, null); // 안내
                return; // 종료
            }

            string lines = script == null ? "-" : $"{script.Location} · 대사 {script.Nodes.Count}줄"; // 장소·길이
            SetDetail(entry.Title, $"{DiaryCatalog.GetCategoryLabel(entry.Category)} · {GetName(entry.CharacterId)}", $"{lines}\n\n다시 볼 때는 호감도·결속 자원 같은 보상이 없고, 기록만 다시 넘겨 봅니다.", "다시 보기", () => DialogueOverlayView.Open(script, null)); // 다시 보기 (보상 없음)
        }

        private void ShowMonster(MonsterData monster, bool seen) // 몬스터 설명 (속성 · 약점 · 등급 · 출현 던전)
        {
            SetPreview(null, null); // 그림 없음 (정식 몬스터 그림은 73일차)

            if (!seen) // 못 만난 몬스터
            {
                SetDetail("???", "미확인 몬스터", "아직 만나지 못한 몬스터입니다. 던전 전투에서 마주치면 기록됩니다.", null, null); // 안내
                return; // 종료
            }

            SaveData saveData = GetSave(); // 현재 저장
            DataManager dataManager = GetData(); // 데이터
            bool weaknessKnown = DiaryService.IsWeaknessKnown(saveData, dataManager, monster.Id); // 약점 공개 여부
            List<string> weakNames = new List<string>(); // 약점 이름
            foreach (ElementType element in DiaryService.GetWeaknesses(monster.Element)) weakNames.Add(DiaryService.GetElementLabel(element)); // 약점 속성
            List<string> dungeonNames = new List<string>(); // 출현 던전 이름

            foreach (string dungeonId in DiaryService.GetAppearanceDungeonIds(dataManager, monster.Id)) // 출현 던전
            {
                DungeonData dungeon = dataManager.GetDungeon(dungeonId); // 던전
                dungeonNames.Add(dungeon == null ? dungeonId : dungeon.DisplayName); // 이름
            }

            string weakness = weaknessKnown ? (weakNames.Count > 0 ? string.Join(" · ", weakNames) : "없음") : "???  (출현 던전을 클리어하면 공개)"; // 약점 문구
            string body = $"속성  {DiaryService.GetElementLabel(monster.Element)}\n약점  {weakness}\n체력  {DiaryService.GetStatGrade(monster.MaxHp, 1500, 3000)}   공격  {DiaryService.GetStatGrade(monster.Attack, 9, 12)}\n출현  {(dungeonNames.Count > 0 ? string.Join(", ", dungeonNames) : "-")}\n\n{monster.Description}"; // 본문
            SetDetail(monster.DisplayName, monster.BossPattern != null ? "보스 몬스터 · 페이즈 패턴 있음" : "몬스터", body, null, null); // 표시
        }

        private void ShowGlossary(GlossaryEntry entry, bool open) // 세계관 단어 설명
        {
            SetPreview(null, null); // 그림 없음
            SetDetail(open ? entry.Term : "???", WorldGlossaryCatalog.GetCategoryLabel(entry.Category), open ? entry.Description : "이야기가 진행되면 알게 되는 단어입니다.", null, null); // 표시
        }

        private void SetPreview(Sprite sprite, string standingCharacterId) // 오른쪽 그림 (없으면 숨기고 글자를 위로)
        {
            bool hasImage = sprite != null; // 그림 여부
            preview.gameObject.SetActive(hasImage); // 표시 여부
            preview.sprite = sprite; // 그림
            preview.color = Color.white; // 기본 색
            previewStanding.gameObject.SetActive(hasImage && !string.IsNullOrEmpty(standingCharacterId)); // 실루엣 여부
            if (previewStanding.gameObject.activeSelf) ApplyStanding(previewStanding, standingCharacterId); // 실루엣 적용
            float top = hasImage ? 0.47f : 0.95f; // 글자 시작 높이
            SetRect(detailTitle.rectTransform, new Vector2(0.06f, top - 0.08f), new Vector2(0.94f, top)); // 제목
            SetRect(detailSub.rectTransform, new Vector2(0.06f, top - 0.13f), new Vector2(0.94f, top - 0.08f)); // 부제
            SetRect(detailBody.rectTransform, new Vector2(0.06f, 0.13f), new Vector2(0.94f, top - 0.14f)); // 본문
        }

        private void SetDetail(string title, string sub, string body, string buttonLabel, System.Action buttonAction) // 오른쪽 글자 + 버튼
        {
            detailTitle.text = title; // 제목
            detailSub.text = sub; // 부제
            detailBody.text = body; // 본문
            action = buttonAction; // 버튼 동작
            actionButton.gameObject.SetActive(buttonAction != null); // 동작 있을 때만 버튼
            actionLabel.text = buttonLabel ?? string.Empty; // 버튼 글자
        }

        private void SetDetailVisible(bool visible) // 오른쪽 설명 요소 표시 여부 (그림 칸 탭에서는 숨김)
        {
            detailTitle.gameObject.SetActive(visible); // 제목
            detailSub.gameObject.SetActive(visible); // 부제
            detailBody.gameObject.SetActive(visible); // 본문
            if (!visible) preview.gameObject.SetActive(false); // 그림
            if (!visible) actionButton.gameObject.SetActive(false); // 버튼
        }

        private void ClearDetail(string hint) // 오른쪽 페이지 비우기
        {
            SetPreview(null, null); // 그림 없음
            SetDetail(string.Empty, string.Empty, hint, null, null); // 안내만
        }

        private static void ApplyStanding(Image image, string characterId) // 캐릭터 스탠딩 (임시면 캐릭터 색)
        {
            image.sprite = DialogueArtFactory.GetStanding(characterId, null, out bool placeholder); // 스탠딩
            image.color = placeholder ? DialogueArtFactory.GetCharacterTint(characterId) : Color.white; // 색
        }

        private static List<string> GetMonsterIds(DataManager dataManager) // 일기장 몬스터 목록 (던전 웨이브 등장 순, 중복 없음)
        {
            List<string> result = new List<string>(); // 결과
            if (dataManager == null) return result; // 데이터 없음

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                DungeonData dungeon = dataManager.GetDungeon(dungeonId); // 던전
                if (dungeon == null || dungeon.EncounterWaves == null) continue; // 웨이브 없음

                foreach (DungeonEncounterWave wave in dungeon.EncounterWaves) // 웨이브 순회
                {
                    if (wave == null || wave.MonsterIds == null) continue; // 빈 웨이브
                    foreach (string id in wave.MonsterIds) if (!string.IsNullOrEmpty(id) && !result.Contains(id)) result.Add(id); // 새 몬스터
                }
            }

            return result; // 결과 반환
        }

        private void Close() // 닫기 → 연 곳으로 돌아감
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) return; // 씬 로더 없음
            GameManager.Instance.Scenes.LoadScene(string.IsNullOrEmpty(ReturnScene) ? GameScenes.Lobby : ReturnScene); // 돌아가기
        }

        private string GetName(string characterId) // 캐릭터 이름
        {
            CharacterData data = GetData() == null ? null : GetData().GetCharacter(characterId); // 원본
            return data == null ? characterId : data.DisplayName; // 이름
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
            Text label = CreateText(button.transform, "Label", labelText, 18, labelColor).BestFit(10); // 자동 크기 라벨
            Stretch(label.rectTransform, 8f); // 라벨 확장
            return button; // 버튼 반환
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) => RuntimeUiKit.SetRect(rect, anchorMin, anchorMax); // UI 앵커 영역 설정 (RuntimeUiKit 위임)

        private static void Stretch(RectTransform rect, float padding = 0f) => RuntimeUiKit.Stretch(rect, padding); // RectTransform 전체 확장 (RuntimeUiKit 위임)
    }
}
