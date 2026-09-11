using ProjectH.Data; // 던전 데이터 기능
using ProjectH.SaveSystem; // 활력 기능

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역
{
    public static class DungeonEntryService // 던전 입장 비용 처리 (Day61 신규 — 경제 밸런스 : 하루 입장 횟수를 활력으로 제한)
    {
        public static string BuildEnteredFlag(string dungeonId) => "DUNGEON_ENTERED_" + dungeonId; // 한 번이라도 도전한 던전 플래그 (Day64 추가 — 동료 합류 조건)

        public static bool CanPayEntry(SaveData saveData, DungeonData dungeon) // 입장 가능 여부
        {
            return saveData != null && dungeon != null && VitalityService.GetVitality(saveData) >= dungeon.VitalityCost; // 활력 확인
        }

        public static bool TryPayEntry(SaveData saveData, DungeonData dungeon, out string error) // 입장 활력 소비
        {
            error = string.Empty; // 오류 초기화

            if (saveData == null || dungeon == null) // 입력 확인
            {
                error = "던전 또는 저장 데이터를 찾을 수 없습니다."; // 안내
                return false; // 실패
            }

            if (dungeon.VitalityCost <= 0) // 무료 던전
            {
                saveData.SetStoryFlag(BuildEnteredFlag(dungeon.Id)); // 도전 기록 (Day64)
                return true; // 성공
            }

            if (!CanPayEntry(saveData, dungeon)) // 활력 부족
            {
                error = $"활력이 부족합니다 · 필요 {dungeon.VitalityCost} / 보유 {VitalityService.GetVitality(saveData)} (다음 날 전체 회복)"; // 안내 (GameTimeService가 일차 변경 시 RestoreFull)
                return false; // 실패
            }

            if (!VitalityService.TrySpendVitality(saveData, dungeon.VitalityCost, out error)) return false; // 활력 소비
            saveData.SetStoryFlag(BuildEnteredFlag(dungeon.Id)); // 도전 기록 (Day64 — 마왕성 도전 시 동료 합류)
            return true; // 입장
        }
    }
}
