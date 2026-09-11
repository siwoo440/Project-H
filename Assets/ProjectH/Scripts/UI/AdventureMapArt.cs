using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class AdventureMapArt // 모험 세계 지도 그림 (Day63 신규 — 정식 지도 전까지 바다 · 대륙 · 지역색 임시 그림)
    {
        private const string ResourcePath = "Map/WORLD"; // 정식 지도 Resources 경로
        private const int Width = 512; // 임시 지도 가로
        private const int Height = 320; // 임시 지도 세로
        private static Sprite cached; // 임시 지도 캐시

        public static Sprite Get() // 세계 지도 (정식 그림 우선)
        {
            Sprite art = RuntimeSpriteLoader.Load(ResourcePath); // 정식 지도
            if (art != null) return art; // 정식 지도 반환
            if (cached == null) cached = Create(); // 임시 지도 생성
            return cached; // 임시 지도 반환
        }

        private static Sprite Create() // 임시 지도 생성 (대륙 = 원 여러 개를 합친 모양, 방위별 색)
        {
            Vector3[] blobs = // 대륙 모양 (x, y, 반지름 — 0~1 좌표)
            {
                new Vector3(0.50f, 0.50f, 0.30f), new Vector3(0.36f, 0.64f, 0.20f), new Vector3(0.66f, 0.66f, 0.20f), // 중앙·북서·북동
                new Vector3(0.80f, 0.80f, 0.14f), new Vector3(0.58f, 0.22f, 0.20f), new Vector3(0.30f, 0.38f, 0.16f), // 균열지대·사막·서부
                new Vector3(0.70f, 0.40f, 0.16f) // 동부 숲
            };
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            Color[] pixels = new Color[Width * Height]; // 픽셀 버퍼

            for (int y = 0; y < Height; y++) // 세로 순회
            {
                float v = y / (float)(Height - 1); // 0 남쪽 ~ 1 북쪽

                for (int x = 0; x < Width; x++) // 가로 순회
                {
                    float u = x / (float)(Width - 1); // 0 서쪽 ~ 1 동쪽
                    float land = -1f; // 대륙 안쪽 정도 (0 이상이면 땅)

                    foreach (Vector3 blob in blobs) // 원 순회
                    {
                        float dx = (u - blob.x) * 1.6f; // 가로 비율 보정
                        float dy = v - blob.y; // 세로 거리
                        land = Mathf.Max(land, 1f - (Mathf.Sqrt((dx * dx) + (dy * dy)) / blob.z)); // 가장 가까운 원 기준
                    }

                    land += 0.06f * Mathf.Sin((u * 37f) + (v * 11f)) * Mathf.Sin((v * 29f) - (u * 7f)); // 해안선 굴곡
                    pixels[(y * Width) + x] = land < 0f ? Ocean(u, v, land) : Land(u, v, land); // 바다 또는 땅
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, Width, Height), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }

        private static Color Ocean(float u, float v, float land) // 바다 색 (해안 가까울수록 밝게)
        {
            Color deep = new Color(0.10f, 0.22f, 0.40f); // 깊은 바다
            Color shallow = new Color(0.28f, 0.52f, 0.70f); // 얕은 바다
            return Color.Lerp(deep, shallow, Mathf.Clamp01(1f + (land * 4f)) * (0.8f + (0.2f * Mathf.Sin(u * 40f + v * 30f)))); // 해안 근처 밝게 + 물결
        }

        private static Color Land(float u, float v, float land) // 땅 색 (방위별 지역 색 섞기)
        {
            Color plain = new Color(0.56f, 0.70f, 0.42f); // 중앙 초원 (레티시아)
            Color forest = new Color(0.22f, 0.46f, 0.26f); // 동부 숲 (실바란)
            Color north = new Color(0.62f, 0.64f, 0.66f); // 북부 설원 (카르니안)
            Color sand = new Color(0.86f, 0.74f, 0.48f); // 남부 사막 (아스타르)
            Color swamp = new Color(0.34f, 0.42f, 0.30f); // 북서 늪지대
            Color rift = new Color(0.28f, 0.10f, 0.14f); // 북동 검은 균열
            Color color = plain; // 기본 초원
            color = Color.Lerp(color, forest, Weight(u, v, 0.64f, 0.46f, 0.18f)); // 숲
            color = Color.Lerp(color, north, Weight(u, v, 0.52f, 0.80f, 0.20f)); // 북부
            color = Color.Lerp(color, sand, Weight(u, v, 0.58f, 0.18f, 0.20f)); // 사막
            color = Color.Lerp(color, swamp, Weight(u, v, 0.30f, 0.66f, 0.14f)); // 늪지대
            color = Color.Lerp(color, rift, Weight(u, v, 0.82f, 0.82f, 0.13f)); // 균열
            color = Color.Lerp(color * 0.85f, color, Mathf.Clamp01(land * 6f)); // 해안 쪽 살짝 어둡게
            color.a = 1f; // 불투명
            return color; // 땅 색 반환
        }

        private static float Weight(float u, float v, float cx, float cy, float radius) // 지역 중심에서 멀어질수록 약해지는 가중치
        {
            float dx = (u - cx) * 1.6f; // 가로 비율 보정
            float dy = v - cy; // 세로 거리
            return Mathf.Clamp01(1f - (Mathf.Sqrt((dx * dx) + (dy * dy)) / radius)); // 0~1
        }
    }
}
