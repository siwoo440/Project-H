using System; // 문자열 비교 및 파싱 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 저장 데이터 기능
using ProjectH.UI; // 지원 던전 ID 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class DungeonProgressSaveAdapter // 던전 클리어 영구 저장 호환 기능
    {
        private const string ClearFlagPrefix = "SYS_DUNGEON_CLEAR:"; // 던전 클리어 시스템 플래그 접두사
        private const string StarsFlagPrefix = "SYS_DUNGEON_STARS:"; // 던전 최고 별점 시스템 플래그 접두사
        public const int MaxStars = 3; // 던전 최고 별점 상한 (Day47)

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

        public static int GetBestStars(SaveData saveData, string dungeonId) // 던전 최고 별점 조회 (Day47)
        {
            if (saveData == null || !DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId)) // 저장 데이터 및 지원 던전 확인
            {
                return 0; // 잘못된 던전 별점 없음 반환
            }

            string keyPrefix = StarsFlagPrefix + dungeonId + ":"; // 대상 던전 별점 플래그 접두사 계산
            int best = 0; // 최고 별점 초기화
            IReadOnlyList<string> flags = saveData.StoryFlags; // 현재 저장 플래그 조회

            for (int index = 0; index < flags.Count; index++) // 저장 플래그 순회
            {
                string flag = flags[index]; // 현재 플래그 조회

                if (string.IsNullOrEmpty(flag) || !flag.StartsWith(keyPrefix, StringComparison.Ordinal)) // 별점 플래그 여부 확인
                {
                    continue; // 다른 플래그 제외
                }

                string valueText = flag.Substring(keyPrefix.Length); // 별점 숫자 문자열 분리

                if (int.TryParse(valueText, out int parsedStars)) // 별점 숫자 변환 확인
                {
                    best = Mathf.Max(best, parsedStars); // 유효 별점 최댓값 보존
                }
            }

            return Mathf.Clamp(best, 0, MaxStars); // 최고 별점 범위 보정 후 반환
        }

        public static bool TrySetBestStars(SaveData saveData, string dungeonId, int stars) // 던전 최고 별점 갱신 시도 (Day47)
        {
            if (saveData == null || !DungeonSelectionRuntimeState.IsSupportedDungeonId(dungeonId)) // 저장 데이터 및 지원 던전 확인
            {
                return false; // 잘못된 던전 갱신 실패
            }

            int clampedStars = Mathf.Clamp(stars, 0, MaxStars); // 신규 별점 범위 보정
            int currentBest = GetBestStars(saveData, dungeonId); // 기존 최고 별점 조회

            if (clampedStars <= currentBest) // 기존 기록 대비 향상 여부 확인
            {
                return false; // 기존 기록 미달 갱신 차단
            }

            string keyPrefix = StarsFlagPrefix + dungeonId + ":"; // 대상 던전 별점 플래그 접두사 계산
            List<string> targets = new List<string>(); // 제거 대상 플래그 목록 생성
            IReadOnlyList<string> flags = saveData.StoryFlags; // 현재 저장 플래그 조회

            for (int index = 0; index < flags.Count; index++) // 저장 플래그 순회
            {
                string flag = flags[index]; // 현재 플래그 조회

                if (!string.IsNullOrEmpty(flag) && flag.StartsWith(keyPrefix, StringComparison.Ordinal)) // 기존 별점 플래그 확인
                {
                    targets.Add(flag); // 제거 대상 추가
                }
            }

            for (int index = 0; index < targets.Count; index++) // 제거 대상 순회
            {
                saveData.RemoveStoryFlag(targets[index]); // 기존 별점 플래그 제거
            }

            saveData.SetStoryFlag(keyPrefix + clampedStars); // 신규 최고 별점 플래그 저장
            return true; // 별점 갱신 성공 반환
        }
    }
}
