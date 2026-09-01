using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 속도 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSpeedRulesTests // 전투 속도 규칙 테스트
    {
        [Test] // 테스트 표시
        public void GetNext_CyclesOneToOnePointFiveToTwo() // 전투 속도 순환 검증
        {
            Assert.That(BattleSpeedRules.GetNext(1f), Is.EqualTo(1.5f)); // 1배속 다음 1.5배속 검증
            Assert.That(BattleSpeedRules.GetNext(1.5f), Is.EqualTo(2f)); // 1.5배속 다음 2배속 검증
            Assert.That(BattleSpeedRules.GetNext(2f), Is.EqualTo(1f)); // 2배속 다음 1배속 검증
        }

        [TestCase(1f, "×1")] // 1배속 문구 사례
        [TestCase(1.5f, "×1.5")] // 1.5배속 문구 사례
        [TestCase(2f, "×2")] // 2배속 문구 사례
        public void GetLabel_ReturnsExpectedText(float speed, string expected) // 전투 속도 문구 검증
        {
            Assert.That(BattleSpeedRules.GetLabel(speed), Is.EqualTo(expected)); // 전투 속도 문구 검증
        }

        [TestCase(0f, 1f)] // 비정상 0배속 보정 사례
        [TestCase(-1f, 1f)] // 비정상 음수 배속 보정 사례
        [TestCase(1.4f, 1.5f)] // 근접 1.5배속 보정 사례
        [TestCase(1.9f, 2f)] // 근접 2배속 보정 사례
        public void Normalize_ClampsToSupportedSpeed(float input, float expected) // 지원 배속 보정 검증
        {
            Assert.That(BattleSpeedRules.Normalize(input), Is.EqualTo(expected)); // 지원 배속 보정 결과 검증
        }
    }
}
