using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle.SkillBlock; // 스킬 사용 요청 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillRequestTests // 스킬 종류와 강화도 분리 테스트
    {
        [Test] // 테스트 표시
        public void FromChain_PreservesSkillIdentityAndEnhancementSeparately() // 스킬 종류와 강화도 독립 보존 검증
        {
            BattleSkillBlock block = BattleSkillBlock.CreateTest("SK_SERENA_02", "CH_SERENA", 2); // 세레나 Skill 2 블록 생성
            BattleSkillChain chain = new BattleSkillChain(0, 3, "SK_SERENA_02"); // 동일 Skill 2 강화도 3 그룹 생성

            BattleSkillRequest request = BattleSkillRequest.FromChain(block, chain); // 스킬 사용 요청 생성

            Assert.That(request.CharacterId, Is.EqualTo("CH_SERENA")); // 캐릭터 ID 유지 검증
            Assert.That(request.SkillId, Is.EqualTo("SK_SERENA_02")); // Skill 2 ID 유지 검증
            Assert.That(request.SkillSlot, Is.EqualTo(2)); // Skill Slot 2 유지 검증
            Assert.That(request.EnhancementLevel, Is.EqualTo(3)); // 강화도 3 독립 전달 검증
            Assert.That(request.BlockCount, Is.EqualTo(3)); // 소비 블록 수 3 검증
        }
    }
}
