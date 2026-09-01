using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 캐릭터 데이터 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 컨트롤러 방지
    public sealed class PartyScreenController : MonoBehaviour // 실제 파티 편성 화면 컨트롤러
    {
        [SerializeField] private PartySlotView[] slotViews; // 4인 파티 슬롯 뷰
        [SerializeField] private Button[] presetButtons; // 편성 프리셋 버튼
        [SerializeField] private Text statusText; // 화면 상태 텍스트
        [SerializeField] private Text presetStateText; // 프리셋 상태 텍스트
        [SerializeField] private Button confirmButton; // 편성 확정 버튼
        [SerializeField] private Button lobbyButton; // 로비 이동 버튼
        [SerializeField] private Button dungeonButton; // 던전 이동 버튼
        [SerializeField] private Button helpButton; // 도움말 버튼
        [SerializeField] private GameObject helpPanel; // 도움말 패널
        [SerializeField] private GameObject popupRoot; // 캐릭터 선택 팝업
        [SerializeField] private Text popupTitleText; // 팝업 제목
        [SerializeField] private Text popupStatusText; // 팝업 상태
        [SerializeField] private Transform rosterContent; // 캐릭터 카드 컨테이너
        [SerializeField] private PartyCharacterCardView cardTemplate; // 캐릭터 카드 템플릿
        [SerializeField] private Button[] filterButtons; // 역할 필터 버튼
        [SerializeField] private Button clearSlotButton; // 슬롯 비우기 버튼
        [SerializeField] private Button cancelPopupButton; // 팝업 취소 버튼
        private readonly List<PartyCharacterCardView> spawnedCards = new List<PartyCharacterCardView>(); // 생성 카드 목록
        private PartyEditState editState; // 파티 임시 편집 상태
        private SaveData currentSave; // 현재 저장 데이터
        private PartyRoleFilter activeFilter = PartyRoleFilter.All; // 현재 역할 필터
        private int editingSlotIndex = -1; // 현재 편집 슬롯 번호
        private bool isTransitioning; // 씬 전환 잠금 상태
        private bool buttonsBound; // 버튼 이벤트 연결 여부

        public void Configure(PartySlotView[] slots, Button[] presets, Text status, Text presetState, Button confirm, Button lobby, Button dungeon, Button help, GameObject helpTarget, GameObject popup, Text popupTitle, Text popupStatus, Transform roster, PartyCharacterCardView template, Button[] filters, Button clear, Button cancel) // 에디터 참조 설정
        {
            slotViews = slots; // 파티 슬롯 연결
            presetButtons = presets; // 프리셋 버튼 연결
            statusText = status; // 상태 텍스트 연결
            presetStateText = presetState; // 프리셋 상태 연결
            confirmButton = confirm; // 확정 버튼 연결
            lobbyButton = lobby; // 로비 버튼 연결
            dungeonButton = dungeon; // 던전 버튼 연결
            helpButton = help; // 도움말 버튼 연결
            helpPanel = helpTarget; // 도움말 패널 연결
            popupRoot = popup; // 팝업 루트 연결
            popupTitleText = popupTitle; // 팝업 제목 연결
            popupStatusText = popupStatus; // 팝업 상태 연결
            rosterContent = roster; // 카드 컨테이너 연결
            cardTemplate = template; // 카드 템플릿 연결
            filterButtons = filters; // 필터 버튼 연결
            clearSlotButton = clear; // 슬롯 비우기 버튼 연결
            cancelPopupButton = cancel; // 팝업 취소 버튼 연결
        }

        private void Start() // 파티 화면 시작
        {
            BindButtons(); // 런타임 버튼 이벤트 연결
            InitializeScreen(); // 파티 화면 데이터 초기화
        }

        private void OnDestroy() // 파티 화면 종료
        {
            ClearSpawnedCards(); // 생성 캐릭터 카드 정리
        }

        public void Refresh() // 파티 화면 전체 갱신
        {
            RefreshSlots(); // 메인 슬롯 갱신
            RefreshPresetButtons(); // 프리셋 버튼 갱신
            RefreshConfirmState(); // 확정 버튼 상태 갱신
        }

        public void OpenCharacterPopup(int slotIndex) // 캐릭터 선택 팝업 열기
        {
            if (editState == null || !editState.CanOpenSlot(slotIndex)) // 편집 상태 및 슬롯 확인
            {
                SetText(statusText, "앞쪽 빈 슬롯부터 편성해 주세요."); // 슬롯 선택 안내 표시
                return; // 팝업 열기 중단
            }

            editingSlotIndex = slotIndex; // 편집 슬롯 저장
            activeFilter = PartyRoleFilter.All; // 역할 필터 초기화
            popupRoot?.SetActive(true); // 캐릭터 선택 팝업 표시
            RefreshPopup(); // 팝업 데이터 갱신
        }

        public void CloseCharacterPopup() // 캐릭터 선택 팝업 닫기
        {
            editingSlotIndex = -1; // 편집 슬롯 초기화
            popupRoot?.SetActive(false); // 캐릭터 선택 팝업 숨김
            ClearSpawnedCards(); // 생성 카드 정리
        }

        public void ConfirmParty() // 편성 확정 및 저장
        {
            if (isTransitioning || editState == null || currentSave == null) // 확정 가능 상태 확인
            {
                return; // 편성 확정 중단
            }

            if (!editState.CommitTo(currentSave, out string error)) // 편집 상태 저장 데이터 반영
            {
                SetText(statusText, error); // 편성 반영 실패 표시
                return; // 편성 확정 중단
            }

            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                SetText(statusText, "SaveManager를 찾을 수 없습니다."); // 저장 관리자 오류 표시
                return; // 저장 중단
            }

            if (!GameManager.Instance.Save.SaveCurrent()) // 실제 저장 실행
            {
                SetText(statusText, "편성 데이터를 저장하지 못했습니다."); // 저장 실패 표시
                return; // 저장 중단
            }

            editState = PartyEditState.Create(currentSave); // 저장 완료 편집 상태 재생성
            SetText(statusText, $"편성 #{editState.SelectedPresetIndex + 1} 저장 완료"); // 저장 완료 표시
            Refresh(); // 메인 화면 갱신
        }

        public void GoLobby() // 로비 화면 이동
        {
            BeginSceneTransition(GameScenes.Lobby); // 로비 씬 전환
        }

        public void GoDungeonSelect() // 던전 선택 화면 이동
        {
            if (editState != null && editState.IsDirty) // 미저장 편성 확인
            {
                SetText(statusText, "던전 이동 전 편성을 확정해 주세요."); // 편성 확정 안내 표시
                return; // 던전 이동 중단
            }

            BeginSceneTransition(GameScenes.DungeonSelect); // 던전 선택 씬 전환
        }

        public void ToggleHelp() // 도움말 패널 전환
        {
            if (helpPanel != null) // 도움말 패널 확인
            {
                helpPanel.SetActive(!helpPanel.activeSelf); // 도움말 표시 상태 전환
            }
        }

        private void InitializeScreen() // 파티 화면 데이터 초기화
        {
            popupRoot?.SetActive(false); // 시작 시 팝업 숨김
            helpPanel?.SetActive(false); // 시작 시 도움말 숨김

            if (GameManager.Instance == null || GameManager.Instance.Save == null || GameManager.Instance.Data == null) // 게임 관리자 상태 확인
            {
                SetText(statusText, "Bootstrap 씬부터 실행해 주세요."); // Bootstrap 실행 안내
                SetMainInteraction(false); // 메인 입력 잠금
                return; // 화면 초기화 중단
            }

            currentSave = GameManager.Instance.Save.CurrentSave; // 현재 저장 데이터 조회

            if (currentSave == null) // 저장 데이터 확인
            {
                SetText(statusText, "진행 데이터가 없습니다. 타이틀에서 새 게임 또는 이어하기를 선택해 주세요."); // 저장 없음 안내
                SetMainInteraction(false); // 메인 입력 잠금
                return; // 화면 초기화 중단
            }

            editState = PartyEditState.Create(currentSave); // 임시 파티 편집 상태 생성
            SetText(statusText, "캐릭터 슬롯을 눌러 편성을 변경하세요."); // 편성 안내 표시
            SetMainInteraction(true); // 메인 입력 활성화
            Refresh(); // 파티 화면 갱신
        }

        private void BindButtons() // 런타임 버튼 이벤트 연결
        {
            if (buttonsBound) // 기존 버튼 연결 확인
            {
                return; // 중복 버튼 연결 중단
            }

            buttonsBound = true; // 버튼 연결 완료 기록

            for (int index = 0; slotViews != null && index < slotViews.Length; index++) // 슬롯 뷰 순회
            {
                int slotIndex = index; // 슬롯 콜백 번호 복사

                if (slotViews[index] != null && slotViews[index].SlotButton != null) // 슬롯 버튼 확인
                {
                    slotViews[index].SlotButton.onClick.AddListener(() => OpenCharacterPopup(slotIndex)); // 슬롯 팝업 이벤트 연결
                }
            }

            for (int index = 0; presetButtons != null && index < presetButtons.Length; index++) // 프리셋 버튼 순회
            {
                int presetIndex = index; // 프리셋 콜백 번호 복사
                presetButtons[index]?.onClick.AddListener(() => SelectPreset(presetIndex)); // 프리셋 선택 이벤트 연결
            }

            for (int index = 0; filterButtons != null && index < filterButtons.Length; index++) // 필터 버튼 순회
            {
                int filterIndex = index; // 필터 콜백 번호 복사
                filterButtons[index]?.onClick.AddListener(() => SetFilter((PartyRoleFilter)filterIndex)); // 역할 필터 이벤트 연결
            }

            confirmButton?.onClick.AddListener(ConfirmParty); // 편성 확정 이벤트 연결
            lobbyButton?.onClick.AddListener(GoLobby); // 로비 이동 이벤트 연결
            dungeonButton?.onClick.AddListener(GoDungeonSelect); // 던전 이동 이벤트 연결
            helpButton?.onClick.AddListener(ToggleHelp); // 도움말 이벤트 연결
            clearSlotButton?.onClick.AddListener(ClearEditingSlot); // 슬롯 비우기 이벤트 연결
            cancelPopupButton?.onClick.AddListener(CloseCharacterPopup); // 팝업 취소 이벤트 연결
        }

        private void SelectPreset(int presetIndex) // 편성 프리셋 선택 처리
        {
            if (editState == null) // 편집 상태 확인
            {
                SetText(statusText, "파티 편집 상태가 없습니다."); // 편집 상태 오류 표시
                return; // 프리셋 선택 중단
            }

            if (!editState.TrySelectPreset(presetIndex, out string error)) // 프리셋 선택 실행
            {
                SetText(statusText, error); // 프리셋 선택 실패 표시
                return; // 프리셋 선택 중단
            }

            CloseCharacterPopup(); // 열린 팝업 정리
            SetText(statusText, $"편성 #{presetIndex + 1} 편집 중"); // 프리셋 편집 상태 표시
            Refresh(); // 메인 화면 갱신
        }

        private void SetFilter(PartyRoleFilter filter) // 역할 필터 선택 처리
        {
            activeFilter = filter; // 활성 필터 저장
            RefreshPopup(); // 팝업 목록 갱신
        }

        private void SelectCharacter(string characterId) // 팝업 캐릭터 선택 처리
        {
            if (editState == null || editingSlotIndex < 0) // 편집 상태 확인
            {
                return; // 캐릭터 선택 중단
            }

            string previousCharacterId = editState.GetMemberAtSlot(editingSlotIndex); // 기존 슬롯 캐릭터 조회

            if (!editState.TryAssignCharacter(editingSlotIndex, characterId, out string error)) // 슬롯 캐릭터 교체
            {
                SetText(popupStatusText, error); // 캐릭터 선택 실패 표시
                return; // 캐릭터 선택 중단
            }

            bool changed = !string.Equals(previousCharacterId, characterId, System.StringComparison.Ordinal); // 실제 교체 여부 계산
            CloseCharacterPopup(); // 캐릭터 선택 팝업 닫기
            SetText(statusText, changed ? "편성이 변경되었습니다. 확정 버튼으로 저장하세요." : "현재 캐릭터를 유지합니다."); // 변경 상태 안내 표시
            Refresh(); // 메인 화면 갱신
        }

        private void ClearEditingSlot() // 현재 슬롯 비우기 처리
        {
            if (editState == null || editingSlotIndex < 0) // 편집 상태 확인
            {
                return; // 슬롯 비우기 중단
            }

            if (!editState.TryClearSlot(editingSlotIndex, out string error)) // 슬롯 캐릭터 제거
            {
                SetText(popupStatusText, error); // 슬롯 비우기 실패 표시
                return; // 슬롯 비우기 중단
            }

            CloseCharacterPopup(); // 캐릭터 선택 팝업 닫기
            SetText(statusText, "슬롯을 비웠습니다. 확정 버튼으로 저장하세요."); // 슬롯 변경 안내 표시
            Refresh(); // 메인 화면 갱신
        }

        private void RefreshSlots() // 메인 파티 슬롯 갱신
        {
            if (editState == null || slotViews == null) // 편집 상태 확인
            {
                return; // 슬롯 갱신 중단
            }

            for (int index = 0; index < slotViews.Length; index++) // 슬롯 목록 순회
            {
                PartySlotView slotView = slotViews[index]; // 현재 슬롯 뷰 조회

                if (slotView == null) // 슬롯 뷰 확인
                {
                    continue; // null 슬롯 제외
                }

                string characterId = editState.GetMemberAtSlot(index); // 슬롯 캐릭터 ID 조회
                bool interactable = editState.CanOpenSlot(index) && !isTransitioning; // 슬롯 상호작용 가능 여부 계산

                if (string.IsNullOrEmpty(characterId)) // 빈 슬롯 확인
                {
                    slotView.SetEmpty(interactable); // 빈 슬롯 표시
                    continue; // 다음 슬롯 이동
                }

                CharacterData character = GameManager.Instance.Data.GetCharacter(characterId); // 캐릭터 원본 조회
                CharacterSaveData progress = currentSave.FindCharacter(characterId); // 캐릭터 진행 조회
                slotView.SetCharacter(character, progress, interactable); // 캐릭터 슬롯 표시
            }
        }

        private void RefreshPresetButtons() // 프리셋 버튼 표시 갱신
        {
            if (presetButtons == null || editState == null) // 프리셋 버튼 확인
            {
                return; // 프리셋 갱신 중단
            }

            for (int index = 0; index < presetButtons.Length; index++) // 프리셋 버튼 순회
            {
                Button button = presetButtons[index]; // 현재 프리셋 버튼 조회

                if (button == null) // 프리셋 버튼 확인
                {
                    continue; // null 버튼 제외
                }

                Image image = button.targetGraphic as Image; // 버튼 이미지 조회

                if (image != null) // 버튼 이미지 확인
                {
                    image.color = index == editState.SelectedPresetIndex ? new Color(0.62f, 0.80f, 1f, 1f) : Color.white; // 선택 프리셋 강조
                }

                button.interactable = !isTransitioning; // 프리셋 버튼 입력 상태 적용
            }

            string dirtyLabel = editState.IsDirty ? " · 미저장 변경" : " · 저장됨"; // 편집 상태 문구 생성
            SetText(presetStateText, $"편성 #{editState.SelectedPresetIndex + 1}{dirtyLabel}"); // 활성 프리셋 상태 표시
        }

        private void RefreshConfirmState() // 편성 확정 버튼 갱신
        {
            if (confirmButton != null) // 확정 버튼 확인
            {
                confirmButton.interactable = editState != null && editState.IsDirty && !isTransitioning; // 변경 있을 때 확정 활성화
            }
        }

        private void RefreshPopup() // 캐릭터 선택 팝업 갱신
        {
            if (editState == null || editingSlotIndex < 0 || currentSave == null) // 팝업 상태 확인
            {
                return; // 팝업 갱신 중단
            }

            ClearSpawnedCards(); // 기존 카드 정리
            string currentCharacterId = editState.GetMemberAtSlot(editingSlotIndex); // 현재 슬롯 캐릭터 조회
            SetText(popupTitleText, $"캐릭터 선택 · SLOT {editingSlotIndex + 1}"); // 팝업 제목 표시
            SetText(popupStatusText, $"보유 {currentSave.Characters.Count}명 · {GetFilterLabel(activeFilter)}"); // 팝업 상태 표시
            RefreshFilterButtons(); // 필터 버튼 표시 갱신

            foreach (CharacterSaveData progress in currentSave.Characters) // 보유 캐릭터 순회
            {
                if (progress == null || string.IsNullOrWhiteSpace(progress.CharacterId)) // 캐릭터 저장 유효성 확인
                {
                    continue; // 잘못된 캐릭터 제외
                }

                CharacterData character = GameManager.Instance.Data.GetCharacter(progress.CharacterId); // 캐릭터 원본 조회

                if (character == null || !PartyRosterFilter.Matches(activeFilter, character.Position)) // 원본 및 필터 확인
                {
                    continue; // 표시 대상 제외
                }

                PartyCharacterCardView card = Instantiate(cardTemplate, rosterContent); // 캐릭터 카드 복제
                card.gameObject.SetActive(true); // 복제 카드 표시
                bool isCurrentSlot = string.Equals(currentCharacterId, character.Id, System.StringComparison.Ordinal); // 현재 슬롯 캐릭터 여부 확인
                int existingSlot = editState.GetCharacterSlot(character.Id); // 기존 편성 슬롯 조회
                bool isOtherPartySlot = existingSlot >= 0 && existingSlot != editingSlotIndex; // 다른 슬롯 편성 여부 계산
                card.Bind(character, progress, isCurrentSlot, isOtherPartySlot, SelectCharacter); // 카드 데이터 연결
                spawnedCards.Add(card); // 생성 카드 목록 등록
            }

            if (clearSlotButton != null) // 슬롯 비우기 버튼 확인
            {
                clearSlotButton.interactable = editState.MemberCount > 1 && editingSlotIndex < editState.MemberCount; // 최소 인원 초과 시 활성화
            }
        }

        private void RefreshFilterButtons() // 역할 필터 버튼 표시 갱신
        {
            for (int index = 0; filterButtons != null && index < filterButtons.Length; index++) // 필터 버튼 순회
            {
                Image image = filterButtons[index] == null ? null : filterButtons[index].targetGraphic as Image; // 필터 버튼 이미지 조회

                if (image != null) // 필터 버튼 이미지 확인
                {
                    image.color = index == (int)activeFilter ? new Color(0.72f, 0.86f, 1f, 1f) : Color.white; // 선택 필터 강조
                }
            }
        }

        private void ClearSpawnedCards() // 생성 캐릭터 카드 정리
        {
            foreach (PartyCharacterCardView card in spawnedCards) // 생성 카드 목록 순회
            {
                if (card != null) // 카드 존재 확인
                {
                    Destroy(card.gameObject); // 생성 카드 제거
                }
            }

            spawnedCards.Clear(); // 생성 카드 목록 초기화
        }

        private void BeginSceneTransition(string sceneName) // 공통 씬 전환
        {
            if (isTransitioning) // 기존 전환 상태 확인
            {
                return; // 중복 전환 차단
            }

            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                SetText(statusText, "SceneLoader를 찾을 수 없습니다."); // 씬 로더 오류 표시
                return; // 씬 전환 중단
            }

            isTransitioning = true; // 전환 잠금 활성화
            SetMainInteraction(false); // 메인 입력 잠금
            GameManager.Instance.Scenes.LoadScene(sceneName); // 대상 씬 이동
        }

        private void SetMainInteraction(bool enabled) // 메인 화면 입력 상태 설정
        {
            for (int index = 0; slotViews != null && index < slotViews.Length; index++) // 슬롯 목록 순회
            {
                if (slotViews[index] != null && slotViews[index].SlotButton != null) // 슬롯 버튼 확인
                {
                    slotViews[index].SlotButton.interactable = enabled; // 슬롯 입력 상태 적용
                }
            }

            for (int index = 0; presetButtons != null && index < presetButtons.Length; index++) // 프리셋 버튼 순회
            {
                if (presetButtons[index] != null) // 프리셋 버튼 확인
                {
                    presetButtons[index].interactable = enabled; // 프리셋 입력 상태 적용
                }
            }

            if (lobbyButton != null) // 로비 버튼 확인
            {
                lobbyButton.interactable = enabled; // 로비 버튼 입력 상태 적용
            }

            if (dungeonButton != null) // 던전 버튼 확인
            {
                dungeonButton.interactable = enabled; // 던전 버튼 입력 상태 적용
            }

            if (helpButton != null) // 도움말 버튼 확인
            {
                helpButton.interactable = enabled; // 도움말 버튼 입력 상태 적용
            }
            RefreshConfirmState(); // 확정 버튼 상태 갱신
        }

        private static string GetFilterLabel(PartyRoleFilter filter) // 역할 필터 표시 이름 반환
        {
            switch (filter) // 역할 필터 종류 분기
            {
                case PartyRoleFilter.Tank: // 탱커 필터 처리
                    return "탱커"; // 탱커 라벨 반환
                case PartyRoleFilter.Dealer: // 딜러 필터 처리
                    return "딜러"; // 딜러 라벨 반환
                case PartyRoleFilter.Healer: // 힐러 필터 처리
                    return "힐러"; // 힐러 라벨 반환
                default: // 전체 필터 처리
                    return "전체"; // 전체 라벨 반환
            }
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target != null) // 텍스트 참조 확인
            {
                target.text = value; // 텍스트 값 적용
            }
        }
    }
}
