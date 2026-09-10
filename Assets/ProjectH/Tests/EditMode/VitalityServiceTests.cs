using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 저장 및 활력 진행 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class VitalityServiceTests // Day42 활력 회귀 테스트
    {
        [Test] // 새 게임 기본 활력 검증
        public void NewGame_DefaultVitality_IsMaxVitality() // 새 게임 기본 활력 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            Assert.That(VitalityService.GetVitality(saveData), Is.EqualTo(SaveData.MaxVitality)); // 기본 활력 검증
        }

        [Test] // 활력 소비 성공 검증
        public void TrySpendVitality_HasEnoughVitality_SubtractsOne() // 활력 1 소비 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            bool result = VitalityService.TrySpendVitality(saveData, 1, out string error); // 활력 1 소비 실행

            Assert.That(result, Is.True, error); // 소비 성공 확인
            Assert.That(VitalityService.GetVitality(saveData), Is.EqualTo(SaveData.MaxVitality - 1)); // 소비 후 활력 확인
        }

        [Test] // 활력 부족 시 소비 차단 검증
        public void TrySpendVitality_NotEnoughVitality_KeepsVitality() // 활력 부족 소비 차단 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            VitalityService.TrySpendVitality(saveData, SaveData.MaxVitality, out _); // 활력 전량 소비

            bool result = VitalityService.TrySpendVitality(saveData, 1, out _); // 추가 활력 소비 시도

            Assert.That(result, Is.False); // 소비 실패 확인
            Assert.That(VitalityService.GetVitality(saveData), Is.EqualTo(0)); // 활력 0 유지 확인
        }

        [Test] // 활력 증가 상한 검증
        public void AddVitality_AboveMax_ClampsToMax() // 최대값 초과 증가 보정 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성

            int result = VitalityService.AddVitality(saveData, 999); // 최대값 초과 활력 증가 실행

            Assert.That(result, Is.EqualTo(SaveData.MaxVitality)); // 최대값 보정 확인
        }

        [Test] // 다음 일차 진입 시 활력 전체 회복 검증
        public void AdvanceTime_ToNextDay_RestoresVitalityToMax() // 다음 날 활력 회복 검증
        {
            SaveData saveData = SaveData.CreateNewGame(System.Array.Empty<string>()); // 새 게임 데이터 생성
            VitalityService.TrySpendVitality(saveData, 50, out _); // 활력 일부 소비
            GameTimeService.AdvanceTime(saveData); // Morning to Day 진행
            GameTimeService.AdvanceTime(saveData); // Day to Evening 진행
            GameTimeService.AdvanceTime(saveData); // Evening to Night 진행

            GameTimeService.AdvanceTime(saveData); // Night to 다음 날 Morning 진행

            Assert.That(GameTimeService.GetCurrentDay(saveData), Is.EqualTo(2)); // 다음 일차 진입 확인
            Assert.That(VitalityService.GetVitality(saveData), Is.EqualTo(SaveData.MaxVitality)); // 활력 전체 회복 확인
        }
    }
}
