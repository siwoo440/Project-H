using System; // 예외 자료형
using ProjectH.Data; // 캐릭터 원본 데이터 기능
using ProjectH.SaveSystem; // 캐릭터 저장 데이터 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleStatsFactory // 전투 스탯 생성기
    {
        public static BattleStats CreateCharacter(CharacterData characterData, CharacterSaveData saveData, string runtimeId) // 저장 기반 캐릭터 스탯 생성
        {
            return CreateCharacter(characterData, saveData, runtimeId, BattleEquipmentStatBonus.Empty); // 장비 미반영 호환 스탯 생성
        }

        public static BattleStats CreateCharacter(CharacterData characterData, CharacterSaveData saveData, string runtimeId, BattleEquipmentStatBonus equipmentBonus) // 저장 및 장비 기반 캐릭터 스탯 생성
        {
            if (characterData == null) // 캐릭터 원본 확인
            {
                throw new ArgumentNullException(nameof(characterData)); // 원본 누락 예외 발생
            }

            if (saveData == null) // 캐릭터 저장 데이터 확인
            {
                throw new ArgumentNullException(nameof(saveData)); // 저장 데이터 누락 예외 발생
            }

            if (!string.Equals(characterData.Id, saveData.CharacterId, StringComparison.Ordinal)) // 데이터 ID 일치 확인
            {
                throw new ArgumentException($"Character ID mismatch. Data={characterData.Id}, Save={saveData.CharacterId}.", nameof(saveData)); // ID 불일치 예외 발생
            }

            AffinityBattleBonus affinity = AffinityRewardCatalog.GetClaimedBonus(saveData); // 수령 호감도 보상 보너스 (Day56)
            AffinityBattleBonus combined = new AffinityBattleBonus(affinity.HpPercent + BondCatalog.GetHpBonus(saveData.BondLevel), affinity.AttackPercent, affinity.StartUltimateGauge); // 결속 1단계 체력 보너스 합산 (Day59 추가, 결속 0이면 기존과 동일)
            BattleBondRuntimeState.Register(characterData.Id, saveData.BondLevel); // 전투 결속 단계 등록 (Day59 추가, 속성과 같은 등록형)
            return CreateCharacter(characterData, saveData.Level, runtimeId, equipmentBonus, combined); // 저장 레벨·장비·호감도·결속 보너스 기반 스탯 재계산
        }

        public static BattleStats CreateCharacter(CharacterData characterData, int level, string runtimeId) // 레벨 기반 캐릭터 스탯 생성
        {
            return CreateCharacter(characterData, level, runtimeId, BattleEquipmentStatBonus.Empty); // 장비 미반영 호환 스탯 생성
        }

        public static BattleStats CreateCharacter(CharacterData characterData, int level, string runtimeId, BattleEquipmentStatBonus equipmentBonus) // 레벨 및 장비 기반 캐릭터 스탯 생성
        {
            return CreateCharacter(characterData, level, runtimeId, equipmentBonus, AffinityBattleBonus.Empty); // 호감도 보너스 없는 호환 스탯 생성
        }

        public static BattleStats CreateCharacter(CharacterData characterData, int level, string runtimeId, BattleEquipmentStatBonus equipmentBonus, AffinityBattleBonus affinityBonus) // 레벨·장비·호감도 보너스 기반 캐릭터 스탯 생성 (Day56 추가)
        {
            if (characterData == null) // 캐릭터 원본 확인
            {
                throw new ArgumentNullException(nameof(characterData)); // 원본 누락 예외 발생
            }

            BattleEquipmentStatBonus safeBonus = equipmentBonus ?? BattleEquipmentStatBonus.Empty; // 장비 보정값 null 보정
            int safeLevel = BattleGrowthFormula.NormalizeLevel(level); // 성장 적용 레벨 범위 보정
            int grownMaxHp = BattleGrowthFormula.ScaleStat(characterData.BaseHp, safeLevel); // 성장 최대 체력 계산
            int grownAttack = BattleGrowthFormula.ScaleStat(characterData.BaseAttack, safeLevel); // 성장 공격력 계산
            int grownDefense = BattleGrowthFormula.ScaleStat(characterData.BaseDefense, safeLevel); // 성장 방어력 계산
            int grownResistance = BattleGrowthFormula.ScaleStat(characterData.BaseResistance, safeLevel); // 성장 저항력 계산
            int maxHp = Mathf.RoundToInt((grownMaxHp + safeBonus.MaxHp) * (1f + affinityBonus.HpPercent)); // 장비·호감도 보너스 포함 최대 체력 계산 (보너스 없으면 기존과 동일)
            int attack = Mathf.RoundToInt((grownAttack + safeBonus.Attack) * (1f + affinityBonus.AttackPercent)); // 장비·호감도 보너스 포함 공격력 계산 (보너스 없으면 기존과 동일)
            int defense = Mathf.RoundToInt(grownDefense + safeBonus.Defense); // 장비 포함 방어력 계산
            int resistance = Mathf.RoundToInt(grownResistance + safeBonus.Resistance); // 장비 포함 저항력 계산
            float attackSpeed = characterData.AttackSpeed + safeBonus.AttackSpeed; // 장비 포함 공격속도 계산
            float accuracy = characterData.Accuracy + safeBonus.Accuracy; // 장비 포함 명중률 계산
            float criticalRate = characterData.CriticalRate + safeBonus.CriticalRate; // 장비 포함 치명타율 계산
            float attackRange = characterData.AttackRange + safeBonus.AttackRange; // 장비 포함 공격 사거리 계산
            float moveSpeed = characterData.MoveSpeed + safeBonus.MoveSpeed; // 장비 포함 이동속도 계산
            BattleStats stats = new BattleStats(runtimeId, characterData.Id, characterData.DisplayName, characterData.Position, safeLevel, maxHp, attack, defense, attackSpeed, accuracy, criticalRate, attackRange, moveSpeed, resistance); // 최종 런타임 스탯 생성 (생성자가 잔여 상태 정리)
            BattleElementRuntimeState.Register(runtimeId, (BattleElement)characterData.Element); // 캐릭터 전투 속성 Runtime 등록 (Day52 추가, Day55 생성 후 등록으로 순서 변경)
            return stats; // 최종 런타임 스탯 반환
        }
    }
}
