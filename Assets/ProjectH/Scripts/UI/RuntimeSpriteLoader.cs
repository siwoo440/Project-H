using System.Collections.Generic; // 사전 자료형
using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class RuntimeSpriteLoader // Resources 그림 불러오기 (Day63 추가 — Sprite로 가져오지 않은 PNG(기본 Texture)도 바로 사용)
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(); // 텍스처 변환 캐시

        public static Sprite Load(string resourcePath) // 경로의 그림 (Sprite 우선, 없으면 Texture2D를 Sprite로 변환, 둘 다 없으면 null)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null; // 빈 경로
            Sprite sprite = Resources.Load<Sprite>(resourcePath); // Sprite로 가져온 그림
            if (sprite != null) return sprite; // 그대로 사용
            if (Cache.TryGetValue(resourcePath, out Sprite cached) && cached != null) return cached; // 변환 캐시
            Texture2D texture = Resources.Load<Texture2D>(resourcePath); // 기본 Texture로 가져온 그림
            if (texture == null) return null; // 그림 없음
            cached = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f); // 전체 영역 스프라이트
            cached.name = texture.name; // 이름
            Cache[resourcePath] = cached; // 캐시 저장
            return cached; // 반환
        }
    }
}
