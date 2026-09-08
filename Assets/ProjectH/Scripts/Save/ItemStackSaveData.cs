using System; // 직렬화 기능
using UnityEngine; // Unity 수치 보정 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class ItemStackSaveData // 일반 아이템 스택 저장 데이터
    {
        [SerializeField] private string itemId; // 아이템 원본 ID
        [SerializeField] private int quantity; // 아이템 보유 수량
        public string ItemId => itemId; // 아이템 ID 반환
        public int Quantity => quantity; // 아이템 수량 반환

        public ItemStackSaveData(string id, int value) // 아이템 스택 생성
        {
            itemId = id ?? string.Empty; // 아이템 ID 저장
            quantity = Mathf.Max(0, value); // 음수 수량 방지
        }

        public void EnsureDefaults() // 아이템 스택 기본값 복원
        {
            if (itemId == null) // 아이템 ID null 확인
            {
                itemId = string.Empty; // 아이템 ID 기본값 복원
            }

            quantity = Mathf.Max(0, quantity); // 음수 수량 보정
        }

        internal void SetQuantity(int value) // 아이템 수량 변경
        {
            quantity = Mathf.Max(0, value); // 음수 수량 방지
        }
    }
}
