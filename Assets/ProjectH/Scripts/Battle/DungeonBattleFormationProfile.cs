using System; // 문자열 비교 기능
using System.Collections.Generic; // 읽기 전용 목록 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class DungeonBattleFormationProfile // 던전별 실제 적 편성 프로필
    {
        private static readonly DungeonBattleFormationProfile Dg001 = new DungeonBattleFormationProfile("DG001", new[] { "MON_CORRUPTED_WOLF" }); // DG001 늑대 단일 편성
        private static readonly DungeonBattleFormationProfile Dg002 = new DungeonBattleFormationProfile("DG002", new[] { "MON_CORRUPTED_SOLDIER", "MON_CORRUPTED_WOLF" }); // DG002 병사 늑대 편성
        private static readonly DungeonBattleFormationProfile Dg003 = new DungeonBattleFormationProfile("DG003", new[] { "MON_CORRUPTED_SOLDIER", "MON_CORRUPTED_WOLF", "MON_POLLUTED_PLANT" }); // DG003 세 종류 편성
        private static readonly DungeonBattleFormationProfile Dg004 = new DungeonBattleFormationProfile("DG004", new[] { "MON_CORRUPTED_SOLDIER", "MON_CORRUPTED_WOLF", "MON_POLLUTED_PLANT", "MON_CORRUPTED_SOLDIER" }); // DG004 네 개체 편성
        private readonly string[] enemyIds; // 적 몬스터 ID 배열
        public string DungeonId { get; } // 편성 던전 ID 반환
        public int EnemyCount => enemyIds.Length; // 편성 적군 수 반환
        public IReadOnlyList<string> EnemyIds => enemyIds; // 편성 적군 ID 목록 반환

        private DungeonBattleFormationProfile(string dungeonId, string[] enemies) // 던전 전투 편성 생성
        {
            DungeonId = dungeonId; // 편성 던전 ID 저장
            enemyIds = enemies ?? Array.Empty<string>(); // 적 몬스터 ID 저장
        }

        public string[] CreateEnemyIds() // 런타임 적군 ID 배열 생성
        {
            string[] copy = new string[enemyIds.Length]; // 적군 ID 복사 배열 생성
            Array.Copy(enemyIds, copy, enemyIds.Length); // 적군 ID 값 복사
            return copy; // 독립 적군 ID 배열 반환
        }

        public static DungeonBattleFormationProfile Get(string dungeonId) // 던전 ID 기반 실제 편성 조회
        {
            if (string.Equals(dungeonId, Dg002.DungeonId, StringComparison.Ordinal)) // DG002 던전 확인
            {
                return Dg002; // DG002 편성 반환
            }

            if (string.Equals(dungeonId, Dg003.DungeonId, StringComparison.Ordinal)) // DG003 던전 확인
            {
                return Dg003; // DG003 편성 반환
            }

            if (string.Equals(dungeonId, Dg004.DungeonId, StringComparison.Ordinal)) // DG004 던전 확인
            {
                return Dg004; // DG004 편성 반환
            }

            return Dg001; // DG001 및 기본 편성 반환
        }
    }
}
