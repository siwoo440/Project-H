using System; // 문자열 비교 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class DungeonBattleTestProfile // 던전별 적 능력치 배율 (27일차 신규 → Day67 1차 밸런스에서 레벨 기준으로 다시 정리)
    {
        private static readonly DungeonBattleTestProfile[] Profiles = // 권장 레벨 순서 (체력 ≈ 1 + 0.16×(Lv-1), 공격 ≈ 1 + 0.10×(Lv-1), 방어·저항은 더 완만)
        {
            new DungeonBattleTestProfile("DG001", 0.75f, 0.70f, 1.00f, 1.00f), // Lv.1 무너진 성역의 숲 (Day68 — 세레나 + 루시아 2인 파티 기준 튜토리얼 난이도)
            new DungeonBattleTestProfile("DG002", 1.34f, 1.22f, 1.20f, 1.18f), // Lv.3 성역 외곽 폐허
            new DungeonBattleTestProfile("DG003", 1.68f, 1.44f, 1.40f, 1.36f), // Lv.5 침식된 회랑
            new DungeonBattleTestProfile("DG005", 1.80f, 1.50f, 1.45f, 1.40f), // Lv.6 노아르 금지된 지하 서고
            new DungeonBattleTestProfile("DG009", 1.82f, 1.52f, 1.47f, 1.42f), // Lv.6 늪지대 독안개 저습지
            new DungeonBattleTestProfile("DG006", 2.00f, 1.64f, 1.58f, 1.52f), // Lv.7 실바란 울부짖는 정령의 숲
            new DungeonBattleTestProfile("DG004", 2.12f, 1.70f, 1.63f, 1.56f), // Lv.8 심연의 관문
            new DungeonBattleTestProfile("DG010", 2.14f, 1.72f, 1.65f, 1.58f), // Lv.8 노아르 의회 금단 실험실
            new DungeonBattleTestProfile("DG007", 2.32f, 1.84f, 1.76f, 1.68f), // Lv.9 카르니안 얼어붙은 요새
            new DungeonBattleTestProfile("DG011", 2.28f, 1.80f, 1.72f, 1.64f), // Lv.9 실바란 세계수 뿌리 성소
            new DungeonBattleTestProfile("DG008", 2.46f, 1.92f, 1.83f, 1.74f), // Lv.10 아스타르 잠든 봉인 신전
            new DungeonBattleTestProfile("DG013", 2.64f, 2.04f, 1.94f, 1.84f), // Lv.11 카르니안 설원 전선 병영
            new DungeonBattleTestProfile("DG014", 2.76f, 2.10f, 1.99f, 1.88f), // Lv.12 아스타르 지하 고대 도시
            new DungeonBattleTestProfile("DG012", 2.94f, 2.22f, 2.10f, 1.98f), // Lv.13 마왕성 검은 성벽
            new DungeonBattleTestProfile("DG015", 2.96f, 2.24f, 2.12f, 2.00f), // Lv.13 바다 검은 균열 해안
            new DungeonBattleTestProfile("DG016", 3.24f, 2.40f, 2.26f, 2.10f), // Lv.15 바다 균열 심층
            new DungeonBattleTestProfile("DG017", 3.62f, 2.62f, 2.44f, 2.26f) // Lv.17 마왕성 무명의 옥좌 (Day71 최종 던전)
        };

        public string DungeonId { get; } // 프로필 던전 ID 반환
        public float HealthMultiplier { get; } // 적 체력 배율 반환
        public float AttackMultiplier { get; } // 적 공격력 배율 반환
        public float DefenseMultiplier { get; } // 적 방어력 배율 반환
        public float ResistanceMultiplier { get; } // 적 저항력 배율 반환

        private DungeonBattleTestProfile(string dungeonId, float healthMultiplier, float attackMultiplier, float defenseMultiplier, float resistanceMultiplier) // 프로필 생성
        {
            DungeonId = dungeonId; // 던전 ID 저장
            HealthMultiplier = healthMultiplier; // 체력 배율 저장
            AttackMultiplier = attackMultiplier; // 공격력 배율 저장
            DefenseMultiplier = defenseMultiplier; // 방어력 배율 저장
            ResistanceMultiplier = resistanceMultiplier; // 저항력 배율 저장
        }

        public static DungeonBattleTestProfile Get(string dungeonId) // 던전 ID 기반 프로필 조회 (없으면 첫 던전 기준)
        {
            for (int index = 0; index < Profiles.Length; index++) // 프로필 순회
            {
                if (string.Equals(dungeonId, Profiles[index].DungeonId, StringComparison.Ordinal)) return Profiles[index]; // 일치 프로필 반환
            }

            return Profiles[0]; // 직접 전투 기본 프로필 반환
        }
    }
}
