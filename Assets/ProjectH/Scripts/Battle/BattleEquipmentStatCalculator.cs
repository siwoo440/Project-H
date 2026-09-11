using ProjectH.Data; // 장비 원본 데이터 기능
using ProjectH.SaveSystem; // 장비 저장 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleEquipmentStatCalculator // 장착 장비 전투 보정 계산기
    {
        public static bool TryCalculate(CharacterSaveData characterSave, SaveData saveData, DataManager dataManager, out BattleEquipmentStatBonus bonus, out string error) // 장착 장비 보정값 계산
        {
            return TryCalculateInternal(characterSave, saveData, dataManager, false, EquipmentSlot.Weapon, string.Empty, out bonus, out error); // 현재 장착 상태 계산 반환
        }

        public static bool TryCalculateWithSlotOverride(CharacterSaveData characterSave, SaveData saveData, DataManager dataManager, EquipmentSlot overrideSlot, string overrideInstanceId, out BattleEquipmentStatBonus bonus, out string error) // 단일 슬롯 가상 교체 보정값 계산
        {
            return TryCalculateInternal(characterSave, saveData, dataManager, true, overrideSlot, overrideInstanceId, out bonus, out error); // 가상 교체 상태 계산 반환
        }

        private static bool TryCalculateInternal(CharacterSaveData characterSave, SaveData saveData, DataManager dataManager, bool hasOverride, EquipmentSlot overrideSlot, string overrideInstanceId, out BattleEquipmentStatBonus bonus, out string error) // 장비 보정 공통 계산
        {
            bonus = BattleEquipmentStatBonus.Empty; // 실패 기본 보정값 설정
            error = string.Empty; // 실패 사유 초기화

            if (characterSave == null) // 캐릭터 저장 데이터 확인
            {
                error = "CharacterSaveData is missing."; // 캐릭터 저장 누락 오류 설정
                return false; // 계산 실패 반환
            }

            if (saveData == null) // 전체 저장 데이터 확인
            {
                error = "SaveData is missing."; // 전체 저장 누락 오류 설정
                return false; // 계산 실패 반환
            }

            if (dataManager == null || !dataManager.IsInitialized) // 데이터 관리자 상태 확인
            {
                error = "DataManager is missing or not initialized."; // 데이터 관리자 오류 설정
                return false; // 계산 실패 반환
            }

            StatAccumulator accumulator = new StatAccumulator(); // 장비 보정 누산기 생성
            foreach (EquipmentSlot slot in EquipmentSlotInfo.All) // 장비 5칸 순회 (Day60 투구·장갑·신발 추가)
            {
                string instanceId = ResolveInstanceId(characterSave, slot, hasOverride, overrideSlot, overrideInstanceId); // 슬롯 계산 인스턴스 결정

                if (!TryAddSlot(characterSave, saveData, dataManager, slot, instanceId, accumulator, out error)) // 슬롯 보정값 계산
                {
                    return false; // 계산 실패 반환
                }
            }

            bonus = accumulator.ToBonus().WithRunes(RuneService.BuildLoadout(saveData, characterSave.CharacterId)); // 최종 장비 보정값 + 장착 룬 합계 (Day60 추가)
            return true; // 계산 성공 반환
        }

        private static string ResolveInstanceId(CharacterSaveData characterSave, EquipmentSlot slot, bool hasOverride, EquipmentSlot overrideSlot, string overrideInstanceId) // 슬롯 계산 인스턴스 결정
        {
            if (hasOverride && slot == overrideSlot) // 가상 교체 대상 슬롯 확인
            {
                return overrideInstanceId ?? string.Empty; // 가상 교체 인스턴스 반환
            }

            return characterSave.Equipment.GetInstanceId(slot); // 실제 장착 인스턴스 반환
        }

        private static bool TryAddSlot(CharacterSaveData characterSave, SaveData saveData, DataManager dataManager, EquipmentSlot slot, string instanceId, StatAccumulator accumulator, out string error) // 단일 슬롯 보정값 누산
        {
            error = string.Empty; // 슬롯 오류 초기화

            if (string.IsNullOrWhiteSpace(instanceId)) // 빈 슬롯 확인
            {
                return true; // 빈 슬롯 계산 성공 반환
            }

            EquipmentInstanceSaveData instance = saveData.FindEquipmentInstance(instanceId); // 장비 인스턴스 조회

            if (instance == null) // 장비 인스턴스 존재 확인
            {
                error = $"Equipped equipment instance not found. Character={characterSave.CharacterId}, Instance={instanceId}."; // 인스턴스 누락 오류 설정
                return false; // 계산 실패 반환
            }

            EquipmentData equipment = dataManager.GetEquipment(instance.EquipmentId); // 장비 원본 데이터 조회

            if (equipment == null) // 장비 원본 존재 확인
            {
                error = $"EquipmentData not found. Equipment={instance.EquipmentId}, Instance={instanceId}."; // 원본 누락 오류 설정
                return false; // 계산 실패 반환
            }

            if (equipment.Slot != slot) // 장착 슬롯 일치 확인
            {
                error = $"Equipment slot mismatch. Equipment={equipment.Id}, Expected={slot}, Actual={equipment.Slot}."; // 슬롯 불일치 오류 설정
                return false; // 계산 실패 반환
            }

            if (equipment.StatOptions == null) // 장비 옵션 목록 확인
            {
                return true; // 옵션 없는 장비 계산 성공 반환
            }

            for (int index = 0; index < equipment.StatOptions.Count; index++) // 장비 옵션 순회
            {
                EquipmentStatOption option = equipment.StatOptions[index]; // 현재 장비 옵션 조회
                accumulator.Add(option); // 장비 옵션 누산
            }

            return true; // 슬롯 계산 성공 반환
        }

        private sealed class StatAccumulator // 장비 옵션 누산기
        {
            private float maxHp; // 최대 체력 누산값
            private float attack; // 공격력 누산값
            private float defense; // 방어력 누산값
            private float resistance; // 저항력 누산값
            private float attackSpeed; // 공격속도 누산값
            private float accuracy; // 명중률 누산값
            private float criticalRate; // 치명타율 누산값
            private float attackRange; // 공격 사거리 누산값
            private float moveSpeed; // 이동속도 누산값

            public void Add(EquipmentStatOption option) // 단일 옵션 누산
            {
                switch (option.StatType) // 옵션 유형 분기
                {
                    case EquipmentStatType.MaxHp: // 최대 체력 옵션 처리
                        maxHp += option.Value; // 최대 체력 누산
                        break; // 최대 체력 처리 종료
                    case EquipmentStatType.Attack: // 공격력 옵션 처리
                        attack += option.Value; // 공격력 누산
                        break; // 공격력 처리 종료
                    case EquipmentStatType.Defense: // 방어력 옵션 처리
                        defense += option.Value; // 방어력 누산
                        break; // 방어력 처리 종료
                    case EquipmentStatType.Resistance: // 저항력 옵션 처리
                        resistance += option.Value; // 저항력 누산
                        break; // 저항력 처리 종료
                    case EquipmentStatType.AttackSpeed: // 공격속도 옵션 처리
                        attackSpeed += option.Value; // 공격속도 누산
                        break; // 공격속도 처리 종료
                    case EquipmentStatType.Accuracy: // 명중률 옵션 처리
                        accuracy += option.Value; // 명중률 누산
                        break; // 명중률 처리 종료
                    case EquipmentStatType.CriticalRate: // 치명타율 옵션 처리
                        criticalRate += option.Value; // 치명타율 누산
                        break; // 치명타율 처리 종료
                    case EquipmentStatType.AttackRange: // 공격 사거리 옵션 처리
                        attackRange += option.Value; // 공격 사거리 누산
                        break; // 공격 사거리 처리 종료
                    case EquipmentStatType.MoveSpeed: // 이동속도 옵션 처리
                        moveSpeed += option.Value; // 이동속도 누산
                        break; // 이동속도 처리 종료
                }
            }

            public BattleEquipmentStatBonus ToBonus() // 누산값 결과 생성
            {
                return new BattleEquipmentStatBonus(maxHp, attack, defense, resistance, attackSpeed, accuracy, criticalRate, attackRange, moveSpeed); // 장비 보정 결과 반환
            }
        }
    }
}
