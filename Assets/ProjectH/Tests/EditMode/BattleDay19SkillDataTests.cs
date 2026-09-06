using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Data; // 스킬 데이터 기능
using UnityEditor; // Unity 에디터 에셋 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattleDay19SkillDataTests // 릴리아·이브 실제 스킬 데이터 테스트
    {
        [Test] // 테스트 표시
        public void LiliaSkill1_UsesMagicDamageAndPeriodicDamage() // 릴리아 아스트랄 스피어 효과 검증
        {
            SkillData skill = Load("SK_LILIA_01"); // 릴리아 Skill 1 로드
            SkillEffectDefinition[] effects = skill.GetEnhancement(1).Effects; // 강화도 1 효과 목록 조회

            Assert.That(skill.DisplayName, Is.EqualTo("아스트랄 스피어")); // 실제 스킬 이름 검증
            Assert.That(effects.Length, Is.EqualTo(2)); // 직접 피해와 DoT 효과 수 검증
            Assert.That(effects[0].Kind, Is.EqualTo(SkillEffectKind.DamageAttackRatio)); // 직접 피해 Effect 검증
            Assert.That(effects[0].DamageType, Is.EqualTo(SkillDamageType.Magic)); // 직접 마법 피해 타입 검증
            Assert.That(effects[1].Kind, Is.EqualTo(SkillEffectKind.PeriodicDamageAttackRatio)); // DoT Effect 검증
        }

        [Test] // 테스트 표시
        public void EveSkill3_UsesThreeHitsAccuracyDownAndStun() // 이브 일제 사격 효과 검증
        {
            SkillData skill = Load("SK_EVE_03"); // 이브 Skill 3 로드
            SkillEffectDefinition[] effects = skill.GetEnhancement(1).Effects; // 강화도 1 효과 목록 조회

            Assert.That(skill.DisplayName, Is.EqualTo("엘프의 일제 사격")); // 실제 스킬 이름 검증
            Assert.That(effects.Length, Is.EqualTo(3)); // 피해·명중 감소·기절 효과 수 검증
            Assert.That(effects[0].Count, Is.EqualTo(3)); // 3 Hit 피해 검증
            Assert.That(effects[1].Kind, Is.EqualTo(SkillEffectKind.AccuracyReductionPercent)); // 명중 감소 Effect 검증
            Assert.That(effects[2].Kind, Is.EqualTo(SkillEffectKind.Stun)); // 기절 Effect 검증
        }

        private static SkillData Load(string skillId) // SkillId 기반 테스트 에셋 로드
        {
            return AssetDatabase.LoadAssetAtPath<SkillData>($"Assets/ProjectH/Data/Skills/{skillId}.asset"); // SkillData 에셋 반환
        }
    }
}
