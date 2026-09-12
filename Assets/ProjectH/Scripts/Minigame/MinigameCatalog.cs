using System; // 수학 기능
using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Minigame // 프로젝트 미니게임 영역
{
    public enum MinigameKind // 시장 놀이판 종류 (Day70 신규 — 능력 축이 서로 겹치지 않게 5종)
    {
        CardMatch = 0, // 카드 뒤집기 : 기억
        Dart = 1, // 다트 : 반사
        ShellGame = 2, // 야바위 : 관찰·추적
        HighLow = 3, // 상인의 내기 : 판단·위험 관리
        CargoBalance = 4 // 마차 균형 : 계산·퍼즐
    }

    public sealed class MinigameDefinition // 미니게임 한 종류 정의 (Day70 신규)
    {
        public MinigameKind Kind { get; } // 종류
        public string Name { get; } // 표시 이름
        public string SkillLabel { get; } // 필요한 능력 (겹치지 않는 축)
        public string Rule { get; } // 한 줄 규칙 설명
        public int MaxScore { get; } // 점수 상한
        public bool UsesBet { get; } // 골드를 걸어서 하는지 (마차 균형만 false)
        public string UnlockFlag { get; } // 해금 스토리 플래그 (빈 값이면 처음부터)
        public string UnlockText { get; } // 해금 조건 안내

        public MinigameDefinition(MinigameKind kind, string name, string skillLabel, string rule, int maxScore, bool usesBet, string unlockFlag = "", string unlockText = "") // 정의 생성
        {
            Kind = kind; // 종류 저장
            Name = name ?? string.Empty; // 이름 저장
            SkillLabel = skillLabel ?? string.Empty; // 능력 축 저장
            Rule = rule ?? string.Empty; // 규칙 저장
            MaxScore = Math.Max(1, maxScore); // 점수 상한 저장
            UsesBet = usesBet; // 배팅 여부 저장
            UnlockFlag = unlockFlag ?? string.Empty; // 해금 플래그 저장
            UnlockText = unlockText ?? string.Empty; // 해금 안내 저장
        }
    }

    public static class MinigameCatalog // 시장 놀이판 5종 수치 목록 (Day70 신규 — 배당·보상은 이 파일에서 조정)
    {
        public const int DailyPlayLimit = 3; // 하루 놀이 횟수 (5종 합산)
        public const int MinBet = 100; // 최소 배팅 골드
        public const int MaxBet = 500; // 최대 배팅 골드
        public const int BetStep = 100; // 배팅 조절 단위
        public const string MarketGameFlag = "MARKET_GAME_LICENSE"; // 뒷골목 놀이판 해금 플래그 (챕터 3 완료)
        public const string ShardItemId = "IT_RUNE_SHARD"; // 마차 균형 보상 : 룬 조각
        public const string MaterialItemId = "IT_MATERIAL_001"; // 마차 균형 보상 : 강화 재료
        public const string ScrollItemId = "IT_SCROLL_ARMOR_C"; // 마차 균형 완벽 보상 : 방어구 주문서

        public const int CardPairCount = 6; // 카드 뒤집기 짝 수 (12장)
        public const int CardMissLimit = 8; // 카드 뒤집기 실패 허용 횟수
        public const int DartThrowCount = 3; // 다트 던지는 횟수
        public const int DartMaxPerThrow = 50; // 다트 한 발 최고 점수
        public const int ShellRoundCount = 3; // 야바위 판 수
        public const int HighLowMaxStreak = 5; // 상인의 내기 최대 연속 성공
        public const int CargoCount = 8; // 마차 균형 화물 수
        public const int CargoSideCapacity = 5; // 마차 한쪽에 실을 수 있는 화물 수

        private static readonly float[] CardPayout = { 0f, 0.30f, 0.60f, 1.00f, 1.40f, 1.90f, 2.50f }; // 카드 뒤집기 : 맞춘 짝 0~6
        private static readonly float[] ShellPayout = { 0f, 1.50f, 2.50f, 5.00f }; // 야바위 : 성공한 판 0~3
        private static readonly float[] HighLowPayout = { 0f, 1.30f, 1.70f, 2.30f, 3.00f, 4.00f }; // 상인의 내기 : 연속 성공 0~5

        private static readonly MinigameDefinition[] Games = // 놀이판 5종
        {
            new MinigameDefinition(MinigameKind.CardMatch, "카드 뒤집기", "기억", "12장을 잠깐 보여 준 뒤 뒤집습니다. 같은 그림 6쌍을 찾으세요 (실패 8회까지).", CardPairCount, true), // 기억
            new MinigameDefinition(MinigameKind.Dart, "다트", "반사", "좌우로 움직이는 조준선을 과녁 한가운데에서 멈추세요. 3발을 던집니다.", DartThrowCount * DartMaxPerThrow, true), // 반사
            new MinigameDefinition(MinigameKind.ShellGame, "야바위", "관찰", "구슬이 든 컵을 눈으로 쫓으세요. 3판 연속이면 배당 5배, 틀려도 직전 배당의 절반은 남습니다.", ShellRoundCount, true), // 관찰
            new MinigameDefinition(MinigameKind.HighLow, "상인의 내기", "판단", "다음 카드가 위인지 아래인지 고릅니다. 멈추면 확정, 틀리면 전부 잃습니다 (최대 5연속).", HighLowMaxStreak, true, MarketGameFlag, "챕터 3을 마치면 뒷골목 놀이판이 열립니다."), // 판단
            new MinigameDefinition(MinigameKind.CargoBalance, "마차 균형", "계산", "화물을 좌우에 나눠 실어 무게를 맞춥니다. 골드를 걸지 않고 재료를 받습니다.", 100, false, MarketGameFlag, "챕터 3을 마치면 뒷골목 놀이판이 열립니다.") // 계산
        };

        public static IReadOnlyList<MinigameDefinition> All => Games; // 전체 놀이판 반환

        public static MinigameDefinition Get(MinigameKind kind) // 종류로 조회
        {
            for (int index = 0; index < Games.Length; index++) // 전체 순회
            {
                if (Games[index].Kind == kind) return Games[index]; // 일치 반환
            }

            return Games[0]; // 기본 반환
        }

        public static float GetPayout(MinigameKind kind, int score, bool busted) // 배팅 배당 배수 (0이면 전부 잃음)
        {
            switch (kind) // 종류 분기
            {
                case MinigameKind.CardMatch: return CardPayout[Clamp(score, CardPairCount)]; // 맞춘 짝만큼
                case MinigameKind.Dart: return Math.Min(2.50f, Math.Max(0, score) / 60f); // 150점 만점 = 2.5배
                case MinigameKind.ShellGame: return busted ? ShellPayout[Clamp(score, ShellRoundCount)] * 0.5f : ShellPayout[Clamp(score, ShellRoundCount)]; // 틀리면 직전 배당 절반
                case MinigameKind.HighLow: return busted ? 0f : HighLowPayout[Clamp(score, HighLowMaxStreak)]; // 틀리면 전부 잃음
                default: return 0f; // 마차 균형은 배당 없음 (재료 보상)
            }
        }

        public static int GetCargoShards(int score) => Math.Max(0, Math.Min(100, score)) / 20; // 마차 균형 룬 조각 (0~5)

        public static int GetCargoMaterials(int score) => Math.Max(0, Math.Min(100, score)) / 25; // 마차 균형 강화 재료 (0~4)

        public static bool IsCargoPerfect(int score) => score >= 100; // 완벽 균형 여부 (주문서 1장 추가)

        public static int ClampBet(int bet) // 배팅 금액 보정 (100 단위 · 100~500)
        {
            int stepped = Math.Max(MinBet, Math.Min(MaxBet, bet)) / BetStep * BetStep; // 단위 맞춤
            return Math.Max(MinBet, stepped); // 하한 보장
        }

        private static int Clamp(int score, int max) => Math.Max(0, Math.Min(max, score)); // 점수 범위 보정
    }
}
