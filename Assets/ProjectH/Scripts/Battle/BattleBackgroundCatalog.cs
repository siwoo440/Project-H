using System.Collections.Generic; // 사전 자료형
using ProjectH.Dungeon; // 모험 지역 표 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleBackgroundCatalog // 전투 배경 표 (Day78 신규 — 던전이 속한 지역에 따라 배경 키를 정한다)
    {
        public const string DefaultKey = "BATTLE_FOREST"; // 지역을 알 수 없을 때 쓰는 배경
        public const float DimAlpha = 0.28f; // 배경을 어둡게 덮는 정도 (유닛과 글자가 묻히지 않게)

        private static readonly Dictionary<string, string> RegionKeys = new Dictionary<string, string> // 지역 ID → 전투 배경 키 (Resources/Dialogues/Backgrounds/{키}.png, 없으면 DialogueArtFactory의 대체 배경)
        {
            { "REGION_FOREST", "BATTLE_FOREST" }, // 숲
            { "REGION_SWAMP", "BATTLE_SWAMP" }, // 늪지대
            { "REGION_DEMON_CASTLE", "BATTLE_DEMON_CASTLE" }, // 마왕성
            { "REGION_NOIR", "BATTLE_NOIR" }, // 노아르
            { "REGION_SILVARAN", "BATTLE_SILVARAN" }, // 실바란
            { "REGION_KARNIAN", "BATTLE_KARNIAN" }, // 카르니안
            { "REGION_DESERT", "BATTLE_DESERT" }, // 사막
            { "REGION_SEA", "BATTLE_SEA" } // 바다
        };

        public static IEnumerable<string> Keys => RegionKeys.Values; // 전투 배경 키 목록 (점검 표·테스트용)

        public static string GetKeyForRegion(string regionId) // 지역의 전투 배경 키 (모르는 지역은 기본)
        {
            return regionId != null && RegionKeys.TryGetValue(regionId, out string key) ? key : DefaultKey; // 배경 키 반환
        }

        public static string GetKeyForDungeon(string dungeonId) // 던전의 전투 배경 키
        {
            AdventureRegion region = string.IsNullOrEmpty(dungeonId) ? null : AdventureRegionCatalog.FindByDungeon(dungeonId); // 던전이 속한 지역
            return GetKeyForRegion(region == null ? null : region.Id); // 지역 기준 배경
        }
    }
}
