using System.Collections.Generic; // 읽기 전용 목록 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    public static class EquipmentSlotInfo // 장비 5칸 공용 정보 (Day60 신규 — 무기·투구·방어구·장갑·신발)
    {
        private static readonly EquipmentSlot[] Slots = // 화면 칸 순서 (좌상·우상·좌하·우하·하단 — 룬 슬롯과 같은 배치)
        {
            EquipmentSlot.Weapon, // 1번 좌상 무기
            EquipmentSlot.Helmet, // 2번 우상 투구
            EquipmentSlot.Armor, // 3번 좌하 방어구
            EquipmentSlot.Gloves, // 4번 우하 장갑
            EquipmentSlot.Boots // 5번 하단 신발
        };

        public static IReadOnlyList<EquipmentSlot> All => Slots; // 전체 슬롯 (화면 순서)

        public static string GetLabel(EquipmentSlot slot) // 슬롯 한글 이름
        {
            switch (slot) // 슬롯 분기
            {
                case EquipmentSlot.Weapon: return "무기"; // 무기
                case EquipmentSlot.Armor: return "방어구"; // 방어구
                case EquipmentSlot.Helmet: return "투구"; // 투구
                case EquipmentSlot.Gloves: return "장갑"; // 장갑
                default: return "신발"; // 신발
            }
        }

        public static string GetSymbol(EquipmentSlot slot) // 슬롯 아이콘 글자
        {
            switch (slot) // 슬롯 분기
            {
                case EquipmentSlot.Weapon: return "검"; // 무기
                case EquipmentSlot.Armor: return "갑"; // 방어구
                case EquipmentSlot.Helmet: return "투"; // 투구
                case EquipmentSlot.Gloves: return "장"; // 장갑
                default: return "신"; // 신발
            }
        }
    }
}
