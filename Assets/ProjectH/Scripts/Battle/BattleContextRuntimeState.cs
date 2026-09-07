using ProjectH.UI; // 던전 선택 상태 기능
using UnityEngine; // Unity 런타임 초기화 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleContextRuntimeState // 현재 전투 컨텍스트 상태
    {
        public const string DefaultDungeonId = "DG001"; // 직접 전투 기본 던전 ID
        private static string capturedDungeonId = string.Empty; // 확정 전투 던전 ID
        public static string DungeonId => capturedDungeonId; // 확정 전투 던전 ID 반환
        public static bool HasContext => !string.IsNullOrWhiteSpace(capturedDungeonId); // 전투 컨텍스트 존재 여부 반환
        public static bool UsedDirectFallback { get; private set; } // 직접 전투 기본값 사용 여부 반환

        public static string CurrentDungeonId // 현재 사용할 던전 ID 반환
        {
            get // 현재 던전 ID 조회
            {
                if (DungeonSelectionRuntimeState.IsSupportedDungeonId(capturedDungeonId)) // 확정 전투 던전 확인
                {
                    return capturedDungeonId; // 확정 전투 던전 반환
                }

                string selectedDungeonId = DungeonSelectionRuntimeState.SelectedDungeonId; // 현재 선택 던전 ID 조회

                if (DungeonSelectionRuntimeState.IsSupportedDungeonId(selectedDungeonId)) // 유효 선택 던전 확인
                {
                    return selectedDungeonId; // 선택 던전 반환
                }

                return DefaultDungeonId; // 직접 전투 기본 던전 반환
            }
        }

        public static string Capture(string dungeonId) // 전투 시작 던전 컨텍스트 확정
        {
            bool supported = DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId); // 지원 던전 여부 확인
            capturedDungeonId = supported ? dungeonId : DefaultDungeonId; // 전투 던전 ID 확정
            UsedDirectFallback = !supported; // 직접 전투 기본값 여부 저장
            return capturedDungeonId; // 확정 던전 ID 반환
        }

        public static string ResolveDungeonId(string dungeonId) // 던전 ID 안전 보정
        {
            return DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId) ? dungeonId : DefaultDungeonId; // 지원 던전 또는 기본 던전 반환
        }

        public static void ResetAll() // 전투 컨텍스트 전체 초기화
        {
            capturedDungeonId = string.Empty; // 확정 던전 ID 초기화
            UsedDirectFallback = false; // 직접 전투 표시 초기화
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 세션 전투 상태 초기화 지정
        private static void ResetRuntime() // 플레이 세션 전투 상태 초기화
        {
            ResetAll(); // 전투 컨텍스트 전체 초기화
        }
    }
}
