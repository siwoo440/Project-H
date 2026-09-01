using System; // 직렬화 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Events // 프로젝트 이벤트 영역
{
    public enum EventConditionType // 이벤트 조건 종류
    {
        Always = 0, // 항상 충족
        StoryFlag = 1, // 스토리 플래그 조건
        DayAtLeast = 2, // 최소 일차 조건
        DayAtMost = 3, // 최대 일차 조건
        ChapterEquals = 4, // 챕터 일치 조건
        CharacterLevelAtLeast = 5, // 캐릭터 최소 레벨 조건
        HasSaveData = 6 // 저장 존재 조건
    }

    public enum EventConditionGroupMode // 조건 그룹 방식
    {
        All = 0, // 모든 조건 충족
        Any = 1 // 하나 이상 조건 충족
    }

    [Serializable] // Unity 직렬화 허용
    public sealed class EventCondition // 단일 이벤트 조건
    {
        [SerializeField] private EventConditionType conditionType = EventConditionType.Always; // 조건 종류
        [SerializeField] private string stringValue = string.Empty; // 문자열 조건 값
        [SerializeField] private int intValue; // 정수 조건 값
        [SerializeField] private bool boolValue = true; // 논리 조건 값

        public EventConditionType ConditionType => conditionType; // 조건 종류 반환
        public string StringValue => stringValue; // 문자열 값 반환
        public int IntValue => intValue; // 정수 값 반환
        public bool BoolValue => boolValue; // 논리 값 반환

        public static EventCondition Always() // 항상 조건 생성
        {
            return new EventCondition(); // 항상 조건 반환
        }

        public static EventCondition StoryFlag(string flagId, bool expected) // 스토리 플래그 조건 생성
        {
            return new EventCondition // 플래그 조건 생성
            {
                conditionType = EventConditionType.StoryFlag, // 조건 종류 설정
                stringValue = flagId ?? string.Empty, // 플래그 ID 설정
                boolValue = expected // 기대 상태 설정
            }; // 플래그 조건 반환
        }

        public static EventCondition DayAtLeast(int day) // 최소 일차 조건 생성
        {
            return new EventCondition // 최소 일차 조건 생성
            {
                conditionType = EventConditionType.DayAtLeast, // 조건 종류 설정
                intValue = Mathf.Max(1, day) // 최소 일차 설정
            }; // 최소 일차 조건 반환
        }

        public static EventCondition DayAtMost(int day) // 최대 일차 조건 생성
        {
            return new EventCondition // 최대 일차 조건 생성
            {
                conditionType = EventConditionType.DayAtMost, // 조건 종류 설정
                intValue = Mathf.Max(1, day) // 최대 일차 설정
            }; // 최대 일차 조건 반환
        }

        public static EventCondition ChapterEquals(string chapterId) // 챕터 일치 조건 생성
        {
            return new EventCondition // 챕터 조건 생성
            {
                conditionType = EventConditionType.ChapterEquals, // 조건 종류 설정
                stringValue = chapterId ?? string.Empty // 챕터 ID 설정
            }; // 챕터 조건 반환
        }

        public static EventCondition CharacterLevelAtLeast(string characterId, int level) // 캐릭터 레벨 조건 생성
        {
            return new EventCondition // 캐릭터 레벨 조건 생성
            {
                conditionType = EventConditionType.CharacterLevelAtLeast, // 조건 종류 설정
                stringValue = characterId ?? string.Empty, // 캐릭터 ID 설정
                intValue = Mathf.Max(1, level) // 최소 레벨 설정
            }; // 캐릭터 레벨 조건 반환
        }

        public static EventCondition HasSave(bool expected) // 저장 존재 조건 생성
        {
            return new EventCondition // 저장 존재 조건 생성
            {
                conditionType = EventConditionType.HasSaveData, // 조건 종류 설정
                boolValue = expected // 기대 상태 설정
            }; // 저장 존재 조건 반환
        }
    }
}
