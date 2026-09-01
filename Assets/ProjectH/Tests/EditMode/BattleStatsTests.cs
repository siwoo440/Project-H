using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 런타임 기능
using ProjectH.Data; // 캐릭터 데이터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleStatsTests // 전투 스탯 테스트
    {
        [Test] // 테스트 표시
        public void TakeDamage_ClampsHealthAtZeroAndMarksDead() // 과잉 피해 처리 검증
        {
            BattleStats stats = CreateStats(); // 테스트 스탯 생성

            int appliedDamage = stats.TakeDamage(5000); // 과잉 피해 적용

            Assert.That(appliedDamage, Is.EqualTo(2200)); // 실제 피해량 검증
            Assert.That(stats.CurrentHp, Is.EqualTo(0)); // 최소 체력 검증
            Assert.That(stats.IsAlive, Is.False); // 사망 상태 검증
        }

        [Test] // 테스트 표시
        public void Heal_ClampsHealthAtMaximum() // 과잉 회복 처리 검증
        {
            BattleStats stats = CreateStats(); // 테스트 스탯 생성
            stats.TakeDamage(1200); // 체력 감소

            int appliedHeal = stats.Heal(5000); // 과잉 회복 적용

            Assert.That(appliedHeal, Is.EqualTo(1200)); // 실제 회복량 검증
            Assert.That(stats.CurrentHp, Is.EqualTo(stats.MaxHp)); // 최대 체력 검증
            Assert.That(stats.IsAlive, Is.True); // 생존 상태 검증
        }

        [Test] // 테스트 표시
        public void NegativeDamageAndHeal_DoNotChangeHealth() // 음수 입력 무시 검증
        {
            BattleStats stats = CreateStats(); // 테스트 스탯 생성

            int damage = stats.TakeDamage(-100); // 음수 피해 적용
            int heal = stats.Heal(-100); // 음수 회복 적용

            Assert.That(damage, Is.EqualTo(0)); // 음수 피해 무시 검증
            Assert.That(heal, Is.EqualTo(0)); // 음수 회복 무시 검증
            Assert.That(stats.CurrentHp, Is.EqualTo(stats.MaxHp)); // 체력 유지 검증
        }

        [Test] // 테스트 표시
        public void RestoreFullHp_RestoresDeadUnit() // 전체 회복 처리 검증
        {
            BattleStats stats = CreateStats(); // 테스트 스탯 생성
            stats.TakeDamage(stats.MaxHp); // 캐릭터 사망 처리

            stats.RestoreFullHp(); // 전체 체력 회복

            Assert.That(stats.CurrentHp, Is.EqualTo(stats.MaxHp)); // 전체 체력 복원 검증
            Assert.That(stats.IsAlive, Is.True); // 생존 복원 검증
        }

        private static BattleStats CreateStats() // 테스트 스탯 생성
        {
            return new BattleStats("ALLY_0", "CH_SERENA", "세레나", BattlePosition.Healer, 1, 2200, 180, 120, 0.90f, 0.98f, 0.05f); // 세레나 테스트 스탯 반환
        }
    }
}
