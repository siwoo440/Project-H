using System; // 이벤트 및 함수 자료형
using System.Collections.Generic; // 읽기 전용 목록 자료형
using UnityEngine; // Unity 런타임 초기화 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DungeonSelectionRuntimeState // 던전 선택 런타임 상태
    {
        private static readonly string[] DungeonIds = { "DG001", "DG002", "DG003", "DG004", "DG005", "DG006", "DG007", "DG008", "DG009", "DG010", "DG011", "DG012", "DG013", "DG014" }; // 지원 던전 ID 목록 (Day65 노아르 · 실바란, Day66 카르니안 · 아스타르 + 지역별 두 번째 던전)
        private static string selectedDungeonId = string.Empty; // 현재 선택 던전 ID
        private static Func<string, bool> unlockEvaluator; // 현재 던전 해금 검사 함수

        public static IReadOnlyList<string> SupportedDungeonIds => DungeonIds; // 지원 던전 ID 목록 반환
        public static string SelectedDungeonId => selectedDungeonId; // 현재 선택 던전 ID 반환
        public static event Action<string> SelectionChanged; // 던전 선택 변경 이벤트

        public static void SetUnlockEvaluator(Func<string, bool> evaluator) // 던전 해금 검사 함수 설정
        {
            unlockEvaluator = evaluator; // 현재 해금 검사 함수 저장
        }

        public static bool TrySelect(string dungeonId, Func<string, bool> dungeonExists) // 던전 선택 시도
        {
            if (!IsSupportedDungeonId(dungeonId)) // 지원 던전 여부 확인
            {
                return false; // 미지원 선택 차단
            }

            if (dungeonExists == null || !dungeonExists(dungeonId)) // 던전 데이터 존재 확인
            {
                return false; // 누락 데이터 선택 차단
            }

            if (!IsUnlockedForSelection(dungeonId)) // 던전 해금 상태 확인
            {
                return false; // 잠긴 던전 선택 차단
            }

            if (string.Equals(selectedDungeonId, dungeonId, StringComparison.Ordinal)) // 동일 선택 여부 확인
            {
                return true; // 기존 선택 유지
            }

            selectedDungeonId = dungeonId; // 현재 선택 ID 저장
            SelectionChanged?.Invoke(selectedDungeonId); // 선택 변경 이벤트 발행
            return true; // 선택 성공 반환
        }

        public static bool CanEnter(Func<string, bool> dungeonExists) // 전투 진입 가능 여부 확인
        {
            if (string.IsNullOrWhiteSpace(selectedDungeonId)) // 선택 존재 확인
            {
                return false; // 미선택 진입 차단
            }

            if (!IsSupportedDungeonId(selectedDungeonId)) // 지원 던전 여부 확인
            {
                return false; // 미지원 선택 진입 차단
            }

            if (!IsUnlockedForSelection(selectedDungeonId)) // 현재 선택 던전 해금 상태 확인
            {
                return false; // 잠긴 선택 진입 차단
            }

            return dungeonExists != null && dungeonExists(selectedDungeonId); // 실제 던전 데이터 존재 결과 반환
        }

        public static bool IsSupportedDungeonId(string dungeonId) // 지원 던전 ID 여부 확인
        {
            if (string.IsNullOrWhiteSpace(dungeonId)) // 던전 ID 유효성 확인
            {
                return false; // 빈 ID 거부
            }

            for (int index = 0; index < DungeonIds.Length; index++) // 지원 던전 ID 순회
            {
                if (string.Equals(DungeonIds[index], dungeonId, StringComparison.Ordinal)) // 지원 ID 일치 확인
                {
                    return true; // 지원 ID 결과 반환
                }
            }

            return false; // 미지원 ID 결과 반환
        }

        public static void Clear() // 현재 던전 선택 해제
        {
            if (string.IsNullOrEmpty(selectedDungeonId)) // 기존 선택 여부 확인
            {
                return; // 중복 해제 중단
            }

            selectedDungeonId = string.Empty; // 선택 ID 초기화
            SelectionChanged?.Invoke(selectedDungeonId); // 선택 해제 이벤트 발행
        }

        public static void ResetAll() // 전체 선택 상태 초기화
        {
            selectedDungeonId = string.Empty; // 선택 ID 초기화
            unlockEvaluator = null; // 해금 검사 함수 초기화
        }

        private static bool IsUnlockedForSelection(string dungeonId) // 현재 던전 해금 여부 확인
        {
            return unlockEvaluator == null || unlockEvaluator(dungeonId); // 해금 검사 미설정 또는 허용 결과 반환
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 세션 상태 초기화 지정
        private static void ResetRuntime() // 플레이 세션 던전 선택 상태 초기화
        {
            selectedDungeonId = string.Empty; // 선택 ID 초기화
            unlockEvaluator = null; // 해금 검사 함수 초기화
            SelectionChanged = null; // 이전 이벤트 구독 해제
        }
    }
}
