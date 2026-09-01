using System; // 예외 자료형
using ProjectH.Data; // 캐릭터 원본 데이터 기능
using ProjectH.SaveSystem; // 캐릭터 저장 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleStatsFactory // 전투 스탯 생성기
    {
        public static BattleStats CreateCharacter(CharacterData characterData, CharacterSaveData saveData, string runtimeId) // 저장 기반 캐릭터 스탯 생성
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

            return CreateCharacter(characterData, saveData.Level, runtimeId); // 레벨 기반 스탯 생성
        }

        public static BattleStats CreateCharacter(CharacterData characterData, int level, string runtimeId) // 레벨 기반 캐릭터 스탯 생성
        {
            if (characterData == null) // 캐릭터 원본 확인
            {
                throw new ArgumentNullException(nameof(characterData)); // 원본 누락 예외 발생
            }

            int safeLevel = Math.Max(1, level); // 최소 레벨 보정
            int maxHp = BattleGrowthFormula.ScaleStat(characterData.BaseHp, safeLevel); // 최대 체력 성장 적용
            int attack = BattleGrowthFormula.ScaleStat(characterData.BaseAttack, safeLevel); // 공격력 성장 적용
            int defense = BattleGrowthFormula.ScaleStat(characterData.BaseDefense, safeLevel); // 방어력 성장 적용
            int resistance = BattleGrowthFormula.ScaleStat(characterData.BaseResistance, safeLevel); // 저항력 성장 적용
            return new BattleStats(runtimeId, characterData.Id, characterData.DisplayName, characterData.Position, safeLevel, maxHp, attack, defense, characterData.AttackSpeed, characterData.Accuracy, characterData.CriticalRate, characterData.AttackRange, characterData.MoveSpeed, resistance); // 런타임 스탯 반환
        }
    }
}
