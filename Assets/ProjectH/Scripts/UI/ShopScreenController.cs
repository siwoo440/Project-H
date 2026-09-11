using ProjectH.Core; // 게임 관리자 및 씬 이름 기능
using ProjectH.Data; // 상점 및 아이템 데이터 기능
using ProjectH.SaveSystem; // 저장 및 Gold 기능
using ProjectH.Shop; // 상점 거래 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 상점 화면 중복 방지
    public sealed class ShopScreenController : MonoBehaviour // Day40 기본 상점 화면 컨트롤러
    {
        private const string ShopResourcePath = "Shops/SHOP_LOBBY"; // 기본 상점 Resources 경로
        private const int MaxTradeQuantity = 99; // UI 최대 거래 수량
        private ShopData shop; // 현재 상점 데이터
        private ShopProductEntry selectedProduct; // 현재 선택 상품
        private int selectedQuantity = 1; // 현재 거래 수량
        private RectTransform productListRoot; // 상품 버튼 목록 루트
        private Text goldText; // 현재 Gold 표시
        private Text shopNameText; // 상점 이름 표시
        private Text productNameText; // 선택 상품 이름 표시
        private Text productDescriptionText; // 선택 상품 설명 표시
        private Text productPriceText; // 선택 상품 가격 표시
        private Text ownedCountText; // 선택 상품 보유량 표시
        private Text quantityText; // 거래 수량 표시
        private Text statusText; // 거래 결과 표시
        private Button buyButton; // 구매 버튼
        private Button sellButton; // 판매 버튼
        private Button minusButton; // 수량 감소 버튼
        private Button plusButton; // 수량 증가 버튼

        private void Start() // 상점 화면 시작
        {
            EnsureEventSystem(); // UI 입력 시스템 보장
            shop = Resources.Load<ShopData>(ShopResourcePath); // 기본 상점 데이터 로드
            BuildUi(); // Runtime 상점 UI 생성
            BuildProductButtons(); // 상점 상품 버튼 생성
            SelectFirstProduct(); // 첫 상품 기본 선택
            RefreshView(); // 화면 데이터 갱신
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
            Image background = CreateImage(canvas.transform, "Background", new Color(0.93f, 0.93f, 0.93f, 1f)); // 전체 배경 생성
            background.raycastTarget = false; // 배경 입력 비활성화
            Stretch(background.rectTransform); // 전체 배경 확장
            BuildHeader(background.transform); // 상단 영역 구성
            BuildProductPanel(background.transform); // 상품 목록 구성
            BuildDetailPanel(background.transform); // 상품 상세 구성
        }

        private void BuildHeader(Transform parent) // 상점 상단 영역 구성
        {
            Button backButton = CreateButton(parent, "BackButton", "◀  로비", new Color(0.78f, 0.87f, 0.94f, 1f)); // 로비 복귀 버튼 생성
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.025f, 0.92f), new Vector2(0.16f, 0.975f)); // 로비 복귀 버튼 배치
            backButton.onClick.AddListener(ReturnToLobby); // 로비 복귀 이벤트 연결
            shopNameText = CreateText(parent, "ShopTitle", "상점", 32, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 상점 제목 생성
            SetRect(shopNameText.rectTransform, new Vector2(0.34f, 0.92f), new Vector2(0.66f, 0.975f)); // 상점 제목 배치
            goldText = CreateText(parent, "GoldText", "G 0", 26, FontStyle.Bold, new Color(0.43f, 0.30f, 0.04f, 1f)); // Gold 표시 생성
            goldText.alignment = TextAnchor.MiddleRight; // Gold 오른쪽 정렬
            SetRect(goldText.rectTransform, new Vector2(0.72f, 0.92f), new Vector2(0.975f, 0.975f)); // Gold 표시 배치
        }

        private void BuildProductPanel(Transform parent) // 좌측 상품 목록 구성
        {
            Image panel = CreateImage(parent, "ProductPanel", new Color(0.97f, 0.97f, 0.97f, 1f)); // 상품 목록 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.025f, 0.07f), new Vector2(0.48f, 0.90f)); // 상품 목록 패널 배치
            AddOutline(panel.gameObject); // 상품 패널 외곽선 추가
            Text label = CreateText(panel.transform, "ProductLabel", "판매 상품", 24, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 상품 목록 제목 생성
            label.alignment = TextAnchor.MiddleLeft; // 상품 목록 제목 왼쪽 정렬
            SetRect(label.rectTransform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f)); // 상품 목록 제목 배치
            GameObject listObject = new GameObject("ProductList", typeof(RectTransform)); // 상품 목록 루트 생성
            listObject.transform.SetParent(panel.transform, false); // 상품 패널 하위 연결
            productListRoot = listObject.GetComponent<RectTransform>(); // 상품 목록 RectTransform 조회
            SetRect(productListRoot, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.89f)); // 상품 목록 영역 배치
        }

        private void BuildDetailPanel(Transform parent) // 우측 상품 상세 구성
        {
            Image panel = CreateImage(parent, "DetailPanel", new Color(0.97f, 0.97f, 0.97f, 1f)); // 상품 상세 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.50f, 0.07f), new Vector2(0.975f, 0.90f)); // 상품 상세 패널 배치
            AddOutline(panel.gameObject); // 상세 패널 외곽선 추가
            productNameText = CreateText(panel.transform, "ProductName", "상품을 선택하세요.", 30, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 상품 이름 생성
            productNameText.alignment = TextAnchor.MiddleLeft; // 상품 이름 왼쪽 정렬
            SetRect(productNameText.rectTransform, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.91f)); // 상품 이름 배치
            productDescriptionText = CreateText(panel.transform, "Description", "", 20, FontStyle.Normal, new Color(0.20f, 0.23f, 0.27f, 1f)); // 상품 설명 생성
            productDescriptionText.alignment = TextAnchor.UpperLeft; // 상품 설명 상단 왼쪽 정렬
            productDescriptionText.horizontalOverflow = HorizontalWrapMode.Wrap; // 상품 설명 자동 줄바꿈 설정
            productDescriptionText.verticalOverflow = VerticalWrapMode.Overflow; // 상품 설명 세로 확장 설정
            SetRect(productDescriptionText.rectTransform, new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.79f)); // 상품 설명 배치
            productPriceText = CreateText(panel.transform, "Price", "구매 - / 판매 -", 23, FontStyle.Bold, new Color(0.23f, 0.27f, 0.32f, 1f)); // 상품 가격 생성
            productPriceText.alignment = TextAnchor.MiddleLeft; // 가격 왼쪽 정렬
            SetRect(productPriceText.rectTransform, new Vector2(0.06f, 0.49f), new Vector2(0.94f, 0.57f)); // 가격 배치
            ownedCountText = CreateText(panel.transform, "OwnedCount", "보유 0", 21, FontStyle.Normal, new Color(0.23f, 0.27f, 0.32f, 1f)); // 보유량 생성
            ownedCountText.alignment = TextAnchor.MiddleLeft; // 보유량 왼쪽 정렬
            SetRect(ownedCountText.rectTransform, new Vector2(0.06f, 0.41f), new Vector2(0.94f, 0.49f)); // 보유량 배치
            minusButton = CreateButton(panel.transform, "MinusButton", "-", new Color(0.84f, 0.88f, 0.92f, 1f)); // 수량 감소 버튼 생성
            SetRect(minusButton.GetComponent<RectTransform>(), new Vector2(0.10f, 0.30f), new Vector2(0.24f, 0.39f)); // 감소 버튼 배치
            minusButton.onClick.AddListener(DecreaseQuantity); // 감소 이벤트 연결
            quantityText = CreateText(panel.transform, "Quantity", "1", 26, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 거래 수량 생성
            SetRect(quantityText.rectTransform, new Vector2(0.26f, 0.30f), new Vector2(0.48f, 0.39f)); // 거래 수량 배치
            plusButton = CreateButton(panel.transform, "PlusButton", "+", new Color(0.84f, 0.88f, 0.92f, 1f)); // 수량 증가 버튼 생성
            SetRect(plusButton.GetComponent<RectTransform>(), new Vector2(0.50f, 0.30f), new Vector2(0.64f, 0.39f)); // 증가 버튼 배치
            plusButton.onClick.AddListener(IncreaseQuantity); // 증가 이벤트 연결
            buyButton = CreateButton(panel.transform, "BuyButton", "구매", new Color(0.76f, 0.88f, 0.78f, 1f)); // 구매 버튼 생성
            SetRect(buyButton.GetComponent<RectTransform>(), new Vector2(0.10f, 0.17f), new Vector2(0.44f, 0.27f)); // 구매 버튼 배치
            buyButton.onClick.AddListener(BuySelected); // 구매 이벤트 연결
            sellButton = CreateButton(panel.transform, "SellButton", "판매", new Color(0.91f, 0.84f, 0.72f, 1f)); // 판매 버튼 생성
            SetRect(sellButton.GetComponent<RectTransform>(), new Vector2(0.56f, 0.17f), new Vector2(0.90f, 0.27f)); // 판매 버튼 배치
            sellButton.onClick.AddListener(SellSelected); // 판매 이벤트 연결
            statusText = CreateText(panel.transform, "Status", "상품을 선택하세요.", 18, FontStyle.Normal, new Color(0.25f, 0.28f, 0.32f, 1f)); // 거래 상태 텍스트 생성
            statusText.alignment = TextAnchor.MiddleLeft; // 거래 상태 왼쪽 정렬
            SetRect(statusText.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.14f)); // 거래 상태 배치
        }

        private void BuildProductButtons() // 상품 선택 버튼 생성
        {
            if (productListRoot == null) // 상품 목록 루트 확인
            {
                return; // 버튼 생성 중단
            }

            if (shop == null || shop.Products == null || shop.Products.Count == 0) // 상점 상품 존재 확인
            {
                statusText.text = "SHOP_LOBBY 데이터를 찾을 수 없습니다."; // 상점 데이터 누락 표시
                return; // 버튼 생성 중단
            }

            DataManager dataManager = GameManager.Instance == null ? null : GameManager.Instance.Data; // 데이터 관리자 조회
            int productCount = shop.Products.Count; // 전체 상품 수 (Day57 선물 추가로 6개 제한 해제)
            int columnCount = productCount > 6 ? 2 : 1; // 6개 초과 시 2열 배치 (Day57 추가)
            int rowCount = Mathf.CeilToInt(productCount / (float)columnCount); // 행 수 계산
            float rowStep = Mathf.Min(0.155f, 0.96f / rowCount); // 행 간격 계산 (많아지면 촘촘하게)
            float columnWidth = 0.96f / columnCount; // 열 너비 계산

            for (int index = 0; index < productCount; index++) // 상품 목록 순회
            {
                ShopProductEntry product = shop.Products[index]; // 현재 상품 조회

                if (product == null) // 상품 데이터 확인
                {
                    continue; // null 상품 제외
                }

                ItemData item = dataManager == null ? null : dataManager.GetItem(product.ItemId); // 상품 아이템 조회
                string itemName = item == null || string.IsNullOrWhiteSpace(item.DisplayName) ? product.ItemId : item.DisplayName; // 상품 표시 이름 결정
                string sellText = product.CanSell ? product.SellPrice + "G" : "판매 불가"; // 판매 가격 문구 결정
                Button button = CreateButton(productListRoot, "Product_" + product.ProductId, itemName + "\n구매 " + product.BuyPrice + "G / " + sellText, new Color(0.88f, 0.91f, 0.94f, 1f)); // 상품 선택 버튼 생성
                float top = 0.98f - ((index / columnCount) * rowStep); // 상품 버튼 상단 위치 계산 (행 기준)
                float left = 0.02f + ((index % columnCount) * columnWidth); // 상품 버튼 왼쪽 위치 계산 (열 기준)
                SetRect(button.GetComponent<RectTransform>(), new Vector2(left, top - (rowStep * 0.84f)), new Vector2(left + columnWidth - 0.01f, top)); // 상품 버튼 배치
                ShopProductEntry capturedProduct = product; // 클릭 이벤트용 상품 보존
                button.onClick.AddListener(() => SelectProduct(capturedProduct)); // 상품 선택 이벤트 연결
            }
        }

        private void SelectFirstProduct() // 첫 유효 상품 선택
        {
            if (shop == null || shop.Products == null) // 상점 상품 목록 확인
            {
                return; // 선택 중단
            }

            for (int index = 0; index < shop.Products.Count; index++) // 상품 목록 순회
            {
                if (shop.Products[index] != null) // 유효 상품 확인
                {
                    SelectProduct(shop.Products[index]); // 첫 상품 선택
                    return; // 첫 상품 선택 완료
                }
            }
        }

        private void SelectProduct(ShopProductEntry product) // 현재 상품 선택
        {
            selectedProduct = product; // 선택 상품 저장
            selectedQuantity = 1; // 거래 수량 초기화
            statusText.text = product == null ? "상품을 선택하세요." : "거래 수량을 정한 뒤 구매 또는 판매하세요."; // 선택 상태 표시
            RefreshView(); // 선택 상품 화면 반영
        }

        private void DecreaseQuantity() // 거래 수량 감소
        {
            selectedQuantity = Mathf.Max(1, selectedQuantity - 1); // 최소 거래 수량 보정
            RefreshView(); // 수량 화면 반영
        }

        private void IncreaseQuantity() // 거래 수량 증가
        {
            selectedQuantity = Mathf.Min(MaxTradeQuantity, selectedQuantity + 1); // 최대 거래 수량 보정
            RefreshView(); // 수량 화면 반영
        }

        private void BuySelected() // 선택 상품 구매
        {
            if (!TryGetRuntimeData(out SaveData saveData, out DataManager dataManager)) // 거래 Runtime 데이터 확인
            {
                return; // 구매 중단
            }

            if (selectedProduct == null) // 선택 상품 확인
            {
                statusText.text = "구매할 상품을 선택하세요."; // 상품 미선택 표시
                return; // 구매 중단
            }

            if (!ShopTransactionService.TryPurchase(saveData, dataManager, shop, selectedProduct.ProductId, selectedQuantity, out string error)) // 구매 거래 실행
            {
                statusText.text = "구매 실패 · " + error; // 구매 실패 표시
                RefreshView(); // 현재 상태 갱신
                return; // 구매 처리 종료
            }

            bool saved = GameManager.Instance.Save.SaveCurrent(); // 구매 결과 즉시 저장
            statusText.text = saved ? "구매 완료 · 저장 완료" : "구매 완료 · 저장 실패"; // 구매 결과 표시
            RefreshView(); // 구매 후 화면 갱신
        }

        private void SellSelected() // 선택 상품 판매
        {
            if (!TryGetRuntimeData(out SaveData saveData, out DataManager dataManager)) // 거래 Runtime 데이터 확인
            {
                return; // 판매 중단
            }

            if (selectedProduct == null) // 선택 상품 확인
            {
                statusText.text = "판매할 상품을 선택하세요."; // 상품 미선택 표시
                return; // 판매 중단
            }

            if (!ShopTransactionService.TrySell(saveData, dataManager, shop, selectedProduct.ProductId, selectedQuantity, out string error)) // 판매 거래 실행
            {
                statusText.text = "판매 실패 · " + error; // 판매 실패 표시
                RefreshView(); // 현재 상태 갱신
                return; // 판매 처리 종료
            }

            bool saved = GameManager.Instance.Save.SaveCurrent(); // 판매 결과 즉시 저장
            statusText.text = saved ? "판매 완료 · 저장 완료" : "판매 완료 · 저장 실패"; // 판매 결과 표시
            RefreshView(); // 판매 후 화면 갱신
        }

        private bool TryGetRuntimeData(out SaveData saveData, out DataManager dataManager) // 상점 거래 Runtime 데이터 조회
        {
            saveData = null; // 저장 데이터 초기화
            dataManager = null; // 데이터 관리자 초기화

            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 게임 및 저장 관리자 확인
            {
                statusText.text = "GameManager 또는 SaveManager를 찾을 수 없습니다."; // 관리자 누락 표시
                return false; // 데이터 조회 실패
            }

            saveData = GameManager.Instance.Save.CurrentSave; // 현재 저장 데이터 조회
            dataManager = GameManager.Instance.Data; // 데이터 관리자 조회

            if (saveData == null || dataManager == null || !dataManager.IsInitialized) // 거래 데이터 유효성 확인
            {
                statusText.text = "상점 거래 데이터를 준비할 수 없습니다."; // 데이터 준비 실패 표시
                return false; // 데이터 조회 실패
            }

            return true; // 데이터 조회 성공
        }

        private void RefreshView() // 상점 화면 데이터 갱신
        {
            SaveData saveData = GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave; // 현재 저장 데이터 조회
            DataManager dataManager = GameManager.Instance == null ? null : GameManager.Instance.Data; // 현재 데이터 관리자 조회
            goldText.text = "G " + GoldCurrencyService.GetGold(saveData); // 현재 Gold 표시
            shopNameText.text = shop == null || string.IsNullOrWhiteSpace(shop.DisplayName) ? "상점" : shop.DisplayName; // 상점 이름 표시
            quantityText.text = selectedQuantity.ToString(); // 거래 수량 표시
            bool hasSelection = selectedProduct != null; // 상품 선택 여부 계산
            minusButton.interactable = hasSelection && selectedQuantity > 1; // 수량 감소 버튼 상태 적용
            plusButton.interactable = hasSelection && selectedQuantity < MaxTradeQuantity; // 수량 증가 버튼 상태 적용
            buyButton.interactable = hasSelection; // 구매 버튼 상태 적용
            sellButton.interactable = hasSelection && selectedProduct.CanSell; // 판매 버튼 상태 적용

            if (!hasSelection) // 선택 상품 없음 확인
            {
                productNameText.text = "상품을 선택하세요."; // 미선택 상품 이름 표시
                productDescriptionText.text = string.Empty; // 미선택 설명 초기화
                productPriceText.text = "구매 - / 판매 -"; // 미선택 가격 표시
                ownedCountText.text = "보유 -"; // 미선택 보유량 표시
                return; // 상세 갱신 종료
            }

            ItemData item = dataManager == null ? null : dataManager.GetItem(selectedProduct.ItemId); // 선택 상품 아이템 조회
            string itemName = item == null || string.IsNullOrWhiteSpace(item.DisplayName) ? selectedProduct.ItemId : item.DisplayName; // 선택 상품 이름 결정
            productNameText.text = itemName; // 상품 이름 표시
            productDescriptionText.text = item == null ? "ItemData를 찾을 수 없습니다." : item.Description; // 상품 설명 표시
            string sellText = selectedProduct.CanSell ? selectedProduct.SellPrice + "G" : "판매 불가"; // 판매 가격 문구 결정
            productPriceText.text = "구매 " + selectedProduct.BuyPrice + "G  /  판매 " + sellText; // 상품 가격 표시

            if (saveData == null || item == null) // 보유량 조회 가능 여부 확인
            {
                ownedCountText.text = "보유 -"; // 보유량 조회 실패 표시
                return; // 보유량 갱신 종료
            }

            int ownedCount = item.Type == ItemType.Equipment ? saveData.GetEquipmentCount(item.Id) : saveData.GetItemCount(item.Id); // 아이템 유형별 보유량 조회
            ownedCountText.text = "보유 " + ownedCount; // 현재 보유량 표시
        }

        private void ReturnToLobby() // Lobby 씬 복귀
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                statusText.text = "SceneLoader를 찾을 수 없습니다."; // 씬 로더 누락 표시
                return; // Lobby 복귀 중단
            }

            GameManager.Instance.Scenes.LoadScene(GameScenes.Lobby); // Lobby 씬 로드
        }

        private static Image CreateImage(Transform parent, string objectName, Color color) => RuntimeUiKit.CreateImage(parent, objectName, color); // 기본 Image 생성 (RuntimeUiKit 위임, 최적화 정리)

        private static Text CreateText(Transform parent, string objectName, string value, int fontSize, FontStyle fontStyle, Color color) // 공통 UI 텍스트 생성 (RuntimeUiKit 위임)
        {
            return RuntimeUiKit.CreateText(parent, objectName, value, fontSize, color, fontStyle); // 기본 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string objectName, string labelText, Color color) // 공통 UI 버튼 생성 (RuntimeUiKit 위임)
        {
            Button button = RuntimeUiKit.CreateButton(parent, objectName, color); // 버튼 생성
            AddOutline(button.gameObject); // 버튼 외곽선 적용
            Text label = CreateText(button.transform, "Label", labelText, 20, FontStyle.Bold, new Color(0.10f, 0.13f, 0.17f, 1f)).BestFit(12); // 자동 크기 버튼 라벨 생성
            Stretch(label.rectTransform, 8f); // 라벨 확장
            return button; // 버튼 반환
        }

        private static void AddOutline(GameObject target) // UI 외곽선 추가
        {
            Outline outline = target.GetComponent<Outline>(); // 기존 Outline 조회

            if (outline == null) // Outline 존재 확인
            {
                outline = target.AddComponent<Outline>(); // Outline 추가
            }

            outline.effectColor = new Color(0.26f, 0.30f, 0.35f, 0.45f); // 외곽선 색상 적용
            outline.effectDistance = new Vector2(1f, -1f); // 외곽선 두께 적용
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) => RuntimeUiKit.SetRect(rect, anchorMin, anchorMax); // UI 앵커 영역 설정 (RuntimeUiKit 위임, 최적화 정리)

        private static void Stretch(RectTransform rect, float padding = 0f) => RuntimeUiKit.Stretch(rect, padding); // RectTransform 전체 확장 (RuntimeUiKit 위임, 최적화 정리)
    }
}
