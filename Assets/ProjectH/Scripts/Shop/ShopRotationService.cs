using System; // 난수 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 상점 데이터 기능
using ProjectH.SaveSystem; // 저장 기능

namespace ProjectH.Shop // 프로젝트 상점 영역
{
    public static class ShopRotationService // 상시 상품·오늘의 상품·하루 재고·리롤 (Day61 신규 — 기획서 7. 상점 '일자 갱신·재고 제한·리롤·품절')
    {
        public const int RotationSlotCount = 4; // 오늘의 상품 칸 수
        public const int MaxRerollsPerDay = 3; // 하루 최대 리롤
        private static readonly int[] RerollCosts = { 100, 200, 400 }; // 1·2·3번째 리롤 골드

        public static int GetRerollCost(int rerollCount) => rerollCount >= MaxRerollsPerDay ? 0 : RerollCosts[Math.Max(0, rerollCount)]; // 다음 리롤 비용 (0 = 불가)

        public static ShopStateSaveData EnsureToday(SaveData saveData, ShopData shop) // 오늘 상태 보장 (날짜가 바뀌면 재고·리롤 초기화 + 오늘의 상품 재생성)
        {
            if (saveData == null || shop == null) return null; // 입력 확인
            ShopStateSaveData state = saveData.GetOrCreateShopState(shop.Id); // 상점 상태

            if (state.Day != saveData.CurrentDay) // 날짜 변경 확인
            {
                state.ResetForDay(saveData.CurrentDay, PickRotation(shop, saveData.CurrentDay, 0)); // 새 일차 상태
            }

            return state; // 상태 반환
        }

        public static List<ShopProductEntry> GetPermanentProducts(ShopData shop) // 상시 상품 목록
        {
            List<ShopProductEntry> result = new List<ShopProductEntry>(); // 결과
            if (shop == null || shop.Products == null) return result; // 입력 확인

            foreach (ShopProductEntry product in shop.Products) // 상품 순회
            {
                if (product != null && !product.Rotating) result.Add(product); // 상시 상품만
            }

            return result; // 결과 반환
        }

        public static List<ShopProductEntry> GetTodayProducts(SaveData saveData, ShopData shop) // 오늘의 상품 목록
        {
            List<ShopProductEntry> result = new List<ShopProductEntry>(); // 결과
            ShopStateSaveData state = EnsureToday(saveData, shop); // 오늘 상태
            if (state == null) return result; // 상태 없음

            foreach (string productId in state.RotationProductIds) // 오늘의 상품 ID 순회
            {
                ShopProductEntry product = shop.FindProduct(productId); // 상품 조회
                if (product != null) result.Add(product); // 유효 상품만
            }

            return result; // 결과 반환
        }

        public static int GetRemaining(SaveData saveData, ShopData shop, ShopProductEntry product) // 남은 재고 (-1 = 무제한)
        {
            if (product == null) return 0; // 상품 없음
            if (!product.HasDailyLimit) return -1; // 무제한
            ShopStateSaveData state = EnsureToday(saveData, shop); // 오늘 상태
            int purchased = state == null ? 0 : state.GetPurchased(product.ProductId); // 오늘 구매 수량
            return Math.Max(0, product.DailyLimit - purchased); // 남은 재고
        }

        public static bool IsAvailableToday(SaveData saveData, ShopData shop, ShopProductEntry product) // 오늘 진열 중인지
        {
            if (product == null) return false; // 상품 없음
            if (!product.Rotating) return true; // 상시 상품
            ShopStateSaveData state = EnsureToday(saveData, shop); // 오늘 상태
            return state != null && ContainsId(state.RotationProductIds, product.ProductId); // 오늘의 상품 포함 여부
        }

        public static bool TryBuy(SaveData saveData, DataManager dataManager, ShopData shop, string productId, int quantity, out string error) // 재고 반영 구매
        {
            ShopProductEntry product = shop == null ? null : shop.FindProduct(productId); // 상품 조회

            if (!IsAvailableToday(saveData, shop, product)) // 오늘 진열 확인
            {
                error = "오늘은 판매하지 않는 상품입니다."; // 안내
                return false; // 실패
            }

            int remaining = GetRemaining(saveData, shop, product); // 남은 재고

            if (remaining >= 0 && remaining < quantity) // 재고 확인
            {
                error = remaining == 0 ? "품절되었습니다. 내일 다시 입고됩니다." : $"재고가 부족합니다. 남은 재고 {remaining}"; // 안내
                return false; // 실패
            }

            if (!ShopTransactionService.TryPurchase(saveData, dataManager, shop, productId, quantity, out error)) // 실제 거래
            {
                return false; // 거래 실패
            }

            EnsureToday(saveData, shop).AddPurchase(productId, quantity); // 구매 기록
            return true; // 성공
        }

        public static bool TryReroll(SaveData saveData, ShopData shop, out string error) // 오늘의 상품 리롤 (골드 소모, 하루 3회)
        {
            ShopStateSaveData state = EnsureToday(saveData, shop); // 오늘 상태

            if (state == null) // 상태 확인
            {
                error = "상점 데이터를 찾을 수 없습니다."; // 안내
                return false; // 실패
            }

            if (state.RerollCount >= MaxRerollsPerDay) // 횟수 확인
            {
                error = "오늘은 더 이상 새로고침할 수 없습니다."; // 안내
                return false; // 실패
            }

            if (!GoldCurrencyService.TrySpendGold(saveData, GetRerollCost(state.RerollCount), out error)) // 골드 소모
            {
                return false; // 골드 부족
            }

            state.ApplyReroll(PickRotation(shop, saveData.CurrentDay, state.RerollCount + 1)); // 오늘의 상품 교체
            return true; // 성공
        }

        public static List<string> PickRotation(ShopData shop, int day, int rerollCount) // 일차·리롤 기반 오늘의 상품 선택 (같은 입력이면 항상 같은 결과)
        {
            List<string> candidates = new List<string>(); // 후보 ID
            if (shop == null || shop.Products == null) return candidates; // 입력 확인

            foreach (ShopProductEntry product in shop.Products) // 상품 순회
            {
                if (product != null && product.Rotating) candidates.Add(product.ProductId); // 오늘의 상품 후보
            }

            Random random = new Random(StableHash($"{shop.Id}|{day}|{rerollCount}")); // 결정적 난수 (저장·재시작해도 동일, Day62 키 전체 해시로 보강)

            for (int index = candidates.Count - 1; index > 0; index--) // 후보 섞기 (Fisher-Yates)
            {
                int swap = random.Next(index + 1); // 교환 위치
                string temp = candidates[index]; // 임시 보관
                candidates[index] = candidates[swap]; // 교환
                candidates[swap] = temp; // 교환
            }

            if (candidates.Count > RotationSlotCount) candidates.RemoveRange(RotationSlotCount, candidates.Count - RotationSlotCount); // 4개만 남김
            return candidates; // 결과 반환
        }

        private static int StableHash(string value) // 실행 환경과 무관한 문자열 해시 (FNV-1a)
        {
            unchecked // 오버플로 허용
            {
                int hash = (int)2166136261; // 초기값
                foreach (char c in value ?? string.Empty) hash = (hash ^ c) * 16777619; // 문자 누적
                return hash & 0x7FFFFFFF; // 양수 반환
            }
        }

        private static bool ContainsId(IReadOnlyList<string> ids, string id) // 목록 포함 여부
        {
            for (int index = 0; index < ids.Count; index++) // 목록 순회
            {
                if (string.Equals(ids[index], id, StringComparison.Ordinal)) return true; // 일치
            }

            return false; // 없음
        }
    }
}
