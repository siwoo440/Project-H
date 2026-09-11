using ProjectH.Data; // 스킬 및 몬스터 데이터 기능
using UnityEditor; // Unity 에디터 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.EditorTools // 프로젝트 에디터 도구 영역
{
    public static class Phase1Day19Setup // 19일차 릴리아·이브 공격형 스킬 설정 도구
    {
        private const string SkillFolder = "Assets/ProjectH/Data/Skills"; // SkillData 저장 폴더
        private const string MonsterFolder = "Assets/ProjectH/Data/Monsters"; // MonsterData 저장 폴더

        private readonly struct EffectSpec // Editor 전용 스킬 효과 설정값
        {
            public SkillEffectKind Kind { get; } // 효과 종류
            public SkillTargetType TargetType { get; } // 효과 대상
            public SkillDamageType DamageType { get; } // 피해 종류
            public float Value { get; } // 효과 수치
            public float Duration { get; } // 효과 지속시간
            public int Count { get; } // Hit·Tick·정화 개수
            public float Chance { get; } // 확률 효과 발동 확률
            public float Interval { get; } // 주기 피해 Tick 간격
            public float Radius { get; } // 주변 대상 판정 반경
            public bool ExcludePrimary { get; } // 주 대상 제외 여부
            public bool RequirePreviousSuccess { get; } // 앞선 효과 성공 조건

            public EffectSpec(SkillEffectKind kind, SkillTargetType targetType, SkillDamageType damageType, float value, float duration = 0f, int count = 0, float chance = 1f, float interval = 1f, float radius = 0f, bool excludePrimary = false, bool requirePreviousSuccess = false) // 19일차 효과 설정 생성
            {
                Kind = kind; // 효과 종류 저장
                TargetType = targetType; // 효과 대상 저장
                DamageType = damageType; // 피해 종류 저장
                Value = value; // 효과 수치 저장
                Duration = duration; // 지속시간 저장
                Count = count; // Hit·Tick·정화 개수 저장
                Chance = chance; // 발동 확률 저장
                Interval = interval; // Tick 간격 저장
                Radius = radius; // 주변 반경 저장
                ExcludePrimary = excludePrimary; // 주 대상 제외 여부 저장
                RequirePreviousSuccess = requirePreviousSuccess; // 앞선 효과 성공 조건 저장
            }
        }

        [MenuItem("Tools/Project H/Phase 1/19일차 릴리아-이브 스킬 및 테스트 몬스터 설정 실행")] // 19일차 설정 메뉴 등록
        public static void Setup() // 릴리아·이브 6스킬 및 테스트 몬스터 수치 설정
        {
            ConfigureLiliaSkill1(); // 릴리아 아스트랄 스피어 설정
            ConfigureLiliaSkill2(); // 릴리아 별빛 폭우 설정
            ConfigureLiliaSkill3(); // 릴리아 카오스 오브 설정
            ConfigureEveSkill1(); // 이브 연속 사격 설정
            ConfigureEveSkill2(); // 이브 정령의 화살 설정
            ConfigureEveSkill3(); // 이브 엘프의 일제 사격 설정
            ConfigureMonster("MON_CORRUPTED_SOLDIER", 3250, 11); // 침식 병사 장시간 테스트 수치 적용
            ConfigureMonster("MON_CORRUPTED_WOLF", 2100, 10); // 침식 늑대 장시간 테스트 수치 적용
            ConfigureMonster("MON_POLLUTED_PLANT", 2600, 8); // 오염 식물 장시간 테스트 수치 적용
            AssetDatabase.SaveAssets(); // SkillData 및 MonsterData 변경 저장
            AssetDatabase.Refresh(); // 에셋 목록 갱신
            Debug.Log("[Project H][DAY19] Lilia/Eve six skills and long-battle monster test values configured."); // 19일차 설정 완료 로그
        }

        private static void ConfigureLiliaSkill1() // 릴리아 Skill 1 아스트랄 스피어 설정
        {
            SkillData skill = LoadSkill("SK_LILIA_01"); // 릴리아 Skill 1 로드
            SerializedObject serialized = BeginSkill(skill, "아스트랄 스피어", "가장 가까운 적에게 강한 마법 피해를 주고 3초 동안 1초 간격의 마법 DoT를 부여한다.", SkillTargetType.NearestEnemy, SkillEffectType.Damage); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 2.40f, 0f, 1), // 강화도 1 단일 마법 240퍼센트
                new EffectSpec(SkillEffectKind.PeriodicDamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 0.30f, 3f, 3, 1f, 1f)); // 강화도 1 DoT 30퍼센트 3Tick
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 3.00f, 0f, 1), // 강화도 2 단일 마법 300퍼센트
                new EffectSpec(SkillEffectKind.PeriodicDamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 0.40f, 3f, 3, 1f, 1f)); // 강화도 2 DoT 40퍼센트 3Tick
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 3.60f, 0f, 1), // 강화도 3 단일 마법 360퍼센트
                new EffectSpec(SkillEffectKind.PeriodicDamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 0.50f, 3f, 3, 1f, 1f)); // 강화도 3 DoT 50퍼센트 3Tick
            EndSkill(serialized, skill); // 릴리아 Skill 1 변경 저장
        }

        private static void ConfigureLiliaSkill2() // 릴리아 Skill 2 별빛 폭우 설정
        {
            SkillData skill = LoadSkill("SK_LILIA_02"); // 릴리아 Skill 2 로드
            SerializedObject serialized = BeginSkill(skill, "별빛 폭우", "생존 적 전체에 마법 피해를 주고 6초 동안 마법 저항을 감소시킨다.", SkillTargetType.AllEnemies, SkillEffectType.Damage); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.AllEnemies, SkillDamageType.Magic, 1.80f, 0f, 1), // 강화도 1 전체 마법 180퍼센트
                new EffectSpec(SkillEffectKind.ResistanceReductionPercent, SkillTargetType.AllEnemies, SkillDamageType.Magic, 0.10f, 6f)); // 강화도 1 마법 저항 10퍼센트 감소
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.AllEnemies, SkillDamageType.Magic, 2.25f, 0f, 1), // 강화도 2 전체 마법 225퍼센트
                new EffectSpec(SkillEffectKind.ResistanceReductionPercent, SkillTargetType.AllEnemies, SkillDamageType.Magic, 0.12f, 6f)); // 강화도 2 마법 저항 12퍼센트 감소
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.AllEnemies, SkillDamageType.Magic, 2.70f, 0f, 1), // 강화도 3 전체 마법 270퍼센트
                new EffectSpec(SkillEffectKind.ResistanceReductionPercent, SkillTargetType.AllEnemies, SkillDamageType.Magic, 0.15f, 6f)); // 강화도 3 마법 저항 15퍼센트 감소
            EndSkill(serialized, skill); // 릴리아 Skill 2 변경 저장
        }

        private static void ConfigureLiliaSkill3() // 릴리아 Skill 3 카오스 오브 설정
        {
            SkillData skill = LoadSkill("SK_LILIA_03"); // 릴리아 Skill 3 로드
            SerializedObject serialized = BeginSkill(skill, "카오스 오브", "가장 가까운 적에게 강한 마법 피해와 마법 저항 감소를 적용하고 주 대상 주변의 다른 적에게 폭발 피해를 준다.", SkillTargetType.NearestEnemy, SkillEffectType.Damage); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 3.20f, 0f, 1), // 강화도 1 주 대상 320퍼센트
                new EffectSpec(SkillEffectKind.ResistanceReductionPercent, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 0.15f, 6f), // 강화도 1 마법 저항 15퍼센트 감소
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearbyEnemiesFromPrimary, SkillDamageType.Magic, 0.90f, 0f, 1, 1f, 1f, 2.4f, true)); // 강화도 1 주변 적 90퍼센트 폭발
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 4.00f, 0f, 1), // 강화도 2 주 대상 400퍼센트
                new EffectSpec(SkillEffectKind.ResistanceReductionPercent, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 0.18f, 6f), // 강화도 2 마법 저항 18퍼센트 감소
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearbyEnemiesFromPrimary, SkillDamageType.Magic, 1.15f, 0f, 1, 1f, 1f, 2.4f, true)); // 강화도 2 주변 적 115퍼센트 폭발
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 4.80f, 0f, 1), // 강화도 3 주 대상 480퍼센트
                new EffectSpec(SkillEffectKind.ResistanceReductionPercent, SkillTargetType.NearestEnemy, SkillDamageType.Magic, 0.20f, 6f), // 강화도 3 마법 저항 20퍼센트 감소
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearbyEnemiesFromPrimary, SkillDamageType.Magic, 1.40f, 0f, 1, 1f, 1f, 2.4f, true)); // 강화도 3 주변 적 140퍼센트 폭발
            EndSkill(serialized, skill); // 릴리아 Skill 3 변경 저장
        }

        private static void ConfigureEveSkill1() // 이브 Skill 1 연속 사격 설정
        {
            SkillData skill = LoadSkill("SK_EVE_01"); // 이브 Skill 1 로드
            SerializedObject serialized = BeginSkill(skill, "연속 사격", "가장 가까운 적에게 물리 피해를 3회 연속 적용한다.", SkillTargetType.NearestEnemy, SkillEffectType.Damage); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Physical, 1.10f, 0f, 3)); // 강화도 1 110퍼센트 3Hit
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Physical, 1.35f, 0f, 3)); // 강화도 2 135퍼센트 3Hit
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, SkillDamageType.Physical, 1.60f, 0f, 3)); // 강화도 3 160퍼센트 3Hit
            EndSkill(serialized, skill); // 이브 Skill 1 변경 저장
        }

        private static void ConfigureEveSkill2() // 이브 Skill 2 정령의 화살 설정
        {
            SkillData skill = LoadSkill("SK_EVE_02"); // 이브 Skill 2 로드
            SerializedObject serialized = BeginSkill(skill, "정령의 화살", "전방 직선의 생존 적을 관통해 물리 피해를 주고 각 대상에게 확률적으로 1초 기절을 부여한다.", SkillTargetType.LineEnemies, SkillEffectType.Damage); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1.80f, 0f, 1), // 강화도 1 직선 180퍼센트
                new EffectSpec(SkillEffectKind.Stun, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1f, 1f, 0, 0.40f)); // 강화도 1 기절 40퍼센트
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.LineEnemies, SkillDamageType.Physical, 2.25f, 0f, 1), // 강화도 2 직선 225퍼센트
                new EffectSpec(SkillEffectKind.Stun, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1f, 1f, 0, 0.50f)); // 강화도 2 기절 50퍼센트
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.LineEnemies, SkillDamageType.Physical, 2.70f, 0f, 1), // 강화도 3 직선 270퍼센트
                new EffectSpec(SkillEffectKind.Stun, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1f, 1f, 0, 0.60f)); // 강화도 3 기절 60퍼센트
            EndSkill(serialized, skill); // 이브 Skill 2 변경 저장
        }

        private static void ConfigureEveSkill3() // 이브 Skill 3 엘프의 일제 사격 설정
        {
            SkillData skill = LoadSkill("SK_EVE_03"); // 이브 Skill 3 로드
            SerializedObject serialized = BeginSkill(skill, "엘프의 일제 사격", "전방 직선의 생존 적에게 3연타 물리 피해를 주고 명중률 감소와 확률 기절을 부여한다.", SkillTargetType.LineEnemies, SkillEffectType.Damage); // 스킬 기본 정보 설정
            ConfigureEnhancement(serialized, 1, 10, // 강화도 1 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1.50f, 0f, 3), // 강화도 1 150퍼센트 3Hit
                new EffectSpec(SkillEffectKind.AccuracyReductionPercent, SkillTargetType.LineEnemies, SkillDamageType.Physical, 0.10f, 6f), // 강화도 1 명중률 10퍼센트 감소
                new EffectSpec(SkillEffectKind.Stun, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1f, 1f, 0, 0.20f)); // 강화도 1 기절 20퍼센트
            ConfigureEnhancement(serialized, 2, 20, // 강화도 2 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1.85f, 0f, 3), // 강화도 2 185퍼센트 3Hit
                new EffectSpec(SkillEffectKind.AccuracyReductionPercent, SkillTargetType.LineEnemies, SkillDamageType.Physical, 0.12f, 6f), // 강화도 2 명중률 12퍼센트 감소
                new EffectSpec(SkillEffectKind.Stun, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1f, 1f, 0, 0.25f)); // 강화도 2 기절 25퍼센트
            ConfigureEnhancement(serialized, 3, 30, // 강화도 3 설정
                new EffectSpec(SkillEffectKind.DamageAttackRatio, SkillTargetType.LineEnemies, SkillDamageType.Physical, 2.20f, 0f, 3), // 강화도 3 220퍼센트 3Hit
                new EffectSpec(SkillEffectKind.AccuracyReductionPercent, SkillTargetType.LineEnemies, SkillDamageType.Physical, 0.15f, 6f), // 강화도 3 명중률 15퍼센트 감소
                new EffectSpec(SkillEffectKind.Stun, SkillTargetType.LineEnemies, SkillDamageType.Physical, 1f, 1f, 0, 0.30f)); // 강화도 3 기절 30퍼센트
            EndSkill(serialized, skill); // 이브 Skill 3 변경 저장
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
                effect.FindPropertyRelative("damageType").enumValueIndex = (int)spec.DamageType; // 피해 종류 적용
                effect.FindPropertyRelative("value").floatValue = spec.Value; // 효과 수치 적용
                effect.FindPropertyRelative("duration").floatValue = Mathf.Max(0f, spec.Duration); // 효과 지속시간 적용
                effect.FindPropertyRelative("count").intValue = Mathf.Max(0, spec.Count); // Hit·Tick·정화 개수 적용
                effect.FindPropertyRelative("chance").floatValue = Mathf.Clamp01(spec.Chance); // 효과 발동 확률 적용
                effect.FindPropertyRelative("interval").floatValue = Mathf.Max(0f, spec.Interval); // 주기 피해 간격 적용
                effect.FindPropertyRelative("radius").floatValue = Mathf.Max(0f, spec.Radius); // 주변 효과 반경 적용
                effect.FindPropertyRelative("excludePrimary").boolValue = spec.ExcludePrimary; // 주 대상 제외 여부 적용
                effect.FindPropertyRelative("requirePreviousSuccess").boolValue = spec.RequirePreviousSuccess; // 앞선 효과 성공 조건 적용
            }
        }

        private static void EndSkill(SerializedObject serialized, SkillData skill) // SkillData 변경 저장
        {
            serialized.ApplyModifiedPropertiesWithoutUndo(); // SkillData 직렬화 변경 적용
            EditorUtility.SetDirty(skill); // SkillData 변경 표시
        }

        private static void ConfigureMonster(string monsterId, int maxHp, int attack) // 스킬 테스트용 몬스터 HP 및 공격력 고정 설정
        {
            string path = $"{MonsterFolder}/{monsterId}.asset"; // MonsterData 에셋 경로 생성
            MonsterData monster = AssetDatabase.LoadAssetAtPath<MonsterData>(path); // MonsterData 에셋 로드

            if (monster == null) // MonsterData 존재 확인
            {
                throw new System.InvalidOperationException($"MonsterData not found: {path}."); // 몬스터 데이터 누락 예외 발생
            }

            SerializedObject serialized = new SerializedObject(monster); // MonsterData 직렬화 객체 생성
            serialized.FindProperty("maxHp").intValue = Mathf.Max(1, maxHp); // 장시간 스킬 테스트용 최대 HP 적용
            serialized.FindProperty("attack").intValue = Mathf.Max(0, attack); // 장시간 스킬 테스트용 낮은 공격력 적용
            serialized.ApplyModifiedPropertiesWithoutUndo(); // MonsterData 변경 적용
            EditorUtility.SetDirty(monster); // MonsterData 변경 표시
        }
    }
}
