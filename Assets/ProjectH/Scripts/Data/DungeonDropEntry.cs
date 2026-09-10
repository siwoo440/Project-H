using System; // 직렬화 기능
using UnityEngine; // Unity 범위 속성 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [Serializable] // 던전 드롭 항목 직렬화 허용
    public sealed class DungeonDropEntry // 단일 던전 드롭 항목
    {
        [SerializeField] private string itemId; // 드롭 아이템 ID
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f; // 드롭 확률
        [SerializeField, Min(1)] private int minQuantity = 1; // 최소 드롭 수량
        [SerializeField, Min(1)] private int maxQuantity = 1; // 최대 드롭 수량
        public string ItemId => itemId; // 아이템 ID 반환
        public float DropChance => dropChance; // 드롭 확률 반환
        public int MinQuantity => minQuantity; // 최소 수량 반환
        public int MaxQuantity => maxQuantity; // 최대 수량 반환

        public DungeonDropEntry(string id, float chance, int minimumQuantity, int maximumQuantity) // 드롭 항목 생성
        {
            itemId = id ?? string.Empty; // 아이템 ID 저장
            dropChance = Mathf.Clamp01(chance); // 드롭 확률 범위 보정
            minQuantity = Mathf.Max(1, minimumQuantity); // 최소 수량 보정
            maxQuantity = Mathf.Max(minQuantity, maximumQuantity); // 최대 수량 보정
        }
    }
}
