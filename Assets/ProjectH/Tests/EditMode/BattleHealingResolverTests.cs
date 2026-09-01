using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 회복 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleHealingResolverTests // 회복 계산 테스트
    {
        [Test] // 테스트 표시
        public void Resolve_CapsHealingAtMissingHp() // 최대 체력 초과 회복 방지 검증
        {
            BattleStats target = CreateStats(); // 회복 대상 생성
            target.TakeDamage(30); // 체력 30 감소
            BattleHealingResult result = BattleHealingResolver.Resolve(target, 50); // 50 회복 계산

            Assert.That(result.RequestedAmount, Is.EqualTo(50)); // 요청 회복량 검증
            Assert.That(result.Healing, Is.EqualTo(30)); // 실제 가능 회복량 검증
        }

        [Test] // 테스트 표시
        public void Resolve_DeadTarget_DoesNotAllowNormalHealing() // 일반 회복 부활 방지 검증
        {
            BattleStats target = CreateStats(); // 회복 대상 생성
            target.TakeDamage(999); // 대상 전투 불능 처리
            BattleHealingResult result = BattleHealingResolver.Resolve(target, 50); // 전투 불능 대상 회복 계산

            Assert.That(target.IsAlive, Is.False); // 전투 불능 상태 검증
            Assert.That(result.Healing, Is.EqualTo(0)); // 일반 회복 차단 검증
        }

        private static BattleStats CreateStats() // 테스트 전투 스탯 생성
        {
            return new BattleStats("ALLY_0", "CH_TEST", "TEST", ProjectH.Data.BattlePosition.Healer, 1, 100, 10, 5, 1f, 1f, 0f); // 테스트 전투 스탯 반환
        }
    }
}
