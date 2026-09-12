using System; // 문자열 비교 기능
using ProjectH.Data; // 장비 원본 데이터 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class CharacterEquipmentService // 캐릭터 장비 착용 서비스
    {
        public static bool TryEquip(SaveData saveData, DataManager dataManager, string characterId, string instanceId, out string error) // 장비 착용 또는 교체
        {
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "저장 데이터가 없습니다."; // 저장 데이터 오류 설정
                return false; // 장비 착용 실패
            }

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 상태 확인
            {
                error = "DataManager가 초기화되지 않았습니다."; // 데이터 관리자 오류 설정
                return false; // 장비 착용 실패
            }

            CharacterSaveData character = saveData.FindCharacter(characterId); // 대상 캐릭터 저장 조회

            if (character == null) // 대상 캐릭터 보유 여부 확인
            {
                error = $"보유 캐릭터를 찾을 수 없습니다. ID={characterId}"; // 캐릭터 조회 오류 설정
                return false; // 장비 착용 실패
            }

            EquipmentInstanceSaveData equipmentInstance = saveData.FindEquipmentInstance(instanceId); // 보유 장비 인스턴스 조회

            if (equipmentInstance == null) // 장비 보유 여부 확인
            {
                error = $"보유 장비 인스턴스를 찾을 수 없습니다. ID={instanceId}"; // 미보유 장비 오류 설정
                return false; // 장비 착용 실패
            }

            EquipmentData equipmentData = dataManager.GetEquipment(equipmentInstance.EquipmentId); // 장비 원본 데이터 조회

            if (equipmentData == null) // 장비 원본 존재 확인
            {
                error = $"장비 원본 데이터를 찾을 수 없습니다. ID={equipmentInstance.EquipmentId}"; // 장비 원본 오류 설정
                return false; // 장비 착용 실패
            }

            if (!UniqueEquipmentService.CanCharacterEquip(character.CharacterId, equipmentInstance.EquipmentId)) // 전용 장비 주인 확인 (Day70 추가)
            {
                error = $"{equipmentData.DisplayName}은(는) 전용 장비라 주인만 착용할 수 있습니다."; // 전용 장비 오류 설정
                return false; // 장비 착용 실패
            }

            CharacterSaveData currentOwner = FindEquippedCharacter(saveData, instanceId); // 현재 장비 착용 캐릭터 조회

            if (currentOwner != null && !string.Equals(currentOwner.CharacterId, character.CharacterId, StringComparison.Ordinal)) // 다른 캐릭터 착용 여부 확인
            {
                error = $"다른 캐릭터가 이미 장착 중입니다. Character={currentOwner.CharacterId}"; // 중복 착용 오류 설정
                return false; // 장비 중복 착용 차단
            }

            character.Equipment.ClearInstance(instanceId); // 같은 캐릭터의 비정상 중복 슬롯 정리
            character.Equipment.SetInstanceId(equipmentData.Slot, instanceId); // 장비 슬롯 착용 또는 교체
            return true; // 장비 착용 성공
        }

        public static bool TryUnequip(SaveData saveData, string characterId, EquipmentSlot slot, out EquipmentInstanceSaveData unequippedInstance, out string error) // 슬롯 장비 해제
        {
            unequippedInstance = null; // 해제 장비 결과 초기화
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "저장 데이터가 없습니다."; // 저장 데이터 오류 설정
                return false; // 장비 해제 실패
            }

            CharacterSaveData character = saveData.FindCharacter(characterId); // 대상 캐릭터 저장 조회

            if (character == null) // 대상 캐릭터 보유 여부 확인
            {
                error = $"보유 캐릭터를 찾을 수 없습니다. ID={characterId}"; // 캐릭터 조회 오류 설정
                return false; // 장비 해제 실패
            }

            string instanceId = character.Equipment.GetInstanceId(slot); // 대상 슬롯 장비 인스턴스 조회

            if (string.IsNullOrWhiteSpace(instanceId)) // 장착 장비 존재 확인
            {
                error = $"해제할 장비가 없습니다. Slot={slot}"; // 빈 슬롯 오류 설정
                return false; // 장비 해제 실패
            }

            unequippedInstance = saveData.FindEquipmentInstance(instanceId); // 해제 대상 장비 인스턴스 조회
            character.Equipment.SetInstanceId(slot, string.Empty); // 대상 장비 슬롯 비우기
            return true; // 장비 해제 성공
        }

        public static EquipmentInstanceSaveData GetEquippedInstance(SaveData saveData, string characterId, EquipmentSlot slot) // 캐릭터 슬롯 장착 장비 조회
        {
            if (saveData == null) // 저장 데이터 확인
            {
                return null; // 장착 장비 조회 실패
            }

            CharacterSaveData character = saveData.FindCharacter(characterId); // 대상 캐릭터 저장 조회

            if (character == null) // 대상 캐릭터 존재 확인
            {
                return null; // 장착 장비 조회 실패
            }

            string instanceId = character.Equipment.GetInstanceId(slot); // 슬롯 장비 인스턴스 ID 조회
            return saveData.FindEquipmentInstance(instanceId); // 장착 장비 인스턴스 반환
        }

        public static CharacterSaveData FindEquippedCharacter(SaveData saveData, string instanceId) // 특정 장비 착용 캐릭터 조회
        {
            if (saveData == null || string.IsNullOrWhiteSpace(instanceId)) // 저장 데이터 및 장비 ID 확인
            {
                return null; // 장비 착용 캐릭터 없음 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 확인

            foreach (CharacterSaveData character in saveData.Characters) // 보유 캐릭터 순회
            {
                if (character != null && character.Equipment.ContainsInstance(instanceId)) // 장비 착용 여부 확인
                {
                    return character; // 장비 착용 캐릭터 반환
                }
            }

            return null; // 장비 미착용 반환
        }

        public static bool IsEquipped(SaveData saveData, string instanceId) // 장비 착용 여부 확인
        {
            return FindEquippedCharacter(saveData, instanceId) != null; // 착용 캐릭터 존재 여부 반환
        }
    }
}
