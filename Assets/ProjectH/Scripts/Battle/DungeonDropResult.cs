namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class DungeonDropResult // 실제 던전 드롭 결과
    {
        public string ItemId { get; } // 드롭 아이템 ID 반환
        public int Quantity { get; } // 드롭 수량 반환

        public DungeonDropResult(string itemId, int quantity) // 드롭 결과 생성
        {
            ItemId = itemId ?? string.Empty; // 아이템 ID 저장
            Quantity = quantity < 0 ? 0 : quantity; // 음수 수량 방지
        }
    }
}
