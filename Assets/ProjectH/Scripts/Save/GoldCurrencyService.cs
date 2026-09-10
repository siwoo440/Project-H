using System; // 문자열 및 숫자 기능
using System.Collections.Generic; // 플래그 목록 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public static class GoldCurrencyService // Gold 재화 공통 처리 기능
    {
        private const string GoldFlagPrefix = "SYS_BATTLE_GOLD:"; // 기존 Gold 저장 플래그 접두사

        public static int GetGold(SaveData saveData) // 현재 Gold 조회
        {
            if (saveData == null) // 저장 데이터 확인
            {
                return 0; // 저장 데이터 없음 반환
            }

            saveData.EnsureDefaults(); // 저장 기본값 보정
            int resolvedGold = 0; // 조회 Gold 초기화
            IReadOnlyList<string> flags = saveData.StoryFlags; // 저장 플래그 조회

            for (int index = 0; index < flags.Count; index++) // 저장 플래그 순회
            {
                string flag = flags[index]; // 현재 플래그 조회

                if (string.IsNullOrEmpty(flag) || !flag.StartsWith(GoldFlagPrefix, StringComparison.Ordinal)) // Gold 플래그 여부 확인
                {
                    continue; // 다른 플래그 제외
                }

                string valueText = flag.Substring(GoldFlagPrefix.Length); // Gold 숫자 문자열 분리

                if (int.TryParse(valueText, out int parsedGold)) // Gold 숫자 변환 확인
                {
                    resolvedGold = Math.Max(resolvedGold, Math.Max(0, parsedGold)); // 유효 Gold 최댓값 보존
                }
            }

            return resolvedGold; // 현재 Gold 반환
        }

        public static int AddGold(SaveData saveData, int amount) // Gold 증가
        {
            if (saveData == null || amount <= 0) // 저장 데이터 및 증가량 확인
            {
                return GetGold(saveData); // 기존 Gold 유지 반환
            }

            int currentGold = GetGold(saveData); // 현재 Gold 조회
            long summedGold = (long)currentGold + amount; // 오버플로 방지 합산
            int nextGold = summedGold > int.MaxValue ? int.MaxValue : (int)summedGold; // 최대 정수 범위 보정
            SetGold(saveData, nextGold); // 합산 Gold 저장
            return nextGold; // 변경 Gold 반환
        }

        public static bool TrySpendGold(SaveData saveData, int amount, out string error) // Gold 안전 차감
        {
            error = string.Empty; // 오류 문구 초기화

            if (saveData == null) // 저장 데이터 확인
            {
                error = "SaveData가 없습니다."; // 저장 데이터 누락 오류
                return false; // 차감 실패 반환
            }

            if (amount <= 0) // 차감량 확인
            {
                error = "Gold 차감량은 1 이상이어야 합니다."; // 잘못된 차감량 오류
                return false; // 차감 실패 반환
            }

            int currentGold = GetGold(saveData); // 현재 Gold 조회

            if (currentGold < amount) // 잔액 부족 확인
            {
                error = $"Gold가 부족합니다. Current={currentGold}, Cost={amount}"; // 잔액 부족 오류
                return false; // 차감 실패 반환
            }

            SetGold(saveData, currentGold - amount); // 차감 후 Gold 저장
            return true; // 차감 성공 반환
        }

        private static void SetGold(SaveData saveData, int value) // Gold 절대값 저장
        {
            RemoveGoldFlags(saveData); // 기존 Gold 플래그 제거
            saveData.SetStoryFlag(GoldFlagPrefix + Math.Max(0, value)); // 신규 Gold 플래그 저장
        }

        private static void RemoveGoldFlags(SaveData saveData) // 기존 Gold 플래그 전체 제거
        {
            List<string> targets = new List<string>(); // 제거 대상 목록 생성
            IReadOnlyList<string> flags = saveData.StoryFlags; // 현재 플래그 조회

            for (int index = 0; index < flags.Count; index++) // 플래그 목록 순회
            {
                string flag = flags[index]; // 현재 플래그 조회

                if (!string.IsNullOrEmpty(flag) && flag.StartsWith(GoldFlagPrefix, StringComparison.Ordinal)) // Gold 플래그 확인
                {
                    targets.Add(flag); // 제거 대상 추가
                }
            }

            for (int index = 0; index < targets.Count; index++) // 제거 대상 순회
            {
                saveData.RemoveStoryFlag(targets[index]); // 기존 Gold 플래그 제거
            }
        }
    }
}
