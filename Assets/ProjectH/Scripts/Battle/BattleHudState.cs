using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleHudHealthState // 전투 HUD 체력 상태
    {
        Normal = 0, // 정상 체력 상태
        Low = 1, // 낮은 체력 상태
        Danger = 2, // 위험 체력 상태
        Down = 3 // 전투 불능 상태
    }

    public static class BattleHudHealthStateEvaluator // HUD 체력 상태 판정 기능
    {
        public static BattleHudHealthState Evaluate(int currentHp, int maxHp) // 현재 체력 기반 HUD 상태 계산
        {
            if (currentHp <= 0 || maxHp <= 0) // 전투 불능 또는 잘못된 최대 체력 확인
            {
                return BattleHudHealthState.Down; // 전투 불능 상태 반환
            }

            float ratio = Mathf.Clamp01((float)currentHp / maxHp); // 현재 체력 비율 계산

            if (ratio <= 0.25f) // 위험 체력 구간 확인
            {
                return BattleHudHealthState.Danger; // 위험 체력 상태 반환
            }

            if (ratio <= 0.50f) // 낮은 체력 구간 확인
            {
                return BattleHudHealthState.Low; // 낮은 체력 상태 반환
            }

            return BattleHudHealthState.Normal; // 정상 체력 상태 반환
        }

        public static string GetLabel(BattleHudHealthState state) // HUD 체력 상태 문구 반환
        {
            switch (state) // HUD 체력 상태 분기
            {
                case BattleHudHealthState.Low: // 낮은 체력 상태 처리
                    return "LOW"; // 낮은 체력 문구 반환
                case BattleHudHealthState.Danger: // 위험 체력 상태 처리
                    return "DANGER"; // 위험 체력 문구 반환
                case BattleHudHealthState.Down: // 전투 불능 상태 처리
                    return "DOWN"; // 전투 불능 문구 반환
                default: // 정상 체력 상태 처리
                    return "HP OK"; // 정상 체력 문구 반환
            }
        }
    }
}
