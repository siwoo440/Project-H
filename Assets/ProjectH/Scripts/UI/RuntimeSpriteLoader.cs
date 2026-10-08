using System.Collections.Generic; // 사전·집합 자료형
using UnityEngine; // 텍스처·스프라이트 기능
using UnityEngine.SceneManagement; // 씬 전환 이벤트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class RuntimeSpriteLoader // Resources 그림 불러오기 (Day63 추가 — Sprite로 가져오지 않은 PNG(기본 Texture)도 바로 사용)
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(); // 불러온 그림 캐시
        private static readonly HashSet<string> Missing = new HashSet<string>(); // 없다고 확인한 경로 (Day80 추가 — 같은 경로를 매번 다시 찾지 않는다)

        public static int CachedCount => Cache.Count; // 캐시에 든 그림 수 (점검·테스트용)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 시작 시 초기화 (도메인 리로드를 끈 Play Mode 대응)
        private static void Initialize() // 캐시 비우기와 씬 전환 구독
        {
            ClearCache(); // 이전 플레이의 캐시 제거
            SceneManager.sceneUnloaded -= HandleSceneUnloaded; // 중복 구독 방지
            SceneManager.sceneUnloaded += HandleSceneUnloaded; // 씬이 내려갈 때 캐시 비우기
        }

        private static void HandleSceneUnloaded(Scene scene) // 씬 전환 처리 (Day80 추가)
        {
            ClearCache(); // 캐시가 그림을 붙잡고 있으면 메모리에서 내려가지 않는다 — 놓아 주면 화면에서 쓰지 않는 그림을 엔진이 정리한다
        }

        public static void ClearCache() // 캐시 비우기 (그림을 지우지는 않는다 — 화면에서 쓰는 그림은 그대로 남는다)
        {
            Cache.Clear(); // 불러온 그림 목록 비움
            Missing.Clear(); // 없는 경로 목록 비움
        }

        public static Sprite Load(string resourcePath) // 경로의 그림 (캐시 → Sprite → Texture2D를 Sprite로 변환, 없으면 null)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null; // 빈 경로
            if (Cache.TryGetValue(resourcePath, out Sprite cached) && cached != null) return cached; // 캐시 먼저 (Day80 — 예전에는 매번 Sprite 조회를 한 번 실패한 뒤에야 캐시를 봤다)
            bool rememberMissing = Application.isPlaying; // 없는 경로 기억은 플레이 중에만 (에디터에서는 그림을 넣자마자 보여야 한다)
            if (rememberMissing && Missing.Contains(resourcePath)) return null; // 없다고 확인한 경로
            Sprite sprite = Resources.Load<Sprite>(resourcePath); // Sprite로 가져온 그림

            if (sprite == null) // 기본 Texture로 가져온 그림
            {
                Texture2D texture = Resources.Load<Texture2D>(resourcePath); // 텍스처 조회

                if (texture != null) // 텍스처 있음
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f); // 전체 영역 스프라이트
                    sprite.name = texture.name; // 이름
                }
            }

            if (sprite == null) // 그림 없음
            {
                if (rememberMissing) Missing.Add(resourcePath); // 다음부터는 바로 null
                return null; // 없음 반환
            }

            Cache[resourcePath] = sprite; // 캐시 저장
            return sprite; // 반환
        }
    }
}
