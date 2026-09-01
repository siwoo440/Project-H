using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 피해 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleDamageResolverTests // 피해 계산 테스트
    {
        [Test] // 테스트 표시
        public void ResolveBasicAttack_SubtractsPhysicalDefense() // 기본 공격 방어력 계산 검증
        {
            BattleStats attacker = CreateStats("ALLY_0", 30, 5, 7); // 공격자 전투 스탯 생성
            BattleStats target = CreateStats("ENEMY_0", 10, 12, 9); // 대상 전투 스탯 생성
            BattleDamageResult result = BattleDamageResolver.ResolveBasicAttack(attacker, target); // 기본 공격 피해 계산

            Assert.That(result.Type, Is.EqualTo(BattleDamageType.Physical)); // 물리 피해 종류 검증
            Assert.That(result.RawPower, Is.EqualTo(30)); // 공격 원본 위력 검증
            Assert.That(result.Mitigation, Is.EqualTo(12)); // 물리 방어력 검증
            Assert.That(result.Damage, Is.EqualTo(18)); // 최종 피해량 검증
        }

        [Test] // 테스트 표시
        public void Resolve_MagicDamage_UsesResistance() // 마법 피해 저항력 계산 검증
        {
            BattleStats attacker = CreateStats("ALLY_0", 10, 5, 7); // 공격자 전투 스탯 생성
            BattleStats target = CreateStats("ENEMY_0", 10, 30, 9); // 대상 전투 스탯 생성
            BattleDamageRequest request = new BattleDamageRequest(attacker, target, BattleDamageType.Magic, 25); // 마법 피해 요청 생성
            BattleDamageResult result = BattleDamageResolver.Resolve(request); // 마법 피해 계산

            Assert.That(result.Mitigation, Is.EqualTo(9)); // 마법 저항력 적용 검증
            Assert.That(result.Damage, Is.EqualTo(16)); // 마법 최종 피해량 검증
        }

        [Test] // 테스트 표시
        public void Resolve_TrueDamage_IgnoresDefense() // 고정 피해 방어 무시 검증
        {
            BattleStats attacker = CreateStats("ALLY_0", 10, 5, 7); // 공격자 전투 스탯 생성
            BattleStats target = CreateStats("ENEMY_0", 10, 999, 999); // 높은 방어 대상 생성
            BattleDamageRequest request = new BattleDamageRequest(attacker, target, BattleDamageType.True, 21); // 고정 피해 요청 생성
            BattleDamageResult result = BattleDamageResolver.Resolve(request); // 고정 피해 계산

            Assert.That(result.Mitigation, Is.EqualTo(0)); // 방어 무시 검증
            Assert.That(result.Damage, Is.EqualTo(21)); // 고정 피해량 검증
        }

        [Test] // 테스트 표시
        public void Resolve_PhysicalDamage_GuaranteesOneDamage() // 최소 피해량 검증
        {
            BattleStats attacker = CreateStats("ALLY_0", 1, 5, 7); // 낮은 공격력 스탯 생성
            BattleStats target = CreateStats("ENEMY_0", 10, 999, 999); // 높은 방어 대상 생성
            BattleDamageResult result = BattleDamageResolver.ResolveBasicAttack(attacker, target); // 기본 공격 피해 계산

            Assert.That(result.Damage, Is.EqualTo(1)); // 최소 1 피해 검증
        }

        private static BattleStats CreateStats(string runtimeId, int attack, int defense, int resistance) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, runtimeId, runtimeId, ProjectH.Data.BattlePosition.Dealer, 1, 100, attack, defense, 1f, 1f, 0f, 1.6f, 2f, resistance); // 테스트 전투 스탯 반환
        }
    }
}
