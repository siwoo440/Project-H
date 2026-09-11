using System; // 난수 기능
using ProjectH.Data; // 장비 데이터 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum EnhanceOutcome // 강화 결과 (Day61 신규)
    {
        Rejected = 0, // 조건 미충족 (아무것도 소모하지 않음)
        Success = 1, // 성공
        Failed = 2 // 실패 (주문서만 소모 — 기획서 '장비 파괴 금지')
    }

    public static class EquipmentUpgradeService // 장비 강화·초월 (Day61 신규 — 대장간 씬에서 사용)
    {
        private static readonly Random SharedRandom = new Random(); // 기본 난수

        public static bool CanUseScroll(EquipmentData equipment, string scrollItemId) // 주문서가 장비 부위에 맞는지
        {
            return equipment != null && EquipmentUpgradeCatalog.TryParseScroll(scrollItemId, out bool weapon, out _) && weapon == EquipmentUpgradeCatalog.IsWeaponScrollSlot(equipment.Slot); // 무기↔무기 주문서, 방어구류↔방어구 주문서
        }

        public static EnhanceOutcome TryEnhance(SaveData saveData, DataManager dataManager, string instanceId, string scrollItemId, out string message, Random random = null) // 강화 1회 (+1)
        {
            EquipmentInstanceSaveData instance = saveData == null ? null : saveData.FindEquipmentInstance(instanceId); // 장비 인스턴스
            EquipmentData equipment = instance == null || dataManager == null ? null : dataManager.GetEquipment(instance.EquipmentId); // 장비 원본

            if (equipment == null) // 장비 확인
            {
                message = "강화할 장비를 선택하세요."; // 안내
                return EnhanceOutcome.Rejected; // 거부
            }

            if (instance.EnhanceLevel >= EquipmentUpgradeCatalog.MaxEnhanceLevel) // 최대 확인
            {
                message = "이미 +5입니다. 초월할 수 있어요."; // 안내
                return EnhanceOutcome.Rejected; // 거부
            }

            if (!CanUseScroll(equipment, scrollItemId)) // 주문서 부위 확인
            {
                message = EquipmentUpgradeCatalog.IsWeaponScrollSlot(equipment.Slot) ? "무기에는 무기 강화 주문서가 필요합니다." : "방어구에는 방어구 강화 주문서가 필요합니다."; // 안내
                return EnhanceOutcome.Rejected; // 거부
            }

            int gold = EquipmentUpgradeCatalog.GetEnhanceGold(instance.EnhanceLevel); // 강화 골드

            if (ItemInventoryService.GetCount(saveData, scrollItemId) <= 0 || GoldCurrencyService.GetGold(saveData) < gold) // 재료 확인
            {
                message = $"재료가 부족합니다 · 주문서 1개 · {gold:N0}G 필요"; // 안내
                return EnhanceOutcome.Rejected; // 거부 (상태 변경 없음)
            }

            EquipmentUpgradeCatalog.TryParseScroll(scrollItemId, out _, out ScrollGrade grade); // 주문서 등급
            float chance = EquipmentUpgradeCatalog.GetSuccessChance(instance.EnhanceLevel, grade, instance.FailStreak); // 성공 확률
            ItemInventoryService.TryRemove(saveData, dataManager, scrollItemId, 1, out _); // 주문서 1개 소모 (성공·실패 공통)

            if ((random ?? SharedRandom).NextDouble() >= chance) // 실패 판정
            {
                instance.SetFailStreak(instance.FailStreak + 1); // 실패 누적 (다음 확률 +10%)
                message = $"강화 실패… 주문서만 사라졌습니다. 다음 확률 {EquipmentUpgradeCatalog.GetSuccessChance(instance.EnhanceLevel, grade, instance.FailStreak) * 100f:0}%"; // 안내
                return EnhanceOutcome.Failed; // 실패 (골드·장비 유지)
            }

            GoldCurrencyService.TrySpendGold(saveData, gold, out _); // 성공 시 골드 소모
            instance.SetEnhanceLevel(instance.EnhanceLevel + 1); // 단계 상승
            instance.SetFailStreak(0); // 실패 누적 초기화
            message = $"강화 성공! {EquipmentUpgradeCatalog.FormatName(equipment.DisplayName, instance)}"; // 안내
            return EnhanceOutcome.Success; // 성공
        }

        public static EquipmentInstanceSaveData FindTranscendMaterial(SaveData saveData, EquipmentInstanceSaveData target) // 초월 재료 : 같은 장비·미장착·다른 인스턴스 (강화·초월이 가장 낮은 것)
        {
            EquipmentInstanceSaveData best = null; // 후보
            if (saveData == null || target == null) return null; // 입력 확인

            foreach (EquipmentInstanceSaveData other in saveData.EquipmentInventory) // 보유 장비 순회
            {
                if (other == null || other == target || other.EquipmentId != target.EquipmentId || CharacterEquipmentService.FindEquippedCharacter(saveData, other.InstanceId) != null) continue; // 조건 불일치 제외
                if (best == null || (other.TranscendStage * 10) + other.EnhanceLevel < (best.TranscendStage * 10) + best.EnhanceLevel) best = other; // 가장 낮은 것 우선
            }

            return best; // 후보 반환
        }

        public static bool TryTranscend(SaveData saveData, DataManager dataManager, string instanceId, out string message) // 초월 (+5 필요, 같은 장비 1개 + 골드, 강화 +0으로 초기화)
        {
            EquipmentInstanceSaveData instance = saveData == null ? null : saveData.FindEquipmentInstance(instanceId); // 장비 인스턴스
            EquipmentData equipment = instance == null || dataManager == null ? null : dataManager.GetEquipment(instance.EquipmentId); // 장비 원본

            if (equipment == null) // 장비 확인
            {
                message = "초월할 장비를 선택하세요."; // 안내
                return false; // 실패
            }

            if (instance.TranscendStage >= EquipmentUpgradeCatalog.MaxTranscendStage) // 최대 초월 확인
            {
                message = "이미 최고 초월(★3)입니다."; // 안내
                return false; // 실패
            }

            if (instance.EnhanceLevel < EquipmentUpgradeCatalog.MaxEnhanceLevel) // +5 확인
            {
                message = "+5까지 강화해야 초월할 수 있습니다."; // 안내
                return false; // 실패
            }

            EquipmentInstanceSaveData material = FindTranscendMaterial(saveData, instance); // 재료 장비
            int gold = EquipmentUpgradeCatalog.GetTranscendGold(instance.TranscendStage); // 초월 골드

            if (material == null || GoldCurrencyService.GetGold(saveData) < gold) // 재료 확인
            {
                message = $"재료가 부족합니다 · 같은 장비({equipment.DisplayName}) 1개 · {gold:N0}G 필요"; // 안내
                return false; // 실패 (상태 변경 없음)
            }

            if (!saveData.TryRemoveEquipmentInstance(material.InstanceId, out _, out string error)) // 재료 장비 제거 (장착 중이면 거부됨)
            {
                message = error; // 오류
                return false; // 실패
            }

            GoldCurrencyService.TrySpendGold(saveData, gold, out _); // 골드 소모
            instance.SetTranscendStage(instance.TranscendStage + 1); // 초월 단계 상승
            instance.SetEnhanceLevel(0); // 강화 초기화 (기획서 '초월 시 강화 단계 초기화')
            instance.SetFailStreak(0); // 실패 누적 초기화
            message = $"초월 성공! {equipment.DisplayName} ★{instance.TranscendStage} · 기본 능력치 ×{EquipmentUpgradeCatalog.GetTranscendMultiplier(instance.TranscendStage):0.0}"; // 안내
            return true; // 성공
        }
    }
}
