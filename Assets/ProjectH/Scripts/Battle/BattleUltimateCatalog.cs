using System.Collections.Generic; // 사전 자료형
using ProjectH.Data; // 스킬 효과 정의 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleUltimateDefinition // 효과 목록으로 정의한 궁극기 (Day64 신규)
    {
        public string CharacterId { get; } // 캐릭터 ID
        public string Name { get; } // 궁극기 이름
        public IReadOnlyList<SkillEffectDefinition> Effects { get; } // 효과 목록 (스킬과 같은 실행기)

        public BattleUltimateDefinition(string characterId, string name, params SkillEffectDefinition[] effects) // 정의 생성
        {
            CharacterId = characterId; // ID 저장
            Name = name; // 이름 저장
            Effects = effects; // 효과 저장
        }
    }

    public static class BattleUltimateCatalog // 합류 8인 궁극기 (Day64 신규 — 기획서 캐릭터 시트 16. 궁극기 기준 임시 수치, 67일차 1차 밸런스에서 조정)
    {
        private static readonly Dictionary<string, BattleUltimateDefinition> Definitions = Build(); // 캐릭터별 궁극기

        public static bool Has(string characterId) => characterId != null && Definitions.ContainsKey(characterId); // 정의 여부

        public static BattleUltimateDefinition Get(string characterId) => Has(characterId) ? Definitions[characterId] : null; // 정의 조회

        public static IEnumerable<string> CharacterIds => Definitions.Keys; // 정의된 캐릭터

        private static Dictionary<string, BattleUltimateDefinition> Build() // 8인 궁극기 목록
        {
            BattleUltimateDefinition[] list = // 기획서 순서
            {
                new BattleUltimateDefinition("CH_NATASHA", "그림자 연무", // 나타샤 : 4연타 총 600%, 체력 50% 이하면 +25%
                    Effect(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, 1.5f, count: 4), // 150% × 4
                    Effect(SkillEffectKind.DamageWhenOwnerLowHp, SkillTargetType.NearestEnemy, 1.5f)), // 저체력 추가 150% (총 600%의 25%)
                new BattleUltimateDefinition("CH_CLAIRE", "생명 연성", // 클레어 : 전체 회복 + 재생 + 피해 감소 10%
                    Effect(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.25f), // 전체 25% 회복 (재생 몫 포함)
                    Effect(SkillEffectKind.HealingReceivedPercent, SkillTargetType.AllAllies, 0.20f, 8f), // 받는 회복 +20% (재생 대신)
                    Effect(SkillEffectKind.DamageReductionPercent, SkillTargetType.AllAllies, 0.10f, 8f)), // 피해 감소 10%
                new BattleUltimateDefinition("CH_LUCIA", "헤드샷 서전트", // 루시아 : 단일 200% × 3발 고정 치명
                    Effect(SkillEffectKind.DamageAttackRatio, SkillTargetType.NearestEnemy, 2.0f, count: 3, damage: SkillDamageType.True)), // 방어 무시 3발
                new BattleUltimateDefinition("CH_PYRA", "화염창 폭발", // 파이라 : 전체 300% + 화상 8초
                    Effect(SkillEffectKind.DamageAttackRatio, SkillTargetType.AllEnemies, 3.0f, damage: SkillDamageType.Magic), // 전체 300%
                    Effect(SkillEffectKind.PeriodicDamageAttackRatio, SkillTargetType.AllEnemies, 0.35f, count: 8, damage: SkillDamageType.Magic)), // 화상 35% × 8초
                new BattleUltimateDefinition("CH_TYRIA", "수호의 의지", // 티리아 : 전체 보호막(HP 18%) + 피해 감소 15%
                    Effect(SkillEffectKind.ShieldOwnerMaxHpPercent, SkillTargetType.AllAllies, 0.18f), // 티리아 최대 체력 18% 보호막
                    Effect(SkillEffectKind.DamageReductionPercent, SkillTargetType.AllAllies, 0.15f, 10f)), // 피해 감소 15%
                new BattleUltimateDefinition("CH_MERCIA", "금강계 봉인", // 메르시아 : 전체 220% + 속박 2초 + 방어 감소 15%
                    Effect(SkillEffectKind.DamageAttackRatio, SkillTargetType.AllEnemies, 2.2f), // 전체 220%
                    Effect(SkillEffectKind.Stun, SkillTargetType.AllEnemies, 0f, 2f), // 속박 2초 (기절로 표현)
                    Effect(SkillEffectKind.DefenseReductionPercent, SkillTargetType.AllEnemies, 0.15f, 8f)), // 방어 감소 15%
                new BattleUltimateDefinition("CH_NOEL", "사막의 폭풍", // 노엘 : 전체 60% × 5연타, 빙결 20%
                    Effect(SkillEffectKind.DamageAttackRatio, SkillTargetType.AllEnemies, 0.6f, count: 5), // 60% × 5
                    Effect(SkillEffectKind.Stun, SkillTargetType.AllEnemies, 0f, 1.5f, chance: 0.2f)), // 빙결 20% (기절로 표현)
                new BattleUltimateDefinition("CH_SEPHIRA", "세라핌의 축복", // 세피라 : 35% 회복 + 1명 부활 + 공격력 18%
                    Effect(SkillEffectKind.HealMaxHpPercent, SkillTargetType.AllAllies, 0.35f), // 전체 35% 회복
                    Effect(SkillEffectKind.ReviveAllies, SkillTargetType.AllAllies, 0.35f, count: 1), // 1명 체력 35%로 부활
                    Effect(SkillEffectKind.AttackPercent, SkillTargetType.AllAllies, 0.18f, 10f)) // 공격력 +18%
            };

            Dictionary<string, BattleUltimateDefinition> map = new Dictionary<string, BattleUltimateDefinition>(); // 결과
            foreach (BattleUltimateDefinition definition in list) map[definition.CharacterId] = definition; // 등록
            return map; // 반환
        }

        private static SkillEffectDefinition Effect(SkillEffectKind kind, SkillTargetType target, float value, float duration = 0f, int count = 0, float chance = 1f, SkillDamageType damage = SkillDamageType.Physical) // 효과 한 줄 (주기 피해 간격 1초)
        {
            return new SkillEffectDefinition(kind, target, damage, value, duration, count, chance, 1f, 0f, false, false); // 효과 생성 (속성은 시전자 속성 상속)
        }
    }
}
