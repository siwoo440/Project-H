using System; // 수학 기능
using ProjectH.Data; // 장비 슬롯 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum ScrollGrade // 강화 주문서 등급 (Day61 신규 — 목업 'C' 표기)
    {
        C = 0, // 기본 주문서
        B = 1, // 중급 주문서 (+15% 확률)
        A = 2 // 상급 주문서 (+30% 확률)
    }

    public static class EquipmentUpgradeCatalog // 장비 강화·초월 수치 (Day61 신규 — 기획서 8.7·장비 초월, 수치는 이 파일에서 조정)
    {
        public const int MaxEnhanceLevel = 5; // 최대 강화 +5 (기획서 탭 2)
        public const int MaxTranscendStage = 3; // 최대 초월 ★3
        public const float EnhanceBonusPerLevel = 0.10f; // 강화 1단계당 장비 옵션 +10%
        public const float FailStreakBonus = 0.10f; // 실패할 때마다 다음 확률 +10% (기획서 '보정치 누적')
        private static readonly int[] EnhanceGold = { 500, 1000, 1500, 2000, 2500 }; // +1~+5 골드 (기획서 탭 2 표)
        private static readonly float[] BaseChance = { 1.00f, 1.00f, 0.90f, 0.75f, 0.60f }; // +1~+5 기본 성공 확률 (C 주문서)
        private static readonly float[] ScrollBonus = { 0f, 0.15f, 0.30f }; // 주문서 등급 추가 확률 (C·B·A)
        private static readonly int[] TranscendGold = { 20000, 30000 }; // ★1→★2, ★2→★3 골드 (기획서 원문)
        private static readonly float[] TranscendMultiplier = { 1.0f, 1.5f, 2.0f }; // ★1·★2·★3 기본 능력치 배율

        public static int GetEnhanceGold(int currentLevel) => currentLevel >= MaxEnhanceLevel ? 0 : EnhanceGold[Math.Max(0, currentLevel)]; // 다음 강화 골드
        public static int GetTranscendGold(int currentStage) => currentStage >= MaxTranscendStage ? 0 : TranscendGold[Math.Max(1, currentStage) - 1]; // 다음 초월 골드

        public static float GetSuccessChance(int currentLevel, ScrollGrade grade, int failStreak) // 성공 확률 (기본 + 주문서 + 실패 보정, 최대 100%)
        {
            if (currentLevel >= MaxEnhanceLevel) return 0f; // 최대 강화
            float chance = BaseChance[Math.Max(0, currentLevel)] + ScrollBonus[(int)grade] + (FailStreakBonus * Math.Max(0, failStreak)); // 합산
            return Math.Min(1f, chance); // 100% 상한
        }

        public static float GetTranscendMultiplier(int stage) => TranscendMultiplier[Math.Max(1, Math.Min(MaxTranscendStage, stage)) - 1]; // 초월 배율
        public static float GetStatMultiplier(int enhanceLevel, int transcendStage) => GetTranscendMultiplier(transcendStage) * (1f + (EnhanceBonusPerLevel * Math.Max(0, Math.Min(MaxEnhanceLevel, enhanceLevel)))); // 최종 옵션 배율 (★3 +5 = 3배)
        public static float GetStatMultiplier(EquipmentInstanceSaveData instance) => instance == null ? 1f : GetStatMultiplier(instance.EnhanceLevel, instance.TranscendStage); // 인스턴스 배율

        public static bool IsWeaponScrollSlot(EquipmentSlot slot) => slot == EquipmentSlot.Weapon; // 무기 주문서 대상 (나머지 4칸은 방어구 주문서)

        public static string GetScrollItemId(EquipmentSlot slot, ScrollGrade grade) => $"IT_SCROLL_{(IsWeaponScrollSlot(slot) ? "WEAPON" : "ARMOR")}_{grade}"; // 슬롯·등급별 주문서 ID

        public static bool TryParseScroll(string itemId, out bool weapon, out ScrollGrade grade) // 주문서 ID 해석
        {
            weapon = false; // 결과 초기화
            grade = ScrollGrade.C; // 결과 초기화

            if (string.IsNullOrEmpty(itemId) || !itemId.StartsWith("IT_SCROLL_", StringComparison.Ordinal)) // 주문서 확인
            {
                return false; // 주문서 아님
            }

            weapon = itemId.StartsWith("IT_SCROLL_WEAPON_", StringComparison.Ordinal); // 무기 주문서 여부
            return Enum.TryParse(itemId.Substring(itemId.LastIndexOf('_') + 1), out grade); // 등급 해석
        }

        public static string FormatName(string displayName, EquipmentInstanceSaveData instance) // 장비 이름 + 강화·초월 표시 (예: 철제 검 +3 ★2)
        {
            if (instance == null) return displayName; // 인스턴스 없음
            string plus = instance.EnhanceLevel > 0 ? $" +{instance.EnhanceLevel}" : string.Empty; // 강화 표시
            string star = instance.TranscendStage > 1 ? $" ★{instance.TranscendStage}" : string.Empty; // 초월 표시 (★1은 생략)
            return displayName + plus + star; // 이름 반환
        }
    }
}
