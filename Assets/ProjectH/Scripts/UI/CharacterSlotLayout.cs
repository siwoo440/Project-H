using UnityEngine; // Unity 수학 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class CharacterSlotLayout // 캐릭터 일러스트 둘레 5칸 배치 (Day60 신규 — 룬 탭·장비 탭 공용, 목업 : 좌상·우상·좌하·우하·하단 중앙)
    {
        public const int SlotCount = 5; // 칸 수

        private static readonly Vector2[] Mins = // 칸 최소 앵커 (좌측 영역 기준)
        {
            new Vector2(0.00f, 0.66f), // 1번 좌상
            new Vector2(0.84f, 0.66f), // 2번 우상
            new Vector2(0.00f, 0.34f), // 3번 좌하
            new Vector2(0.84f, 0.34f), // 4번 우하
            new Vector2(0.42f, 0.07f) // 5번 하단 중앙
        };

        private static readonly Vector2 Size = new Vector2(0.16f, 0.12f); // 칸 크기

        public static Vector2 GetMin(int index) => Mins[index]; // 칸 최소 앵커
        public static Vector2 GetMax(int index) => Mins[index] + Size; // 칸 최대 앵커
        public static Vector2 GetCaptionMin(int index) => new Vector2(Mins[index].x - 0.02f, Mins[index].y - 0.045f); // 칸 아래 문구 최소 앵커
        public static Vector2 GetCaptionMax(int index) => new Vector2(Mins[index].x + Size.x + 0.02f, Mins[index].y); // 칸 아래 문구 최대 앵커
    }
}
