using System.Collections.Generic; // 사전 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 프로젝트 데이터 기능
using ProjectH.SaveSystem; // 저장 및 지역 침식도 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 컨트롤러 방지
    public sealed class DungeonSelectScreenController : MonoBehaviour // 26일차 던전 선택 화면 컨트롤러
    {
        private static readonly Color BackgroundColor = new Color(0.035f, 0.055f, 0.10f, 1f); // 전체 배경 색상
        private static readonly Color HeaderColor = new Color(0.09f, 0.17f, 0.29f, 1f); // 상단 헤더 색상
        private static readonly Color PanelColor = new Color(0.92f, 0.90f, 0.84f, 0.99f); // 정보 패널 색상
        private static readonly Color CardColor = new Color(0.15f, 0.22f, 0.32f, 1f); // 기본 카드 색상
        private static readonly Color SelectedCardColor = new Color(0.23f, 0.33f, 0.46f, 1f); // 선택 카드 색상
        private static readonly Color NavyColor = new Color(0.07f, 0.13f, 0.23f, 1f); // 진한 텍스트 색상
        private static readonly Color GoldColor = new Color(0.94f, 0.72f, 0.22f, 1f); // 선택 강조 색상
        private static readonly Color DisabledColor = new Color(0.22f, 0.24f, 0.28f, 1f); // 비활성 카드 색상
        private readonly Dictionary<string, DungeonCardVisual> cardVisuals = new Dictionary<string, DungeonCardVisual>(); // 던전 카드 시각 사전
        private Text detailNameText; // 상세 던전 이름 텍스트
        private Text detailRegionText; // 상세 지역 텍스트
        private Text detailErosionText; // 상세 침식도 및 적 스탯 증가 텍스트 (Day44 추가 작업)
        private Text detailLevelText; // 상세 권장 레벨 텍스트
        private Text detailRewardText; // 상세 보상 텍스트
        private Text detailStatusText; // 상세 상태 텍스트
        private Button enterButton; // 전투 진입 버튼
        private Text enterButtonLabel; // 전투 진입 버튼 라벨
        private Text erosionDebugText; // 지역 침식도 디버그 텍스트 (Day44)

        public int RenderedCardCount => cardVisuals.Count; // 생성 카드 수 반환
        public bool IsEnterInteractable => enterButton != null && enterButton.interactable; // 전투 진입 버튼 상태 반환
        public string SelectedDungeonId => DungeonSelectionRuntimeState.SelectedDungeonId; // 현재 선택 던전 ID 반환

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void RegisterSceneLoadedHandler() // 씬 로드 이벤트 구독
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded; // 중복 씬 로드 구독 해제
            SceneManager.sceneLoaded += HandleSceneLoaded; // 씬 로드 이벤트 구독
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode _) // 씬 로드 완료 처리
        {
            if (!string.Equals(scene.name, GameScenes.DungeonSelect, System.StringComparison.Ordinal)) // 던전 선택 씬 여부 확인
            {
                return; // 다른 씬 설치 중단
            }

            EnsureRuntimeController(); // 던전 선택 컨트롤러 설치
        }

        private static void EnsureRuntimeController() // 던전 선택 런타임 컨트롤러 보장
        {
            if (Object.FindFirstObjectByType<DungeonSelectScreenController>() != null) // 기존 컨트롤러 존재 확인
            {
                return; // 중복 설치 중단
            }

            new GameObject("DungeonSelectScreenRuntime", typeof(DungeonSelectScreenController)); // 런타임 컨트롤러 객체 생성
        }

        private void Awake() // 런타임 화면 초기화
        {
            DungeonSelectionRuntimeState.SelectionChanged += HandleSelectionChanged; // 선택 변경 이벤트 구독
            BuildRuntimeScreen(); // 런타임 던전 선택 화면 생성
        }

        private void OnDestroy() // 런타임 화면 해제
        {
            DungeonSelectionRuntimeState.SelectionChanged -= HandleSelectionChanged; // 선택 변경 이벤트 구독 해제
        }

        private void BuildRuntimeScreen() // 전체 던전 선택 화면 생성
        {
            GameObject canvasObject = new GameObject("DungeonSelectRuntimeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 컨트롤러 하위 Canvas 배치
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 400; // 기존 프로토타입 UI 상단 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 참조 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용

            Image background = CreateImage(canvasObject.transform, "Background", BackgroundColor); // 전체 배경 생성
            Stretch(background.rectTransform); // 전체 배경 확장
            Image header = CreateImage(canvasObject.transform, "Header", HeaderColor); // 상단 헤더 생성
            SetRect(header.rectTransform, new Vector2(0f, 0.87f), new Vector2(1f, 1f)); // 상단 헤더 배치
            Text title = CreateText(header.transform, "Title", "SELECT DUNGEON", 48, FontStyle.Bold, Color.white); // 화면 제목 생성
            SetRect(title.rectTransform, new Vector2(0.05f, 0.18f), new Vector2(0.46f, 0.82f)); // 화면 제목 배치
            title.alignment = TextAnchor.MiddleLeft; // 화면 제목 좌측 정렬
            Text subtitle = CreateText(header.transform, "Subtitle", "출격할 지역을 선택하세요", 22, FontStyle.Normal, new Color(0.78f, 0.84f, 0.92f, 1f)); // 화면 보조 제목 생성
            SetRect(subtitle.rectTransform, new Vector2(0.48f, 0.22f), new Vector2(0.80f, 0.78f)); // 화면 보조 제목 배치
            subtitle.alignment = TextAnchor.MiddleRight; // 화면 보조 제목 우측 정렬
            Button backButton = CreateButton(header.transform, "BackButton", "로비"); // 로비 복귀 버튼 생성
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.20f), new Vector2(0.95f, 0.80f)); // 로비 복귀 버튼 배치
            backButton.onClick.AddListener(ReturnToLobby); // 로비 복귀 이벤트 연결

            Image cardPanel = CreateImage(canvasObject.transform, "DungeonCardPanel", new Color(0.055f, 0.085f, 0.14f, 0.98f)); // 던전 카드 패널 생성
            SetRect(cardPanel.rectTransform, new Vector2(0.045f, 0.08f), new Vector2(0.60f, 0.83f)); // 던전 카드 패널 배치
            Text listLabel = CreateText(cardPanel.transform, "ListLabel", "DUNGEON LIST", 22, FontStyle.Bold, new Color(0.75f, 0.82f, 0.91f, 1f)); // 던전 목록 라벨 생성
            SetRect(listLabel.rectTransform, new Vector2(0.045f, 0.91f), new Vector2(0.50f, 0.985f)); // 던전 목록 라벨 배치
            listLabel.alignment = TextAnchor.MiddleLeft; // 던전 목록 라벨 좌측 정렬
            BuildDungeonCards(cardPanel.transform); // 던전 카드 목록 생성
            EnsureRegionErosionInitialized(); // 지역별 침식도 랜덤 초기화 (Day44 추가 작업)

            Image detailPanel = CreateImage(canvasObject.transform, "DungeonDetailPanel", PanelColor); // 상세 정보 패널 생성
            SetRect(detailPanel.rectTransform, new Vector2(0.63f, 0.08f), new Vector2(0.955f, 0.83f)); // 상세 정보 패널 배치
            BuildDetailPanel(detailPanel.transform); // 상세 정보 패널 구성
            BuildErosionDebugBar(canvasObject.transform); // 하단 지역 침식도 디버그 바 구성 (Day44)
            RefreshSelectionVisuals(); // 초기 선택 상태 표시
        }

        private void BuildErosionDebugBar(Transform parent) // 하단 지역 침식도 디버그 바 구성 (Day44)
        {
            erosionDebugText = CreateText(parent, "ErosionDebugText", "침식도 · -", 18, FontStyle.Bold, new Color(0.80f, 0.55f, 0.30f, 1f)); // 침식도 디버그 텍스트 생성
            SetRect(erosionDebugText.rectTransform, new Vector2(0.045f, 0.01f), new Vector2(0.55f, 0.065f)); // 하단 여백에 침식도 디버그 텍스트 배치
            erosionDebugText.alignment = TextAnchor.MiddleLeft; // 침식도 디버그 텍스트 좌측 정렬
            Button minusButton = CreateButton(parent, "ErosionMinusDebug", "침식도 -10"); // 침식도 감소 디버그 버튼 생성
            SetRect(minusButton.GetComponent<RectTransform>(), new Vector2(0.57f, 0.01f), new Vector2(0.72f, 0.065f)); // 침식도 감소 버튼 배치
            minusButton.onClick.AddListener(DecreaseErosionDebug); // 침식도 감소 버튼 이벤트 연결
            Button plusButton = CreateButton(parent, "ErosionPlusDebug", "침식도 +10"); // 침식도 증가 디버그 버튼 생성
            SetRect(plusButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.01f), new Vector2(0.89f, 0.065f)); // 침식도 증가 버튼 배치
            plusButton.onClick.AddListener(IncreaseErosionDebug); // 침식도 증가 버튼 이벤트 연결
        }

        private void BuildDungeonCards(Transform parent) // 던전 카드 목록 생성
        {
            cardVisuals.Clear(); // 기존 카드 참조 초기화

            for (int index = 0; index < DungeonSelectionRuntimeState.SupportedDungeonIds.Count; index++) // 지원 던전 ID 순회
            {
                string dungeonId = DungeonSelectionRuntimeState.SupportedDungeonIds[index]; // 현재 던전 ID 조회
                DungeonData dungeon = GetDungeon(dungeonId); // 현재 던전 데이터 조회
                float maxY = 0.865f - (index * 0.215f); // 현재 카드 최대 Y 계산
                float minY = maxY - 0.18f; // 현재 카드 최소 Y 계산
                Button cardButton = CreateCardButton(parent, $"DungeonCard_{dungeonId}"); // 던전 카드 버튼 생성
                SetRect(cardButton.GetComponent<RectTransform>(), new Vector2(0.045f, minY), new Vector2(0.955f, maxY)); // 던전 카드 배치
                Image cardImage = cardButton.GetComponent<Image>(); // 던전 카드 이미지 조회
                Outline cardOutline = cardButton.gameObject.AddComponent<Outline>(); // 던전 카드 외곽선 추가
                cardOutline.effectColor = GoldColor; // 선택 외곽선 색상 설정
                cardOutline.effectDistance = new Vector2(3f, -3f); // 선택 외곽선 두께 설정
                cardOutline.enabled = false; // 초기 선택 외곽선 비활성화
                Text idText = CreateText(cardButton.transform, "Id", dungeonId, 18, FontStyle.Bold, GoldColor); // 던전 ID 텍스트 생성
                SetRect(idText.rectTransform, new Vector2(0.035f, 0.56f), new Vector2(0.24f, 0.90f)); // 던전 ID 텍스트 배치
                idText.alignment = TextAnchor.MiddleLeft; // 던전 ID 텍스트 좌측 정렬
                string dungeonName = dungeon == null ? "데이터 없음" : dungeon.DisplayName; // 카드 던전 이름 결정
                Text nameText = CreateText(cardButton.transform, "Name", dungeonName, 27, FontStyle.Bold, Color.white); // 던전 이름 텍스트 생성
                SetRect(nameText.rectTransform, new Vector2(0.035f, 0.16f), new Vector2(0.66f, 0.61f)); // 던전 이름 텍스트 배치
                nameText.alignment = TextAnchor.MiddleLeft; // 던전 이름 텍스트 좌측 정렬
                string levelValue = dungeon == null ? "DATA MISSING" : $"권장 Lv.{dungeon.RecommendedLevel}"; // 카드 권장 레벨 문구 결정
                Text levelText = CreateText(cardButton.transform, "Level", levelValue, 20, FontStyle.Bold, dungeon == null ? new Color(0.88f, 0.42f, 0.42f, 1f) : new Color(0.82f, 0.87f, 0.93f, 1f)); // 권장 레벨 텍스트 생성
                SetRect(levelText.rectTransform, new Vector2(0.68f, 0.18f), new Vector2(0.95f, 0.82f)); // 권장 레벨 텍스트 배치
                levelText.alignment = TextAnchor.MiddleRight; // 권장 레벨 텍스트 우측 정렬
                cardButton.interactable = dungeon != null; // 데이터 존재 기반 카드 활성화
                string capturedDungeonId = dungeonId; // 버튼 이벤트용 던전 ID 복사
                cardButton.onClick.AddListener(() => SelectDungeon(capturedDungeonId)); // 던전 선택 이벤트 연결
                cardVisuals[dungeonId] = new DungeonCardVisual(cardButton, cardImage, cardOutline, dungeon != null); // 카드 시각 참조 저장
            }
        }

        private void BuildDetailPanel(Transform parent) // 상세 정보 패널 구성
        {
            Text detailLabel = CreateText(parent, "DetailLabel", "DUNGEON DETAIL", 21, FontStyle.Bold, NavyColor); // 상세 정보 라벨 생성
            SetRect(detailLabel.rectTransform, new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.97f)); // 상세 정보 라벨 배치
            detailLabel.alignment = TextAnchor.MiddleLeft; // 상세 정보 라벨 좌측 정렬
            Image divider = CreateImage(parent, "Divider", new Color(0.20f, 0.28f, 0.38f, 0.32f)); // 상세 구분선 생성
            SetRect(divider.rectTransform, new Vector2(0.08f, 0.855f), new Vector2(0.92f, 0.86f)); // 상세 구분선 배치
            detailNameText = CreateText(parent, "DungeonName", "던전을 선택하세요", 34, FontStyle.Bold, NavyColor); // 상세 던전 이름 생성
            SetRect(detailNameText.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.84f)); // 상세 던전 이름 배치
            detailNameText.alignment = TextAnchor.MiddleLeft; // 상세 던전 이름 좌측 정렬
            detailRegionText = CreateText(parent, "Region", "REGION · -", 20, FontStyle.Bold, new Color(0.32f, 0.39f, 0.47f, 1f)); // 상세 지역 텍스트 생성
            SetRect(detailRegionText.rectTransform, new Vector2(0.08f, 0.64f), new Vector2(0.92f, 0.71f)); // 상세 지역 텍스트 배치
            detailRegionText.alignment = TextAnchor.MiddleLeft; // 상세 지역 텍스트 좌측 정렬
            detailErosionText = CreateText(parent, "Erosion", "침식도 · -", 17, FontStyle.Bold, new Color(0.60f, 0.28f, 0.16f, 1f)); // 상세 침식도 및 적 스탯 증가 텍스트 생성 (Day44 추가 작업)
            SetRect(detailErosionText.rectTransform, new Vector2(0.08f, 0.575f), new Vector2(0.92f, 0.635f)); // 상세 침식도 텍스트 배치
            detailErosionText.alignment = TextAnchor.MiddleLeft; // 상세 침식도 텍스트 좌측 정렬
            Image infoBox = CreateImage(parent, "InfoBox", new Color(0.84f, 0.82f, 0.75f, 0.92f)); // 상세 수치 상자 생성
            SetRect(infoBox.rectTransform, new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.565f)); // 상세 수치 상자 배치
            detailLevelText = CreateText(infoBox.transform, "RecommendedLevel", "권장 레벨\n-", 24, FontStyle.Bold, NavyColor); // 상세 권장 레벨 생성
            SetRect(detailLevelText.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.47f, 0.88f)); // 상세 권장 레벨 배치
            detailLevelText.alignment = TextAnchor.MiddleCenter; // 상세 권장 레벨 중앙 정렬
            detailRewardText = CreateText(infoBox.transform, "Rewards", "예상 보상\nEXP -   GOLD -", 22, FontStyle.Bold, NavyColor); // 상세 보상 텍스트 생성
            SetRect(detailRewardText.rectTransform, new Vector2(0.48f, 0.12f), new Vector2(0.95f, 0.88f)); // 상세 보상 텍스트 배치
            detailRewardText.alignment = TextAnchor.MiddleCenter; // 상세 보상 텍스트 중앙 정렬
            detailStatusText = CreateText(parent, "Status", "선택된 던전이 없습니다.", 18, FontStyle.Normal, new Color(0.35f, 0.39f, 0.43f, 1f)); // 상세 상태 텍스트 생성
            SetRect(detailStatusText.rectTransform, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.31f)); // 상세 상태 텍스트 배치
            detailStatusText.alignment = TextAnchor.MiddleLeft; // 상세 상태 텍스트 좌측 정렬
            enterButton = CreateButton(parent, "EnterButton", "전투 시작"); // 전투 진입 버튼 생성
            SetRect(enterButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.075f), new Vector2(0.92f, 0.19f)); // 전투 진입 버튼 배치
            enterButton.onClick.AddListener(EnterBattle); // 전투 진입 이벤트 연결
            enterButtonLabel = enterButton.GetComponentInChildren<Text>(); // 전투 진입 라벨 조회
            enterButton.interactable = false; // 초기 전투 진입 비활성화
        }

        private void SelectDungeon(string dungeonId) // 던전 선택 처리
        {
            if (!DungeonSelectionRuntimeState.TrySelect(dungeonId, HasDungeonData)) // 던전 선택 성공 여부 확인
            {
                RefreshSelectionVisuals(); // 실패 상태 화면 갱신
                return; // 선택 처리 중단
            }

            RefreshSelectionVisuals(); // 성공 선택 화면 갱신
        }

        private void HandleSelectionChanged(string dungeonId) // 선택 변경 이벤트 처리
        {
            RefreshSelectionVisuals(); // 선택 변경 화면 갱신
        }

        private void RefreshSelectionVisuals() // 선택 카드 및 상세 정보 갱신
        {
            string selectedId = DungeonSelectionRuntimeState.SelectedDungeonId; // 현재 선택 던전 ID 조회

            foreach (KeyValuePair<string, DungeonCardVisual> pair in cardVisuals) // 던전 카드 시각 순회
            {
                bool selected = string.Equals(pair.Key, selectedId, System.StringComparison.Ordinal); // 현재 카드 선택 여부 계산
                pair.Value.Outline.enabled = selected && pair.Value.HasData; // 선택 카드 외곽선 적용
                pair.Value.Image.color = pair.Value.HasData ? (selected ? SelectedCardColor : CardColor) : DisabledColor; // 카드 상태 색상 적용
            }

            DungeonData selectedDungeon = GetDungeon(selectedId); // 선택 던전 데이터 조회

            if (selectedDungeon == null || !DungeonSelectionRuntimeState.IsSupportedDungeonId(selectedId)) // 유효 선택 데이터 확인
            {
                SetText(detailNameText, "던전을 선택하세요"); // 미선택 상세 이름 표시
                SetText(detailRegionText, "REGION · -"); // 미선택 지역 표시
                SetText(detailLevelText, "권장 레벨\n-"); // 미선택 권장 레벨 표시
                SetText(detailRewardText, "예상 보상\nEXP -   GOLD -"); // 미선택 보상 표시
                SetText(detailStatusText, GameManager.Instance == null ? "Bootstrap 씬부터 실행해 주세요." : "선택된 던전이 없습니다."); // 미선택 상태 표시
                SetEnterState(false); // 전투 진입 비활성화
                RefreshErosionDebugText(null); // 미선택 침식도 디버그 표시 (Day44)
                return; // 상세 갱신 중단
            }

            SetText(detailNameText, selectedDungeon.DisplayName); // 선택 던전 이름 표시
            SetText(detailRegionText, $"REGION · {selectedDungeon.RegionId}"); // 선택 던전 지역 표시
            SetText(detailLevelText, $"권장 레벨\nLv.{selectedDungeon.RecommendedLevel}"); // 선택 던전 권장 레벨 표시
            SetText(detailRewardText, $"예상 보상\nEXP {selectedDungeon.RewardExp}   GOLD {selectedDungeon.RewardGold}"); // 선택 던전 보상 표시
            SetText(detailStatusText, $"{selectedDungeon.Id} · 출격 준비 완료"); // 선택 던전 상태 표시
            SetEnterState(DungeonSelectionRuntimeState.CanEnter(HasDungeonData)); // 선택 데이터 기반 전투 진입 상태 적용
            RefreshErosionDebugText(selectedDungeon); // 선택 던전 지역 침식도 디버그 표시 (Day44)
        }

        private void RefreshErosionDebugText(DungeonData selectedDungeon) // 지역 침식도 디버그 및 상세 패널 텍스트 갱신 (Day44)
        {
            if (selectedDungeon == null || string.IsNullOrWhiteSpace(selectedDungeon.RegionId)) // 선택 던전 및 지역 정보 확인
            {
                SetText(erosionDebugText, "침식도 · -"); // 지역 정보 없음 하단 표시
                SetText(detailErosionText, "침식도 · -"); // 지역 정보 없음 상세 패널 표시
                return; // 침식도 텍스트 갱신 종료
            }

            if (GameManager.Instance == null || GameManager.Instance.Save == null || GameManager.Instance.Save.CurrentSave == null) // 저장 데이터 확인
            {
                SetText(erosionDebugText, $"침식도({selectedDungeon.RegionId}) · -"); // 저장 데이터 없음 하단 표시
                SetText(detailErosionText, "침식도 · 저장 데이터 없음"); // 저장 데이터 없음 상세 패널 표시
                return; // 침식도 텍스트 갱신 종료
            }

            SaveData saveData = GameManager.Instance.Save.CurrentSave; // 현재 저장 데이터 조회
            int erosion = RegionErosionService.GetOrInitializeErosion(saveData, selectedDungeon.RegionId); // 선택 지역 침식도 조회 (미등록 시 랜덤 초기화)
            RegionErosionTier tier = RegionErosionService.GetErosionTier(saveData, selectedDungeon.RegionId); // 선택 지역 침식도 등급 조회
            int statBonusPercent = RegionErosionService.GetEnemyStatBonusPercent(saveData, selectedDungeon.RegionId); // 선택 지역 적 스탯 증가율 조회
            string tierLabel = GetErosionTierLabel(tier); // 침식도 등급 한글 라벨 조회
            SetText(erosionDebugText, $"침식도({selectedDungeon.RegionId}) · {erosion}/{RegionErosionSaveData.MaxErosion} · {tierLabel}"); // 침식도 디버그 문구 하단 표시
            SetText(detailErosionText, $"침식도 {erosion}/{RegionErosionSaveData.MaxErosion} · {tierLabel} · 적 스탯 +{statBonusPercent}%"); // 침식도 및 적 스탯 증가 상세 패널 표시
        }

        private void EnsureRegionErosionInitialized() // 지원 던전 소속 지역 침식도 랜덤 초기화 (Day44 추가 작업)
        {
            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                return; // 초기화 중단
            }

            SaveManager saveManager = GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager.CurrentSave; // 현재 저장 데이터 조회

            if (saveData == null) // 현재 저장 데이터 확인
            {
                return; // 초기화 중단
            }

            bool changed = false; // 신규 초기화 발생 여부 초기화

            for (int index = 0; index < DungeonSelectionRuntimeState.SupportedDungeonIds.Count; index++) // 지원 던전 ID 순회
            {
                DungeonData dungeon = GetDungeon(DungeonSelectionRuntimeState.SupportedDungeonIds[index]); // 현재 던전 데이터 조회

                if (dungeon == null || string.IsNullOrWhiteSpace(dungeon.RegionId)) // 던전 및 지역 정보 확인
                {
                    continue; // 지역 정보 없는 던전 제외
                }

                if (RegionErosionService.TryInitializeRandomErosion(saveData, dungeon.RegionId, out _)) // 미등록 지역 랜덤 침식도 초기화 시도
                {
                    changed = true; // 신규 초기화 발생 기록
                }
            }

            if (changed) // 신규 초기화 발생 확인
            {
                saveManager.SaveCurrent(); // 랜덤 초기화 결과 저장
            }
        }

        private void DecreaseErosionDebug() // 지역 침식도 디버그 감소 처리 (Day44)
        {
            ApplyErosionDebugDelta(-10); // 침식도 10 감소 실행
        }

        private void IncreaseErosionDebug() // 지역 침식도 디버그 증가 처리 (Day44)
        {
            ApplyErosionDebugDelta(10); // 침식도 10 증가 실행
        }

        private void ApplyErosionDebugDelta(int delta) // 지역 침식도 디버그 증감 공통 처리 (Day44)
        {
            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                SetText(detailStatusText, "저장 데이터를 찾을 수 없습니다."); // 저장 관리자 누락 안내
                return; // 침식도 디버그 변경 중단
            }

            DungeonData selectedDungeon = GetDungeon(DungeonSelectionRuntimeState.SelectedDungeonId); // 선택 던전 데이터 조회

            if (selectedDungeon == null || string.IsNullOrWhiteSpace(selectedDungeon.RegionId)) // 선택 던전 및 지역 정보 확인
            {
                SetText(detailStatusText, "지역 정보가 있는 던전을 먼저 선택해 주세요."); // 지역 정보 없음 안내
                return; // 침식도 디버그 변경 중단
            }

            SaveManager saveManager = GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager.CurrentSave; // 현재 저장 데이터 조회

            if (saveData == null) // 현재 저장 데이터 확인
            {
                SetText(detailStatusText, "저장 데이터를 찾을 수 없습니다."); // 저장 데이터 누락 안내
                return; // 침식도 디버그 변경 중단
            }

            RegionErosionService.AddErosion(saveData, selectedDungeon.RegionId, delta); // 지역 침식도 디버그 증감 적용
            bool saved = saveManager.SaveCurrent(); // 침식도 변경 즉시 저장
            SetText(detailStatusText, saved ? $"{selectedDungeon.RegionId} 침식도를 변경하고 저장했습니다." : "침식도는 변경되었지만 저장에 실패했습니다."); // 침식도 변경 결과 표시
            RefreshErosionDebugText(selectedDungeon); // 침식도 디버그 텍스트 갱신
        }

        private static string GetErosionTierLabel(RegionErosionTier tier) // 침식도 등급 한글 라벨 변환 (Day44)
        {
            switch (tier) // 등급별 분기
            {
                case RegionErosionTier.Stable: // 안정 등급 처리
                    return "안정"; // 안정 라벨 반환
                case RegionErosionTier.Cracked: // 균열 등급 처리
                    return "균열"; // 균열 라벨 반환
                case RegionErosionTier.Eroded: // 침식 등급 처리
                    return "침식"; // 침식 라벨 반환
                case RegionErosionTier.Dangerous: // 위험 등급 처리
                    return "위험"; // 위험 라벨 반환
                default: // 붕괴 등급 처리
                    return "붕괴"; // 붕괴 라벨 반환
            }
        }

        private void SetEnterState(bool interactable) // 전투 진입 버튼 상태 적용
        {
            if (enterButton == null) // 전투 진입 버튼 참조 확인
            {
                return; // 버튼 상태 처리 중단
            }

            enterButton.interactable = interactable; // 전투 진입 버튼 활성 상태 적용

            if (enterButtonLabel != null) // 전투 진입 라벨 참조 확인
            {
                enterButtonLabel.text = interactable ? "전투 시작" : "던전 선택 필요"; // 전투 진입 라벨 상태 적용
            }
        }

        private void EnterBattle() // 선택 던전 전투 진입
        {
            if (!DungeonSelectionRuntimeState.CanEnter(HasDungeonData)) // 유효 선택 여부 확인
            {
                RefreshSelectionVisuals(); // 진입 차단 상태 갱신
                return; // 전투 진입 중단
            }

            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 준비 확인
            {
                SetText(detailStatusText, "SceneLoader를 찾을 수 없습니다."); // 씬 로더 누락 표시
                return; // 전투 진입 중단
            }

            GameManager.Instance.Scenes.LoadScene(GameScenes.Battle); // 전투 씬 이동
        }

        private void ReturnToLobby() // 로비 복귀 처리
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 준비 확인
            {
                SetText(detailStatusText, "SceneLoader를 찾을 수 없습니다."); // 씬 로더 누락 표시
                return; // 로비 복귀 중단
            }

            GameManager.Instance.Scenes.LoadScene(GameScenes.Lobby); // 로비 씬 이동
        }

        private bool HasDungeonData(string dungeonId) // 던전 데이터 존재 확인
        {
            return GetDungeon(dungeonId) != null; // 던전 조회 결과 반환
        }

        private DungeonData GetDungeon(string dungeonId) // 던전 데이터 안전 조회
        {
            if (string.IsNullOrWhiteSpace(dungeonId)) // 던전 ID 유효성 확인
            {
                return null; // 빈 ID 조회 차단
            }

            if (GameManager.Instance == null || GameManager.Instance.Data == null || !GameManager.Instance.Data.IsInitialized) // 데이터 관리자 준비 확인
            {
                return null; // 데이터 조회 차단
            }

            return GameManager.Instance.Data.GetDungeon(dungeonId); // 던전 데이터 조회 반환
        }

        private static Button CreateCardButton(Transform parent, string name) // 공통 던전 카드 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 던전 카드 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // 던전 카드 부모 배치
            Image image = buttonObject.GetComponent<Image>(); // 던전 카드 이미지 조회
            image.color = CardColor; // 던전 카드 기본 색상 설정
            Button button = buttonObject.GetComponent<Button>(); // 던전 카드 버튼 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 연결
            return button; // 던전 카드 버튼 반환
        }

        private static Image CreateImage(Transform parent, string name, Color color) // 공통 UI 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // UI 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // UI 이미지 부모 배치
            Image image = imageObject.GetComponent<Image>(); // UI 이미지 참조 조회
            image.color = color; // UI 이미지 색상 적용
            return image; // UI 이미지 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color) // 공통 UI 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // UI 텍스트 부모 배치
            Text text = textObject.GetComponent<Text>(); // UI Text 참조 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // UI 텍스트 값 적용
            text.fontSize = size; // UI 텍스트 크기 적용
            text.fontStyle = style; // UI 텍스트 스타일 적용
            text.color = color; // UI 텍스트 색상 적용
            text.alignment = TextAnchor.MiddleCenter; // UI 텍스트 기본 중앙 정렬
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // UI 텍스트 가로 줄바꿈 적용
            text.verticalOverflow = VerticalWrapMode.Truncate; // UI 텍스트 세로 초과 제한
            text.raycastTarget = false; // UI 텍스트 입력 차단 해제
            return text; // UI 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string name, string label) // 공통 UI 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // UI 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // UI 버튼 부모 배치
            Image image = buttonObject.GetComponent<Image>(); // UI 버튼 이미지 조회
            image.color = HeaderColor; // UI 버튼 기본 색상 적용
            Button button = buttonObject.GetComponent<Button>(); // UI 버튼 참조 조회
            button.targetGraphic = image; // UI 버튼 대상 그래픽 연결
            ColorBlock colors = button.colors; // UI 버튼 색상 설정 복사
            colors.normalColor = HeaderColor; // UI 버튼 기본 상태 색상 설정
            colors.highlightedColor = new Color(0.14f, 0.27f, 0.43f, 1f); // UI 버튼 강조 상태 색상 설정
            colors.pressedColor = new Color(0.06f, 0.12f, 0.21f, 1f); // UI 버튼 누름 상태 색상 설정
            colors.disabledColor = new Color(0.32f, 0.34f, 0.36f, 0.65f); // UI 버튼 비활성 상태 색상 설정
            button.colors = colors; // UI 버튼 색상 설정 적용
            Text text = CreateText(buttonObject.transform, "Label", label, 22, FontStyle.Bold, Color.white); // UI 버튼 라벨 생성
            Stretch(text.rectTransform, 8f); // UI 버튼 라벨 확장
            return button; // UI 버튼 반환
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target == null) // 텍스트 참조 확인
            {
                return; // 텍스트 설정 중단
            }

            target.text = value; // 텍스트 값 적용
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) // 정규화 UI 영역 배치
        {
            rect.anchorMin = anchorMin; // 최소 앵커 설정
            rect.anchorMax = anchorMax; // 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        private static void Stretch(RectTransform rect, float padding = 0f) // 부모 전체 영역 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 여백 설정
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 여백 설정
        }

        private sealed class DungeonCardVisual // 던전 카드 시각 참조 묶음
        {
            public DungeonCardVisual(Button button, Image image, Outline outline, bool hasData) // 던전 카드 시각 참조 생성
            {
                Button = button; // 버튼 참조 저장
                Image = image; // 이미지 참조 저장
                Outline = outline; // 외곽선 참조 저장
                HasData = hasData; // 데이터 존재 상태 저장
            }

            public Button Button { get; } // 버튼 참조 반환
            public Image Image { get; } // 이미지 참조 반환
            public Outline Outline { get; } // 외곽선 참조 반환
            public bool HasData { get; } // 데이터 존재 상태 반환
        }
    }
}
