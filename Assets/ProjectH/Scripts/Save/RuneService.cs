using System; // 난수 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 장비·아이템 데이터 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class RuneService // 룬 획득·장착·강화·합성·분해 (Day60 신규)
    {
        private static readonly Random SharedRandom = new Random(); // 상자 열기 기본 난수

        public static RuneInstanceSaveData Grant(SaveData saveData, RuneKind kind, int grade) // 룬 지급 (상자·테스트용)
        {
            return saveData == null ? null : saveData.AddRuneInternal(kind, grade); // 새 룬 반환
        }

        public static bool TryOpenBox(SaveData saveData, DataManager dataManager, string itemId, out RuneInstanceSaveData rune, out string message, Random random = null) // 룬 상자 열기 (상자 1개 → 룬 1개 무작위)
        {
            rune = null; // 결과 초기화

            if (!RuneCatalog.IsRuneBox(itemId)) // 상자 확인
            {
                message = "룬 상자가 아닙니다."; // 안내
                return false; // 실패
            }

            if (!ItemInventoryService.TryRemove(saveData, dataManager, itemId, 1, out string error)) // 상자 1개 차감
            {
                message = ItemInventoryService.GetCount(saveData, itemId) <= 0 ? "룬 상자가 없습니다." : error; // 실패 사유
                return false; // 실패
            }

            RuneKind kind = (RuneKind)(random ?? SharedRandom).Next(RuneCatalog.KindCount); // 15종 중 무작위
            rune = Grant(saveData, kind, RuneCatalog.GetBoxGrade(itemId)); // 룬 지급
            message = $"{RuneCatalog.GetStars(rune.Grade)} {RuneCatalog.Get(kind).Name} 획득! {RuneCatalog.Describe(kind, rune.Grade, rune.Level)}"; // 결과 안내
            return true; // 성공
        }

        public static RuneInstanceSaveData[] GetEquipped(SaveData saveData, string characterId) // 캐릭터 슬롯별 장착 룬 (빈 칸 null)
        {
            RuneInstanceSaveData[] slots = new RuneInstanceSaveData[RuneCatalog.SlotCount]; // 슬롯 배열
            if (saveData == null) return slots; // 저장 확인

            foreach (RuneInstanceSaveData rune in saveData.RuneInventory) // 룬 순회
            {
                if (rune.IsEquipped && rune.EquippedCharacterId == characterId) slots[rune.SlotIndex] = rune; // 슬롯 배치
            }

            return slots; // 슬롯 반환
        }

        public static bool IsSlotUnlocked(SaveData saveData, DataManager dataManager, string characterId, int slotIndex) // 슬롯 해금 여부 (기획서 8.12)
        {
            CharacterSaveData character = saveData == null ? null : saveData.FindCharacter(characterId); // 캐릭터 진행 조회

            if (character == null || slotIndex < 0 || slotIndex >= RuneCatalog.SlotCount) // 입력 확인
            {
                return false; // 잠김
            }

            switch (slotIndex) // 슬롯 분기
            {
                case 0: return true; // 기본 슬롯
                case 1: return character.Level >= 10; // 레벨 10
                case 2: return character.BondLevel >= 3; // 결속 3단계 (Day59)
                case 3: return HasHighGradeEquipment(saveData, dataManager, character); // 고급 이상 장비 착용
                default: return HasTranscendedEquipment(saveData, character, 2); // ★2 이상 초월 장비 착용 (Day61 — 임시 결속 5단계 조건 교체, 전용 장비는 Day70)
            }
        }

        private static bool HasHighGradeEquipment(SaveData saveData, DataManager dataManager, CharacterSaveData character) // 고급(Uncommon) 이상 장비 착용 여부
        {
            if (dataManager == null || !dataManager.IsInitialized) // 데이터 확인
            {
                return false; // 판정 불가
            }

            foreach (EquipmentSlot slot in EquipmentSlotInfo.All) // 장비 5칸 순회
            {
                string instanceId = character.Equipment.GetInstanceId(slot); // 장착 장비 인스턴스
                EquipmentInstanceSaveData instance = string.IsNullOrWhiteSpace(instanceId) ? null : saveData.FindEquipmentInstance(instanceId); // 인스턴스 조회
                ItemData item = instance == null ? null : dataManager.GetItem(instance.EquipmentId); // 원본 아이템 조회
                if (item != null && item.Grade >= ItemGrade.Uncommon) return true; // 고급 이상 확인
            }

            return false; // 없음
        }

        private static bool HasTranscendedEquipment(SaveData saveData, CharacterSaveData character, int stage) // 지정 초월 단계 이상 장비 착용 여부 (Day61 추가)
        {
            foreach (EquipmentSlot slot in EquipmentSlotInfo.All) // 장비 5칸 순회
            {
                string instanceId = character.Equipment.GetInstanceId(slot); // 장착 장비 인스턴스
                EquipmentInstanceSaveData instance = string.IsNullOrWhiteSpace(instanceId) ? null : saveData.FindEquipmentInstance(instanceId); // 인스턴스 조회
                if (instance != null && instance.TranscendStage >= stage) return true; // 초월 단계 확인
            }

            return false; // 없음
        }

        public static bool TryEquip(SaveData saveData, DataManager dataManager, string characterId, string runeId, int slotIndex, out string message) // 룬 장착 (다른 캐릭터에 있으면 옮김, 칸이 차 있으면 교체)
        {
            RuneInstanceSaveData rune = saveData == null ? null : saveData.FindRune(runeId); // 룬 조회

            if (rune == null || saveData.FindCharacter(characterId) == null) // 룬·캐릭터 확인
            {
                message = "장착할 룬 또는 캐릭터를 찾을 수 없습니다."; // 안내
                return false; // 실패
            }

            if (!IsSlotUnlocked(saveData, dataManager, characterId, slotIndex)) // 슬롯 해금 확인
            {
                message = $"{slotIndex + 1}번 슬롯이 잠겨 있습니다 · 조건 : {RuneCatalog.GetSlotRequirement(slotIndex)}"; // 안내
                return false; // 실패
            }

            RuneInstanceSaveData[] slots = GetEquipped(saveData, characterId); // 현재 장착 상태

            if (RuneCatalog.Get(rune.Kind).Unique) // 특수 룬 중복 확인 (기획서 '1개만 소지 가능')
            {
                for (int index = 0; index < slots.Length; index++) // 슬롯 순회
                {
                    if (index != slotIndex && slots[index] != null && slots[index] != rune && slots[index].Kind == rune.Kind) // 같은 특수 룬 다른 칸 확인
                    {
                        message = $"{RuneCatalog.Get(rune.Kind).Name}은(는) 캐릭터당 1개만 장착할 수 있습니다."; // 안내
                        return false; // 실패
                    }
                }
            }

            if (slots[slotIndex] != null && slots[slotIndex] != rune) slots[slotIndex].Unequip(); // 기존 룬 해제 (교체)
            rune.Equip(characterId, slotIndex); // 장착
            message = $"{RuneCatalog.Get(rune.Kind).Name} 장착 · {slotIndex + 1}번 슬롯"; // 안내
            return true; // 성공
        }

        public static bool TryUnequip(SaveData saveData, string runeId, out string message) // 룬 해제
        {
            RuneInstanceSaveData rune = saveData == null ? null : saveData.FindRune(runeId); // 룬 조회

            if (rune == null || !rune.IsEquipped) // 장착 확인
            {
                message = "장착 중인 룬이 아닙니다."; // 안내
                return false; // 실패
            }

            rune.Unequip(); // 해제
            message = $"{RuneCatalog.Get(rune.Kind).Name} 해제"; // 안내
            return true; // 성공
        }

        public static bool TryEnhance(SaveData saveData, DataManager dataManager, string runeId, out string message) // 룬 강화 (룬 조각 + 골드, 실패 없음)
        {
            RuneInstanceSaveData rune = saveData == null ? null : saveData.FindRune(runeId); // 룬 조회

            if (rune == null) // 룬 확인
            {
                message = "룬을 찾을 수 없습니다."; // 안내
                return false; // 실패
            }

            if (rune.Level >= RuneCatalog.MaxLevel) // 최대 확인
            {
                message = "이미 최대 강화(Lv.10)입니다."; // 안내
                return false; // 실패
            }

            int shards = RuneCatalog.GetEnhanceShards(rune.Grade, rune.Level); // 필요 조각
            int gold = RuneCatalog.GetEnhanceGold(rune.Grade, rune.Level); // 필요 골드

            if (ItemInventoryService.GetCount(saveData, RuneCatalog.ShardItemId) < shards || GoldCurrencyService.GetGold(saveData) < gold) // 재료 확인
            {
                message = $"재료가 부족합니다 · 룬 조각 {shards} · {gold}G 필요"; // 안내
                return false; // 실패 (상태 변경 없음)
            }

            if (!ItemInventoryService.TryRemove(saveData, dataManager, RuneCatalog.ShardItemId, shards, out string error)) // 조각 차감
            {
                message = error; // 오류
                return false; // 실패
            }

            GoldCurrencyService.TrySpendGold(saveData, gold, out _); // 골드 차감 (앞에서 잔액 확인)
            rune.SetLevel(rune.Level + 1); // 레벨 상승
            message = $"{RuneCatalog.Get(rune.Kind).Name} Lv.{rune.Level} · {RuneCatalog.Describe(rune.Kind, rune.Grade, rune.Level)}"; // 안내
            return true; // 성공
        }

        public static bool TrySynthesize(SaveData saveData, DataManager dataManager, string runeIdA, string runeIdB, out RuneInstanceSaveData result, out string message) // 합성 : 같은 종류·등급 2개 → 상위 등급 1개 (기획서 8.14)
        {
            result = null; // 결과 초기화
            RuneInstanceSaveData a = saveData == null ? null : saveData.FindRune(runeIdA); // 재료 A
            RuneInstanceSaveData b = saveData == null ? null : saveData.FindRune(runeIdB); // 재료 B

            if (a == null || b == null || a == b) // 재료 확인
            {
                message = "서로 다른 룬 2개가 필요합니다."; // 안내
                return false; // 실패
            }

            if (a.Kind != b.Kind || a.Grade != b.Grade) // 종류·등급 확인
            {
                message = "같은 종류·같은 등급 룬끼리만 합성할 수 있습니다."; // 안내
                return false; // 실패
            }

            if (a.Grade >= RuneCatalog.MaxGrade) // 최고 등급 확인
            {
                message = "★3 룬은 더 합성할 수 없습니다."; // 안내
                return false; // 실패
            }

            if (!CanConsume(a, out message) || !CanConsume(b, out message)) // 잠금·장착 확인
            {
                return false; // 실패
            }

            int gold = RuneCatalog.GetSynthesisGold(a.Grade); // 합성 골드

            if (GoldCurrencyService.GetGold(saveData) < gold) // 골드 확인
            {
                message = $"골드가 부족합니다 · {gold}G 필요"; // 안내
                return false; // 실패
            }

            int refund = (RuneCatalog.GetInvestedShards(a.Grade, a.Level) + RuneCatalog.GetInvestedShards(b.Grade, b.Level)) / 2; // 강화 투자 조각 50% 반환
            GoldCurrencyService.TrySpendGold(saveData, gold, out _); // 골드 차감 (앞에서 잔액 확인)
            saveData.RemoveRuneInternal(a.InstanceId); // 재료 A 제거
            saveData.RemoveRuneInternal(b.InstanceId); // 재료 B 제거
            result = Grant(saveData, a.Kind, a.Grade + 1); // 상위 등급 지급
            if (refund > 0) ItemInventoryService.TryAdd(saveData, dataManager, RuneCatalog.ShardItemId, refund, out _); // 조각 반환
            message = $"합성 성공! {RuneCatalog.GetStars(result.Grade)} {RuneCatalog.Get(result.Kind).Name}{(refund > 0 ? $" · 룬 조각 {refund} 반환" : string.Empty)}"; // 안내
            return true; // 성공
        }

        public static RuneInstanceSaveData FindSynthesisPartner(SaveData saveData, RuneInstanceSaveData rune) // 합성 짝 자동 선택 (같은 종류·등급·잠금/장착 아님, 레벨 낮은 순)
        {
            RuneInstanceSaveData best = null; // 후보
            if (saveData == null || rune == null) return null; // 입력 확인

            foreach (RuneInstanceSaveData other in saveData.RuneInventory) // 룬 순회
            {
                if (other == rune || other.Kind != rune.Kind || other.Grade != rune.Grade || other.Locked || other.IsEquipped) continue; // 조건 불일치 제외
                if (best == null || other.Level < best.Level) best = other; // 낮은 레벨 우선
            }

            return best; // 후보 반환
        }

        public static bool TryDismantle(SaveData saveData, DataManager dataManager, string runeId, out string message) // 분해 : 룬 → 룬 조각
        {
            RuneInstanceSaveData rune = saveData == null ? null : saveData.FindRune(runeId); // 룬 조회

            if (rune == null) // 룬 확인
            {
                message = "룬을 찾을 수 없습니다."; // 안내
                return false; // 실패
            }

            if (!CanConsume(rune, out message)) // 잠금·장착 확인
            {
                return false; // 실패
            }

            int shards = RuneCatalog.GetDismantleShards(rune.Grade, rune.Level); // 획득 조각

            if (!ItemInventoryService.TryAdd(saveData, dataManager, RuneCatalog.ShardItemId, shards, out string error)) // 조각 지급 먼저 (실패 시 룬 유지)
            {
                message = error; // 오류
                return false; // 실패
            }

            saveData.RemoveRuneInternal(rune.InstanceId); // 룬 제거
            message = $"{RuneCatalog.Get(rune.Kind).Name} 분해 · 룬 조각 {shards} 획득"; // 안내
            return true; // 성공
        }

        public static bool ToggleLock(SaveData saveData, string runeId) // 잠금 전환 (기획서 '실수 방지')
        {
            RuneInstanceSaveData rune = saveData == null ? null : saveData.FindRune(runeId); // 룬 조회
            if (rune == null) return false; // 룬 확인
            rune.SetLocked(!rune.Locked); // 전환
            return rune.Locked; // 현재 잠금 반환
        }

        public static RuneLoadout BuildLoadout(SaveData saveData, string characterId) // 장착 룬 효과 합계 (전투 스탯·발동 효과용)
        {
            RuneInstanceSaveData[] slots = GetEquipped(saveData, characterId); // 장착 룬
            RuneLoadout loadout = null; // 합계 (없으면 Empty 공유)

            foreach (RuneInstanceSaveData rune in slots) // 슬롯 순회
            {
                if (rune == null) continue; // 빈 칸 제외
                loadout = loadout ?? new RuneLoadout(); // 합계 생성
                loadout.Add(rune.Kind, RuneCatalog.GetValue(rune.Kind, rune.Grade, rune.Level)); // 효과 누적
            }

            return loadout ?? RuneLoadout.Empty; // 합계 반환
        }

        private static bool CanConsume(RuneInstanceSaveData rune, out string message) // 합성·분해 재료 가능 여부
        {
            if (rune.Locked) // 잠금 확인
            {
                message = "잠긴 룬은 합성·분해할 수 없습니다."; // 안내
                return false; // 불가
            }

            if (rune.IsEquipped) // 장착 확인
            {
                message = "장착 중인 룬은 합성·분해할 수 없습니다. 먼저 해제하세요."; // 안내
                return false; // 불가
            }

            message = string.Empty; // 사유 없음
            return true; // 가능
        }
    }
}
