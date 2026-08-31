using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 관리 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class SceneLoader : MonoBehaviour // 공통 씬 로더
    {
        public AsyncOperation LoadScene(string sceneName) // 이름 기반 씬 전환
        {
            if (string.IsNullOrWhiteSpace(sceneName)) // 씬 이름 유효성 확인
            {
                Debug.LogError("[Project H] Scene name is empty."); // 잘못된 씬 이름 로그
                return null; // 씬 전환 취소
            }

            return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // 비동기 씬 전환
        }

        public AsyncOperation ReloadActiveScene() // 현재 씬 다시 불러오기
        {
            string sceneName = SceneManager.GetActiveScene().name; // 현재 씬 이름 조회
            return LoadScene(sceneName); // 현재 씬 재전환
        }
    }
}
