using System.Collections.Generic; // 하단 내비게이션 버튼 목록 기능
using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Events; // 프로젝트 이벤트 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 컨트롤러 방지
    public sealed class LobbyScreenController : MonoBehaviour // 로비 화면 전용 컨트롤러
    {
        [SerializeField] private Text statusText; // 진행 상태 텍스트
        [SerializeField] private Text bodyText; // 챕터 본문 텍스트
        [SerializeField] private Text saveStateText; // 저장 상태 텍스트
        [SerializeField] private Text partyText; // 파티 요약 텍스트
        [SerializeField] private Button saveButton; // 저장 버튼
        [SerializeField] private Button partyButton; // 파티 이동 버튼
        [SerializeField] private Button dungeonButton; // 던전 이동 버튼
        [SerializeField] private Button titleButton; // 타이틀 이동 버튼
        [SerializeField] private Button characterButton; // 기존 하단 캐릭터 이동 버튼
        [SerializeField] private Button bagButton; // Runtime 가방 이동 버튼

        private bool isTransitioning; // 씬 전환 잠금 상태

        public void Configure(Text status, Text body, Text saveState, Text party, Button saveTarget, Button partyTarget, Button dungeonTarget, Button titleTarget) // 에디터 참조 설정
        {
            statusText = status; // 상태 텍스트 연결
            bodyText = body; // 본문 텍스트 연결
            saveStateText = saveState; // 저장 상태 연결
            partyText = party; // 파티 요약 연결
            saveButton = saveTarget; // 저장 버튼 연결
            partyButton = partyTarget; // 파티 버튼 연결
            dungeonButton = dungeonTarget; // 던전 버튼 연결
            titleButton = titleTarget; // 타이틀 버튼 연결
        }

        private void OnEnable() // 저장 이벤트 구독
        {
            ProjectHEventBus.Subscribe<SaveLifecycleEvent>(OnSaveLifecycle); // 저장 이벤트 연결
        }

        private void OnDisable() // 저장 이벤트 해제
        {
            ProjectHEventBus.Unsubscribe<SaveLifecycleEvent>(OnSaveLifecycle); // 저장 이벤트 연결 해제
        }

        private void Start() // 로비 초기 표시
        {
            BindCharacterButton(); // 기존 하단 캐릭터 버튼 연결
            BindBagButton(); // 하단 가방 버튼 연결
            Refresh(); // 화면 상태 갱신
        }

        public void Refresh() // 로비 화면 갱신
        {
            if (GameManager.Instance == null) // 게임 관리자 확인
            {
                SetText(statusText, "Bootstrap 씬부터 실행해 주세요."); // 실행 안내 표시
                SetPlayable(false); // 진행 버튼 잠금
                return; // 화면 갱신 중단
            }

            if (characterButton == null) // 캐릭터 버튼 연결 여부 확인
            {
                BindCharacterButton(); // 기존 하단 캐릭터 버튼 재탐색
            }

            if (bagButton == null) // 가방 버튼 연결 여부 확인
            {
                BindBagButton(); // 하단 가방 버튼 재탐색 및 생성
            }

            SaveManager saveManager = GameManager.Instance.Save; // 저장 관리자 조회
            SaveData saveData = saveManager == null ? null : saveManager.CurrentSave; // 현재 저장 데이터 조회
            bool hasSaveData = saveManager != null && saveManager.HasSaveData; // 저장 파일 상태 조회
            LobbyScreenViewData state = LobbyScreenViewData.Build(GameManager.Instance.Data, saveData, hasSaveData); // 로비 표시 데이터 생성
            SetText(statusText, state.StatusText); // 진행 상태 표시
            SetText(bodyText, state.BodyText); // 챕터 본문 표시
            SetText(saveStateText, state.SaveStateText); // 저장 상태 표시
            SetText(partyText, state.PartyText); // 파티 요약 표시
            SetPlayable(state.CanNavigate && !isTransitioning); // 주요 버튼 상태 적용
        }

        public void SaveGame() // 현재 진행 저장
        {
            if (isTransitioning) // 전환 상태 확인
            {
                return; // 저장 입력 중단
            }

            if (GameManager.Instance == null || GameManager.Instance.Save == null) // 저장 관리자 확인
            {
                SetText(saveStateText, "SAVE FAILED · MANAGER MISSING"); // 관리자 누락 표시
                return; // 저장 중단
            }

            if (!GameManager.Instance.Save.SaveCurrent()) // 저장 실행 결과 확인
            {
                SetText(saveStateText, "SAVE FAILED"); // 저장 실패 표시
            }
        }

        public void GoLobby() // 현재 로비 새로고침
        {
            if (isTransitioning) // 전환 상태 확인
            {
                return; // 중복 입력 중단
            }

            Refresh(); // 현재 로비 상태 갱신
        }

        public void GoParty() // 파티 화면 이동
        {
            BeginSceneTransition(GameScenes.Party); // 파티 씬 전환
        }

        public void GoDungeonSelect() // 던전 선택 이동
        {
            BeginSceneTransition(GameScenes.DungeonSelect); // 던전 선택 씬 전환
        }

        public void GoCharacter() // 캐릭터 화면 이동
        {
            BeginSceneTransition(GameScenes.Character); // 캐릭터 씬 전환
        }

        public void GoBag() // 가방 화면 이동
        {
            BeginSceneTransition(GameScenes.Bag); // 가방 씬 전환
        }

        public void GoTitle() // 타이틀 화면 이동
        {
            BeginSceneTransition(GameScenes.Title); // 타이틀 씬 전환
        }

        private void BindCharacterButton() // 기존 하단 캐릭터 버튼 Runtime 연결
        {
            GameObject staleButton = GameObject.Find("CharacterSceneEntryButton"); // 이전 Runtime 추가 버튼 조회

            if (staleButton != null) // 이전 Runtime 추가 버튼 존재 확인
            {
                Destroy(staleButton); // 이전 Runtime 추가 버튼 제거
            }

            if (characterButton == null) // 기존 캐릭터 버튼 참조 확인
            {
                Transform navigationRoot = transform.Find("BottomNavigation"); // 하단 내비게이션 루트 조회
                Transform searchRoot = navigationRoot == null ? transform : navigationRoot; // 캐릭터 버튼 검색 루트 결정
                Button[] navigationButtons = searchRoot.GetComponentsInChildren<Button>(true); // 기존 하단 버튼 목록 조회

                for (int index = 0; index < navigationButtons.Length; index++) // 하단 버튼 목록 순회
                {
                    Button candidate = navigationButtons[index]; // 현재 버튼 조회
                    Text label = candidate == null ? null : candidate.GetComponentInChildren<Text>(true); // 현재 버튼 라벨 조회

                    if (label == null || label.text == null || label.text.Trim() != "캐릭터") // 캐릭터 버튼 라벨 확인
                    {
                        continue; // 다른 버튼 건너뛰기
                    }

                    characterButton = candidate; // 기존 캐릭터 버튼 참조 저장
                    break; // 캐릭터 버튼 검색 종료
                }
            }

            if (characterButton == null) // 기존 캐릭터 버튼 검색 결과 확인
            {
                Debug.LogWarning("[Project H] Lobby character button was not found."); // 캐릭터 버튼 누락 로그
                return; // 캐릭터 버튼 연결 중단
            }

            characterButton.onClick.RemoveListener(GoCharacter); // 중복 캐릭터 이동 이벤트 제거
            characterButton.onClick.AddListener(GoCharacter); // 기존 하단 캐릭터 버튼 이동 이벤트 연결
        }

        private void BindBagButton() // 하단 가방 버튼 Runtime 연결
        {
            Transform navigationRoot = transform.Find("BottomNavigation"); // 하단 내비게이션 루트 조회

            if (navigationRoot == null) // 내비게이션 루트 존재 확인
            {
                Debug.LogWarning("[Project H] Lobby BottomNavigation was not found."); // 하단 내비게이션 누락 로그
                return; // 가방 버튼 연결 중단
            }

            Button[] navigationButtons = navigationRoot.GetComponentsInChildren<Button>(true); // 하단 버튼 목록 조회

            for (int index = 0; index < navigationButtons.Length; index++) // 기존 하단 버튼 순회
            {
                Button candidate = navigationButtons[index]; // 현재 버튼 조회
                Text label = candidate == null ? null : candidate.GetComponentInChildren<Text>(true); // 현재 버튼 라벨 조회

                if (label == null || label.text == null || label.text.Trim() != "가방") // 가방 버튼 라벨 확인
                {
                    continue; // 다른 버튼 건너뛰기
                }

                bagButton = candidate; // 기존 가방 버튼 참조 저장
                break; // 가방 버튼 검색 종료
            }

            if (bagButton == null) // 기존 가방 버튼 존재 확인
            {
                bagButton = CreateRuntimeBagButton(navigationRoot, navigationButtons); // Runtime 가방 버튼 생성
            }

            if (bagButton == null) // 가방 버튼 생성 결과 확인
            {
                Debug.LogWarning("[Project H] Lobby bag button could not be created."); // 가방 버튼 생성 실패 로그
                return; // 가방 버튼 연결 중단
            }

            bagButton.onClick.RemoveListener(GoBag); // 중복 가방 이동 이벤트 제거
            bagButton.onClick.AddListener(GoBag); // 가방 씬 이동 이벤트 연결
            NormalizeBottomNavigation(navigationRoot); // 하단 내비게이션 6칸 재배치
        }

        private Button CreateRuntimeBagButton(Transform navigationRoot, Button[] navigationButtons) // Runtime 가방 버튼 생성
        {
            Button template = characterButton; // 캐릭터 버튼 템플릿 우선 선택

            if (template == null && navigationButtons != null && navigationButtons.Length > 0) // 캐릭터 버튼 템플릿 누락 확인
            {
                template = navigationButtons[0]; // 첫 하단 버튼 템플릿 선택
            }

            GameObject buttonObject = new GameObject("BagSceneEntryButton", typeof(RectTransform), typeof(Image), typeof(Button)); // 가방 버튼 객체 생성
            buttonObject.transform.SetParent(navigationRoot, false); // 하단 내비게이션 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 가방 버튼 이미지 조회
            Button button = buttonObject.GetComponent<Button>(); // 가방 버튼 컴포넌트 조회

            if (template != null) // 기존 버튼 템플릿 존재 확인
            {
                Image templateImage = template.GetComponent<Image>(); // 템플릿 버튼 이미지 조회

                if (templateImage != null) // 템플릿 이미지 존재 확인
                {
                    image.sprite = templateImage.sprite; // 기존 하단 버튼 스프라이트 복사
                    image.type = templateImage.type; // 기존 하단 버튼 이미지 유형 복사
                    image.color = templateImage.color; // 기존 하단 버튼 색상 복사
                }

                button.colors = template.colors; // 기존 하단 버튼 전환 색상 복사
                button.transition = template.transition; // 기존 하단 버튼 전환 방식 복사
            }
            else // 기존 버튼 템플릿 누락 처리
            {
                image.color = new Color(0.90f, 0.95f, 1f, 1f); // 기본 가방 버튼 색상 적용
            }

            button.targetGraphic = image; // 가방 버튼 대상 그래픽 연결
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); // 가방 버튼 라벨 객체 생성
            labelObject.transform.SetParent(buttonObject.transform, false); // 가방 버튼 라벨 부모 연결
            Text label = labelObject.GetComponent<Text>(); // 가방 버튼 라벨 조회
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            label.text = "가방"; // 가방 버튼 라벨 설정
            label.fontSize = 21; // 가방 버튼 글자 크기 설정
            label.fontStyle = FontStyle.Bold; // 가방 버튼 글자 스타일 설정
            label.color = new Color(0.18f, 0.27f, 0.40f, 1f); // 가방 버튼 글자 색상 설정
            label.alignment = TextAnchor.MiddleCenter; // 가방 버튼 글자 중앙 정렬
            label.resizeTextForBestFit = true; // 가방 버튼 글자 자동 크기 활성화
            label.resizeTextMinSize = 12; // 가방 버튼 최소 글자 크기 설정
            label.resizeTextMaxSize = 21; // 가방 버튼 최대 글자 크기 설정
            label.raycastTarget = false; // 가방 버튼 라벨 입력 비활성화
            RectTransform labelRect = labelObject.GetComponent<RectTransform>(); // 가방 버튼 라벨 RectTransform 조회
            labelRect.anchorMin = Vector2.zero; // 가방 버튼 라벨 최소 앵커 설정
            labelRect.anchorMax = Vector2.one; // 가방 버튼 라벨 최대 앵커 설정
            labelRect.offsetMin = new Vector2(8f, 8f); // 가방 버튼 라벨 최소 여백 설정
            labelRect.offsetMax = new Vector2(-8f, -8f); // 가방 버튼 라벨 최대 여백 설정
            return button; // 생성 가방 버튼 반환
        }

        private static void NormalizeBottomNavigation(Transform navigationRoot) // 하단 내비게이션 버튼 균등 배치
        {
            List<Button> buttons = new List<Button>(); // 직접 하위 버튼 목록 생성

            for (int index = 0; index < navigationRoot.childCount; index++) // 하단 내비게이션 자식 순회
            {
                Transform child = navigationRoot.GetChild(index); // 현재 하위 객체 조회
                Button button = child.GetComponent<Button>(); // 현재 하위 버튼 조회

                if (button != null && child.gameObject.activeSelf) // 활성 버튼 여부 확인
                {
                    buttons.Add(button); // 하단 버튼 목록 추가
                }
            }

            if (buttons.Count == 0) // 하단 버튼 존재 확인
            {
                return; // 균등 배치 중단
            }

            const float margin = 0.02f; // 내비게이션 좌우 여백
            const float gap = 0.02f; // 내비게이션 버튼 사이 간격
            float usableWidth = 1f - (margin * 2f) - (gap * (buttons.Count - 1)); // 버튼 사용 가능 전체 너비 계산
            float buttonWidth = usableWidth / buttons.Count; // 버튼별 너비 계산

            for (int index = 0; index < buttons.Count; index++) // 하단 버튼 순회
            {
                RectTransform rect = buttons[index].GetComponent<RectTransform>(); // 현재 버튼 RectTransform 조회
                float minX = margin + (index * (buttonWidth + gap)); // 현재 버튼 최소 X 계산
                rect.anchorMin = new Vector2(minX, 0.12f); // 현재 버튼 최소 앵커 적용
                rect.anchorMax = new Vector2(minX + buttonWidth, 0.88f); // 현재 버튼 최대 앵커 적용
                rect.offsetMin = Vector2.zero; // 현재 버튼 최소 오프셋 초기화
                rect.offsetMax = Vector2.zero; // 현재 버튼 최대 오프셋 초기화
            }
        }

        private void BeginSceneTransition(string sceneName) // 공통 씬 전환 처리
        {
            if (isTransitioning) // 기존 전환 상태 확인
            {
                return; // 중복 전환 차단
            }

            if (GameManager.Instance == null || GameManager.Instance.Scenes == null) // 씬 로더 확인
            {
                SetText(statusText, "SceneLoader를 찾을 수 없습니다."); // 씬 로더 누락 표시
                return; // 씬 전환 중단
            }

            isTransitioning = true; // 전환 잠금 활성화
            SetPlayable(false); // 주요 버튼 잠금
            GameManager.Instance.Scenes.LoadScene(sceneName); // 대상 씬 이동
        }

        private void OnSaveLifecycle(SaveLifecycleEvent message) // 저장 생명주기 처리
        {
            if (isTransitioning) // 전환 상태 확인
            {
                return; // 전환 중 갱신 생략
            }

            Refresh(); // 저장 상태 변경 반영
        }

        private void SetPlayable(bool canNavigate) // 로비 버튼 상태 설정
        {
            if (saveButton != null) // 저장 버튼 확인
            {
                saveButton.interactable = canNavigate; // 저장 버튼 상태 적용
            }

            if (partyButton != null) // 파티 버튼 확인
            {
                partyButton.interactable = canNavigate; // 파티 버튼 상태 적용
            }

            if (dungeonButton != null) // 던전 버튼 확인
            {
                dungeonButton.interactable = canNavigate; // 던전 버튼 상태 적용
            }

            if (characterButton != null) // 캐릭터 버튼 확인
            {
                characterButton.interactable = canNavigate; // 캐릭터 버튼 상태 적용
            }

            if (bagButton != null) // 가방 버튼 확인
            {
                bagButton.interactable = canNavigate; // 가방 버튼 상태 적용
            }

            if (titleButton != null) // 타이틀 버튼 확인
            {
                titleButton.interactable = !isTransitioning; // 타이틀 버튼 상태 적용
            }
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target == null) // 텍스트 참조 확인
            {
                return; // 텍스트 설정 중단
            }

            target.text = value; // 텍스트 값 적용
        }
    }
}
