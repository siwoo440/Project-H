using System; // 숫자 범위 기능
using System.Collections.Generic; // 장비 롤백 목록 기능
using ProjectH.Data; // 상점 및 아이템 데이터 기능
using ProjectH.SaveSystem; // 저장 및 인벤토리 기능

namespace ProjectH.Shop // 프로젝트 상점 영역
{
    public static class ShopTransactionService // 상점 구매 및 판매 처리 기능
    {
        public static bool TryPurchase(SaveData saveData, DataManager dataManager, ShopData shop, string productId, int quantity, out string error) // 상점 상품 구매
        {
            if (!TryResolveTransaction(saveData, dataManager, shop, productId, quantity, out ShopProductEntry product, out ItemData item, out error)) // 공통 거래 데이터 검증
            {
                return false; // 구매 검증 실패
            }

            if (!TryCalculateTotal(product.BuyPrice, quantity, out int totalPrice, out error)) // 총 구매 가격 계산
            {
                return false; // 가격 계산 실패
            }

            if (GoldCurrencyService.GetGold(saveData) < totalPrice) // Gold 잔액 확인
            {
                error = $"Gold가 부족합니다. Cost={totalPrice}, Gold={GoldCurrencyService.GetGold(saveData)}"; // 잔액 부족 오류 설정
                return false; // 구매 실패 반환
            }

            if (item.Type == ItemType.Equipment) // 장비 상품 여부 확인
            {
                if (dataManager.GetEquipment(item.Id) == null) // 장비 원본 데이터 확인
                {
                    error = $"EquipmentData를 찾을 수 없습니다. ID={item.Id}"; // 장비 원본 누락 오류 설정
                    return false; // 구매 실패 반환
                }
            }
            else // 일반 아이템 처리
            {
                int currentCount = ItemInventoryService.GetCount(saveData, item.Id); // 현재 보유 수량 조회

                if (quantity > item.MaxStack || currentCount > item.MaxStack - quantity) // 최대 스택 초과 확인
                {
                    error = $"아이템 MaxStack을 초과합니다. ID={item.Id}, Current={currentCount}, Add={quantity}, MaxStack={item.MaxStack}"; // 최대 스택 오류 설정
                    return false; // 구매 실패 반환
                }
            }

            if (!GoldCurrencyService.TrySpendGold(saveData, totalPrice, out error)) // Gold 실제 차감
            {
                return false; // 구매 실패 반환
            }

            if (item.Type == ItemType.Equipment) // 장비 상품 지급 여부 확인
            {
                if (!TryGrantEquipment(saveData, item.Id, quantity, out error)) // 고유 장비 인스턴스 지급
                {
                    GoldCurrencyService.AddGold(saveData, totalPrice); // 실패 거래 Gold 환불
                    return false; // 구매 실패 반환
                }

                return true; // 장비 구매 성공 반환
            }

            if (!ItemInventoryService.TryAdd(saveData, dataManager, item.Id, quantity, out error)) // 일반 아이템 지급
            {
                GoldCurrencyService.AddGold(saveData, totalPrice); // 실패 거래 Gold 환불
                return false; // 구매 실패 반환
            }

            return true; // 일반 아이템 구매 성공 반환
        }

        public static bool TrySell(SaveData saveData, DataManager dataManager, ShopData shop, string productId, int quantity, out string error) // 상점 일반 아이템 판매
        {
            if (!TryResolveTransaction(saveData, dataManager, shop, productId, quantity, out ShopProductEntry product, out ItemData item, out error)) // 공통 거래 데이터 검증
            {
                return false; // 판매 검증 실패
            }

            if (!product.CanSell) // 판매 가능 가격 확인
            {
                error = $"판매할 수 없는 상품입니다. Product={product.ProductId}"; // 판매 불가 오류 설정
                return false; // 판매 실패 반환
            }

            if (item.Type == ItemType.Equipment) // 장비 판매 여부 확인
            {
                error = "Day40 기본 상점에서는 장비 판매를 지원하지 않습니다."; // 장비 판매 범위 안내
                return false; // 장비 판매 차단
            }

            if (ItemInventoryService.GetCount(saveData, item.Id) < quantity) // 보유 수량 확인
            {
                error = $"보유 수량이 부족합니다. ID={item.Id}, Need={quantity}"; // 수량 부족 오류 설정
                return false; // 판매 실패 반환
            }

            if (!TryCalculateTotal(product.SellPrice, quantity, out int totalPrice, out error)) // 총 판매 가격 계산
            {
                return false; // 가격 계산 실패
            }

            if (!ItemInventoryService.TryRemove(saveData, dataManager, item.Id, quantity, out error)) // 판매 아이템 실제 제거
            {
                return false; // 판매 실패 반환
            }

            GoldCurrencyService.AddGold(saveData, totalPrice); // 판매 Gold 지급
            return true; // 판매 성공 반환
        }

        private static bool TryResolveTransaction(SaveData saveData, DataManager dataManager, ShopData shop, string productId, int quantity, out ShopProductEntry product, out ItemData item, out string error) // 공통 거래 대상 검증
        {
            product = null; // 상품 결과 초기화
            item = null; // 아이템 결과 초기화
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "SaveData가 없습니다."; // 저장 데이터 누락 오류
                return false; // 거래 검증 실패
            }

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 확인
            {
                error = "DataManager가 초기화되지 않았습니다."; // 데이터 관리자 오류
                return false; // 거래 검증 실패
            }

            if (shop == null) // 상점 데이터 확인
            {
                error = "ShopData가 없습니다."; // 상점 데이터 누락 오류
                return false; // 거래 검증 실패
            }

            if (quantity <= 0) // 거래 수량 확인
            {
                error = "거래 수량은 1 이상이어야 합니다."; // 거래 수량 오류
                return false; // 거래 검증 실패
            }

            product = shop.FindProduct(productId); // 상품 ID 조회

            if (product == null) // 상품 존재 확인
            {
                error = $"상점 상품을 찾을 수 없습니다. Product={productId}"; // 상품 누락 오류
                return false; // 거래 검증 실패
            }

            item = dataManager.GetItem(product.ItemId); // 상품 아이템 조회

            if (item == null) // 아이템 데이터 존재 확인
            {
                error = $"ItemData를 찾을 수 없습니다. ID={product.ItemId}"; // 아이템 누락 오류
                return false; // 거래 검증 실패
            }

            return true; // 공통 거래 검증 성공
        }

        private static bool TryCalculateTotal(int unitPrice, int quantity, out int totalPrice, out string error) // 거래 총액 안전 계산
        {
            totalPrice = 0; // 총액 초기화
            error = string.Empty; // 오류 문구 초기화

            if (unitPrice <= 0 || quantity <= 0) // 가격 및 수량 확인
            {
                error = "거래 가격과 수량은 1 이상이어야 합니다."; // 잘못된 거래 값 오류
                return false; // 총액 계산 실패
            }

            long calculated = (long)unitPrice * quantity; // 오버플로 방지 총액 계산

            if (calculated > int.MaxValue) // 정수 최대 범위 확인
            {
                error = "거래 총액이 허용 범위를 초과했습니다."; // 총액 초과 오류
                return false; // 총액 계산 실패
            }

            totalPrice = (int)calculated; // 안전한 총액 변환
            return true; // 총액 계산 성공
        }

        private static bool TryGrantEquipment(SaveData saveData, string equipmentId, int quantity, out string error) // 구매 장비 인스턴스 지급
        {
            error = string.Empty; // 오류 문구 초기화
            List<string> createdInstanceIds = new List<string>(); // 생성 장비 롤백 목록

            for (int index = 0; index < quantity; index++) // 요청 장비 수량 순회
            {
                if (!saveData.TryCreateEquipmentInstance(equipmentId, out EquipmentInstanceSaveData instance, out error)) // 고유 장비 인스턴스 생성
                {
                    RollbackEquipment(saveData, createdInstanceIds); // 이전 생성 장비 롤백
                    return false; // 장비 지급 실패 반환
                }

                createdInstanceIds.Add(instance.InstanceId); // 생성 인스턴스 ID 기록
            }

            return true; // 장비 지급 성공 반환
        }

        private static void RollbackEquipment(SaveData saveData, IReadOnlyList<string> instanceIds) // 실패 장비 거래 롤백
        {
            for (int index = instanceIds.Count - 1; index >= 0; index--) // 생성 장비 역순 순회
            {
                saveData.TryRemoveEquipmentInstance(instanceIds[index], out _, out _); // 생성 장비 인벤토리 제거
            }
        }
    }
}
