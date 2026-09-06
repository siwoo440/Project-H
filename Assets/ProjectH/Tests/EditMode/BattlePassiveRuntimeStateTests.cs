using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 패시브 Runtime 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattlePassiveRuntimeStateTests // 패시브 Runtime 상태 테스트
    {
        [SetUp] // 테스트 초기화
        public void SetUp() // 패시브 상태 초기화
        {
            BattlePassiveRuntimeState.ResetAll(); // 패시브 Runtime 전체 초기화
        }

        [Test] // 테스트 표시
        public void Shield_AbsorbsDamageBeforeHp() // 보호막 우선 흡수 검증
        {
            BattlePassiveRuntimeState.GrantShield("ALLY_0", 6); // 테스트 보호막 부여
            int remaining = BattlePassiveRuntimeState.AbsorbShield("ALLY_0", 10, out int absorbed); // 피해 보호막 흡수

            Assert.That(absorbed, Is.EqualTo(6)); // 보호막 흡수량 검증
            Assert.That(remaining, Is.EqualTo(4)); // 잔여 피해량 검증
            Assert.That(BattlePassiveRuntimeState.GetShield("ALLY_0"), Is.EqualTo(0)); // 보호막 소진 검증
        }

        [Test] // 테스트 표시
        public void Cooldown_BlocksUntilExpiry() // 패시브 쿨다운 검증
        {
            BattlePassiveRuntimeState.StartCooldown("PASSIVE_TEST", 12f, 10f); // 테스트 쿨다운 시작

            Assert.That(BattlePassiveRuntimeState.IsCooldownReady("PASSIVE_TEST", 21.9f), Is.False); // 만료 전 사용 차단 검증
            Assert.That(BattlePassiveRuntimeState.IsCooldownReady("PASSIVE_TEST", 22f), Is.True); // 만료 시 사용 가능 검증
        }

        [Test] // 테스트 표시
        public void CriticalBonus_ExpiresAtDurationEnd() // 치명타 증가 지속시간 검증
        {
            BattlePassiveRuntimeState.SetCriticalChanceBonus("ALLY_0", 0.04f, 10f, 5f); // 치명타 증가 적용

            Assert.That(BattlePassiveRuntimeState.GetCriticalChanceBonus("ALLY_0", 14.9f), Is.EqualTo(0.04f).Within(0.0001f)); // 지속 중 치명타 증가 검증
            Assert.That(BattlePassiveRuntimeState.GetCriticalChanceBonus("ALLY_0", 15f), Is.EqualTo(0f).Within(0.0001f)); // 만료 후 치명타 증가 제거 검증
        }

        [Test] // 테스트 표시
        public void DefenseReduction_ExpiresAtDurationEnd() // 방어 감소 지속시간 검증
        {
            BattlePassiveRuntimeState.SetDefenseReduction("ENEMY_0", 0.05f, 6f, 3f); // 방어 감소 적용

            Assert.That(BattlePassiveRuntimeState.GetDefenseReduction("ENEMY_0", 8.9f), Is.EqualTo(0.05f).Within(0.0001f)); // 지속 중 방어 감소 검증
            Assert.That(BattlePassiveRuntimeState.GetDefenseReduction("ENEMY_0", 9f), Is.EqualTo(0f).Within(0.0001f)); // 만료 후 방어 감소 제거 검증
        }

        [Test] // 테스트 표시
        public void DefenseReduction_IsUsedByPhysicalDamageResolver() // 방어 감소 피해 계산 연결 검증
        {
            BattleStats attacker = new BattleStats("ALLY_0", "CH_LILIA", "Lilia", ProjectH.Data.BattlePosition.Dealer, 1, 100, 200, 0, 1f, 1f, 0f); // 공격자 테스트 스탯 생성
            BattleStats target = new BattleStats("ENEMY_0", "MON_TEST", "Enemy", ProjectH.Data.BattlePosition.Dealer, 1, 100, 10, 100, 1f, 1f, 0f); // 대상 테스트 스탯 생성
            BattlePassiveRuntimeState.SetDefenseReduction(target.RuntimeId, 0.05f, 6f, 0f); // 방어력 5퍼센트 감소 적용
            BattleDamageResult result = BattleDamageResolver.Resolve(new BattleDamageRequest(attacker, target, BattleDamageType.Physical, 200)); // 물리 피해 계산

            Assert.That(result.Mitigation, Is.EqualTo(95)); // 감소된 방어력 95 적용 검증
            Assert.That(result.Damage, Is.EqualTo(105)); // 최종 피해량 검증
        }
    }
}
