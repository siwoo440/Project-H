using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 Runtime 상태 기능
using ProjectH.Data; // 전투 위치 데이터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillRuntimeStateDay19Tests // 19일차 공격형 Runtime 상태 테스트
    {
        [SetUp] // 테스트 준비 표시
        public void SetUp() // Runtime 상태 초기화
        {
            BattleSkillRuntimeState.ResetAll(); // 테스트 간 Runtime 효과 초기화
        }

        [Test] // 테스트 표시
        public void ResistanceReduction_LowersEffectiveMagicResistance() // 마법 저항 감소 적용 검증
        {
            BattleEnemyStats target = new BattleEnemyStats("ENEMY_0", "MON_TEST", "Enemy", 100, 10, 10, 50, 1f, 1f, 1f); // 저항 50 적군 생성
            BattleSkillRuntimeState.AddModifier(target.RuntimeId, BattleRuntimeModifierKind.ResistanceReductionPercent, 0.20f, 6f, "SK_TEST:RES", 1f); // 저항 20퍼센트 감소 등록

            Assert.That(BattleSkillRuntimeState.GetEffectiveResistance(target, 2f), Is.EqualTo(40)); // 유효 저항 40 검증
        }

        [Test] // 테스트 표시
        public void AccuracyReduction_LowersEffectiveAccuracy() // 명중률 감소 적용 검증
        {
            BattleStats attacker = new BattleStats("ALLY_0", "CH_TEST", "Tester", BattlePosition.Dealer, 1, 100, 10, 5, 1f, 0.90f, 0f); // 명중률 90퍼센트 아군 생성
            BattleSkillRuntimeState.AddModifier(attacker.RuntimeId, BattleRuntimeModifierKind.AccuracyReductionPercent, 0.20f, 6f, "SK_TEST:ACC", 1f); // 명중률 20퍼센트 감소 등록

            Assert.That(BattleSkillRuntimeState.GetEffectiveAccuracy(attacker, 2f), Is.EqualTo(0.72f).Within(0.0001f)); // 유효 명중률 72퍼센트 검증
        }

        [Test] // 테스트 표시
        public void Stun_BlocksActionUntilDurationExpires() // 기절 지속시간 검증
        {
            BattleSkillRuntimeState.AddModifier("ENEMY_0", BattleRuntimeModifierKind.Stun, 1f, 1f, "SK_TEST:STUN", 10f); // 10초 시점 1초 기절 등록

            Assert.That(BattleSkillRuntimeState.IsStunned("ENEMY_0", 10.5f), Is.True); // 기절 지속 중 상태 검증
            Assert.That(BattleSkillRuntimeState.IsStunned("ENEMY_0", 11.01f), Is.False); // 기절 만료 후 상태 검증
        }
    }
}
