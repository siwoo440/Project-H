using System; // 문자열 및 숫자 변환 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleProgressSaveAdapter // 기존 저장 구조 기반 전투 진행 저장 호환 기능
    {
        private const string GoldFlagPrefix = "SYS_BATTLE_GOLD:"; // 골드 저장용 시스템 플래그 접두사
        private const string CommitFlagPrefix = "SYS_BATTLE_RESULT:"; // 전투 결과 반영 기록 접두사

        public static int GetGold(SaveData saveData) // 현재 저장 골드 조회
        {
            if (saveData == null) // 저장 데이터 존재 확인
            {
                return 0; // 저장 데이터 없음 골드 반환
            }

            saveData.EnsureDefaults(); // 이전 저장 데이터 기본값 보정
            int resolvedGold = 0; // 조회 골드 초기화
            IReadOnlyList<string> flags = saveData.StoryFlags; // 현재 저장 플래그 조회

            for (int index = 0; index < flags.Count; index++) // 저장 플래그 순회
            {
                string flag = flags[index]; // 현재 플래그 조회

                if (string.IsNullOrEmpty(flag) || !flag.StartsWith(GoldFlagPrefix, StringComparison.Ordinal)) // 골드 시스템 플래그 여부 확인
                {
                    continue; // 일반 플래그 제외
                }

                string valueText = flag.Substring(GoldFlagPrefix.Length); // 골드 숫자 문자열 분리

                if (int.TryParse(valueText, out int parsedGold)) // 골드 숫자 변환 확인
                {
                    resolvedGold = Math.Max(resolvedGold, Math.Max(0, parsedGold)); // 유효 골드 최댓값 보존
                }
            }

            return resolvedGold; // 현재 저장 골드 반환
        }

        public static int AddGold(SaveData saveData, int amount) // 저장 골드 증가
        {
            if (saveData == null || amount <= 0) // 저장 데이터 및 증가량 확인
            {
                return GetGold(saveData); // 기존 골드 유지 반환
            }

            int currentGold = GetGold(saveData); // 기존 골드 조회
            long summedGold = (long)currentGold + amount; // 정수 오버플로 방지 합산
            int nextGold = summedGold > int.MaxValue ? int.MaxValue : (int)summedGold; // 최대 정수 범위 보정
            RemoveGoldFlags(saveData); // 기존 골드 시스템 플래그 제거
            saveData.SetStoryFlag(GoldFlagPrefix + nextGold); // 신규 골드 시스템 플래그 저장
            return nextGold; // 변경 골드 반환
        }

        public static bool HasBattleResultCommit(SaveData saveData, string resultId) // 전투 결과 반영 여부 확인
        {
            if (saveData == null || string.IsNullOrWhiteSpace(resultId)) // 저장 데이터 및 결과 ID 확인
            {
                return false; // 잘못된 결과 미반영 반환
            }

            return saveData.HasStoryFlag(CommitFlagPrefix + resultId); // 결과 반영 시스템 플래그 확인
        }

        public static bool MarkBattleResultCommitted(SaveData saveData, string resultId) // 전투 결과 반영 완료 기록
        {
            if (saveData == null || string.IsNullOrWhiteSpace(resultId)) // 저장 데이터 및 결과 ID 확인
            {
                return false; // 잘못된 결과 기록 실패
            }

            return saveData.SetStoryFlag(CommitFlagPrefix + resultId); // 결과 반영 시스템 플래그 저장
        }

        private static void RemoveGoldFlags(SaveData saveData) // 기존 골드 시스템 플래그 제거
        {
            List<string> targets = new List<string>(); // 제거 대상 플래그 목록 생성
            IReadOnlyList<string> flags = saveData.StoryFlags; // 현재 저장 플래그 조회

            for (int index = 0; index < flags.Count; index++) // 저장 플래그 순회
            {
                string flag = flags[index]; // 현재 플래그 조회

                if (!string.IsNullOrEmpty(flag) && flag.StartsWith(GoldFlagPrefix, StringComparison.Ordinal)) // 골드 시스템 플래그 여부 확인
                {
                    targets.Add(flag); // 제거 대상 플래그 추가
                }
            }

            for (int index = 0; index < targets.Count; index++) // 제거 대상 플래그 순회
            {
                saveData.RemoveStoryFlag(targets[index]); // 기존 골드 시스템 플래그 제거
            }
        }
    }
}
