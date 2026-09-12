using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class CharacterPortraitArt // 캐릭터 정사각 초상화 (Day73 신규 — Resources/Portraits/{캐릭터ID}, 없으면 색 원 임시 그림)
    {
        public const string PortraitFolder = "Portraits/"; // 초상화 Resources 경로
        private const int PlaceholderSize = 128; // 임시 초상화 크기
        private static Sprite placeholder; // 임시 초상화 캐시 (색은 사용처에서 입힌다)

        public static Sprite Get(string characterId, out bool isPlaceholder) // 초상화 조회 (정식 → 임시)
        {
            Sprite art = string.IsNullOrEmpty(characterId) ? null : RuntimeSpriteLoader.Load(PortraitFolder + characterId); // 정식 초상화
            isPlaceholder = art == null; // 임시 여부
            if (art != null) return art; // 정식 반환
            if (placeholder == null) placeholder = CreatePlaceholder(); // 임시 생성
            return placeholder; // 임시 반환
        }

        public static Color GetPlaceholderTint(string characterId) => DialogueArtFactory.GetCharacterTint(characterId); // 임시 초상화에 입힐 색

        private static Sprite CreatePlaceholder() // 임시 초상화 : 가운데가 밝은 둥근 원 (색은 사용처에서 곱한다)
        {
            Texture2D texture = new Texture2D(PlaceholderSize, PlaceholderSize, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            texture.filterMode = FilterMode.Bilinear; // 부드럽게
            Color[] pixels = new Color[PlaceholderSize * PlaceholderSize]; // 픽셀 버퍼
            float center = (PlaceholderSize - 1) * 0.5f; // 중심
            float radius = PlaceholderSize * 0.47f; // 반지름

            for (int y = 0; y < PlaceholderSize; y++) // 세로 순회
            {
                for (int x = 0; x < PlaceholderSize; x++) // 가로 순회
                {
                    float distance = Mathf.Sqrt(((x - center) * (x - center)) + ((y - center) * (y - center))); // 중심 거리
                    float edge = Mathf.Clamp01(radius - distance + 1f); // 가장자리 부드럽게
                    float shade = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01(distance / radius)); // 가운데가 밝음
                    pixels[(y * PlaceholderSize) + x] = new Color(shade, shade, shade, edge); // 픽셀 기록
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, PlaceholderSize, PlaceholderSize), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }
    }
}
