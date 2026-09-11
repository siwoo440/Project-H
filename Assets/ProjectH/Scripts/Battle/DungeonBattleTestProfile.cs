using System; // 문자열 비교 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class DungeonBattleTestProfile // 27일차 던전별 테스트 전투 프로필
    {
        private static readonly DungeonBattleTestProfile Dg001 = new DungeonBattleTestProfile("DG001", 1.00f, 1.00f, 1.00f, 1.00f); // DG001 기본 테스트 배율
        private static readonly DungeonBattleTestProfile Dg002 = new DungeonBattleTestProfile("DG002", 1.50f, 1.25f, 1.20f, 1.10f); // DG002 중간 테스트 배율
        private static readonly DungeonBattleTestProfile Dg003 = new DungeonBattleTestProfile("DG003", 2.25f, 1.60f, 1.50f, 1.35f); // DG003 상급 테스트 배율
        private static readonly DungeonBattleTestProfile Dg004 = new DungeonBattleTestProfile("DG004", 3.50f, 2.10f, 1.90f, 1.70f); // DG004 고난도 테스트 배율
        private static readonly DungeonBattleTestProfile[] Expansion = // 지역 확장 던전 배율 (Day65~66 — 권장 레벨 순으로 올라감)
        {
            new DungeonBattleTestProfile("DG009", 2.55f, 1.75f, 1.62f, 1.45f), // Lv.6 늪지대 독안개 저습지
            new DungeonBattleTestProfile("DG005", 2.60f, 1.80f, 1.65f, 1.50f), // Lv.6 노아르 금지된 지하 서고
            new DungeonBattleTestProfile("DG006", 2.90f, 1.90f, 1.75f, 1.55f), // Lv.7 실바란 울부짖는 정령의 숲
            new DungeonBattleTestProfile("DG010", 3.40f, 2.05f, 1.85f, 1.68f), // Lv.8 노아르 의회 금단 실험실
            new DungeonBattleTestProfile("DG007", 3.80f, 2.20f, 1.95f, 1.75f), // Lv.9 카르니안 얼어붙은 요새
            new DungeonBattleTestProfile("DG011", 3.70f, 2.15f, 1.92f, 1.72f), // Lv.9 실바란 세계수 뿌리 성소 (같은 Lv.9 카르니안과 겹치지 않게 조금 낮춤)
            new DungeonBattleTestProfile("DG008", 4.20f, 2.35f, 2.05f, 1.85f), // Lv.10 아스타르 잠든 봉인 신전
            new DungeonBattleTestProfile("DG013", 4.60f, 2.50f, 2.15f, 1.95f), // Lv.11 카르니안 설원 전선 병영
            new DungeonBattleTestProfile("DG014", 5.00f, 2.65f, 2.25f, 2.05f), // Lv.12 아스타르 지하 고대 도시
            new DungeonBattleTestProfile("DG012", 5.50f, 2.80f, 2.35f, 2.15f) // Lv.13 마왕성 검은 성벽
        };

        public string DungeonId { get; } // 프로필 던전 ID 반환
        public float HealthMultiplier { get; } // 적 체력 배율 반환
        public float AttackMultiplier { get; } // 적 공격력 배율 반환
        public float DefenseMultiplier { get; } // 적 방어력 배율 반환
        public float ResistanceMultiplier { get; } // 적 저항력 배율 반환

        private DungeonBattleTestProfile(string dungeonId, float healthMultiplier, float attackMultiplier, float defenseMultiplier, float resistanceMultiplier) // 테스트 프로필 생성
        {
            DungeonId = dungeonId; // 던전 ID 저장
            HealthMultiplier = healthMultiplier; // 체력 배율 저장
            AttackMultiplier = attackMultiplier; // 공격력 배율 저장
            DefenseMultiplier = defenseMultiplier; // 방어력 배율 저장
            ResistanceMultiplier = resistanceMultiplier; // 저항력 배율 저장
        }

        public static DungeonBattleTestProfile Get(string dungeonId) // 던전 ID 기반 테스트 프로필 조회
        {
            if (string.Equals(dungeonId, Dg002.DungeonId, StringComparison.Ordinal)) // DG002 선택 확인
            {
                return Dg002; // DG002 프로필 반환
            }

            if (string.Equals(dungeonId, Dg003.DungeonId, StringComparison.Ordinal)) // DG003 선택 확인
            {
                return Dg003; // DG003 프로필 반환
            }

            if (string.Equals(dungeonId, Dg004.DungeonId, StringComparison.Ordinal)) // DG004 선택 확인
            {
                return Dg004; // DG004 프로필 반환
            }

            foreach (DungeonBattleTestProfile profile in Expansion) // 지역 확장 던전 (Day65~66)
            {
                if (string.Equals(dungeonId, profile.DungeonId, StringComparison.Ordinal)) return profile; // 일치 프로필 반환
            }

            return Dg001; // DG001 및 직접 전투 기본 프로필 반환
        }
    }
}
