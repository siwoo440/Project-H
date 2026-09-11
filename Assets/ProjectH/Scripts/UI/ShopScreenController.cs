using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.Data; // 상점 및 아이템 데이터 기능
using ProjectH.SaveSystem; // 저장 및 Gold 기능
using ProjectH.Shop; // 상점 거래·재고 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 상점 화면 중복 방지
    public sealed class ShopScreenController : MonoBehaviour // 상점 화면 컨트롤러 (Day61 목업 개편 — 왼쪽 상점 주인·대사, 오른쪽 탭·카드 격자·오늘의 상품·재고·품절·리롤)
    {
        private const string ShopResourcePath = "Shops/SHOP_LOBBY"; // 기본 상점 Resources 경로
        private const int GridColumns = 4; // 카드 열 수
        private const int GridRows = 3; // 카드 행 수
        private const int CardsPerPage = GridColumns * GridRows; // 한 페이지 카드 수
        private static readonly Color PanelColor = new Color(0.07f, 0.06f, 0.08f, 0.82f); // 어두운 패널 색
        private static readonly Color CardColor = new Color(0.16f, 0.14f, 0.17f, 0.96f); // 카드 색
        private static readonly Color GoldColor = new Color(1f, 0.82f, 0.36f, 1f); // 골드 글자 색
        private static readonly Color TabOnColor = new Color(0.86f, 0.62f, 0.30f, 1f); // 선택 탭 색
        private static readonly Color TabOffColor = new Color(0.24f, 0.21f, 0.24f, 1f); // 일반 탭 색

        private enum ShopTab // 상점 탭
        {
            Permanent = 0, // 상시 상품
            Today = 1, // 오늘의 상품
            Sell = 2 // 판매
        }

        private sealed class ShopCardView // 상품 카드 한 칸 참조
        {
            public GameObject Root; // 카드 루트
            public Image Icon; // 아이템 아이콘
            public Text Name; // 이름
            public Text Price; // 가격
            public Text Stock; // 재고·보유
            public Button Action; // 구매·판매 버튼
            public Text ActionLabel; // 버튼 글자
            public GameObject SoldOut; // 품절 덮개
            public ShopProductEntry Product; // 표시 중인 상품
        }

        private readonly List<ShopCardView> cards = new List<ShopCardView>(); // 카드 12칸
        private readonly Button[] tabButtons = new Button[3]; // 탭 버튼
        private ShopData shop; // 현재 상점 데이터
        private ShopTab currentTab = ShopTab.Permanent; // 현재 탭
        private int page; // 현재 페이지
        private int lineSeed; // 대사 변주 값
        private Text goldText; // 골드 표시
        private Text dialogueText; // 상점 주인 대사
        private Text dayText; // 일차·갱신 안내
        private Text pageText; // 페이지 표시
        private Button prevPageButton; // 이전 페이지
        private Button nextPageButton; // 다음 페이지
        private Button rerollButton; // 오늘의 상품 새로고침
        private Text rerollLabel; // 새로고침 버튼 글자
        private Text emptyText; // 빈 목록 안내

        private void Start() // 상점 화면 시작
        {
            EnsureEventSystem(); // UI 입력 시스템 보장
            shop = Resources.Load<ShopData>(ShopResourcePath); // 기본 상점 데이터 로드
            BuildUi(); // Runtime 상점 UI 생성
            SaveData saveData = GetSave(); // 현재 저장
            lineSeed = saveData == null ? 0 : saveData.CurrentDay; // 일차 기반 인사 변주
            Say(NpcLineKind.Greeting, shop == null ? "SHOP_LOBBY 데이터를 찾을 수 없습니다." : null); // 입장 인사
            SelectTab(ShopTab.Permanent); // 상시 상품 탭으로 시작
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
            eventSystemObject.transform.SetParent(transform, false); // 상점 화면 하위 연결
        }

        private void BuildUi() // 전체 상점 UI 구성
        {
            GameObject canvasObject = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Runtime Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 상점 화면 하위 Canvas 연결
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 100; // 상점 UI 정렬 순서 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 스케일 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            Image background = CreateImage(canvas.transform, "Background", Color.white); // 가게 배경
            background.sprite = DialogueArtFactory.GetBackground("SHOP"); // 상점 배경 (정식 배경 우선)
            background.raycastTarget = false; // 배경 입력 비활성화
            Stretch(background.rectTransform); // 전체 배경 확장
            Image shade = CreateImage(background.transform, "Shade", new Color(0f, 0f, 0f, 0.25f)); // 배경 어둡게
            shade.raycastTarget = false; // 입력 통과
            Stretch(shade.rectTransform); // 전체 확장
            BuildTopBar(background.transform); // 상단 바
            BuildShopkeeper(background.transform); // 왼쪽 상점 주인
            BuildProductPanel(background.transform); // 오른쪽 상품 패널
        }

        private void BuildTopBar(Transform parent) // 상단 바 : 뒤로·제목·골드 (대장간은 로비 하단 버튼으로 진입)
        {
            Image bar = CreateImage(parent, "TopBar", new Color(0.05f, 0.04f, 0.06f, 0.88f)); // 상단 띠
            SetRect(bar.rectTransform, new Vector2(0f, 0.915f), new Vector2(1f, 1f)); // 상단 배치
            Button backButton = CreateButton(bar.transform, "BackButton", "◀  로비", new Color(0.24f, 0.21f, 0.24f, 1f), Color.white); // 로비 복귀 버튼
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.01f, 0.14f), new Vector2(0.11f, 0.86f)); // 왼쪽 배치
            backButton.onClick.AddListener(() => LoadScene(GameScenes.Lobby)); // 로비 이동
            Text title = CreateText(bar.transform, "Title", shop == null || string.IsNullOrWhiteSpace(shop.DisplayName) ? "상점" : shop.DisplayName, 30, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft); // 상점 이름
            SetRect(title.rectTransform, new Vector2(0.125f, 0f), new Vector2(0.45f, 1f)); // 제목 배치
            goldText = CreateText(bar.transform, "Gold", "● 0 G", 26, GoldColor, FontStyle.Bold, TextAnchor.MiddleRight); // 골드 표시
            SetRect(goldText.rectTransform, new Vector2(0.70f, 0f), new Vector2(0.985f, 1f)); // 골드 오른쪽 배치
        }

        private void BuildShopkeeper(Transform parent) // 왼쪽 상점 주인 스탠딩 + 대사 상자
        {
            NpcProfile npc = NpcLineCatalog.Shopkeeper; // 상점 주인 프로필
            Image standing = CreateImage(parent, "Shopkeeper", Color.white); // 스탠딩
            standing.sprite = DialogueArtFactory.GetStanding(npc.Id, null, out bool placeholder); // 정식 스탠딩 우선 · 없으면 실루엣
            standing.color = placeholder ? DialogueArtFactory.GetCharacterTint(npc.Id) : Color.white; // 실루엣이면 NPC 색
            standing.preserveAspect = true; // 비율 유지
            standing.raycastTarget = false; // 입력 통과
            SetRect(standing.rectTransform, new Vector2(0.02f, 0.20f), new Vector2(0.38f, 0.91f)); // 왼쪽 배치
            Image box = CreateImage(parent, "DialogueBox", new Color(0.06f, 0.05f, 0.07f, 0.90f)); // 대사 상자
            SetRect(box.rectTransform, new Vector2(0.01f, 0.02f), new Vector2(0.40f, 0.24f)); // 하단 배치
            AddOutline(box.gameObject, new Color(0.86f, 0.62f, 0.30f, 0.8f)); // 금색 테두리
            Image plate = CreateImage(parent, "NamePlate", new Color(0.86f, 0.62f, 0.30f, 1f)); // 이름표
            SetRect(plate.rectTransform, new Vector2(0.02f, 0.215f), new Vector2(0.22f, 0.265f)); // 상자 위 왼쪽
            Text plateText = CreateText(plate.transform, "Name", NpcLineCatalog.FormatSpeaker(npc), 20, new Color(0.10f, 0.07f, 0.04f, 1f), FontStyle.Bold).BestFit(12); // 이름·소속
            Stretch(plateText.rectTransform, 4f); // 이름표 확장
            dialogueText = CreateText(box.transform, "Line", string.Empty, 21, Color.white, FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 대사
            SetRect(dialogueText.rectTransform, new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.80f)); // 대사 배치
        }

        private void BuildProductPanel(Transform parent) // 오른쪽 탭 + 카드 격자 + 페이지·새로고침
        {
            Image panel = CreateImage(parent, "ProductPanel", PanelColor); // 상품 패널
            SetRect(panel.rectTransform, new Vector2(0.41f, 0.02f), new Vector2(0.99f, 0.90f)); // 오른쪽 배치
            string[] tabNames = { "상시 상품", "오늘의 상품", "판매" }; // 탭 이름

            for (int index = 0; index < tabButtons.Length; index++) // 탭 생성
            {
                ShopTab tab = (ShopTab)index; // 탭 값
                tabButtons[index] = CreateButton(panel.transform, "Tab_" + tab, tabNames[index], TabOffColor, Color.white); // 탭 버튼
                SetRect(tabButtons[index].GetComponent<RectTransform>(), new Vector2(0.015f + (index * 0.165f), 0.915f), new Vector2(0.17f + (index * 0.165f), 0.985f)); // 왼쪽부터 배치
                tabButtons[index].onClick.AddListener(() => SelectTab(tab)); // 탭 선택
            }

            dayText = CreateText(panel.transform, "DayText", string.Empty, 17, new Color(0.85f, 0.82f, 0.78f, 1f), FontStyle.Normal, TextAnchor.MiddleRight).BestFit(11); // 일차 안내
            SetRect(dayText.rectTransform, new Vector2(0.52f, 0.915f), new Vector2(0.985f, 0.985f)); // 탭 오른쪽
            GameObject grid = new GameObject("CardGrid", typeof(RectTransform)); // 카드 격자 루트
            grid.transform.SetParent(panel.transform, false); // 패널 하위
            RectTransform gridRect = grid.GetComponent<RectTransform>(); // 격자 영역
            SetRect(gridRect, new Vector2(0.015f, 0.105f), new Vector2(0.985f, 0.90f)); // 격자 배치

            for (int index = 0; index < CardsPerPage; index++) // 카드 12칸 생성
            {
                cards.Add(BuildCard(gridRect, index)); // 카드 추가
            }

            emptyText = CreateText(gridRect, "Empty", string.Empty, 22, new Color(1f, 1f, 1f, 0.6f), FontStyle.Normal); // 빈 목록 안내
            Stretch(emptyText.rectTransform); // 격자 전체
            prevPageButton = CreateButton(panel.transform, "PrevPage", "◀", TabOffColor, Color.white); // 이전 페이지
            SetRect(prevPageButton.GetComponent<RectTransform>(), new Vector2(0.015f, 0.02f), new Vector2(0.075f, 0.085f)); // 왼쪽 아래
            prevPageButton.onClick.AddListener(() => ChangePage(-1)); // 이전
            pageText = CreateText(panel.transform, "Page", "1 / 1", 18, Color.white, FontStyle.Bold); // 페이지 표시
            SetRect(pageText.rectTransform, new Vector2(0.08f, 0.02f), new Vector2(0.16f, 0.085f)); // 가운데
            nextPageButton = CreateButton(panel.transform, "NextPage", "▶", TabOffColor, Color.white); // 다음 페이지
            SetRect(nextPageButton.GetComponent<RectTransform>(), new Vector2(0.165f, 0.02f), new Vector2(0.225f, 0.085f)); // 오른쪽
            nextPageButton.onClick.AddListener(() => ChangePage(1)); // 다음
            rerollButton = CreateButton(panel.transform, "Reroll", "새로고침", new Color(0.30f, 0.45f, 0.62f, 1f), Color.white); // 오늘의 상품 새로고침
            SetRect(rerollButton.GetComponent<RectTransform>(), new Vector2(0.66f, 0.02f), new Vector2(0.985f, 0.085f)); // 오른쪽 아래
            rerollLabel = rerollButton.GetComponentInChildren<Text>(); // 버튼 글자
            rerollButton.onClick.AddListener(Reroll); // 새로고침 실행
        }

        private ShopCardView BuildCard(RectTransform grid, int index) // 상품 카드 한 칸 생성
        {
            int column = index % GridColumns; // 열
            int row = index / GridColumns; // 행
            float width = 1f / GridColumns; // 칸 너비
            float height = 1f / GridRows; // 칸 높이
            ShopCardView card = new ShopCardView(); // 카드 참조
            Button body = CreateButton(grid, "Card_" + index, string.Empty, CardColor, Color.white); // 카드 본체 (누르면 설명)
            SetRect(body.GetComponent<RectTransform>(), new Vector2((column * width) + 0.006f, 1f - ((row + 1) * height) + 0.01f), new Vector2(((column + 1) * width) - 0.006f, 1f - (row * height) - 0.01f)); // 격자 배치
            AddOutline(body.gameObject, new Color(0.86f, 0.62f, 0.30f, 0.35f)); // 금색 얇은 테두리
            card.Root = body.gameObject; // 루트 저장
            body.onClick.AddListener(() => Describe(card.Product)); // 설명 대사
            card.Icon = ItemIconView.Create(body.transform, "Icon"); // 아이콘
            RectTransform iconRect = card.Icon.rectTransform; // 아이콘 영역
            iconRect.anchorMin = new Vector2(0.5f, 0.72f); // 가운데 위 기준점
            iconRect.anchorMax = new Vector2(0.5f, 0.72f); // 가운데 위 기준점
            iconRect.sizeDelta = new Vector2(76f, 76f); // 정사각 크기
            card.Name = CreateText(body.transform, "Name", string.Empty, 18, Color.white, FontStyle.Bold).BestFit(11); // 이름
            SetRect(card.Name.rectTransform, new Vector2(0.04f, 0.37f), new Vector2(0.96f, 0.50f)); // 이름 배치
            card.Price = CreateText(body.transform, "Price", string.Empty, 18, GoldColor, FontStyle.Bold, TextAnchor.MiddleLeft).BestFit(11); // 가격
            SetRect(card.Price.rectTransform, new Vector2(0.06f, 0.23f), new Vector2(0.56f, 0.36f)); // 가격 배치
            card.Stock = CreateText(body.transform, "Stock", string.Empty, 15, new Color(0.85f, 0.85f, 0.88f, 1f), FontStyle.Normal, TextAnchor.MiddleRight).BestFit(10); // 재고
            SetRect(card.Stock.rectTransform, new Vector2(0.50f, 0.23f), new Vector2(0.94f, 0.36f)); // 재고 배치
            card.Action = CreateButton(body.transform, "Action", "구매", TabOnColor, new Color(0.10f, 0.07f, 0.04f, 1f)); // 구매·판매 버튼
            SetRect(card.Action.GetComponent<RectTransform>(), new Vector2(0.10f, 0.04f), new Vector2(0.90f, 0.20f)); // 버튼 배치
            card.ActionLabel = card.Action.GetComponentInChildren<Text>(); // 버튼 글자
            card.Action.onClick.AddListener(() => Trade(card.Product)); // 거래 실행
            Image sold = CreateImage(body.transform, "SoldOut", new Color(0f, 0f, 0f, 0.62f)); // 품절 덮개
            sold.raycastTarget = false; // 설명 클릭은 통과
            Stretch(sold.rectTransform); // 카드 전체
            Text soldText = CreateText(sold.transform, "Label", "품 절", 30, new Color(1f, 0.45f, 0.40f, 1f), FontStyle.Bold).Outlined(new Color(0f, 0f, 0f, 0.8f), new Vector2(2f, -2f)); // 품절 글자
            Stretch(soldText.rectTransform); // 가운데
            soldText.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 18f); // 비스듬히
            card.SoldOut = sold.gameObject; // 덮개 저장
            return card; // 카드 반환
        }

        private void SelectTab(ShopTab tab) // 탭 선택
        {
            currentTab = tab; // 탭 저장
            page = 0; // 첫 페이지

            for (int index = 0; index < tabButtons.Length; index++) // 탭 색 갱신
            {
                tabButtons[index].GetComponent<Image>().color = index == (int)tab ? TabOnColor : TabOffColor; // 선택 탭 강조
            }

            RefreshView(); // 목록 갱신
        }

        private void ChangePage(int delta) // 페이지 이동
        {
            page += delta; // 페이지 변경
            RefreshView(); // 목록 갱신
        }

        private List<ShopProductEntry> GetCurrentProducts(SaveData saveData, DataManager dataManager) // 현재 탭 상품 목록
        {
            if (shop == null) return new List<ShopProductEntry>(); // 상점 없음
            if (currentTab == ShopTab.Permanent) return ShopRotationService.GetPermanentProducts(shop); // 상시 상품
            if (currentTab == ShopTab.Today) return ShopRotationService.GetTodayProducts(saveData, shop); // 오늘의 상품
            List<ShopProductEntry> sellable = new List<ShopProductEntry>(); // 판매 가능 목록
            HashSet<string> seen = new HashSet<string>(); // 중복 아이템 방지

            foreach (ShopProductEntry product in shop.Products) // 상품 순회
            {
                if (product == null || !product.CanSell || !seen.Add(product.ItemId)) continue; // 판매 불가·중복 제외
                if (saveData != null && saveData.GetItemCount(product.ItemId) > 0) sellable.Add(product); // 보유한 것만
            }

            return sellable; // 판매 목록 반환
        }

        private void RefreshView() // 화면 갱신
        {
            SaveData saveData = GetSave(); // 현재 저장
            DataManager dataManager = GetData(); // 데이터 관리자
            goldText.text = $"● {GoldCurrencyService.GetGold(saveData):N0} G"; // 골드 표시
            List<ShopProductEntry> products = GetCurrentProducts(saveData, dataManager); // 현재 탭 상품
            int pageCount = Mathf.Max(1, Mathf.CeilToInt(products.Count / (float)CardsPerPage)); // 페이지 수
            page = Mathf.Clamp(page, 0, pageCount - 1); // 페이지 범위 보정
            pageText.text = $"{page + 1} / {pageCount}"; // 페이지 표시
            prevPageButton.interactable = page > 0; // 이전 가능 여부
            nextPageButton.interactable = page < pageCount - 1; // 다음 가능 여부

            for (int index = 0; index < cards.Count; index++) // 카드 순회
            {
                int productIndex = (page * CardsPerPage) + index; // 상품 번호
                ApplyCard(cards[index], productIndex < products.Count ? products[productIndex] : null, saveData, dataManager); // 카드 적용
            }

            emptyText.text = products.Count > 0 ? string.Empty : currentTab == ShopTab.Sell ? "판매할 수 있는 아이템이 없습니다." : "진열된 상품이 없습니다."; // 빈 목록 안내
            RefreshDayAndReroll(saveData); // 일차·새로고침 갱신
        }

        private void ApplyCard(ShopCardView card, ShopProductEntry product, SaveData saveData, DataManager dataManager) // 카드 한 칸 갱신
        {
            card.Product = product; // 상품 저장
            card.Root.SetActive(product != null); // 빈 칸 숨김
            if (product == null) return; // 빈 칸 종료
            ItemData item = dataManager == null ? null : dataManager.GetItem(product.ItemId); // 아이템 원본
            ItemIconView.Apply(card.Icon, item, dataManager); // 아이콘
            card.Name.text = item == null ? product.ItemId : item.DisplayName; // 이름
            bool selling = currentTab == ShopTab.Sell; // 판매 탭 여부

            if (selling) // 판매 카드
            {
                card.Price.text = $"● {product.SellPrice:N0}"; // 판매가
                card.Stock.text = $"보유 {(saveData == null ? 0 : saveData.GetItemCount(product.ItemId))}"; // 보유 수
                card.ActionLabel.text = "판매"; // 버튼 글자
                card.Action.interactable = true; // 판매 가능
                card.SoldOut.SetActive(false); // 품절 없음
                return; // 종료
            }

            int remaining = ShopRotationService.GetRemaining(saveData, shop, product); // 남은 재고 (-1 무제한)
            bool soldOut = remaining == 0; // 품절 여부
            card.Price.text = $"● {product.BuyPrice:N0}"; // 구매가
            card.Stock.text = remaining < 0 ? "재고 ∞" : $"재고 {remaining}/{product.DailyLimit}"; // 재고 표시
            card.ActionLabel.text = soldOut ? "품절" : "구매"; // 버튼 글자
            card.Action.interactable = !soldOut && GoldCurrencyService.GetGold(saveData) >= product.BuyPrice; // 골드 부족·품절 시 비활성 (기획서 7.11)
            card.SoldOut.SetActive(soldOut); // 품절 덮개
        }

        private void RefreshDayAndReroll(SaveData saveData) // 일차 안내·새로고침 버튼
        {
            int day = saveData == null ? 1 : saveData.CurrentDay; // 현재 일차
            ShopStateSaveData state = ShopRotationService.EnsureToday(saveData, shop); // 오늘 상태
            int rerolls = state == null ? 0 : state.RerollCount; // 오늘 리롤 횟수
            int cost = ShopRotationService.GetRerollCost(rerolls); // 다음 비용
            bool todayTab = currentTab == ShopTab.Today; // 오늘의 상품 탭 여부
            dayText.text = todayTab ? $"DAY {day}  ·  재고와 오늘의 상품은 내일 갱신" : $"DAY {day}  ·  재고는 매일 갱신"; // 일차 안내
            rerollButton.gameObject.SetActive(todayTab); // 오늘의 상품 탭에서만 표시
            rerollLabel.text = rerolls >= ShopRotationService.MaxRerollsPerDay ? "새로고침 (오늘 끝)" : $"새로고침  ● {cost}G  ({ShopRotationService.MaxRerollsPerDay - rerolls}회 남음)"; // 버튼 글자
            rerollButton.interactable = state != null && rerolls < ShopRotationService.MaxRerollsPerDay && GoldCurrencyService.GetGold(saveData) >= cost; // 가능 여부
        }

        private void Describe(ShopProductEntry product) // 상품 설명 대사
        {
            ItemData item = product == null || GetData() == null ? null : GetData().GetItem(product.ItemId); // 아이템 원본
            if (item == null) return; // 설명 없음
            lineSeed++; // 변주
            dialogueText.text = $"{NpcLineCatalog.GetLine(NpcLineCatalog.Shopkeeper, NpcLineKind.Describe, lineSeed)} <b>{item.DisplayName}</b>.\n{item.Description}"; // 설명 표시
        }

        private void Trade(ShopProductEntry product) // 카드 버튼 : 구매 또는 판매 1개
        {
            SaveData saveData = GetSave(); // 현재 저장
            DataManager dataManager = GetData(); // 데이터 관리자

            if (product == null || saveData == null || dataManager == null || !dataManager.IsInitialized) // 데이터 확인
            {
                Say(NpcLineKind.NeedMaterial, "Bootstrap 씬부터 실행해 주세요."); // 안내
                return; // 중단
            }

            bool selling = currentTab == ShopTab.Sell; // 판매 여부
            bool success = selling ? ShopTransactionService.TrySell(saveData, dataManager, shop, product.ProductId, 1, out string error) : ShopRotationService.TryBuy(saveData, dataManager, shop, product.ProductId, 1, out error); // 거래 실행

            if (!success) // 실패
            {
                bool soldOut = !selling && ShopRotationService.GetRemaining(saveData, shop, product) == 0; // 품절 여부
                bool noGold = !selling && GoldCurrencyService.GetGold(saveData) < product.BuyPrice; // 골드 부족 여부
                Say(soldOut ? NpcLineKind.SoldOut : noGold ? NpcLineKind.NoGold : NpcLineKind.NeedMaterial, error); // 실패 대사
                RefreshView(); // 갱신
                return; // 종료
            }

            GameManager.Instance.Save.SaveCurrent(); // 즉시 저장
            ItemData item = dataManager.GetItem(product.ItemId); // 아이템 원본
            Say(selling ? NpcLineKind.Sell : NpcLineKind.Buy, $"{(item == null ? product.ItemId : item.DisplayName)} {(selling ? "판매" : "구매")} 완료"); // 성공 대사
            RefreshView(); // 갱신
        }

        private void Reroll() // 오늘의 상품 새로고침
        {
            SaveData saveData = GetSave(); // 현재 저장

            if (!ShopRotationService.TryReroll(saveData, shop, out string error)) // 리롤 실행
            {
                Say(NpcLineKind.NoGold, error); // 실패 대사
                RefreshView(); // 갱신
                return; // 종료
            }

            GameManager.Instance.Save.SaveCurrent(); // 즉시 저장
            Say(NpcLineKind.Reroll, null); // 새로고침 대사
            page = 0; // 첫 페이지
            RefreshView(); // 갱신
        }

        private void Say(NpcLineKind kind, string detail) // 상점 주인 대사 + 시스템 안내
        {
            lineSeed++; // 변주
            string line = NpcLineCatalog.GetLine(NpcLineCatalog.Shopkeeper, kind, lineSeed); // 대사
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
            Text label = CreateText(button.transform, "Label", labelText, 20, labelColor).BestFit(11); // 자동 크기 버튼 라벨 생성
            Stretch(label.rectTransform, 6f); // 라벨 확장
            return button; // 버튼 반환
        }

        private static void AddOutline(GameObject target, Color color) // UI 외곽선 추가
        {
            Outline outline = target.GetComponent<Outline>(); // 기존 Outline 조회

            if (outline == null) // Outline 존재 확인 (Unity 객체는 ?? 대신 명시 비교)
            {
                outline = target.AddComponent<Outline>(); // Outline 추가
            }

            outline.effectColor = color; // 외곽선 색상 적용
            outline.effectDistance = new Vector2(2f, -2f); // 외곽선 두께 적용
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) => RuntimeUiKit.SetRect(rect, anchorMin, anchorMax); // UI 앵커 영역 설정 (RuntimeUiKit 위임)

        private static void Stretch(RectTransform rect, float padding = 0f) => RuntimeUiKit.Stretch(rect, padding); // RectTransform 전체 확장 (RuntimeUiKit 위임)
    }
}
