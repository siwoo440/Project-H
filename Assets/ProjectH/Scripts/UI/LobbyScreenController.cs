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

        public void GoTitle() // 타이틀 화면 이동
        {
            BeginSceneTransition(GameScenes.Title); // 타이틀 씬 전환
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
