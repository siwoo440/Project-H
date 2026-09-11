using System; // 문자열 비교 및 수학 기능
using ProjectH.Battle; // 장비 최종 능력치 비교 기능
using ProjectH.Battle.Rhythm; // 원형 스프라이트 기능 (Day60 추가)
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Data; // 캐릭터 및 장비 데이터 기능
using ProjectH.Dialogue; // 대화 진행·결과 반영 기능 (Day58 추가)
using ProjectH.SaveSystem; // 저장 및 장착 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 캐릭터 화면 방지
    public sealed class CharacterEquipmentScreenController : MonoBehaviour // 캐릭터 화면 (Day36 장비 화면 → Day60 목업 구성 : 상단 바 · 좌측 일러스트 · 스텟/장비/룬/스킬/프로필 탭)
    {
        private static readonly string[] DemoEquipmentIds = // 임시 테스트 장비 ID 목록
        {
            "EQ_WEAPON_TRAINING", // 훈련용 검 ID
            "EQ_WEAPON_IRON", // 철제 검 ID
            "EQ_ARMOR_TRAINING", // 훈련용 갑옷 ID
            "EQ_ARMOR_GUARD", // 수호자 갑옷 ID
            "EQ_HELMET_TRAINING", // 훈련용 투구 ID (Day60 추가)
            "EQ_GLOVES_TRAINING", // 훈련용 장갑 ID (Day60 추가)
            "EQ_BOOTS_TRAINING" // 훈련용 신발 ID (Day60 추가)
        };
        private const int EquipmentTabIndex = 1; // 장비 탭 (Day60 추가)

        private static readonly string[] TabNames = { "스텟", "장비", "룬", "스킬", "프로필" }; // 탭 이름 (Day60 목업 순서)
        private const int StatTabIndex = 0; // 스텟 탭
        private const int RuneTabIndex = 2; // 룬 탭
        private Canvas canvas; // Runtime Canvas
        private readonly RectTransform[] tabRoots = new RectTransform[5]; // 탭 내용 영역 (Day60 추가)
        private readonly Button[] tabButtons = new Button[5]; // 탭 버튼 (Day60 추가)
        private int currentTab; // 현재 탭 (Day60 추가)
        private RectTransform leftArea; // 좌측 일러스트 영역 (Day60 추가, 룬 슬롯이 둘레에 배치)
        private Image portraitImage; // 캐릭터 일러스트 (Day60 추가)
        private Text goldText; // 상단 골드 표시 (Day60 추가)
        private GameObject helpPanel; // ? 도움말 창 (Day60 추가)
        private Text helpText; // 도움말 문구 (Day60 추가)
        private CharacterRuneTab runeTab; // 룬 탭 (Day60 추가)
        private CharacterSkillTab skillTab; // 스킬 탭 (Day60 추가)
        private CharacterProfileTab profileTab; // 프로필 탭 (Day60 추가)
        private Text characterNameText; // 캐릭터 이름 텍스트
        private Text characterLevelText; // 캐릭터 레벨 텍스트
        private Text portraitText; // 캐릭터 임시 초상 텍스트
        private Text currentStatsText; // 현재 최종 능력치 텍스트
        private readonly Image[] equipmentSlotFrames = new Image[CharacterSlotLayout.SlotCount]; // 장비 5칸 틀 (Day60 추가)
        private readonly Text[] equipmentSlotNames = new Text[CharacterSlotLayout.SlotCount]; // 장비 5칸 아래 이름 (Day60 추가)
        private GameObject equipmentSlotRoot; // 일러스트 둘레 장비 칸 묶음 (Day60 추가, 장비 탭에서만 표시)
        private int equipmentFilter = -1; // 보유 장비 목록 슬롯 필터 (-1 전체, Day60 추가)
        private Text inventoryCountText; // 인벤토리 개수 텍스트
        private RectTransform inventoryContent; // 장비 인벤토리 목록 영역
        private Text detailTitleText; // 선택 장비 이름 텍스트
        private Text detailGradeText; // 선택 장비 등급 텍스트
        private Text detailStatsText; // 선택 장비 옵션 텍스트
        private Text comparisonText; // 장비 교체 비교 텍스트
        private Text statusText; // 화면 상태 텍스트
        private Text affinityText; // 성장·관계 정보 텍스트 (Day43 호감도 디버그 → Day60 스텟 탭 정보)
        private CharacterAffinityRewardPanel affinityRewardPanel; // 호감도 보상 패널 (Day56 추가, 최적화로 별도 클래스 분리)
        private CharacterGiftPanel giftPanel; // 선물하기 패널 (Day57 추가)
        private CharacterBondPanel bondPanel; // 결속 패널 (Day59 추가)
        private Button actionButton; // 장착 액션 버튼
        private Text actionButtonText; // 장착 액션 라벨
        private int selectedCharacterIndex; // 현재 캐릭터 목록 번호
        private string selectedInstanceId = string.Empty; // 선택 장비 인스턴스 ID

        private void Start() // 캐릭터 장비 화면 시작
        {
            EnsureEventSystem(); // UI 입력 시스템 보장
            BuildUi(); // Day36 장비 UI 구성
            Refresh(); // 초기 화면 데이터 갱신
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

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // 신규 Input System EventSystem 생성
            eventSystemObject.transform.SetParent(transform, false); // 캐릭터 화면 하위 입력 시스템 연결
        }

        private void BuildUi() // 전체 캐릭터 화면 구성 (Day60 목업 구성으로 재배치)
        {
            GameObject canvasObject = new GameObject("CharacterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Runtime Canvas 생성
            canvasObject.transform.SetParent(transform, false); // 캐릭터 화면 하위 Canvas 연결
            canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 100; // 화면 UI 정렬 순서 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1600f, 900f); // 캐릭터 화면 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로 세로 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            Image background = CreateImage(canvas.transform, "Background", new Color(0.94f, 0.94f, 0.95f, 1f)); // 전체 배경 생성
            Stretch(background.rectTransform); // 전체 배경 확장
            BuildTopBar(background.transform); // 상단 바 (캐릭터 · ? · 골드 · 설정)
            BuildLeftArea(background.transform); // 좌측 일러스트 영역
            BuildTabs(background.transform); // 우측 탭과 내용
            affinityRewardPanel = CharacterAffinityRewardPanel.Create(background.transform, Refresh, PlayCharacterEvent); // 호감도 보상 패널 (Day56, 스텟 탭 버튼으로 열기)
            giftPanel = CharacterGiftPanel.Create(background.transform, Refresh, OpenAffinityRewardFromGift); // 선물 패널 (Day57)
            bondPanel = CharacterBondPanel.Create(background.transform, Refresh); // 결속 패널 (Day59)
            BuildHelpPanel(background.transform); // ? 도움말 창
            SelectTab(StatTabIndex); // 스텟 탭으로 시작
        }

        private void BuildTopBar(Transform parent) // 상단 바 (목업 : ◀ 캐릭터 · ? · 골드 · 설정)
        {
            Button backButton = CreateButton(parent, "BackButton", "◀  캐릭터", new Color(0.80f, 0.88f, 0.97f, 1f)); // 뒤로 버튼 (로비 복귀)
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.012f, 0.925f), new Vector2(0.14f, 0.982f)); // 좌상단 배치
            backButton.onClick.AddListener(ReturnToLobby); // 로비 복귀 연결
            Button helpButton = CreateButton(parent, "HelpButton", "?", new Color(0.82f, 0.88f, 0.98f, 1f)); // 도움말 버튼
            SetRect(helpButton.GetComponent<RectTransform>(), new Vector2(0.735f, 0.928f), new Vector2(0.77f, 0.98f)); // 배치
            helpButton.onClick.AddListener(ToggleHelp); // 도움말 연결
            Image goldPill = CreateImage(parent, "GoldPill", new Color(0.86f, 0.86f, 0.88f, 1f)); // 골드 표시 바탕
            SetRect(goldPill.rectTransform, new Vector2(0.78f, 0.93f), new Vector2(0.93f, 0.978f)); // 배치
            AddOutline(goldPill.gameObject); // 테두리
            Image coin = CreateImage(goldPill.transform, "Coin", new Color(1f, 0.80f, 0.25f, 1f)); // 동전 아이콘
            coin.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // 원형
            coin.preserveAspect = true; // 비율 유지
            SetRect(coin.rectTransform, new Vector2(0.03f, 0.10f), new Vector2(0.20f, 0.90f)); // 왼쪽 배치
            Text coinLabel = CreateText(coin.transform, "G", "G", 15, FontStyle.Bold, new Color(0.45f, 0.30f, 0.02f, 1f)); // 동전 글자
            Stretch(coinLabel.rectTransform); // 채움
            goldText = CreateText(goldPill.transform, "Gold", "0", 20, FontStyle.Bold, new Color(0.15f, 0.15f, 0.18f, 1f)); // 골드 수치
            goldText.alignment = TextAnchor.MiddleRight; // 오른쪽 정렬
            SetRect(goldText.rectTransform, new Vector2(0.22f, 0f), new Vector2(0.93f, 1f)); // 배치
            Button settingsButton = CreateButton(parent, "SettingsButton", "⚙", new Color(0.94f, 0.94f, 0.95f, 1f)); // 설정 버튼
            SetRect(settingsButton.GetComponent<RectTransform>(), new Vector2(0.94f, 0.925f), new Vector2(0.985f, 0.982f)); // 우상단 배치
            settingsButton.onClick.AddListener(() => SetStatus("설정 화면은 설정·접근성 일차(Day71)에 연결됩니다.")); // 설정 안내
        }

        private void BuildLeftArea(Transform parent) // 좌측 일러스트 영역 (캐릭터 전환 · 이름 · 일러스트, 룬 탭에서는 둘레에 슬롯)
        {
            GameObject area = new GameObject("LeftArea", typeof(RectTransform)); // 좌측 영역
            area.transform.SetParent(parent, false); // 부모 연결
            leftArea = (RectTransform)area.transform; // 저장
            SetRect(leftArea, new Vector2(0.015f, 0.03f), new Vector2(0.39f, 0.905f)); // 배치
            Button previousButton = CreateButton(leftArea, "PreviousCharacter", "◀", new Color(0.90f, 0.90f, 0.92f, 1f)); // 이전 캐릭터
            SetRect(previousButton.GetComponent<RectTransform>(), new Vector2(0.14f, 0.93f), new Vector2(0.24f, 1f)); // 배치
            previousButton.onClick.AddListener(SelectPreviousCharacter); // 연결
            characterNameText = CreateText(leftArea, "CharacterName", "CHARACTER", 26, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 이름
            SetRect(characterNameText.rectTransform, new Vector2(0.25f, 0.93f), new Vector2(0.60f, 1f)); // 배치
            characterLevelText = CreateText(leftArea, "CharacterLevel", "Lv. 1", 18, FontStyle.Bold, new Color(0.30f, 0.34f, 0.40f, 1f)); // 레벨
            SetRect(characterLevelText.rectTransform, new Vector2(0.60f, 0.93f), new Vector2(0.75f, 1f)); // 배치
            Button nextButton = CreateButton(leftArea, "NextCharacter", "▶", new Color(0.90f, 0.90f, 0.92f, 1f)); // 다음 캐릭터
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.93f), new Vector2(0.86f, 1f)); // 배치
            nextButton.onClick.AddListener(SelectNextCharacter); // 연결
            Image frame = CreateImage(leftArea, "IllustrationFrame", Color.white); // 흰색 일러스트 칸 (목업)
            SetRect(frame.rectTransform, new Vector2(0.14f, 0.15f), new Vector2(0.86f, 0.91f)); // 배치
            AddOutline(frame.gameObject); // 테두리
            portraitImage = CreateImage(frame.transform, "Portrait", Color.white); // 일러스트
            portraitImage.preserveAspect = true; // 비율 유지
            SetRect(portraitImage.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.98f)); // 칸 채움
            portraitText = CreateText(portraitImage.transform, "PortraitText", string.Empty, 24, FontStyle.Bold, new Color(0.20f, 0.16f, 0.26f, 0.9f)); // 임시 실루엣 이름
            SetRect(portraitText.rectTransform, new Vector2(0f, 0.40f), new Vector2(1f, 0.56f)); // 가슴 높이
        }

        private void BuildTabs(Transform parent) // 탭 버튼 5개 + 내용 틀 + 공용 상태 문구
        {
            for (int index = 0; index < TabNames.Length; index++) // 탭 순회
            {
                int tab = index; // 클릭용 번호
                tabButtons[index] = CreateButton(parent, $"Tab_{TabNames[index]}", TabNames[index], new Color(0.97f, 0.97f, 0.98f, 1f)); // 탭 버튼
                float left = 0.405f + (index * 0.117f); // 가로 위치
                SetRect(tabButtons[index].GetComponent<RectTransform>(), new Vector2(left, 0.845f), new Vector2(left + 0.11f, 0.90f)); // 배치
                AddOutline(tabButtons[index].gameObject); // 테두리
                tabButtons[index].onClick.AddListener(() => SelectTab(tab)); // 탭 전환
            }

            Image frame = CreateImage(parent, "ContentFrame", new Color(0.97f, 0.97f, 0.98f, 1f)); // 바깥 틀 (목업 이중 테두리)
            SetRect(frame.rectTransform, new Vector2(0.405f, 0.03f), new Vector2(0.985f, 0.835f)); // 배치
            AddOutline(frame.gameObject); // 테두리
            Image inner = CreateImage(frame.transform, "Inner", new Color(0.93f, 0.93f, 0.94f, 1f)); // 안쪽 틀
            Stretch(inner.rectTransform, 8f); // 여백
            AddOutline(inner.gameObject); // 테두리
            statusText = CreateText(inner.transform, "Status", string.Empty, 16, FontStyle.Normal, new Color(0.25f, 0.28f, 0.32f, 1f)); // 공용 상태 문구
            statusText.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            SetRect(statusText.rectTransform, new Vector2(0.02f, 0.005f), new Vector2(0.98f, 0.06f)); // 맨 아래 배치

            for (int index = 0; index < TabNames.Length; index++) // 탭 내용 영역
            {
                GameObject root = new GameObject($"{TabNames[index]}Tab", typeof(RectTransform)); // 영역 생성
                root.transform.SetParent(inner.transform, false); // 부모 연결
                tabRoots[index] = (RectTransform)root.transform; // 저장
                SetRect(tabRoots[index], new Vector2(0f, 0.065f), new Vector2(1f, 1f)); // 상태 문구 위 채움
            }

            BuildStatTab(tabRoots[0]); // 스텟 탭
            BuildEquipmentTab(tabRoots[1]); // 장비 탭 (기존 Day36 장비 화면)
            runeTab = CharacterRuneTab.Create(tabRoots[2], leftArea, Refresh, SetStatus); // 룬 탭 (Day60)
            skillTab = CharacterSkillTab.Create(tabRoots[3]); // 스킬 탭
            profileTab = CharacterProfileTab.Create(tabRoots[4]); // 프로필 탭
        }

        private void SelectTab(int index) // 탭 전환
        {
            currentTab = index; // 현재 탭 저장

            for (int tab = 0; tab < tabRoots.Length; tab++) // 탭 순회
            {
                if (tab == EquipmentTabIndex) equipmentSlotRoot.SetActive(tab == index); // 장비 탭은 일러스트 둘레 장비 칸도 함께 (Day60 추가)
                if (tab == RuneTabIndex) runeTab.SetVisible(tab == index); // 룬 탭은 일러스트 둘레 슬롯도 함께
                else tabRoots[tab].gameObject.SetActive(tab == index); // 내용 표시
                tabButtons[tab].GetComponent<Image>().color = tab == index ? new Color(0.78f, 0.86f, 0.97f, 1f) : new Color(0.97f, 0.97f, 0.98f, 1f); // 선택 강조
            }

            if (helpPanel != null && helpPanel.activeSelf) helpText.text = GetHelpText(index); // 열린 도움말 갱신
            Refresh(); // 선택 탭 내용 갱신
        }

        private void BuildStatTab(RectTransform root) // 스텟 탭 (최종 능력치 + 성장·관계 + 호감도 기능 버튼)
        {
            Text statsLabel = CreateText(root, "CurrentStatsLabel", "현재 최종 능력치", 20, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 능력치 제목
            statsLabel.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            SetRect(statsLabel.rectTransform, new Vector2(0.03f, 0.90f), new Vector2(0.48f, 0.98f)); // 배치
            currentStatsText = CreateText(root, "CurrentStats", "-", 17, FontStyle.Normal, new Color(0.20f, 0.23f, 0.27f, 1f)); // 능력치
            currentStatsText.alignment = TextAnchor.UpperLeft; // 왼쪽 위 정렬
            currentStatsText.horizontalOverflow = HorizontalWrapMode.Wrap; // 줄바꿈
            SetRect(currentStatsText.rectTransform, new Vector2(0.04f, 0.24f), new Vector2(0.48f, 0.89f)); // 배치
            Text growthLabel = CreateText(root, "GrowthLabel", "성장 · 관계", 20, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 성장 제목
            growthLabel.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            SetRect(growthLabel.rectTransform, new Vector2(0.52f, 0.90f), new Vector2(0.97f, 0.98f)); // 배치
            affinityText = CreateText(root, "GrowthInfo", string.Empty, 17, FontStyle.Normal, new Color(0.20f, 0.23f, 0.27f, 1f)); // 성장·관계 정보
            affinityText.alignment = TextAnchor.UpperLeft; // 왼쪽 위 정렬
            affinityText.horizontalOverflow = HorizontalWrapMode.Wrap; // 줄바꿈
            affinityText.supportRichText = true; // 색 태그
            SetRect(affinityText.rectTransform, new Vector2(0.53f, 0.24f), new Vector2(0.97f, 0.89f)); // 배치
            CreateStatButton(root, "AffinityRewardButton", "호감도 보상", new Color(1f, 0.88f, 0.60f, 1f), 0, ToggleAffinityRewardPanel); // 호감도 보상 (Day56)
            CreateStatButton(root, "GiftButton", "선물하기", new Color(1f, 0.82f, 0.88f, 1f), 1, ToggleGiftPanel); // 선물 (Day57)
            CreateStatButton(root, "TalkButton", "대화하기", new Color(0.84f, 0.80f, 0.98f, 1f), 2, StartDailyTalk); // 일상 대화 (Day58)
            CreateStatButton(root, "BondButton", "결속", new Color(0.78f, 0.70f, 0.96f, 1f), 3, ToggleBondPanel); // 결속 (Day59)
            Button minusButton = CreateButton(root, "AffinityMinusDebug", "호감도 -10", new Color(0.98f, 0.90f, 0.95f, 1f)); // 호감도 감소 디버그
            SetRect(minusButton.GetComponent<RectTransform>(), new Vector2(0.03f, 0.02f), new Vector2(0.23f, 0.10f)); // 배치
            minusButton.onClick.AddListener(DecreaseAffinityDebug); // 연결
            DevelopmentFeatures.HideInRelease(minusButton); // 출시 빌드 숨김
            Button plusButton = CreateButton(root, "AffinityPlusDebug", "호감도 +10", new Color(0.90f, 0.95f, 1f, 1f)); // 호감도 증가 디버그
            SetRect(plusButton.GetComponent<RectTransform>(), new Vector2(0.25f, 0.02f), new Vector2(0.45f, 0.10f)); // 배치
            plusButton.onClick.AddListener(IncreaseAffinityDebug); // 연결
            DevelopmentFeatures.HideInRelease(plusButton); // 출시 빌드 숨김
        }

        private void CreateStatButton(RectTransform root, string name, string label, Color color, int index, UnityEngine.Events.UnityAction action) // 스텟 탭 관계 기능 버튼
        {
            Button button = CreateButton(root, name, label, color); // 버튼 생성
            float left = 0.03f + (index * 0.24f); // 가로 위치
            SetRect(button.GetComponent<RectTransform>(), new Vector2(left, 0.12f), new Vector2(left + 0.22f, 0.21f)); // 배치
            AddOutline(button.gameObject); // 테두리
            button.onClick.AddListener(action); // 연결
        }

        private void BuildEquipmentTab(RectTransform root) // 장비 탭 (Day36 장비 화면을 탭 안으로 이동)
        {
            BuildEquipmentSlots(); // 일러스트 둘레 장비 5칸 (Day60 — 룬 슬롯과 같은 배치)
            Button allButton = CreateButton(root, "EquipmentFilterAll", "전체 보기", new Color(0.90f, 0.90f, 0.92f, 1f)); // 슬롯 필터 해제 (Day60 추가)
            SetRect(allButton.GetComponent<RectTransform>(), new Vector2(0.40f, 0.915f), new Vector2(0.54f, 0.975f)); // 배치
            allButton.onClick.AddListener(ClearEquipmentFilter); // 연결
            Text inventoryLabel = CreateText(root, "InventoryLabel", "보유 장비", 20, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 목록 제목
            SetRect(inventoryLabel.rectTransform, new Vector2(0.56f, 0.91f), new Vector2(0.70f, 0.98f)); // 배치
            inventoryCountText = CreateText(root, "InventoryCount", "0개", 16, FontStyle.Bold, new Color(0.30f, 0.34f, 0.40f, 1f)); // 개수
            SetRect(inventoryCountText.rectTransform, new Vector2(0.70f, 0.91f), new Vector2(0.78f, 0.98f)); // 배치
            Button grantButton = CreateButton(root, "GrantDemoEquipment", "테스트 장비 지급", new Color(0.88f, 0.83f, 0.66f, 1f)); // 테스트 장비
            SetRect(grantButton.GetComponent<RectTransform>(), new Vector2(0.79f, 0.915f), new Vector2(0.98f, 0.975f)); // 배치
            DevelopmentFeatures.HideInRelease(grantButton); // 출시 빌드 숨김
            grantButton.onClick.AddListener(GrantDemoEquipment); // 연결
            BuildInventoryScroll(root); // 장비 스크롤 목록
            Image detailPanel = CreateImage(root, "DetailPanel", new Color(0.97f, 0.97f, 0.98f, 1f)); // 상세 칸
            SetRect(detailPanel.rectTransform, new Vector2(0.02f, 0.46f), new Vector2(0.54f, 0.90f)); // 배치 (Day60 상단 슬롯 버튼 제거로 확장)
            AddOutline(detailPanel.gameObject); // 테두리
            detailTitleText = CreateText(detailPanel.transform, "DetailTitle", "장비를 선택하세요", 22, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 이름
            detailTitleText.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            SetRect(detailTitleText.rectTransform, new Vector2(0.04f, 0.80f), new Vector2(0.72f, 0.97f)); // 배치
            detailGradeText = CreateText(detailPanel.transform, "DetailGrade", string.Empty, 18, FontStyle.Bold, new Color(0.63f, 0.48f, 0.14f, 1f)); // 등급
            SetRect(detailGradeText.rectTransform, new Vector2(0.72f, 0.80f), new Vector2(0.96f, 0.97f)); // 배치
            detailStatsText = CreateText(detailPanel.transform, "DetailStats", "-", 15, FontStyle.Normal, new Color(0.20f, 0.23f, 0.27f, 1f)); // 옵션
            detailStatsText.alignment = TextAnchor.UpperLeft; // 왼쪽 위 정렬
            detailStatsText.horizontalOverflow = HorizontalWrapMode.Wrap; // 줄바꿈
            detailStatsText.verticalOverflow = VerticalWrapMode.Truncate; // 세로 제한
            SetRect(detailStatsText.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.80f)); // 배치
            Image comparisonPanel = CreateImage(root, "ComparisonPanel", new Color(0.90f, 0.93f, 0.96f, 1f)); // 비교 칸
            SetRect(comparisonPanel.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.54f, 0.44f)); // 배치
            AddOutline(comparisonPanel.gameObject); // 테두리
            Text comparisonLabel = CreateText(comparisonPanel.transform, "ComparisonLabel", "변경 전 → 변경 후", 17, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 비교 제목
            SetRect(comparisonLabel.rectTransform, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.98f)); // 배치
            comparisonText = CreateText(comparisonPanel.transform, "ComparisonText", "장비를 선택하면 예상 능력치가 표시됩니다.", 14, FontStyle.Normal, new Color(0.18f, 0.22f, 0.27f, 1f)); // 비교
            comparisonText.alignment = TextAnchor.UpperLeft; // 왼쪽 위 정렬
            comparisonText.horizontalOverflow = HorizontalWrapMode.Wrap; // 줄바꿈
            comparisonText.verticalOverflow = VerticalWrapMode.Truncate; // 세로 제한
            SetRect(comparisonText.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.95f, 0.84f)); // 배치
            actionButton = CreateButton(root, "ActionButton", "사용 불가", new Color(0.72f, 0.82f, 0.72f, 1f)); // 장착 버튼
            SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.70f, 0.02f), new Vector2(0.98f, 0.10f)); // 배치
            actionButtonText = actionButton.GetComponentInChildren<Text>(); // 라벨
            actionButton.onClick.AddListener(ApplySelectedEquipmentAction); // 연결
        }

        private void BuildEquipmentSlots() // 일러스트 둘레 장비 5칸 (Day60 추가 — 무기·투구·방어구·장갑·신발)
        {
            equipmentSlotRoot = new GameObject("EquipmentSlots", typeof(RectTransform)); // 칸 묶음
            equipmentSlotRoot.transform.SetParent(leftArea, false); // 좌측 영역 자식
            Stretch((RectTransform)equipmentSlotRoot.transform); // 좌측 영역 채움

            for (int index = 0; index < CharacterSlotLayout.SlotCount; index++) // 칸 순회
            {
                EquipmentSlot slot = EquipmentSlotInfo.All[index]; // 칸 슬롯
                Button button = RuntimeUiKit.CreateButton(equipmentSlotRoot.transform, $"EquipSlot_{slot}", new Color(0.12f, 0.12f, 0.16f, 1f)); // 칸 버튼
                SetRect(button.GetComponent<RectTransform>(), CharacterSlotLayout.GetMin(index), CharacterSlotLayout.GetMax(index)); // 룬 슬롯과 같은 자리
                equipmentSlotFrames[index] = button.GetComponent<Image>(); // 틀 저장
                Outline outline = button.gameObject.AddComponent<Outline>(); // 테두리
                outline.effectColor = new Color(0.85f, 0.85f, 0.9f, 0.6f); // 연회색
                outline.effectDistance = new Vector2(2f, -2f); // 두께
                Text symbol = CreateText(button.transform, "Symbol", EquipmentSlotInfo.GetSymbol(slot), 30, FontStyle.Bold, new Color(0.95f, 0.95f, 0.98f, 1f)); // 슬롯 글자
                SetRect(symbol.rectTransform, new Vector2(0f, 0.30f), new Vector2(1f, 1f)); // 위쪽
                Text label = CreateText(button.transform, "SlotLabel", EquipmentSlotInfo.GetLabel(slot), 13, FontStyle.Normal, new Color(0.80f, 0.80f, 0.86f, 1f)); // 슬롯 이름
                SetRect(label.rectTransform, new Vector2(0f, 0.02f), new Vector2(1f, 0.30f)); // 아래쪽
                equipmentSlotNames[index] = CreateText(equipmentSlotRoot.transform, $"EquipName_{slot}", string.Empty, 14, FontStyle.Bold, new Color(0.25f, 0.20f, 0.10f, 1f)); // 칸 아래 장비 이름
                equipmentSlotNames[index].horizontalOverflow = HorizontalWrapMode.Overflow; // 넘침 허용
                SetRect(equipmentSlotNames[index].rectTransform, CharacterSlotLayout.GetCaptionMin(index), CharacterSlotLayout.GetCaptionMax(index)); // 칸 아래 배치
                button.onClick.AddListener(() => SelectEquipmentSlot(slot)); // 칸 선택
            }
        }

        private void RefreshEquipmentSlots(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave) // 장비 5칸 갱신 (Day60 추가)
        {
            for (int index = 0; index < CharacterSlotLayout.SlotCount; index++) // 칸 순회
            {
                EquipmentSlot slot = EquipmentSlotInfo.All[index]; // 칸 슬롯
                EquipmentInstanceSaveData instance = CharacterEquipmentService.GetEquippedInstance(saveData, characterSave.CharacterId, slot); // 장착 장비
                EquipmentData equipment = instance == null ? null : dataManager.GetEquipment(instance.EquipmentId); // 원본
                equipmentSlotFrames[index].color = equipment == null ? new Color(0.12f, 0.12f, 0.16f, 1f) : GetGradeColor(equipment.Grade); // 등급 색
                equipmentSlotNames[index].text = equipment == null ? string.Empty : equipment.DisplayName; // 장비 이름
                equipmentSlotFrames[index].GetComponent<Outline>().effectColor = equipmentFilter == (int)slot ? new Color(1f, 0.80f, 0.25f, 1f) : new Color(0.85f, 0.85f, 0.9f, 0.6f); // 선택 칸 금색
            }
        }

        private static Color GetGradeColor(ItemGrade grade) // 장비 등급 칸 색 (Day60 추가)
        {
            switch (grade) // 등급 분기
            {
                case ItemGrade.Common: return new Color(0.35f, 0.36f, 0.40f, 1f); // 일반 회색
                case ItemGrade.Uncommon: return new Color(0.20f, 0.45f, 0.30f, 1f); // 고급 초록
                case ItemGrade.Rare: return new Color(0.20f, 0.35f, 0.65f, 1f); // 희귀 파랑
                case ItemGrade.Epic: return new Color(0.45f, 0.25f, 0.62f, 1f); // 영웅 보라
                default: return new Color(0.70f, 0.50f, 0.12f, 1f); // 전설 금색
            }
        }

        private void SelectEquipmentSlot(EquipmentSlot slot) // 장비 칸 선택 : 목록을 그 슬롯으로 거르고 장착 장비 선택 (Day60 추가)
        {
            equipmentFilter = (int)slot; // 목록 필터
            SelectEquippedSlot(slot); // 장착 장비 선택 (빈 칸이면 안내)
            Refresh(); // 목록 갱신
        }

        private void ClearEquipmentFilter() // 목록 필터 해제 (Day60 추가)
        {
            equipmentFilter = -1; // 전체
            Refresh(); // 목록 갱신
        }

        private void BuildHelpPanel(Transform parent) // ? 도움말 창
        {
            Image panel = CreateImage(parent, "HelpPanel", new Color(1f, 1f, 1f, 0.98f)); // 창 배경
            SetRect(panel.rectTransform, new Vector2(0.30f, 0.25f), new Vector2(0.70f, 0.80f)); // 가운데 배치
            AddOutline(panel.gameObject); // 테두리
            helpText = CreateText(panel.transform, "HelpText", string.Empty, 17, FontStyle.Normal, new Color(0.12f, 0.12f, 0.16f, 1f)); // 도움말 문구
            helpText.alignment = TextAnchor.UpperLeft; // 왼쪽 위 정렬
            helpText.horizontalOverflow = HorizontalWrapMode.Wrap; // 줄바꿈
            SetRect(helpText.rectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.95f)); // 배치
            Button close = CreateButton(panel.transform, "CloseHelp", "닫기", new Color(0.88f, 0.88f, 0.90f, 1f)); // 닫기
            SetRect(close.GetComponent<RectTransform>(), new Vector2(0.38f, 0.03f), new Vector2(0.62f, 0.11f)); // 배치
            close.onClick.AddListener(ToggleHelp); // 연결
            helpPanel = panel.gameObject; // 저장
            helpPanel.SetActive(false); // 초기 숨김
        }

        private void ToggleHelp() // 도움말 열기·닫기
        {
            helpPanel.SetActive(!helpPanel.activeSelf); // 전환
            helpPanel.transform.SetAsLastSibling(); // 최상단
            helpText.text = GetHelpText(currentTab); // 현재 탭 설명
        }

        private static string GetHelpText(int tab) // 탭별 도움말
        {
            switch (tab) // 탭 분기
            {
                case 0: return "[스텟]\n장비·룬·호감도·결속이 모두 반영된 최종 능력치입니다.\n\n아래 버튼으로 호감도 보상, 선물, 일상 대화, 결속을 관리할 수 있어요."; // 스텟
                case 1: return "[장비]\n일러스트 둘레 5칸이 장비 슬롯입니다.\n무기 · 투구 · 방어구 · 장갑 · 신발\n\n칸을 누르면 그 슬롯 장비만 목록에 보이고, 장착 중인 장비가 선택됩니다.\n목록에서 장비를 고르면 변경 전후 능력치를 비교합니다. [전체 보기]로 필터를 풉니다."; // 장비
                case 2: return "[룬]\n일러스트 둘레 5칸이 룬 슬롯입니다.\n1번 기본 · 2번 레벨 10 · 3번 결속 3단계 · 4번 고급 이상 장비 · 5번 결속 5단계\n\n강화 : 룬 조각 + 골드 (Lv.10까지, 실패 없음)\n합성 : 같은 종류·등급 2개 → 한 등급 위 (강화 조각 50% 반환)\n분해 : 룬 → 룬 조각 · 잠금 룬은 보호됩니다."; // 룬
                case 3: return "[스킬]\n스킬 1~3, 궁극기, 패시브, 결속 스킬을 확인합니다."; // 스킬
                default: return "[프로필]\n캐릭터 이름, 성우, 신체 정보와 이야기를 확인합니다.\n'###'은 아직 확정되지 않은 설정입니다."; // 프로필
            }
        }

        private void CloseOtherPanels(MonoBehaviour keep) // 호감도 보상·선물·결속 패널이 겹치지 않게 닫기 (Day59 추가)
        {
            if (affinityRewardPanel != null && keep != affinityRewardPanel && affinityRewardPanel.IsOpen) affinityRewardPanel.Toggle(); // 보상 패널 닫기
            if (giftPanel != null && keep != giftPanel && giftPanel.IsOpen) giftPanel.Toggle(); // 선물 패널 닫기
            if (bondPanel != null && keep != bondPanel && bondPanel.IsOpen) bondPanel.Toggle(); // 결속 패널 닫기
        }

        private void ToggleBondPanel() // 결속 패널 열기·닫기 (Day59 추가)
        {
            CloseOtherPanels(bondPanel); // 다른 패널 닫기
            bondPanel.Toggle(); // 결속 패널 전환
        }

        private void StartDailyTalk() // 선택 캐릭터와 현재 시간대 일상 대화 (Day58 추가)
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 확인
            {
                SetStatus("저장 데이터를 찾을 수 없습니다."); // 오류 안내
                return; // 대화 중단
            }

            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 조회

            if (characterSave == null) // 캐릭터 확인
            {
                SetStatus("선택된 캐릭터가 없습니다."); // 캐릭터 없음 안내
                return; // 대화 중단
            }

            if (!DialogueService.CanTalk(saveData, characterSave.CharacterId, out string reason)) // 대화 가능 여부 확인
            {
                SetStatus(reason); // 불가 사유 안내
                return; // 대화 중단
            }

            string characterId = characterSave.CharacterId; // 대화 캐릭터 ID 보관
            string scriptId = DialogueService.GetTalkScriptId(characterId, GameTimeService.GetCurrentPhase(saveData)); // 시간대 대화 파일 ID

            if (OpenDialogue(scriptId, runner => OnDailyTalkFinished(characterId, runner)) == null) // 대화 화면 열기
            {
                SetStatus($"대화 파일을 찾을 수 없습니다. ({scriptId})"); // 파일 누락 안내
            }
        }

        private void OnDailyTalkFinished(string characterId, DialogueRunner runner) // 일상 대화 종료 반영 (Day58 추가)
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 확인
            {
                return; // 반영 중단
            }

            DialogueRewardResult result = DialogueService.CompleteTalk(saveData, characterId, runner); // 호감도·시간 칸 반영
            bool saved = !result.Applied || saveManager.SaveCurrent(); // 반영 시 즉시 저장
            SetStatus(saved ? result.Message : $"{result.Message} (저장 실패)"); // 결과 안내
            Refresh(); // 호감도 표시 갱신
        }

        private void PlayCharacterEvent(CharacterEventDefinition definition) // 개인 이벤트 보기·다시보기 (Day58 추가)
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData) || definition == null) // 전역 데이터 확인
            {
                return; // 보기 중단
            }

            if (DialogueService.GetEventState(saveData, definition) == CharacterEventState.Locked) // 잠김 확인
            {
                affinityRewardPanel.ShowResult($"「{definition.Title}」은(는) 아직 잠겨 있습니다."); // 잠김 안내
                return; // 보기 중단
            }

            if (OpenDialogue(definition.ScriptId, runner => OnCharacterEventFinished(definition, runner)) == null) // 대화 화면 열기
            {
                affinityRewardPanel.ShowResult($"대화 파일을 찾을 수 없습니다. ({definition.ScriptId})"); // 파일 누락 안내
            }
        }

        private void OnCharacterEventFinished(CharacterEventDefinition definition, DialogueRunner runner) // 개인 이벤트 종료 반영 (Day58 추가)
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 확인
            {
                return; // 반영 중단
            }

            DialogueRewardResult result = DialogueService.CompleteEvent(saveData, definition, runner); // 완료 기록·보상 반영
            bool saved = !result.Applied || saveManager.SaveCurrent(); // 반영 시 즉시 저장
            Refresh(); // 호감도·이벤트 상태 갱신
            affinityRewardPanel.ShowResult(saved ? result.Message : $"{result.Message} (저장 실패)"); // 결과 안내
        }

        private static DialogueOverlayView OpenDialogue(string scriptId, Action<DialogueRunner> finished) // 대화 파일 불러와 화면 열기 (Day58 추가)
        {
            return DialogueOverlayView.Open(DialogueLibrary.Load(scriptId), finished); // 대화 화면 반환 (파일 없으면 null)
        }

        private void ToggleAffinityRewardPanel() // 호감도 보상 패널 열기·닫기 (Day57 추가, 선물 패널은 닫음)
        {
            CloseOtherPanels(affinityRewardPanel); // 다른 패널 닫기 (Day59 결속 패널 포함)
            affinityRewardPanel.Toggle(); // 보상 패널 전환
        }

        private void ToggleGiftPanel() // 선물 패널 열기·닫기 (Day57 추가, 보상 패널은 닫음)
        {
            CloseOtherPanels(giftPanel); // 다른 패널 닫기 (Day59 결속 패널 포함)
            giftPanel.Toggle(); // 선물 패널 전환
        }

        private void OpenAffinityRewardFromGift() // 선물 패널의 호감도 보상 바로가기 (Day57 추가)
        {
            if (giftPanel.IsOpen) giftPanel.Toggle(); // 선물 패널 닫기
            if (!affinityRewardPanel.IsOpen) affinityRewardPanel.Toggle(); // 보상 패널 열기
        }

        private void BuildInventoryScroll(Transform parent) // 장비 인벤토리 스크롤 구성
        {
            GameObject scrollObject = new GameObject("InventoryScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect)); // 스크롤 영역 생성
            scrollObject.transform.SetParent(parent, false); // 스크롤 영역 부모 연결
            Image scrollImage = scrollObject.GetComponent<Image>(); // 스크롤 배경 이미지 조회
            scrollImage.color = new Color(0.92f, 0.92f, 0.92f, 1f); // 스크롤 배경 색상 적용
            SetRect(scrollObject.GetComponent<RectTransform>(), new Vector2(0.56f, 0.13f), new Vector2(0.98f, 0.90f)); // 오른쪽 장비 스크롤 영역 배치
            AddOutline(scrollObject); // 스크롤 영역 외곽선 추가
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); // 스크롤 뷰포트 생성
            viewportObject.transform.SetParent(scrollObject.transform, false); // 스크롤 뷰포트 부모 연결
            Image viewportImage = viewportObject.GetComponent<Image>(); // 뷰포트 이미지 조회
            viewportImage.color = new Color(1f, 1f, 1f, 0.02f); // 뷰포트 투명 배경 적용
            Mask viewportMask = viewportObject.GetComponent<Mask>(); // 뷰포트 마스크 조회
            viewportMask.showMaskGraphic = false; // 뷰포트 마스크 그래픽 숨김
            Stretch(viewportObject.GetComponent<RectTransform>(), 8f); // 뷰포트 영역 확장
            GameObject contentObject = new GameObject("InventoryContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); // 장비 목록 컨텐츠 생성
            contentObject.transform.SetParent(viewportObject.transform, false); // 장비 목록 컨텐츠 부모 연결
            inventoryContent = contentObject.GetComponent<RectTransform>(); // 장비 목록 RectTransform 저장
            inventoryContent.anchorMin = new Vector2(0f, 1f); // 장비 목록 최소 앵커 설정
            inventoryContent.anchorMax = new Vector2(1f, 1f); // 장비 목록 최대 앵커 설정
            inventoryContent.pivot = new Vector2(0.5f, 1f); // 장비 목록 피벗 설정
            inventoryContent.anchoredPosition = Vector2.zero; // 장비 목록 위치 초기화
            inventoryContent.sizeDelta = Vector2.zero; // 장비 목록 크기 초기화
            VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>(); // 장비 목록 레이아웃 조회
            layout.padding = new RectOffset(6, 6, 6, 6); // 장비 목록 여백 설정
            layout.spacing = 7f; // 장비 목록 간격 설정
            layout.childAlignment = TextAnchor.UpperCenter; // 장비 목록 상단 정렬
            layout.childControlWidth = true; // 장비 버튼 너비 제어
            layout.childControlHeight = false; // 장비 버튼 높이 직접 사용
            layout.childForceExpandWidth = true; // 장비 버튼 가로 확장
            layout.childForceExpandHeight = false; // 장비 버튼 세로 확장 차단
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>(); // 장비 목록 크기 조절기 조회
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; // 장비 목록 가로 크기 유지
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 장비 목록 세로 크기 자동 조절
            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>(); // ScrollRect 조회
            scrollRect.viewport = viewportObject.GetComponent<RectTransform>(); // ScrollRect 뷰포트 연결
            scrollRect.content = inventoryContent; // ScrollRect 컨텐츠 연결
            scrollRect.horizontal = false; // 가로 스크롤 비활성화
            scrollRect.vertical = true; // 세로 스크롤 활성화
            scrollRect.movementType = ScrollRect.MovementType.Clamped; // 스크롤 범위 제한
            scrollRect.scrollSensitivity = 30f; // 스크롤 감도 설정
        }

        private void Refresh() // 캐릭터 및 장비 화면 갱신
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 컨텍스트 확인
            {
                SetStatus("Bootstrap → Title → Lobby 순서로 실행하고 저장 데이터를 생성해 주세요."); // 전역 데이터 누락 안내
                SetAction(false, "사용 불가"); // 장비 액션 비활성화
                return; // 화면 갱신 중단
            }

            if (saveData.Characters.Count == 0) // 보유 캐릭터 존재 확인
            {
                SetStatus("보유 캐릭터가 없습니다."); // 캐릭터 없음 안내
                SetAction(false, "사용 불가"); // 장비 액션 비활성화
                return; // 화면 갱신 중단
            }

            selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, saveData.Characters.Count - 1); // 선택 캐릭터 번호 보정
            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 저장 조회
            CharacterData characterData = dataManager.GetCharacter(characterSave.CharacterId); // 선택 캐릭터 원본 조회
            string displayName = characterData == null ? characterSave.CharacterId : characterData.DisplayName; // 캐릭터 표시 이름 결정
            characterNameText.text = displayName; // 캐릭터 이름 갱신
            characterLevelText.text = $"Lv. {characterSave.Level}"; // 캐릭터 레벨 갱신
            portraitImage.sprite = DialogueArtFactory.GetStanding(characterSave.CharacterId, string.Empty, out bool placeholder); // 일러스트 (정식 스탠딩 없으면 임시 실루엣, Day60)
            portraitImage.color = placeholder ? DialogueArtFactory.GetCharacterTint(characterSave.CharacterId) : Color.white; // 임시 실루엣은 캐릭터 색
            portraitText.text = placeholder ? displayName : string.Empty; // 임시 실루엣 위 이름
            goldText.text = GoldCurrencyService.GetGold(saveData).ToString("N0"); // 상단 골드 (Day60)
            UpdateAffinityDebugText(saveData, characterSave); // 호감도 디버그 텍스트 갱신 (Day43)
            if (affinityRewardPanel != null) affinityRewardPanel.Refresh(saveData, characterSave, displayName); // 호감도 보상 패널 갱신 (Day56 추가, Unity 객체 null 비교)
            if (giftPanel != null) giftPanel.Refresh(saveData, dataManager, characterSave, displayName); // 선물 패널 갱신 (Day57 추가)
            if (bondPanel != null) bondPanel.Refresh(saveData, characterSave, displayName); // 결속 패널 갱신 (Day59 추가)
            UpdateCurrentStats(saveData, dataManager, characterSave, characterData); // 현재 최종 능력치 갱신
            RefreshEquipmentSlots(saveData, dataManager, characterSave); // 일러스트 둘레 장비 5칸 갱신 (Day60)
            EnsureSelectedInstance(saveData); // 선택 장비 인스턴스 보정
            BuildInventoryButtons(saveData, dataManager, characterSave); // 보유 장비 목록 구성
            UpdateDetail(saveData, dataManager, characterSave); // 선택 장비 상세 갱신
            if (runeTab != null) runeTab.Refresh(saveData, dataManager, characterSave, characterData); // 룬 탭 갱신 (Day60, 숨겨져 있으면 생략)
            if (skillTab != null) skillTab.Refresh(characterData, characterSave); // 스킬 탭 갱신 (Day60)
            if (profileTab != null) profileTab.Refresh(characterData, characterSave); // 프로필 탭 갱신 (Day60)
        }

        private bool TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData) // 전역 장비 화면 컨텍스트 조회
        {
            dataManager = null; // 데이터 관리자 결과 초기화
            saveManager = null; // 저장 관리자 결과 초기화
            saveData = null; // 저장 데이터 결과 초기화

            if (GameManager.Instance == null) // 전역 게임 관리자 확인
            {
                return false; // 전역 컨텍스트 조회 실패
            }

            dataManager = GameManager.Instance.Data; // 전역 데이터 관리자 조회
            saveManager = GameManager.Instance.Save; // 전역 저장 관리자 조회
            saveData = saveManager == null ? null : saveManager.CurrentSave; // 현재 저장 데이터 조회
            return dataManager != null && dataManager.IsInitialized && saveManager != null && saveData != null; // 전역 컨텍스트 사용 가능 여부 반환
        }

        private void UpdateCurrentStats(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave, CharacterData characterData) // 현재 최종 능력치 갱신
        {
            if (characterData == null) // 캐릭터 원본 존재 확인
            {
                currentStatsText.text = "CharacterData를 찾을 수 없습니다."; // 캐릭터 원본 누락 표시
                return; // 현재 능력치 갱신 중단
            }

            if (!BattleEquipmentStatCalculator.TryCalculate(characterSave, saveData, dataManager, out BattleEquipmentStatBonus bonus, out string error)) // 현재 장비 보정 계산
            {
                currentStatsText.text = error; // 현재 장비 계산 오류 표시
                return; // 현재 능력치 갱신 중단
            }

            BattleStats stats = BattleStatsFactory.CreateCharacter(characterData, characterSave, "CHARACTER_SCREEN", bonus); // 현재 최종 능력치 생성
            currentStatsText.text = BuildStatsDescription(stats); // 현재 최종 능력치 표시
        }

        private void UpdateAffinityDebugText(SaveData saveData, CharacterSaveData characterSave) // 호감도 디버그 텍스트 갱신 (Day43)
        {
            if (affinityText == null) // 호감도 디버그 텍스트 존재 확인
            {
                return; // 호감도 디버그 텍스트 갱신 중단
            }

            int affinity = AffinityService.GetAffinity(saveData, characterSave.CharacterId); // 선택 캐릭터 호감도 조회
            AffinityTier tier = AffinityService.GetAffinityTier(saveData, characterSave.CharacterId); // 선택 캐릭터 호감도 등급 조회
            int equippedRunes = 0; // 장착 룬 수 (Day60)
            foreach (RuneInstanceSaveData rune in RuneService.GetEquipped(saveData, characterSave.CharacterId)) if (rune != null) equippedRunes++; // 장착 룬 집계
            bool canTalk = DialogueService.CanTalk(saveData, characterSave.CharacterId, out _); // 이번 시간대 대화 가능 여부
            affinityText.text = $"레벨  Lv.{characterSave.Level}   (경험치 {characterSave.Experience})\n\n호감도  {affinity}/{CharacterSaveData.MaxAffinity} · {GetAffinityTierLabel(tier)}\n결속  {characterSave.BondLevel}/{BondCatalog.MaxLevel} · 결속 자원 {BondService.GetResource(saveData)}/{BondCatalog.MaxResource}\n오늘 선물  {GiftService.GetGiftsGivenToday(saveData, characterSave.CharacterId)}/{GiftService.DailyGiftLimit}\n일상 대화  {(canTalk ? "<color=#2E8B57>가능</color>" : "이번 시간대 완료")}\n\n장착 룬  {equippedRunes}/{RuneCatalog.SlotCount}"; // 성장·관계 정보 (Day60 스텟 탭)
        }

        private void DecreaseAffinityDebug() // 호감도 디버그 감소 처리 (Day43)
        {
            ApplyAffinityDebugDelta(-10); // 호감도 10 감소 실행
        }

        private void IncreaseAffinityDebug() // 호감도 디버그 증가 처리 (Day43)
        {
            ApplyAffinityDebugDelta(10); // 호감도 10 증가 실행
        }

        private void ApplyAffinityDebugDelta(int delta) // 호감도 디버그 증감 공통 처리 (Day43)
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 컨텍스트 확인
            {
                SetStatus("저장 데이터를 찾을 수 없습니다."); // 전역 데이터 누락 안내
                return; // 호감도 디버그 변경 중단
            }

            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 저장 조회

            if (characterSave == null) // 선택 캐릭터 존재 확인
            {
                SetStatus("선택된 캐릭터가 없습니다."); // 선택 캐릭터 없음 안내
                return; // 호감도 디버그 변경 중단
            }

            AffinityService.AddAffinity(saveData, characterSave.CharacterId, delta); // 호감도 디버그 증감 적용
            bool saved = saveManager.SaveCurrent(); // 호감도 변경 즉시 저장
            SetStatus(saved ? "호감도를 변경하고 저장했습니다." : "호감도는 변경되었지만 저장에 실패했습니다."); // 호감도 변경 결과 표시
            Refresh(); // 호감도 변경 화면 갱신
        }

        private static string GetAffinityTierLabel(AffinityTier tier) => AffinityService.GetTierLabel(tier); // 호감도 등급 한글 라벨 변환 (Day43, Day56 공용 함수로 위임)

        private void BuildInventoryButtons(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave) // 보유 장비 목록 버튼 구성
        {
            for (int index = inventoryContent.childCount - 1; index >= 0; index--) // 기존 장비 버튼 역순 순회
            {
                Destroy(inventoryContent.GetChild(index).gameObject); // 기존 장비 버튼 제거 예약
            }

            inventoryCountText.text = $"{saveData.EquipmentInventory.Count}개"; // 보유 장비 개수 갱신

            for (int index = 0; index < saveData.EquipmentInventory.Count; index++) // 장비 인벤토리 순회
            {
                EquipmentInstanceSaveData instance = saveData.EquipmentInventory[index]; // 현재 장비 인스턴스 조회

                if (instance == null) // 장비 인스턴스 존재 확인
                {
                    continue; // 빈 장비 인스턴스 제외
                }

                EquipmentData equipment = dataManager.GetEquipment(instance.EquipmentId); // 장비 원본 데이터 조회

                if (equipmentFilter >= 0 && (equipment == null || (int)equipment.Slot != equipmentFilter)) // 슬롯 필터 확인 (Day60 추가)
                {
                    continue; // 다른 슬롯 장비 제외
                }

                string equipmentName = equipment == null ? instance.EquipmentId : equipment.DisplayName; // 장비 표시 이름 결정
                CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 장비 착용 캐릭터 조회
                string state = BuildEquipmentState(owner, characterSave); // 장비 착용 상태 문구 생성
                string slotName = equipment == null ? "?" : EquipmentSlotInfo.GetLabel(equipment.Slot); // 장비 슬롯 이름 (Day60 5칸 대응)
                string grade = equipment == null ? string.Empty : GetGradeStars(equipment.Grade); // 장비 등급 별 문구 생성
                string optionSummary = equipment == null ? "원본 데이터 없음" : BuildCompactStatDescription(equipment); // 장비 옵션 축약 문구 생성
                Button itemButton = CreateButton(inventoryContent, $"Equipment_{index}", $"[{slotName}] {grade} {equipmentName}\n{optionSummary}\n{state}", new Color(0.90f, 0.90f, 0.90f, 1f)); // 장비 목록 버튼 생성
                RectTransform itemRect = itemButton.GetComponent<RectTransform>(); // 장비 버튼 RectTransform 조회
                itemRect.sizeDelta = new Vector2(0f, 82f); // 장비 버튼 높이 설정
                LayoutElement layoutElement = itemButton.gameObject.AddComponent<LayoutElement>(); // 장비 버튼 레이아웃 요소 추가
                layoutElement.preferredHeight = 82f; // 장비 버튼 선호 높이 설정
                string capturedInstanceId = instance.InstanceId; // 장비 버튼 선택 ID 복사
                itemButton.onClick.AddListener(() => SelectEquipment(capturedInstanceId)); // 장비 선택 이벤트 연결

                if (string.Equals(selectedInstanceId, instance.InstanceId, StringComparison.Ordinal)) // 현재 선택 장비 여부 확인
                {
                    itemButton.GetComponent<Image>().color = new Color(0.75f, 0.86f, 0.94f, 1f); // 선택 장비 버튼 강조
                }
            }
        }

        private static string BuildEquipmentState(CharacterSaveData owner, CharacterSaveData selectedCharacter) // 장비 착용 상태 문구 생성
        {
            if (owner == null) // 장비 미착용 여부 확인
            {
                return "미착용"; // 미착용 상태 반환
            }

            if (selectedCharacter != null && string.Equals(owner.CharacterId, selectedCharacter.CharacterId, StringComparison.Ordinal)) // 현재 캐릭터 착용 여부 확인
            {
                return "현재 캐릭터 장착 중"; // 현재 캐릭터 착용 상태 반환
            }

            return $"{owner.CharacterId} 장착 중"; // 다른 캐릭터 착용 상태 반환
        }

        private void EnsureSelectedInstance(SaveData saveData) // 선택 장비 인스턴스 보정
        {
            if (!string.IsNullOrWhiteSpace(selectedInstanceId) && saveData.FindEquipmentInstance(selectedInstanceId) != null) // 기존 선택 장비 유효성 확인
            {
                return; // 기존 선택 장비 유지
            }

            selectedInstanceId = string.Empty; // 선택 장비 초기화
            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 저장 조회

            if (characterSave != null && !string.IsNullOrWhiteSpace(characterSave.Equipment.WeaponInstanceId)) // 현재 무기 장착 여부 확인
            {
                selectedInstanceId = characterSave.Equipment.WeaponInstanceId; // 현재 무기 우선 선택
                return; // 선택 장비 보정 종료
            }

            if (characterSave != null && !string.IsNullOrWhiteSpace(characterSave.Equipment.ArmorInstanceId)) // 현재 방어구 장착 여부 확인
            {
                selectedInstanceId = characterSave.Equipment.ArmorInstanceId; // 현재 방어구 선택
                return; // 선택 장비 보정 종료
            }

            if (saveData.EquipmentInventory.Count > 0 && saveData.EquipmentInventory[0] != null) // 첫 보유 장비 존재 확인
            {
                selectedInstanceId = saveData.EquipmentInventory[0].InstanceId; // 첫 보유 장비 선택
            }
        }

        private void UpdateDetail(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave) // 선택 장비 상세 정보 갱신
        {
            EquipmentInstanceSaveData instance = saveData.FindEquipmentInstance(selectedInstanceId); // 선택 장비 인스턴스 조회

            if (instance == null) // 선택 장비 존재 확인
            {
                detailTitleText.text = "장비를 선택하세요"; // 선택 장비 없음 제목 표시
                detailGradeText.text = string.Empty; // 선택 장비 없음 등급 초기화
                detailStatsText.text = "보유 장비를 선택하면 상세 정보가 표시됩니다."; // 선택 장비 없음 안내 표시
                comparisonText.text = "장비를 선택하면 예상 능력치가 표시됩니다."; // 선택 장비 없음 비교 안내 표시
                SetAction(false, "사용 불가"); // 장비 액션 비활성화
                return; // 상세 갱신 종료
            }

            EquipmentData equipment = dataManager.GetEquipment(instance.EquipmentId); // 선택 장비 원본 조회

            if (equipment == null) // 선택 장비 원본 존재 확인
            {
                detailTitleText.text = instance.EquipmentId; // 누락 장비 ID 표시
                detailGradeText.text = "?"; // 누락 장비 등급 표시
                detailStatsText.text = "EquipmentData를 찾을 수 없습니다."; // 장비 원본 누락 안내
                comparisonText.text = "비교 계산을 수행할 수 없습니다."; // 비교 계산 불가 안내
                SetAction(false, "사용 불가"); // 장비 액션 비활성화
                return; // 상세 갱신 종료
            }

            CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 선택 장비 착용 캐릭터 조회
            detailTitleText.text = equipment.DisplayName; // 장비 이름 상세 표시
            detailGradeText.text = GetGradeStars(equipment.Grade); // 장비 등급 상세 표시
            detailStatsText.text = $"슬롯: {GetSlotDisplayName(equipment.Slot)}\n상태: {BuildEquipmentState(owner, characterSave)}\n\n{BuildStatDescription(equipment)}\n\n{equipment.Description}"; // 장비 상세 정보 표시

            if (owner != null && !string.Equals(owner.CharacterId, characterSave.CharacterId, StringComparison.Ordinal)) // 다른 캐릭터 장착 여부 확인
            {
                comparisonText.text = $"{owner.CharacterId}가 장착 중인 장비입니다.\n현재 캐릭터에는 바로 장착할 수 없습니다."; // 다른 캐릭터 장착 비교 안내
                SetAction(false, "다른 캐릭터 장착 중"); // 다른 캐릭터 장비 액션 비활성화
                return; // 상세 갱신 종료
            }

            if (!CharacterEquipmentPreviewService.TryCreate(saveData, dataManager, characterSave.CharacterId, instance.InstanceId, out CharacterEquipmentPreview preview, out string error)) // 장비 변경 미리보기 계산
            {
                comparisonText.text = error; // 미리보기 계산 오류 표시
                SetAction(false, "사용 불가"); // 장비 액션 비활성화
                return; // 상세 갱신 종료
            }

            comparisonText.text = BuildComparisonDescription(preview.CurrentStats, preview.PreviewStats); // 변경 전후 능력치 비교 표시
            SetAction(true, preview.ActionLabel); // 장비 액션 활성화
        }

        private void ApplySelectedEquipmentAction() // 선택 장비 장착 또는 해제 처리
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 컨텍스트 확인
            {
                SetStatus("저장 데이터를 찾을 수 없습니다."); // 전역 데이터 누락 안내
                return; // 장비 액션 중단
            }

            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 저장 조회
            EquipmentInstanceSaveData instance = saveData.FindEquipmentInstance(selectedInstanceId); // 선택 장비 인스턴스 조회

            if (characterSave == null || instance == null) // 캐릭터 및 장비 존재 확인
            {
                SetStatus("장비 변경 대상을 찾을 수 없습니다."); // 변경 대상 누락 안내
                return; // 장비 액션 중단
            }

            EquipmentData equipment = dataManager.GetEquipment(instance.EquipmentId); // 선택 장비 원본 조회

            if (equipment == null) // 장비 원본 존재 확인
            {
                SetStatus($"EquipmentData 누락: {instance.EquipmentId}"); // 장비 원본 누락 안내
                return; // 장비 액션 중단
            }

            CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 현재 장비 착용 캐릭터 조회
            bool succeeded; // 장비 액션 성공 상태 선언
            string error; // 장비 액션 오류 문구 선언

            if (owner != null && string.Equals(owner.CharacterId, characterSave.CharacterId, StringComparison.Ordinal)) // 현재 캐릭터 장착 여부 확인
            {
                succeeded = CharacterEquipmentService.TryUnequip(saveData, characterSave.CharacterId, equipment.Slot, out EquipmentInstanceSaveData removedInstance, out error); // 현재 슬롯 장비 해제 실행
            }
            else // 미착용 장비 처리
            {
                succeeded = CharacterEquipmentService.TryEquip(saveData, dataManager, characterSave.CharacterId, instance.InstanceId, out error); // 장비 장착 또는 교체 실행
            }

            if (!succeeded) // 장비 액션 실패 확인
            {
                SetStatus(error); // 장비 액션 오류 표시
                return; // 장비 액션 종료
            }

            bool saved = saveManager.SaveCurrent(); // 장비 변경 즉시 저장
            SetStatus(saved ? "장비 변경을 저장했습니다." : "장비는 변경되었지만 저장에 실패했습니다."); // 장비 저장 결과 표시
            Refresh(); // 장비 변경 화면 갱신
        }

        private void GrantDemoEquipment() // 임시 테스트 장비 지급
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 컨텍스트 확인
            {
                SetStatus("저장 데이터를 찾을 수 없습니다."); // 전역 데이터 누락 안내
                return; // 테스트 장비 지급 중단
            }

            int addedCount = 0; // 신규 지급 장비 개수 초기화

            for (int index = 0; index < DemoEquipmentIds.Length; index++) // 임시 장비 ID 순회
            {
                string equipmentId = DemoEquipmentIds[index]; // 현재 임시 장비 ID 조회

                if (dataManager.GetEquipment(equipmentId) == null) // 임시 장비 원본 등록 확인
                {
                    continue; // 미등록 임시 장비 제외
                }

                if (saveData.GetEquipmentCount(equipmentId) > 0) // 동일 장비 보유 여부 확인
                {
                    continue; // 이미 보유한 테스트 장비 제외
                }

                if (saveData.TryCreateEquipmentInstance(equipmentId, out EquipmentInstanceSaveData createdInstance, out string error)) // 테스트 장비 인스턴스 생성
                {
                    addedCount++; // 신규 지급 장비 개수 증가
                }
                else // 테스트 장비 생성 실패 처리
                {
                    SetStatus(error); // 테스트 장비 생성 오류 표시
                }
            }

            bool saved = addedCount == 0 || saveManager.SaveCurrent(); // 신규 지급 시 저장 실행
            SetStatus(addedCount > 0 ? $"테스트 장비 {addedCount}개를 지급했습니다." : "지급할 새 테스트 장비가 없습니다."); // 테스트 장비 지급 결과 표시

            if (!saved) // 테스트 장비 저장 실패 확인
            {
                SetStatus("테스트 장비는 지급되었지만 저장에 실패했습니다."); // 테스트 장비 저장 실패 안내
            }

            Refresh(); // 테스트 장비 지급 화면 갱신
        }

        private CharacterSaveData GetSelectedCharacter(SaveData saveData) // 선택 캐릭터 저장 조회
        {
            if (saveData == null || saveData.Characters.Count == 0) // 캐릭터 저장 목록 확인
            {
                return null; // 선택 캐릭터 없음 반환
            }

            selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, saveData.Characters.Count - 1); // 선택 캐릭터 번호 보정
            return saveData.Characters[selectedCharacterIndex]; // 선택 캐릭터 저장 반환
        }

        private void SelectPreviousCharacter() // 이전 캐릭터 선택
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData) || saveData.Characters.Count == 0) // 캐릭터 목록 확인
            {
                return; // 이전 캐릭터 선택 중단
            }

            selectedCharacterIndex = (selectedCharacterIndex - 1 + saveData.Characters.Count) % saveData.Characters.Count; // 이전 캐릭터 번호 순환 계산
            selectedInstanceId = string.Empty; // 캐릭터 변경 선택 장비 초기화
            SetStatus("이전 캐릭터를 선택했습니다."); // 캐릭터 선택 상태 표시
            Refresh(); // 캐릭터 화면 갱신
        }

        private void SelectNextCharacter() // 다음 캐릭터 선택
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData) || saveData.Characters.Count == 0) // 캐릭터 목록 확인
            {
                return; // 다음 캐릭터 선택 중단
            }

            selectedCharacterIndex = (selectedCharacterIndex + 1) % saveData.Characters.Count; // 다음 캐릭터 번호 순환 계산
            selectedInstanceId = string.Empty; // 캐릭터 변경 선택 장비 초기화
            SetStatus("다음 캐릭터를 선택했습니다."); // 캐릭터 선택 상태 표시
            Refresh(); // 캐릭터 화면 갱신
        }

        private void SelectEquipment(string instanceId) // 장비 인벤토리 선택
        {
            selectedInstanceId = instanceId ?? string.Empty; // 선택 장비 인스턴스 저장
            SetStatus("선택 장비의 변경 전후 능력치를 계산했습니다."); // 장비 선택 상태 표시
            Refresh(); // 장비 상세 화면 갱신
        }

        private void SelectEquippedSlot(EquipmentSlot slot) // 현재 슬롯 장비 선택
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 컨텍스트 확인
            {
                return; // 슬롯 장비 선택 중단
            }

            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 저장 조회
            EquipmentInstanceSaveData instance = CharacterEquipmentService.GetEquippedInstance(saveData, characterSave.CharacterId, slot); // 현재 슬롯 장비 조회

            if (instance == null) // 현재 슬롯 장비 존재 확인
            {
                SetStatus($"{GetSlotDisplayName(slot)} 슬롯이 비어 있습니다."); // 빈 슬롯 안내
                return; // 슬롯 선택 중단
            }

            selectedInstanceId = instance.InstanceId; // 현재 슬롯 장비 선택
            SetStatus($"현재 {GetSlotDisplayName(slot)} 장비를 선택했습니다."); // 슬롯 선택 상태 표시
            Refresh(); // 장비 상세 화면 갱신
        }

        private void ReturnToLobby() // 로비 복귀
        {
            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                SetStatus("SceneLoader를 찾을 수 없습니다."); // 씬 로더 누락 안내
                return; // 로비 복귀 중단
            }

            GameManager.Instance.Scenes.LoadScene(GameScenes.Lobby); // 로비 씬 이동
        }

        private void SetAction(bool interactable, string label) // 장비 액션 상태 설정
        {
            actionButton.interactable = interactable; // 장비 액션 활성 상태 적용
            actionButtonText.text = label ?? string.Empty; // 장비 액션 문구 적용
        }

        private void SetStatus(string value) // 화면 상태 문구 설정
        {
            if (statusText != null) // 상태 텍스트 존재 확인
            {
                statusText.text = value ?? string.Empty; // 상태 문구 적용
            }
        }

        private static string BuildStatsDescription(BattleStats stats) // 현재 최종 능력치 문구 생성
        {
            if (stats == null) // 전투 능력치 존재 확인
            {
                return "능력치를 계산할 수 없습니다."; // 능력치 누락 문구 반환
            }

            return $"HP        {stats.MaxHp}\nATK       {stats.Attack}\nDEF       {stats.Defense}\nRES       {stats.Resistance}\n공격속도   {stats.AttackSpeed:0.00}\n명중률     {stats.Accuracy * 100f:0.#}%\n치명타율   {stats.CriticalRate * 100f:0.#}%\n사거리     {stats.AttackRange:0.00}\n이동속도   {stats.MoveSpeed:0.00}"; // 최종 능력치 문구 반환
        }

        private static string BuildComparisonDescription(BattleStats currentStats, BattleStats previewStats) // 변경 전후 능력치 비교 문구 생성
        {
            if (currentStats == null || previewStats == null) // 비교 능력치 존재 확인
            {
                return "비교 능력치를 계산할 수 없습니다."; // 비교 능력치 누락 문구 반환
            }

            string hp = BuildIntComparison("HP", currentStats.MaxHp, previewStats.MaxHp); // 최대 체력 비교 문구 생성
            string attack = BuildIntComparison("ATK", currentStats.Attack, previewStats.Attack); // 공격력 비교 문구 생성
            string defense = BuildIntComparison("DEF", currentStats.Defense, previewStats.Defense); // 방어력 비교 문구 생성
            string resistance = BuildIntComparison("RES", currentStats.Resistance, previewStats.Resistance); // 저항력 비교 문구 생성
            string attackSpeed = BuildFloatComparison("공격속도", currentStats.AttackSpeed, previewStats.AttackSpeed, false); // 공격속도 비교 문구 생성
            string accuracy = BuildFloatComparison("명중률", currentStats.Accuracy, previewStats.Accuracy, true); // 명중률 비교 문구 생성
            string criticalRate = BuildFloatComparison("치명타율", currentStats.CriticalRate, previewStats.CriticalRate, true); // 치명타율 비교 문구 생성
            string attackRange = BuildFloatComparison("사거리", currentStats.AttackRange, previewStats.AttackRange, false); // 공격 사거리 비교 문구 생성
            string moveSpeed = BuildFloatComparison("이동속도", currentStats.MoveSpeed, previewStats.MoveSpeed, false); // 이동속도 비교 문구 생성
            return $"{hp}\n{attack}\n{defense}\n{resistance}\n{attackSpeed}\n{accuracy}\n{criticalRate}\n{attackRange}\n{moveSpeed}"; // 전체 능력치 비교 문구 반환
        }

        private static string BuildIntComparison(string label, int currentValue, int previewValue) // 정수 능력치 비교 문구 생성
        {
            int delta = previewValue - currentValue; // 정수 변화량 계산
            string deltaText = delta == 0 ? "=" : delta > 0 ? $"+{delta}" : delta.ToString(); // 정수 변화량 문구 생성
            return $"{label,-6} {currentValue} → {previewValue}   ({deltaText})"; // 정수 비교 문구 반환
        }

        private static string BuildFloatComparison(string label, float currentValue, float previewValue, bool percentage) // 실수 능력치 비교 문구 생성
        {
            float multiplier = percentage ? 100f : 1f; // 백분율 배율 결정
            float currentDisplay = currentValue * multiplier; // 현재 표시값 계산
            float previewDisplay = previewValue * multiplier; // 예상 표시값 계산
            float delta = previewDisplay - currentDisplay; // 실수 변화량 계산
            string suffix = percentage ? "%" : string.Empty; // 실수 표시 접미사 결정
            string deltaText = Mathf.Abs(delta) < 0.0001f ? "=" : delta > 0f ? $"+{delta:0.##}{suffix}" : $"{delta:0.##}{suffix}"; // 실수 변화량 문구 생성
            return $"{label,-6} {currentDisplay:0.##}{suffix} → {previewDisplay:0.##}{suffix}   ({deltaText})"; // 실수 비교 문구 반환
        }

        private static string BuildStatDescription(EquipmentData equipment) // 장비 상세 능력치 문구 생성
        {
            if (equipment == null || equipment.StatOptions == null || equipment.StatOptions.Count == 0) // 장비 옵션 존재 확인
            {
                return "추가 능력치 없음"; // 장비 옵션 없음 문구 반환
            }

            string description = string.Empty; // 장비 옵션 문구 초기화

            for (int index = 0; index < equipment.StatOptions.Count; index++) // 장비 옵션 순회
            {
                EquipmentStatOption option = equipment.StatOptions[index]; // 현재 장비 옵션 조회
                string value = FormatStatValue(option.StatType, option.Value); // 장비 옵션 수치 문구 생성
                string separator = index == 0 ? string.Empty : "\n"; // 장비 옵션 줄바꿈 결정
                description += $"{separator}{GetStatDisplayName(option.StatType)}  {value}"; // 장비 옵션 문구 누적
            }

            return description; // 장비 상세 문구 반환
        }

        private static string BuildCompactStatDescription(EquipmentData equipment) // 장비 목록 축약 능력치 문구 생성
        {
            if (equipment == null || equipment.StatOptions == null || equipment.StatOptions.Count == 0) // 장비 옵션 존재 확인
            {
                return "옵션 없음"; // 장비 옵션 없음 문구 반환
            }

            string description = string.Empty; // 장비 축약 옵션 문구 초기화
            int count = Mathf.Min(2, equipment.StatOptions.Count); // 장비 축약 옵션 최대 개수 결정

            for (int index = 0; index < count; index++) // 장비 축약 옵션 순회
            {
                EquipmentStatOption option = equipment.StatOptions[index]; // 현재 장비 옵션 조회
                string separator = index == 0 ? string.Empty : " / "; // 장비 축약 옵션 구분자 결정
                description += $"{separator}{GetStatDisplayName(option.StatType)} {FormatStatValue(option.StatType, option.Value)}"; // 장비 축약 옵션 문구 누적
            }

            if (equipment.StatOptions.Count > count) // 추가 장비 옵션 존재 확인
            {
                description += $" 외 {equipment.StatOptions.Count - count}"; // 추가 옵션 개수 표시
            }

            return description; // 장비 축약 문구 반환
        }

        private static string FormatStatValue(EquipmentStatType statType, float value) // 장비 능력치 수치 문구 생성
        {
            bool percentage = statType == EquipmentStatType.Accuracy || statType == EquipmentStatType.CriticalRate; // 백분율 능력치 여부 계산
            float displayValue = percentage ? value * 100f : value; // 장비 능력치 표시값 계산
            string suffix = percentage ? "%" : string.Empty; // 장비 능력치 접미사 결정
            string sign = displayValue >= 0f ? "+" : string.Empty; // 양수 장비 능력치 부호 생성
            return $"{sign}{displayValue:0.##}{suffix}"; // 장비 능력치 수치 문구 반환
        }

        private static string GetStatDisplayName(EquipmentStatType statType) // 장비 능력치 표시 이름 조회
        {
            switch (statType) // 장비 능력치 유형 분기
            {
                case EquipmentStatType.MaxHp: // 최대 체력 유형 처리
                    return "HP"; // 최대 체력 이름 반환
                case EquipmentStatType.Attack: // 공격력 유형 처리
                    return "공격력"; // 공격력 이름 반환
                case EquipmentStatType.Defense: // 방어력 유형 처리
                    return "방어력"; // 방어력 이름 반환
                case EquipmentStatType.Resistance: // 저항력 유형 처리
                    return "저항력"; // 저항력 이름 반환
                case EquipmentStatType.AttackSpeed: // 공격속도 유형 처리
                    return "공격속도"; // 공격속도 이름 반환
                case EquipmentStatType.Accuracy: // 명중률 유형 처리
                    return "명중률"; // 명중률 이름 반환
                case EquipmentStatType.CriticalRate: // 치명타율 유형 처리
                    return "치명타율"; // 치명타율 이름 반환
                case EquipmentStatType.AttackRange: // 공격 사거리 유형 처리
                    return "사거리"; // 공격 사거리 이름 반환
                case EquipmentStatType.MoveSpeed: // 이동속도 유형 처리
                    return "이동속도"; // 이동속도 이름 반환
                default: // 알 수 없는 유형 처리
                    return statType.ToString(); // 열거형 이름 반환
            }
        }

        private static string GetSlotDisplayName(EquipmentSlot slot) // 장비 슬롯 표시 이름 조회
        {
            return EquipmentSlotInfo.GetLabel(slot); // 장비 슬롯 이름 반환 (Day60 5칸 대응)
        }

        private static string GetGradeStars(ItemGrade grade) // 장비 등급 별 문구 생성
        {
            int starCount = Mathf.Clamp((int)grade + 1, 1, 5); // 장비 등급 별 개수 계산
            return new string('★', starCount) + new string('☆', 5 - starCount); // 장비 등급 별 문구 반환
        }

        private static Image CreateImage(Transform parent, string name, Color color) => RuntimeUiKit.CreateImage(parent, name, color); // 공통 UI 이미지 생성 (RuntimeUiKit 위임, 최적화 정리)

        private static Text CreateText(Transform parent, string name, string value, int fontSize, FontStyle fontStyle, Color color) // 공통 UI 텍스트 생성 (RuntimeUiKit 위임)
        {
            return RuntimeUiKit.CreateText(parent, name, value, fontSize, color, fontStyle); // 기본 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color) // 공통 UI 버튼 생성 (RuntimeUiKit 위임, 기존과 같이 색 전환 대상 미연결)
        {
            Button button = RuntimeUiKit.CreateButton(parent, name, color, false); // 버튼 생성
            Text text = CreateText(button.transform, "Text", label, 16, FontStyle.Bold, new Color(0.10f, 0.10f, 0.10f, 1f)).Wrap(); // 줄바꿈 버튼 라벨 생성
            Stretch(text.rectTransform, 7f); // 라벨 확장
            return button; // 버튼 반환
        }

        private static void AddOutline(GameObject target) // UI 외곽선 추가
        {
            Outline outline = target.AddComponent<Outline>(); // UI 외곽선 컴포넌트 추가
            outline.effectColor = new Color(0.25f, 0.25f, 0.25f, 0.35f); // UI 외곽선 색상 설정
            outline.effectDistance = new Vector2(1f, -1f); // UI 외곽선 거리 설정
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax) => RuntimeUiKit.SetRect(rectTransform, anchorMin, anchorMax); // 앵커 기반 UI 배치 (RuntimeUiKit 위임, 최적화 정리)

        private static void Stretch(RectTransform rectTransform, float padding = 0f) => RuntimeUiKit.Stretch(rectTransform, padding); // 부모 전체 영역 확장 (RuntimeUiKit 위임, 최적화 정리)
    }
}
