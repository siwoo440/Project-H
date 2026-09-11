using System; // 문자열 비교 및 수학 기능
using ProjectH.Battle; // 장비 최종 능력치 비교 기능
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
    public sealed class CharacterEquipmentScreenController : MonoBehaviour // Day36 캐릭터 장비 관리 화면
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
        private Text currentStatsText; // 현재 최종 능력치 텍스트
        private Text weaponSlotText; // 무기 슬롯 텍스트
        private Text armorSlotText; // 방어구 슬롯 텍스트
        private Text inventoryCountText; // 인벤토리 개수 텍스트
        private RectTransform inventoryContent; // 장비 인벤토리 목록 영역
        private Text detailTitleText; // 선택 장비 이름 텍스트
        private Text detailGradeText; // 선택 장비 등급 텍스트
        private Text detailStatsText; // 선택 장비 옵션 텍스트
        private Text comparisonText; // 장비 교체 비교 텍스트
        private Text statusText; // 화면 상태 텍스트
        private Text affinityText; // 캐릭터 호감도 디버그 텍스트 (Day43)
        private CharacterAffinityRewardPanel affinityRewardPanel; // 호감도 보상 패널 (Day56 추가, 최적화로 별도 클래스 분리)
        private CharacterGiftPanel giftPanel; // 선물하기 패널 (Day57 추가)
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

        private void BuildUi() // 전체 캐릭터 장비 화면 구성
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
            BuildEquipmentPanel(background.transform); // 우측 장비 관리 패널 구성
            BuildAffinityDebugBar(background.transform); // 하단 호감도 디버그 바 구성 (Day43)
        }

        private void BuildAffinityDebugBar(Transform parent) // 하단 호감도 디버그 바 구성 (Day43)
        {
            affinityText = CreateText(parent, "AffinityDebugText", "호감도 · -", 15, FontStyle.Bold, new Color(0.36f, 0.20f, 0.30f, 1f)); // 호감도 디버그 텍스트 생성
            affinityText.alignment = TextAnchor.MiddleLeft; // 호감도 디버그 텍스트 왼쪽 정렬
            SetRect(affinityText.rectTransform, new Vector2(0.02f, 0.005f), new Vector2(0.30f, 0.045f)); // 하단 여백에 호감도 디버그 텍스트 배치 (Day58 버튼 추가로 폭 조정)
            Button minusButton = CreateButton(parent, "AffinityMinusDebug", "호감도 -10", new Color(0.98f, 0.90f, 0.95f, 1f)); // 호감도 감소 디버그 버튼 생성
            SetRect(minusButton.GetComponent<RectTransform>(), new Vector2(0.31f, 0.005f), new Vector2(0.41f, 0.045f)); // 호감도 감소 버튼 배치 (Day58 재배치)
            minusButton.onClick.AddListener(DecreaseAffinityDebug); // 호감도 감소 버튼 이벤트 연결
            DevelopmentFeatures.HideInRelease(minusButton); // 출시 빌드에서는 호감도 디버그 버튼 숨김 (최적화)
            Button plusButton = CreateButton(parent, "AffinityPlusDebug", "호감도 +10", new Color(0.90f, 0.95f, 1f, 1f)); // 호감도 증가 디버그 버튼 생성
            SetRect(plusButton.GetComponent<RectTransform>(), new Vector2(0.42f, 0.005f), new Vector2(0.52f, 0.045f)); // 호감도 증가 버튼 배치 (Day58 재배치)
            plusButton.onClick.AddListener(IncreaseAffinityDebug); // 호감도 증가 버튼 이벤트 연결
            DevelopmentFeatures.HideInRelease(plusButton); // 출시 빌드에서는 호감도 디버그 버튼 숨김 (최적화)
            Button rewardButton = CreateButton(parent, "AffinityRewardButton", "호감도 보상", new Color(1f, 0.88f, 0.60f, 1f)); // 호감도 보상 버튼 생성 (Day56 추가)
            SetRect(rewardButton.GetComponent<RectTransform>(), new Vector2(0.53f, 0.005f), new Vector2(0.65f, 0.045f)); // 호감도 보상 버튼 배치 (Day58 재배치)
            affinityRewardPanel = CharacterAffinityRewardPanel.Create(parent, Refresh, PlayCharacterEvent); // 호감도 보상 패널 생성 (수령 후 화면 갱신·Day58 개인 이벤트 보기 연결)
            rewardButton.onClick.AddListener(ToggleAffinityRewardPanel); // 보상 패널 열기·닫기 연결 (Day57 선물 패널과 겹치지 않도록 전용 함수 사용)
            Button giftButton = CreateButton(parent, "GiftButton", "선물하기", new Color(1f, 0.82f, 0.88f, 1f)); // 선물하기 버튼 생성 (Day57 추가)
            SetRect(giftButton.GetComponent<RectTransform>(), new Vector2(0.66f, 0.005f), new Vector2(0.78f, 0.045f)); // 선물하기 버튼 배치 (Day58 재배치)
            giftPanel = CharacterGiftPanel.Create(parent, Refresh, OpenAffinityRewardFromGift); // 선물 패널 생성 (선물 후 갱신·보상 바로가기 연결)
            giftButton.onClick.AddListener(ToggleGiftPanel); // 선물 패널 열기·닫기 연결
            Button talkButton = CreateButton(parent, "TalkButton", "대화하기", new Color(0.84f, 0.80f, 0.98f, 1f)); // 대화하기 버튼 생성 (Day58 추가)
            SetRect(talkButton.GetComponent<RectTransform>(), new Vector2(0.79f, 0.005f), new Vector2(0.91f, 0.045f)); // 대화하기 버튼 배치
            talkButton.onClick.AddListener(StartDailyTalk); // 일상 대화 연결
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
            if (giftPanel != null && giftPanel.IsOpen) giftPanel.Toggle(); // 열린 선물 패널 닫기
            affinityRewardPanel.Toggle(); // 보상 패널 전환
        }

        private void ToggleGiftPanel() // 선물 패널 열기·닫기 (Day57 추가, 보상 패널은 닫음)
        {
            if (affinityRewardPanel != null && affinityRewardPanel.IsOpen) affinityRewardPanel.Toggle(); // 열린 보상 패널 닫기
            giftPanel.Toggle(); // 선물 패널 전환
        }

        private void OpenAffinityRewardFromGift() // 선물 패널의 호감도 보상 바로가기 (Day57 추가)
        {
            if (giftPanel.IsOpen) giftPanel.Toggle(); // 선물 패널 닫기
            if (!affinityRewardPanel.IsOpen) affinityRewardPanel.Toggle(); // 보상 패널 열기
        }

        private void BuildHeader(Transform parent) // 상단 메뉴 구성
        {
            Button backButton = CreateButton(parent, "BackButton", "◀  로비", new Color(0.78f, 0.87f, 0.94f, 1f)); // 로비 복귀 버튼 생성
            SetRect(backButton.GetComponent<RectTransform>(), new Vector2(0.025f, 0.92f), new Vector2(0.16f, 0.975f)); // 로비 복귀 버튼 배치
            backButton.onClick.AddListener(ReturnToLobby); // 로비 복귀 이벤트 연결
            Text titleText = CreateText(parent, "Title", "캐릭터 장비 관리", 27, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 화면 제목 생성
            SetRect(titleText.rectTransform, new Vector2(0.20f, 0.92f), new Vector2(0.52f, 0.975f)); // 화면 제목 배치
            Text dayText = CreateText(parent, "DayLabel", "DAY36 · EQUIPMENT MANAGEMENT", 18, FontStyle.Bold, new Color(0.25f, 0.30f, 0.36f, 1f)); // Day36 화면 표시 생성
            SetRect(dayText.rectTransform, new Vector2(0.69f, 0.92f), new Vector2(0.975f, 0.975f)); // Day36 화면 표시 배치
        }

        private void BuildCharacterPanel(Transform parent) // 좌측 캐릭터 영역 구성
        {
            Image panel = CreateImage(parent, "CharacterPanel", new Color(0.96f, 0.96f, 0.96f, 1f)); // 캐릭터 패널 배경 생성
            SetRect(panel.rectTransform, new Vector2(0.02f, 0.05f), new Vector2(0.39f, 0.90f)); // 캐릭터 패널 배치
            AddOutline(panel.gameObject); // 캐릭터 패널 외곽선 추가
            characterNameText = CreateText(panel.transform, "CharacterName", "CHARACTER", 30, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 캐릭터 이름 생성
            SetRect(characterNameText.rectTransform, new Vector2(0.16f, 0.91f), new Vector2(0.84f, 0.985f)); // 캐릭터 이름 배치
            characterLevelText = CreateText(panel.transform, "CharacterLevel", "Lv. 1", 19, FontStyle.Bold, new Color(0.25f, 0.30f, 0.36f, 1f)); // 캐릭터 레벨 생성
            SetRect(characterLevelText.rectTransform, new Vector2(0.39f, 0.855f), new Vector2(0.61f, 0.91f)); // 캐릭터 레벨 배치
            Button previousButton = CreateButton(panel.transform, "PreviousCharacter", "◀", new Color(0.90f, 0.90f, 0.90f, 1f)); // 이전 캐릭터 버튼 생성
            SetRect(previousButton.GetComponent<RectTransform>(), new Vector2(0.04f, 0.855f), new Vector2(0.16f, 0.925f)); // 이전 캐릭터 버튼 배치
            previousButton.onClick.AddListener(SelectPreviousCharacter); // 이전 캐릭터 이벤트 연결
            Button nextButton = CreateButton(panel.transform, "NextCharacter", "▶", new Color(0.90f, 0.90f, 0.90f, 1f)); // 다음 캐릭터 버튼 생성
            SetRect(nextButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.855f), new Vector2(0.96f, 0.925f)); // 다음 캐릭터 버튼 배치
            nextButton.onClick.AddListener(SelectNextCharacter); // 다음 캐릭터 이벤트 연결
            Image portraitPanel = CreateImage(panel.transform, "PortraitPlaceholder", Color.white); // 캐릭터 임시 초상 배경 생성
            SetRect(portraitPanel.rectTransform, new Vector2(0.28f, 0.55f), new Vector2(0.72f, 0.84f)); // 캐릭터 임시 초상 배치
            AddOutline(portraitPanel.gameObject); // 캐릭터 임시 초상 외곽선 추가
            portraitText = CreateText(portraitPanel.transform, "PortraitText", "CHARACTER", 28, FontStyle.Bold, new Color(0.65f, 0.28f, 0.28f, 1f)); // 캐릭터 임시 초상 텍스트 생성
            Stretch(portraitText.rectTransform, 8f); // 캐릭터 임시 초상 텍스트 확장
            CreateLockedSlot(panel.transform, "HelmetPlaceholder", "투구\n준비 중", new Vector2(0.04f, 0.68f), new Vector2(0.24f, 0.81f)); // 투구 준비 슬롯 생성
            CreateLockedSlot(panel.transform, "BootsPlaceholder", "신발\n준비 중", new Vector2(0.76f, 0.53f), new Vector2(0.96f, 0.66f)); // 신발 준비 슬롯 생성
            Button armorSlot = CreateButton(panel.transform, "ArmorSlot", "방어구\n비어 있음", new Color(0.82f, 0.87f, 0.92f, 1f)); // 방어구 슬롯 버튼 생성
            SetRect(armorSlot.GetComponent<RectTransform>(), new Vector2(0.04f, 0.53f), new Vector2(0.24f, 0.66f)); // 방어구 슬롯 버튼 배치
            armorSlotText = armorSlot.GetComponentInChildren<Text>(); // 방어구 슬롯 라벨 조회
            armorSlot.onClick.AddListener(SelectEquippedArmor); // 방어구 슬롯 선택 이벤트 연결
            Button weaponSlot = CreateButton(panel.transform, "WeaponSlot", "무기\n비어 있음", new Color(0.88f, 0.83f, 0.76f, 1f)); // 무기 슬롯 버튼 생성
            SetRect(weaponSlot.GetComponent<RectTransform>(), new Vector2(0.38f, 0.41f), new Vector2(0.62f, 0.53f)); // 무기 슬롯 버튼 배치
            weaponSlotText = weaponSlot.GetComponentInChildren<Text>(); // 무기 슬롯 라벨 조회
            weaponSlot.onClick.AddListener(SelectEquippedWeapon); // 무기 슬롯 선택 이벤트 연결
            CreateLockedSlot(panel.transform, "GlovesPlaceholder", "장갑\n준비 중", new Vector2(0.76f, 0.68f), new Vector2(0.96f, 0.81f)); // 장갑 준비 슬롯 생성
            Text statsLabel = CreateText(panel.transform, "CurrentStatsLabel", "현재 최종 능력치", 20, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 현재 능력치 라벨 생성
            SetRect(statsLabel.rectTransform, new Vector2(0.06f, 0.345f), new Vector2(0.94f, 0.40f)); // 현재 능력치 라벨 배치
            currentStatsText = CreateText(panel.transform, "CurrentStats", "-", 16, FontStyle.Normal, new Color(0.20f, 0.23f, 0.27f, 1f)); // 현재 능력치 텍스트 생성
            currentStatsText.alignment = TextAnchor.UpperLeft; // 현재 능력치 왼쪽 정렬
            currentStatsText.horizontalOverflow = HorizontalWrapMode.Wrap; // 현재 능력치 가로 줄바꿈 적용
            currentStatsText.verticalOverflow = VerticalWrapMode.Truncate; // 현재 능력치 세로 영역 제한
            SetRect(currentStatsText.rectTransform, new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.34f)); // 현재 능력치 텍스트 배치
        }

        private void BuildEquipmentPanel(Transform parent) // 우측 장비 관리 영역 구성
        {
            Image panel = CreateImage(parent, "EquipmentPanel", new Color(0.96f, 0.96f, 0.96f, 1f)); // 장비 관리 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.41f, 0.05f), new Vector2(0.98f, 0.90f)); // 장비 관리 패널 배치
            AddOutline(panel.gameObject); // 장비 관리 패널 외곽선 추가
            Text inventoryLabel = CreateText(panel.transform, "InventoryLabel", "보유 장비", 22, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 장비 목록 라벨 생성
            SetRect(inventoryLabel.rectTransform, new Vector2(0.57f, 0.91f), new Vector2(0.72f, 0.98f)); // 오른쪽 장비 목록 라벨 배치
            inventoryCountText = CreateText(panel.transform, "InventoryCount", "0개", 17, FontStyle.Bold, new Color(0.30f, 0.34f, 0.40f, 1f)); // 장비 개수 텍스트 생성
            SetRect(inventoryCountText.rectTransform, new Vector2(0.72f, 0.91f), new Vector2(0.80f, 0.98f)); // 오른쪽 장비 개수 텍스트 배치
            Button grantButton = CreateButton(panel.transform, "GrantDemoEquipment", "테스트 장비 지급", new Color(0.88f, 0.83f, 0.66f, 1f)); // 테스트 장비 지급 버튼 생성
            SetRect(grantButton.GetComponent<RectTransform>(), new Vector2(0.80f, 0.915f), new Vector2(0.97f, 0.975f)); // 오른쪽 테스트 장비 지급 버튼 배치
            DevelopmentFeatures.HideInRelease(grantButton); // 출시 빌드에서는 테스트 장비 지급 버튼 숨김 (최적화)
            grantButton.onClick.AddListener(GrantDemoEquipment); // 테스트 장비 지급 이벤트 연결
            BuildInventoryScroll(panel.transform); // 장비 스크롤 목록 구성
            Image detailPanel = CreateImage(panel.transform, "DetailPanel", new Color(0.92f, 0.92f, 0.92f, 1f)); // 장비 상세 패널 생성
            SetRect(detailPanel.rectTransform, new Vector2(0.02f, 0.52f), new Vector2(0.54f, 0.90f)); // 왼쪽 장비 상세 패널 배치
            AddOutline(detailPanel.gameObject); // 장비 상세 패널 외곽선 추가
            detailTitleText = CreateText(detailPanel.transform, "DetailTitle", "장비를 선택하세요", 23, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 장비 상세 이름 생성
            detailTitleText.alignment = TextAnchor.MiddleLeft; // 장비 상세 이름 왼쪽 정렬
            SetRect(detailTitleText.rectTransform, new Vector2(0.04f, 0.80f), new Vector2(0.72f, 0.97f)); // 장비 상세 이름 배치
            detailGradeText = CreateText(detailPanel.transform, "DetailGrade", string.Empty, 20, FontStyle.Bold, new Color(0.63f, 0.48f, 0.14f, 1f)); // 장비 등급 텍스트 생성
            SetRect(detailGradeText.rectTransform, new Vector2(0.72f, 0.80f), new Vector2(0.96f, 0.97f)); // 장비 등급 텍스트 배치
            detailStatsText = CreateText(detailPanel.transform, "DetailStats", "-", 16, FontStyle.Normal, new Color(0.20f, 0.23f, 0.27f, 1f)); // 장비 옵션 텍스트 생성
            detailStatsText.alignment = TextAnchor.UpperLeft; // 장비 옵션 왼쪽 정렬
            detailStatsText.horizontalOverflow = HorizontalWrapMode.Wrap; // 장비 옵션 가로 줄바꿈 적용
            detailStatsText.verticalOverflow = VerticalWrapMode.Truncate; // 장비 옵션 세로 영역 제한
            SetRect(detailStatsText.rectTransform, new Vector2(0.05f, 0.07f), new Vector2(0.95f, 0.80f)); // 장비 옵션 텍스트 배치
            Image comparisonPanel = CreateImage(panel.transform, "ComparisonPanel", new Color(0.90f, 0.93f, 0.95f, 1f)); // 능력치 비교 패널 생성
            SetRect(comparisonPanel.rectTransform, new Vector2(0.02f, 0.13f), new Vector2(0.54f, 0.50f)); // 왼쪽 능력치 비교 패널 배치
            AddOutline(comparisonPanel.gameObject); // 능력치 비교 패널 외곽선 추가
            Text comparisonLabel = CreateText(comparisonPanel.transform, "ComparisonLabel", "변경 전 → 변경 후", 19, FontStyle.Bold, new Color(0.08f, 0.10f, 0.13f, 1f)); // 능력치 비교 라벨 생성
            SetRect(comparisonLabel.rectTransform, new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.98f)); // 능력치 비교 라벨 배치
            comparisonText = CreateText(comparisonPanel.transform, "ComparisonText", "장비를 선택하면 예상 능력치가 표시됩니다.", 15, FontStyle.Normal, new Color(0.18f, 0.22f, 0.27f, 1f)); // 능력치 비교 텍스트 생성
            comparisonText.alignment = TextAnchor.UpperLeft; // 능력치 비교 왼쪽 정렬
            comparisonText.horizontalOverflow = HorizontalWrapMode.Wrap; // 능력치 비교 가로 줄바꿈 적용
            comparisonText.verticalOverflow = VerticalWrapMode.Truncate; // 능력치 비교 세로 영역 제한
            SetRect(comparisonText.rectTransform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.84f)); // 능력치 비교 텍스트 배치
            actionButton = CreateButton(panel.transform, "ActionButton", "사용 불가", new Color(0.72f, 0.82f, 0.72f, 1f)); // 장비 액션 버튼 생성
            SetRect(actionButton.GetComponent<RectTransform>(), new Vector2(0.70f, 0.035f), new Vector2(0.97f, 0.105f)); // 장비 액션 버튼 배치
            actionButtonText = actionButton.GetComponentInChildren<Text>(); // 장비 액션 라벨 조회
            actionButton.onClick.AddListener(ApplySelectedEquipmentAction); // 장비 액션 이벤트 연결
            statusText = CreateText(panel.transform, "Status", "장비를 선택하세요.", 16, FontStyle.Normal, new Color(0.25f, 0.28f, 0.32f, 1f)); // 화면 상태 텍스트 생성
            statusText.alignment = TextAnchor.MiddleLeft; // 화면 상태 왼쪽 정렬
            SetRect(statusText.rectTransform, new Vector2(0.03f, 0.035f), new Vector2(0.67f, 0.105f)); // 화면 상태 텍스트 배치
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
            portraitText.text = displayName; // 임시 초상 텍스트 갱신
            UpdateAffinityDebugText(saveData, characterSave); // 호감도 디버그 텍스트 갱신 (Day43)
            if (affinityRewardPanel != null) affinityRewardPanel.Refresh(saveData, characterSave, displayName); // 호감도 보상 패널 갱신 (Day56 추가, Unity 객체 null 비교)
            if (giftPanel != null) giftPanel.Refresh(saveData, dataManager, characterSave, displayName); // 선물 패널 갱신 (Day57 추가)
            UpdateCurrentStats(saveData, dataManager, characterSave, characterData); // 현재 최종 능력치 갱신
            UpdateSlotText(saveData, dataManager, characterSave, EquipmentSlot.Weapon, weaponSlotText, "무기"); // 무기 슬롯 갱신
            UpdateSlotText(saveData, dataManager, characterSave, EquipmentSlot.Armor, armorSlotText, "방어구"); // 방어구 슬롯 갱신
            EnsureSelectedInstance(saveData); // 선택 장비 인스턴스 보정
            BuildInventoryButtons(saveData, dataManager, characterSave); // 보유 장비 목록 구성
            UpdateDetail(saveData, dataManager, characterSave); // 선택 장비 상세 갱신
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

        private void UpdateSlotText(SaveData saveData, DataManager dataManager, CharacterSaveData characterSave, EquipmentSlot slot, Text target, string slotLabel) // 장착 슬롯 텍스트 갱신
        {
            EquipmentInstanceSaveData instance = CharacterEquipmentService.GetEquippedInstance(saveData, characterSave.CharacterId, slot); // 슬롯 장착 장비 조회

            if (instance == null) // 장착 장비 존재 확인
            {
                target.text = $"{slotLabel}\n비어 있음"; // 빈 장비 슬롯 표시
                return; // 슬롯 갱신 종료
            }

            EquipmentData equipment = dataManager.GetEquipment(instance.EquipmentId); // 장착 장비 원본 조회
            string equipmentName = equipment == null ? instance.EquipmentId : equipment.DisplayName; // 장착 장비 표시 이름 결정
            target.text = $"{slotLabel}\n{equipmentName}"; // 장착 장비 슬롯 표시
        }

        private void UpdateAffinityDebugText(SaveData saveData, CharacterSaveData characterSave) // 호감도 디버그 텍스트 갱신 (Day43)
        {
            if (affinityText == null) // 호감도 디버그 텍스트 존재 확인
            {
                return; // 호감도 디버그 텍스트 갱신 중단
            }

            int affinity = AffinityService.GetAffinity(saveData, characterSave.CharacterId); // 선택 캐릭터 호감도 조회
            AffinityTier tier = AffinityService.GetAffinityTier(saveData, characterSave.CharacterId); // 선택 캐릭터 호감도 등급 조회
            affinityText.text = $"호감도 · {affinity}/{CharacterSaveData.MaxAffinity} · {GetAffinityTierLabel(tier)}"; // 호감도 디버그 문구 표시
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
                string equipmentName = equipment == null ? instance.EquipmentId : equipment.DisplayName; // 장비 표시 이름 결정
                CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, instance.InstanceId); // 장비 착용 캐릭터 조회
                string state = BuildEquipmentState(owner, characterSave); // 장비 착용 상태 문구 생성
                string slotName = equipment == null ? "?" : equipment.Slot == EquipmentSlot.Weapon ? "W" : "A"; // 장비 슬롯 축약 문구 생성
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

        private void SelectEquippedWeapon() // 현재 무기 슬롯 선택
        {
            SelectEquippedSlot(EquipmentSlot.Weapon); // 무기 슬롯 선택 실행
        }

        private void SelectEquippedArmor() // 현재 방어구 슬롯 선택
        {
            SelectEquippedSlot(EquipmentSlot.Armor); // 방어구 슬롯 선택 실행
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
            return slot == EquipmentSlot.Weapon ? "무기" : "방어구"; // 장비 슬롯 이름 반환
        }

        private static string GetGradeStars(ItemGrade grade) // 장비 등급 별 문구 생성
        {
            int starCount = Mathf.Clamp((int)grade + 1, 1, 5); // 장비 등급 별 개수 계산
            return new string('★', starCount) + new string('☆', 5 - starCount); // 장비 등급 별 문구 반환
        }

        private static void CreateLockedSlot(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax) // 준비 중 장비 슬롯 생성
        {
            Button slot = CreateButton(parent, name, label, new Color(0.88f, 0.88f, 0.88f, 1f)); // 준비 중 슬롯 버튼 생성
            SetRect(slot.GetComponent<RectTransform>(), anchorMin, anchorMax); // 준비 중 슬롯 배치
            slot.interactable = false; // 준비 중 슬롯 입력 비활성화
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
