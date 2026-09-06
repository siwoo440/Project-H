using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle.SkillBlock; // 스킬 요청 및 게이지 충전량 기능
using ProjectH.Data; // 스킬 데이터 기능
using UnityEditor; // Unity 에디터 에셋 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleUltimateGaugeGainResolverTests // 블록 강화도별 궁극기 충전량 테스트
    {
        [TestCase(1, 10)] // 강화도 1 기대값
        [TestCase(2, 20)] // 강화도 2 기대값
        [TestCase(3, 30)] // 강화도 3 기대값
        public void Resolve_UsesSkillEnhancementUltimateGaugeGain(int enhancementLevel, int expectedGain) // 실제 SkillData 충전량 연결 검증
        {
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/ProjectH/Data/Skills/SK_SERENA_01.asset"); // 세레나 실제 SkillData 로드
            Assert.That(skill, Is.Not.Null); // 테스트 SkillData 존재 검증
            BattleSkillRequest request = new BattleSkillRequest(skill.OwnerCharacterId, skill.Id, skill.SkillSlot, enhancementLevel, enhancementLevel, skill); // 강화도별 스킬 요청 생성

            int gain = BattleUltimateGaugeGainResolver.Resolve(request); // 강화도별 궁극기 충전량 조회

            Assert.That(gain, Is.EqualTo(expectedGain)); // 강화도별 10·20·30 충전량 검증
        }

        [Test] // 테스트 표시
        public void Resolve_InvalidOrMissingSkillReturnsZero() // 잘못된 요청 충전 차단 검증
        {
            BattleSkillRequest request = new BattleSkillRequest("CH_TEST", "SK_TEST", 1, 1, 1, null); // SkillData 없는 요청 생성

            int gain = BattleUltimateGaugeGainResolver.Resolve(request); // 잘못된 요청 충전량 조회

            Assert.That(gain, Is.EqualTo(0)); // 잘못된 요청 충전 없음 검증
        }
    }
}
