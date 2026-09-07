using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결과 및 진행 기능
using ProjectH.Data; // 전투 포지션 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class BattleResultProgressionIntegrationTests // 전투 결과 성장 및 던전 진행 연동 테스트
    {
        [Test] // 승리 던전 클리어 테스트 지정
        public void CommitOnce_Victory_MarksDungeonCleared() // 승리 결과 던전 클리어 기록 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, "RESULT_PROGRESS_WIN", "DG001"); // 첫 던전 승리 결과 생성

            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 승리 결과 영구 반영

            Assert.IsTrue(committed); // 승리 결과 반영 성공 확인
            Assert.IsTrue(DungeonProgressSaveAdapter.IsCleared(saveData, "DG001")); // 첫 던전 클리어 저장 확인
            Assert.IsTrue(DungeonProgressionPolicy.IsUnlocked(saveData, "DG002")); // 두 번째 던전 해금 확인
        }

        [Test] // 패배 던전 미클리어 테스트 지정
        public void CommitOnce_Defeat_DoesNotMarkDungeonCleared() // 패배 결과 던전 미클리어 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Defeat, members, "RESULT_PROGRESS_LOSE", "DG001"); // 첫 던전 패배 결과 생성

            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 패배 결과 영구 반영

            Assert.IsTrue(committed); // 패배 결과 기록 성공 확인
            Assert.IsFalse(DungeonProgressSaveAdapter.IsCleared(saveData, "DG001")); // 첫 던전 미클리어 확인
            Assert.IsFalse(DungeonProgressionPolicy.IsUnlocked(saveData, "DG002")); // 두 번째 던전 잠금 유지 확인
        }

        private static BattleStats CreateStats(string runtimeId, string characterId) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, 100, 20, 10, 1f, 1f, 0.1f); // 기본 테스트 전투 스탯 반환
        }
    }
}
