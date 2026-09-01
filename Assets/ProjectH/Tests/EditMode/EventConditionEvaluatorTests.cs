using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Events; // 이벤트 기능
using ProjectH.SaveSystem; // 저장 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class EventConditionEvaluatorTests // 이벤트 조건 평가 테스트
    {
        [Test] // 테스트 표시
        public void EvaluateAll_WithMatchingConditions_ReturnsTrue() // 전체 조건 성공 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 테스트 저장 데이터 생성
            saveData.SetCurrentDay(3); // 테스트 일차 설정
            saveData.SetStoryFlag("STORY_SERENA_JOINED"); // 테스트 플래그 설정
            saveData.FindCharacter("CH_SERENA").SetLevel(2); // 테스트 캐릭터 레벨 설정
            EventCondition[] conditions = // 테스트 조건 목록 생성
            {
                EventCondition.StoryFlag("STORY_SERENA_JOINED", true), // 플래그 조건 생성
                EventCondition.DayAtLeast(3), // 일차 조건 생성
                EventCondition.CharacterLevelAtLeast("CH_SERENA", 2) // 레벨 조건 생성
            }; // 테스트 조건 목록 종료
            EventEvaluationContext context = new EventEvaluationContext(saveData, true); // 평가 문맥 생성

            bool result = EventConditionEvaluator.Evaluate(conditions, EventConditionGroupMode.All, context, out string reason); // 전체 조건 평가

            Assert.That(result, Is.True, reason); // 평가 성공 검증
        }

        [Test] // 테스트 표시
        public void EvaluateAll_WithFailedDay_ReturnsReason() // 일차 조건 실패 사유 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 테스트 저장 데이터 생성
            EventCondition[] conditions = // 테스트 조건 목록 생성
            {
                EventCondition.DayAtLeast(3) // 일차 조건 생성
            }; // 테스트 조건 목록 종료
            EventEvaluationContext context = new EventEvaluationContext(saveData, true); // 평가 문맥 생성

            bool result = EventConditionEvaluator.Evaluate(conditions, EventConditionGroupMode.All, context, out string reason); // 전체 조건 평가

            Assert.That(result, Is.False); // 평가 실패 검증
            StringAssert.Contains("CurrentDay", reason); // 실패 사유 검증
        }

        [Test] // 테스트 표시
        public void EvaluateAny_WithOneMatchingCondition_ReturnsTrue() // 선택 조건 성공 테스트
        {
            SaveData saveData = SaveData.CreateNewGame(new[] { "CH_SERENA" }); // 테스트 저장 데이터 생성
            saveData.SetStoryFlag("STORY_A"); // 테스트 플래그 설정
            EventCondition[] conditions = // 테스트 조건 목록 생성
            {
                EventCondition.StoryFlag("STORY_B", true), // 실패 플래그 조건 생성
                EventCondition.StoryFlag("STORY_A", true) // 성공 플래그 조건 생성
            }; // 테스트 조건 목록 종료
            EventEvaluationContext context = new EventEvaluationContext(saveData, true); // 평가 문맥 생성

            bool result = EventConditionEvaluator.Evaluate(conditions, EventConditionGroupMode.Any, context, out string reason); // 선택 조건 평가

            Assert.That(result, Is.True, reason); // 평가 성공 검증
        }
    }
}
