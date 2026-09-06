using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 던전 데이터 기능
using ProjectH.UI; // 던전 선택 상태 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleRewardCalculator // 전투 결과 보상 계산 기능
    {
        public const int VictoryGold = 120; // 직접 전투 기본 승리 골드
        public const int VictoryExperience = 80; // 직접 전투 기본 승리 경험치

        public static BattleReward Calculate(BattleOutcome outcome) // 승패 및 선택 던전 기반 보상 계산
        {
            if (outcome != BattleOutcome.Victory) // 승리 여부 확인
            {
                return new BattleReward(0, 0); // 패배 및 비종료 상태 보상 없음 반환
            }

            string dungeonId = DungeonSelectionRuntimeState.SelectedDungeonId; // 현재 선택 던전 ID 조회

            if (!string.IsNullOrWhiteSpace(dungeonId) && GameManager.Instance != null && GameManager.Instance.Data != null && GameManager.Instance.Data.IsInitialized) // 던전 데이터 조회 가능 상태 확인
            {
                DungeonData dungeon = GameManager.Instance.Data.GetDungeon(dungeonId); // 선택 던전 데이터 조회

                if (dungeon != null) // 선택 던전 데이터 존재 확인
                {
                    return new BattleReward(dungeon.RewardGold, dungeon.RewardExp); // 선택 던전 실제 보상 반환
                }
            }

            return new BattleReward(VictoryGold, VictoryExperience); // 직접 Battle 실행 기본 보상 반환
        }
    }
}
