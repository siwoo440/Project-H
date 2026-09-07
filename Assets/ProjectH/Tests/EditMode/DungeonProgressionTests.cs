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
    }
}
