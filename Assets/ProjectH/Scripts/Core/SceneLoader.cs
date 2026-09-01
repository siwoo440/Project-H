using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 관리 기능

namespace ProjectH.Core // 프로젝트 핵심 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class SceneLoader : MonoBehaviour // 공통 씬 로더
    {
        public bool IsLoading { get; private set; } // 씬 전환 상태

        public AsyncOperation LoadScene(string sceneName) // 이름 기반 씬 전환
        {
            if (IsLoading) // 기존 전환 확인
            {
                Debug.LogWarning("[Project H] Scene transition is already running."); // 중복 전환 로그
                return null; // 중복 전환 취소
            }

            if (string.IsNullOrWhiteSpace(sceneName)) // 씬 이름 유효성 확인
            {
                Debug.LogError("[Project H] Scene name is empty."); // 잘못된 씬 이름 로그
                return null; // 씬 전환 취소
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName)) // 빌드 씬 등록 확인
            {
                Debug.LogError($"[Project H] Scene is not registered in Build Settings. Scene={sceneName}"); // 미등록 씬 로그
                return null; // 씬 전환 취소
            }

            IsLoading = true; // 씬 전환 시작 기록
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); // 비동기 씬 전환 생성

            if (operation == null) // 전환 작업 확인
            {
                IsLoading = false; // 전환 상태 복구
                Debug.LogError($"[Project H] Failed to start scene load. Scene={sceneName}"); // 전환 시작 실패 로그
                return null; // 씬 전환 실패
            }

            operation.completed += OnSceneLoadCompleted; // 완료 이벤트 연결
            return operation; // 전환 작업 반환
        }

        public AsyncOperation ReloadActiveScene() // 현재 씬 다시 불러오기
        {
            string sceneName = SceneManager.GetActiveScene().name; // 현재 씬 이름 조회
            return LoadScene(sceneName); // 현재 씬 재전환
        }

        private void OnSceneLoadCompleted(AsyncOperation operation) // 씬 전환 완료 처리
        {
            IsLoading = false; // 전환 상태 해제
        }
    }
}
