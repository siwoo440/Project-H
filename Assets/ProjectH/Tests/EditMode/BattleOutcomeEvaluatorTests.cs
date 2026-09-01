using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 승패 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleOutcomeEvaluatorTests // 전투 승패 판정 테스트
    {
        [Test] // 테스트 표시
        public void Evaluate_WhenBothTeamsAlive_ReturnsRunning() // 양팀 생존 전투 진행 검증
        {
            BattleOutcome outcome = BattleOutcomeEvaluator.Evaluate(4, 3); // 양팀 생존 승패 판정
            Assert.That(outcome, Is.EqualTo(BattleOutcome.Running)); // 전투 진행 상태 검증
        }

        [Test] // 테스트 표시
        public void Evaluate_WhenEnemiesAreZero_ReturnsVictory() // 적 전멸 승리 검증
        {
            BattleOutcome outcome = BattleOutcomeEvaluator.Evaluate(2, 0); // 적 전멸 승패 판정
            Assert.That(outcome, Is.EqualTo(BattleOutcome.Victory)); // 승리 상태 검증
        }

        [Test] // 테스트 표시
        public void Evaluate_WhenAlliesAreZero_ReturnsDefeat() // 아군 전멸 패배 검증
        {
            BattleOutcome outcome = BattleOutcomeEvaluator.Evaluate(0, 2); // 아군 전멸 승패 판정
            Assert.That(outcome, Is.EqualTo(BattleOutcome.Defeat)); // 패배 상태 검증
        }

        [Test] // 테스트 표시
        public void Evaluate_WhenBothTeamsAreZero_PrioritizesDefeat() // 동시 전멸 안전 판정 검증
        {
            BattleOutcome outcome = BattleOutcomeEvaluator.Evaluate(0, 0); // 동시 전멸 승패 판정
            Assert.That(outcome, Is.EqualTo(BattleOutcome.Defeat)); // 파티 전멸 우선 판정 검증
        }
    }
}
