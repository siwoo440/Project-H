using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class RiftMarkArt // 검은 균열 표시 그림 (Day67 신규 — 글자 없이 소용돌이 모양)
    {
        private const string ResourcePath = "UI/Icons/RIFT"; // 정식 아이콘 Resources 경로 (PNG를 넣으면 교체)
        private const int Size = 64; // 임시 아이콘 크기 (픽셀)
        private static Sprite cached; // 임시 아이콘 캐시

        public static Sprite Get() // 균열 표시 (정식 그림 우선)
        {
            Sprite art = RuntimeSpriteLoader.Load(ResourcePath); // 정식 아이콘
            if (art != null) return art; // 정식 아이콘 반환
            if (cached == null) cached = Create(); // 임시 아이콘 생성
            return cached; // 임시 아이콘 반환
        }

        private static Sprite Create() // 임시 아이콘 그리기 (보랏빛 테두리 + 검은 소용돌이)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            texture.filterMode = FilterMode.Bilinear; // 부드럽게
            Color[] pixels = new Color[Size * Size]; // 픽셀 버퍼
            Color ring = new Color(0.66f, 0.32f, 0.92f, 1f); // 보랏빛 테두리
            Color inner = new Color(0.10f, 0.04f, 0.16f, 1f); // 균열 속 어둠
            Color glow = new Color(0.42f, 0.18f, 0.62f, 1f); // 소용돌이 빛
            float center = (Size - 1) * 0.5f; // 중심
            float radius = Size * 0.5f - 1f; // 바깥 반지름

            for (int y = 0; y < Size; y++) // 세로 순회
            {
                for (int x = 0; x < Size; x++) // 가로 순회
                {
                    float dx = x - center; // 중심 가로 거리
                    float dy = y - center; // 중심 세로 거리
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy)); // 중심 거리
                    float outer = Mathf.Clamp01(radius - distance + 0.5f); // 바깥 경계 부드럽게
                    if (outer <= 0f) { pixels[(y * Size) + x] = Color.clear; continue; } // 원 밖은 투명
                    float angle = Mathf.Atan2(dy, dx); // 각도
                    float spiral = Mathf.Sin((angle * 2f) + (distance * 0.55f)); // 소용돌이 무늬 (안쪽으로 감김)
                    Color color = distance > radius - 4f ? ring : Color.Lerp(inner, glow, Mathf.Clamp01((spiral * 0.5f) + 0.5f) * Mathf.Clamp01(1f - (distance / radius))); // 테두리 · 소용돌이
                    color.a = outer; // 가장자리 투명도
                    pixels[(y * Size) + x] = color; // 픽셀 기록
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }
    }
}
