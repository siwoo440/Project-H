using ProjectH.Data; // 스킬 데이터 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    public sealed class BattleSkillBlock // 전투 중 단일 스킬 블록 데이터
    {
        public string RuntimeId { get; } // 블록 런타임 ID
        public SkillData Skill { get; } // 연결된 스킬 원본 데이터
        public string SkillId => Skill == null ? testSkillId : Skill.Id; // 블록 스킬 ID 반환
        public string CharacterId => Skill == null ? testCharacterId : Skill.OwnerCharacterId; // 블록 캐릭터 ID 반환
        public int SkillSlot => Skill == null ? testSkillSlot : Skill.SkillSlot; // 블록 스킬 슬롯 반환
        private readonly string testSkillId; // EditMode 순수 테스트용 스킬 ID
        private readonly string testCharacterId; // EditMode 순수 테스트용 캐릭터 ID
        private readonly int testSkillSlot; // EditMode 순수 테스트용 스킬 슬롯

        public BattleSkillBlock(string runtimeId, SkillData skill) // 실제 스킬 데이터 기반 블록 생성
        {
            RuntimeId = runtimeId ?? string.Empty; // 블록 런타임 ID 저장
            Skill = skill; // 스킬 데이터 저장
            testSkillId = string.Empty; // 테스트 스킬 ID 초기화
            testCharacterId = string.Empty; // 테스트 캐릭터 ID 초기화
            testSkillSlot = 0; // 테스트 스킬 슬롯 초기화
        }

        private BattleSkillBlock(string runtimeId, string skillId, string characterId, int skillSlot) // 테스트용 블록 생성
        {
            RuntimeId = runtimeId ?? string.Empty; // 테스트 블록 런타임 ID 저장
            Skill = null; // 테스트 블록 실제 SkillData 미사용
            testSkillId = skillId ?? string.Empty; // 테스트 스킬 ID 저장
            testCharacterId = characterId ?? string.Empty; // 테스트 캐릭터 ID 저장
            testSkillSlot = skillSlot; // 테스트 스킬 슬롯 저장
        }

        public static BattleSkillBlock CreateTest(string skillId, string characterId, int skillSlot) // EditMode 순수 테스트 블록 생성
        {
            return new BattleSkillBlock($"TEST_{skillId}_{skillSlot}", skillId, characterId, skillSlot); // 테스트 블록 반환
        }
    }
}
