using System.Collections.Generic; // 목록 자료형
using System.Linq; // 목록 검색 기능
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Minigame; // 놀이판 기능
using ProjectH.SaveSystem; // 저장·골드 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class MarketMinigameTests // Day70 시장 야시장 놀이판 5종 테스트
    {
        private static SaveData CreateSave(int gold = 10000) // 골드를 가진 새 게임
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 새 게임
            GoldCurrencyService.TrySpendGold(saveData, GoldCurrencyService.GetGold(saveData), out _); // 시작 골드 비우기
            GoldCurrencyService.AddGold(saveData, gold); // 골드 지급
            return saveData; // 저장 반환
        }

        [Test] // 놀이 5종은 이름·능력 축·규칙이 모두 다르다
        public void Games_AreFiveDistinctSkills() // 구성 테스트
        {
            Assert.That(MinigameCatalog.All.Count, Is.EqualTo(5)); // 5종
            List<string> skills = MinigameCatalog.All.Select(game => game.SkillLabel).ToList(); // 능력 축
            Assert.That(skills.Distinct().Count(), Is.EqualTo(5), "능력 축이 겹치면 안 됩니다"); // 전부 다름
            Assert.That(MinigameCatalog.All.Select(game => game.Name).Distinct().Count(), Is.EqualTo(5)); // 이름도 전부 다름

            foreach (MinigameDefinition definition in MinigameCatalog.All) // 전체 순회
            {
                Assert.That(definition.Rule, Is.Not.Empty, definition.Name); // 규칙 설명
                Assert.That(MinigameCatalog.Get(definition.Kind), Is.SameAs(definition)); // 조회 일치
            }

            Assert.That(MinigameCatalog.All.Count(game => game.UsesBet), Is.EqualTo(4)); // 배팅 4종
            Assert.That(MinigameCatalog.Get(MinigameKind.CargoBalance).UsesBet, Is.False); // 마차 균형만 재료 보상
        }

        [Test] // 배당표 : 점수가 오르면 배당도 오르고, 틀리면 규칙대로 깎인다
        public void Payout_RisesWithScoreAndPunishesBust() // 배당 테스트
        {
            for (int score = 1; score <= MinigameCatalog.CardPairCount; score++) // 카드 뒤집기
            {
                Assert.That(MinigameCatalog.GetPayout(MinigameKind.CardMatch, score, false), Is.GreaterThan(MinigameCatalog.GetPayout(MinigameKind.CardMatch, score - 1, false))); // 단조 증가
            }

            Assert.That(MinigameCatalog.GetPayout(MinigameKind.Dart, 150, false), Is.EqualTo(2.5f).Within(0.001f)); // 다트 만점
            Assert.That(MinigameCatalog.GetPayout(MinigameKind.ShellGame, 3, false), Is.EqualTo(5f).Within(0.001f)); // 야바위 3판
            Assert.That(MinigameCatalog.GetPayout(MinigameKind.ShellGame, 2, true), Is.EqualTo(MinigameCatalog.GetPayout(MinigameKind.ShellGame, 2, false) * 0.5f).Within(0.001f)); // 틀려도 절반
            Assert.That(MinigameCatalog.GetPayout(MinigameKind.HighLow, 4, true), Is.EqualTo(0f)); // 내기는 틀리면 전액 손실
            Assert.That(MinigameCatalog.GetPayout(MinigameKind.HighLow, MinigameCatalog.HighLowMaxStreak, false), Is.EqualTo(4f).Within(0.001f)); // 5연속 최고 배당
            Assert.That(MinigameCatalog.ClampBet(0), Is.EqualTo(MinigameCatalog.MinBet)); // 하한
            Assert.That(MinigameCatalog.ClampBet(99999), Is.EqualTo(MinigameCatalog.MaxBet)); // 상한
            Assert.That(MinigameCatalog.ClampBet(250), Is.EqualTo(200)); // 100 단위
        }

        [Test] // 하루 3회 · 5종 합산 · 날짜가 바뀌면 회복
        public void DailyLimit_IsSharedAcrossGamesAndResetsNextDay() // 하루 횟수 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            Assert.That(MinigameService.GetRemainingPlays(saveData), Is.EqualTo(MinigameCatalog.DailyPlayLimit)); // 3회
            MinigameService.Settle(saveData, null, MinigameKind.CardMatch, 100, 6, false); // 1회
            MinigameService.Settle(saveData, null, MinigameKind.Dart, 100, 150, false); // 2회
            MinigameService.Settle(saveData, null, MinigameKind.ShellGame, 100, 3, false); // 3회
            Assert.That(MinigameService.GetRemainingPlays(saveData), Is.EqualTo(0)); // 소진
            Assert.That(MinigameService.CanPlay(saveData, MinigameKind.CardMatch, 100, out string reason), Is.False); // 더 못 놈
            Assert.That(reason, Does.Contain("내일")); // 안내
            int today = GameTimeService.GetCurrentDay(saveData); // 오늘
            while (GameTimeService.GetCurrentDay(saveData) == today) GameTimeService.AdvanceTime(saveData); // 다음 날 아침까지
            Assert.That(MinigameService.GetRemainingPlays(saveData), Is.EqualTo(MinigameCatalog.DailyPlayLimit)); // 회복
        }

        [Test] // 배팅 결과가 골드에 그대로 반영된다
        public void Settle_AppliesGoldChange() // 골드 반영 테스트
        {
            SaveData saveData = CreateSave(5000); // 5000G
            saveData.SetStoryFlag(MinigameCatalog.MarketGameFlag); // 뒷골목 놀이판 해금 (상인의 내기)
            MinigameResult win = MinigameService.Settle(saveData, null, MinigameKind.ShellGame, 500, 3, false); // 야바위 3판 성공 (5배)
            Assert.That(win.Bet, Is.EqualTo(500)); // 배팅
            Assert.That(win.GoldChange, Is.EqualTo(2000)); // 2500 - 500
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(7000)); // 반영

            MinigameResult lose = MinigameService.Settle(saveData, null, MinigameKind.HighLow, 500, 0, true); // 내기 실패
            Assert.That(lose.GoldChange, Is.EqualTo(-500)); // 전액 손실
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(6500)); // 반영
            Assert.That(saveData.MinigameBoard.Find((int)MinigameKind.ShellGame).BestGain, Is.EqualTo(2000)); // 최고 이득 기록
        }

        [Test] // 마차 균형은 골드를 걸지 않고 재료를 준다
        public void CargoBalance_GivesMaterialsWithoutBet() // 재료 보상 테스트
        {
            SaveData saveData = CreateSave(0); // 골드 0원이어도 가능
            Assert.That(MinigameService.CanPlay(saveData, MinigameKind.CargoBalance, 500, out _), Is.False); // 아직 잠김 (챕터 3)
            saveData.SetStoryFlag(MinigameCatalog.MarketGameFlag); // 해금
            Assert.That(MinigameService.CanPlay(saveData, MinigameKind.CargoBalance, 500, out _), Is.True); // 골드 없이 가능
            MinigameResult perfect = MinigameService.Settle(saveData, null, MinigameKind.CargoBalance, 500, 100, false); // 완벽 균형
            Assert.That(perfect.GoldChange, Is.EqualTo(0)); // 골드 변화 없음
            Assert.That(perfect.Shards, Is.EqualTo(5)); // 룬 조각
            Assert.That(perfect.Materials, Is.EqualTo(4)); // 강화 재료
            Assert.That(perfect.Scrolls, Is.EqualTo(1)); // 완벽 보너스
            MinigameResult poor = MinigameService.Settle(saveData, null, MinigameKind.CargoBalance, 500, 10, false); // 낮은 점수
            Assert.That(poor.Shards, Is.EqualTo(0)); // 보상 없음
            Assert.That(poor.Scrolls, Is.EqualTo(0)); // 보너스 없음
        }

        [Test] // 해금 : 카드·다트·야바위는 처음부터, 내기·마차는 챕터 3 완료 후
        public void Unlock_FollowsChapterThree() // 해금 테스트
        {
            SaveData saveData = CreateSave(); // 새 게임
            Assert.That(MinigameService.IsUnlocked(saveData, MinigameKind.CardMatch), Is.True); // 기본 개방
            Assert.That(MinigameService.IsUnlocked(saveData, MinigameKind.Dart), Is.True); // 기본 개방
            Assert.That(MinigameService.IsUnlocked(saveData, MinigameKind.ShellGame), Is.True); // 기본 개방
            Assert.That(MinigameService.IsUnlocked(saveData, MinigameKind.HighLow), Is.False); // 잠김
            Assert.That(MinigameService.IsUnlocked(saveData, MinigameKind.CargoBalance), Is.False); // 잠김
            saveData.SetStoryFlag(MinigameCatalog.MarketGameFlag); // 챕터 3 완료
            Assert.That(MinigameService.IsUnlocked(saveData, MinigameKind.HighLow), Is.True); // 열림
            Assert.That(MinigameService.IsUnlocked(saveData, MinigameKind.CargoBalance), Is.True); // 열림

            SaveData legacy = SaveData.CreateNewGame(new[] { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }); // 68일차 이전 저장
            ProjectH.Story.ChapterService.EnsureStarted(legacy); // 스토리 건너뜀 (완료 상태)
            Assert.That(MinigameService.IsUnlocked(legacy, MinigameKind.HighLow), Is.True); // 스토리를 다 본 저장도 열림
        }

        [Test] // 골드가 모자라면 시작할 수 없고, 실패한 시도는 횟수를 쓰지 않는다
        public void Settle_RejectsWhenGoldIsShort() // 거부 테스트
        {
            SaveData saveData = CreateSave(50); // 부족한 골드
            Assert.That(MinigameService.CanPlay(saveData, MinigameKind.CardMatch, 100, out string reason), Is.False); // 불가
            Assert.That(reason, Does.Contain("골드")); // 안내
            MinigameResult result = MinigameService.Settle(saveData, null, MinigameKind.CardMatch, 100, 6, false); // 시도
            Assert.That(result.GoldChange, Is.EqualTo(0)); // 변화 없음
            Assert.That(GoldCurrencyService.GetGold(saveData), Is.EqualTo(50)); // 그대로
            Assert.That(MinigameService.GetRemainingPlays(saveData), Is.EqualTo(MinigameCatalog.DailyPlayLimit)); // 횟수도 그대로
        }
    }
}
