using System; // 고유 결과 ID 생성 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 게임 관리자 기능
using ProjectH.Data; // 던전 데이터 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public sealed class BattleResultData // 전투 종료 결과 데이터
    {
        private readonly List<BattleResultPartyMember> members; // 종료 파티원 스냅샷 목록
        private readonly List<DungeonDropResult> drops; // 실제 던전 드롭 결과 목록
        public string ResultId { get; } // 전투 결과 고유 ID 반환
        public string DungeonId { get; } // 전투 결과 던전 ID 반환
        public BattleOutcome Outcome { get; } // 최종 승패 상태 반환
        public int Gold { get; } // 획득 골드 반환
        public int Experience { get; } // 획득 경험치 반환
        public int StarCount { get; } // 결과 별 개수 반환
        public IReadOnlyList<BattleResultPartyMember> Members => members; // 종료 파티원 목록 반환
        public IReadOnlyList<DungeonDropResult> Drops => drops; // 실제 던전 드롭 결과 반환

        private BattleResultData(string resultId, string dungeonId, BattleOutcome outcome, BattleReward reward, List<BattleResultPartyMember> partyMembers, List<DungeonDropResult> dropResults) // 전투 결과 데이터 생성
        {
            ResultId = resultId ?? string.Empty; // 결과 고유 ID 저장
            DungeonId = BattleContextRuntimeState.ResolveDungeonId(dungeonId); // 결과 던전 ID 저장
            Outcome = outcome; // 최종 승패 저장
            Gold = reward == null ? 0 : reward.Gold; // 획득 골드 저장
            Experience = reward == null ? 0 : reward.Experience; // 획득 경험치 저장
            StarCount = outcome == BattleOutcome.Victory ? 3 : 0; // 임시 승리 별 세 개 설정
            members = partyMembers ?? new List<BattleResultPartyMember>(); // 파티원 스냅샷 저장
            drops = dropResults ?? new List<DungeonDropResult>(); // 던전 드롭 결과 저장
        }

        public static BattleResultData Create(BattleOutcome outcome, IReadOnlyList<BattleStats> battleMembers) // 현재 파티 상태 기반 전투 결과 생성
        {
            return Create(outcome, battleMembers, Guid.NewGuid().ToString("N")); // 신규 고유 ID 기반 결과 생성
        }

        public static BattleResultData Create(BattleOutcome outcome, IReadOnlyList<BattleStats> battleMembers, string resultId) // 현재 전투 컨텍스트 결과 생성
        {
            return Create(outcome, battleMembers, resultId, BattleContextRuntimeState.CurrentDungeonId); // 현재 전투 던전 포함 결과 생성
        }

        public static BattleResultData Create(BattleOutcome outcome, IReadOnlyList<BattleStats> battleMembers, string resultId, string dungeonId) // 지정 던전 결과 생성
        {
            string resolvedDungeonId = BattleContextRuntimeState.ResolveDungeonId(dungeonId); // 결과 던전 ID 보정
            BattleReward reward = BattleRewardCalculator.Calculate(outcome, resolvedDungeonId); // 던전 및 승패 기반 보상 계산
            List<BattleResultPartyMember> snapshots = new List<BattleResultPartyMember>(); // 파티원 스냅샷 목록 생성
            List<DungeonDropResult> dropResults = CreateDrops(outcome, resolvedDungeonId, resultId); // 던전 승리 드롭 결과 생성

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

            return new BattleResultData(resultId, resolvedDungeonId, outcome, reward, snapshots, dropResults); // 던전 드롭 포함 완성 전투 결과 반환
        }

        private static List<DungeonDropResult> CreateDrops(BattleOutcome outcome, string dungeonId, string resultId) // 현재 던전 드롭 결과 생성
        {
            if (outcome != BattleOutcome.Victory) // 승리 여부 확인
            {
                return new List<DungeonDropResult>(); // 패배 드롭 없음 반환
            }

            if (GameManager.Instance == null || GameManager.Instance.Data == null || !GameManager.Instance.Data.IsInitialized) // 던전 데이터 조회 가능 상태 확인
            {
                return new List<DungeonDropResult>(); // 데이터 관리자 누락 시 드롭 없음 반환
            }

            DungeonData dungeon = GameManager.Instance.Data.GetDungeon(dungeonId); // 현재 던전 데이터 조회

            if (dungeon == null) // 던전 데이터 존재 확인
            {
                return new List<DungeonDropResult>(); // 던전 누락 시 드롭 없음 반환
            }

            return DungeonDropRollService.Roll(dungeon.DropTable, resultId); // 결과 ID 기반 실제 드롭 계산 반환
        }
    }
}
