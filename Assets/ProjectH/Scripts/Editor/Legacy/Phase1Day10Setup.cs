using ProjectH.SaveSystem; // 저장 데이터 기능
using ProjectH.UI; // 파티 UI 기능
using UnityEditor; // Unity 에디터 기능
using UnityEditor.SceneManagement; // 에디터 씬 관리
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // UI 이벤트 시스템 기능
using UnityEngine.InputSystem.UI; // 입력 시스템 UI 기능
using UnityEngine.SceneManagement; // 씬 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day10Setup // 10일차 파티 편성 Scene 설정 도구
    {
        private const string PartyScenePath = "Assets/ProjectH/Scenes/Party.unity"; // 파티 씬 경로
        private const string ArtRoot = "Assets/ProjectH/UI/Art/Prototype"; // 프로토타입 UI 아트 경로
        private static readonly Color Cream = new Color(0.98f, 0.96f, 0.91f, 0.97f); // 크림 패널색
        private static readonly Color Navy = new Color(0.16f, 0.25f, 0.39f, 1f); // 남색 텍스트
        private static readonly Color SoftText = new Color(0.30f, 0.34f, 0.40f, 1f); // 부드러운 본문색
        private static readonly Color Overlay = new Color(0.04f, 0.07f, 0.12f, 0.72f); // 팝업 오버레이색

        [MenuItem("Tools/Project H/Phase 1/10일차 파티 편성 Scene 재구성")] // 파티 Scene 재구성 메뉴 등록
        public static void Setup() // 10일차 파티 Scene 구축
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 빈 파티 씬 생성
            CreateMainCamera(); // 메인 카메라 생성
            CreateEventSystem(); // UI 입력 이벤트 시스템 생성
            Canvas canvas = CreateCanvas(); // 기준 해상도 Canvas 생성
            CreateBackground(canvas.transform); // 파티 배경 생성
            PartyScreenController controller = canvas.gameObject.AddComponent<PartyScreenController>(); // 파티 전용 컨트롤러 추가
            Text statusText = CreateHeader(canvas.transform, out Button lobbyButton, out Button helpButton); // 상단 헤더 생성
            GameObject helpPanel = CreateHelpPanel(canvas.transform); // 도움말 패널 생성
            PartySlotView[] slots = CreatePartySlots(canvas.transform); // 4인 편성 슬롯 생성
            Button[] presetButtons = CreatePresetBar(canvas.transform, out Text presetStateText); // 편성 프리셋 바 생성
            Button confirmButton = CreateButton(canvas.transform, "ConfirmButton", "편성 확정", "button_primary.png", 27); // 편성 확정 버튼 생성
            SetRect(confirmButton.GetComponent<RectTransform>(), new Vector2(0.355f, 0.035f), new Vector2(0.495f, 0.115f)); // 편성 확정 버튼 중앙 배치
            Button dungeonButton = CreateButton(canvas.transform, "DungeonButton", "던전 선택", "button_secondary.png", 25); // 던전 선택 버튼 생성
            SetRect(dungeonButton.GetComponent<RectTransform>(), new Vector2(0.505f, 0.035f), new Vector2(0.645f, 0.115f)); // 던전 선택 버튼 중앙 배치
            CreateCharacterPopup(canvas.transform, out GameObject popupRoot, out Text popupTitle, out Text popupStatus, out Transform rosterContent, out PartyCharacterCardView cardTemplate, out Button[] filterButtons, out Button clearButton, out Button cancelButton); // 캐릭터 선택 팝업 생성
            controller.Configure(slots, presetButtons, statusText, presetStateText, confirmButton, lobbyButton, dungeonButton, helpButton, helpPanel, popupRoot, popupTitle, popupStatus, rosterContent, cardTemplate, filterButtons, clearButton, cancelButton); // 파티 컨트롤러 참조 연결
            EditorUtility.SetDirty(controller); // 파티 컨트롤러 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // 파티 씬 변경 표시
            EditorSceneManager.SaveScene(scene, PartyScenePath); // 파티 씬 저장
            AssetDatabase.SaveAssets(); // 에셋 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][UI] Phase 1 Day 10 Party scene rebuild complete."); // 파티 Scene 구축 완료 로그
        }

        private static Canvas CreateCanvas() // 기준 Canvas 생성
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // Canvas 객체 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 렌더링 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // Canvas Scaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로세로 비율 대응
            scaler.matchWidthOrHeight = 0.5f; // 화면 대응 균형 설정
            return canvas; // Canvas 반환
        }


        private static void CreateMainCamera() // 파티 씬 메인 카메라 생성
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); // 메인 카메라 객체 생성
            cameraObject.tag = "MainCamera"; // 메인 카메라 태그 설정
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 2D 카메라 위치 설정
            Camera camera = cameraObject.GetComponent<Camera>(); // 카메라 컴포넌트 조회
            camera.orthographic = true; // 2D 직교 카메라 설정
            camera.orthographicSize = 5f; // 직교 카메라 크기 설정
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 초기화 설정
            camera.backgroundColor = new Color(0.94f, 0.93f, 0.90f, 1f); // 카메라 배경색 설정
            camera.depth = -10f; // UI 뒤 카메라 깊이 설정
        }

        private static void CreateEventSystem() // UI EventSystem 생성
        {
            GameObject eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); // EventSystem 객체 생성
            InputSystemUIInputModule inputModule = eventObject.GetComponent<InputSystemUIInputModule>(); // 입력 모듈 조회
            inputModule.AssignDefaultActions(); // 기본 UI 입력 액션 연결
        }

        private static void CreateBackground(Transform parent) // 파티 화면 배경 생성
        {
            Image background = CreateImage(parent, "Background", LoadSprite("bg_party.png"), Color.white); // 파티 배경 이미지 생성
            SetRect(background.rectTransform, Vector2.zero, Vector2.one); // 배경 전체 화면 확장
            Image wash = CreateImage(parent, "BackgroundWash", null, new Color(0.96f, 0.94f, 0.89f, 0.56f)); // 배경 밝기 보정 패널 생성
            SetRect(wash.rectTransform, Vector2.zero, Vector2.one); // 배경 보정 전체 확장
        }

        private static Text CreateHeader(Transform parent, out Button lobbyButton, out Button helpButton) // 편성 상단 헤더 생성
        {
            Image header = CreateImage(parent, "FormationHeader", LoadSprite("Frames/frame_topbar.png"), Cream); // 편성 상단바 생성
            SetRect(header.rectTransform, new Vector2(0.025f, 0.865f), new Vector2(0.975f, 0.985f)); // 상단바 배치
            Image titleBanner = CreateImage(header.transform, "TitleBanner", LoadSprite("Buttons/button_primary.png"), new Color(0.72f, 0.84f, 1f, 1f)); // 편성 제목 배너 생성
            SetRect(titleBanner.rectTransform, new Vector2(0.01f, 0.12f), new Vector2(0.20f, 0.88f)); // 제목 배너 배치
            Text title = CreateText(titleBanner.transform, "Title", "편성", 40, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 편성 제목 생성
            Stretch(title.rectTransform, 8f); // 제목 영역 확장
            Text description = CreateText(header.transform, "Description", "탐험에 출전할 4명의 캐릭터를 구성하세요. 역할과 활력, 스킬 시너지를 고려해 전략적인 파티를 만드세요.", 21, FontStyle.Normal, SoftText, TextAnchor.MiddleCenter); // 편성 설명 생성
            SetRect(description.rectTransform, new Vector2(0.205f, 0.18f), new Vector2(0.665f, 0.82f)); // 편성 설명 중앙 배치
            lobbyButton = CreateButton(header.transform, "LobbyButton", "로비", "button_small.png", 20); // 로비 이동 버튼 생성
            SetRect(lobbyButton.GetComponent<RectTransform>(), new Vector2(0.665f, 0.23f), new Vector2(0.73f, 0.77f)); // 로비 버튼 배치
            helpButton = CreateButton(header.transform, "HelpButton", "?", "button_small.png", 28); // 도움말 버튼 생성
            SetRect(helpButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.23f), new Vector2(0.785f, 0.77f)); // 도움말 버튼 배치
            Image currency = CreateImage(header.transform, "CurrencyPanel", LoadSprite("Buttons/button_small.png"), Color.white); // 재화 패널 생성
            SetRect(currency.rectTransform, new Vector2(0.795f, 0.23f), new Vector2(0.90f, 0.77f)); // 재화 패널 배치
            Text currencyText = CreateText(currency.transform, "CurrencyText", "◆  10000", 22, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 재화 텍스트 생성
            Stretch(currencyText.rectTransform, 6f); // 재화 텍스트 확장
            Button settings = CreateButton(header.transform, "SettingsButton", "SET", "button_small.png", 17); // 설정 자리 버튼 생성
            SetRect(settings.GetComponent<RectTransform>(), new Vector2(0.915f, 0.23f), new Vector2(0.96f, 0.77f)); // 설정 버튼 배치
            settings.interactable = false; // 미구현 설정 버튼 비활성화
            Text status = CreateText(parent, "PartyStatus", string.Empty, 20, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 파티 상태 텍스트 생성
            SetRect(status.rectTransform, new Vector2(0.16f, 0.815f), new Vector2(0.84f, 0.855f)); // 파티 상태 텍스트 중앙 배치
            return status; // 파티 상태 텍스트 반환
        }

        private static GameObject CreateHelpPanel(Transform parent) // 편성 도움말 패널 생성
        {
            Image panel = CreateImage(parent, "HelpPanel", LoadSprite("Frames/frame_panel.png"), Cream); // 도움말 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.55f, 0.68f), new Vector2(0.95f, 0.84f)); // 도움말 패널 배치
            Text text = CreateText(panel.transform, "HelpText", "캐릭터 슬롯을 누르면 보유 캐릭터 선택 창이 열립니다.\n편성 #1~#4를 각각 저장할 수 있으며, 현재 선택한 편성이 전투에 사용됩니다.", 19, FontStyle.Normal, SoftText, TextAnchor.MiddleLeft); // 도움말 문구 생성
            Stretch(text.rectTransform, 22f); // 도움말 문구 확장
            panel.gameObject.SetActive(false); // 도움말 패널 초기 숨김
            return panel.gameObject; // 도움말 패널 반환
        }

        private static PartySlotView[] CreatePartySlots(Transform parent) // 4인 편성 슬롯 생성
        {
            PartySlotView[] slots = new PartySlotView[SaveData.MaxPartySize]; // 파티 슬롯 배열 생성

            for (int index = 0; index < slots.Length; index++) // 파티 슬롯 순회
            {
                float minX = 0.055f + index * 0.235f; // 슬롯 시작 X 계산
                float maxX = minX + 0.185f; // 슬롯 종료 X 계산
                GameObject slotObject = new GameObject($"PartySlot_{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PartySlotView)); // 슬롯 객체 생성
                slotObject.transform.SetParent(parent, false); // 슬롯 Canvas 연결
                Image slotImage = slotObject.GetComponent<Image>(); // 슬롯 이미지 조회
                slotImage.sprite = LoadSprite("Frames/frame_party_slot.png"); // 파티 슬롯 프레임 적용
                slotImage.color = Color.white; // 슬롯 이미지색 적용
                Button slotButton = slotObject.GetComponent<Button>(); // 슬롯 버튼 조회
                slotButton.targetGraphic = slotImage; // 슬롯 버튼 대상 이미지 설정
                SetRect(slotObject.GetComponent<RectTransform>(), new Vector2(minX, 0.285f), new Vector2(maxX, 0.79f)); // 슬롯 메인 영역 배치
                Image portrait = CreateImage(slotObject.transform, "PortraitFrame", LoadSprite("Frames/frame_portrait.png"), new Color(0.93f, 0.95f, 0.99f, 0.82f)); // 슬롯 초상화 프레임 생성
                SetRect(portrait.rectTransform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.88f)); // 초상화 영역 배치
                Text portraitText = CreateText(portrait.transform, "PortraitText", "EMPTY SLOT", 28, FontStyle.Bold, new Color(0.43f, 0.50f, 0.62f, 0.85f), TextAnchor.MiddleCenter); // 임시 초상화 텍스트 생성
                Stretch(portraitText.rectTransform, 16f); // 초상화 텍스트 확장
                Image badge = CreateImage(slotObject.transform, "RoleBadge", null, new Color(0.55f, 0.60f, 0.67f, 1f)); // 역할 배지 생성
                SetRect(badge.rectTransform, new Vector2(0.33f, 0.84f), new Vector2(0.67f, 0.98f)); // 역할 배지 중앙 배치
                Text roleText = CreateText(badge.transform, "RoleText", "+", 18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 역할 텍스트 생성
                Stretch(roleText.rectTransform, 3f); // 역할 텍스트 확장
                Image plate = CreateImage(slotObject.transform, "InfoPlate", LoadSprite("Buttons/button_small.png"), Cream); // 캐릭터 정보판 생성
                SetRect(plate.rectTransform, new Vector2(0.07f, 0.07f), new Vector2(0.93f, 0.21f)); // 정보판 배치
                Text levelText = CreateText(plate.transform, "LevelText", "--", 20, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 레벨 텍스트 생성
                SetRect(levelText.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.32f, 0.92f)); // 레벨 텍스트 중앙 배치
                Text nameText = CreateText(plate.transform, "NameText", "캐릭터 선택", 23, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 캐릭터 이름 생성
                SetRect(nameText.rectTransform, new Vector2(0.33f, 0.08f), new Vector2(0.97f, 0.92f)); // 캐릭터 이름 중앙 배치
                Text hintText = CreateText(slotObject.transform, "HintText", "클릭하여 추가", 16, FontStyle.Normal, SoftText, TextAnchor.MiddleCenter); // 슬롯 안내 생성
                SetRect(hintText.rectTransform, new Vector2(0.10f, 0.005f), new Vector2(0.90f, 0.065f)); // 슬롯 안내 배치
                PartySlotView view = slotObject.GetComponent<PartySlotView>(); // 슬롯 뷰 컴포넌트 조회
                view.Configure(slotButton, badge, roleText, portraitText, levelText, nameText, hintText); // 슬롯 뷰 참조 연결
                slots[index] = view; // 슬롯 배열 등록
            }

            return slots; // 파티 슬롯 배열 반환
        }

        private static Button[] CreatePresetBar(Transform parent, out Text presetStateText) // 편성 프리셋 바 생성
        {
            Image bar = CreateImage(parent, "PresetBar", LoadSprite("Frames/frame_panel.png"), Cream); // 프리셋 패널 생성
            SetRect(bar.rectTransform, new Vector2(0.045f, 0.135f), new Vector2(0.97f, 0.265f)); // 프리셋 패널 배치
            Button[] buttons = new Button[SaveData.PartyPresetCount]; // 프리셋 버튼 배열 생성

            const float buttonWidth = 0.16f; // 프리셋 버튼 너비
            const float buttonGap = 0.025f; // 프리셋 버튼 간격
            float groupWidth = (buttonWidth * buttons.Length) + (buttonGap * (buttons.Length - 1)); // 프리셋 버튼 그룹 너비 계산
            float groupStart = (1f - groupWidth) * 0.5f; // 프리셋 버튼 그룹 중앙 시작점 계산

            for (int index = 0; index < buttons.Length; index++) // 프리셋 버튼 순회
            {
                float minX = groupStart + index * (buttonWidth + buttonGap); // 프리셋 중앙 정렬 X 계산
                Button button = CreateButton(bar.transform, $"PresetButton_{index + 1}", $"편성 #{index + 1}", "button_nav.png", 23); // 편성 프리셋 버튼 생성
                SetRect(button.GetComponent<RectTransform>(), new Vector2(minX, 0.30f), new Vector2(minX + buttonWidth, 0.86f)); // 프리셋 버튼 중앙 배치
                buttons[index] = button; // 프리셋 버튼 배열 등록
            }

            presetStateText = CreateText(bar.transform, "PresetState", "편성 #1 · 저장됨", 16, FontStyle.Bold, SoftText, TextAnchor.MiddleCenter); // 프리셋 상태 텍스트 생성
            SetRect(presetStateText.rectTransform, new Vector2(0.34f, 0.02f), new Vector2(0.66f, 0.27f)); // 프리셋 상태 텍스트 중앙 배치
            return buttons; // 프리셋 버튼 배열 반환
        }

        private static void CreateCharacterPopup(Transform parent, out GameObject popupRoot, out Text popupTitle, out Text popupStatus, out Transform rosterContent, out PartyCharacterCardView cardTemplate, out Button[] filterButtons, out Button clearButton, out Button cancelButton) // 캐릭터 선택 팝업 생성
        {
            Image overlay = CreateImage(parent, "CharacterSelectPopup", null, Overlay); // 전체 화면 팝업 오버레이 생성
            SetRect(overlay.rectTransform, Vector2.zero, Vector2.one); // 팝업 오버레이 전체 확장
            overlay.raycastTarget = true; // 팝업 외부 입력 차단
            popupRoot = overlay.gameObject; // 팝업 루트 반환
            Image panel = CreateImage(overlay.transform, "PopupPanel", LoadSprite("Frames/frame_panel.png"), Cream); // 캐릭터 선택 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.92f)); // 팝업 패널 배치
            popupTitle = CreateText(panel.transform, "PopupTitle", "캐릭터 선택", 34, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 팝업 제목 생성
            SetRect(popupTitle.rectTransform, new Vector2(0.045f, 0.88f), new Vector2(0.48f, 0.96f)); // 팝업 제목 배치
            popupStatus = CreateText(panel.transform, "PopupStatus", string.Empty, 18, FontStyle.Normal, SoftText, TextAnchor.MiddleRight); // 팝업 상태 생성
            SetRect(popupStatus.rectTransform, new Vector2(0.52f, 0.88f), new Vector2(0.95f, 0.96f)); // 팝업 상태 배치
            filterButtons = new Button[4]; // 역할 필터 버튼 배열 생성
            string[] filterLabels = { "전체", "탱커", "딜러", "힐러" }; // 역할 필터 라벨 목록

            for (int index = 0; index < filterButtons.Length; index++) // 역할 필터 버튼 순회
            {
                float minX = 0.255f + index * 0.125f; // 필터 중앙 정렬 X 계산
                Button filter = CreateButton(panel.transform, $"FilterButton_{index}", filterLabels[index], "button_nav.png", 20); // 역할 필터 버튼 생성
                SetRect(filter.GetComponent<RectTransform>(), new Vector2(minX, 0.80f), new Vector2(minX + 0.105f, 0.865f)); // 역할 필터 버튼 배치
                filterButtons[index] = filter; // 필터 버튼 배열 등록
            }

            GameObject scrollObject = new GameObject("CharacterScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect)); // 캐릭터 스크롤 객체 생성
            scrollObject.transform.SetParent(panel.transform, false); // 캐릭터 스크롤 패널 연결
            Image scrollBackground = scrollObject.GetComponent<Image>(); // 스크롤 배경 이미지 조회
            scrollBackground.color = new Color(0.96f, 0.97f, 0.99f, 0.65f); // 스크롤 배경색 적용
            SetRect(scrollObject.GetComponent<RectTransform>(), new Vector2(0.045f, 0.16f), new Vector2(0.955f, 0.775f)); // 스크롤 영역 배치
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); // 스크롤 Viewport 생성
            viewportObject.transform.SetParent(scrollObject.transform, false); // Viewport 스크롤 연결
            Image viewportImage = viewportObject.GetComponent<Image>(); // Viewport 이미지 조회
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f); // Viewport 투명 배경 적용
            Mask viewportMask = viewportObject.GetComponent<Mask>(); // Viewport Mask 조회
            viewportMask.showMaskGraphic = false; // Viewport Mask 그래픽 숨김
            Stretch(viewportObject.GetComponent<RectTransform>(), 8f); // Viewport 영역 확장
            GameObject contentObject = new GameObject("RosterContent", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter)); // 캐릭터 Grid 컨테이너 생성
            contentObject.transform.SetParent(viewportObject.transform, false); // Grid Viewport 연결
            RectTransform contentRect = contentObject.GetComponent<RectTransform>(); // Grid RectTransform 조회
            contentRect.anchorMin = new Vector2(0f, 1f); // Grid 최소 앵커 설정
            contentRect.anchorMax = new Vector2(1f, 1f); // Grid 최대 앵커 설정
            contentRect.pivot = new Vector2(0.5f, 1f); // Grid 피벗 설정
            contentRect.anchoredPosition = Vector2.zero; // Grid 위치 초기화
            contentRect.sizeDelta = Vector2.zero; // Grid 크기 초기화
            GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>(); // Grid Layout 조회
            grid.cellSize = new Vector2(250f, 180f); // 캐릭터 카드 크기 설정
            grid.spacing = new Vector2(18f, 18f); // 캐릭터 카드 간격 설정
            grid.padding = new RectOffset(20, 20, 20, 20); // Grid 내부 여백 설정
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 개수 설정
            grid.constraintCount = 5; // 캐릭터 카드 5열 설정
            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>(); // 콘텐츠 크기 조절 컴포넌트 조회
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // Grid 높이 자동 조정
            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>(); // ScrollRect 조회
            scroll.viewport = viewportObject.GetComponent<RectTransform>(); // ScrollRect Viewport 연결
            scroll.content = contentRect; // ScrollRect Content 연결
            scroll.horizontal = false; // 가로 스크롤 비활성화
            scroll.vertical = true; // 세로 스크롤 활성화
            scroll.movementType = ScrollRect.MovementType.Clamped; // 스크롤 범위 제한
            rosterContent = contentObject.transform; // 캐릭터 Grid 컨테이너 반환
            cardTemplate = CreateCharacterCardTemplate(contentObject.transform); // 캐릭터 카드 템플릿 생성
            clearButton = CreateButton(panel.transform, "ClearSlotButton", "이 슬롯 비우기", "button_secondary.png", 21); // 슬롯 비우기 버튼 생성
            SetRect(clearButton.GetComponent<RectTransform>(), new Vector2(0.63f, 0.055f), new Vector2(0.79f, 0.125f)); // 슬롯 비우기 버튼 배치
            cancelButton = CreateButton(panel.transform, "CancelPopupButton", "취소", "button_small.png", 21); // 팝업 취소 버튼 생성
            SetRect(cancelButton.GetComponent<RectTransform>(), new Vector2(0.81f, 0.055f), new Vector2(0.94f, 0.125f)); // 팝업 취소 버튼 배치
            popupRoot.SetActive(false); // 캐릭터 선택 팝업 초기 숨김
        }

        private static PartyCharacterCardView CreateCharacterCardTemplate(Transform parent) // 캐릭터 선택 카드 템플릿 생성
        {
            GameObject cardObject = new GameObject("CharacterCardTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PartyCharacterCardView)); // 캐릭터 카드 객체 생성
            cardObject.transform.SetParent(parent, false); // 캐릭터 Grid 연결
            Image cardImage = cardObject.GetComponent<Image>(); // 카드 이미지 조회
            cardImage.sprite = LoadSprite("Frames/frame_party_slot.png"); // 파티 슬롯 프레임 적용
            cardImage.color = Color.white; // 카드 기본색 설정
            Button cardButton = cardObject.GetComponent<Button>(); // 카드 버튼 조회
            cardButton.targetGraphic = cardImage; // 카드 버튼 대상 이미지 설정
            Image badge = CreateImage(cardObject.transform, "RoleBadge", null, new Color(0.55f, 0.60f, 0.67f, 1f)); // 카드 역할 배지 생성
            SetRect(badge.rectTransform, new Vector2(0.04f, 0.75f), new Vector2(0.35f, 0.94f)); // 카드 역할 배지 배치
            Text roleText = CreateText(badge.transform, "RoleText", "ROLE", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); // 카드 역할 텍스트 생성
            Stretch(roleText.rectTransform, 3f); // 카드 역할 텍스트 확장
            Text portraitText = CreateText(cardObject.transform, "PortraitText", "PORTRAIT", 20, FontStyle.Bold, new Color(0.40f, 0.48f, 0.60f, 1f), TextAnchor.MiddleCenter); // 카드 초상화 자리 생성
            SetRect(portraitText.rectTransform, new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.74f)); // 카드 초상화 자리 배치
            Text levelText = CreateText(cardObject.transform, "LevelText", "LV.1", 16, FontStyle.Bold, Navy, TextAnchor.MiddleLeft); // 카드 레벨 생성
            SetRect(levelText.rectTransform, new Vector2(0.06f, 0.22f), new Vector2(0.35f, 0.36f)); // 카드 레벨 배치
            Text nameText = CreateText(cardObject.transform, "NameText", "이름", 18, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 카드 이름 생성
            SetRect(nameText.rectTransform, new Vector2(0.34f, 0.22f), new Vector2(0.94f, 0.36f)); // 카드 이름 배치
            Text stateText = CreateText(cardObject.transform, "StateText", "선택 가능", 14, FontStyle.Normal, SoftText, TextAnchor.MiddleCenter); // 카드 상태 생성
            SetRect(stateText.rectTransform, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.19f)); // 카드 상태 배치
            PartyCharacterCardView view = cardObject.GetComponent<PartyCharacterCardView>(); // 캐릭터 카드 뷰 조회
            view.ConfigureReferences(cardButton, badge, roleText, portraitText, levelText, nameText, stateText); // 카드 뷰 참조 연결
            cardObject.SetActive(false); // 카드 템플릿 숨김
            return view; // 카드 템플릿 반환
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color) // 공통 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // 부모 객체 연결
            Image image = imageObject.GetComponent<Image>(); // Image 컴포넌트 조회
            image.sprite = sprite; // 이미지 Sprite 설정
            image.color = color; // 이미지 색상 설정
            image.raycastTarget = false; // 기본 이미지 입력 비활성화
            return image; // Image 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color, TextAnchor alignment) // 공통 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow)); // 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // 부모 객체 연결
            Text text = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 설정
            text.text = value; // 텍스트 내용 설정
            text.fontSize = size; // 텍스트 크기 설정
            text.fontStyle = style; // 텍스트 스타일 설정
            text.color = color; // 텍스트 색상 설정
            text.alignment = alignment; // 텍스트 정렬 설정
            text.alignByGeometry = true; // 글리프 기준 중앙 정렬 사용
            text.lineSpacing = 1f; // 텍스트 줄 간격 통일
            text.resizeTextForBestFit = true; // 자동 텍스트 크기 사용
            text.resizeTextMinSize = 11; // 최소 텍스트 크기 설정
            text.resizeTextMaxSize = size; // 최대 텍스트 크기 설정
            text.raycastTarget = false; // 텍스트 입력 비활성화
            return text; // Text 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, string spriteFile, int fontSize) // 공통 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // 부모 객체 연결
            Image image = buttonObject.GetComponent<Image>(); // 버튼 이미지 조회
            image.sprite = LoadSprite("Buttons/" + spriteFile); // 버튼 Sprite 설정
            image.color = Color.white; // 버튼 기본색 설정
            Button button = buttonObject.GetComponent<Button>(); // Button 컴포넌트 조회
            button.targetGraphic = image; // 버튼 대상 그래픽 설정
            Text text = CreateText(buttonObject.transform, "Label", label, fontSize, FontStyle.Bold, Navy, TextAnchor.MiddleCenter); // 버튼 라벨 생성
            Stretch(text.rectTransform, 6f); // 버튼 라벨 확장
            return button; // Button 반환
        }

        private static Sprite LoadSprite(string relativePath) // UI Sprite 로드
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{relativePath}"); // 프로젝트 UI Sprite 반환
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) // RectTransform 앵커 설정
        {
            rect.anchorMin = min; // 최소 앵커 설정
            rect.anchorMax = max; // 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        private static void Stretch(RectTransform rect, float padding) // RectTransform 전체 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 내부 여백 설정
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 내부 여백 설정
        }
    }
}
