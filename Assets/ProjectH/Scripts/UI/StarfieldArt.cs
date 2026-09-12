using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class StarfieldArt // 밤하늘(우주) 배경 그림 (Day71 추가 — 이름 입력 화면 배경, 정식 그림을 넣으면 교체)
    {
        private const string BackgroundResourcePath = "UI/Backgrounds/STARFIELD"; // 정식 배경 Resources 경로
        private const int Width = 1024; // 배경 가로 (픽셀)
        private const int Height = 576; // 배경 세로 (16:9)
        private const int StarCount = 900; // 작은 별 수
        private const int BrightStarCount = 26; // 빛줄기가 있는 큰 별 수
        private const int GlowSize = 48; // 반짝이는 별 그림 크기
        private const int Seed = 71; // 항상 같은 하늘이 나오도록 고정
        private static Sprite cachedBackground; // 배경 캐시
        private static Sprite cachedGlow; // 반짝임 캐시

        public static Sprite GetBackground() // 우주 배경 (정식 그림 우선)
        {
            Sprite art = RuntimeSpriteLoader.Load(BackgroundResourcePath); // 정식 배경
            if (art != null) return art; // 정식 배경 반환
            if (cachedBackground == null) cachedBackground = CreateBackground(); // 임시 배경 생성
            return cachedBackground; // 임시 배경 반환
        }

        public static Sprite GetStarGlow() // 반짝이는 별 하나 (가운데가 밝고 가장자리로 사라짐)
        {
            if (cachedGlow == null) cachedGlow = CreateGlow(); // 생성
            return cachedGlow; // 반환
        }

        private static Sprite CreateBackground() // 밤하늘 그리기 (바탕 그라데이션 + 성운 + 별)
        {
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            texture.filterMode = FilterMode.Bilinear; // 부드럽게
            Color[] pixels = new Color[Width * Height]; // 픽셀 버퍼
            System.Random random = new System.Random(Seed); // 고정 난수
            PaintSky(pixels); // 바탕 하늘
            PaintNebula(pixels, 0.28f, 0.66f, 0.42f, new Color(0.24f, 0.16f, 0.46f, 1f)); // 보라 성운 (왼쪽 위)
            PaintNebula(pixels, 0.74f, 0.30f, 0.34f, new Color(0.10f, 0.22f, 0.42f, 1f)); // 푸른 성운 (오른쪽 아래)
            PaintMilkyWay(pixels, random); // 은하수 띠
            PaintStars(pixels, random); // 작은 별
            PaintBrightStars(pixels, random); // 큰 별 + 십자 빛줄기
            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, Width, Height), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }

        private static void PaintSky(Color[] pixels) // 위는 검고 아래로 갈수록 살짝 푸른 바탕
        {
            Color top = new Color(0.015f, 0.016f, 0.035f, 1f); // 위쪽 (거의 검정)
            Color bottom = new Color(0.045f, 0.040f, 0.085f, 1f); // 아래쪽 (짙은 남보라)

            for (int y = 0; y < Height; y++) // 세로 순회
            {
                Color row = Color.Lerp(bottom, top, (float)y / (Height - 1)); // 세로 그라데이션 (y가 클수록 위)

                for (int x = 0; x < Width; x++) // 가로 순회
                {
                    pixels[(y * Width) + x] = row; // 픽셀 기록
                }
            }
        }

        private static void PaintNebula(Color[] pixels, float centerX, float centerY, float radiusRatio, Color color) // 둥글게 번지는 성운 한 덩어리
        {
            float cx = centerX * Width; // 중심 가로
            float cy = centerY * Height; // 중심 세로
            float radius = radiusRatio * Width; // 반지름

            for (int y = 0; y < Height; y++) // 세로 순회
            {
                for (int x = 0; x < Width; x++) // 가로 순회
                {
                    float dx = (x - cx) / radius; // 가로 거리 비율
                    float dy = (y - cy) / (radius * 0.72f); // 세로 거리 비율 (살짝 납작하게)
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy)); // 중심 거리
                    if (distance >= 1f) continue; // 성운 밖
                    float strength = Mathf.Pow(1f - distance, 2.2f) * 0.85f; // 가운데가 진하고 밖으로 사라짐
                    int index = (y * Width) + x; // 픽셀 위치
                    pixels[index] = Color.Lerp(pixels[index], color, strength); // 섞기
                }
            }
        }

        private static void PaintMilkyWay(Color[] pixels, System.Random random) // 비스듬히 가로지르는 옅은 은하수 띠
        {
            for (int x = 0; x < Width; x++) // 가로 순회
            {
                float center = (Height * 0.62f) - (x * 0.22f); // 왼쪽 위에서 오른쪽 아래로 기울어진 중심선
                float thickness = Height * 0.16f; // 띠 두께

                for (int y = 0; y < Height; y++) // 세로 순회
                {
                    float offset = Mathf.Abs(y - center) / thickness; // 중심선에서 벗어난 정도
                    if (offset >= 1f) continue; // 띠 밖
                    float strength = Mathf.Pow(1f - offset, 2f) * 0.22f * (0.6f + (float)random.NextDouble() * 0.4f); // 얼룩덜룩한 밝기
                    int index = (y * Width) + x; // 픽셀 위치
                    pixels[index] = Color.Lerp(pixels[index], new Color(0.62f, 0.60f, 0.78f, 1f), strength); // 옅게 밝히기
                }
            }
        }

        private static void PaintStars(Color[] pixels, System.Random random) // 작은 별 뿌리기
        {
            for (int count = 0; count < StarCount; count++) // 별 수만큼
            {
                int x = random.Next(Width); // 가로 위치
                int y = random.Next(Height); // 세로 위치
                float brightness = 0.35f + (float)random.NextDouble() * 0.65f; // 밝기
                Color tint = PickStarColor(random); // 별 색 (흰색·푸른색·노란색)
                Blend(pixels, x, y, tint, brightness); // 중심 점
                if (brightness <= 0.82f) continue; // 밝은 별만 번짐 추가
                Blend(pixels, x + 1, y, tint, brightness * 0.32f); // 오른쪽 번짐
                Blend(pixels, x - 1, y, tint, brightness * 0.32f); // 왼쪽 번짐
                Blend(pixels, x, y + 1, tint, brightness * 0.32f); // 위 번짐
                Blend(pixels, x, y - 1, tint, brightness * 0.32f); // 아래 번짐
            }
        }

        private static void PaintBrightStars(Color[] pixels, System.Random random) // 큰 별 + 십자 빛줄기
        {
            for (int count = 0; count < BrightStarCount; count++) // 큰 별 수만큼
            {
                int cx = random.Next(Width); // 가로 위치
                int cy = random.Next(Height); // 세로 위치
                int length = 5 + random.Next(9); // 빛줄기 길이
                Color tint = PickStarColor(random); // 별 색

                for (int offset = -length; offset <= length; offset++) // 십자 그리기
                {
                    float fade = 1f - (Mathf.Abs(offset) / (float)(length + 1)); // 끝으로 갈수록 흐려짐
                    Blend(pixels, cx + offset, cy, tint, fade * 0.55f); // 가로 줄기
                    Blend(pixels, cx, cy + offset, tint, fade * 0.55f); // 세로 줄기
                }

                Blend(pixels, cx, cy, Color.white, 1f); // 가운데 핵
                Blend(pixels, cx + 1, cy, tint, 0.7f); // 핵 번짐
                Blend(pixels, cx - 1, cy, tint, 0.7f); // 핵 번짐
                Blend(pixels, cx, cy + 1, tint, 0.7f); // 핵 번짐
                Blend(pixels, cx, cy - 1, tint, 0.7f); // 핵 번짐
            }
        }

        private static Color PickStarColor(System.Random random) // 별 색 고르기 (대부분 흰색, 가끔 푸르거나 노랗게)
        {
            int roll = random.Next(10); // 주사위
            if (roll < 6) return new Color(1f, 1f, 1f, 1f); // 흰색
            if (roll < 8) return new Color(0.72f, 0.84f, 1f, 1f); // 푸른 별
            return new Color(1f, 0.90f, 0.72f, 1f); // 노란 별
        }

        private static void Blend(Color[] pixels, int x, int y, Color color, float strength) // 한 픽셀을 밝게 섞기 (화면 밖은 무시)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || strength <= 0f) return; // 범위 확인
            int index = (y * Width) + x; // 픽셀 위치
            pixels[index] = Color.Lerp(pixels[index], color, Mathf.Clamp01(strength)); // 섞기
        }

        private static Sprite CreateGlow() // 반짝이는 별 한 점 (가운데가 밝고 십자로 살짝 번짐)
        {
            Texture2D texture = new Texture2D(GlowSize, GlowSize, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            texture.filterMode = FilterMode.Bilinear; // 부드럽게
            Color[] pixels = new Color[GlowSize * GlowSize]; // 픽셀 버퍼
            float center = (GlowSize - 1) * 0.5f; // 중심
            float radius = GlowSize * 0.5f; // 반지름

            for (int y = 0; y < GlowSize; y++) // 세로 순회
            {
                for (int x = 0; x < GlowSize; x++) // 가로 순회
                {
                    float dx = (x - center) / radius; // 가로 거리 비율
                    float dy = (y - center) / radius; // 세로 거리 비율
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy)); // 중심 거리
                    float core = Mathf.Clamp01(1f - (distance * 3.4f)); // 가운데 핵
                    float halo = Mathf.Pow(Mathf.Clamp01(1f - distance), 3f) * 0.5f; // 둥근 번짐
                    float cross = Mathf.Max(Line(dx, dy), Line(dy, dx)); // 십자 빛줄기
                    float alpha = Mathf.Clamp01(core + halo + cross); // 최종 진하기
                    pixels[(y * GlowSize) + x] = new Color(1f, 1f, 1f, alpha); // 흰 별 (색은 사용처에서 지정)
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, GlowSize, GlowSize), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }

        private static float Line(float along, float across) // 십자 빛줄기 한 방향 (가운데에서 멀어질수록 흐려짐)
        {
            float thickness = Mathf.Clamp01(1f - (Mathf.Abs(across) * 26f)); // 줄기 두께
            float fade = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Abs(along)), 2f); // 길이 방향 감쇠
            return thickness * fade * 0.7f; // 진하기 반환
        }
    }
}
