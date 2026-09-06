using ProjectH.Data; // 스킬 데이터 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    public readonly struct BattleSkillRequest // 블록 소비 기반 스킬 사용 요청
    {
        public string CharacterId { get; } // 스킬 소유 캐릭터 ID
        public string SkillId { get; } // 사용 스킬 ID
        public int SkillSlot { get; } // 스킬 종류 슬롯 번호
        public int EnhancementLevel { get; } // 스킬 강화도
        public int BlockCount { get; } // 소비 블록 수
        public SkillData Skill { get; } // 실제 스킬 데이터 참조
        public bool IsValid => !string.IsNullOrWhiteSpace(CharacterId) && !string.IsNullOrWhiteSpace(SkillId) && SkillSlot >= 1 && SkillSlot <= 3 && EnhancementLevel >= 1 && EnhancementLevel <= SkillData.MaxEnhancementLevel; // 스킬 요청 유효성 반환

        public BattleSkillRequest(string characterId, string skillId, int skillSlot, int enhancementLevel, int blockCount, SkillData skill) // 스킬 사용 요청 생성
        {
            CharacterId = characterId ?? string.Empty; // 캐릭터 ID 저장
            SkillId = skillId ?? string.Empty; // 스킬 ID 저장
            SkillSlot = skillSlot; // 스킬 슬롯 저장
            EnhancementLevel = enhancementLevel; // 강화도 저장
            BlockCount = blockCount; // 소비 블록 수 저장
            Skill = skill; // 스킬 데이터 저장
        }

        public static BattleSkillRequest FromChain(BattleSkillBlock block, BattleSkillChain chain) // 블록과 강화 그룹에서 사용 요청 생성
        {
            if (block == null || !chain.IsValid) // 블록 및 강화 그룹 유효성 확인
            {
                return default; // 빈 스킬 요청 반환
            }

            return new BattleSkillRequest(block.CharacterId, block.SkillId, block.SkillSlot, chain.EnhancementLevel, chain.Count, block.Skill); // 스킬 종류와 강화도 분리 요청 반환
        }
    }
}
