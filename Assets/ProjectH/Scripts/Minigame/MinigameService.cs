using System; // 수학 기능
using ProjectH.Data; // 아이템 데이터 기능
using ProjectH.SaveSystem; // 저장·골드·아이템·시간 기능

namespace ProjectH.Minigame // 프로젝트 미니게임 영역
{
    public sealed class MinigameResult // 놀이판 한 판의 결과 (Day70 신규)
    {
        public MinigameKind Kind { get; set; } // 놀이판 종류
        public int Bet { get; set; } // 건 골드
        public int Score { get; set; } // 점수
        public bool Busted { get; set; } // 중간에 틀렸는지 (야바위·상인의 내기)
        public float Payout { get; set; } // 배당 배수
        public int GoldChange { get; set; } // 골드 증감 (배당 - 배팅)
        public int Shards { get; set; } // 받은 룬 조각
        public int Materials { get; set; } // 받은 강화 재료
        public int Scrolls { get; set; } // 받은 주문서
        public string Message { get; set; } = string.Empty; // 결과 안내
    }

    public static class MinigameService // 시장 놀이판 진행·보상 (Day70 신규 — 하루 3회 · 5종 합산)
    {
        public static bool IsUnlocked(SaveData saveData, MinigameKind kind) // 해금 여부 (해금 플래그가 없는 3종은 처음부터)
        {
            MinigameDefinition definition = MinigameCatalog.Get(kind); // 정의 조회
            if (string.IsNullOrEmpty(definition.UnlockFlag)) return true; // 기본 개방
            if (saveData == null) return false; // 저장 확인
            return saveData.HasStoryFlag(definition.UnlockFlag) || saveData.ChapterProgress.IsFinished; // 플래그 또는 스토리 완료 저장 (68일차 이전 세이브 배려)
        }

        public static int GetRemainingPlays(SaveData saveData) // 오늘 남은 놀이 횟수
        {
            return saveData == null ? 0 : saveData.MinigameBoard.GetRemaining(GameTimeService.GetCurrentDay(saveData)); // 남은 횟수 반환
        }

        public static bool CanPlay(SaveData saveData, MinigameKind kind, int bet, out string reason) // 시작 가능 여부
        {
            MinigameDefinition definition = MinigameCatalog.Get(kind); // 정의 조회

            if (saveData == null) // 저장 확인
            {
                reason = "저장 데이터를 찾을 수 없습니다."; // 안내
                return false; // 불가
            }

            if (!IsUnlocked(saveData, kind)) // 해금 확인
            {
                reason = definition.UnlockText; // 안내
                return false; // 불가
            }

            if (GetRemainingPlays(saveData) <= 0) // 하루 횟수 확인
            {
                reason = $"오늘은 {MinigameCatalog.DailyPlayLimit}번 다 놀았어요. 내일 다시 오세요."; // 안내
                return false; // 불가
            }

            if (definition.UsesBet && GoldCurrencyService.GetGold(saveData) < MinigameCatalog.ClampBet(bet)) // 배팅 골드 확인
            {
                reason = $"골드가 부족합니다 · {MinigameCatalog.ClampBet(bet)}G 필요"; // 안내
                return false; // 불가
            }

            reason = string.Empty; // 사유 없음
            return true; // 가능
        }

        public static MinigameResult Settle(SaveData saveData, DataManager dataManager, MinigameKind kind, int bet, int score, bool busted) // 한 판 결과 반영 (골드·재료 지급, 오늘 횟수 소모)
        {
            MinigameDefinition definition = MinigameCatalog.Get(kind); // 정의 조회
            MinigameResult result = new MinigameResult { Kind = kind, Score = Math.Max(0, score), Busted = busted }; // 결과 생성

            if (!CanPlay(saveData, kind, bet, out string reason)) // 조건 재확인
            {
                result.Message = reason; // 실패 사유
                return result; // 보상 없음
            }

            saveData.MinigameBoard.ConsumePlay(GameTimeService.GetCurrentDay(saveData)); // 오늘 횟수 소모

            if (definition.UsesBet) // 배팅 놀이 (카드·다트·야바위·내기)
            {
                result.Bet = MinigameCatalog.ClampBet(bet); // 배팅 확정
                result.Payout = MinigameCatalog.GetPayout(kind, result.Score, busted); // 배당 배수
                int back = (int)Math.Round(result.Bet * result.Payout, MidpointRounding.AwayFromZero); // 돌려받는 골드
                result.GoldChange = back - result.Bet; // 순이익
                if (result.GoldChange > 0) GoldCurrencyService.AddGold(saveData, result.GoldChange); // 이득 지급
                else if (result.GoldChange < 0) GoldCurrencyService.TrySpendGold(saveData, -result.GoldChange, out _); // 손해 차감 (AddGold는 음수를 받지 않는다)
                result.Message = BuildBetMessage(definition, result); // 안내
            }
            else // 재료 놀이 (마차 균형)
            {
                result.Shards = MinigameCatalog.GetCargoShards(result.Score); // 룬 조각
                result.Materials = MinigameCatalog.GetCargoMaterials(result.Score); // 강화 재료
                result.Scrolls = MinigameCatalog.IsCargoPerfect(result.Score) ? 1 : 0; // 완벽 균형 보너스
                GrantItem(saveData, dataManager, MinigameCatalog.ShardItemId, result.Shards); // 조각 지급
                GrantItem(saveData, dataManager, MinigameCatalog.MaterialItemId, result.Materials); // 재료 지급
                GrantItem(saveData, dataManager, MinigameCatalog.ScrollItemId, result.Scrolls); // 주문서 지급
                result.Message = BuildCargoMessage(result); // 안내
            }

            saveData.MinigameBoard.Report(kind, result.Score, Math.Max(0, result.GoldChange)); // 기록 반영
            return result; // 결과 반환
        }

        public static string GetRemainingText(SaveData saveData) => $"오늘 남은 놀이 {GetRemainingPlays(saveData)}/{MinigameCatalog.DailyPlayLimit}회"; // 남은 횟수 문구

        private static void GrantItem(SaveData saveData, DataManager dataManager, string itemId, int count) // 아이템 지급 (0개면 건너뜀)
        {
            if (count <= 0) return; // 지급 없음
            ItemInventoryService.TryAdd(saveData, dataManager, itemId, count, out _); // 지급 (데이터가 없으면 조용히 무시)
        }

        private static string BuildBetMessage(MinigameDefinition definition, MinigameResult result) // 배팅 놀이 결과 문구
        {
            string head = result.GoldChange > 0 ? $"{definition.Name} 성공!" : result.GoldChange == 0 ? $"{definition.Name} 본전." : $"{definition.Name} 아쉽네요."; // 머리말
            string payout = $"배당 {result.Payout:0.0}배"; // 배당
            string gold = result.GoldChange >= 0 ? $"+{result.GoldChange}G" : $"{result.GoldChange}G"; // 손익
            return $"{head} {payout} · {gold}"; // 안내 반환
        }

        private static string BuildCargoMessage(MinigameResult result) // 마차 균형 결과 문구
        {
            if (result.Shards <= 0 && result.Materials <= 0) return $"마차 균형 {result.Score}점. 이번엔 삯을 받지 못했어요."; // 보상 없음
            string bonus = result.Scrolls > 0 ? " · 방어구 주문서 1 (완벽 균형!)" : string.Empty; // 완벽 보너스
            return $"마차 균형 {result.Score}점 · 룬 조각 {result.Shards} · 강화 재료 {result.Materials}{bonus}"; // 안내 반환
        }
    }
}
