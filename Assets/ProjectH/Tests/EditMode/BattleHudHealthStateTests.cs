using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 HUD 상태 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleHudHealthStateTests // HUD 체력 상태 판정 테스트
    {
        [TestCase(100, 100, BattleHudHealthState.Normal)] // 최대 체력 정상 상태 사례
        [TestCase(51, 100, BattleHudHealthState.Normal)] // 절반 초과 정상 상태 사례
        [TestCase(50, 100, BattleHudHealthState.Low)] // 절반 이하 낮은 체력 사례
        [TestCase(26, 100, BattleHudHealthState.Low)] // 25퍼센트 초과 낮은 체력 사례
        [TestCase(25, 100, BattleHudHealthState.Danger)] // 25퍼센트 이하 위험 체력 사례
        [TestCase(1, 100, BattleHudHealthState.Danger)] // 생존 최소 체력 위험 상태 사례
        [TestCase(0, 100, BattleHudHealthState.Down)] // 전투 불능 상태 사례
        public void Evaluate_ReturnsExpectedState(int currentHp, int maxHp, BattleHudHealthState expected) // 체력 상태 구간 검증
        {
            BattleHudHealthState result = BattleHudHealthStateEvaluator.Evaluate(currentHp, maxHp); // HUD 체력 상태 계산
            Assert.That(result, Is.EqualTo(expected)); // HUD 체력 상태 검증
        }

        [Test] // 테스트 표시
        public void GetLabel_ReturnsReadableStateText() // 체력 상태 문구 검증
        {
            Assert.That(BattleHudHealthStateEvaluator.GetLabel(BattleHudHealthState.Normal), Is.EqualTo("HP OK")); // 정상 체력 문구 검증
            Assert.That(BattleHudHealthStateEvaluator.GetLabel(BattleHudHealthState.Low), Is.EqualTo("LOW")); // 낮은 체력 문구 검증
            Assert.That(BattleHudHealthStateEvaluator.GetLabel(BattleHudHealthState.Danger), Is.EqualTo("DANGER")); // 위험 체력 문구 검증
            Assert.That(BattleHudHealthStateEvaluator.GetLabel(BattleHudHealthState.Down), Is.EqualTo("DOWN")); // 전투 불능 문구 검증
        }
    }
}
