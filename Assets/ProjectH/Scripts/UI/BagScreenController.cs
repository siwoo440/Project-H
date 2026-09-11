using System; // 문자열 비교 기능
using System.Collections.Generic; // 아이템 스택 목록 기능
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Data; // 아이템 정적 데이터 기능
using ProjectH.SaveSystem; // 아이템 저장 및 사용 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 가방 화면 방지
    public sealed class BagScreenController : MonoBehaviour // Day38 가방 화면 컨트롤러
    {
        public const int SlotColumnCount = 7; // 가방 가로 슬롯 수
        public const int MinimumSlotCount = 28; // 가방 최소 표시 슬롯 수
        private const int DemoPotionTargetCount = 5; // 테스트 물약 목표 수량
        private const int DemoMaterialTargetCount = 12; // 테스트 재료 목표 수량
        private const int DemoQuestTargetCount = 1; // 테스트 퀘스트 아이템 목표 수량
        private RectTransform inventoryContent; // 가방 Grid 콘텐츠 영역
        private Image illustrationImage; // 선택 아이템 일러스트 이미지
        private Text illustrationPlaceholderText; // 선택 아이템 임시 일러스트 텍스트
        private Text itemNameText; // 선택 아이템 이름 텍스트
        private Text itemMetaText; // 선택 아이템 메타 정보 텍스트
        private Text itemDescriptionText; // 선택 아이템 설명 텍스트
        private Text itemCountText; // 선택 아이템 수량 텍스트
        private Text inventoryCountText; // 보유 아이템 종류 수 텍스트
        private Text statusText; // 가방 상태 텍스트
        private Button useButton; // 선택 아이템 사용 버튼
        private string selectedItemId = string.Empty; // 현재 선택 아이템 ID

        private void Start() // 가방 화면 시작
        {
            EnsureEventSystem(); // UI 입력 시스템 보장
            BuildUi(); // 가방 Runtime UI 생성
            Refresh(); // 가방 데이터 초기 갱신
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

                return; // 기존 EventSystem 보정 완료
            }

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 신규 EventSystem 생성
            eventSystemObject.transform.SetParent(transform, false); // 가방 화면 하위 EventSystem 연결
        }

        private void BuildUi() // 전체 가방 UI 구성
        {
            GameObject canvasObject = new GameObject("BagCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Runtime Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 가방 화면 하위 Canvas 연결
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 100; // 가방 UI 정렬 순서 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1600f, 900f); // 가방 화면 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로 세로 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            Image background = CreateImage(canvas.transform, "Background", new Color(0.93f, 0.93f, 0.93f, 1f)); // 전체 배경 생성
            background.raycastTarget = false; // 전체 배경 입력 비활성화
            Stretch(background.rectTransform); // 전체 배경 확장
            BuildHeader(background.transform); // 상단 메뉴 구성
            BuildInventoryPanel(background.transform); // 좌측 가방 Grid 구성
            BuildDetailPanel(background.transform); // 우측 아이템 상세 구성
        }

        private void BuildHeader(Transform parent) // 가방 상단 메뉴 구성
        {
            Button backButton = CreateButton(parent, "BackButton", "◀  로비", new Color(0.78f, 0.87f, 0.94f, 1f)); // 로비 복귀 버튼 생성
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.025f, 0.92f), new Vector2(0.16f, 0.975f)); // 로비 복귀 버튼 배치
            backButton.onClick.AddListener(ReturnToLobby); // 로비 복귀 이벤트 연결
            Text titleText = CreateText(parent, "Title", "가방", 32, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 가방 제목 생성
            SetRect(titleText.rectTransform, new Vector2(0.38f, 0.92f), new Vector2(0.62f, 0.975f)); // 가방 제목 배치
            Button grantButton = CreateButton(parent, "GrantDemoItems", "테스트 아이템 지급", new Color(0.88f, 0.83f, 0.66f, 1f)); // 테스트 아이템 지급 버튼 생성
            SetRect(grantButton.GetComponent<RectTransform>(), new Vector2(0.78f, 0.92f), new Vector2(0.975f, 0.975f)); // 테스트 아이템 지급 버튼 배치
            grantButton.onClick.AddListener(GrantDemoItems); // 테스트 아이템 지급 이벤트 연결
        }

        private void BuildInventoryPanel(Transform parent) // 좌측 가방 목록 구성
        {
            Image panel = CreateImage(parent, "InventoryPanel", new Color(0.97f, 0.97f, 0.97f, 1f)); // 가방 목록 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.025f, 0.055f), new Vector2(0.70f, 0.90f)); // 가방 목록 패널 배치
            AddOutline(panel.gameObject); // 가방 목록 패널 외곽선 추가
            Text label = CreateText(panel.transform, "InventoryLabel", "보유 아이템", 24, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 가방 목록 제목 생성
            SetRect(label.rectTransform, new Vector2(0.03f, 0.92f), new Vector2(0.26f, 0.985f)); // 가방 목록 제목 배치
            inventoryCountText = CreateText(panel.transform, "InventoryCount", "0종", 18, FontStyle.Bold, new Color(0.30f, 0.34f, 0.40f, 1f)); // 보유 아이템 종류 수 텍스트 생성
            SetRect(inventoryCountText.rectTransform, new Vector2(0.26f, 0.92f), new Vector2(0.40f, 0.985f)); // 보유 아이템 종류 수 배치
            BuildScrollView(panel.transform); // 7열 가방 ScrollView 생성
            statusText = CreateText(panel.transform, "Status", "아이템을 선택하세요.", 17, FontStyle.Normal, new Color(0.25f, 0.28f, 0.32f, 1f)); // 가방 상태 텍스트 생성
            statusText.alignment = TextAnchor.MiddleLeft; // 가방 상태 텍스트 왼쪽 정렬
            SetRect(statusText.rectTransform, new Vector2(0.035f, 0.015f), new Vector2(0.965f, 0.075f)); // 가방 상태 텍스트 배치
        }

        private void BuildScrollView(Transform parent) // 가방 ScrollView 구성
        {
            GameObject scrollObject = new GameObject("ItemScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect)); // ScrollView 루트 생성
            scrollObject.transform.SetParent(parent, false); // 가방 목록 패널 하위 연결
            Image scrollImage = scrollObject.GetComponent<Image>(); // ScrollView 배경 이미지 조회
            scrollImage.color = new Color(0.90f, 0.91f, 0.92f, 1f); // ScrollView 배경색 적용
            AddOutline(scrollObject); // ScrollView 외곽선 추가
            SetRect(scrollObject.GetComponent<RectTransform>(), new Vector2(0.025f, 0.09f), new Vector2(0.975f, 0.91f)); // ScrollView 배치
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); // ScrollView Viewport 생성
            viewportObject.transform.SetParent(scrollObject.transform, false); // ScrollView Viewport 부모 연결
            Image viewportImage = viewportObject.GetComponent<Image>(); // Viewport 이미지 조회
            viewportImage.color = new Color(1f, 1f, 1f, 0.35f); // Viewport 마스크 색상 적용
            Mask viewportMask = viewportObject.GetComponent<Mask>(); // Viewport 마스크 조회
            viewportMask.showMaskGraphic = false; // Viewport 마스크 이미지 숨김
            SetRect(viewportObject.GetComponent<RectTransform>(), new Vector2(0.0f, 0.0f), new Vector2(0.965f, 1.0f)); // Viewport 배치
            GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // Grid 콘텐츠 생성
            contentObject.transform.SetParent(viewportObject.transform, false); // Grid 콘텐츠 Viewport 연결
            inventoryContent = contentObject.GetComponent<RectTransform>(); // Grid 콘텐츠 RectTransform 조회
            inventoryContent.anchorMin = new Vector2(0f, 1f); // Grid 콘텐츠 최소 앵커 설정
            inventoryContent.anchorMax = new Vector2(1f, 1f); // Grid 콘텐츠 최대 앵커 설정
            inventoryContent.pivot = new Vector2(0.5f, 1f); // Grid 콘텐츠 상단 Pivot 설정
            inventoryContent.anchoredPosition = Vector2.zero; // Grid 콘텐츠 위치 초기화
            inventoryContent.sizeDelta = Vector2.zero; // Grid 콘텐츠 크기 초기화
            GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>(); // GridLayoutGroup 조회
            grid.cellSize = new Vector2(130f, 130f); // 가방 슬롯 크기 설정
            grid.spacing = new Vector2(10f, 10f); // 가방 슬롯 간격 설정
            grid.padding = new RectOffset(12, 12, 12, 12); // 가방 Grid 내부 여백 설정
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft; // 가방 Grid 시작 위치 설정
            grid.startAxis = GridLayoutGroup.Axis.Horizontal; // 가방 Grid 가로 우선 배치 설정
            grid.childAlignment = TextAnchor.UpperLeft; // 가방 Grid 자식 정렬 설정
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 가방 Grid 고정 열 설정
            grid.constraintCount = SlotColumnCount; // 가방 가로 7칸 설정
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>(); // Grid 콘텐츠 크기 조절기 조회
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // Grid 콘텐츠 가로 크기 고정
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Grid 콘텐츠 세로 크기 자동 확장
            Scrollbar scrollbar = BuildVerticalScrollbar(scrollObject.transform); // 세로 Scrollbar 생성
            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>(); // ScrollRect 컴포넌트 조회
            scrollRect.viewport = viewportObject.GetComponent<RectTransform>(); // ScrollRect Viewport 연결
            scrollRect.content = inventoryContent; // ScrollRect Grid 콘텐츠 연결
            scrollRect.horizontal = false; // 가로 스크롤 비활성화
            scrollRect.vertical = true; // 세로 스크롤 활성화
            scrollRect.movementType = ScrollRect.MovementType.Clamped; // 스크롤 범위 제한 설정
            scrollRect.inertia = true; // 스크롤 관성 활성화
            scrollRect.decelerationRate = 0.135f; // 스크롤 감속률 설정
            scrollRect.scrollSensitivity = 35f; // 마우스 휠 감도 설정
            scrollRect.verticalScrollbar = scrollbar; // 세로 Scrollbar 연결
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide; // 세로 Scrollbar 자동 숨김 설정
        }

        private static Scrollbar BuildVerticalScrollbar(Transform parent) // 세로 Scrollbar 구성
        {
            GameObject scrollbarObject = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar)); // 세로 Scrollbar 루트 생성
            scrollbarObject.transform.SetParent(parent, false); // ScrollView 하위 Scrollbar 연결
            Image background = scrollbarObject.GetComponent<Image>(); // Scrollbar 배경 이미지 조회
            background.color = new Color(0.73f, 0.75f, 0.78f, 1f); // Scrollbar 배경색 적용
            SetRect(scrollbarObject.GetComponent<RectTransform>(), new Vector2(0.972f, 0.02f), new Vector2(0.995f, 0.98f)); // Scrollbar 배치
            GameObject slidingAreaObject = new GameObject("SlidingArea", typeof(RectTransform)); // Scrollbar 이동 영역 생성
            slidingAreaObject.transform.SetParent(scrollbarObject.transform, false); // Scrollbar 이동 영역 연결
            Stretch(slidingAreaObject.GetComponent<RectTransform>(), 2f); // Scrollbar 이동 영역 확장
            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image)); // Scrollbar Handle 생성
            handleObject.transform.SetParent(slidingAreaObject.transform, false); // Scrollbar Handle 연결
            Image handleImage = handleObject.GetComponent<Image>(); // Scrollbar Handle 이미지 조회
            handleImage.color = new Color(0.32f, 0.37f, 0.44f, 1f); // Scrollbar Handle 색상 적용
            RectTransform handleRect = handleObject.GetComponent<RectTransform>(); // Scrollbar Handle RectTransform 조회
            handleRect.anchorMin = Vector2.zero; // Scrollbar Handle 최소 앵커 설정
            handleRect.anchorMax = Vector2.one; // Scrollbar Handle 최대 앵커 설정
            handleRect.offsetMin = Vector2.zero; // Scrollbar Handle 최소 오프셋 초기화
            handleRect.offsetMax = Vector2.zero; // Scrollbar Handle 최대 오프셋 초기화
            Scrollbar scrollbar = scrollbarObject.GetComponent<Scrollbar>(); // Scrollbar 컴포넌트 조회
            scrollbar.handleRect = handleRect; // Scrollbar Handle 연결
            scrollbar.targetGraphic = handleImage; // Scrollbar 대상 그래픽 연결
            scrollbar.direction = Scrollbar.Direction.BottomToTop; // Scrollbar 세로 방향 설정
            return scrollbar; // 생성 Scrollbar 반환
        }

        private void BuildDetailPanel(Transform parent) // 우측 아이템 상세 구성
        {
            Image illustrationPanel = CreateImage(parent, "IllustrationPanel", new Color(0.96f, 0.96f, 0.96f, 1f)); // 상단 일러스트 패널 생성
            SetRect(illustrationPanel.rectTransform, new Vector2(0.72f, 0.48f), new Vector2(0.975f, 0.90f)); // 상단 일러스트 패널 배치
            AddOutline(illustrationPanel.gameObject); // 상단 일러스트 패널 외곽선 추가
            Text illustrationLabel = CreateText(illustrationPanel.transform, "IllustrationLabel", "아이템 일러스트", 20, FontStyle.Bold, new Color(0.18f, 0.20f, 0.23f, 1f)); // 일러스트 패널 제목 생성
            SetRect(illustrationLabel.rectTransform, new Vector2(0.06f, 0.88f), new Vector2(0.94f, 0.98f)); // 일러스트 패널 제목 배치
            illustrationImage = CreateImage(illustrationPanel.transform, "ItemIllustration", Color.white); // 실제 아이템 일러스트 이미지 생성
            illustrationImage.preserveAspect = true; // 아이템 일러스트 비율 유지
            SetRect(illustrationImage.rectTransform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.84f)); // 아이템 일러스트 이미지 배치
            illustrationPlaceholderText = CreateText(illustrationPanel.transform, "IllustrationPlaceholder", "아이템을 선택하세요", 24, FontStyle.Bold, new Color(0.38f, 0.42f, 0.48f, 1f)); // 임시 일러스트 텍스트 생성
            SetRect(illustrationPlaceholderText.rectTransform, new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.84f)); // 임시 일러스트 텍스트 배치
            Image detailPanel = CreateImage(parent, "ItemDetailPanel", new Color(0.97f, 0.97f, 0.97f, 1f)); // 하단 아이템 상세 패널 생성
            SetRect(detailPanel.rectTransform, new Vector2(0.72f, 0.055f), new Vector2(0.975f, 0.46f)); // 하단 아이템 상세 패널 배치
            AddOutline(detailPanel.gameObject); // 하단 아이템 상세 패널 외곽선 추가
            itemNameText = CreateText(detailPanel.transform, "ItemName", "아이템을 선택하세요", 25, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 아이템 이름 텍스트 생성
            SetRect(itemNameText.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f)); // 아이템 이름 배치
            itemMetaText = CreateText(detailPanel.transform, "ItemMeta", "-", 17, FontStyle.Bold, new Color(0.28f, 0.32f, 0.38f, 1f)); // 아이템 메타 정보 생성
            SetRect(itemMetaText.rectTransform, new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.82f)); // 아이템 메타 정보 배치
            itemDescriptionText = CreateText(detailPanel.transform, "ItemDescription", "설명", 18, FontStyle.Normal, new Color(0.16f, 0.18f, 0.22f, 1f)); // 아이템 설명 텍스트 생성
            itemDescriptionText.alignment = TextAnchor.UpperLeft; // 아이템 설명 왼쪽 위 정렬
            itemDescriptionText.resizeTextForBestFit = false; // 아이템 설명 자동 크기 비활성화
            itemDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 아이템 설명 줄바꿈 활성화
            itemDescriptionText.verticalOverflow = VerticalWrapMode.Truncate; // 아이템 설명 영역 초과 잘림 설정
            SetRect(itemDescriptionText.rectTransform, new Vector2(0.07f, 0.34f), new Vector2(0.93f, 0.67f)); // 아이템 설명 배치
            itemCountText = CreateText(detailPanel.transform, "ItemCount", "보유 수량 0", 19, FontStyle.Bold, new Color(0.20f, 0.24f, 0.30f, 1f)); // 아이템 수량 텍스트 생성
            SetRect(itemCountText.rectTransform, new Vector2(0.07f, 0.22f), new Vector2(0.55f, 0.33f)); // 아이템 수량 배치
            useButton = CreateButton(detailPanel.transform, "UseButton", "사용", new Color(0.78f, 0.87f, 0.94f, 1f)); // 아이템 사용 버튼 생성
            SetRect(useButton.GetComponent<RectTransform>(), new Vector2(0.60f, 0.08f), new Vector2(0.93f, 0.27f)); // 아이템 사용 버튼 배치
            useButton.onClick.AddListener(UseSelectedItem); // 아이템 사용 이벤트 연결
            useButton.interactable = false; // 초기 아이템 사용 버튼 비활성화
        }

        public void Refresh() // 가방 화면 데이터 갱신
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData, out string error)) // 가방 실행 컨텍스트 조회
            {
                SetStatus(error); // 가방 컨텍스트 오류 표시
                ClearInventorySlots(); // 가방 슬롯 초기화
                ShowEmptyDetails(); // 아이템 상세 초기화
                return; // 가방 갱신 중단
            }

            List<ItemStackSaveData> visibleStacks = BuildVisibleStacks(saveData, dataManager); // 표시 가능한 아이템 스택 생성
            inventoryCountText.text = $"{visibleStacks.Count}종"; // 보유 아이템 종류 수 표시
            RebuildInventorySlots(visibleStacks, dataManager); // 7열 가방 슬롯 재생성

            if (!string.IsNullOrWhiteSpace(selectedItemId) && ItemInventoryService.GetCount(saveData, selectedItemId) <= 0) // 선택 아이템 소진 여부 확인
            {
                selectedItemId = string.Empty; // 소진 아이템 선택 해제
            }

            if (string.IsNullOrWhiteSpace(selectedItemId)) // 선택 아이템 존재 확인
            {
                ShowEmptyDetails(); // 빈 상세 화면 표시
            }
            else // 선택 아이템 존재 처리
            {
                ShowSelectedDetails(dataManager, saveData); // 선택 아이템 상세 갱신
            }

            if (saveManager == null) // 저장 관리자 사용 참조 확인
            {
                SetStatus("SaveManager를 찾을 수 없습니다."); // 저장 관리자 누락 상태 표시
            }
        }

        private static List<ItemStackSaveData> BuildVisibleStacks(SaveData saveData, DataManager dataManager) // 가방 표시 아이템 목록 생성
        {
            List<ItemStackSaveData> visibleStacks = new List<ItemStackSaveData>(); // 표시 아이템 목록 생성

            for (int index = 0; index < saveData.ItemInventory.Count; index++) // 저장 아이템 스택 순회
            {
                ItemStackSaveData stack = saveData.ItemInventory[index]; // 현재 아이템 스택 조회

                if (stack == null || stack.Quantity <= 0) // 아이템 스택 유효성 확인
                {
                    continue; // 빈 스택 제외
                }

                ItemData item = dataManager.GetItem(stack.ItemId); // 아이템 원본 데이터 조회

                if (item == null || item.Type == ItemType.Equipment) // 일반 가방 표시 가능 여부 확인
                {
                    continue; // 누락 데이터 및 장비 제외
                }

                visibleStacks.Add(stack); // 표시 아이템 스택 추가
            }

            visibleStacks.Sort((left, right) => string.Compare(left.ItemId, right.ItemId, StringComparison.Ordinal)); // 아이템 ID 기준 정렬
            return visibleStacks; // 표시 아이템 목록 반환
        }

        private void RebuildInventorySlots(List<ItemStackSaveData> stacks, DataManager dataManager) // 가방 슬롯 Grid 재생성
        {
            ClearInventorySlots(); // 기존 가방 슬롯 제거
            int rows = Mathf.Max(MinimumSlotCount / SlotColumnCount, Mathf.CeilToInt(stacks.Count / (float)SlotColumnCount)); // 필요한 가방 행 수 계산
            int slotCount = Mathf.Max(MinimumSlotCount, rows * SlotColumnCount); // 7열 기준 슬롯 수 계산

            for (int index = 0; index < slotCount; index++) // 가방 슬롯 순회
            {
                int slotNumber = index + 1; // 가방 슬롯 표시 번호 계산

                if (index >= stacks.Count) // 빈 슬롯 여부 확인
                {
                    Button emptyButton = CreateButton(inventoryContent, $"EmptySlot_{slotNumber:00}", $"[{slotNumber:00}]", new Color(0.86f, 0.87f, 0.89f, 1f)); // 빈 가방 슬롯 생성
                    emptyButton.interactable = false; // 빈 슬롯 입력 비활성화
                    continue; // 다음 슬롯 이동
                }

                ItemStackSaveData stack = stacks[index]; // 현재 보유 아이템 스택 조회
                ItemData item = dataManager.GetItem(stack.ItemId); // 현재 아이템 원본 조회
                string displayName = item == null || string.IsNullOrWhiteSpace(item.DisplayName) ? stack.ItemId : item.DisplayName; // 가방 슬롯 표시 이름 결정
                string slotLabel = $"[{slotNumber:00}]\n{displayName}\nx{stack.Quantity}"; // 가방 슬롯 라벨 생성
                Button itemButton = CreateButton(inventoryContent, $"ItemSlot_{slotNumber:00}", slotLabel, new Color(0.94f, 0.94f, 0.94f, 1f)); // 보유 아이템 슬롯 생성
                string capturedItemId = stack.ItemId; // 클릭 이벤트 아이템 ID 캡처
                itemButton.onClick.AddListener(() => SelectItem(capturedItemId)); // 아이템 슬롯 선택 이벤트 연결
            }
        }

        private void ClearInventorySlots() // 기존 가방 슬롯 제거
        {
            if (inventoryContent == null) // 가방 Grid 콘텐츠 존재 확인
            {
                return; // 슬롯 제거 중단
            }

            for (int index = inventoryContent.childCount - 1; index >= 0; index--) // 기존 슬롯 역순 순회
            {
                GameObject child = inventoryContent.GetChild(index).gameObject; // 기존 슬롯 객체 조회
                child.SetActive(false); // 기존 슬롯 즉시 숨김
                Destroy(child); // 기존 슬롯 제거 예약
            }
        }

        private void SelectItem(string itemId) // 가방 아이템 선택
        {
            selectedItemId = itemId ?? string.Empty; // 선택 아이템 ID 저장

            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData, out string error)) // 선택 아이템 컨텍스트 조회
            {
                SetStatus(error); // 선택 아이템 컨텍스트 오류 표시
                ShowEmptyDetails(); // 선택 아이템 상세 초기화
                return; // 아이템 선택 중단
            }

            ShowSelectedDetails(dataManager, saveData); // 선택 아이템 상세 표시
            SetStatus("아이템을 선택했습니다."); // 아이템 선택 상태 표시

            if (saveManager == null) // 저장 관리자 참조 유지 확인
            {
                SetStatus("SaveManager를 찾을 수 없습니다."); // 저장 관리자 누락 상태 표시
            }
        }

        private void ShowSelectedDetails(DataManager dataManager, SaveData saveData) // 선택 아이템 상세 표시
        {
            ItemData item = dataManager.GetItem(selectedItemId); // 선택 아이템 원본 조회
            int count = ItemInventoryService.GetCount(saveData, selectedItemId); // 선택 아이템 수량 조회

            if (item == null || count <= 0) // 선택 아이템 데이터 및 수량 확인
            {
                selectedItemId = string.Empty; // 잘못된 선택 아이템 해제
                ShowEmptyDetails(); // 빈 상세 화면 표시
                return; // 선택 아이템 상세 표시 중단
            }

            itemNameText.text = string.IsNullOrWhiteSpace(item.DisplayName) ? item.Id : item.DisplayName; // 선택 아이템 이름 표시
            itemMetaText.text = $"{item.Type} · {item.Grade} · MaxStack {item.MaxStack}"; // 선택 아이템 유형 및 등급 표시
            itemDescriptionText.text = string.IsNullOrWhiteSpace(item.Description) ? "설명이 없습니다." : item.Description; // 선택 아이템 설명 표시
            itemCountText.text = $"보유 수량  {count}"; // 선택 아이템 수량 표시
            illustrationImage.sprite = item.Icon; // 선택 아이템 일러스트 연결
            illustrationImage.enabled = item.Icon != null; // 실제 일러스트 존재 시 이미지 활성화
            illustrationPlaceholderText.gameObject.SetActive(item.Icon == null); // 일러스트 누락 시 임시 텍스트 활성화
            illustrationPlaceholderText.text = item.Icon == null ? $"{item.DisplayName}\n{item.Type}" : string.Empty; // 임시 일러스트 문구 표시
            useButton.interactable = item.Type == ItemType.Consumable && count > 0; // Consumable 아이템 사용 버튼 활성화
        }

        private void ShowEmptyDetails() // 빈 아이템 상세 표시
        {
            itemNameText.text = "아이템을 선택하세요"; // 빈 상세 이름 표시
            itemMetaText.text = "-"; // 빈 상세 메타 표시
            itemDescriptionText.text = "왼쪽 가방 칸에서 아이템을 선택하면 설명이 표시됩니다."; // 빈 상세 설명 표시
            itemCountText.text = "보유 수량  0"; // 빈 상세 수량 표시
            illustrationImage.sprite = null; // 빈 상세 일러스트 제거
            illustrationImage.enabled = false; // 빈 상세 일러스트 이미지 비활성화
            illustrationPlaceholderText.gameObject.SetActive(true); // 빈 상세 임시 일러스트 활성화
            illustrationPlaceholderText.text = "아이템을 선택하세요"; // 빈 상세 임시 일러스트 문구 표시
            useButton.interactable = false; // 빈 상세 사용 버튼 비활성화
        }

        private void UseSelectedItem() // 선택 소비 아이템 사용
        {
            if (string.IsNullOrWhiteSpace(selectedItemId)) // 선택 아이템 존재 확인
            {
                SetStatus("사용할 아이템을 선택하세요."); // 선택 아이템 누락 안내 표시
                return; // 아이템 사용 중단
            }

            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData, out string error)) // 아이템 사용 컨텍스트 조회
            {
                SetStatus(error); // 아이템 사용 컨텍스트 오류 표시
                return; // 아이템 사용 중단
            }

            if (RuneCatalog.IsRuneBox(selectedItemId)) // 룬 상자 확인 (Day60 추가)
            {
                bool opened = RuneService.TryOpenBox(saveData, dataManager, selectedItemId, out RuneInstanceSaveData rune, out string boxMessage); // 상자 열기
                bool boxSaved = opened && saveManager.SaveCurrent(); // 성공 시 저장
                SetStatus(opened && !boxSaved ? $"{boxMessage} (저장 실패)" : opened ? $"{boxMessage} · 캐릭터 창 [룬] 탭에서 장착" : boxMessage); // 결과 안내
                Refresh(); // 가방 갱신
                return; // 처리 종료
            }

            if (!ItemUseService.TryUse(saveData, dataManager, selectedItemId, out int remainingCount, out error)) // 소비 아이템 사용 시도
            {
                SetStatus(error); // 아이템 사용 오류 표시
                return; // 아이템 사용 중단
            }

            if (!saveManager.SaveCurrent()) // 사용 후 즉시 저장 실행
            {
                SetStatus("아이템은 사용됐지만 저장에 실패했습니다."); // 저장 실패 상태 표시
                Refresh(); // 사용 후 화면 갱신
                return; // 아이템 사용 처리 종료
            }

            SetStatus($"아이템 사용 완료 · 남은 수량 {remainingCount}"); // 아이템 사용 완료 상태 표시
            Refresh(); // 사용 후 가방 화면 갱신
        }

        private void GrantDemoItems() // 가방 테스트 아이템 지급
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData, out string error)) // 테스트 아이템 지급 컨텍스트 조회
            {
                SetStatus(error); // 테스트 아이템 지급 오류 표시
                return; // 테스트 아이템 지급 중단
            }

            if (!EnsureDemoItem(saveData, dataManager, "IT_POTION_SMALL", DemoPotionTargetCount, out error)) // 테스트 물약 목표 수량 지급
            {
                SetStatus(error); // 테스트 물약 지급 오류 표시
                return; // 테스트 아이템 지급 중단
            }

            if (!EnsureDemoItem(saveData, dataManager, "IT_MATERIAL_001", DemoMaterialTargetCount, out error)) // 테스트 재료 목표 수량 지급
            {
                SetStatus(error); // 테스트 재료 지급 오류 표시
                return; // 테스트 아이템 지급 중단
            }

            if (!EnsureDemoItem(saveData, dataManager, "IT_QUEST_KEY_001", DemoQuestTargetCount, out error)) // 테스트 퀘스트 아이템 목표 수량 지급
            {
                SetStatus(error); // 테스트 퀘스트 아이템 지급 오류 표시
                return; // 테스트 아이템 지급 중단
            }

            if (!saveManager.SaveCurrent()) // 테스트 아이템 지급 상태 저장
            {
                SetStatus("테스트 아이템 지급 후 저장에 실패했습니다."); // 테스트 아이템 저장 실패 표시
                Refresh(); // 테스트 아이템 지급 화면 갱신
                return; // 테스트 아이템 지급 처리 종료
            }

            SetStatus("테스트 아이템 지급 완료"); // 테스트 아이템 지급 완료 표시
            Refresh(); // 테스트 아이템 지급 후 화면 갱신
        }

        private static bool EnsureDemoItem(SaveData saveData, DataManager dataManager, string itemId, int targetCount, out string error) // 테스트 아이템 목표 수량 보장
        {
            error = string.Empty; // 오류 문구 초기화
            int currentCount = ItemInventoryService.GetCount(saveData, itemId); // 현재 테스트 아이템 수량 조회

            if (currentCount >= targetCount) // 목표 수량 도달 여부 확인
            {
                return true; // 기존 보유 수량 유지
            }

            int addCount = targetCount - currentCount; // 추가 지급 수량 계산
            return ItemInventoryService.TryAdd(saveData, dataManager, itemId, addCount, out error); // 부족 수량만 추가 지급
        }

        private void ReturnToLobby() // 로비 화면 복귀
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 존재 확인
            {
                SetStatus("SceneLoader를 찾을 수 없습니다."); // 씬 로더 누락 안내 표시
                return; // 로비 복귀 중단
            }

            GameManager.Instance.Scenes.LoadScene(GameScenes.Lobby); // 로비 씬 전환 실행
        }

        private static bool TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData, out string error) // 가방 실행 컨텍스트 조회
        {
            dataManager = null; // 데이터 관리자 결과 초기화
            saveManager = null; // 저장 관리자 결과 초기화
            saveData = null; // 저장 데이터 결과 초기화
            error = string.Empty; // 오류 문구 초기화

            if (GameManager.Instance == null) // 전역 게임 관리자 존재 확인
            {
                error = "Bootstrap 씬부터 실행해 주세요."; // 게임 관리자 누락 안내 설정
                return false; // 가방 컨텍스트 조회 실패
            }

            dataManager = GameManager.Instance.Data; // 전역 데이터 관리자 조회
            saveManager = GameManager.Instance.Save; // 전역 저장 관리자 조회
            saveData = saveManager == null ? null : saveManager.CurrentSave; // 현재 저장 데이터 조회

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 초기화 확인
            {
                error = "DataManager가 초기화되지 않았습니다."; // 데이터 관리자 오류 설정
                return false; // 가방 컨텍스트 조회 실패
            }

            if (saveManager == null || saveData == null) // 현재 저장 데이터 존재 확인
            {
                error = "새 게임을 생성하거나 저장 데이터를 불러와 주세요."; // 저장 데이터 누락 안내 설정
                return false; // 가방 컨텍스트 조회 실패
            }

            saveData.EnsureDefaults(); // 일반 아이템 인벤토리 기본값 복원
            return true; // 가방 컨텍스트 조회 성공
        }

        private void SetStatus(string message) // 가방 상태 문구 설정
        {
            if (statusText == null) // 상태 텍스트 존재 확인
            {
                return; // 상태 문구 설정 중단
            }

            statusText.text = message ?? string.Empty; // 가방 상태 문구 적용
        }

        private static Image CreateImage(Transform parent, string name, Color color) => RuntimeUiKit.CreateImage(parent, name, color); // 공통 UI 이미지 생성 (RuntimeUiKit 위임, 최적화 정리)

        private static Text CreateText(Transform parent, string name, string value, int fontSize, FontStyle fontStyle, Color color) // 공통 UI 텍스트 생성 (RuntimeUiKit 위임)
        {
            return RuntimeUiKit.CreateText(parent, name, value, fontSize, color, fontStyle).AlignByGeometry().BestFit(10); // 모양 정렬·자동 크기 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color) // 공통 UI 버튼 생성 (RuntimeUiKit 위임)
        {
            Button button = RuntimeUiKit.CreateButton(parent, name, color); // 버튼 생성
            AddOutline(button.gameObject); // 버튼 외곽선 적용
            Text text = CreateText(button.transform, "Label", label, 18, FontStyle.Normal, new Color(0.08f, 0.09f, 0.11f, 1f)); // 버튼 라벨 생성
            Stretch(text.rectTransform, 5f); // 라벨 확장
            return button; // 버튼 반환
        }

        private static void AddOutline(GameObject target) // 공통 UI 외곽선 추가
        {
            Outline outline = target.GetComponent<Outline>(); // 기존 UI 외곽선 조회

            if (outline == null) // 기존 UI 외곽선 존재 확인
            {
                outline = target.AddComponent<Outline>(); // UI 외곽선 컴포넌트 추가
            }

            outline.effectColor = new Color(0.18f, 0.18f, 0.18f, 0.65f); // UI 외곽선 색상 적용
            outline.effectDistance = new Vector2(1f, -1f); // UI 외곽선 두께 적용
        }

        private static void Stretch(RectTransform rect) => RuntimeUiKit.Stretch(rect); // RectTransform 전체 확장 (RuntimeUiKit 위임, 최적화 정리)

        private static void Stretch(RectTransform rect, float padding) => RuntimeUiKit.Stretch(rect, padding); // RectTransform 내부 여백 확장 (RuntimeUiKit 위임, 최적화 정리)

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) => RuntimeUiKit.SetRect(rect, min, max); // RectTransform 앵커 배치 (RuntimeUiKit 위임, 최적화 정리)
    }
}
