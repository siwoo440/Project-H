using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결과 기능
using ProjectH.Data; // 전투 포지션 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class BattleResultCommitServiceTests // 전투 결과 영구 반영 테스트
    {
        [Test] // 승리 보상 반영 테스트 지정
        public void CommitOnce_Victory_AppliesGoldAndExperienceOnlyOnce() // 승리 보상 1회 반영 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A", "CH_B" }); // 테스트 저장 데이터 생성
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A"), CreateStats("ALLY_1", "CH_B") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, "RESULT_VICTORY_A"); // 고정 ID 승리 결과 생성

            bool firstCommit = BattleResultCommitService.CommitOnce(saveData, result); // 첫 번째 결과 반영 실행
            bool secondCommit = BattleResultCommitService.CommitOnce(saveData, result); // 동일 결과 재반영 실행

            Assert.IsTrue(firstCommit); // 첫 번째 결과 반영 성공 확인
            Assert.IsFalse(secondCommit); // 두 번째 중복 반영 차단 확인
            Assert.AreEqual(120, BattleProgressSaveAdapter.GetGold(saveData)); // 골드 1회 지급 확인
            Assert.AreEqual(80, saveData.FindCharacter("CH_A").Experience); // 첫 번째 캐릭터 경험치 지급 확인
            Assert.AreEqual(80, saveData.FindCharacter("CH_B").Experience); // 두 번째 캐릭터 경험치 지급 확인
            Assert.IsTrue(BattleProgressSaveAdapter.HasBattleResultCommit(saveData, "RESULT_VICTORY_A")); // 결과 반영 기록 저장 확인
        }

        [Test] // 패배 보상 미반영 테스트 지정
        public void CommitOnce_Defeat_RecordsResultWithoutReward() // 패배 결과 기록 및 보상 없음 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Defeat, members, "RESULT_DEFEAT_A"); // 고정 ID 패배 결과 생성

            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 패배 결과 반영 실행

            Assert.IsTrue(committed); // 패배 결과 기록 성공 확인
            Assert.AreEqual(0, BattleProgressSaveAdapter.GetGold(saveData)); // 패배 골드 미지급 확인
            Assert.AreEqual(0, saveData.FindCharacter("CH_A").Experience); // 패배 경험치 미지급 확인
            Assert.IsTrue(BattleProgressSaveAdapter.HasBattleResultCommit(saveData, "RESULT_DEFEAT_A")); // 패배 결과 반영 기록 확인
        }

        [Test] // 미보유 파티원 안전 처리 테스트 지정
        public void CommitOnce_Victory_SkipsMissingSavedCharacter() // 미보유 캐릭터 경험치 처리 안전성 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            BattleStats[] members = { CreateStats("ALLY_0", "CH_UNKNOWN") }; // 미보유 캐릭터 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, "RESULT_UNKNOWN_A"); // 미보유 캐릭터 승리 결과 생성

            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 승리 결과 반영 실행

            Assert.IsTrue(committed); // 결과 반영 성공 확인
            Assert.AreEqual(120, BattleProgressSaveAdapter.GetGold(saveData)); // 계정 골드 지급 확인
            Assert.AreEqual(0, saveData.FindCharacter("CH_A").Experience); // 비참가 보유 캐릭터 경험치 미지급 확인
        }

        [Test] // 결과 ID 검증 테스트 지정
        public void CommitOnce_EmptyResultId_DoesNotApplyReward() // 빈 결과 ID 보상 반영 차단 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 테스트 저장 데이터 생성
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, string.Empty); // 빈 ID 승리 결과 생성

            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 빈 ID 결과 반영 실행

            Assert.IsFalse(committed); // 빈 결과 ID 반영 차단 확인
            Assert.AreEqual(0, BattleProgressSaveAdapter.GetGold(saveData)); // 빈 ID 골드 미지급 확인
            Assert.AreEqual(0, saveData.FindCharacter("CH_A").Experience); // 빈 ID 경험치 미지급 확인
        }

        private static BattleStats CreateStats(string runtimeId, string characterId) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, 100, 20, 10, 1f, 1f, 0.1f); // 기본 테스트 전투 스탯 반환
        }
    }
}
