using ProjectH.Data; // 스킬 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day18Setup // 18일차 세레나·엘렌 실제 스킬 데이터 설정 도구
    {
        private const string SkillFolder = "Assets/ProjectH/Data/Skills"; // SkillData 저장 폴더

        private readonly struct EffectSpec // Editor 전용 스킬 효과 설정값
        {
            public SkillEffectKind Kind { get; } // 효과 종류
            public SkillTargetType TargetType { get; } // 효과 대상
            public float Value { get; } // 효과 수치
            public float Duration { get; } // 효과 지속시간
            public int Count { get; } // 효과 개수
            public bool RequirePreviousSuccess { get; } // 앞선 효과 성공 조건

            public EffectSpec(SkillEffectKind kind, SkillTargetType targetType, float value, float duration = 0f, int count = 0, bool requirePreviousSuccess = false) // 효과 설정 생성
            {
                Kind = kind; // 효과 종류 저장
                TargetType = targetType; // 효과 대상 저장
                Value = value; // 효과 수치 저장
                Duration = duration; // 지속시간 저장
                Count = count; // 효과 개수 저장
                RequirePreviousSuccess = requirePreviousSuccess; // 앞선 효과 성공 조건 저장
            }
        }

        [MenuItem("Tools/Project H/Phase 1/18일차 세레나-엘렌 스킬 설정 실행")] // 18일차 스킬 설정 메뉴 등록
        public static void Setup() // 세레나·엘렌 SkillData 실제 효과 설정
        {
            ConfigureSerenaSkill1(); // 세레나 정화의 빛 설정
            ConfigureSerenaSkill2(); // 세레나 치유 기도 설정
            ConfigureSerenaSkill3(); // 세레나 성유의 가호 설정
            ConfigureEllenSkill1(); // 엘렌 방어 태세 설정
            ConfigureEllenSkill2(); // 엘렌 도발 설정
            ConfigureEllenSkill3(); // 엘렌 철의 맹세 설정
            AssetDatabase.SaveAssets(); // SkillData 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][DAY18] Serena/Ellen six skills configured with editable enhancement effects."); // 18일차 스킬 설정 완료 로그
        }

        private static void ConfigureSerenaSkill1() // 세레나 Skill 1 정화의 빛 설정
        {
            SkillData skill = LoadSkill("SK_SERENA_01"); // 세레나 Skill 1 로드
            SerializedObject serialized = BeginSkill(skill, "정화의 빛", "생존 아군 전체를 회복하고 제거 가능한 약화 효과를 정화한다.", SkillTargetType.AllAllies, SkillEffectType.Heal); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.10f), // 강화도 1 전체 10퍼센트 회복
                new EffectSpec(SkillEffectKind.CleanseDebuff, SkillTargetType.AllAllies, 0f, 0f, 1)); // 강화도 1 약화 1개 제거
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.14f), // 강화도 2 전체 14퍼센트 회복
                new EffectSpec(SkillEffectKind.CleanseDebuff, SkillTargetType.AllAllies, 0f, 0f, 1)); // 강화도 2 약화 1개 제거
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.18f), // 강화도 3 전체 18퍼센트 회복
                new EffectSpec(SkillEffectKind.CleanseDebuff, SkillTargetType.AllAllies, 0f, 0f, 2)); // 강화도 3 약화 최대 2개 제거
            EndSkill(serialized, skill); // 세레나 Skill 1 변경 저장
        }

        private static void ConfigureSerenaSkill2() // 세레나 Skill 2 치유 기도 설정
        {
            SkillData skill = LoadSkill("SK_SERENA_02"); // 세레나 Skill 2 로드
            SerializedObject serialized = BeginSkill(skill, "치유 기도", "현재 HP 비율이 가장 낮은 생존 아군 한 명을 크게 회복하고 일정 시간 방어력을 증가시킨다.", SkillTargetType.LowestHpAlly, SkillEffectType.Heal); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.LowestHpAlly, 0.18f), // 강화도 1 단일 18퍼센트 회복
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.LowestHpAlly, 0.10f, 5f)); // 강화도 1 방어 10퍼센트 증가
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.LowestHpAlly, 0.24f), // 강화도 2 단일 24퍼센트 회복
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.LowestHpAlly, 0.15f, 5f)); // 강화도 2 방어 15퍼센트 증가
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.LowestHpAlly, 0.30f), // 강화도 3 단일 30퍼센트 회복
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.LowestHpAlly, 0.20f, 5f)); // 강화도 3 방어 20퍼센트 증가
            EndSkill(serialized, skill); // 세레나 Skill 2 변경 저장
        }

        private static void ConfigureSerenaSkill3() // 세레나 Skill 3 성유의 가호 설정
        {
            SkillData skill = LoadSkill("SK_SERENA_03"); // 세레나 Skill 3 로드
            SerializedObject serialized = BeginSkill(skill, "성유의 가호", "생존 아군 전체를 회복하고 받는 회복량을 증가시키며 제거 가능한 약화 효과를 정화한다.", SkillTargetType.AllAllies, SkillEffectType.Heal); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.12f), // 강화도 1 전체 12퍼센트 회복
                new EffectSpec(SkillEffectKind.HealingReceivedPercent, SkillTargetType.AllAllies, 0.08f, 6f), // 강화도 1 받는 회복 8퍼센트 증가
                new EffectSpec(SkillEffectKind.CleanseDebuff, SkillTargetType.AllAllies, 0f, 0f, 1)); // 강화도 1 약화 1개 제거
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.16f), // 강화도 2 전체 16퍼센트 회복
                new EffectSpec(SkillEffectKind.HealingReceivedPercent, SkillTargetType.AllAllies, 0.12f, 6f), // 강화도 2 받는 회복 12퍼센트 증가
                new EffectSpec(SkillEffectKind.CleanseDebuff, SkillTargetType.AllAllies, 0f, 0f, 1)); // 강화도 2 약화 1개 제거
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 기본값 설정
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.20f), // 강화도 3 전체 20퍼센트 회복
                new EffectSpec(SkillEffectKind.HealingReceivedPercent, SkillTargetType.AllAllies, 0.15f, 6f), // 강화도 3 받는 회복 15퍼센트 증가
                new EffectSpec(SkillEffectKind.CleanseDebuff, SkillTargetType.AllAllies, 0f, 0f, 2)); // 강화도 3 약화 최대 2개 제거
            EndSkill(serialized, skill); // 세레나 Skill 3 변경 저장
        }

        private static void ConfigureEllenSkill1() // 엘렌 Skill 1 방어 태세 설정
        {
            SkillData skill = LoadSkill("SK_ELLEN_01"); // 엘렌 Skill 1 로드
            SerializedObject serialized = BeginSkill(skill, "방어 태세", "자신의 방어력과 최종 피해 감소율을 일정 시간 증가시킨다.", SkillTargetType.Self, SkillEffectType.Buff); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 기본값 설정
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.20f, 6f), // 강화도 1 방어 20퍼센트 증가
                new EffectSpec(SkillEffectKind.DamageReductionPercent, SkillTargetType.Self, 0.10f, 6f)); // 강화도 1 피해 10퍼센트 감소
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 기본값 설정
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.30f, 6f), // 강화도 2 방어 30퍼센트 증가
                new EffectSpec(SkillEffectKind.DamageReductionPercent, SkillTargetType.Self, 0.15f, 6f)); // 강화도 2 피해 15퍼센트 감소
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 기본값 설정
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.40f, 6f), // 강화도 3 방어 40퍼센트 증가
                new EffectSpec(SkillEffectKind.DamageReductionPercent, SkillTargetType.Self, 0.20f, 6f)); // 강화도 3 피해 20퍼센트 감소
            EndSkill(serialized, skill); // 엘렌 Skill 1 변경 저장
        }

        private static void ConfigureEllenSkill2() // 엘렌 Skill 2 도발 설정
        {
            SkillData skill = LoadSkill("SK_ELLEN_02"); // 엘렌 Skill 2 로드
            SerializedObject serialized = BeginSkill(skill, "도발", "생존 적 전체의 공격 목표를 엘렌 쪽으로 유도하고 자신에게 방어 증가와 조건부 회복을 부여한다.", SkillTargetType.AllEnemies, SkillEffectType.Debuff); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 기본값 설정
                new EffectSpec(SkillEffectKind.Taunt, SkillTargetType.AllEnemies, 1f, 2.5f), // 강화도 1 도발 2.5초
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.Self, 0.03f, 0f, 0, true), // 강화도 1 도발 성공 시 자기 3퍼센트 회복
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.10f, 2.5f)); // 강화도 1 자기 방어 10퍼센트 증가
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 기본값 설정
                new EffectSpec(SkillEffectKind.Taunt, SkillTargetType.AllEnemies, 1f, 3.5f), // 강화도 2 도발 3.5초
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.Self, 0.05f, 0f, 0, true), // 강화도 2 도발 성공 시 자기 5퍼센트 회복
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.15f, 3.5f)); // 강화도 2 자기 방어 15퍼센트 증가
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 기본값 설정
                new EffectSpec(SkillEffectKind.Taunt, SkillTargetType.AllEnemies, 1f, 4.5f), // 강화도 3 도발 4.5초
                new EffectSpec(SkillEffectKind.HealMaxHpPercent, SkillTargetType.Self, 0.07f, 0f, 0, true), // 강화도 3 도발 성공 시 자기 7퍼센트 회복
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.20f, 4.5f)); // 강화도 3 자기 방어 20퍼센트 증가
            EndSkill(serialized, skill); // 엘렌 Skill 2 변경 저장
        }

        private static void ConfigureEllenSkill3() // 엘렌 Skill 3 철의 맹세 설정
        {
            SkillData skill = LoadSkill("SK_ELLEN_03"); // 엘렌 Skill 3 로드
            SerializedObject serialized = BeginSkill(skill, "철의 맹세", "자신의 방어력과 피해 감소를 크게 높이고 피격 시 기본 공격력 기준 반격 확률을 부여한다.", SkillTargetType.Self, SkillEffectType.Buff); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 기본값 설정
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.30f, 6f), // 강화도 1 방어 30퍼센트 증가
                new EffectSpec(SkillEffectKind.DamageReductionPercent, SkillTargetType.Self, 0.15f, 6f), // 강화도 1 피해 15퍼센트 감소
                new EffectSpec(SkillEffectKind.CounterChance, SkillTargetType.Self, 0.50f, 6f)); // 강화도 1 반격 확률 50퍼센트
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 기본값 설정
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.40f, 6f), // 강화도 2 방어 40퍼센트 증가
                new EffectSpec(SkillEffectKind.DamageReductionPercent, SkillTargetType.Self, 0.20f, 6f), // 강화도 2 피해 20퍼센트 감소
                new EffectSpec(SkillEffectKind.CounterChance, SkillTargetType.Self, 0.70f, 6f)); // 강화도 2 반격 확률 70퍼센트
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 기본값 설정
                new EffectSpec(SkillEffectKind.DefensePercent, SkillTargetType.Self, 0.50f, 6f), // 강화도 3 방어 50퍼센트 증가
                new EffectSpec(SkillEffectKind.DamageReductionPercent, SkillTargetType.Self, 0.25f, 6f), // 강화도 3 피해 25퍼센트 감소
                new EffectSpec(SkillEffectKind.CounterChance, SkillTargetType.Self, 0.90f, 6f)); // 강화도 3 반격 확률 90퍼센트
            EndSkill(serialized, skill); // 엘렌 Skill 3 변경 저장
        }

        private static SkillData LoadSkill(string skillId) // SkillId 기반 기존 SkillData 로드
        {
            string path = $"{SkillFolder}/{skillId}.asset"; // SkillData 에셋 경로 생성
            SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(path); // SkillData 에셋 로드

            if (skill == null) // SkillData 존재 확인
            {
                throw new System.InvalidOperationException($"SkillData not found: {path}. Run Day17 setup first."); // 17일차 데이터 누락 예외 발생
            }

            return skill; // 기존 SkillData 반환
        }

        private static SerializedObject BeginSkill(SkillData skill, string displayName, string description, SkillTargetType targetType, SkillEffectType effectType) // SkillData 기본 정보 설정 시작
        {
            SerializedObject serialized = new SerializedObject(skill); // SkillData 직렬화 객체 생성
            serialized.FindProperty("displayName").stringValue = displayName; // 실제 스킬 이름 적용
            serialized.FindProperty("description").stringValue = description; // 실제 스킬 설명 적용
            serialized.FindProperty("targetType").enumValueIndex = (int)targetType; // 대표 타겟 종류 적용
            serialized.FindProperty("effectType").enumValueIndex = (int)effectType; // 대표 효과 종류 적용
            SerializedProperty enhancements = serialized.FindProperty("enhancements"); // 강화도 배열 조회
            enhancements.arraySize = SkillData.MaxEnhancementLevel; // 강화도 3단계 고정
            return serialized; // 기본 정보가 설정된 직렬화 객체 반환
        }

        private static void ConfigureEnhancement(SerializedObject serialized, int enhancementLevel, int gaugeGain, params EffectSpec[] effects) // 강화도별 효과 목록 설정
        {
            SerializedProperty enhancements = serialized.FindProperty("enhancements"); // 강화도 배열 조회
            SerializedProperty enhancement = enhancements.GetArrayElementAtIndex(enhancementLevel - 1); // 현재 강화도 데이터 조회
            enhancement.FindPropertyRelative("powerRatio").floatValue = 1f; // 명시적 Effect 사용으로 범용 계수 1 설정
            enhancement.FindPropertyRelative("flatValue").intValue = 0; // 범용 고정값 0 설정
            enhancement.FindPropertyRelative("ultimateGaugeGain").intValue = gaugeGain; // 21일차 연결용 궁극기 게이지 획득량 저장
            SerializedProperty effectArray = enhancement.FindPropertyRelative("effects"); // 실제 효과 배열 조회
            effectArray.arraySize = effects == null ? 0 : effects.Length; // 효과 배열 크기 설정

            for (int index = 0; index < effectArray.arraySize; index++) // 강화도 실제 효과 순회
            {
                SerializedProperty effect = effectArray.GetArrayElementAtIndex(index); // 현재 효과 직렬화 데이터 조회
                EffectSpec spec = effects[index]; // 현재 효과 설정값 조회
                effect.FindPropertyRelative("kind").enumValueIndex = (int)spec.Kind; // 실제 효과 종류 적용
                effect.FindPropertyRelative("targetType").enumValueIndex = (int)spec.TargetType; // 효과 대상 종류 적용
                effect.FindPropertyRelative("value").floatValue = spec.Value; // 효과 수치 적용
                effect.FindPropertyRelative("duration").floatValue = Mathf.Max(0f, spec.Duration); // 효과 지속시간 적용
                effect.FindPropertyRelative("count").intValue = Mathf.Max(0, spec.Count); // 효과 개수 적용
                effect.FindPropertyRelative("requirePreviousSuccess").boolValue = spec.RequirePreviousSuccess; // 앞선 효과 성공 조건 적용
            }
        }

        private static void EndSkill(SerializedObject serialized, SkillData skill) // SkillData 변경 저장
        {
            serialized.ApplyModifiedPropertiesWithoutUndo(); // SkillData 직렬화 변경 적용
            EditorUtility.SetDirty(skill); // SkillData 변경 표시
        }
    }
}
