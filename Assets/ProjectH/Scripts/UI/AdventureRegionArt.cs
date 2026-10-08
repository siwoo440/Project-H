using UnityEngine; // 스프라이트·벡터·색 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class AdventureRegionArt // 모험 지도 지역 아이콘 (Day78 신규 — Resources/Map/Regions/{지역ID}.png, 없으면 예전의 색 원과 글자)
    {
        public const string Folder = "Map/Regions/"; // 지역 아이콘 Resources 경로
        public const float IconSize = 104f; // 지도 위 아이콘 한 변
        public const float IconLift = 34f; // 받침에서 아이콘 중심까지 올리는 높이 (아이콘 밑동이 받침 위에 놓인다)
        public const float AlertLift = 98f; // 아이콘이 있을 때 "!" 안내 높이 (아이콘 머리 위)
        public const float LabelDrop = -44f; // 아이콘이 있을 때 이름 위치 (받침 아래)
        public static readonly Vector2 PedestalSize = new Vector2(92f, 36f); // 아이콘 아래 납작한 받침 크기 (지역 상태 색이 여기에 남는다)
        public static readonly Color LockedTint = new Color(0.36f, 0.37f, 0.42f, 1f); // 잠긴 지역 아이콘 색

        public static Sprite Get(string regionId) // 지역 아이콘 조회 (없으면 null)
        {
            return string.IsNullOrEmpty(regionId) ? null : RuntimeSpriteLoader.Load(Folder + regionId); // 정식 아이콘
        }
    }
}
