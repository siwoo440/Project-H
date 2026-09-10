using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 아이템 데이터 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class DungeonDropGrantService // 던전 드롭 영구 지급 기능
    {
        public static bool CanGrantAll(SaveData saveData, DataManager dataManager, IReadOnlyList<DungeonDropResult> drops, out string error) // 전체 드롭 지급 가능 여부 검증
        {
            error = string.Empty; // 오류 문구 초기화

            if (drops == null || drops.Count == 0) // 드롭 존재 확인
            {
                return true; // 지급 대상 없음 처리
            }

            if (saveData == null) // 저장 데이터 확인
            {
                error = "SaveData가 없습니다."; // 저장 데이터 누락 오류 설정
                return false; // 지급 검증 실패
            }

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 초기화 확인
            {
                error = "DataManager가 초기화되지 않았습니다."; // 데이터 관리자 오류 설정
                return false; // 지급 검증 실패
            }

            for (int index = 0; index < drops.Count; index++) // 드롭 결과 순회
            {
                DungeonDropResult drop = drops[index]; // 현재 드롭 결과 조회

                if (drop == null || string.IsNullOrWhiteSpace(drop.ItemId) || drop.Quantity <= 0) // 드롭 결과 유효성 확인
                {
                    error = $"잘못된 던전 드롭 결과가 있습니다. index={index}"; // 잘못된 드롭 결과 오류 설정
                    return false; // 지급 검증 실패
                }

                ItemData item = dataManager.GetItem(drop.ItemId); // 드롭 아이템 데이터 조회

                if (item == null) // 아이템 데이터 존재 확인
                {
                    error = $"드롭 ItemData를 찾을 수 없습니다. ID={drop.ItemId}"; // 아이템 누락 오류 설정
                    return false; // 지급 검증 실패
                }

                if (item.Type == ItemType.Equipment && dataManager.GetEquipment(item.Id) == null) // 장비 원본 연결 확인
                {
                    error = $"드롭 EquipmentData를 찾을 수 없습니다. ID={drop.ItemId}"; // 장비 원본 누락 오류 설정
                    return false; // 지급 검증 실패
                }
            }

            return true; // 전체 드롭 지급 가능 반환
        }

        public static void GrantAll(SaveData saveData, DataManager dataManager, IReadOnlyList<DungeonDropResult> drops) // 전체 드롭 영구 지급
        {
            if (saveData == null || dataManager == null || drops == null) // 지급 필수 데이터 확인
            {
                return; // 잘못된 지급 요청 중단
            }

            for (int index = 0; index < drops.Count; index++) // 드롭 결과 순회
            {
                DungeonDropResult drop = drops[index]; // 현재 드롭 결과 조회

                if (drop == null || drop.Quantity <= 0) // 유효 드롭 여부 확인
                {
                    continue; // 잘못된 드롭 결과 제외
                }

                ItemData item = dataManager.GetItem(drop.ItemId); // 드롭 아이템 데이터 조회

                if (item == null) // 아이템 데이터 존재 확인
                {
                    continue; // 누락 아이템 지급 제외
                }

                if (item.Type == ItemType.Equipment) // 장비 드롭 여부 확인
                {
                    GrantEquipment(saveData, item.Id, drop.Quantity); // 장비 인스턴스 지급
                    continue; // 일반 아이템 지급 제외
                }

                int currentCount = ItemInventoryService.GetCount(saveData, item.Id); // 현재 일반 아이템 수량 조회
                int availableCount = item.MaxStack - currentCount; // 남은 일반 아이템 스택 공간 계산
                int grantQuantity = availableCount <= 0 ? 0 : System.Math.Min(drop.Quantity, availableCount); // 실제 지급 가능 수량 계산

                if (grantQuantity <= 0) // 실제 지급 수량 확인
                {
                    continue; // 가득 찬 스택 지급 제외
                }

                ItemInventoryService.TryAdd(saveData, dataManager, item.Id, grantQuantity, out _); // 일반 아이템 실제 지급
            }
        }

        private static void GrantEquipment(SaveData saveData, string equipmentId, int quantity) // 장비 드롭 인스턴스 지급
        {
            for (int index = 0; index < quantity; index++) // 장비 드롭 수량 순회
            {
                saveData.TryCreateEquipmentInstance(equipmentId, out _, out _); // 고유 장비 인스턴스 생성
            }
        }
    }
}
