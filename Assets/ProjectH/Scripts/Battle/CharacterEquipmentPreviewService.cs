using System; // 문자열 비교 기능
using ProjectH.Data; // 캐릭터 및 장비 데이터 기능
using ProjectH.SaveSystem; // 저장 및 장착 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class CharacterEquipmentPreviewService // 캐릭터 장비 변경 미리보기 서비스
    {
        public static bool TryCreate(SaveData saveData, DataManager dataManager, string characterId, string selectedInstanceId, out CharacterEquipmentPreview preview, out string error) // 선택 장비 변경 미리보기 생성
        {
            preview = null; // 미리보기 결과 초기화
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "저장 데이터가 없습니다."; // 저장 데이터 오류 설정
                return false; // 미리보기 생성 실패
            }

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 상태 확인
            {
                error = "DataManager가 초기화되지 않았습니다."; // 데이터 관리자 오류 설정
                return false; // 미리보기 생성 실패
            }

            CharacterSaveData characterSave = saveData.FindCharacter(characterId); // 대상 캐릭터 저장 조회

            if (characterSave == null) // 대상 캐릭터 존재 확인
            {
                error = $"보유 캐릭터를 찾을 수 없습니다. ID={characterId}"; // 캐릭터 조회 오류 설정
                return false; // 미리보기 생성 실패
            }

            CharacterData characterData = dataManager.GetCharacter(characterSave.CharacterId); // 캐릭터 원본 데이터 조회

            if (characterData == null) // 캐릭터 원본 존재 확인
            {
                error = $"CharacterData를 찾을 수 없습니다. ID={characterSave.CharacterId}"; // 캐릭터 원본 오류 설정
                return false; // 미리보기 생성 실패
            }

            EquipmentInstanceSaveData selectedInstance = saveData.FindEquipmentInstance(selectedInstanceId); // 선택 장비 인스턴스 조회

            if (selectedInstance == null) // 선택 장비 보유 여부 확인
            {
                error = $"보유 장비 인스턴스를 찾을 수 없습니다. ID={selectedInstanceId}"; // 장비 인스턴스 오류 설정
                return false; // 미리보기 생성 실패
            }

            EquipmentData selectedEquipment = dataManager.GetEquipment(selectedInstance.EquipmentId); // 선택 장비 원본 조회

            if (selectedEquipment == null) // 선택 장비 원본 존재 확인
            {
                error = $"EquipmentData를 찾을 수 없습니다. ID={selectedInstance.EquipmentId}"; // 장비 원본 오류 설정
                return false; // 미리보기 생성 실패
            }

            CharacterSaveData owner = CharacterEquipmentService.FindEquippedCharacter(saveData, selectedInstance.InstanceId); // 선택 장비 착용 캐릭터 조회

            if (owner != null && !string.Equals(owner.CharacterId, characterSave.CharacterId, StringComparison.Ordinal)) // 다른 캐릭터 착용 여부 확인
            {
                error = $"다른 캐릭터가 이미 장착 중입니다. Character={owner.CharacterId}"; // 다른 캐릭터 장착 오류 설정
                return false; // 미리보기 생성 실패
            }

            if (!BattleEquipmentStatCalculator.TryCalculate(characterSave, saveData, dataManager, out BattleEquipmentStatBonus currentBonus, out error)) // 현재 장비 보정 계산
            {
                return false; // 현재 능력치 계산 실패
            }

            EquipmentInstanceSaveData equippedInstance = CharacterEquipmentService.GetEquippedInstance(saveData, characterSave.CharacterId, selectedEquipment.Slot); // 대상 슬롯 현재 장비 조회
            bool willUnequip = equippedInstance != null && string.Equals(equippedInstance.InstanceId, selectedInstance.InstanceId, StringComparison.Ordinal); // 현재 장비 선택 여부 계산
            string overrideInstanceId = willUnequip ? string.Empty : selectedInstance.InstanceId; // 가상 장비 슬롯 값 결정
            string replacedInstanceId = equippedInstance == null ? string.Empty : equippedInstance.InstanceId; // 기존 장착 인스턴스 ID 저장

            if (!BattleEquipmentStatCalculator.TryCalculateWithSlotOverride(characterSave, saveData, dataManager, selectedEquipment.Slot, overrideInstanceId, out BattleEquipmentStatBonus previewBonus, out error)) // 변경 후 장비 보정 계산
            {
                return false; // 예상 능력치 계산 실패
            }

            BattleStats currentStats = BattleStatsFactory.CreateCharacter(characterData, characterSave, "EQUIPMENT_CURRENT", currentBonus); // 현재 최종 능력치 생성
            BattleStats previewStats = BattleStatsFactory.CreateCharacter(characterData, characterSave, "EQUIPMENT_PREVIEW", previewBonus); // 변경 후 예상 능력치 생성
            preview = new CharacterEquipmentPreview(currentStats, previewStats, selectedEquipment.Slot, selectedInstance.InstanceId, replacedInstanceId, willUnequip); // 최종 장비 미리보기 생성
            return true; // 미리보기 생성 성공
        }
    }
}
