using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 전환 이벤트 기능
using UnityEngine.UI; // Button 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class ButtonSoundScenePatch // 씬에 미리 놓인 버튼에 누르는 소리를 붙이는 연결 (Day88 신규 — 씬 파일은 건드리지 않는다. 코드로 만든 버튼은 RuntimeUiKit.CreateButton이 붙인다)
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // 중복 구독 방지
            SceneManager.sceneLoaded += OnSceneLoaded; // 모든 씬에서 버튼 소리 연결
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 처리
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 순회
            {
                Apply(root.transform); // 하위 버튼에 소리 연결
            }
        }

        public static int Apply(Transform root) // 하위의 버튼에 소리 붙이기 (꺼져 있는 것 포함, 새로 붙인 개수 반환)
        {
            if (root == null) return 0; // 대상 없음
            int attached = 0; // 새로 붙인 개수

            foreach (Button button in root.GetComponentsInChildren<Button>(true)) // 버튼 순회
            {
                if (button.GetComponent<UiButtonSound>() != null) continue; // 이미 소리가 붙은 버튼 (두 번 나지 않게)
                UiSound.Attach(button); // 이름으로 고른 소리 붙이기
                attached++; // 집계
            }

            return attached; // 개수 반환
        }
    }
}
