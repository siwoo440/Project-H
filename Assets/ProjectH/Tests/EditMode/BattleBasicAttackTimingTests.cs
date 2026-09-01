using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 기본 공격 시간 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleBasicAttackTimingTests // 기본 공격 시간 테스트
    {
        [TestCase(1f, 1f)] // 기본 공격속도 사례
        [TestCase(2f, 0.5f)] // 빠른 공격속도 사례
        [TestCase(0.5f, 2f)] // 느린 공격속도 사례
        public void GetInterval_UsesInverseAttackSpeed(float attackSpeed, float expected) // 공격 주기 계산 검증
        {
            float interval = BattleBasicAttackTiming.GetInterval(attackSpeed); // 기본 공격 주기 계산
            Assert.That(interval, Is.EqualTo(expected).Within(0.0001f)); // 기본 공격 주기 검증
        }

        [Test] // 테스트 표시
        public void GetInterval_ClampsInvalidAttackSpeed() // 잘못된 공격속도 보정 검증
        {
            float interval = BattleBasicAttackTiming.GetInterval(0f); // 0 공격속도 주기 계산
            Assert.That(interval, Is.EqualTo(100f).Within(0.0001f)); // 최소 공격속도 보정 검증
        }
    }
}
