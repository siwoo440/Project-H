using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle.SkillBlock; // 스킬 블록 강화도 판정 기능
using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillChainResolverTests // 동일 SkillId 강화도 판정 테스트
    {
        [Test] // 테스트 표시
        public void ResolveAll_SameSkillIdCreatesEnhancementLevel() // 동일 SkillId 블록 강화도 판정 검증
        {
            List<BattleSkillBlock> blocks = new List<BattleSkillBlock> // 테스트 블록 목록 생성
            {
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 동일 스킬 첫 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 동일 스킬 둘째 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1) // 동일 스킬 셋째 블록
            }; // 테스트 블록 목록 종료

            IReadOnlyList<BattleSkillChain> chains = BattleSkillChainResolver.ResolveAll(blocks); // 전체 강화 그룹 판정

            Assert.That(chains.Count, Is.EqualTo(1)); // 강화 그룹 1개 검증
            Assert.That(chains[0].SkillId, Is.EqualTo("SK_A_01")); // 강화 그룹 SkillId 검증
            Assert.That(chains[0].EnhancementLevel, Is.EqualTo(3)); // 강화도 3 검증
        }

        [Test] // 테스트 표시
        public void ResolveAll_DifferentSkillsOfSameCharacterDoNotMerge() // 같은 캐릭터 다른 스킬 비결합 검증
        {
            List<BattleSkillBlock> blocks = new List<BattleSkillBlock> // 테스트 블록 목록 생성
            {
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // A 캐릭터 Skill 1 블록
                BattleSkillBlock.CreateTest("SK_A_02", "CH_A", 2) // A 캐릭터 Skill 2 블록
            }; // 테스트 블록 목록 종료

            IReadOnlyList<BattleSkillChain> chains = BattleSkillChainResolver.ResolveAll(blocks); // 전체 강화 그룹 판정

            Assert.That(chains.Count, Is.EqualTo(2)); // 서로 다른 스킬 그룹 2개 검증
            Assert.That(chains[0].EnhancementLevel, Is.EqualTo(1)); // Skill 1 강화도 1 검증
            Assert.That(chains[1].EnhancementLevel, Is.EqualTo(1)); // Skill 2 강화도 1 검증
        }

        [Test] // 테스트 표시
        public void ResolveAll_FourSameBlocksSplitIntoThreeAndOne() // 최대 강화도 3 분할 검증
        {
            List<BattleSkillBlock> blocks = new List<BattleSkillBlock> // 테스트 블록 목록 생성
            {
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 첫 동일 스킬 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 둘째 동일 스킬 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 셋째 동일 스킬 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1) // 넷째 동일 스킬 블록
            }; // 테스트 블록 목록 종료

            IReadOnlyList<BattleSkillChain> chains = BattleSkillChainResolver.ResolveAll(blocks); // 전체 강화 그룹 판정

            Assert.That(chains.Count, Is.EqualTo(2)); // 최대 강화도 분할 그룹 수 검증
            Assert.That(chains[0].EnhancementLevel, Is.EqualTo(3)); // 첫 그룹 강화도 3 검증
            Assert.That(chains[1].EnhancementLevel, Is.EqualTo(1)); // 둘째 그룹 강화도 1 검증
        }

        [Test] // 테스트 표시
        public void ResolveAtIndex_ReturnsChunkContainingClickedBlock() // 클릭 블록 소속 강화 그룹 판정 검증
        {
            List<BattleSkillBlock> blocks = new List<BattleSkillBlock> // 테스트 블록 목록 생성
            {
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 첫 동일 스킬 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 둘째 동일 스킬 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 셋째 동일 스킬 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1), // 넷째 동일 스킬 블록
                BattleSkillBlock.CreateTest("SK_A_01", "CH_A", 1) // 다섯째 동일 스킬 블록
            }; // 테스트 블록 목록 종료

            BattleSkillChain chain = BattleSkillChainResolver.ResolveAtIndex(blocks, 4); // 마지막 블록 소속 그룹 판정

            Assert.That(chain.StartIndex, Is.EqualTo(3)); // 둘째 그룹 시작 위치 검증
            Assert.That(chain.Count, Is.EqualTo(2)); // 둘째 그룹 블록 수 검증
            Assert.That(chain.EnhancementLevel, Is.EqualTo(2)); // 둘째 그룹 강화도 2 검증
        }
    }
}
