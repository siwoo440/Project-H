using System.Collections.Generic; // 목록 자료형
using ProjectH.Battle; // 던전 클리어 기록 기능
using ProjectH.Dialogue; // 대화 진행 결과 기능
using ProjectH.Dungeon; // 던전 도전 기록 기능
using ProjectH.SaveSystem; // 저장 기능

namespace ProjectH.Village // 프로젝트 마을 영역
{
    public enum RecruitConditionKind // 합류 조건 종류 (Day64 신규)
    {
        DungeonCleared = 0, // 던전 클리어
        DungeonEntered = 1 // 던전 도전 (입장, 클리어해도 인정)
    }

    public sealed class RecruitDefinition // 동료 한 명의 합류 조건 (Day64 신규)
    {
        public string CharacterId { get; } // 캐릭터 ID
        public RecruitConditionKind Condition { get; } // 조건 종류
        public string DungeonId { get; } // 조건 던전
        public string Hint { get; } // 조건 안내 문구

        public RecruitDefinition(string characterId, RecruitConditionKind condition, string dungeonId, string hint) // 정의 생성
        {
            CharacterId = characterId; // ID 저장
            Condition = condition; // 조건 저장
            DungeonId = dungeonId; // 던전 저장
            Hint = hint; // 안내 저장
        }

        public string ScriptId => "RECRUIT_" + CharacterId.Substring(3); // 합류 이야기 파일 (예: RECRUIT_CLAIRE)
    }

    public static class RecruitService // 새 동료 합류 (Day64 신규 — 던전 진행 → 마을 길드 합류 이벤트, 68~69일차 챕터 합류로 교체 예정)
    {
        private static readonly RecruitDefinition[] Definitions = // 기획서 합류 순서(챕터 2~5)를 지금 던전 진행에 맞춤
        {
            new RecruitDefinition("CH_CLAIRE", RecruitConditionKind.DungeonCleared, "DG001", "숲(무너진 성역의 숲)을 클리어하면"), // 챕터 2 연금술사
            new RecruitDefinition("CH_MERCIA", RecruitConditionKind.DungeonCleared, "DG001", "숲(무너진 성역의 숲)을 클리어하면"), // 챕터 3 무도승
            new RecruitDefinition("CH_PYRA", RecruitConditionKind.DungeonCleared, "DG003", "늪지대(침식된 회랑)를 클리어하면"), // 챕터 4 창기사
            new RecruitDefinition("CH_TYRIA", RecruitConditionKind.DungeonCleared, "DG003", "늪지대(침식된 회랑)를 클리어하면"), // 챕터 4 방패병
            new RecruitDefinition("CH_NOEL", RecruitConditionKind.DungeonEntered, "DG004", "마왕성(심연의 관문)에 도전하면"), // 챕터 5 탐험가
            new RecruitDefinition("CH_NATASHA", RecruitConditionKind.DungeonEntered, "DG004", "마왕성(심연의 관문)에 도전하면"), // 챕터 5 잠입자
            new RecruitDefinition("CH_SEPHIRA", RecruitConditionKind.DungeonEntered, "DG004", "마왕성(심연의 관문)에 도전하면") // 챕터 5 순례자
        };

        public static IReadOnlyList<RecruitDefinition> All => Definitions; // 전체 합류 정의

        public static RecruitDefinition Find(string characterId) // 캐릭터로 조회
        {
            foreach (RecruitDefinition definition in Definitions) // 정의 순회
            {
                if (definition.CharacterId == characterId) return definition; // 일치
            }

            return null; // 없음
        }

        public static bool IsConditionMet(SaveData saveData, RecruitDefinition definition) // 합류 조건 충족 여부 (이전 세이브도 진행 기록으로 바로 판정)
        {
            if (saveData == null || definition == null) return false; // 입력 확인
            bool cleared = DungeonProgressSaveAdapter.IsCleared(saveData, definition.DungeonId); // 클리어 기록
            if (definition.Condition == RecruitConditionKind.DungeonCleared) return cleared; // 클리어 조건
            return cleared || saveData.HasStoryFlag(DungeonEntryService.BuildEnteredFlag(definition.DungeonId)); // 도전 조건 (클리어해도 인정)
        }

        public static List<RecruitDefinition> GetPending(SaveData saveData) // 길드에 소식이 뜬 동료 (조건 충족 · 아직 합류 전)
        {
            List<RecruitDefinition> result = new List<RecruitDefinition>(); // 결과

            foreach (RecruitDefinition definition in Definitions) // 정의 순회
            {
                if (saveData != null && !saveData.HasCharacter(definition.CharacterId) && IsConditionMet(saveData, definition)) result.Add(definition); // 대기 중
            }

            return result; // 결과 반환
        }

        public static string CompleteRecruit(SaveData saveData, string characterId, DialogueRunner runner, string displayName) // 합류 이야기를 끝까지 보면 동료가 됨
        {
            RecruitDefinition definition = Find(characterId); // 정의
            if (runner == null || !runner.IsFinished) return "끝까지 보지 않아 합류하지 않았습니다."; // 중간 종료
            if (definition == null || !IsConditionMet(saveData, definition)) return "아직 만날 수 없는 동료입니다."; // 조건 미충족
            if (!saveData.AddCharacterInternal(characterId)) return $"{displayName}은(는) 이미 동료입니다."; // 중복
            AffinityService.AddAffinity(saveData, characterId, runner.AccumulatedAffinity); // 선택지 호감도
            return $"{displayName}이(가) 동료가 되었습니다! 파티 편성에서 함께할 수 있어요."; // 결과 안내
        }

        public static int RecruitAllForDebug(SaveData saveData) // 개발용 전원 합류 (조건 무시, 합류 수 반환)
        {
            int added = 0; // 합류 수

            foreach (RecruitDefinition definition in Definitions) // 정의 순회
            {
                if (saveData != null && saveData.AddCharacterInternal(definition.CharacterId)) added++; // 합류
            }

            return added; // 합류 수 반환
        }
    }
}
