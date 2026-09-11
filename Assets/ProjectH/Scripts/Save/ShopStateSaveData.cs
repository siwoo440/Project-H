using System; // 직렬화 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 직렬화 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    [Serializable] // JSON 직렬화 허용
    public sealed class ShopPurchaseSaveData // 오늘 구매한 상품 수량 (Day61 신규)
    {
        [SerializeField] private string productId; // 상품 ID
        [SerializeField] private int count; // 오늘 구매 수량

        public string ProductId => productId ?? string.Empty; // 상품 ID 반환
        public int Count => Math.Max(0, count); // 구매 수량 반환

        public ShopPurchaseSaveData(string id, int amount) // 구매 기록 생성
        {
            productId = id ?? string.Empty; // 상품 ID 저장
            count = Math.Max(0, amount); // 수량 저장
        }

        internal void Add(int amount) => count = Math.Max(0, count + amount); // 수량 누적
    }

    [Serializable] // JSON 직렬화 허용
    public sealed class ShopStateSaveData // 상점 하루 상태 (Day61 신규 — 오늘의 상품·재고·리롤, 날짜가 바뀌면 초기화)
    {
        [SerializeField] private string shopId; // 상점 ID
        [SerializeField] private int day; // 상태가 만들어진 일차 (0 = 아직 없음)
        [SerializeField] private int rerollCount; // 오늘 리롤 횟수
        [SerializeField] private List<string> rotationProductIds = new List<string>(); // 오늘의 상품 ID 목록
        [SerializeField] private List<ShopPurchaseSaveData> purchases = new List<ShopPurchaseSaveData>(); // 오늘 구매 기록

        public string ShopId => shopId ?? string.Empty; // 상점 ID 반환
        public int Day => Math.Max(0, day); // 일차 반환
        public int RerollCount => Math.Max(0, rerollCount); // 리롤 횟수 반환
        public IReadOnlyList<string> RotationProductIds => rotationProductIds; // 오늘의 상품 반환

        public ShopStateSaveData(string id) // 상점 상태 생성
        {
            shopId = id ?? string.Empty; // 상점 ID 저장
        }

        public int GetPurchased(string productId) // 오늘 구매 수량 조회
        {
            ShopPurchaseSaveData record = Find(productId); // 기록 조회
            return record == null ? 0 : record.Count; // 수량 반환
        }

        internal void EnsureDefaults() // 역직렬화 기본값 보정
        {
            if (rotationProductIds == null) rotationProductIds = new List<string>(); // 오늘의 상품 복원
            if (purchases == null) purchases = new List<ShopPurchaseSaveData>(); // 구매 기록 복원
            purchases.RemoveAll(record => record == null || string.IsNullOrWhiteSpace(record.ProductId)); // 잘못된 기록 제거
        }

        internal void ResetForDay(int newDay, IEnumerable<string> rotation) // 새 일차 상태로 초기화 (재고·리롤 초기화)
        {
            EnsureDefaults(); // 목록 확인
            day = Math.Max(1, newDay); // 일차 저장
            rerollCount = 0; // 리롤 초기화
            purchases.Clear(); // 구매 기록 초기화
            SetRotation(rotation); // 오늘의 상품 저장
        }

        internal void ApplyReroll(IEnumerable<string> rotation) // 리롤 적용 (구매 기록은 유지 — 리롤로 재고 초기화 방지)
        {
            rerollCount++; // 리롤 횟수 증가
            SetRotation(rotation); // 오늘의 상품 교체
        }

        internal void AddPurchase(string productId, int amount) // 구매 기록 추가
        {
            EnsureDefaults(); // 목록 확인
            ShopPurchaseSaveData record = Find(productId); // 기존 기록

            if (record == null) // 첫 구매
            {
                purchases.Add(new ShopPurchaseSaveData(productId, amount)); // 기록 추가
                return; // 종료
            }

            record.Add(amount); // 수량 누적
        }

        private void SetRotation(IEnumerable<string> rotation) // 오늘의 상품 목록 교체
        {
            rotationProductIds.Clear(); // 기존 목록 제거
            if (rotation != null) rotationProductIds.AddRange(rotation); // 새 목록 추가
        }

        private ShopPurchaseSaveData Find(string productId) // 구매 기록 조회
        {
            if (purchases == null) return null; // 목록 없음

            for (int index = 0; index < purchases.Count; index++) // 기록 순회
            {
                if (purchases[index] != null && string.Equals(purchases[index].ProductId, productId, StringComparison.Ordinal)) return purchases[index]; // 일치 기록
            }

            return null; // 없음
        }
    }
}
