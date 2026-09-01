using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 관리 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class BootstrapStartup : MonoBehaviour // 초기 화면 라우터
    {
        private void Start() // 초기 화면 진입
        {
            if (GameManager.Instance == null) // 게임 관리자 확인
            {
                Debug.LogError("[Project H] GameManager is missing during bootstrap."); // 관리자 누락 로그
                return; // 초기 화면 진입 중단
            }

            if (!GameManager.Instance.IsInitialized) // 공통 초기화 확인
            {
                Debug.LogError("[Project H] GameManager is not initialized."); // 초기화 실패 로그
                return; // 초기 화면 진입 중단
            }

            if (SceneManager.GetActiveScene().name != GameScenes.Bootstrap) // 현재 씬 확인
            {
                return; // 직접 실행 씬 유지
            }

            GameManager.Instance.Scenes.LoadScene(GameScenes.Title); // 타이틀 화면 진입
        }
    }
}
