using System; // 문자열 비교 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using ProjectH.UI; // 지원 던전 ID 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum DungeonProgressState // 던전 진행 표시 상태
    {
        Locked = 0, // 잠금 상태
        Available = 1, // 진입 가능 상태
        Cleared = 2 // 클리어 상태
    }

    public static class DungeonProgressionPolicy // 임시 순차 던전 해금 규칙
    {
        public static DungeonProgressState GetState(SaveData saveData, string dungeonId) // 던전 진행 상태 계산
        {
            if (!DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId)) // 지원 던전 여부 확인
            {
                return DungeonProgressState.Locked; // 미지원 던전 잠금 반환
            }

            if (DungeonProgressSaveAdapter.IsCleared(saveData, dungeonId)) // 현재 던전 클리어 여부 확인
            {
                return DungeonProgressState.Cleared; // 클리어 상태 반환
            }

            return IsUnlocked(saveData, dungeonId) ? DungeonProgressState.Available : DungeonProgressState.Locked; // 해금 여부 기반 상태 반환
        }

        public static bool IsUnlocked(SaveData saveData, string dungeonId) // 던전 해금 여부 계산
        {
            if (!DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId)) // 지원 던전 여부 확인
            {
                return false; // 미지원 던전 잠금 반환
            }

            if (string.Equals(dungeonId, "DG001", StringComparison.Ordinal)) // 첫 던전 여부 확인
            {
                return true; // 첫 던전 기본 해금 반환
            }

            string previousDungeonId = GetPreviousDungeonId(dungeonId); // 이전 던전 ID 조회
            return !string.IsNullOrWhiteSpace(previousDungeonId) && DungeonProgressSaveAdapter.IsCleared(saveData, previousDungeonId); // 이전 던전 클리어 기반 해금 반환
        }

        public static string GetStatusLabel(DungeonProgressState state) // 던전 상태 표시 문구 반환
        {
            switch (state) // 던전 상태 분기
            {
                case DungeonProgressState.Cleared: // 클리어 상태 처리
                    return "CLEARED"; // 클리어 문구 반환
                case DungeonProgressState.Available: // 진입 가능 상태 처리
                    return "AVAILABLE"; // 진입 가능 문구 반환
                default: // 잠금 상태 처리
                    return "LOCKED"; // 잠금 문구 반환
            }
        }

        public static string GetLockReason(string dungeonId) // 던전 잠금 사유 반환
        {
            string previousDungeonId = GetPreviousDungeonId(dungeonId); // 이전 던전 ID 조회

            if (string.IsNullOrWhiteSpace(previousDungeonId)) // 이전 던전 존재 여부 확인
            {
                return "진입 조건을 확인할 수 없습니다."; // 기본 잠금 사유 반환
            }

            return $"{previousDungeonId} 클리어 필요"; // 이전 던전 클리어 요구 문구 반환
        }

        public static string GetPreviousDungeonId(string dungeonId) // 이전 던전 ID 반환
        {
            switch (dungeonId) // 던전 ID 분기
            {
                case "DG002": // 두 번째 던전 처리
                    return "DG001"; // 첫 던전 반환
                case "DG003": // 세 번째 던전 처리
                    return "DG002"; // 두 번째 던전 반환
                case "DG004": // 네 번째 던전 처리
                    return "DG003"; // 세 번째 던전 반환
                default: // 첫 던전 또는 미지원 처리
                    return string.Empty; // 이전 던전 없음 반환
            }
        }
    }
}
