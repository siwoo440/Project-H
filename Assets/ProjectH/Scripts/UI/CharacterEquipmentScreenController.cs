using System; // 문자열 비교 기능
using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Data; // 캐릭터 및 장비 데이터 기능
using ProjectH.SaveSystem; // 저장 및 장착 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // Unity UI 입력 기능
using UnityEngine.InputSystem.UI; // 신규 Input System UI 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 캐릭터 화면 방지
    public sealed class CharacterEquipmentScreenController : MonoBehaviour // 임시 캐릭터 상세 장비 화면
    {
        private static readonly string[] DemoEquipmentIds = // 임시 테스트 장비 ID 목록
        {
            "EQ_WEAPON_TRAINING", // 훈련용 검 ID
            "EQ_WEAPON_IRON", // 철제 검 ID
            "EQ_ARMOR_TRAINING", // 훈련용 갑옷 ID
            "EQ_ARMOR_GUARD" // 수호자 갑옷 ID
        };

        private Canvas canvas; // Runtime Canvas
        private Text characterNameText; // 캐릭터 이름 텍스트
        private Text characterLevelText; // 캐릭터 레벨 텍스트
        private Text portraitText; // 캐릭터 임시 초상 텍스트
        private Text weaponSlotText; // 무기 슬롯 텍스트
        private Text armorSlotText; // 방어구 슬롯 텍스트
        private Text detailTitleText; // 장비 상세 제목 텍스트
        private Text detailGradeText; // 장비 등급 텍스트
        private Text detailStatsText; // 장비 상세 능력치 텍스트
        private Text statusText; // 화면 상태 텍스트
        private Text inventoryCountText; // 인벤토리 개수 텍스트
        private RectTransform inventoryContent; // 장비 인벤토리 목록 영역
        private Button actionButton; // 장착 또는 해제 버튼
        private Text actionButtonText; // 장착 또는 해제 버튼 라벨
        private int selectedCharacterIndex; // 현재 캐릭터 목록 번호
        private string selectedInstanceId = string.Empty; // 선택 장비 인스턴스 ID

        private void Start() // 캐릭터 장비 화면 시작
        {
            EnsureEventSystem(); // UI 입력 시스템 보장
            BuildUi(); // 임시 캐릭터 UI 구성
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
                    legacyInputModule.enabled = false; // 구형 입력 모듈 즉시 비활성화
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

        private void BuildUi() // 전체 캐릭터 화면 UI 구성
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
            Image background = CreateImage(canvas.transform, "Background", new Color(0.93f, 0.93f, 0.93f, 1f)); // 전체 배경 생성
            Stretch(background.rectTransform); // 전체 배경 확장
            BuildHeader(background.transform); // 상단 메뉴 구성
            BuildCharacterPanel(background.transform); // 좌측 캐릭터 패널 구성
            BuildEquipmentPanel(background.transform); // 우측 장비 패널 구성
        }

        private void BuildHeader(Transform parent) // 상단 메뉴 구성
        {
            Button backButton = CreateButton(parent, "BackButton", "◀  캐릭터", new Color(0.78f, 0.87f, 0.94f, 1f)); // 로비 복귀 버튼 생성
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.025f, 0.92f), new Vector2(0.18f, 0.975f)); // 로비 복귀 버튼 배치
            backButton.onClick.AddListener(ReturnToLobby); // 로비 복귀 이벤트 연결
            Button helpButton = CreateButton(parent, "HelpButton", "?", new Color(0.80f, 0.88f, 0.94f, 1f)); // 도움말 버튼 생성
            SetRect(helpButton.GetComponent<RectTransform>(), new Vector2(0.71f, 0.92f), new Vector2(0.755f, 0.975f)); // 도움말 버튼 배치
            helpButton.onClick.AddListener(ShowHelp); // 도움말 이벤트 연결
            Text prototypeText = CreateText(parent, "PrototypeText", "DAY34 · CHARACTER EQUIPMENT", 18, FontStyle.Bold, new Color(0.20f, 0.22f, 0.25f, 1f)); // 임시 화면 표시 생성
            SetRect(prototypeText.rectTransform, new Vector2(0.765f, 0.92f), new Vector2(0.975f, 0.975f)); // 임시 화면 표시 배치
            string[] tabNames = { "스탯", "장비", "룬", "스킬", "프로필" }; // 상단 탭 이름 목록
            float tabStart = 0.41f; // 탭 시작 위치 설정
            float tabWidth = 0.105f; // 탭 너비 설정

            for (int index = 0; index < tabNames.Length; index++) // 상단 탭 순회
            {
                string tabName = tabNames[index]; // 현재 탭 이름 저장
                Color tabColor = tabName == "장비" ? new Color(0.78f, 0.87f, 0.94f, 1f) : new Color(0.94f, 0.94f, 0.94f, 1f); // 장비 탭 활성 색상 선택
                Button tabButton = CreateButton(parent, $"Tab_{index}", tabName, tabColor); // 상단 탭 버튼 생성
                float minX = tabStart + (tabWidth * index); // 현재 탭 최소 X 계산
                SetRect(tabButton.GetComponent<RectTransform>(), new Vector2(minX, 0.84f), new Vector2(minX + tabWidth - 0.006f, 0.90f)); // 현재 탭 버튼 배치

                if (tabName != "장비") // 미구현 탭 여부 확인
                {
                    tabButton.onClick.AddListener(ShowPrototypeOnly); // 미구현 탭 안내 이벤트 연결
                }
            }
        }

        private void BuildCharacterPanel(Transform parent) // 좌측 캐릭터 영역 구성
        {
            Image panel = CreateImage(parent, "CharacterPanel", new Color(0.96f, 0.96f, 0.96f, 1f)); // 캐릭터 패널 배경 생성
            SetRect(panel.rectTransform, new Vector2(0.02f, 0.05f), new Vector2(0.39f, 0.90f)); // 캐릭터 패널 배치
            AddOutline(panel.gameObject); // 캐릭터 패널 외곽선 추가
            characterNameText = CreateText(panel.transform, "CharacterName", "CHARACTER", 30, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 캐릭터 이름 텍스트 생성
            SetRect(characterNameText.rectTransform, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.10f)); // 캐릭터 이름 배치
            characterLevelText = CreateText(panel.transform, "CharacterLevel", "Lv. 1", 20, FontStyle.Bold, new Color(0.25f, 0.30f, 0.36f, 1f)); // 캐릭터 레벨 텍스트 생성
            SetRect(characterLevelText.rectTransform, new Vector2(0.36f, 0.10f), new Vector2(0.64f, 0.16f)); // 캐릭터 레벨 배치
            Button previousButton = CreateButton(panel.transform, "PreviousCharacter", "◀", new Color(0.90f, 0.90f, 0.90f, 1f)); // 이전 캐릭터 버튼 생성
            SetRect(previousButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.10f), new Vector2(0.17f, 0.17f)); // 이전 캐릭터 버튼 배치
            previousButton.onClick.AddListener(SelectPreviousCharacter); // 이전 캐릭터 이벤트 연결
            Button nextButton = CreateButton(panel.transform, "NextCharacter", "▶", new Color(0.90f, 0.90f, 0.90f, 1f)); // 다음 캐릭터 버튼 생성
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.83f, 0.10f), new Vector2(0.94f, 0.17f)); // 다음 캐릭터 버튼 배치
            nextButton.onClick.AddListener(SelectNextCharacter); // 다음 캐릭터 이벤트 연결
            Image portraitPanel = CreateImage(panel.transform, "PortraitPlaceholder", Color.white); // 캐릭터 임시 초상 배경 생성
            SetRect(portraitPanel.rectTransform, new Vector2(0.24f, 0.28f), new Vector2(0.76f, 0.83f)); // 캐릭터 임시 초상 배치
            AddOutline(portraitPanel.gameObject); // 캐릭터 임시 초상 외곽선 추가
            portraitText = CreateText(portraitPanel.transform, "PortraitText", "CHARACTER", 34, FontStyle.Bold, new Color(0.65f, 0.28f, 0.28f, 1f)); // 캐릭터 임시 초상 텍스트 생성
            Stretch(portraitText.rectTransform, 10f); // 캐릭터 임시 초상 텍스트 확장
            CreateLockedSlot(panel.transform, "HelmetPlaceholder", "투구\n준비 중", new Vector2(0.04f, 0.66f), new Vector2(0.22f, 0.80f)); // 투구 준비중 슬롯 생성
            CreateLockedSlot(panel.transform, "BootsPlaceholder", "신발\n준비 중", new Vector2(0.78f, 0.66f), new Vector2(0.96f, 0.80f)); // 신발 준비중 슬롯 생성
            CreateLockedSlot(panel.transform, "GlovesPlaceholder", "장갑\n준비 중", new Vector2(0.78f, 0.35f), new Vector2(0.96f, 0.49f)); // 장갑 준비중 슬롯 생성
            Button armorSlot = CreateButton(panel.transform, "ArmorSlot", "방어구\n비어 있음", new Color(0.82f, 0.87f, 0.92f, 1f)); // 방어구 슬롯 버튼 생성
            SetRect(armorSlot.GetComponent<RectTransform>(), new Vector2(0.04f, 0.35f), new Vector2(0.22f, 0.49f)); // 방어구 슬롯 버튼 배치
            armorSlotText = armorSlot.GetComponentInChildren<Text>(); // 방어구 슬롯 라벨 조회
            armorSlot.onClick.AddListener(SelectEquippedArmor); // 방어구 슬롯 선택 이벤트 연결
            Button weaponSlot = CreateButton(panel.transform, "WeaponSlot", "무기\n비어 있음", new Color(0.88f, 0.83f, 0.76f, 1f)); // 무기 슬롯 버튼 생성
            SetRect(weaponSlot.GetComponent<RectTransform>(), new Vector2(0.38f, 0.18f), new Vector2(0.62f, 0.30f)); // 무기 슬롯 버튼 배치
            weaponSlotText = weaponSlot.GetComponentInChildren<Text>(); // 무기 슬롯 라벨 조회
            weaponSlot.onClick.AddListener(SelectEquippedWeapon); // 무기 슬롯 선택 이벤트 연결
        }

        private void BuildEquipmentPanel(Transform parent) // 우측 장비 영역 구성
        {
            Image outerPanel = CreateImage(parent, "EquipmentPanel", new Color(0.91f, 0.91f, 0.91f, 1f)); // 장비 전체 패널 생성
            SetRect(outerPanel.rectTransform, new Vector2(0.41f, 0.05f), new Vector2(0.98f, 0.82f)); // 장비 전체 패널 배치
            AddOutline(outerPanel.gameObject); // 장비 전체 패널 외곽선 추가
            Image sideLabel = CreateImage(outerPanel.transform, "EquipmentSideLabel", new Color(0.83f, 0.83f, 0.83f, 1f)); // 장비 좌측 라벨 생성
            SetRect(sideLabel.rectTransform, new Vector2(0.02f, 0.88f), new Vector2(0.13f, 0.98f)); // 장비 좌측 라벨 배치
            Text sideText = CreateText(sideLabel.transform, "Text", "장비", 24, FontStyle.Normal, new Color(0.10f, 0.10f, 0.10f, 1f)); // 장비 좌측 라벨 텍스트 생성
            Stretch(sideText.rectTransform, 4f); // 장비 좌측 라벨 텍스트 확장
            Image contentPanel = CreateImage(outerPanel.transform, "EquipmentContent", new Color(0.85f, 0.85f, 0.85f, 1f)); // 장비 콘텐츠 패널 생성
            SetRect(contentPanel.rectTransform, new Vector2(0.15f, 0.02f), new Vector2(0.98f, 0.98f)); // 장비 콘텐츠 패널 배치
            AddOutline(contentPanel.gameObject); // 장비 콘텐츠 패널 외곽선 추가
            Image detailHeader = CreateImage(contentPanel.transform, "DetailHeader", new Color(0.82f, 0.82f, 0.82f, 1f)); // 장비 상세 헤더 생성
            SetRect(detailHeader.rectTransform, new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.98f)); // 장비 상세 헤더 배치
            detailGradeText = CreateText(detailHeader.transform, "Grade", "☆☆☆☆☆", 25, FontStyle.Bold, new Color(0.20f, 0.18f, 0.10f, 1f)); // 장비 등급 텍스트 생성
            SetRect(detailGradeText.rectTransform, new Vector2(0.02f, 0.08f), new Vector2(0.27f, 0.92f)); // 장비 등급 텍스트 배치
            detailTitleText = CreateText(detailHeader.transform, "Title", "장비를 선택하세요", 24, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 장비 상세 제목 생성
            SetRect(detailTitleText.rectTransform, new Vector2(0.28f, 0.08f), new Vector2(0.72f, 0.92f)); // 장비 상세 제목 배치
            actionButton = CreateButton(detailHeader.transform, "ActionButton", "장착", new Color(0.78f, 0.87f, 0.94f, 1f)); // 장착 액션 버튼 생성
            SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.16f), new Vector2(0.97f, 0.84f)); // 장착 액션 버튼 배치
            actionButtonText = actionButton.GetComponentInChildren<Text>(); // 장착 액션 버튼 라벨 조회
            actionButton.onClick.AddListener(ApplySelectedEquipmentAction); // 장비 액션 이벤트 연결
            Image statsPanel = CreateImage(contentPanel.transform, "StatsPanel", new Color(0.88f, 0.88f, 0.88f, 1f)); // 장비 스탯 상세 패널 생성
            SetRect(statsPanel.rectTransform, new Vector2(0.02f, 0.47f), new Vector2(0.98f, 0.80f)); // 장비 스탯 상세 패널 배치
            detailStatsText = CreateText(statsPanel.transform, "Stats", "장비 능력치", 21, FontStyle.Normal, new Color(0.12f, 0.14f, 0.17f, 1f)); // 장비 능력치 텍스트 생성
            detailStatsText.alignment = TextAnchor.UpperLeft; // 장비 능력치 좌상단 정렬
            detailStatsText.resizeTextForBestFit = false; // 장비 능력치 자동 크기 비활성화
            SetRect(detailStatsText.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.94f)); // 장비 능력치 텍스트 배치
            Text inventoryLabel = CreateText(contentPanel.transform, "InventoryLabel", "보유 장비", 22, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 보유 장비 라벨 생성
            SetRect(inventoryLabel.rectTransform, new Vector2(0.03f, 0.40f), new Vector2(0.28f, 0.46f)); // 보유 장비 라벨 배치
            inventoryCountText = CreateText(contentPanel.transform, "InventoryCount", "0개", 18, FontStyle.Bold, new Color(0.30f, 0.34f, 0.40f, 1f)); // 장비 보유 개수 텍스트 생성
            SetRect(inventoryCountText.rectTransform, new Vector2(0.28f, 0.40f), new Vector2(0.43f, 0.46f)); // 장비 보유 개수 배치
            Button grantButton = CreateButton(contentPanel.transform, "GrantDemoEquipment", "테스트 장비 지급", new Color(0.88f, 0.83f, 0.66f, 1f)); // 테스트 장비 지급 버튼 생성
            SetRect(grantButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.40f), new Vector2(0.97f, 0.46f)); // 테스트 장비 지급 버튼 배치
            grantButton.onClick.AddListener(GrantDemoEquipment); // 테스트 장비 지급 이벤트 연결
            Image inventoryPanel = CreateImage(contentPanel.transform, "InventoryPanel", new Color(0.92f, 0.92f, 0.92f, 1f)); // 보유 장비 목록 패널 생성
            SetRect(inventoryPanel.rectTransform, new Vector2(0.02f, 0.10f), new Vector2(0.98f, 0.39f)); // 보유 장비 목록 패널 배치
            inventoryContent = inventoryPanel.rectTransform; // 보유 장비 목록 부모 저장
            statusText = CreateText(contentPanel.transform, "Status", "장비를 선택하세요.", 17, FontStyle.Normal, new Color(0.25f, 0.28f, 0.32f, 1f)); // 화면 상태 텍스트 생성
            statusText.alignment = TextAnchor.MiddleLeft; // 화면 상태 왼쪽 정렬
            SetRect(statusText.rectTransform, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.085f)); // 화면 상태 텍스트 배치
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
                SetStatus("보유 캐릭터가 없습니다."); // 보유 캐릭터 누락 안내
                SetAction(false, "사용 불가"); // 장비 액션 비활성화
                return; // 화면 갱신 중단
            }

            selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, saveData.Characters.Count - 1); // 선택 캐릭터 번호 범위 보정
            CharacterSaveData characterSave = saveData.Characters[selectedCharacterIndex]; // 선택 캐릭터 저장 조회
            CharacterData characterData = characterSave == null ? null : dataManager.GetCharacter(characterSave.CharacterId); // 선택 캐릭터 원본 조회
            string displayName = characterData == null ? characterSave.CharacterId : characterData.DisplayName; // 캐릭터 표시 이름 결정
            characterNameText.text = string.IsNullOrWhiteSpace(displayName) ? characterSave.CharacterId : displayName; // 캐릭터 이름 화면 반영
            characterLevelText.text = $"Lv. {characterSave.Level}"; // 캐릭터 레벨 화면 반영
            portraitText.text = string.IsNullOrWhiteSpace(displayName) ? "CHARACTER" : $"{displayName}\n\nCHARACTER"; // 캐릭터 임시 초상 문구 반영
            UpdateSlotText(saveData, dataManager, characterSave, EquipmentSlot.Weapon, weaponSlotText, "무기"); // 무기 슬롯 화면 갱신
            UpdateSlotText(saveData, dataManager, characterSave, EquipmentSlot.Armor, armorSlotText, "방어구"); // 방어구 슬롯 화면 갱신
            BuildInventoryButtons(saveData, dataManager, characterSave); // 보유 장비 목록 버튼 구성
            EnsureSelectedInstance(saveData); // 선택 장비 인스턴스 보정
            UpdateDetail(saveData, dataManager, characterSave); // 장비 상세 정보 갱신
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

        private void UpdateSlotText(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave, EquipmentSlot slot, Text target, string slotLabel) // 장착 슬롯 텍스트 갱신
        {
            EquipmentInstanceSaveData instance = CharacterEquipmentService.GetEquippedInstance(saveData, characterSave.CharacterId, slot); // 슬롯 장착 장비 조회

            if (instance == null) // 장착 장비 존재 확인
            {
                target.text = $"{slotLabel}\n비어 있음"; // 빈 장비 슬롯 표시
                return; // 슬롯 갱신 종료
            }

            EquipmentData equipmentData = dataManager.GetEquipment(instance.EquipmentId); // 장착 장비 원본 조회
            string equipmentName = equipmentData == null ? instance.EquipmentId : equipmentData.DisplayName; // 장착 장비 이름 결정
            target.text = $"{slotLabel}\n{equipmentName}"; // 장착 장비 슬롯 표시
        }

        private void BuildInventoryButtons(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave) // 보유 장비 목록 버튼 구성
        {
            for (int childIndex = inventoryContent.childCount - 1; childIndex >= 0; childIndex--) // 기존 장비 버튼 역순 순회
            {
                Destroy(inventoryContent.GetChild(childIndex).gameObject); // 기존 장비 버튼 제거
            }

            inventoryCountText.text = $"{saveData.EquipmentInventory.Count}개"; // 장비 보유 개수 표시
            int visibleCount = Mathf.Min(8, saveData.EquipmentInventory.Count); // 임시 화면 최대 표시 수 계산

            for (int index = 0; index < visibleCount; index++) // 표시 장비 인스턴스 순회
            {
                EquipmentInstanceSaveData instance = saveData.EquipmentInventory[index]; // 현재 장비 인스턴스 조회

                if (instance == null) // 장비 인스턴스 존재 확인
                {
                    continue; // 빈 장비 인스턴스 제외
                }

                EquipmentData equipmentData = dataManager.GetEquipment(instance.EquipmentId); // 장비 원본 데이터 조회
                string equipmentName = equipmentData == null ? instance.EquipmentId : equipmentData.DisplayName; // 장비 표시 이름 결정
                CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 장비 착용 캐릭터 조회
                string state = owner == null ? "미착용" : owner.CharacterId == characterSave.CharacterId ? "장착 중" : $"{owner.CharacterId} 장착"; // 장비 착용 상태 문구 생성
                string slotName = equipmentData == null ? "?" : equipmentData.Slot == EquipmentSlot.Weapon ? "W" : "A"; // 장비 슬롯 축약 문구 생성
                string grade = equipmentData == null ? string.Empty : GetGradeStars(equipmentData.Grade); // 장비 등급 별 문구 생성
                Button itemButton = CreateButton(inventoryContent, $"Equipment_{index}", $"[{slotName}] {grade} {equipmentName}\n{state}", new Color(0.90f, 0.90f, 0.90f, 1f)); // 장비 목록 버튼 생성
                float rowHeight = 0.12f; // 장비 목록 행 높이 설정
                float top = 0.96f - (rowHeight * index); // 장비 목록 행 상단 계산
                SetRect(itemButton.GetComponent<RectTransform>(), new Vector2(0.02f, top - rowHeight + 0.01f), new Vector2(0.98f, top)); // 장비 목록 버튼 배치
                string capturedInstanceId = instance.InstanceId; // 버튼 이벤트용 인스턴스 ID 저장
                itemButton.onClick.AddListener(() => SelectEquipment(capturedInstanceId)); // 장비 목록 선택 이벤트 연결
            }
        }

        private void EnsureSelectedInstance(SaveData saveData) // 선택 장비 인스턴스 유효성 보정
        {
            if (!string.IsNullOrWhiteSpace(selectedInstanceId) && saveData.FindEquipmentInstance(selectedInstanceId) != null) // 기존 선택 장비 유효성 확인
            {
                return; // 기존 선택 장비 유지
            }

            selectedInstanceId = string.Empty; // 기존 선택 장비 초기화

            if (saveData.EquipmentInventory.Count > 0 && saveData.EquipmentInventory[0] != null) // 첫 장비 존재 확인
            {
                selectedInstanceId = saveData.EquipmentInventory[0].InstanceId; // 첫 보유 장비 자동 선택
            }
        }

        private void UpdateDetail(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave) // 선택 장비 상세 갱신
        {
            EquipmentInstanceSaveData instance = saveData.FindEquipmentInstance(selectedInstanceId); // 선택 장비 인스턴스 조회

            if (instance == null) // 선택 장비 존재 확인
            {
                detailTitleText.text = "장비를 선택하세요"; // 선택 장비 없음 제목 표시
                detailGradeText.text = "☆☆☆☆☆"; // 선택 장비 없음 등급 표시
                detailStatsText.text = "보유 장비를 선택하면 상세 정보가 표시됩니다."; // 선택 장비 없음 설명 표시
                SetAction(false, "장착"); // 선택 장비 없음 액션 비활성화
                return; // 상세 갱신 종료
            }

            EquipmentData equipmentData = dataManager.GetEquipment(instance.EquipmentId); // 선택 장비 원본 조회

            if (equipmentData == null) // 장비 원본 존재 확인
            {
                detailTitleText.text = instance.EquipmentId; // 누락 장비 ID 표시
                detailGradeText.text = "?????"; // 누락 장비 등급 표시
                detailStatsText.text = "EquipmentData가 등록되지 않았습니다."; // 누락 장비 안내
                SetAction(false, "사용 불가"); // 누락 장비 액션 비활성화
                return; // 상세 갱신 종료
            }

            detailTitleText.text = equipmentData.DisplayName; // 장비 이름 상세 표시
            detailGradeText.text = GetGradeStars(equipmentData.Grade); // 장비 등급 상세 표시
            detailStatsText.text = BuildStatDescription(equipmentData); // 장비 능력치 상세 표시
            CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 장비 착용 캐릭터 조회

            if (owner != null && owner.CharacterId != characterSave.CharacterId) // 다른 캐릭터 장착 여부 확인
            {
                SetAction(false, "다른 캐릭터 장착 중"); // 다른 캐릭터 장비 액션 비활성화
                return; // 액션 갱신 종료
            }

            if (owner != null) // 현재 캐릭터 장착 여부 확인
            {
                SetAction(true, "해제"); // 현재 캐릭터 장착 장비 해제 액션 설정
                return; // 액션 갱신 종료
            }

            string equippedInstanceId = characterSave.Equipment.GetInstanceId(equipmentData.Slot); // 동일 슬롯 기존 장비 조회
            SetAction(true, string.IsNullOrWhiteSpace(equippedInstanceId) ? "장착" : "교체"); // 장착 또는 교체 액션 설정
        }

        private string BuildStatDescription(EquipmentData equipmentData) // 장비 상세 능력치 문구 생성
        {
            string description = $"슬롯 : {(equipmentData.Slot == EquipmentSlot.Weapon ? "무기" : "방어구")}\nID : {equipmentData.Id}\n\n"; // 장비 기본 상세 문구 생성
            bool hasStat = false; // 장비 옵션 존재 상태 초기화

            foreach (EquipmentStatType statType in Enum.GetValues(typeof(EquipmentStatType))) // 전체 장비 능력치 종류 순회
            {
                float value = equipmentData.GetStatValue(statType); // 현재 능력치 옵션 합계 조회

                if (Mathf.Approximately(value, 0f)) // 능력치 옵션 존재 확인
                {
                    continue; // 값 없는 능력치 제외
                }

                description += $"{GetStatDisplayName(statType)}  +{FormatStatValue(statType, value)}\n"; // 능력치 옵션 상세 문구 추가
                hasStat = true; // 장비 옵션 존재 기록
            }

            if (!hasStat) // 장비 옵션 없음 확인
            {
                description += "등록된 능력치 옵션이 없습니다.\n"; // 빈 장비 옵션 안내 추가
            }

            return description; // 장비 상세 문구 반환
        }

        private void ApplySelectedEquipmentAction() // 선택 장비 장착 또는 해제 처리
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 컨텍스트 확인
            {
                SetStatus("저장 데이터를 찾을 수 없습니다."); // 전역 데이터 누락 안내
                return; // 장비 액션 중단
            }

            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 저장 조회
            EquipmentInstanceSaveData instance = saveData.FindEquipmentInstance(selectedInstanceId); // 선택 장비 조회

            if (characterSave == null || instance == null) // 캐릭터 및 장비 선택 상태 확인
            {
                SetStatus("캐릭터와 장비를 먼저 선택하세요."); // 선택 상태 오류 안내
                return; // 장비 액션 중단
            }

            EquipmentData equipmentData = dataManager.GetEquipment(instance.EquipmentId); // 선택 장비 원본 조회

            if (equipmentData == null) // 장비 원본 존재 확인
            {
                SetStatus($"EquipmentData 누락: {instance.EquipmentId}"); // 장비 원본 누락 안내
                return; // 장비 액션 중단
            }

            CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 현재 장비 착용 캐릭터 조회
            bool succeeded; // 장비 액션 성공 상태 선언
            string error; // 장비 액션 오류 문구 선언

            if (owner != null && owner.CharacterId == characterSave.CharacterId) // 현재 캐릭터 장착 여부 확인
            {
                succeeded = CharacterEquipmentService.TryUnequip(saveData, characterSave.CharacterId, equipmentData.Slot, out EquipmentInstanceSaveData removedInstance, out error); // 현재 슬롯 장비 해제 실행
            }
            else // 미착용 장비 처리
            {
                succeeded = CharacterEquipmentService.TryEquip(saveData, dataManager, characterSave.CharacterId, instance.InstanceId, out error); // 장비 착용 또는 교체 실행
            }

            if (!succeeded) // 장비 액션 실패 확인
            {
                SetStatus(error); // 장비 액션 오류 표시
                Refresh(); // 장비 화면 재갱신
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

                if (saveData.GetEquipmentCount(equipmentId) > 0) // 기존 동일 장비 보유 확인
                {
                    continue; // 이미 보유한 임시 장비 재지급 제외
                }

                if (saveData.TryCreateEquipmentInstance(equipmentId, out EquipmentInstanceSaveData addedInstance, out string error)) // 신규 임시 장비 인스턴스 생성
                {
                    addedCount++; // 신규 지급 장비 개수 증가
                }
            }

            bool saved = addedCount == 0 || saveManager.SaveCurrent(); // 신규 지급 시 저장 실행
            SetStatus(addedCount > 0 ? $"테스트 장비 {addedCount}개를 지급했습니다." : "지급할 새 테스트 장비가 없습니다."); // 테스트 장비 지급 결과 표시

            if (!saved) // 테스트 장비 저장 실패 확인
            {
                SetStatus("테스트 장비는 지급되었지만 저장에 실패했습니다."); // 테스트 장비 저장 실패 안내
            }

            Refresh(); // 장비 인벤토리 화면 갱신
        }

        private CharacterSaveData GetSelectedCharacter(SaveData saveData) // 선택 캐릭터 저장 조회
        {
            if (saveData == null || saveData.Characters.Count == 0) // 저장 데이터 및 캐릭터 목록 확인
            {
                return null; // 선택 캐릭터 조회 실패
            }

            selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, saveData.Characters.Count - 1); // 선택 캐릭터 번호 범위 보정
            return saveData.Characters[selectedCharacterIndex]; // 선택 캐릭터 저장 반환
        }

        private void SelectPreviousCharacter() // 이전 캐릭터 선택
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData) || saveData.Characters.Count == 0) // 캐릭터 목록 확인
            {
                return; // 이전 캐릭터 선택 중단
            }

            selectedCharacterIndex = (selectedCharacterIndex - 1 + saveData.Characters.Count) % saveData.Characters.Count; // 이전 캐릭터 번호 순환 계산
            selectedInstanceId = string.Empty; // 캐릭터 변경 시 장비 선택 초기화
            Refresh(); // 캐릭터 화면 갱신
        }

        private void SelectNextCharacter() // 다음 캐릭터 선택
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData) || saveData.Characters.Count == 0) // 캐릭터 목록 확인
            {
                return; // 다음 캐릭터 선택 중단
            }

            selectedCharacterIndex = (selectedCharacterIndex + 1) % saveData.Characters.Count; // 다음 캐릭터 번호 순환 계산
            selectedInstanceId = string.Empty; // 캐릭터 변경 시 장비 선택 초기화
            Refresh(); // 캐릭터 화면 갱신
        }

        private void SelectEquippedWeapon() // 현재 무기 슬롯 장비 선택
        {
            SelectEquippedSlot(EquipmentSlot.Weapon); // 무기 슬롯 장비 선택 실행
        }

        private void SelectEquippedArmor() // 현재 방어구 슬롯 장비 선택
        {
            SelectEquippedSlot(EquipmentSlot.Armor); // 방어구 슬롯 장비 선택 실행
        }

        private void SelectEquippedSlot(EquipmentSlot slot) // 현재 슬롯 장비 선택
        {
            if (!TryGetContext(out DataManager dataManager, out SaveManager saveManager, out SaveData saveData)) // 전역 데이터 컨텍스트 확인
            {
                return; // 슬롯 장비 선택 중단
            }

            CharacterSaveData characterSave = GetSelectedCharacter(saveData); // 선택 캐릭터 저장 조회

            if (characterSave == null) // 선택 캐릭터 존재 확인
            {
                return; // 슬롯 장비 선택 중단
            }

            string instanceId = characterSave.Equipment.GetInstanceId(slot); // 슬롯 장착 장비 ID 조회

            if (string.IsNullOrWhiteSpace(instanceId)) // 슬롯 장비 존재 확인
            {
                SetStatus(slot == EquipmentSlot.Weapon ? "무기 슬롯이 비어 있습니다." : "방어구 슬롯이 비어 있습니다."); // 빈 슬롯 상태 표시
                return; // 슬롯 장비 선택 종료
            }

            SelectEquipment(instanceId); // 슬롯 장비 상세 선택
        }

        private void SelectEquipment(string instanceId) // 장비 인스턴스 상세 선택
        {
            selectedInstanceId = instanceId ?? string.Empty; // 선택 장비 인스턴스 저장
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

        private void ShowHelp() // 임시 장비 화면 도움말 표시
        {
            SetStatus("테스트 장비 지급 → 보유 장비 선택 → 장착/교체/해제 순서로 확인할 수 있습니다."); // 임시 화면 사용법 표시
        }

        private void ShowPrototypeOnly() // 미구현 탭 안내
        {
            SetStatus("현재 Day34 임시 화면에서는 장비 탭만 동작합니다."); // 미구현 탭 범위 안내
        }

        private void SetAction(bool interactable, string label) // 장비 액션 버튼 상태 설정
        {
            actionButton.interactable = interactable; // 장비 액션 버튼 입력 상태 적용
            actionButtonText.text = label; // 장비 액션 버튼 라벨 적용
        }

        private void SetStatus(string message) // 화면 상태 문구 설정
        {
            if (statusText != null) // 화면 상태 텍스트 존재 확인
            {
                statusText.text = message ?? string.Empty; // 화면 상태 문구 적용
            }
        }

        private static string GetGradeStars(ItemGrade grade) // 장비 등급 별 문구 생성
        {
            int starCount = Mathf.Clamp(((int)grade) + 1, 1, 5); // 장비 등급 별 개수 계산
            return new string('★', starCount) + new string('☆', 5 - starCount); // 장비 등급 별 문구 반환
        }

        private static string GetStatDisplayName(EquipmentStatType statType) // 장비 능력치 표시 이름 반환
        {
            switch (statType) // 장비 능력치 종류 분기
            {
                case EquipmentStatType.MaxHp: return "최대 HP"; // 최대 체력 표시 이름 반환
                case EquipmentStatType.Attack: return "공격력"; // 공격력 표시 이름 반환
                case EquipmentStatType.Defense: return "방어력"; // 방어력 표시 이름 반환
                case EquipmentStatType.Resistance: return "저항력"; // 저항력 표시 이름 반환
                case EquipmentStatType.AttackSpeed: return "공격 속도"; // 공격 속도 표시 이름 반환
                case EquipmentStatType.Accuracy: return "명중률"; // 명중률 표시 이름 반환
                case EquipmentStatType.CriticalRate: return "치명타율"; // 치명타율 표시 이름 반환
                case EquipmentStatType.AttackRange: return "공격 사거리"; // 공격 사거리 표시 이름 반환
                case EquipmentStatType.MoveSpeed: return "이동 속도"; // 이동 속도 표시 이름 반환
                default: return statType.ToString(); // 미지원 능력치 이름 반환
            }
        }

        private static string FormatStatValue(EquipmentStatType statType, float value) // 장비 능력치 수치 문구 생성
        {
            if (statType == EquipmentStatType.Accuracy || statType == EquipmentStatType.CriticalRate) // 비율 능력치 여부 확인
            {
                return $"{value * 100f:0.#}%"; // 비율 능력치 백분율 문구 반환
            }

            return Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.##"); // 일반 능력치 문구 반환
        }

        private static void CreateLockedSlot(Transform parent, string name, string label, Vector2 min, Vector2 max) // 준비중 장비 슬롯 생성
        {
            Image image = CreateImage(parent, name, new Color(0.83f, 0.83f, 0.83f, 1f)); // 준비중 슬롯 배경 생성
            SetRect(image.rectTransform, min, max); // 준비중 슬롯 배치
            AddOutline(image.gameObject); // 준비중 슬롯 외곽선 추가
            Text text = CreateText(image.transform, "Label", label, 18, FontStyle.Bold, new Color(0.38f, 0.38f, 0.38f, 1f)); // 준비중 슬롯 라벨 생성
            Stretch(text.rectTransform, 4f); // 준비중 슬롯 라벨 확장
        }

        private static Image CreateImage(Transform parent, string name, Color color) // 공통 UI 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // UI 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // UI 이미지 부모 연결
            Image image = imageObject.GetComponent<Image>(); // UI 이미지 컴포넌트 조회
            image.color = color; // UI 이미지 색상 적용
            return image; // 생성 UI 이미지 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, FontStyle fontStyle, Color color) // 공통 UI 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // UI 텍스트 부모 연결
            Text text = textObject.GetComponent<Text>(); // UI Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // UI 텍스트 값 적용
            text.fontSize = fontSize; // UI 텍스트 크기 적용
            text.fontStyle = fontStyle; // UI 텍스트 스타일 적용
            text.color = color; // UI 텍스트 색상 적용
            text.alignment = TextAnchor.MiddleCenter; // UI 텍스트 중앙 정렬
            text.alignByGeometry = true; // UI 텍스트 글리프 정렬 활성화
            text.resizeTextForBestFit = true; // UI 텍스트 자동 크기 활성화
            text.resizeTextMinSize = 10; // UI 텍스트 최소 크기 설정
            text.resizeTextMaxSize = fontSize; // UI 텍스트 최대 크기 설정
            text.raycastTarget = false; // UI 텍스트 입력 비활성화
            return text; // 생성 UI 텍스트 반환
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color) // 공통 UI 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // UI 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // UI 버튼 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // UI 버튼 이미지 조회
            image.color = color; // UI 버튼 배경색 적용
            Button button = buttonObject.GetComponent<Button>(); // UI Button 컴포넌트 조회
            button.targetGraphic = image; // UI 버튼 대상 그래픽 연결
            AddOutline(buttonObject); // UI 버튼 외곽선 추가
            Text text = CreateText(buttonObject.transform, "Label", label, 20, FontStyle.Normal, new Color(0.08f, 0.09f, 0.11f, 1f)); // UI 버튼 라벨 생성
            Stretch(text.rectTransform, 5f); // UI 버튼 라벨 확장
            return button; // 생성 UI 버튼 반환
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

        private static void Stretch(RectTransform rect) // RectTransform 전체 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        private static void Stretch(RectTransform rect, float padding) // RectTransform 내부 여백 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 내부 여백 설정
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 내부 여백 설정
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) // RectTransform 앵커 배치
        {
            rect.anchorMin = min; // 최소 앵커 설정
            rect.anchorMax = max; // 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }
    }
}
