using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 전환 이벤트 기능
using UnityEngine.UI; // Text 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class GameFontScenePatch // 씬에 미리 놓인 글자를 정식 폰트로 바꾸는 연결 (Day81 신규 — 정식 폰트가 없으면 아무 일도 하지 않는다)
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // 중복 구독 방지
            SceneManager.sceneLoaded += OnSceneLoaded; // 모든 씬에서 폰트 교체
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 처리
        {
            if (!RuntimeUiKit.HasGameFont) return; // 정식 폰트 없음 — 씬에 놓인 그대로 둔다
            Font font = RuntimeUiKit.DefaultFont; // 정식 폰트

            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 순회
            {
                Apply(root.transform, font); // 하위 글자 교체
            }
        }

        public static int Apply(Transform root, Font font) // 하위의 모든 글자에 폰트 적용 (꺼져 있는 것 포함, 바꾼 개수 반환)
        {
            if (root == null || font == null) return 0; // 대상 · 폰트 없음
            int changed = 0; // 바꾼 개수

            foreach (Text text in root.GetComponentsInChildren<Text>(true)) // 글자 순회
            {
                if (text.font == font) continue; // 이미 같은 폰트
                text.font = font; // 폰트 교체
                changed++; // 집계
            }

            return changed; // 개수 반환
        }
    }
}
