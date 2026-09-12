using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 캐릭터·몬스터 데이터 기능
using UnityEngine; // 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BalanceEstimate // 한 웨이브의 예상 전투 결과 (Day67 신규)
    {
        public float EnemyHp { get; } // 적 전체 체력
        public float AllyDps { get; } // 아군 초당 피해
        public float EnemyDps { get; } // 적 초당 피해
        public float PartyHp { get; } // 파티 전체 체력 (회복 몫 포함)
        public float ClearSeconds { get; } // 예상 처치 시간
        public float SurviveSeconds { get; } // 예상 버티는 시간

        public BalanceEstimate(float enemyHp, float allyDps, float enemyDps, float partyHp) // 예상 결과 생성
        {
            EnemyHp = enemyHp; // 적 체력 저장
            AllyDps = Mathf.Max(1f, allyDps); // 아군 피해 저장
            EnemyDps = Mathf.Max(1f, enemyDps); // 적 피해 저장
            PartyHp = partyHp; // 파티 체력 저장
            ClearSeconds = EnemyHp / Mathf.Max(1f, allyDps); // 처치 시간 계산
            SurviveSeconds = partyHp / Mathf.Max(1f, enemyDps); // 버티는 시간 계산
        }

        public float SafetyRatio => ClearSeconds <= 0f ? 0f : SurviveSeconds / ClearSeconds; // 여유 비율 (1보다 크면 이길 수 있음)
    }

    public static class BalanceSimulator // 던전 난이도 계산기 (Day67 신규 — 권장 레벨 파티가 얼마나 걸리고 얼마나 버티는지 추정, 밸런스 조정 기준)
    {
        public const float EquipmentMultiplier = 1.35f; // 장비 강화·룬 평균 가정
        public const float SkillMultiplier = 1.6f; // 기본 공격 외 스킬·궁극기 몫
        public const float HealingMultiplier = 1.3f; // 회복 담당이 채워 주는 몫
        public const float RegularClearMaxSeconds = 60f; // 일반 웨이브 목표 상한
        public const float BossClearMinSeconds = 60f; // 보스 목표 하한
        public const float BossClearMaxSeconds = 150f; // 보스 목표 상한
        public const float MinSafetyRatio = 1.2f; // 최소 여유 비율 (이보다 낮으면 전멸 위험)

        public static BalanceEstimate Estimate(IReadOnlyList<CharacterData> party, int level, IReadOnlyList<MonsterData> wave, DungeonBattleTestProfile profile) // 한 웨이브 예상 결과
        {
            if (party == null || party.Count == 0 || wave == null || wave.Count == 0 || profile == null) return new BalanceEstimate(0f, 1f, 1f, 0f); // 입력 확인
            float growth = BattleGrowthFormula.GetLevelMultiplier(level) * EquipmentMultiplier; // 레벨 성장 + 장비
            float partyHp = 0f; // 파티 체력
            float partyDefense = 0f; // 파티 평균 방어력

            foreach (CharacterData member in party) // 파티 순회
            {
                partyHp += member.BaseHp * growth; // 체력 누적
                partyDefense += member.BaseDefense * growth; // 방어 누적
            }

            partyDefense /= party.Count; // 평균 방어력
            float enemyHp = 0f; // 적 체력
            float enemyDefense = 0f; // 적 평균 방어력
            float enemyDps = 0f; // 적 피해

            foreach (MonsterData monster in wave) // 적 순회
            {
                enemyHp += monster.MaxHp * profile.HealthMultiplier; // 체력 누적
                enemyDefense += monster.Defense * profile.DefenseMultiplier; // 방어 누적
                enemyDps += Mathf.Max(1f, (monster.Attack * profile.AttackMultiplier) - partyDefense) * monster.AttackSpeed; // 적 피해 누적
            }

            enemyDefense /= wave.Count; // 평균 방어력
            float allyDps = 0f; // 아군 피해

            foreach (CharacterData member in party) // 파티 순회
            {
                allyDps += Mathf.Max(1f, (member.BaseAttack * growth * SkillMultiplier) - enemyDefense) * member.AttackSpeed; // 아군 피해 누적
            }

            return new BalanceEstimate(enemyHp, allyDps, enemyDps, partyHp * HealingMultiplier); // 예상 결과 반환
        }

        public static bool IsBossWave(IReadOnlyList<MonsterData> wave) // 보스가 있는 웨이브인지
        {
            if (wave == null) return false; // 입력 확인

            foreach (MonsterData monster in wave) // 적 순회
            {
                if (monster != null && monster.BossPattern != null) return true; // 보스 있음
            }

            return false; // 일반 웨이브
        }

        public static string Describe(string dungeonId, int level, BalanceEstimate estimate, bool boss) // 밸런스 표 한 줄 (개발용 출력)
        {
            return $"{dungeonId} Lv.{level} {(boss ? "보스" : "일반")} · 처치 {estimate.ClearSeconds:0}초 · 버팀 {estimate.SurviveSeconds:0}초 · 여유 {estimate.SafetyRatio:0.0}배"; // 문구 반환
        }
    }
}
