using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 캐릭터 레벨링 기능

namespace ProjectH.Tests.EditMode // 프로젝트 EditMode 테스트 영역
{
    public sealed class CharacterLevelProgressionTests // 캐릭터 레벨링 테스트
    {
        [Test] // 필요 경험치 공식 테스트 지정
        public void GetRequiredExperience_UsesTemporaryLinearFormula() // 임시 선형 경험치 공식 확인
        {
            Assert.AreEqual(100, CharacterLevelProgression.GetRequiredExperience(1)); // 1레벨 필요 경험치 확인
            Assert.AreEqual(150, CharacterLevelProgression.GetRequiredExperience(2)); // 2레벨 필요 경험치 확인
            Assert.AreEqual(1000, CharacterLevelProgression.GetRequiredExperience(19)); // 19레벨 필요 경험치 확인
            Assert.AreEqual(0, CharacterLevelProgression.GetRequiredExperience(20)); // 최대 레벨 필요 경험치 없음 확인
        }

        [Test] // 경험치 부족 테스트 지정
        public void Apply_InsufficientExperience_KeepsLevelAndCarriesExperience() // 경험치 부족 시 레벨 유지 확인
        {
            CharacterLevelProgressionResult result = CharacterLevelProgression.Apply(1, 0, 80); // 80 경험치 적용

            Assert.AreEqual(1, result.Level); // 레벨 유지 확인
            Assert.AreEqual(80, result.Experience); // 경험치 누적 확인
            Assert.AreEqual(0, result.LevelsGained); // 레벨 상승 없음 확인
            Assert.IsFalse(result.ReachedMaxLevel); // 최대 레벨 미도달 확인
        }

        [Test] // 다중 레벨업 테스트 지정
        public void Apply_LargeExperience_PerformsMultipleLevelUps() // 큰 경험치 다중 레벨업 확인
        {
            CharacterLevelProgressionResult result = CharacterLevelProgression.Apply(1, 0, 380); // 380 경험치 적용

            Assert.AreEqual(3, result.Level); // 3레벨 도달 확인
            Assert.AreEqual(130, result.Experience); // 초과 경험치 이월 확인
            Assert.AreEqual(2, result.LevelsGained); // 두 단계 상승 확인
        }

        [Test] // 기존 경험치 합산 테스트 지정
        public void Apply_ExistingExperience_IsIncludedBeforeLevelUp() // 기존 경험치 포함 레벨업 확인
        {
            CharacterLevelProgressionResult result = CharacterLevelProgression.Apply(1, 60, 70); // 기존 60과 획득 70 합산

            Assert.AreEqual(2, result.Level); // 2레벨 도달 확인
            Assert.AreEqual(30, result.Experience); // 잔여 경험치 확인
        }

        [Test] // 최대 레벨 처리 테스트 지정
        public void Apply_ReachesMaxLevel_DiscardsRemainingExperience() // 최대 레벨 도달 시 잔여 경험치 폐기 확인
        {
            CharacterLevelProgressionResult result = CharacterLevelProgression.Apply(19, 950, 100); // 최대 레벨 도달 경험치 적용

            Assert.AreEqual(20, result.Level); // 최대 레벨 확인
            Assert.AreEqual(0, result.Experience); // 최대 레벨 경험치 초기화 확인
            Assert.AreEqual(1, result.LevelsGained); // 한 단계 상승 확인
            Assert.IsTrue(result.ReachedMaxLevel); // 최대 레벨 도달 상태 확인
        }

        [Test] // 최대 레벨 추가 경험치 테스트 지정
        public void Apply_AlreadyMaxLevel_DiscardsAdditionalExperience() // 최대 레벨 추가 경험치 폐기 확인
        {
            CharacterLevelProgressionResult result = CharacterLevelProgression.Apply(20, 999, 500); // 최대 레벨 추가 경험치 적용

            Assert.AreEqual(20, result.Level); // 최대 레벨 유지 확인
            Assert.AreEqual(0, result.Experience); // 추가 경험치 폐기 확인
            Assert.AreEqual(0, result.LevelsGained); // 추가 레벨 상승 없음 확인
            Assert.IsTrue(result.ReachedMaxLevel); // 최대 레벨 상태 확인
        }
    }
}
