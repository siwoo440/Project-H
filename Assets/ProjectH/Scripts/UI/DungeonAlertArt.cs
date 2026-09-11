using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DungeonAlertArt // 새 던전 안내 아이콘 (Day66 추가 요청 — 글자 대신 그림 : 노란 원 + 느낌표 모양)
    {
        private const string ResourcePath = "UI/Icons/ALERT"; // 정식 아이콘 Resources 경로 (PNG를 넣으면 교체)
        private const int Size = 64; // 임시 아이콘 크기 (픽셀)
        private static Sprite cached; // 임시 아이콘 캐시

        public static Sprite Get() // 안내 아이콘 (정식 그림 우선)
        {
            Sprite art = RuntimeSpriteLoader.Load(ResourcePath); // 정식 아이콘
            if (art != null) return art; // 정식 아이콘 반환
            if (cached == null) cached = Create(); // 임시 아이콘 생성
            return cached; // 임시 아이콘 반환
        }

        private static Sprite Create() // 임시 아이콘 그리기 (가장자리 부드럽게)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            texture.filterMode = FilterMode.Bilinear; // 부드럽게
            Color[] pixels = new Color[Size * Size]; // 픽셀 버퍼
            Color border = new Color(0.36f, 0.20f, 0.02f, 1f); // 진한 갈색 테두리
            Color fill = new Color(1f, 0.82f, 0.22f, 1f); // 노란 원
            Color shine = new Color(1f, 0.94f, 0.62f, 1f); // 위쪽 밝은 빛
            Color mark = new Color(0.26f, 0.13f, 0f, 1f); // 느낌표 색
            float center = (Size - 1) * 0.5f; // 중심
            float radius = Size * 0.5f - 1f; // 바깥 반지름

            for (int y = 0; y < Size; y++) // 세로 순회
            {
                for (int x = 0; x < Size; x++) // 가로 순회
                {
                    float dx = x - center; // 중심 가로 거리
                    float dy = y - center; // 중심 세로 거리 (위가 +)
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy)); // 중심 거리
                    float outer = Mathf.Clamp01(radius - distance + 0.5f); // 바깥 경계 부드럽게
                    if (outer <= 0f) { pixels[(y * Size) + x] = Color.clear; continue; } // 원 밖은 투명
                    Color color = distance > radius - 5f ? border : Color.Lerp(fill, shine, Mathf.Clamp01(dy / radius)); // 테두리 · 위로 갈수록 밝은 노랑
                    float markAlpha = Mathf.Max(Bar(dx, dy), Dot(dx, dy)); // 느낌표 모양
                    color = Color.Lerp(color, mark, markAlpha); // 느낌표 칠하기
                    color.a = outer; // 가장자리 투명도
                    pixels[(y * Size) + x] = color; // 픽셀 기록
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }

        private static float Bar(float dx, float dy) // 느낌표 윗부분 : 아래로 갈수록 좁아지는 막대
        {
            if (dy < -4f || dy > 20f) return 0f; // 막대 세로 범위 (중심 위쪽)
            float halfWidth = Mathf.Lerp(3.2f, 5.6f, (dy + 4f) / 24f); // 위가 넓고 아래가 좁음
            return Mathf.Clamp01(halfWidth - Mathf.Abs(dx) + 0.5f); // 막대 가장자리 부드럽게
        }

        private static float Dot(float dx, float dy) // 느낌표 아랫점
        {
            float distance = Mathf.Sqrt((dx * dx) + ((dy + 14f) * (dy + 14f))); // 점 중심 거리
            return Mathf.Clamp01(5f - distance + 0.5f); // 원 가장자리 부드럽게
        }
    }
}
