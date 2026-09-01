using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleGrowthFormula // 프로토타입 성장 공식
    {
        public const float GrowthPerLevel = 0.05f; // 레벨당 성장 비율

        public static float GetLevelMultiplier(int level) // 레벨 성장 배율 계산
        {
            int safeLevel = Mathf.Max(1, level); // 최소 레벨 보정
            return 1f + ((safeLevel - 1) * GrowthPerLevel); // 성장 배율 반환
        }

        public static int ScaleStat(int baseValue, int level) // 정수 스탯 성장 계산
        {
            int safeBaseValue = Mathf.Max(0, baseValue); // 음수 원본 수치 방지
            return Mathf.RoundToInt(safeBaseValue * GetLevelMultiplier(level)); // 성장 적용 수치 반환
        }
    }
}
