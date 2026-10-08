using ProjectH.Data; // 아이템 데이터 기능
using UnityEngine; // 스프라이트 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class ItemIconArt // 아이템 · 장비 아이콘 그림 (Day80 신규 — Resources/Icons/Items/{아이템ID}.png → 데이터에 연결된 그림 → 없음)
    {
        public const string Folder = "Icons/Items/"; // 아이콘 Resources 경로

        public static Sprite Get(string itemId) // 아이콘 파일 조회 (없으면 null)
        {
            return string.IsNullOrEmpty(itemId) ? null : RuntimeSpriteLoader.Load(Folder + itemId); // 정식 아이콘
        }

        public static Sprite Get(ItemData item) // 아이템의 아이콘 : 아이콘 파일 → 데이터에 연결된 그림 (없으면 null — 색 원과 글자로 그린다)
        {
            if (item == null) return null; // 아이템 없음
            Sprite art = Get(item.Id); // 아이콘 파일
            return art != null ? art : item.Icon; // 없으면 데이터의 그림
        }
    }
}
