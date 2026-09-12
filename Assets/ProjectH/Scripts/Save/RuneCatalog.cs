using System; // 수학 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 전투 포지션 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum RuneKind // 룬 15종 (Day60 신규 — 기획서 8.3 표 순서, 저장 값이므로 순서 변경 금지)
    {
        Power = 0, // 힘
        Critical = 1, // 치명타
        Frenzy = 2, // 광기
        Battle = 3, // 전투
        Vampire = 4, // 흡혈
        Guard = 5, // 수비
        Barrier = 6, // 보호막
        Thorns = 7, // 가시
        Swift = 8, // 신속
        Agility = 9, // 민첩
        Regen = 10, // 재생
        Vitality = 11, // 체력
        Ultimate = 12, // 궁극
        Skill = 13, // 스킬
        Revive = 14 // 부활
    }

    public enum RuneCategory // 룬 분류 (필터용)
    {
        Attack = 0, // 공격
        Defense = 1, // 방어
        Recovery = 2, // 회복
        Special = 3 // 특수
    }

    public sealed class RuneDefinition // 룬 한 종류 정의 (Day60 신규)
    {
        public RuneKind Kind { get; } // 종류
        public string Name { get; } // 이름
        public string Symbol { get; } // 아이콘 글자
        public RuneCategory Category { get; } // 분류
        public float[] GradeValues { get; } // ★1·★2·★3 효과 수치
        public bool Unique { get; } // 캐릭터당 1개만 장착 (기획서 '1개만 소지 가능')
        public string EffectFormat { get; } // 효과 문구 ({0} = 수치)
        public bool Percent { get; } // 백분율 표시 여부

        public RuneDefinition(RuneKind kind, string name, string symbol, RuneCategory category, float[] gradeValues, bool unique, string effectFormat, bool percent = true) // 정의 생성
        {
            Kind = kind; // 종류 저장
            Name = name; // 이름 저장
            Symbol = symbol; // 글자 저장
            Category = category; // 분류 저장
            GradeValues = gradeValues; // 수치 저장
            Unique = unique; // 고유 여부 저장
            EffectFormat = effectFormat; // 문구 저장
            Percent = percent; // 백분율 여부 저장
        }
    }

    public static class RuneCatalog // 룬 목록·수치·비용 (Day60 신규 — 수치는 이 파일에서 조정, 기획서 8.3·8.11~8.14·16.6)
    {
        public const int MaxGrade = 3; // 최고 등급 ★3
        public const int MaxLevel = 10; // 최대 강화 레벨 (기획서 8.13)
        public const int SlotCount = 5; // 캐릭터 룬 슬롯 수 (기획서 8.12 '3~5개')
        public const float LevelBonusPerLevel = 0.05f; // 레벨당 효과 증가율
        public const string ShardItemId = "IT_RUNE_SHARD"; // 룬 조각
        public const string Box1ItemId = "IT_RUNE_BOX_1"; // 하급 룬 상자 (★1)
        public const string Box2ItemId = "IT_RUNE_BOX_2"; // 중급 룬 상자 (★2)
        private static readonly int[] EnhanceShards = { 1, 2, 3, 4, 5, 6, 7, 8, 10 }; // Lv.2~10 룬 조각 (★1 기준)
        private static readonly int[] EnhanceGold = { 50, 100, 150, 200, 300, 400, 500, 600, 1000 }; // Lv.2~10 골드 (★1 기준)
        private static readonly int[] DismantleShards = { 2, 5, 12 }; // ★1·★2·★3 분해 조각
        private static readonly int[] SynthesisGold = { 100, 200 }; // ★1→★2, ★2→★3 합성 골드

        private static readonly RuneDefinition[] Definitions = // 기획서 8.3 룬 표 (★ 표시는 구조에 맞춰 조정한 룬)
        {
            new RuneDefinition(RuneKind.Power, "힘의 룬", "힘", RuneCategory.Attack, new[] { 0.10f, 0.15f, 0.20f }, false, "공격력 +{0}"), // 힘
            new RuneDefinition(RuneKind.Critical, "치명타의 룬", "치", RuneCategory.Attack, new[] { 0.10f, 0.20f, 0.30f }, false, "치명타율 +{0}"), // 치명타
            new RuneDefinition(RuneKind.Frenzy, "광기의 룬", "광", RuneCategory.Attack, new[] { 0.15f, 0.25f, 0.35f }, false, "체력 50% 이하일 때 공격력 +{0}"), // 광기
            new RuneDefinition(RuneKind.Battle, "전투의 룬", "전", RuneCategory.Attack, new[] { 0.10f, 0.20f, 0.30f }, false, "흐트러짐 누적 +{0}"), // ★ 전투 (보호막 피해 → 흐트러짐)
            new RuneDefinition(RuneKind.Vampire, "흡혈의 룬", "흡", RuneCategory.Attack, new[] { 0.03f, 0.06f, 0.09f }, false, "준 피해의 {0} 회복"), // ★ 흡혈 (피해량 기준)
            new RuneDefinition(RuneKind.Guard, "수비의 룬", "수", RuneCategory.Defense, new[] { 0.10f, 0.20f, 0.30f }, false, "방어력 +{0}"), // 수비
            new RuneDefinition(RuneKind.Barrier, "보호막의 룬", "막", RuneCategory.Defense, new[] { 0.03f, 0.06f, 0.09f }, false, "5초마다 최대 체력 {0} 보호막"), // 보호막
            new RuneDefinition(RuneKind.Thorns, "가시의 룬", "가", RuneCategory.Defense, new[] { 0.05f, 0.07f, 0.09f }, false, "피격 시 공격력의 {0} 반사"), // 가시
            new RuneDefinition(RuneKind.Swift, "신속의 룬", "속", RuneCategory.Attack, new[] { 0.10f, 0.20f, 0.30f }, false, "공격 속도 +{0}", false), // ★ 신속 (+1/2/3 → +0.1/0.2/0.3)
            new RuneDefinition(RuneKind.Agility, "민첩의 룬", "민", RuneCategory.Defense, new[] { 0.05f, 0.10f, 0.15f }, false, "적 공격 회피 {0}"), // 민첩
            new RuneDefinition(RuneKind.Regen, "재생의 룬", "재", RuneCategory.Recovery, new[] { 0.01f, 0.02f, 0.03f }, false, "매초 최대 체력 {0} 회복"), // 재생
            new RuneDefinition(RuneKind.Vitality, "체력의 룬", "체", RuneCategory.Defense, new[] { 0.05f, 0.10f, 0.15f }, false, "최대 체력 +{0}"), // 체력
            new RuneDefinition(RuneKind.Ultimate, "궁극의 룬", "궁", RuneCategory.Special, new[] { 0.15f, 0.20f, 0.25f }, true, "궁극기 게이지 충전 +{0}"), // ★ 궁극 (필요 횟수 -1 → 게이지 충전)
            new RuneDefinition(RuneKind.Skill, "스킬의 룬", "기", RuneCategory.Special, new[] { 0.10f, 0.15f, 0.20f }, true, "스킬 블록 생성 주기 -{0}"), // ★ 스킬 (생성 속도 +1 → 생성 주기 단축)
            new RuneDefinition(RuneKind.Revive, "부활의 룬", "부", RuneCategory.Special, new[] { 0.10f, 0.20f, 0.30f }, true, "쓰러지면 1회 체력 {0}로 부활") // 부활
        };

        public static IReadOnlyList<RuneDefinition> All => Definitions; // 전체 정의 반환
        public static int KindCount => Definitions.Length; // 종류 수

        public static RuneDefinition Get(RuneKind kind) => Definitions[(int)kind]; // 종류 정의 조회 (enum 순서 = 배열 순서)

        public static float GetValue(RuneKind kind, int grade, int level) // 등급·레벨 반영 최종 수치
        {
            float baseValue = Get(kind).GradeValues[Math.Max(1, Math.Min(MaxGrade, grade)) - 1]; // 등급 수치
            return baseValue * (1f + (LevelBonusPerLevel * (Math.Max(1, Math.Min(MaxLevel, level)) - 1))); // 레벨당 +5% 반영
        }

        public static string FormatValue(RuneKind kind, float value) // 수치 표시 문구
        {
            return Get(kind).Percent ? $"{value * 100f:0.#}%" : $"{value:0.##}"; // 백분율 또는 소수
        }

        public static string Describe(RuneKind kind, int grade, int level) // 효과 설명
        {
            return string.Format(Get(kind).EffectFormat, FormatValue(kind, GetValue(kind, grade, level))); // 문구 반환
        }

        public static string GetStars(int grade) => new string('★', Math.Max(1, Math.Min(MaxGrade, grade))) + new string('☆', MaxGrade - Math.Max(1, Math.Min(MaxGrade, grade))); // 별 표시

        public static int GetEnhanceShards(int grade, int currentLevel) => currentLevel >= MaxLevel ? 0 : EnhanceShards[currentLevel - 1] * GradeMultiplier(grade); // 다음 레벨 조각 비용
        public static int GetEnhanceGold(int grade, int currentLevel) => currentLevel >= MaxLevel ? 0 : EnhanceGold[currentLevel - 1] * GradeMultiplier(grade); // 다음 레벨 골드 비용
        public static int GetSynthesisGold(int grade) => grade >= MaxGrade ? 0 : SynthesisGold[grade - 1]; // 합성 골드
        public static int GetDismantleShards(int grade, int level) => DismantleShards[Math.Max(1, Math.Min(MaxGrade, grade)) - 1] + Math.Max(0, level - 1); // 분해 조각 (강화 레벨만큼 추가)
        private static int GradeMultiplier(int grade) => grade >= 3 ? 4 : grade == 2 ? 2 : 1; // 등급별 비용 배수 (×1·×2·×4)

        public static int GetInvestedShards(int grade, int level) // 강화에 들인 조각 합계 (합성 반환 계산용)
        {
            int total = 0; // 합계
            for (int lv = 1; lv < Math.Min(MaxLevel, level); lv++) total += GetEnhanceShards(grade, lv); // Lv.1→현재까지 누적
            return total; // 합계 반환
        }

        public static string GetSlotRequirement(int slotIndex) // 슬롯 해금 조건 문구 (기획서 8.12)
        {
            switch (slotIndex) // 슬롯 분기
            {
                case 0: return "기본"; // 1번 기본 슬롯
                case 1: return "레벨 10"; // 2번 레벨 슬롯
                case 2: return "결속 3단계"; // 3번 결속 슬롯
                case 3: return "고급 이상 장비"; // 4번 장비 슬롯
                default: return "전용 장비 ★2 초월"; // 5번 초월 슬롯 (Day70 — 기획서 8.12 원래 조건으로 교체)
            }
        }

        public static RuneKind[] GetRecommended(BattlePosition position) // 역할별 추천 룬 (기획서 11.12)
        {
            switch (position) // 포지션 분기
            {
                case BattlePosition.Tank: return new[] { RuneKind.Guard, RuneKind.Vitality, RuneKind.Thorns }; // 탱커
                case BattlePosition.Healer: return new[] { RuneKind.Regen, RuneKind.Barrier, RuneKind.Ultimate }; // 힐러
                default: return new[] { RuneKind.Power, RuneKind.Critical, RuneKind.Frenzy }; // 딜러
            }
        }

        public static bool IsRuneBox(string itemId) => itemId == Box1ItemId || itemId == Box2ItemId; // 룬 상자 여부
        public static int GetBoxGrade(string itemId) => itemId == Box2ItemId ? 2 : 1; // 상자 등급
    }

    public sealed class RuneLoadout // 캐릭터 장착 룬 효과 합계 (Day60 신규 — 전투 스탯·발동 효과 공용)
    {
        public static RuneLoadout Empty { get; } = new RuneLoadout(); // 빈 합계
        private readonly float[] values = new float[RuneCatalog.KindCount]; // 종류별 합계

        public float Get(RuneKind kind) => values[(int)kind]; // 종류별 합계 조회
        public bool Has(RuneKind kind) => values[(int)kind] > 0f; // 보유 여부

        public bool IsEmpty // 빈 합계 여부
        {
            get
            {
                for (int index = 0; index < values.Length; index++) if (values[index] > 0f) return false; // 값 존재 확인
                return true; // 비어 있음
            }
        }

        internal void Add(RuneKind kind, float value) // 합계 추가
        {
            values[(int)kind] += value; // 누적
        }
    }
}
