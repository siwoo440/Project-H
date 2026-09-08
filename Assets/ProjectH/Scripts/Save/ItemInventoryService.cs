using ProjectH.Data; // 아이템 정적 데이터 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class ItemInventoryService // 일반 아이템 인벤토리 서비스
    {
        public static int GetCount(SaveData saveData, string itemId) // 아이템 보유 수량 조회
        {
            return saveData == null ? 0 : saveData.GetItemCount(itemId); // 저장 데이터 기반 수량 반환
        }

        public static bool TryAdd(SaveData saveData, DataManager dataManager, string itemId, int quantity, out string error) // 일반 아이템 획득
        {
            error = string.Empty; // 오류 문구 초기화

            if (quantity <= 0) // 획득 수량 확인
            {
                error = "획득 수량은 1 이상이어야 합니다."; // 잘못된 획득 수량 오류 설정
                return false; // 아이템 획득 실패
            }

            if (!TryResolveStorableItem(saveData, dataManager, itemId, out ItemData item, out error)) // 저장 가능 아이템 검증
            {
                return false; // 아이템 획득 실패
            }

            int currentCount = saveData.GetItemCount(itemId); // 현재 아이템 수량 조회

            if (currentCount > item.MaxStack - quantity) // 최대 스택 초과 여부 확인
            {
                error = $"아이템 MaxStack을 초과합니다. ID={itemId}, Current={currentCount}, Add={quantity}, MaxStack={item.MaxStack}"; // 최대 스택 오류 설정
                return false; // 아이템 획득 실패
            }

            int targetCount = currentCount + quantity; // 획득 후 수량 계산
            return saveData.TrySetItemCountInternal(itemId, targetCount, out error); // 저장 데이터 수량 적용
        }

        public static bool TryRemove(SaveData saveData, DataManager dataManager, string itemId, int quantity, out string error) // 일반 아이템 제거
        {
            error = string.Empty; // 오류 문구 초기화

            if (quantity <= 0) // 제거 수량 확인
            {
                error = "제거 수량은 1 이상이어야 합니다."; // 잘못된 제거 수량 오류 설정
                return false; // 아이템 제거 실패
            }

            if (!TryResolveStorableItem(saveData, dataManager, itemId, out ItemData item, out error)) // 저장 가능 아이템 검증
            {
                return false; // 아이템 제거 실패
            }

            int currentCount = saveData.GetItemCount(item.Id); // 현재 아이템 수량 조회

            if (currentCount < quantity) // 보유 수량 부족 여부 확인
            {
                error = $"보유 아이템 수량이 부족합니다. ID={itemId}, Current={currentCount}, Remove={quantity}"; // 수량 부족 오류 설정
                return false; // 아이템 제거 실패
            }

            int targetCount = currentCount - quantity; // 제거 후 수량 계산
            return saveData.TrySetItemCountInternal(itemId, targetCount, out error); // 저장 데이터 수량 적용
        }

        private static bool TryResolveStorableItem(SaveData saveData, DataManager dataManager, string itemId, out ItemData item, out string error) // 일반 인벤토리 저장 가능 아이템 검증
        {
            item = null; // 아이템 조회 결과 초기화
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "SaveData가 없습니다."; // 저장 데이터 누락 오류 설정
                return false; // 아이템 검증 실패
            }

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 초기화 확인
            {
                error = "DataManager가 초기화되지 않았습니다."; // 데이터 관리자 오류 설정
                return false; // 아이템 검증 실패
            }

            if (string.IsNullOrWhiteSpace(itemId)) // 아이템 ID 확인
            {
                error = "아이템 ID가 비어 있습니다."; // 빈 아이템 ID 오류 설정
                return false; // 아이템 검증 실패
            }

            item = dataManager.GetItem(itemId); // 아이템 원본 데이터 조회

            if (item == null) // 아이템 데이터 존재 확인
            {
                error = $"ItemData를 찾을 수 없습니다. ID={itemId}"; // 아이템 데이터 누락 오류 설정
                return false; // 아이템 검증 실패
            }

            if (item.Type == ItemType.Equipment) // 장비 아이템 여부 확인
            {
                error = $"장비 아이템은 EquipmentInventory를 사용해야 합니다. ID={itemId}"; // 장비 전용 인벤토리 안내 설정
                return false; // 일반 인벤토리 저장 차단
            }

            return true; // 일반 인벤토리 저장 가능 반환
        }
    }
}
