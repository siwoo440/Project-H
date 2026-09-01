using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 행동 디버그 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleActionDebugTextTests // 전투 행동 디버그 텍스트 테스트
    {
        [TestCase(BattleActionKind.BasicAttack, "공격!")] // 기본 공격 라벨 사례
        [TestCase(BattleActionKind.Skill, "스킬!")] // 스킬 라벨 사례
        [TestCase(BattleActionKind.Ultimate, "궁극기!")] // 궁극기 라벨 사례
        public void GetLabel_ReturnsKoreanDebugLabel(BattleActionKind kind, string expected) // 행동 라벨 반환 검증
        {
            string label = BattleActionDebugText.GetLabel(kind); // 행동 디버그 라벨 조회
            Assert.That(label, Is.EqualTo(expected)); // 행동 디버그 라벨 검증
        }
    }
}
