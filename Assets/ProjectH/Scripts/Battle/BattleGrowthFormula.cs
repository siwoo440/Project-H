using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleGrowthFormula // 캐릭터 기본 스탯 성장 공식
    {
        public const int MinLevel = 1; // 최소 성장 적용 레벨
        public const int MaxLevel = CharacterLevelProgression.MaxLevel; // 최대 성장 적용 레벨
        public const float GrowthPerLevel = 0.08f; // 레벨당 성장 비율 (Day67 1차 밸런스 — 후반 던전 적 성장에 맞춰 5% → 8%)

        public static int NormalizeLevel(int level) // 성장 적용 레벨 범위 보정
        {
            return Mathf.Clamp(level, MinLevel, MaxLevel); // 최소·최대 레벨 범위 반환
        }

        public static float GetLevelMultiplier(int level) // 레벨 성장 배율 계산
        {
            int safeLevel = NormalizeLevel(level); // 성장 적용 레벨 보정
            return 1f + ((safeLevel - MinLevel) * GrowthPerLevel); // 선형 성장 배율 반환
        }

        public static int ScaleStat(int baseValue, int level) // 정수 스탯 성장 계산
        {
            int safeBaseValue = Mathf.Max(0, baseValue); // 음수 원본 수치 방지
            return Mathf.RoundToInt(safeBaseValue * GetLevelMultiplier(level)); // 성장 적용 수치 반환
        }
    }
}
