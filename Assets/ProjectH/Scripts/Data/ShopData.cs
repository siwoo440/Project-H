using System.Collections.Generic; // 상품 목록 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Data // 프로젝트 데이터 영역
{
    [CreateAssetMenu(fileName = "ShopData", menuName = "Project H/Data/Shop")] // 상점 에셋 메뉴
    public sealed class ShopData : ScriptableObject, IDataRecord // 상점 정적 데이터
    {
        [SerializeField] private string id; // 상점 고유 ID
        [SerializeField] private string displayName; // 상점 표시 이름
        [SerializeField] private List<ShopProductEntry> products = new List<ShopProductEntry>(); // 상점 상품 목록
        public string Id => id; // 상점 ID 반환
        public string DisplayName => displayName; // 상점 이름 반환
        public IReadOnlyList<ShopProductEntry> Products => products; // 상품 목록 반환

        public ShopProductEntry FindProduct(string productId) // 상품 ID 기반 조회
        {
            if (string.IsNullOrWhiteSpace(productId) || products == null) // 상품 ID 및 목록 확인
            {
                return null; // 조회 실패 반환
            }

            for (int index = 0; index < products.Count; index++) // 상품 목록 순회
            {
                ShopProductEntry product = products[index]; // 현재 상품 조회

                if (product != null && string.Equals(product.ProductId, productId, System.StringComparison.Ordinal)) // 상품 ID 일치 확인
                {
                    return product; // 일치 상품 반환
                }
            }

            return null; // 일치 상품 없음 반환
        }

        public void Configure(string shopId, string shopName, IEnumerable<ShopProductEntry> entries) // Runtime 및 테스트용 상점 구성
        {
            id = shopId ?? string.Empty; // 상점 ID 저장
            displayName = shopName ?? string.Empty; // 상점 이름 저장
            products = entries == null ? new List<ShopProductEntry>() : new List<ShopProductEntry>(entries); // 상품 목록 교체
        }
    }
}
