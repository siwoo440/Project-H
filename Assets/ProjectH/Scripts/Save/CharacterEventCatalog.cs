using System.Collections.Generic; // 목록 자료형

namespace ProjectH.SaveSystem // 프로젝트 저장 영역
{
    public sealed class CharacterEventDefinition // 캐릭터 개인 이벤트 목록 항목 (Day56 신규 — 이야기 내용은 대화 시스템 일차에서 연결)
    {
        public string Id { get; } // 이벤트 ID
        public string CharacterId { get; } // 캐릭터 ID
        public int Episode { get; } // 화수
        public string Title { get; } // 제목
        public string ScriptId => Id; // 연결할 대화 파일 ID (Day58 추가, Resources/Dialogues/{이벤트 ID}.json)
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

    public static class CharacterEventCatalog // 12인 개인 이벤트 목록 (Day56 신규 · Day70 합류 8인 1화 추가 — 1화를 마치면 전용 장비를 받는다)
    {
        private static readonly CharacterEventDefinition[] Events = // 개인 이벤트 목록
        {
            new CharacterEventDefinition("EVT_SERENA_01", "CH_SERENA", 1, "치유 연습", GameEventCondition.AffinityAtLeast("CH_SERENA", AffinityTier.Friendly)), // 세레나 1화 (Day58 기획서 Lv.4 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_SERENA_02", "CH_SERENA", 2, "비밀 기도실", GameEventCondition.AffinityAtLeast("CH_SERENA", AffinityTier.Trusted), GameEventCondition.DayAtLeast(5)), // 세레나 2화 (Day58 기획서 Lv.6 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_ELLEN_01", "CH_ELLEN", 1, "조용한 미소", GameEventCondition.AffinityAtLeast("CH_ELLEN", AffinityTier.Friendly), GameEventCondition.AtTime(SaveTimeOfDay.Morning)), // 엘렌 1화 (Day58 기획서 Lv.4 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_ELLEN_02", "CH_ELLEN", 2, "검의 무게", GameEventCondition.AffinityAtLeast("CH_ELLEN", AffinityTier.Trusted), GameEventCondition.Cleared("DG002", "성역 외곽 폐허")), // 엘렌 2화 (Day58 기획서 Lv.6 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_LILIA_01", "CH_LILIA", 1, "마나 불안정", GameEventCondition.AffinityAtLeast("CH_LILIA", AffinityTier.Friendly), GameEventCondition.AtTime(SaveTimeOfDay.Night)), // 릴리아 1화 (Day58 기획서 Lv.4 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_LILIA_02", "CH_LILIA", 2, "어린 시절의 방", GameEventCondition.AffinityAtLeast("CH_LILIA", AffinityTier.Trusted), GameEventCondition.Cleared("DG003", "침식된 회랑")), // 릴리아 2화 (Day58 기획서 Lv.6 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_EVE_01", "CH_EVE", 1, "정령 송가", GameEventCondition.AffinityAtLeast("CH_EVE", AffinityTier.Friendly)), // 이브 1화 (Day58 기획서 Lv.4 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_EVE_02", "CH_EVE", 2, "숲의 기억", GameEventCondition.AffinityAtLeast("CH_EVE", AffinityTier.Trusted), GameEventCondition.DayAtLeast(3), GameEventCondition.AtTime(SaveTimeOfDay.Evening)), // 이브 2화 (Day58 기획서 Lv.6 이벤트명으로 변경)
            new CharacterEventDefinition("EVT_LUCIA_01", "CH_LUCIA", 1, "계산이 맞지 않는 일", GameEventCondition.AffinityAtLeast("CH_LUCIA", AffinityTier.Friendly), GameEventCondition.Cleared("DG002", "성역 외곽 폐허")), // 루시아 1화 (Day70 추가)
            new CharacterEventDefinition("EVT_CLAIRE_01", "CH_CLAIRE", 1, "되살릴 수 없는 것", GameEventCondition.AffinityAtLeast("CH_CLAIRE", AffinityTier.Friendly), GameEventCondition.Cleared("DG005", "금지된 지하 서고")), // 클레어 1화 (Day70 추가)
            new CharacterEventDefinition("EVT_MERCIA_01", "CH_MERCIA", 1, "염주 한 알", GameEventCondition.AffinityAtLeast("CH_MERCIA", AffinityTier.Friendly), GameEventCondition.AtTime(SaveTimeOfDay.Morning)), // 메르시아 1화 (Day70 추가)
            new CharacterEventDefinition("EVT_PYRA_01", "CH_PYRA", 1, "두 번은 없다", GameEventCondition.AffinityAtLeast("CH_PYRA", AffinityTier.Friendly), GameEventCondition.Cleared("DG006", "울부짖는 정령의 숲")), // 파이라 1화 (Day70 추가)
            new CharacterEventDefinition("EVT_TYRIA_01", "CH_TYRIA", 1, "안뜰의 맹세", GameEventCondition.AffinityAtLeast("CH_TYRIA", AffinityTier.Friendly), GameEventCondition.Cleared("DG007", "혹한의 성채")), // 티리아 1화 (Day70 추가)
            new CharacterEventDefinition("EVT_NOEL_01", "CH_NOEL", 1, "다 그리지 못한 지도", GameEventCondition.AffinityAtLeast("CH_NOEL", AffinityTier.Friendly), GameEventCondition.Cleared("DG008", "모래에 묻힌 신전")), // 노엘 1화 (Day70 추가)
            new CharacterEventDefinition("EVT_NATASHA_01", "CH_NATASHA", 1, "복수의 값", GameEventCondition.AffinityAtLeast("CH_NATASHA", AffinityTier.Friendly), GameEventCondition.AtTime(SaveTimeOfDay.Night)), // 나타샤 1화 (Day70 추가)
            new CharacterEventDefinition("EVT_SEPHIRA_01", "CH_SEPHIRA", 1, "처음으로 의심한 날", GameEventCondition.AffinityAtLeast("CH_SEPHIRA", AffinityTier.Friendly), GameEventCondition.AtTime(SaveTimeOfDay.Evening)) // 세피라 1화 (Day70 추가)
        };

        public static IReadOnlyList<CharacterEventDefinition> All => Events; // 전체 개인 이벤트 반환

        public static CharacterEventDefinition Find(string eventId) // 이벤트 ID로 조회 (Day58 추가)
        {
            for (int index = 0; index < Events.Length; index++) // 전체 순회
            {
                if (Events[index].Id == eventId) // ID 일치 확인
                {
                    return Events[index]; // 이벤트 반환
                }
            }

            return null; // 조회 실패 반환
        }

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
