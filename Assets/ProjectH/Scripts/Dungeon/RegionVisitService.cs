using System; // 문자열 비교 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Dialogue; // 대화 진행 결과 기능
using ProjectH.SaveSystem; // 저장·호감도 기능

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역
{
    public sealed class RegionArrivalDefinition // 지역 첫 방문 이야기 한 편 (Day65 신규)
    {
        public string RegionId { get; } // 지도 지역 ID
        public string ScriptId { get; } // 대사 파일 ID
        public string CharacterId { get; } // 안내하는 동료 (고향 출신)
        public string VisitFlag { get; } // 방문 기록 스토리 플래그 (세계관 단어도 이 플래그로 열림)

        public RegionArrivalDefinition(string regionId, string scriptId, string characterId, string visitFlag) // 정의 생성
        {
            RegionId = regionId; // 지역 저장
            ScriptId = scriptId; // 대사 저장
            CharacterId = characterId; // 동료 저장
            VisitFlag = visitFlag; // 플래그 저장
        }
    }

    public static class RegionVisitService // 지역 첫 방문 이야기 (Day65 신규 — 던전에 처음 들어갈 때 1회)
    {
        public const int ArrivalAffinity = 3; // 끝까지 본 경우 안내 동료 호감도
        public const string NoirVisitFlag = "REGION_VISITED_NOIR"; // 노아르 방문 플래그
        public const string SilvaranVisitFlag = "REGION_VISITED_SILVARAN"; // 실바란 방문 플래그
        public const string KarnianVisitFlag = "REGION_VISITED_KARNIAN"; // 카르니안 방문 플래그 (Day66)
        public const string AstarVisitFlag = "REGION_VISITED_ASTAR"; // 아스타르 방문 플래그 (Day66)
        public const string SeaVisitFlag = "REGION_VISITED_SEA"; // 서쪽 해안 방문 플래그 (Day67)

        private static readonly RegionArrivalDefinition[] Definitions = // 지역별 첫 방문 이야기
        {
            new RegionArrivalDefinition("REGION_NOIR", "REGION_NOIR_ARRIVAL", "CH_LILIA", NoirVisitFlag), // 노아르 : 릴리아의 고향
            new RegionArrivalDefinition("REGION_SILVARAN", "REGION_SILVARAN_ARRIVAL", "CH_EVE", SilvaranVisitFlag), // 실바란 : 이브의 고향
            new RegionArrivalDefinition("REGION_KARNIAN", "REGION_KARNIAN_ARRIVAL", "CH_ELLEN", KarnianVisitFlag), // 카르니안 : 엘렌의 조국 (Day66)
            new RegionArrivalDefinition("REGION_DESERT", "REGION_DESERT_ARRIVAL", "CH_SERENA", AstarVisitFlag), // 아스타르 : 세레나와 봉인 장치 (Day66)
            new RegionArrivalDefinition("REGION_SEA", "REGION_SEA_ARRIVAL", "CH_LILIA", SeaVisitFlag) // 서쪽 해안 : 릴리아의 균열 분석 (Day67)
        };

        public static IReadOnlyList<RegionArrivalDefinition> All => Definitions; // 전체 정의

        public static RegionArrivalDefinition Find(string regionId) // 지역으로 조회
        {
            foreach (RegionArrivalDefinition definition in Definitions) // 정의 순회
            {
                if (string.Equals(definition.RegionId, regionId, StringComparison.Ordinal)) return definition; // 일치
            }

            return null; // 없음
        }

        public static RegionArrivalDefinition FindByDungeon(string dungeonId) // 던전이 속한 지역의 정의
        {
            AdventureRegion region = AdventureRegionCatalog.FindByDungeon(dungeonId); // 지역
            return region == null ? null : Find(region.Id); // 정의
        }

        public static bool HasVisited(SaveData saveData, RegionArrivalDefinition definition) => saveData != null && definition != null && saveData.HasStoryFlag(definition.VisitFlag); // 방문 여부

        public static RegionArrivalDefinition GetPendingArrival(SaveData saveData, string dungeonId) // 이번 입장에서 보여 줄 첫 방문 이야기 (없으면 null)
        {
            RegionArrivalDefinition definition = FindByDungeon(dungeonId); // 정의
            return definition == null || saveData == null || HasVisited(saveData, definition) ? null : definition; // 처음일 때만
        }

        public static string CompleteArrival(SaveData saveData, RegionArrivalDefinition definition, DialogueRunner runner) // 이야기가 닫히면 방문 기록 (끝까지 봤고 동료가 있으면 호감도)
        {
            if (saveData == null || definition == null) return string.Empty; // 입력 확인
            saveData.SetStoryFlag(definition.VisitFlag); // 방문 기록 (중간에 닫아도 다시 나오지 않음)
            if (runner == null || !runner.IsFinished || !saveData.HasCharacter(definition.CharacterId)) return string.Empty; // 보상 조건
            int before = AffinityService.GetAffinity(saveData, definition.CharacterId); // 이전 호감도
            int gained = AffinityService.AddAffinity(saveData, definition.CharacterId, ArrivalAffinity + runner.AccumulatedAffinity) - before; // 호감도 반영
            return $"호감도 +{gained}"; // 결과 안내
        }
    }
}
