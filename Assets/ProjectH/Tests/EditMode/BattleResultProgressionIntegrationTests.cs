using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 전투 결과 및 진행 기능
using ProjectH.Data; // 전투 포지션 기능
using ProjectH.SaveSystem; // 저장 데이터 기능
using UnityEngine; // JSON 직렬화 기능

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
            Assert.IsFalse(result.Members[0].HasGrowthResult); // 패배 시 성장 결과 미생성 확인
        }

        [Test] // 실제 성장 결과 기록 테스트 지정
        public void CommitOnce_Victory_RecordsAppliedGrowthResult() // 저장 적용값과 결과 UI 성장 데이터 일치 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            CharacterSaveData character = saveData.FindCharacter("CH_A"); // 테스트 저장 캐릭터 조회
            character.SetExperience(CharacterLevelProgression.GetRequiredExperience(1) - 1); // 다음 보상에서 레벨업 가능한 경험치 설정
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, "RESULT_GROWTH_RECORD", "DG001"); // 성장 결과 확인용 승리 결과 생성
            int startLevel = character.Level; // 성장 전 레벨 저장
            int startExperience = character.Experience; // 성장 전 경험치 저장
            CharacterLevelProgressionResult expected = CharacterLevelProgression.Apply(startLevel, startExperience, result.Experience); // 예상 성장 결과 계산

            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 승리 결과 영구 반영
            BattleResultPartyMember member = result.Members[0]; // 결과 파티원 성장 데이터 조회

            Assert.IsTrue(committed); // 승리 결과 반영 성공 확인
            Assert.IsTrue(member.HasGrowthResult); // 결과 파티원 성장 정보 생성 확인
            Assert.AreEqual(startLevel, member.GrowthStartLevel); // 성장 전 레벨 기록 확인
            Assert.AreEqual(startExperience, member.GrowthStartExperience); // 성장 전 경험치 기록 확인
            Assert.AreEqual(result.Experience, member.GainedExperience); // 획득 경험치 기록 확인
            Assert.AreEqual(expected.Level, member.GrowthEndLevel); // 성장 후 레벨 기록 확인
            Assert.AreEqual(expected.Experience, member.GrowthEndExperience); // 성장 후 경험치 기록 확인
            Assert.AreEqual(expected.LevelsGained, member.LevelsGained); // 상승 레벨 수 기록 확인
            Assert.AreEqual(expected.ReachedMaxLevel, member.ReachedMaxLevel); // 최대 레벨 상태 기록 확인
            Assert.AreEqual(expected.Level, character.Level); // 저장 캐릭터 최종 레벨 확인
            Assert.AreEqual(expected.Experience, character.Experience); // 저장 캐릭터 최종 경험치 확인
        }

        [Test] // 중복 결과 보상 차단 테스트 지정
        public void CommitOnce_SameResultTwice_DoesNotGrantExperienceAgain() // 동일 결과 ID 경험치 중복 지급 차단 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, "RESULT_DUPLICATE_GROWTH", "DG001"); // 중복 지급 확인용 승리 결과 생성

            bool firstCommitted = BattleResultCommitService.CommitOnce(saveData, result); // 첫 번째 결과 반영
            CharacterSaveData character = saveData.FindCharacter("CH_A"); // 첫 반영 후 저장 캐릭터 조회
            int levelAfterFirst = character.Level; // 첫 반영 후 레벨 저장
            int experienceAfterFirst = character.Experience; // 첫 반영 후 경험치 저장
            bool secondCommitted = BattleResultCommitService.CommitOnce(saveData, result); // 동일 결과 두 번째 반영 시도

            Assert.IsTrue(firstCommitted); // 첫 번째 결과 반영 성공 확인
            Assert.IsFalse(secondCommitted); // 두 번째 결과 반영 차단 확인
            Assert.AreEqual(levelAfterFirst, character.Level); // 중복 처리 후 레벨 유지 확인
            Assert.AreEqual(experienceAfterFirst, character.Experience); // 중복 처리 후 경험치 유지 확인
        }

        [Test] // 저장 직렬화 회귀 테스트 지정
        public void CommitOnce_SaveJsonRoundTrip_RetainsLevelExperienceAndDuplicateGuard() // 재시작 대응 레벨 경험치 및 결과 ID 유지 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            CharacterSaveData character = saveData.FindCharacter("CH_A"); // 테스트 저장 캐릭터 조회
            character.SetExperience(CharacterLevelProgression.GetRequiredExperience(1) - 1); // 레벨업 직전 경험치 설정
            BattleStats[] members = { CreateStats("ALLY_0", "CH_A") }; // 테스트 전투 파티 생성
            BattleResultData result = BattleResultData.Create(BattleOutcome.Victory, members, "RESULT_SAVE_ROUNDTRIP", "DG001"); // 저장 회귀 확인용 승리 결과 생성
            bool committed = BattleResultCommitService.CommitOnce(saveData, result); // 승리 결과 영구 반영
            int savedLevel = character.Level; // 저장 대상 레벨 기록
            int savedExperience = character.Experience; // 저장 대상 경험치 기록
            string json = JsonUtility.ToJson(saveData, true); // SaveManager와 동일한 JSON 직렬화 실행
            SaveData loadedSave = JsonUtility.FromJson<SaveData>(json); // SaveManager와 동일한 JSON 역직렬화 실행
            loadedSave.EnsureDefaults(); // 불러온 저장 기본값 복원
            CharacterSaveData loadedCharacter = loadedSave.FindCharacter("CH_A"); // 재로드 캐릭터 진행 조회
            bool duplicateCommitted = BattleResultCommitService.CommitOnce(loadedSave, result); // 재로드 후 동일 결과 중복 반영 시도

            Assert.IsTrue(committed); // 최초 결과 반영 성공 확인
            Assert.NotNull(loadedCharacter); // 재로드 캐릭터 존재 확인
            Assert.AreEqual(savedLevel, loadedCharacter.Level); // 재로드 후 레벨 유지 확인
            Assert.AreEqual(savedExperience, loadedCharacter.Experience); // 재로드 후 경험치 유지 확인
            Assert.IsTrue(BattleProgressSaveAdapter.HasBattleResultCommit(loadedSave, result.ResultId)); // 재로드 후 결과 ID 기록 유지 확인
            Assert.IsFalse(duplicateCommitted); // 재로드 후 동일 결과 보상 재지급 차단 확인
        }

        private static BattleStats CreateStats(string runtimeId, string characterId) // 테스트 전투 스탯 생성
        {
            return new BattleStats(runtimeId, characterId, characterId, BattlePosition.Dealer, 1, 100, 20, 10, 1f, 1f, 0.1f); // 기본 테스트 전투 스탯 반환
        }
    }
}
