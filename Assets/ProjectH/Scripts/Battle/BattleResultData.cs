using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleResultData // 전투 종료 결과 데이터
    {
        private readonly List<BattleResultPartyMember> members; // 종료 파티원 스냅샷 목록
        public BattleOutcome Outcome { get; } // 최종 승패 상태 반환
        public int Gold { get; } // 획득 골드 반환
        public int Experience { get; } // 획득 경험치 반환
        public int StarCount { get; } // 결과 별 개수 반환
        public IReadOnlyList<BattleResultPartyMember> Members => members; // 종료 파티원 목록 반환

        private BattleResultData(BattleOutcome outcome, BattleReward reward, List<BattleResultPartyMember> partyMembers) // 전투 결과 데이터 생성
        {
            Outcome = outcome; // 최종 승패 저장
            Gold = reward == null ? 0 : reward.Gold; // 획득 골드 저장
            Experience = reward == null ? 0 : reward.Experience; // 획득 경험치 저장
            StarCount = outcome == BattleOutcome.Victory ? 3 : 0; // 임시 승리 별 세 개 설정
            members = partyMembers ?? new List<BattleResultPartyMember>(); // 파티원 스냅샷 저장
        }

        public static BattleResultData Create(BattleOutcome outcome, IReadOnlyList<BattleStats> battleMembers) // 현재 파티 상태 기반 전투 결과 생성
        {
            BattleReward reward = BattleRewardCalculator.Calculate(outcome); // 승패 기반 임시 보상 계산
            List<BattleResultPartyMember> snapshots = new List<BattleResultPartyMember>(); // 파티원 스냅샷 목록 생성

            if (battleMembers != null) // 현재 파티 목록 존재 확인
            {
                for (int index = 0; index < battleMembers.Count; index++) // 현재 파티원 순회
                {
                    BattleResultPartyMember snapshot = BattleResultPartyMember.Create(battleMembers[index]); // 현재 파티원 종료 상태 복사

                    if (snapshot != null) // 유효 파티원 스냅샷 확인
                    {
                        snapshots.Add(snapshot); // 결과 파티원 목록 추가
                    }
                }
            }

            return new BattleResultData(outcome, reward, snapshots); // 완성 전투 결과 데이터 반환
        }
    }
}
