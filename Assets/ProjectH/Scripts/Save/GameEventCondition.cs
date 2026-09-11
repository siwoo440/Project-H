using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 던전 클리어 조회 기능

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public enum GameEventConditionKind // 이벤트 해금 조건 종류 (Day56 신규)
    {
        AffinityTierAtLeast = 0, // 캐릭터 호감도 단계 이상
        DayAtLeast = 1, // 일차 이상
        TimeOfDay = 2, // 특정 시간대
        DungeonCleared = 3, // 던전 클리어
        StoryFlag = 4, // 스토리 플래그 보유
        AffinityValueAtLeast = 5 // 캐릭터 호감도 수치 이상 (Day59 추가, 결속 5단계 호감도 100 조건)
    }

    public sealed class GameEventCondition // 단일 이벤트 해금 조건 (Day56 신규 — 대화·개인 이벤트·결속 스토리 공용)
    {
        public GameEventConditionKind Kind { get; } // 조건 종류
        public string TargetId { get; } // 대상 ID (캐릭터·던전·플래그)
        public int Value { get; } // 조건 수치 (단계·일차·시간대 번호)
        public string Label { get; } // 미충족 사유 표시용 대상 이름 (선택)

        private GameEventCondition(GameEventConditionKind kind, string targetId, int value, string label) // 조건 생성
        {
            Kind = kind; // 종류 저장
            TargetId = targetId ?? string.Empty; // 대상 ID 저장
            Value = value; // 수치 저장
            Label = label ?? string.Empty; // 표시 이름 저장
        }

        public static GameEventCondition AffinityAtLeast(string characterId, AffinityTier tier, string characterLabel = "") => new GameEventCondition(GameEventConditionKind.AffinityTierAtLeast, characterId, (int)tier, characterLabel); // 호감도 단계 조건 생성
        public static GameEventCondition DayAtLeast(int day) => new GameEventCondition(GameEventConditionKind.DayAtLeast, string.Empty, day, string.Empty); // 일차 조건 생성
        public static GameEventCondition AtTime(SaveTimeOfDay phase) => new GameEventCondition(GameEventConditionKind.TimeOfDay, string.Empty, (int)phase, string.Empty); // 시간대 조건 생성
        public static GameEventCondition Cleared(string dungeonId, string dungeonLabel = "") => new GameEventCondition(GameEventConditionKind.DungeonCleared, dungeonId, 0, dungeonLabel); // 던전 클리어 조건 생성
        public static GameEventCondition AffinityValueAtLeast(string characterId, int value, string characterLabel = "") => new GameEventCondition(GameEventConditionKind.AffinityValueAtLeast, characterId, value, characterLabel); // 호감도 수치 조건 생성 (Day59 추가)
        public static GameEventCondition Flag(string flagId, string flagLabel) => new GameEventCondition(GameEventConditionKind.StoryFlag, flagId, 0, flagLabel); // 스토리 플래그 조건 생성
    }

    public static class GameEventConditionEvaluator // 이벤트 해금 조건 판정 기능 (Day56 신규, 순수 판정)
    {
        public static bool Evaluate(SaveData saveData, IReadOnlyList<GameEventCondition> conditions, List<string> unmetReasons = null) // 전체 조건 AND 판정 (미충족 사유 수집)
        {
            unmetReasons?.Clear(); // 이전 사유 초기화

            if (conditions == null || conditions.Count == 0) // 조건 없음 확인
            {
                return true; // 조건 없는 이벤트는 항상 해금
            }

            bool passed = true; // 전체 통과 여부 초기화

            for (int index = 0; index < conditions.Count; index++) // 조건 순회
            {
                GameEventCondition condition = conditions[index]; // 조건 조회

                if (condition == null || IsMet(saveData, condition)) // 조건 충족 확인
                {
                    continue; // 충족 조건 제외
                }

                passed = false; // 미충족 기록
                unmetReasons?.Add(Describe(condition)); // 미충족 사유 추가 (모든 사유를 모아 한 번에 안내)
            }

            return passed; // 전체 통과 여부 반환
        }

        public static bool IsMet(SaveData saveData, GameEventCondition condition) // 단일 조건 충족 판정
        {
            if (saveData == null || condition == null) // 저장 및 조건 확인
            {
                return false; // 판정 불가 미충족 반환
            }

            switch (condition.Kind) // 조건 종류 분기
            {
                case GameEventConditionKind.AffinityTierAtLeast: // 호감도 단계 처리
                    return (int)AffinityService.GetAffinityTier(saveData, condition.TargetId) >= condition.Value; // 현재 단계 비교
                case GameEventConditionKind.DayAtLeast: // 일차 처리
                    return GameTimeService.GetCurrentDay(saveData) >= condition.Value; // 현재 일차 비교
                case GameEventConditionKind.TimeOfDay: // 시간대 처리
                    return (int)GameTimeService.GetCurrentPhase(saveData) == condition.Value; // 현재 시간대 비교
                case GameEventConditionKind.DungeonCleared: // 던전 클리어 처리
                    return DungeonProgressSaveAdapter.IsCleared(saveData, condition.TargetId); // 클리어 여부 조회
                case GameEventConditionKind.StoryFlag: // 스토리 플래그 처리
                    return saveData.HasStoryFlag(condition.TargetId); // 플래그 보유 여부 조회
                case GameEventConditionKind.AffinityValueAtLeast: // 호감도 수치 처리 (Day59 추가)
                    return AffinityService.GetAffinity(saveData, condition.TargetId) >= condition.Value; // 현재 호감도 비교
                default: // 미정의 조건 처리
                    return false; // 미충족 반환
            }
        }

        public static string Describe(GameEventCondition condition) // 조건 안내 문구 생성
        {
            string subject = string.IsNullOrEmpty(condition.Label) ? string.Empty : $"{condition.Label} "; // 대상 이름 접두 문구

            switch (condition.Kind) // 조건 종류 분기
            {
                case GameEventConditionKind.AffinityTierAtLeast: // 호감도 단계 처리
                    return $"{subject}{AffinityService.GetTierLabel((AffinityTier)condition.Value)} 이상"; // 호감도 조건 문구
                case GameEventConditionKind.DayAtLeast: // 일차 처리
                    return $"{condition.Value}일차 이후"; // 일차 조건 문구
                case GameEventConditionKind.TimeOfDay: // 시간대 처리
                    return $"{GetPhaseLabel((SaveTimeOfDay)condition.Value)}에만"; // 시간대 조건 문구
                case GameEventConditionKind.DungeonCleared: // 던전 클리어 처리
                    return $"{(string.IsNullOrEmpty(condition.Label) ? condition.TargetId : condition.Label)} 클리어"; // 클리어 조건 문구
                case GameEventConditionKind.AffinityValueAtLeast: // 호감도 수치 처리 (Day59 추가)
                    return $"{subject}호감도 {condition.Value}"; // 호감도 수치 조건 문구
                default: // 스토리 플래그 처리
                    return string.IsNullOrEmpty(condition.Label) ? "특정 이야기 진행" : condition.Label; // 플래그 조건 문구
            }
        }

        private static string GetPhaseLabel(SaveTimeOfDay phase) // 시간대 한글 라벨 반환
        {
            switch (phase) // 시간대 분기
            {
                case SaveTimeOfDay.Morning: return "아침"; // 아침 라벨
                case SaveTimeOfDay.Day: return "낮"; // 낮 라벨
                case SaveTimeOfDay.Evening: return "저녁"; // 저녁 라벨
                default: return "밤"; // 밤 라벨
            }
        }
    }
}
