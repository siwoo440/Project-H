using System; // 문자열 비교 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class DungeonBattleTestProfile // 27일차 던전별 테스트 전투 프로필
    {
        private static readonly DungeonBattleTestProfile Dg001 = new DungeonBattleTestProfile("DG001", 1.00f, 1.00f, 1.00f, 1.00f); // DG001 기본 테스트 배율
        private static readonly DungeonBattleTestProfile Dg002 = new DungeonBattleTestProfile("DG002", 1.50f, 1.25f, 1.20f, 1.10f); // DG002 중간 테스트 배율
        private static readonly DungeonBattleTestProfile Dg003 = new DungeonBattleTestProfile("DG003", 2.25f, 1.60f, 1.50f, 1.35f); // DG003 상급 테스트 배율
        private static readonly DungeonBattleTestProfile Dg004 = new DungeonBattleTestProfile("DG004", 3.50f, 2.10f, 1.90f, 1.70f); // DG004 고난도 테스트 배율

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

            return Dg001; // DG001 및 직접 전투 기본 프로필 반환
        }
    }
}
