using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "Project H/Data/Item")] // 아이템 에셋 메뉴
    public sealed class ItemData : ScriptableObject, IDataRecord // 아이템 데이터 정의
    {
        [SerializeField] private string id; // 아이템 고유 ID
        [SerializeField] private string displayName; // 아이템 표시 이름
        [SerializeField] private ItemType itemType; // 아이템 유형
        [SerializeField] private ItemGrade grade; // 아이템 등급
        [SerializeField, Min(1)] private int maxStack = 99; // 최대 보유 수량
        [SerializeField, TextArea] private string description; // 아이템 설명

        public string Id => id; // 고유 ID 반환
        public string DisplayName => displayName; // 표시 이름 반환
        public ItemType Type => itemType; // 아이템 유형 반환
        public ItemGrade Grade => grade; // 아이템 등급 반환
        public int MaxStack => maxStack; // 최대 수량 반환
        public string Description => description; // 설명 반환
    }
}
