using ProjectH.Data; // 아이템 정적 데이터 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class ItemUseService // 일반 아이템 사용 서비스
    {
        public static bool TryUse(SaveData saveData, DataManager dataManager, string itemId, out int remainingCount, out string error) // 소비 아이템 1개 사용
        {
            remainingCount = ItemInventoryService.GetCount(saveData, itemId); // 현재 아이템 수량 초기화
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "SaveData가 없습니다."; // 저장 데이터 누락 오류 설정
                return false; // 아이템 사용 실패
            }

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 확인
            {
                error = "DataManager가 초기화되지 않았습니다."; // 데이터 관리자 오류 설정
                return false; // 아이템 사용 실패
            }

            ItemData item = dataManager.GetItem(itemId); // 아이템 원본 데이터 조회

            if (item == null) // 아이템 데이터 존재 확인
            {
                error = $"ItemData를 찾을 수 없습니다. ID={itemId}"; // 아이템 데이터 누락 오류 설정
                return false; // 아이템 사용 실패
            }

            if (item.Type != ItemType.Consumable) // 소비 아이템 유형 확인
            {
                error = $"Consumable 타입만 직접 사용할 수 있습니다. ID={itemId}, Type={item.Type}"; // 사용 불가 유형 오류 설정
                return false; // 아이템 사용 실패
            }

            if (remainingCount <= 0) // 아이템 보유 여부 확인
            {
                error = $"사용할 아이템을 보유하고 있지 않습니다. ID={itemId}"; // 미보유 아이템 오류 설정
                return false; // 아이템 사용 실패
            }

            if (!ItemInventoryService.TryRemove(saveData, dataManager, itemId, 1, out error)) // 소비 아이템 1개 제거 시도
            {
                return false; // 아이템 사용 실패
            }

            remainingCount = ItemInventoryService.GetCount(saveData, itemId); // 사용 후 남은 수량 갱신
            return true; // 아이템 사용 성공
        }
    }
}
