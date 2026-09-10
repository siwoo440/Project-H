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
        public string ProductId => productId; // 상품 ID 반환
        public string ItemId => itemId; // 아이템 ID 반환
        public int BuyPrice => buyPrice; // 구매 가격 반환
        public int SellPrice => sellPrice; // 판매 가격 반환
        public bool CanSell => sellPrice > 0; // 판매 가능 여부 반환

        public ShopProductEntry(string id, string targetItemId, int purchasePrice, int resalePrice) // 상점 상품 데이터 생성
        {
            productId = id ?? string.Empty; // 상품 ID 저장
            itemId = targetItemId ?? string.Empty; // 아이템 ID 저장
            buyPrice = Math.Max(1, purchasePrice); // 최소 구매 가격 보정
            sellPrice = Math.Max(0, resalePrice); // 최소 판매 가격 보정
        }
    }
}
