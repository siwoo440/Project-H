using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Events; // 프로젝트 이벤트 기능
using ProjectH.SaveSystem; // 프로젝트 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 컨트롤러 방지
    public sealed class TitleScreenController : MonoBehaviour // 타이틀 화면 전용 컨트롤러
    {
        [SerializeField] private Text statusText; // 상태 표시 텍스트
        [SerializeField] private Button newGameButton; // 새 게임 버튼
        [SerializeField] private Button continueButton; // 이어하기 버튼
        [SerializeField] private Button quitButton; // 게임 종료 버튼

        private bool isTransitioning; // 화면 전환 잠금 상태

        public void Configure(Text status, Button newGame, Button continueTarget, Button quitTarget) // 에디터 참조 설정
        {
            statusText = status; // 상태 텍스트 연결
            newGameButton = newGame; // 새 게임 버튼 연결
            continueButton = continueTarget; // 이어하기 버튼 연결
            quitButton = quitTarget; // 종료 버튼 연결
        }

        private void OnEnable() // 이벤트 구독 처리
        {
            ProjectHEventBus.Subscribe<SaveLifecycleEvent>(OnSaveLifecycle); // 저장 이벤트 구독
        }

        private void OnDisable() // 이벤트 해제 처리
        {
            ProjectHEventBus.Unsubscribe<SaveLifecycleEvent>(OnSaveLifecycle); // 저장 이벤트 해제
        }

        private void Start() // 타이틀 초기 표시
        {
            Refresh(); // 화면 상태 갱신
        }

        public void Refresh() // 타이틀 화면 갱신
        {
            SaveManager save = GameManager.Instance == null ? null : GameManager.Instance.Save; // 저장 관리자 조회
            bool hasSaveData = save != null && save.HasSaveData; // 저장 존재 여부 조회
            TitleScreenViewState state = TitleScreenViewState.Build(hasSaveData); // 타이틀 표시 상태 생성
            SetText(statusText, state.StatusText); // 상태 문구 적용
            SetInteraction(!isTransitioning, state.CanContinue); // 버튼 상태 적용
        }

        public void NewGame() // 새 게임 시작
        {
            if (!TryBeginTransition(out SceneLoader scenes, out SaveManager save)) // 화면 전환 준비 확인
            {
                return; // 새 게임 중단
            }

            if (!save.CreateNewGame()) // 새 게임 저장 생성 확인
            {
                CancelTransition("새 게임 데이터를 생성하지 못했습니다."); // 실패 상태 복구
                return; // 로비 이동 중단
            }

            scenes.LoadScene(GameScenes.Lobby); // 로비 씬 이동
        }

        public void ContinueGame() // 이어하기 시작
        {
            if (!TryBeginTransition(out SceneLoader scenes, out SaveManager save)) // 화면 전환 준비 확인
            {
                return; // 이어하기 중단
            }

            if (!save.HasSaveData) // 저장 존재 확인
            {
                CancelTransition("이어갈 저장 데이터가 없습니다."); // 저장 없음 안내
                return; // 이어하기 중단
            }

            if (!save.LoadCurrent()) // 저장 불러오기 확인
            {
                CancelTransition("저장 데이터를 불러오지 못했습니다."); // 불러오기 실패 안내
                return; // 로비 이동 중단
            }

            scenes.LoadScene(GameScenes.Lobby); // 로비 씬 이동
        }

        public void QuitGame() // 게임 종료
        {
            if (isTransitioning) // 화면 전환 상태 확인
            {
                return; // 중복 입력 중단
            }

            Debug.Log("[Project H][UI] Quit requested from Title."); // 종료 요청 로그
            Application.Quit(); // 애플리케이션 종료
        }

        private bool TryBeginTransition(out SceneLoader scenes, out SaveManager save) // 전환 공통 준비
        {
            scenes = null; // 씬 로더 초기화
            save = null; // 저장 관리자 초기화

            if (isTransitioning) // 기존 전환 상태 확인
            {
                return false; // 중복 전환 차단
            }

            if (GameManager.Instance == null) // 게임 관리자 확인
            {
                SetText(statusText, "Bootstrap 씬부터 실행해 주세요."); // 부트스트랩 실행 안내
                return false; // 관리자 조회 실패
            }

            scenes = GameManager.Instance.Scenes; // 씬 로더 연결
            save = GameManager.Instance.Save; // 저장 관리자 연결

            if (scenes == null || save == null) // 필수 관리자 확인
            {
                SetText(statusText, "게임 초기화가 완료되지 않았습니다."); // 초기화 실패 안내
                return false; // 전환 준비 실패
            }

            isTransitioning = true; // 전환 잠금 활성화
            SetInteraction(false, false); // 전체 버튼 잠금
            return true; // 전환 준비 성공
        }

        private void CancelTransition(string message) // 전환 실패 복구
        {
            isTransitioning = false; // 전환 잠금 해제
            bool canContinue = GameManager.Instance != null && GameManager.Instance.Save != null && GameManager.Instance.Save.HasSaveData; // 이어하기 가능 여부 재계산
            SetInteraction(true, canContinue); // 버튼 상태 복구
            SetText(statusText, message); // 실패 문구 표시
        }

        private void OnSaveLifecycle(SaveLifecycleEvent message) // 저장 상태 변경 처리
        {
            if (isTransitioning) // 전환 중 이벤트 확인
            {
                return; // 전환 중 갱신 생략
            }

            Refresh(); // 저장 상태 기반 화면 갱신
        }

        private void SetInteraction(bool enabled, bool canContinue) // 타이틀 버튼 상태 설정
        {
            if (newGameButton != null) // 새 게임 버튼 확인
            {
                newGameButton.interactable = enabled; // 새 게임 버튼 상태 적용
            }

            if (continueButton != null) // 이어하기 버튼 확인
            {
                continueButton.interactable = enabled && canContinue; // 이어하기 버튼 상태 적용
            }

            if (quitButton != null) // 종료 버튼 확인
            {
                quitButton.interactable = enabled; // 종료 버튼 상태 적용
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
