namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public enum BattleOutcome // 전투 승패 상태
    {
        Preparing = 0, // 전투 준비 상태
        Running = 1, // 전투 진행 상태
        Victory = 2, // 아군 승리 상태
        Defeat = 3 // 아군 패배 상태
    }

    public static class BattleOutcomeEvaluator // 팀별 생존 수 기반 승패 판정 기능
    {
        public static BattleOutcome Evaluate(int livingAllies, int livingEnemies) // 현재 생존 수 승패 판정
        {
            if (livingAllies <= 0) // 아군 전멸 여부 확인
            {
                return BattleOutcome.Defeat; // 아군 전멸 패배 반환
            }

            if (livingEnemies <= 0) // 적군 전멸 여부 확인
            {
                return BattleOutcome.Victory; // 적군 전멸 승리 반환
            }

            return BattleOutcome.Running; // 양팀 생존 전투 진행 반환
        }
    }
}
