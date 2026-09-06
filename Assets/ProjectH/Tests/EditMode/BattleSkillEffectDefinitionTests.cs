using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 스킬 효과 데이터 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleSkillEffectDefinitionTests // 데이터 기반 스킬 효과 정의 테스트
    {
        [Test] // 테스트 표시
        public void Constructor_PreservesEditableEffectValues() // 스킬 효과 설정값 보존 검증
        {
            SkillEffectDefinition effect = new SkillEffectDefinition(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.18f, 0f, 2, false); // 전체 회복 및 정화 설정 예시 생성

            Assert.That(effect.Kind, Is.EqualTo(SkillEffectKind.HealMaxHpPercent)); // 효과 종류 보존 검증
            Assert.That(effect.TargetType, Is.EqualTo(SkillTargetType.AllAllies)); // 효과 대상 보존 검증
            Assert.That(effect.Value, Is.EqualTo(0.18f).Within(0.0001f)); // 효과 수치 보존 검증
            Assert.That(effect.Count, Is.EqualTo(2)); // 효과 개수 보존 검증
        }

        [Test] // 테스트 표시
        public void Constructor_ClampsNegativeDurationAndCount() // 잘못된 데이터 보정 검증
        {
            SkillEffectDefinition effect = new SkillEffectDefinition(SkillEffectKind.Taunt, SkillTargetType.AllEnemies, 1f, -2f, -1, false); // 음수 지속시간과 개수 효과 생성

            Assert.That(effect.Duration, Is.EqualTo(0f)); // 음수 지속시간 0 보정 검증
            Assert.That(effect.Count, Is.EqualTo(0)); // 음수 개수 0 보정 검증
        }
    }
}
