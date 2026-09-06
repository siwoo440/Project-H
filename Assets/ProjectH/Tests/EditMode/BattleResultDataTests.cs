using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결과 기능
using ProjectH.Data; // 전투 포지션 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class BattleResultDataTests // 전투 결과 데이터 테스트
    {
        [Test] // 승리 보상 테스트 지정
        public void RewardCalculator_Victory_ReturnsTemporaryReward() // 승리 임시 보상 반환 확인
        {
            BattleReward reward = BattleRewardCalculator.Calculate(BattleOutcome.Victory); // 승리 보상 계산

            Assert.AreEqual(120, reward.Gold); // 승리 골드 보상 확인
            Assert.AreEqual(80, reward.Experience); // 승리 경험치 보상 확인
        }

        [Test] // 패배 보상 테스트 지정
        public void RewardCalculator_Defeat_ReturnsZeroReward() // 패배 보상 미지급 확인
        {
            BattleReward reward = BattleRewardCalculator.Calculate(BattleOutcome.Defeat); // 패배 보상 계산

            Assert.AreEqual(0, reward.Gold); // 패배 골드 미지급 확인
            Assert.AreEqual(0, reward.Experience); // 패배 경험치 미지급 확인
        }

        [Test] // 파티 결과 스냅샷 테스트 지정
        public void ResultData_Create_CapturesFourPartyMembers() // 4인 결과 스냅샷 생성 확인
        {
            BattleStats[] members = new BattleStats[4]; // 4인 파티 배열 생성
            members[0] = CreateStats("ALLY_0", "CH_A", "A", 10, 100); // 첫 번째 파티원 생성
            members[1] = CreateStats("ALLY_1", "CH_B", "B", 11, 110); // 두 번째 파티원 생성
            members[2] = CreateStats("ALLY_2", "CH_C", "C", 12, 120); // 세 번째 파티원 생성
            members[3] = CreateStats("ALLY_3", "CH_D", "D", 13, 130); // 네 번째 파티원 생성

            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members); // 승리 결과 데이터 생성

            Assert.AreEqual(BattleOutcome.Victory, result.Outcome); // 결과 승리 상태 확인
            Assert.AreEqual(4, result.Members.Count); // 4인 결과 인원 확인
            Assert.AreEqual("ALLY_0", result.Members[0].RuntimeId); // 첫 번째 런타임 ID 확인
            Assert.AreEqual("D", result.Members[3].DisplayName); // 네 번째 표시 이름 확인
            Assert.AreEqual(13, result.Members[3].Level); // 네 번째 레벨 확인
            Assert.AreEqual(3, result.StarCount); // 승리 별 개수 확인
        }

        [Test] // 전투 불능 스냅샷 테스트 지정
        public void ResultData_Create_PreservesDownHpState() // 전투 불능 체력 상태 보존 확인
        {
            BattleStats downMember = CreateStats("ALLY_0", "CH_A", "Down", 7, 100); // 전투 불능 대상 생성
            downMember.TakeDamage(1000); // 대상 전투 불능 처리
            BattleStats aliveMember = CreateStats("ALLY_1", "CH_B", "Alive", 8, 150); // 생존 대상 생성
            aliveMember.TakeDamage(40); // 생존 대상 체력 감소
            BattleStats[] members = { downMember, aliveMember }; // 결과 대상 배열 생성

            BattleResultData result = BattleResultData.Create(BattleOutcome.Defeat, members); // 패배 결과 데이터 생성

            Assert.IsFalse(result.Members[0].IsAlive); // 전투 불능 상태 확인
            Assert.AreEqual(0, result.Members[0].CurrentHp); // 전투 불능 현재 체력 확인
            Assert.AreEqual(100, result.Members[0].MaxHp); // 전투 불능 최대 체력 확인
            Assert.IsTrue(result.Members[1].IsAlive); // 생존 상태 확인
            Assert.AreEqual(110, result.Members[1].CurrentHp); // 생존 현재 체력 확인
            Assert.AreEqual(0, result.StarCount); // 패배 별 개수 확인
        }

        private static BattleStats CreateStats(string runtimeId, string characterId, string displayName, int level, int maxHp) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, displayName, BattlePosition.Dealer, level, maxHp, 20, 10, 1f, 1f, 0.1f); // 기본 테스트 전투 스탯 반환
        }
    }
}
