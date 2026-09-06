using System; // 선택 변경 이벤트 기능
using UnityEngine; // Unity Runtime 초기화 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleSelectionRuntimeState // 전투 캐릭터 선택 Runtime 상태
    {
        private static string selectedRuntimeId = string.Empty; // 현재 선택 캐릭터 Runtime ID
        public static event Action<string> SelectionChanged; // 선택 캐릭터 변경 이벤트
        public static string SelectedRuntimeId => selectedRuntimeId; // 현재 선택 Runtime ID 반환

        public static bool IsSelected(string runtimeId) // 특정 Runtime ID 선택 여부 확인
        {
            return !string.IsNullOrWhiteSpace(runtimeId) && selectedRuntimeId == runtimeId; // 유효 Runtime ID 선택 일치 여부 반환
        }

        public static void SetSelected(string runtimeId) // 현재 선택 캐릭터 설정
        {
            string nextRuntimeId = string.IsNullOrWhiteSpace(runtimeId) ? string.Empty : runtimeId; // 안전한 선택 Runtime ID 계산

            if (selectedRuntimeId == nextRuntimeId) // 기존 선택과 동일 여부 확인
            {
                return; // 중복 선택 이벤트 차단
            }

            selectedRuntimeId = nextRuntimeId; // 현재 선택 Runtime ID 저장
            SelectionChanged?.Invoke(selectedRuntimeId); // 선택 변경 이벤트 전달
        }

        public static void Clear() // 현재 선택 해제
        {
            SetSelected(string.Empty); // 빈 Runtime ID로 선택 해제
        }

        public static void ResetAll() // 선택 Runtime 상태 전체 초기화
        {
            selectedRuntimeId = string.Empty; // 현재 선택 Runtime ID 초기화
            SelectionChanged = null; // 선택 변경 이벤트 구독 초기화
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // Play Mode 진입 Runtime 초기화 표시
        private static void ResetOnSubsystemRegistration() // Play Mode 진입 선택 상태 초기화
        {
            ResetAll(); // 이전 Play Mode 선택 상태 제거
        }
    }
}
