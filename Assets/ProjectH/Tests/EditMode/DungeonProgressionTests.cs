using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 던전 진행 기능
using ProjectH.SaveSystem; // 저장 데이터 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class DungeonProgressionTests // 던전 클리어 및 해금 테스트
    {
        [Test] // 초기 해금 상태 테스트 지정
        public void InitialState_OnlyFirstDungeonIsAvailable() // 신규 저장 첫 던전만 해금 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            Assert.AreEqual(DungeonProgressState.Available, DungeonProgressionPolicy.GetState(saveData, "DG001")); // 첫 던전 이용 가능 확인
            Assert.AreEqual(DungeonProgressState.Locked, DungeonProgressionPolicy.GetState(saveData, "DG002")); // 두 번째 던전 잠금 확인
            Assert.AreEqual(DungeonProgressState.Locked, DungeonProgressionPolicy.GetState(saveData, "DG003")); // 세 번째 던전 잠금 확인
            Assert.AreEqual(DungeonProgressState.Locked, DungeonProgressionPolicy.GetState(saveData, "DG004")); // 네 번째 던전 잠금 확인
        }

        [Test] // 첫 던전 클리어 테스트 지정
        public void MarkCleared_FirstDungeon_UnlocksSecondDungeon() // 첫 던전 클리어 후 다음 던전 해금 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            bool firstMark = DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 첫 던전 클리어 기록
            bool duplicateMark = DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 동일 던전 중복 기록 시도

            Assert.IsTrue(firstMark); // 최초 클리어 기록 성공 확인
            Assert.IsFalse(duplicateMark); // 중복 클리어 기록 차단 확인
            Assert.IsTrue(DungeonProgressSaveAdapter.IsCleared(saveData, "DG001")); // 첫 던전 클리어 저장 확인
            Assert.AreEqual(DungeonProgressState.Cleared, DungeonProgressionPolicy.GetState(saveData, "DG001")); // 첫 던전 완료 상태 확인
            Assert.AreEqual(DungeonProgressState.Available, DungeonProgressionPolicy.GetState(saveData, "DG002")); // 두 번째 던전 해금 확인
            Assert.AreEqual(DungeonProgressState.Locked, DungeonProgressionPolicy.GetState(saveData, "DG003")); // 세 번째 던전 잠금 유지 확인
        }

        [Test] // 연속 해금 테스트 지정
        public void SequentialClears_UnlockDungeonsInOrder() // 순차 클리어 해금 흐름 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG001"); // 첫 던전 클리어 기록
            DungeonProgressSaveAdapter.MarkCleared(saveData, "DG002"); // 두 번째 던전 클리어 기록

            Assert.AreEqual(DungeonProgressState.Cleared, DungeonProgressionPolicy.GetState(saveData, "DG002")); // 두 번째 던전 완료 상태 확인
            Assert.AreEqual(DungeonProgressState.Available, DungeonProgressionPolicy.GetState(saveData, "DG003")); // 세 번째 던전 해금 확인
            Assert.AreEqual(DungeonProgressState.Locked, DungeonProgressionPolicy.GetState(saveData, "DG004")); // 네 번째 던전 잠금 유지 확인
        }

        [Test] // 잘못된 던전 기록 테스트 지정
        public void MarkCleared_UnsupportedDungeon_DoesNotCreateProgress() // 미지원 던전 클리어 기록 차단 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            bool marked = DungeonProgressSaveAdapter.MarkCleared(saveData, "DG999"); // 미지원 던전 기록 시도

            Assert.IsFalse(marked); // 미지원 던전 기록 실패 확인
            Assert.IsFalse(DungeonProgressSaveAdapter.IsCleared(saveData, "DG999")); // 미지원 던전 미클리어 확인
        }

        [Test] // 최고 별점 기본값 테스트 지정 (Day47)
        public void GetBestStars_UnclearedDungeon_ReturnsZero() // 미클리어 던전 기본 별점 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            Assert.AreEqual(0, DungeonProgressSaveAdapter.GetBestStars(saveData, "DG001")); // 미클리어 던전 별점 0 확인
        }

        [Test] // 최고 별점 최초 기록 테스트 지정 (Day47)
        public void TrySetBestStars_FirstRecord_SavesStars() // 최초 별점 기록 저장 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            bool updated = DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG001", 2); // 최초 별점 2개 기록

            Assert.IsTrue(updated); // 최초 기록 성공 확인
            Assert.AreEqual(2, DungeonProgressSaveAdapter.GetBestStars(saveData, "DG001")); // 저장 별점 확인
        }

        [Test] // 낮은 별점 재기록 차단 테스트 지정 (Day47)
        public void TrySetBestStars_LowerThanExisting_DoesNotOverwrite() // 기존 기록보다 낮은 별점 차단 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG001", 3); // 최고 별점 3개 기록

            bool updated = DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG001", 1); // 더 낮은 별점 1개 재기록 시도

            Assert.IsFalse(updated); // 낮은 기록 갱신 차단 확인
            Assert.AreEqual(3, DungeonProgressSaveAdapter.GetBestStars(saveData, "DG001")); // 기존 최고 별점 유지 확인
        }

        [Test] // 더 높은 별점 갱신 테스트 지정 (Day47)
        public void TrySetBestStars_HigherThanExisting_Overwrites() // 더 높은 별점 갱신 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성
            DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG001", 1); // 최초 별점 1개 기록

            bool updated = DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG001", 3); // 더 높은 별점 3개 재기록 시도

            Assert.IsTrue(updated); // 향상 기록 성공 확인
            Assert.AreEqual(3, DungeonProgressSaveAdapter.GetBestStars(saveData, "DG001")); // 갱신 최고 별점 확인
        }

        [Test] // 별점 범위 보정 테스트 지정 (Day47)
        public void TrySetBestStars_AboveMax_ClampsToMax() // 최대 별점 초과 입력 보정 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG001", 99); // 최대치 초과 별점 기록 시도

            Assert.AreEqual(DungeonProgressSaveAdapter.MaxStars, DungeonProgressSaveAdapter.GetBestStars(saveData, "DG001")); // 최대 별점 보정 확인
        }

        [Test] // 던전별 별점 독립성 테스트 지정 (Day47)
        public void TrySetBestStars_DifferentDungeons_AreIndependent() // 던전별 별점 독립 저장 확인
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_A" }); // 신규 테스트 저장 생성

            DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG001", 3); // 첫 던전 별점 기록
            DungeonProgressSaveAdapter.TrySetBestStars(saveData, "DG002", 1); // 두 번째 던전 별점 기록

            Assert.AreEqual(3, DungeonProgressSaveAdapter.GetBestStars(saveData, "DG001")); // 첫 던전 별점 확인
            Assert.AreEqual(1, DungeonProgressSaveAdapter.GetBestStars(saveData, "DG002")); // 두 번째 던전 별점 확인
        }
    }
}
