using System; // 직렬화 기능
using UnityEngine; // Unity 직렬화 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [Serializable] // 상점 상품 직렬화 허용
    public sealed class ShopProductEntry // 단일 상점 상품 데이터
    {
        [SerializeField] private string productId; // 상품 고유 ID
        [SerializeField] private string itemId; // 판매 아이템 ID
        [SerializeField, Min(1)] private int buyPrice = 1; // 구매 가격
        [SerializeField, Min(0)] private int sellPrice; // 판매 가격
        [SerializeField] private bool rotating; // 오늘의 상품 후보 여부 (Day61 추가, false = 상시 상품)
        [SerializeField, Min(0)] private int dailyLimit; // 하루 재고 (Day61 추가, 0 = 무제한)
        public string ProductId => productId; // 상품 ID 반환
        public string ItemId => itemId; // 아이템 ID 반환
        public int BuyPrice => buyPrice; // 구매 가격 반환
        public int SellPrice => sellPrice; // 판매 가격 반환
        public bool CanSell => sellPrice > 0; // 판매 가능 여부 반환
        public bool Rotating => rotating; // 오늘의 상품 후보 여부 반환 (Day61 추가)
        public int DailyLimit => Math.Max(0, dailyLimit); // 하루 재고 반환 (Day61 추가)
        public bool HasDailyLimit => DailyLimit > 0; // 재고 제한 여부 (Day61 추가)

        public ShopProductEntry(string id, string targetItemId, int purchasePrice, int resalePrice) : this(id, targetItemId, purchasePrice, resalePrice, false, 0) // 기존 생성자 (상시·무제한)
        {
        }

        public ShopProductEntry(string id, string targetItemId, int purchasePrice, int resalePrice, bool isRotating, int limitPerDay) // 상점 상품 데이터 생성 (Day61 오늘의 상품·재고 추가)
        {
            productId = id ?? string.Empty; // 상품 ID 저장
            itemId = targetItemId ?? string.Empty; // 아이템 ID 저장
            buyPrice = Math.Max(1, purchasePrice); // 최소 구매 가격 보정
            sellPrice = Math.Max(0, resalePrice); // 최소 판매 가격 보정
            rotating = isRotating; // 오늘의 상품 여부 저장
            dailyLimit = Math.Max(0, limitPerDay); // 하루 재고 저장
        }
    }
}
