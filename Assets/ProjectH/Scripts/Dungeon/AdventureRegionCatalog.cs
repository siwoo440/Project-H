using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 자료형
using UnityEngine; // 좌표·색 기능

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역
{
    public enum AdventureRegionKind // 지도 지역 종류 (Day63 신규)
    {
        Dungeon = 0, // 던전 지역
        Village = 1, // 마을로 이동
        Locked = 2 // 아직 열리지 않은 지역
    }

    public sealed class AdventureRegion // 모험 지도 지역 하나
    {
        public string Id { get; } // 지역 ID
        public string Name { get; } // 지도 이름 (목업 표기)
        public string LoreName { get; } // 세계관 지명
        public string Description { get; } // 설명
        public Vector2 Position { get; } // 지도 위 위치 (0~1)
        public AdventureRegionKind Kind { get; } // 종류
        public IReadOnlyList<string> DungeonIds { get; } // 속한 던전
        public string LockedHint { get; } // 잠김 안내

        public AdventureRegion(string id, string name, string loreName, string description, Vector2 position, AdventureRegionKind kind, string[] dungeonIds, string lockedHint = "") // 지역 생성
        {
            Id = id; // ID 저장
            Name = name; // 이름 저장
            LoreName = loreName; // 지명 저장
            Description = description; // 설명 저장
            Position = position; // 위치 저장
            Kind = kind; // 종류 저장
            DungeonIds = dungeonIds ?? Array.Empty<string>(); // 던전 저장
            LockedHint = lockedHint ?? string.Empty; // 안내 저장
        }
    }

    public static class AdventureRegionCatalog // 모험 지도 지역 표 (Day63 신규 — 목업 10번 : 마왕성 · 숲 · 늪지대 · 마을 · 사막 · 바다, Day65 노아르 · 실바란 추가)
    {
        public const string VillageRegionId = "REGION_VILLAGE"; // 마을 지역 ID

        private static readonly AdventureRegion[] Regions = // 지역 목록 (지도 위치는 기획서 2.3 대륙 방위 기준)
        {
            new AdventureRegion("REGION_FOREST", "숲", "레티시아 성역의 숲", "주인공이 처음 소환된 성역을 둘러싼 숲. 성역 근처까지 번진 침식을 막는 첫 무대다.", new Vector2(0.55f, 0.50f), AdventureRegionKind.Dungeon, new[] { "DG001", "DG002" }), // 숲
            new AdventureRegion("REGION_SWAMP", "늪지대", "국경 침식 지대", "왕국 서북쪽 국경의 습지. 침식이 고인 물처럼 번져 회랑 전체가 던전으로 변했다.", new Vector2(0.30f, 0.66f), AdventureRegionKind.Dungeon, new[] { "DG003", "DG009" }), // 늪지대 (Day66 독안개 저습지 추가)
            new AdventureRegion("REGION_DEMON_CASTLE", "마왕성", "검은 균열지대 입구", "대륙 외곽, 검은 안개가 피어오르는 금지된 땅으로 가는 관문. 가장 강한 침식이 기다린다.", new Vector2(0.80f, 0.80f), AdventureRegionKind.Dungeon, new[] { "DG004", "DG012" }), // 마왕성 (Day66 검은 성벽 추가)
            new AdventureRegion(VillageRegionId, "마을", "모험가의 마을", "여관 · 광장 · 시장 · 온천 · 길드가 있는 쉼터. 동료들과 시간을 보낼 수 있다.", new Vector2(0.44f, 0.36f), AdventureRegionKind.Village, null), // 마을
            new AdventureRegion("REGION_NOIR", "노아르", "노아르 마법도시", "대륙 서부, 마법사 의회가 다스리는 독립 도시. 푸른 마법등 아래 지하 서고에서 폭주한 마력이 새어 나오고 있다.", new Vector2(0.18f, 0.52f), AdventureRegionKind.Dungeon, new[] { "DG005", "DG010" }), // 노아르 (Day65, Day66 의회 금단 실험실 추가)
            new AdventureRegion("REGION_SILVARAN", "실바란", "실바란 숲", "대륙 동부의 거대한 원시림. 마나가 모이는 신성한 숲이지만, 침식에 물든 정령들이 울부짖고 있다.", new Vector2(0.78f, 0.46f), AdventureRegionKind.Dungeon, new[] { "DG006", "DG011" }), // 실바란 (Day65, Day66 세계수 뿌리 성소 추가)
            new AdventureRegion("REGION_KARNIAN", "카르니안", "카르니안 제국", "대륙 북부의 설원 군사 국가. 침식에 물든 제국군이 얼어붙은 요새와 전선을 지키고 있다.", new Vector2(0.54f, 0.84f), AdventureRegionKind.Dungeon, new[] { "DG007", "DG013" }), // 카르니안 (Day66)
            new AdventureRegion("REGION_DESERT", "사막", "아스타르 사막", "고대 문명의 신전과 지하 도시가 잠든 남쪽 사막. 마왕 봉인 장치의 잔해가 모래 아래에서 깨어나고 있다.", new Vector2(0.60f, 0.15f), AdventureRegionKind.Dungeon, new[] { "DG008", "DG014" }), // 사막 (Day66 열림)
            new AdventureRegion("REGION_SEA", "바다", "서쪽 해안", "노아르 마법도시 너머로 이어지는 바다. 검은 균열이 바다 위에도 나타났다는 소문이 돈다.", new Vector2(0.12f, 0.30f), AdventureRegionKind.Locked, null, "67일차 검은 균열 · 긴급 던전에서 열립니다.") // 바다 (잠김)
        };

        public static IReadOnlyList<AdventureRegion> All => Regions; // 전체 지역

        public static AdventureRegion Get(string regionId) // ID로 조회
        {
            foreach (AdventureRegion region in Regions) // 지역 순회
            {
                if (string.Equals(region.Id, regionId, StringComparison.Ordinal)) return region; // 일치
            }

            return null; // 없음
        }

        public static AdventureRegion FindByDungeon(string dungeonId) // 던전이 속한 지역
        {
            foreach (AdventureRegion region in Regions) // 지역 순회
            {
                foreach (string id in region.DungeonIds) // 던전 순회
                {
                    if (string.Equals(id, dungeonId, StringComparison.Ordinal)) return region; // 일치
                }
            }

            return null; // 없음
        }
    }
}
