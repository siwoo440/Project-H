using System.Reflection; // 비공개 데이터 테스트 설정 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 패시브 시스템 기능
using ProjectH.Battle.SkillBlock; // 스킬 요청 기능
using ProjectH.Data; // 스킬 데이터 기능
using UnityEngine; // ScriptableObject 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class BattlePassiveAreaSkillTests // 릴리아 광역 스킬 판정 테스트
    {
        [Test] // 테스트 표시
        public void IsAreaSkill_ReturnsTrue_WhenEnhancementContainsAllEnemiesEffect() // 실제 효과 기반 광역 판정 검증
        {
            SkillData skill = ScriptableObject.CreateInstance<SkillData>(); // 테스트 SkillData 생성
            SetField(skill, "id", "SK_LILIA_TEST"); // 테스트 스킬 ID 설정
            SetField(skill, "ownerCharacterId", "CH_LILIA"); // 릴리아 소유자 ID 설정
            SetField(skill, "skillSlot", 1); // 테스트 스킬 슬롯 설정
            SetField(skill, "targetType", SkillTargetType.NearestEnemy); // 대표 대상 단일 적 설정
            SkillEffectDefinition areaEffect = new SkillEffectDefinition(SkillEffectKind.DamageAttackRatio, SkillTargetType.AllEnemies, SkillDamageType.Magic, 1f, 0f, 1, 1f, 1f, 0f, false, false); // 전체 적 피해 효과 생성
            SkillEnhancementData enhancement = new SkillEnhancementData(); // 테스트 강화도 데이터 생성
            SetField(enhancement, "effects", new[] { areaEffect }); // 광역 실제 효과 연결
            SetField(skill, "enhancements", new[] { enhancement }); // 테스트 강화도 배열 연결
            BattleSkillRequest request = new BattleSkillRequest("CH_LILIA", "SK_LILIA_TEST", 1, 1, 1, skill); // 릴리아 스킬 요청 생성

            Assert.That(BattlePassiveSystem.IsAreaSkill(request), Is.True); // 실제 효과 기반 광역 판정 검증
            Object.DestroyImmediate(skill); // 테스트 SkillData 제거
        }

        private static void SetField(object target, string fieldName, object value) // 비공개 필드 테스트 값 설정
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 비공개 필드 정보 조회
            Assert.That(field, Is.Not.Null, $"{fieldName} 필드를 찾을 수 없습니다."); // 테스트 필드 존재 검증
            field.SetValue(target, value); // 테스트 필드 값 적용
        }
    }
}
