using System; // 수학 보정 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleReward // 전투 결과 임시 보상 값
    {
        public int Gold { get; } // 획득 골드 반환
        public int Experience { get; } // 획득 경험치 반환

        public BattleReward(int gold, int experience) // 전투 보상 생성
        {
            Gold = Math.Max(0, gold); // 골드 음수 방지
            Experience = Math.Max(0, experience); // 경험치 음수 방지
        }
    }
}
