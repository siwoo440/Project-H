using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 스킬 Runtime 상태 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillRuntimeStateTests // 스킬 Runtime Modifier 테스트
    {
        [SetUp] // 테스트 준비 표시
        public void SetUp() // Runtime 상태 초기화
        {
            BattleSkillRuntimeState.ResetAll(); // 테스트 간 Runtime 효과 초기화
        }

        [Test] // 테스트 표시
        public void Modifier_ExpiresAtRequestedTime() // 시간 제한 Modifier 만료 검증
        {
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.DefensePercent, 0.30f, 6f, 10f); // 10초 시점 6초 방어 Modifier 추가

            Assert.That(BattleSkillRuntimeState.GetModifierTotal("ALLY_0", BattleRuntimeModifierKind.DefensePercent, 15f), Is.EqualTo(0.30f).Within(0.0001f)); // 만료 전 방어 Modifier 유지 검증
            Assert.That(BattleSkillRuntimeState.GetModifierTotal("ALLY_0", BattleRuntimeModifierKind.DefensePercent, 16.01f), Is.EqualTo(0f).Within(0.0001f)); // 만료 후 방어 Modifier 제거 검증
        }

        [Test] // 테스트 표시
        public void HealingReceivedMultiplier_UsesAdditiveBonus() // 받는 회복량 증가 계산 검증
        {
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.HealingReceivedPercent, 0.15f, 6f, 1f); // 받는 회복량 15퍼센트 Modifier 추가

            Assert.That(BattleSkillRuntimeState.GetHealingReceivedMultiplier("ALLY_0", 2f), Is.EqualTo(1.15f).Within(0.0001f)); // 받는 회복 배율 1.15 검증
        }

        [Test] // 테스트 표시
        public void DamageReduction_IsClampedBelowCompleteImmunity() // 피해 감소 상한 검증
        {
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.DamageReductionPercent, 0.70f, 6f, 1f); // 피해 감소 70퍼센트 Modifier 추가
            BattleSkillRuntimeState.AddModifier("ALLY_0", BattleRuntimeModifierKind.DamageReductionPercent, 0.70f, 6f, 1f); // 피해 감소 70퍼센트 Modifier 추가

            Assert.That(BattleSkillRuntimeState.GetDamageReduction("ALLY_0", 2f), Is.EqualTo(0.90f).Within(0.0001f)); // 피해 감소 최대 90퍼센트 검증
        }

        [Test] // 테스트 표시
        public void Taunt_ExpiresAndReturnsNoForcedTarget() // 도발 Runtime 만료 검증
        {
            BattleSkillRuntimeState.ApplyTaunt("ENEMY_0", "ALLY_1", 4f, 20f); // 20초 시점 4초 도발 적용

            Assert.That(BattleSkillRuntimeState.TryGetTauntTargetRuntimeId("ENEMY_0", 23f, out string activeTarget), Is.True); // 도발 지속 중 강제 타겟 존재 검증
            Assert.That(activeTarget, Is.EqualTo("ALLY_1")); // 도발 강제 타겟 ID 검증
            Assert.That(BattleSkillRuntimeState.TryGetTauntTargetRuntimeId("ENEMY_0", 24.01f, out _), Is.False); // 도발 만료 후 강제 타겟 제거 검증
        }
    }
}
