using System.Collections.Generic; // 목록 자료형
using ProjectH.SaveSystem; // 저장 기능

namespace ProjectH.Events // 프로젝트 이벤트 영역
{
    public sealed class EventEvaluationContext // 이벤트 평가 문맥
    {
        public SaveData SaveData { get; } // 현재 저장 데이터
        public bool HasSaveData { get; } // 저장 파일 존재 상태

        public EventEvaluationContext(SaveData saveData, bool hasSaveData) // 평가 문맥 생성
        {
            SaveData = saveData; // 저장 데이터 설정
            HasSaveData = hasSaveData; // 저장 상태 설정
        }
    }

    public static class EventConditionEvaluator // 이벤트 조건 평가기
    {
        public static bool Evaluate(EventDefinition definition, EventEvaluationContext context, out string reason) // 이벤트 정의 평가
        {
            if (definition == null) // 이벤트 정의 확인
            {
                reason = "EventDefinition is null."; // 정의 누락 사유 설정
                return false; // 평가 실패
            }

            return Evaluate(definition.Conditions, definition.GroupMode, context, out reason); // 조건 목록 평가 반환
        }

        public static bool Evaluate(IReadOnlyList<EventCondition> conditions, EventConditionGroupMode groupMode, EventEvaluationContext context, out string reason) // 조건 목록 평가
        {
            if (context == null) // 평가 문맥 확인
            {
                reason = "EventEvaluationContext is null."; // 문맥 누락 사유 설정
                return false; // 평가 실패
            }

            if (conditions == null || conditions.Count == 0) // 조건 목록 확인
            {
                reason = "No conditions. Available by default."; // 기본 성공 사유 설정
                return true; // 조건 없음 성공
            }

            if (groupMode == EventConditionGroupMode.Any) // 선택 조건 그룹 확인
            {
                string lastReason = string.Empty; // 마지막 실패 사유 초기화

                foreach (EventCondition condition in conditions) // 조건 목록 순회
                {
                    if (EvaluateSingle(condition, context, out string conditionReason)) // 단일 조건 평가
                    {
                        reason = conditionReason; // 성공 사유 설정
                        return true; // 선택 조건 성공
                    }

                    lastReason = conditionReason; // 마지막 실패 사유 갱신
                }

                reason = "ANY failed. " + lastReason; // 전체 실패 사유 설정
                return false; // 선택 조건 실패
            }

            foreach (EventCondition condition in conditions) // 전체 조건 순회
            {
                if (EvaluateSingle(condition, context, out string conditionReason)) // 단일 조건 성공 확인
                {
                    continue; // 다음 조건 이동
                }

                reason = "ALL failed. " + conditionReason; // 실패 사유 설정
                return false; // 전체 조건 실패
            }

            reason = "All conditions satisfied."; // 전체 성공 사유 설정
            return true; // 전체 조건 성공
        }

        private static bool EvaluateSingle(EventCondition condition, EventEvaluationContext context, out string reason) // 단일 조건 평가
        {
            if (condition == null) // 조건 존재 확인
            {
                reason = "Condition is null."; // 조건 누락 사유 설정
                return false; // 평가 실패
            }

            SaveData saveData = context.SaveData; // 현재 저장 데이터 조회

            switch (condition.ConditionType) // 조건 종류 분기
            {
                case EventConditionType.Always: // 항상 조건 처리
                    reason = "Always = true."; // 성공 사유 설정
                    return true; // 항상 성공
                case EventConditionType.StoryFlag: // 스토리 플래그 조건 처리
                    return EvaluateStoryFlag(condition, saveData, out reason); // 플래그 조건 반환
                case EventConditionType.DayAtLeast: // 최소 일차 조건 처리
                    return EvaluateDayAtLeast(condition, saveData, out reason); // 최소 일차 조건 반환
                case EventConditionType.DayAtMost: // 최대 일차 조건 처리
                    return EvaluateDayAtMost(condition, saveData, out reason); // 최대 일차 조건 반환
                case EventConditionType.ChapterEquals: // 챕터 조건 처리
                    return EvaluateChapter(condition, saveData, out reason); // 챕터 조건 반환
                case EventConditionType.CharacterLevelAtLeast: // 캐릭터 레벨 조건 처리
                    return EvaluateCharacterLevel(condition, saveData, out reason); // 캐릭터 레벨 조건 반환
                case EventConditionType.HasSaveData: // 저장 존재 조건 처리
                    bool saveResult = context.HasSaveData == condition.BoolValue; // 저장 상태 비교
                    reason = $"HasSaveData expected={condition.BoolValue}, actual={context.HasSaveData}."; // 저장 상태 사유 설정
                    return saveResult; // 저장 상태 결과 반환
                default: // 미지원 조건 처리
                    reason = $"Unsupported condition type: {condition.ConditionType}."; // 미지원 사유 설정
                    return false; // 평가 실패
            }
        }

        private static bool EvaluateStoryFlag(EventCondition condition, SaveData saveData, out string reason) // 스토리 플래그 평가
        {
            if (saveData == null) // 저장 데이터 확인
            {
                reason = "SaveData is null for StoryFlag."; // 저장 누락 사유 설정
                return false; // 평가 실패
            }

            bool actual = saveData.HasStoryFlag(condition.StringValue); // 플래그 현재 상태 조회
            bool result = actual == condition.BoolValue; // 기대 상태 비교
            reason = $"StoryFlag {condition.StringValue} expected={condition.BoolValue}, actual={actual}."; // 플래그 평가 사유 설정
            return result; // 플래그 평가 결과 반환
        }

        private static bool EvaluateDayAtLeast(EventCondition condition, SaveData saveData, out string reason) // 최소 일차 평가
        {
            if (saveData == null) // 저장 데이터 확인
            {
                reason = "SaveData is null for DayAtLeast."; // 저장 누락 사유 설정
                return false; // 평가 실패
            }

            bool result = saveData.CurrentDay >= condition.IntValue; // 최소 일차 비교
            reason = $"CurrentDay >= {condition.IntValue}, actual={saveData.CurrentDay}."; // 일차 평가 사유 설정
            return result; // 일차 평가 결과 반환
        }

        private static bool EvaluateDayAtMost(EventCondition condition, SaveData saveData, out string reason) // 최대 일차 평가
        {
            if (saveData == null) // 저장 데이터 확인
            {
                reason = "SaveData is null for DayAtMost."; // 저장 누락 사유 설정
                return false; // 평가 실패
            }

            bool result = saveData.CurrentDay <= condition.IntValue; // 최대 일차 비교
            reason = $"CurrentDay <= {condition.IntValue}, actual={saveData.CurrentDay}."; // 일차 평가 사유 설정
            return result; // 일차 평가 결과 반환
        }

        private static bool EvaluateChapter(EventCondition condition, SaveData saveData, out string reason) // 챕터 조건 평가
        {
            if (saveData == null) // 저장 데이터 확인
            {
                reason = "SaveData is null for ChapterEquals."; // 저장 누락 사유 설정
                return false; // 평가 실패
            }

            bool result = string.Equals(saveData.CurrentChapter, condition.StringValue, System.StringComparison.Ordinal); // 챕터 ID 비교
            reason = $"CurrentChapter == {condition.StringValue}, actual={saveData.CurrentChapter}."; // 챕터 평가 사유 설정
            return result; // 챕터 평가 결과 반환
        }

        private static bool EvaluateCharacterLevel(EventCondition condition, SaveData saveData, out string reason) // 캐릭터 레벨 평가
        {
            if (saveData == null) // 저장 데이터 확인
            {
                reason = "SaveData is null for CharacterLevelAtLeast."; // 저장 누락 사유 설정
                return false; // 평가 실패
            }

            CharacterSaveData character = saveData.FindCharacter(condition.StringValue); // 캐릭터 진행 조회

            if (character == null) // 캐릭터 진행 확인
            {
                reason = $"CharacterSaveData not found. ID={condition.StringValue}."; // 캐릭터 누락 사유 설정
                return false; // 평가 실패
            }

            bool result = character.Level >= condition.IntValue; // 최소 레벨 비교
            reason = $"Character {condition.StringValue} Level >= {condition.IntValue}, actual={character.Level}."; // 레벨 평가 사유 설정
            return result; // 레벨 평가 결과 반환
        }
    }
}
