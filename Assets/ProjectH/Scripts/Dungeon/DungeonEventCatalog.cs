using System; // 난수 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 전투 Modifier 종류 기능

namespace ProjectH.Dungeon // 프로젝트 던전 탐험 영역 (Day55)
{
    public enum DungeonOutcomeKind // 노드 결과 종류 (Day55 신규)
    {
        Nothing = 0, // 아무 일 없음
        GainGold = 1, // 골드 획득
        SpendGold = 2, // 골드 소모
        NextBattleBuff = 3, // 다음 전투 아군 유리 효과
        NextBattleDebuff = 4 // 다음 전투 아군 불리 효과
    }

    public sealed class DungeonRunModifier // 다음 전투 시작 시 아군에게 거는 효과 (Day55 신규)
    {
        public string Label { get; } // 표시 문구
        public BattleRuntimeModifierKind Kind { get; } // 전투 Modifier 종류
        public float Value { get; } // 효과 수치
        public float Duration { get; } // 효과 지속시간
        public bool IsDebuff { get; } // 불리 효과 여부

        public DungeonRunModifier(string label, BattleRuntimeModifierKind kind, float value, float duration, bool isDebuff) // 탐험 효과 생성
        {
            Label = label ?? string.Empty; // 표시 문구 저장
            Kind = kind; // Modifier 종류 저장
            Value = value; // 효과 수치 저장
            Duration = duration; // 지속시간 저장
            IsDebuff = isDebuff; // 불리 효과 여부 저장
        }
    }

    public sealed class DungeonOutcome // 노드·선택지 결과 (Day55 신규)
    {
        public DungeonOutcomeKind Kind { get; } // 결과 종류
        public float GoldRatio { get; } // 던전 기본 골드 대비 획득·소모 비율
        public DungeonRunModifier Modifier { get; } // 다음 전투 효과
        public string Message { get; } // 결과 안내 문구

        public DungeonOutcome(DungeonOutcomeKind kind, float goldRatio, DungeonRunModifier modifier, string message) // 결과 생성
        {
            Kind = kind; // 결과 종류 저장
            GoldRatio = goldRatio; // 골드 비율 저장
            Modifier = modifier; // 다음 전투 효과 저장
            Message = message ?? string.Empty; // 안내 문구 저장
        }
    }

    public sealed class DungeonEventChoice // 이벤트 선택지 (Day55 신규)
    {
        public string Label { get; } // 선택지 문구
        public float SuccessChance { get; } // 성공 확률 (0~1)
        public DungeonOutcome Success { get; } // 성공 결과
        public DungeonOutcome Failure { get; } // 실패 결과
        public float RequiredGoldRatio { get; } // 선택에 필요한 골드 비율 (0이면 조건 없음)

        public DungeonEventChoice(string label, float successChance, DungeonOutcome success, DungeonOutcome failure, float requiredGoldRatio = 0f) // 선택지 생성
        {
            Label = label ?? string.Empty; // 선택지 문구 저장
            SuccessChance = Math.Max(0f, Math.Min(1f, successChance)); // 성공 확률 범위 보정
            Success = success; // 성공 결과 저장
            Failure = failure ?? success; // 실패 결과 저장 (없으면 성공 결과와 동일)
            RequiredGoldRatio = Math.Max(0f, requiredGoldRatio); // 필요 골드 비율 저장
        }

        public DungeonOutcome Roll(Random random) // 확률 기반 결과 결정
        {
            return random == null || random.NextDouble() < SuccessChance ? Success : Failure; // 성공 확률 추첨 결과 반환
        }
    }

    public sealed class DungeonEventDefinition // 던전 이벤트 정의 (Day55 신규)
    {
        public string Id { get; } // 이벤트 ID
        public string Title { get; } // 이벤트 제목
        public string Description { get; } // 이벤트 설명
        public IReadOnlyList<DungeonEventChoice> Choices { get; } // 선택지 목록

        public DungeonEventDefinition(string id, string title, string description, params DungeonEventChoice[] choices) // 이벤트 생성
        {
            Id = id ?? string.Empty; // 이벤트 ID 저장
            Title = title ?? string.Empty; // 제목 저장
            Description = description ?? string.Empty; // 설명 저장
            Choices = choices ?? new DungeonEventChoice[0]; // 선택지 저장
        }
    }

    public static class DungeonEventCatalog // 던전 이벤트·보물·함정·휴식 결과 목록 (Day55 신규)
    {
        public const float NextBattleEffectSeconds = 25f; // 다음 전투 효과 기본 지속시간
        public const float TreasureMinGoldRatio = 0.4f; // 보물 최소 골드 비율
        public const float TreasureMaxGoldRatio = 0.6f; // 보물 최대 골드 비율

        private static readonly DungeonOutcome NothingOutcome = new DungeonOutcome(DungeonOutcomeKind.Nothing, 0f, null, "아무 일도 일어나지 않았다."); // 공통 무사통과 결과

        private static readonly DungeonEventDefinition[] Events = // 이벤트 목록 (4종)
        {
            new DungeonEventDefinition("EVT_ALTAR", "수상한 제단", "침식의 흔적이 남은 제단이 희미하게 빛나고 있다.", // 수상한 제단
                new DungeonEventChoice("기도한다 (60%)", 0.6f,
                    Buff("제단의 가호 · 공격력 +25%", BattleRuntimeModifierKind.AttackPercent, 0.25f, "제단이 응답했다. 힘이 솟아오른다."),
                    Debuff("침식의 저주 · 명중률 -25%", BattleRuntimeModifierKind.AccuracyReductionPercent, 0.25f, "검은 안개가 시야를 가린다...")),
                new DungeonEventChoice("지나친다", 1f, NothingOutcome, null)),

            new DungeonEventDefinition("EVT_MERCHANT", "떠돌이 상인", "낡은 수레를 끄는 상인이 수상한 부적을 내민다.", // 떠돌이 상인
                new DungeonEventChoice("부적을 산다 (골드 소모)", 1f,
                    new DungeonOutcome(DungeonOutcomeKind.SpendGold, 0.4f, new DungeonRunModifier("수호 부적 · 방어력 +30%", BattleRuntimeModifierKind.DefensePercent, 0.3f, NextBattleEffectSeconds, false), "부적이 은은하게 파티를 감싼다."),
                    null, 0.4f),
                new DungeonEventChoice("거절한다", 1f, NothingOutcome, null)),

            new DungeonEventDefinition("EVT_CAMP", "무너진 야영지", "먼저 온 탐험가들의 흔적이다. 짐이 흩어져 있다.", // 무너진 야영지
                new DungeonEventChoice("짐을 뒤진다 (70%)", 0.7f,
                    new DungeonOutcome(DungeonOutcomeKind.GainGold, 0.6f, null, "쓸 만한 골드 주머니를 찾았다!"),
                    Debuff("덫에 걸림 · 공격 속도 -30%", BattleRuntimeModifierKind.AttackSpeedReductionPercent, 0.3f, "숨겨진 덫이 발목을 붙잡았다!")),
                new DungeonEventChoice("쉬어간다", 1f,
                    Buff("충분한 휴식 · 받는 피해 -15%", BattleRuntimeModifierKind.DamageReductionPercent, 0.15f, "잠시 숨을 고르며 전열을 정비했다."), null)),

            new DungeonEventDefinition("EVT_SPRING", "침식된 샘", "보랏빛으로 물든 샘물이 조용히 솟아오른다.", // 침식된 샘
                new DungeonEventChoice("마신다 (50%)", 0.5f,
                    Buff("샘의 활력 · 공격 속도 +25%", BattleRuntimeModifierKind.AttackSpeedPercent, 0.25f, "몸이 가벼워졌다!"),
                    Debuff("침식 중독 · 마법 저항 -25%", BattleRuntimeModifierKind.ResistanceReductionPercent, 0.25f, "속이 뒤틀린다. 마력에 취약해졌다...")),
                new DungeonEventChoice("떠난다", 1f, NothingOutcome, null))
        };

        private static readonly DungeonRunModifier[] TrapEffects = // 함정 효과 목록
        {
            new DungeonRunModifier("가시 덫 · 공격 속도 -30%", BattleRuntimeModifierKind.AttackSpeedReductionPercent, 0.3f, NextBattleEffectSeconds, true), // 둔화 함정
            new DungeonRunModifier("연막 함정 · 명중률 -25%", BattleRuntimeModifierKind.AccuracyReductionPercent, 0.25f, NextBattleEffectSeconds, true), // 실명 함정
            new DungeonRunModifier("침식 가스 · 마법 저항 -25%", BattleRuntimeModifierKind.ResistanceReductionPercent, 0.25f, NextBattleEffectSeconds, true) // 저항 감소 함정
        };

        private static readonly DungeonRunModifier[] RestEffects = // 휴식 효과 목록
        {
            new DungeonRunModifier("모닥불의 온기 · 공격력 +20%", BattleRuntimeModifierKind.AttackPercent, 0.2f, NextBattleEffectSeconds, false), // 공격 증가 휴식
            new DungeonRunModifier("장비 정비 · 방어력 +30%", BattleRuntimeModifierKind.DefensePercent, 0.3f, NextBattleEffectSeconds, false), // 방어 증가 휴식
            new DungeonRunModifier("전열 정비 · 받는 피해 -15%", BattleRuntimeModifierKind.DamageReductionPercent, 0.15f, NextBattleEffectSeconds, false) // 피해 감소 휴식
        };

        public static IReadOnlyList<DungeonEventDefinition> AllEvents => Events; // 전체 이벤트 목록 반환

        public static DungeonEventDefinition PickEvent(Random random) // 무작위 이벤트 선택
        {
            return Events[random == null ? 0 : random.Next(Events.Length)]; // 무작위 이벤트 반환
        }

        public static DungeonRunModifier PickTrap(Random random) // 무작위 함정 효과 선택
        {
            return TrapEffects[random == null ? 0 : random.Next(TrapEffects.Length)]; // 무작위 함정 효과 반환
        }

        public static DungeonRunModifier PickRest(Random random) // 무작위 휴식 효과 선택
        {
            return RestEffects[random == null ? 0 : random.Next(RestEffects.Length)]; // 무작위 휴식 효과 반환
        }

        public static float RollTreasureGoldRatio(Random random) // 보물 골드 비율 추첨
        {
            double t = random == null ? 0.5 : random.NextDouble(); // 0~1 보간 계수 추첨
            return (float)(TreasureMinGoldRatio + ((TreasureMaxGoldRatio - TreasureMinGoldRatio) * t)); // 최소~최대 비율 반환
        }

        private static DungeonOutcome Buff(string label, BattleRuntimeModifierKind kind, float value, string message) // 다음 전투 유리 결과 생성
        {
            return new DungeonOutcome(DungeonOutcomeKind.NextBattleBuff, 0f, new DungeonRunModifier(label, kind, value, NextBattleEffectSeconds, false), message); // 유리 결과 반환
        }

        private static DungeonOutcome Debuff(string label, BattleRuntimeModifierKind kind, float value, string message) // 다음 전투 불리 결과 생성
        {
            return new DungeonOutcome(DungeonOutcomeKind.NextBattleDebuff, 0f, new DungeonRunModifier(label, kind, value, NextBattleEffectSeconds, true), message); // 불리 결과 반환
        }
    }
}
