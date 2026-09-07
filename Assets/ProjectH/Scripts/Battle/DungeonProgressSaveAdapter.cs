using ProjectH.SaveSystem; // 저장 데이터 기능
using ProjectH.UI; // 지원 던전 ID 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class DungeonProgressSaveAdapter // 던전 클리어 영구 저장 호환 기능
    {
        private const string ClearFlagPrefix = "SYS_DUNGEON_CLEAR:"; // 던전 클리어 시스템 플래그 접두사

        public static bool IsCleared(SaveData saveData, string dungeonId) // 던전 클리어 여부 확인
        {
            if (saveData == null || !DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId)) // 저장 데이터 및 지원 던전 확인
            {
                return false; // 잘못된 던전 미클리어 반환
            }

            return saveData.HasStoryFlag(ClearFlagPrefix + dungeonId); // 던전 클리어 시스템 플래그 확인
        }

        public static bool MarkCleared(SaveData saveData, string dungeonId) // 던전 최초 클리어 기록
        {
            if (saveData == null || !DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId)) // 저장 데이터 및 지원 던전 확인
            {
                return false; // 잘못된 던전 기록 실패
            }

            if (IsCleared(saveData, dungeonId)) // 기존 클리어 여부 확인
            {
                return false; // 중복 클리어 기록 차단
            }

            return saveData.SetStoryFlag(ClearFlagPrefix + dungeonId); // 던전 클리어 시스템 플래그 저장
        }
    }
}
