using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 던전 진행 상태 기능
using ProjectH.SaveSystem; // 저장 기능
using ProjectH.UI; // 지원 던전 목록 기능

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역
{
    public static class DungeonGuideService // 다음 던전 안내 (Day66 추가 요청 — 열렸지만 아직 클리어하지 않은 던전과 그 지역에 "!" 표시)
    {
        public static bool IsNewDungeon(SaveData saveData, string dungeonId) // 새 목표 던전 여부 (진입 가능 · 미클리어)
        {
            return saveData != null && DungeonProgressionPolicy.GetState(saveData, dungeonId) == DungeonProgressState.Available; // 잠김·클리어가 아니면 목표
        }

        public static bool HasNewDungeon(SaveData saveData, AdventureRegion region) // 지역에 새 목표 던전이 있는지
        {
            if (region == null || region.Kind != AdventureRegionKind.Dungeon) return false; // 던전 지역만

            foreach (string dungeonId in region.DungeonIds) // 던전 순회
            {
                if (IsNewDungeon(saveData, dungeonId)) return true; // 하나라도 있으면
            }

            return false; // 없음
        }

        public static List<string> GetNewDungeonIds(SaveData saveData) // 전체 새 목표 던전 (지원 순서)
        {
            List<string> result = new List<string>(); // 결과

            foreach (string dungeonId in DungeonSelectionRuntimeState.SupportedDungeonIds) // 던전 순회
            {
                if (IsNewDungeon(saveData, dungeonId)) result.Add(dungeonId); // 목표 추가
            }

            return result; // 결과 반환
        }
    }
}
