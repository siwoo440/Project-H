using ProjectH.Core; // 프로젝트 핵심 기능
using ProjectH.Data; // 던전 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleRewardCalculator // 전투 결과 보상 계산 기능
    {
        public const int VictoryGold = 120; // 직접 전투 기본 승리 골드
        public const int VictoryExperience = 80; // 직접 전투 기본 승리 경험치

        public static BattleReward Calculate(BattleOutcome outcome) // 현재 전투 컨텍스트 기반 보상 계산
        {
            return Calculate(outcome, BattleContextRuntimeState.CurrentDungeonId); // 현재 전투 던전 기반 보상 반환
        }

        public static BattleReward Calculate(BattleOutcome outcome, string dungeonId) // 지정 던전 기반 보상 계산
        {
            if (outcome != BattleOutcome.Victory) // 승리 여부 확인
            {
                return new BattleReward(0, 0); // 패배 및 비종료 상태 보상 없음 반환
            }

            string resolvedDungeonId = BattleContextRuntimeState.ResolveDungeonId(dungeonId); // 보상 대상 던전 ID 보정

            if (GameManager.Instance != null && GameManager.Instance.Data != null && GameManager.Instance.Data.IsInitialized) // 던전 데이터 조회 가능 상태 확인
            {
                DungeonData dungeon = GameManager.Instance.Data.GetDungeon(resolvedDungeonId); // 전투 컨텍스트 던전 데이터 조회

                if (dungeon != null) // 전투 컨텍스트 던전 데이터 존재 확인
                {
                    return new BattleReward(ProjectH.Dungeon.RiftService.ApplyRewardBonus(dungeon.RewardGold), dungeon.RewardExp); // 전투 컨텍스트 실제 보상 반환 (Day67 — 긴급 균열이면 골드 1.5배)
                }
            }

            return new BattleReward(VictoryGold, VictoryExperience); // 직접 Battle 실행 기본 보상 반환
        }
    }
}
