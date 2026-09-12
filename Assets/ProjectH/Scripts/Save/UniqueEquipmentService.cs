using ProjectH.Data; // 장비 슬롯·데이터 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class UniqueEquipmentService // 전용 장비 지급·초월·조건 확인 (Day70 신규 — 룬 5번 칸 조건과 연결)
    {
        public static bool HasUnique(SaveData saveData, string characterId) // 그 동료의 전용 장비를 이미 가지고 있는지
        {
            UniqueEquipmentDefinition definition = UniqueEquipmentCatalog.FindByCharacter(characterId); // 정의 조회
            return FindInstance(saveData, definition) != null; // 보유 여부 반환
        }

        public static EquipmentInstanceSaveData FindInstance(SaveData saveData, UniqueEquipmentDefinition definition) // 보유 중인 전용 장비 인스턴스 조회
        {
            if (saveData == null || definition == null) return null; // 입력 확인

            foreach (EquipmentInstanceSaveData instance in saveData.EquipmentInventory) // 보유 장비 순회
            {
                if (instance != null && instance.EquipmentId == definition.EquipmentId) return instance; // 일치 반환
            }

            return null; // 없음
        }

        public static bool TryGrant(SaveData saveData, string characterId, out EquipmentInstanceSaveData instance, out string message) // 전용 장비 지급 (개인 이야기 1화 보상 · 중복 지급 없음)
        {
            instance = null; // 결과 초기화
            UniqueEquipmentDefinition definition = UniqueEquipmentCatalog.FindByCharacter(characterId); // 정의 조회

            if (saveData == null || definition == null) // 입력 확인
            {
                message = string.Empty; // 안내 없음
                return false; // 실패
            }

            EquipmentInstanceSaveData owned = FindInstance(saveData, definition); // 이미 보유한 장비

            if (owned != null) // 중복 확인
            {
                instance = owned; // 기존 장비 반환
                message = string.Empty; // 안내 없음 (조용히 넘어감)
                return false; // 새로 지급하지 않음
            }

            if (!saveData.TryCreateEquipmentInstance(definition.EquipmentId, out instance, out _)) // 장비 생성
            {
                message = string.Empty; // 데이터가 아직 없으면 조용히 무시
                return false; // 실패
            }

            message = $"전용 장비 획득! {definition.DisplayName} (공격력 +{definition.Attack} · {definition.SubStatText})"; // 안내
            return true; // 성공
        }

        public static bool CanTranscend(SaveData saveData, EquipmentInstanceSaveData instance, out string reason) // 전용 장비 초월 가능 여부 (같은 장비 대신 결속 단계가 재료)
        {
            UniqueEquipmentDefinition definition = instance == null ? null : UniqueEquipmentCatalog.FindByEquipment(instance.EquipmentId); // 정의 조회

            if (saveData == null || definition == null) // 전용 장비 확인
            {
                reason = "전용 장비가 아닙니다."; // 안내
                return false; // 불가
            }

            if (instance.TranscendStage >= EquipmentUpgradeCatalog.MaxTranscendStage) // 최대 초월 확인
            {
                reason = "이미 최고 초월(★3)입니다."; // 안내
                return false; // 불가
            }

            if (instance.EnhanceLevel < EquipmentUpgradeCatalog.MaxEnhanceLevel) // +5 확인
            {
                reason = $"+{EquipmentUpgradeCatalog.MaxEnhanceLevel}까지 강화해야 초월할 수 있습니다."; // 안내
                return false; // 불가
            }

            int requiredBond = UniqueEquipmentCatalog.GetRequiredBondLevel(instance.TranscendStage); // 필요한 결속 단계
            int bondLevel = BondService.GetLevel(saveData, definition.CharacterId); // 현재 결속 단계

            if (bondLevel < requiredBond) // 결속 확인
            {
                reason = $"{definition.DisplayName}은(는) 결속 {requiredBond}단계가 필요합니다 (현재 {bondLevel}단계)."; // 안내
                return false; // 불가
            }

            if (GoldCurrencyService.GetGold(saveData) < EquipmentUpgradeCatalog.GetTranscendGold(instance.TranscendStage)) // 골드 확인
            {
                reason = $"골드가 부족합니다 · {EquipmentUpgradeCatalog.GetTranscendGold(instance.TranscendStage):N0}G 필요"; // 안내
                return false; // 불가
            }

            reason = string.Empty; // 사유 없음
            return true; // 가능
        }

        public static bool TryTranscend(SaveData saveData, EquipmentInstanceSaveData instance, out string message) // 전용 장비 초월 (결속 단계 + 골드, 같은 장비 불필요)
        {
            if (!CanTranscend(saveData, instance, out message)) return false; // 조건 확인
            UniqueEquipmentDefinition definition = UniqueEquipmentCatalog.FindByEquipment(instance.EquipmentId); // 정의 조회
            GoldCurrencyService.TrySpendGold(saveData, EquipmentUpgradeCatalog.GetTranscendGold(instance.TranscendStage), out _); // 골드 소모 (앞에서 잔액 확인)
            instance.SetTranscendStage(instance.TranscendStage + 1); // 초월 단계 상승
            instance.SetEnhanceLevel(0); // 강화 초기화 (일반 장비와 같은 규칙)
            instance.SetFailStreak(0); // 실패 누적 초기화
            message = $"초월 성공! {definition.DisplayName} ★{instance.TranscendStage} · 기본 능력치 ×{EquipmentUpgradeCatalog.GetTranscendMultiplier(instance.TranscendStage):0.0}"; // 안내
            return true; // 성공
        }

        public static bool HasTranscendedUnique(SaveData saveData, CharacterSaveData character, int stage) // 그 동료가 자기 전용 장비를 지정 초월 단계 이상으로 착용 중인지 (룬 5번 칸 조건)
        {
            UniqueEquipmentDefinition definition = character == null ? null : UniqueEquipmentCatalog.FindByCharacter(character.CharacterId); // 정의 조회
            if (saveData == null || definition == null) return false; // 입력 확인
            string instanceId = character.Equipment.GetInstanceId(EquipmentSlot.Weapon); // 무기 칸 장착 장비 (전용 장비는 전부 무기)
            EquipmentInstanceSaveData instance = string.IsNullOrWhiteSpace(instanceId) ? null : saveData.FindEquipmentInstance(instanceId); // 인스턴스 조회
            return instance != null && instance.EquipmentId == definition.EquipmentId && instance.TranscendStage >= stage; // 전용 장비 초월 확인
        }

        public static bool CanCharacterEquip(string characterId, string equipmentId) // 전용 장비 착용 자격 (주인만 착용 가능)
        {
            string ownerId = UniqueEquipmentCatalog.GetOwnerId(equipmentId); // 주인 조회
            return string.IsNullOrEmpty(ownerId) || ownerId == characterId; // 일반 장비는 누구나, 전용 장비는 주인만
        }
    }
}
