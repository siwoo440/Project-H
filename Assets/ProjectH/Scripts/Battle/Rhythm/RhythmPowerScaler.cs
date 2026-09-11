using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day50)
{
    public static class RhythmPowerScaler // 리듬 챌린지 성적 기반 궁극기 위력 배율 계산 기능 (Day50 신규)
    {
        public const float MinMultiplier = 0.60f; // 전부 Miss 최저 위력 배율
        public const float BaseMultiplier = 1.00f; // 전부 Good 기준 위력 배율
        public const float MaxMultiplier = 1.50f; // 전부 Perfect 풀콤보 최대 위력 배율
        public const float FullComboBonus = 0.10f; // 풀콤보 달성 추가 위력 배율
        private const float AccuracyTopMultiplier = 1.40f; // 정확도 100퍼센트 기준 위력 배율 (풀콤보 보너스 전)

        public static float Evaluate(RhythmChallengeResult result) // 리듬 챌린지 결과 기반 궁극기 위력 배율 계산
        {
            if (result.TotalCount <= 0) // 판정 대상 원 존재 확인
            {
                return BaseMultiplier; // 챌린지 미진행 기본 배율 반환
            }

            float accuracy = Mathf.Clamp01(result.Accuracy); // 정확도 범위 보정 (Perfect 1점·Good 0.5점 기준)
            float multiplier = Mathf.Lerp(MinMultiplier, AccuracyTopMultiplier, accuracy); // 정확도 기반 선형 위력 배율 계산

            if (result.IsFullCombo) // 전 판정 성공 풀콤보 여부 확인
            {
                multiplier += FullComboBonus; // 풀콤보 추가 위력 배율 가산
            }

            return Mathf.Clamp(multiplier, MinMultiplier, MaxMultiplier); // 최저·최대 배율 범위 보정 후 반환
        }

        public static float Evaluate(RhythmChallengeResult result, float perfectBonusPerHit) // 결속 3단계 Perfect 보너스 포함 배율 (Day59 추가, 보너스 0이면 기존과 동일)
        {
            float multiplier = Evaluate(result); // 기본 배율 계산

            if (perfectBonusPerHit <= 0f || result.PerfectCount <= 0) // 보너스 적용 확인
            {
                return multiplier; // 기존 배율 반환
            }

            float bonus = Mathf.Min(ProjectH.SaveSystem.BondCatalog.PerfectBonusCap, result.PerfectCount * perfectBonusPerHit); // Perfect 수만큼 추가 (상한 +0.20)
            return Mathf.Min(MaxMultiplier + ProjectH.SaveSystem.BondCatalog.PerfectBonusCap, multiplier + bonus); // 최대 1.70 보정 후 반환
        }

        public static string GetSummaryLabel(float multiplier) // 위력 배율 표시 문구 반환
        {
            return $"POWER ×{multiplier:0.00}"; // 소수 둘째 자리 배율 문구 반환
        }
    }
}
