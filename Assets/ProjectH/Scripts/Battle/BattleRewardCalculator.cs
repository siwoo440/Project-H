namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleRewardCalculator // 전투 결과 임시 보상 계산 기능
    {
        public const int VictoryGold = 120; // 24일차 임시 승리 골드
        public const int VictoryExperience = 80; // 24일차 임시 승리 경험치

        public static BattleReward Calculate(BattleOutcome outcome) // 승패 기반 임시 보상 계산
        {
            if (outcome != BattleOutcome.Victory) // 승리 여부 확인
            {
                return new BattleReward(0, 0); // 패배 및 비종료 상태 보상 없음 반환
            }

            return new BattleReward(VictoryGold, VictoryExperience); // 승리 임시 보상 반환
        }
    }
}
