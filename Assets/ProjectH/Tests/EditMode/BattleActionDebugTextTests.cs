using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 행동 디버그 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleActionDebugTextTests // 전투 행동 디버그 텍스트 테스트
    {
        [TestCase(BattleActionKind.BasicAttack, "공격!")] // 기본 공격 라벨 사례
        [TestCase(BattleActionKind.Skill, "스킬!")] // 스킬 기본 라벨 사례
        [TestCase(BattleActionKind.Ultimate, "궁극기!")] // 궁극기 라벨 사례
        public void GetLabel_ReturnsKoreanDebugLabel(BattleActionKind kind, string expected) // 행동 라벨 반환 검증
        {
            string label = BattleActionDebugText.GetLabel(kind); // 행동 디버그 라벨 조회
            Assert.That(label, Is.EqualTo(expected)); // 행동 디버그 라벨 검증
        }

        [Test] // 테스트 표시
        public void GetLabel_SkillUsesDisplayNameWhenProvided() // 실제 스킬명 우선 표시 검증
        {
            string label = BattleActionDebugText.GetLabel(BattleActionKind.Skill, "아스트랄 스피어"); // 실제 스킬명 라벨 조회
            Assert.That(label, Is.EqualTo("아스트랄 스피어")); // 실제 스킬명 표시 검증
        }

        [Test] // 테스트 표시
        public void GetLabel_EmptySkillNameFallsBackToDefaultSkillLabel() // 빈 스킬명 기본 라벨 대체 검증
        {
            string label = BattleActionDebugText.GetLabel(BattleActionKind.Skill, string.Empty); // 빈 스킬명 라벨 조회
            Assert.That(label, Is.EqualTo("스킬!")); // 기본 스킬 라벨 대체 검증
        }

        [Test] // 테스트 표시
        public void GetLabel_CustomNameDoesNotOverrideBasicAttack() // 기본 공격 라벨 보호 검증
        {
            string label = BattleActionDebugText.GetLabel(BattleActionKind.BasicAttack, "임의 이름"); // 기본 공격에 임의 이름 전달
            Assert.That(label, Is.EqualTo("공격!")); // 기본 공격 고정 라벨 유지 검증
        }
    }
}
