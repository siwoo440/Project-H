using UnityEngine; // 텍스처·스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DiaryBadgeArt // 일기장 칸 표시 아이콘 (Day63 추가 — 사진 = 정지 CG, 동영상 = 컷신. Resources/Diary/Icons/PHOTO · VIDEO로 교체 가능)
    {
        private const int Size = 64; // 아이콘 크기
        private static Sprite photo; // 사진 아이콘 캐시
        private static Sprite video; // 동영상 아이콘 캐시

        public static Sprite Photo => RuntimeSpriteLoader.Load("Diary/Icons/PHOTO") ?? (photo != null ? photo : photo = Create(false)); // 사진 아이콘 (정식 아이콘 우선)

        public static Sprite Video => RuntimeSpriteLoader.Load("Diary/Icons/VIDEO") ?? (video != null ? video : video = Create(true)); // 동영상 아이콘 (정식 아이콘 우선)

        private static Sprite Create(bool isVideo) // 임시 아이콘 그리기 (어두운 둥근 바탕 + 흰 그림)
        {
            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false); // 텍스처
            texture.wrapMode = TextureWrapMode.Clamp; // 가장자리 반복 없음
            Color[] pixels = new Color[Size * Size]; // 픽셀 버퍼
            Color back = new Color(0.08f, 0.06f, 0.06f, 0.78f); // 둥근 바탕
            Color ink = Color.white; // 흰 그림

            for (int y = 0; y < Size; y++) // 세로 순회
            {
                for (int x = 0; x < Size; x++) // 가로 순회
                {
                    Color color = Rounded(x, y, 2, 2, 61, 61, 14) ? back : Color.clear; // 둥근 바탕
                    bool frame = Rounded(x, y, 12, 16, 51, 47, 5) && !Rounded(x, y, 15, 19, 48, 44, 3); // 흰 테두리 사각형 (사진·필름 공통)
                    if (frame) color = ink; // 테두리

                    if (!isVideo) // 사진 : 산 + 해
                    {
                        bool mountain = y >= 20 && y <= 34 && Mathf.Abs(x - 27) <= (34 - y) * 1.0f + 0.5f; // 큰 산 (위가 좁은 삼각형)
                        bool small = y >= 20 && y <= 29 && Mathf.Abs(x - 39) <= (29 - y) * 1.1f + 0.5f; // 작은 산
                        bool sun = ((x - 40) * (x - 40)) + ((y - 37) * (y - 37)) <= 12; // 해
                        if ((mountain || small || sun) && x > 15 && x < 48 && y > 19 && y < 44) color = ink; // 칸 안에만
                    }
                    else // 동영상 : 필름 구멍 + 재생 삼각형
                    {
                        bool holes = (y == 11 || y == 12 || y == 51 || y == 52) && x >= 14 && x <= 50 && ((x - 14) % 8) < 4; // 위아래 필름 구멍
                        bool play = x >= 26 && x <= 41 && Mathf.Abs(y - 31.5f) <= (41 - x) * 0.62f; // 오른쪽을 향한 재생 삼각형
                        if (holes || play) color = ink; // 흰 그림
                    }

                    pixels[(y * Size) + x] = color; // 픽셀 저장
                }
            }

            texture.SetPixels(pixels); // 픽셀 적용
            texture.Apply(); // 업로드
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f); // 스프라이트 반환
        }

        private static bool Rounded(int x, int y, int x0, int y0, int x1, int y1, int radius) // 둥근 사각형 안인지
        {
            if (x < x0 || x > x1 || y < y0 || y > y1) return false; // 바깥
            int cx = Mathf.Clamp(x, x0 + radius, x1 - radius); // 가장 가까운 안쪽 x
            int cy = Mathf.Clamp(y, y0 + radius, y1 - radius); // 가장 가까운 안쪽 y
            return ((x - cx) * (x - cx)) + ((y - cy) * (y - cy)) <= radius * radius; // 모서리 원 안
        }
    }
}
