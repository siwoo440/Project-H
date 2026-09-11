using System.Collections.Generic; // 목록 자료형

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public sealed class CharacterEventDefinition // 캐릭터 개인 이벤트 목록 항목 (Day56 신규 — 이야기 내용은 대화 시스템 일차에서 연결)
    {
        public string Id { get; } // 이벤트 ID
        public string CharacterId { get; } // 캐릭터 ID
        public int Episode { get; } // 화수
        public string Title { get; } // 제목
        public IReadOnlyList<GameEventCondition> Conditions { get; } // 해금 조건 목록

        public CharacterEventDefinition(string id, string characterId, int episode, string title, params GameEventCondition[] conditions) // 개인 이벤트 항목 생성
        {
            Id = id ?? string.Empty; // ID 저장
            CharacterId = characterId ?? string.Empty; // 캐릭터 ID 저장
            Episode = episode; // 화수 저장
            Title = title ?? string.Empty; // 제목 저장
            Conditions = conditions ?? new GameEventCondition[0]; // 조건 저장
        }
    }

    public static class CharacterEventCatalog // 초기 4인 개인 이벤트 목록 (Day56 신규)
    {
        private static readonly CharacterEventDefinition[] Events = // 개인 이벤트 목록
        {
            new CharacterEventDefinition("EVT_SERENA_01", "CH_SERENA", 1, "성역의 기도", GameEventCondition.AffinityAtLeast("CH_SERENA", AffinityTier.Friendly)), // 세레나 1화
            new CharacterEventDefinition("EVT_SERENA_02", "CH_SERENA", 2, "오래된 약속", GameEventCondition.AffinityAtLeast("CH_SERENA", AffinityTier.Trusted), GameEventCondition.DayAtLeast(5)), // 세레나 2화
            new CharacterEventDefinition("EVT_ELLEN_01", "CH_ELLEN", 1, "기사의 아침 훈련", GameEventCondition.AffinityAtLeast("CH_ELLEN", AffinityTier.Friendly), GameEventCondition.AtTime(SaveTimeOfDay.Morning)), // 엘렌 1화
            new CharacterEventDefinition("EVT_ELLEN_02", "CH_ELLEN", 2, "부러진 검", GameEventCondition.AffinityAtLeast("CH_ELLEN", AffinityTier.Trusted), GameEventCondition.Cleared("DG002", "성역 외곽 폐허")), // 엘렌 2화
            new CharacterEventDefinition("EVT_LILIA_01", "CH_LILIA", 1, "별을 세는 밤", GameEventCondition.AffinityAtLeast("CH_LILIA", AffinityTier.Friendly), GameEventCondition.AtTime(SaveTimeOfDay.Night)), // 릴리아 1화
            new CharacterEventDefinition("EVT_LILIA_02", "CH_LILIA", 2, "금지된 서가", GameEventCondition.AffinityAtLeast("CH_LILIA", AffinityTier.Trusted), GameEventCondition.Cleared("DG003", "침식된 회랑")), // 릴리아 2화
            new CharacterEventDefinition("EVT_EVE_01", "CH_EVE", 1, "정령의 속삭임", GameEventCondition.AffinityAtLeast("CH_EVE", AffinityTier.Friendly)), // 이브 1화
            new CharacterEventDefinition("EVT_EVE_02", "CH_EVE", 2, "비 오는 날의 약속", GameEventCondition.AffinityAtLeast("CH_EVE", AffinityTier.Trusted), GameEventCondition.DayAtLeast(3), GameEventCondition.AtTime(SaveTimeOfDay.Evening)) // 이브 2화
        };

        public static IReadOnlyList<CharacterEventDefinition> All => Events; // 전체 개인 이벤트 반환

        public static List<CharacterEventDefinition> GetForCharacter(string characterId) // 캐릭터별 개인 이벤트 목록 (화수 순)
        {
            List<CharacterEventDefinition> result = new List<CharacterEventDefinition>(); // 결과 목록 생성

            for (int index = 0; index < Events.Length; index++) // 전체 순회
            {
                if (Events[index].CharacterId == characterId) // 캐릭터 일치 확인
                {
                    result.Add(Events[index]); // 결과 추가
                }
            }

            result.Sort((left, right) => left.Episode.CompareTo(right.Episode)); // 화수 순 정렬
            return result; // 결과 반환
        }
    }
}
