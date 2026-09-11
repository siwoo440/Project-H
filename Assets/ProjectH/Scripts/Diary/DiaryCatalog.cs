using System.Collections.Generic; // 목록 자료형
using ProjectH.Dialogue; // 대화 파일 기능
using ProjectH.Dungeon; // 지역 첫 방문 이야기 기능
using ProjectH.SaveSystem; // 개인 이벤트·결속 목록 기능
using ProjectH.Village; // 마을 대사 ID 기능

namespace ProjectH.Diary // 프로젝트 일기장 영역 (Day63 신규)
{
    public enum DiaryScenarioCategory // 시나리오 분류 (표시 순서)
    {
        Personal = 0, // 개인 이벤트
        Bond = 1, // 결속 대사
        Village = 2, // 마을 이벤트
        InnNight = 3, // 여관 특별한 밤
        Recruit = 4, // 동료 합류 (Day64 추가)
        Region = 5 // 지역 첫 방문 (Day65 추가)
    }

    public sealed class DiaryScenarioEntry // 다시 볼 수 있는 이야기 한 편
    {
        public string ScriptId { get; } // 대사 파일 ID
        public string CharacterId { get; } // 캐릭터 ID
        public DiaryScenarioCategory Category { get; } // 분류

        public DiaryScenarioEntry(string scriptId, string characterId, DiaryScenarioCategory category) // 항목 생성
        {
            ScriptId = scriptId; // 파일 ID 저장
            CharacterId = characterId; // 캐릭터 저장
            Category = category; // 분류 저장
        }

        public string Title // 제목 (대사 파일 제목, 없으면 ID)
        {
            get
            {
                DialogueScript script = DialogueLibrary.Load(ScriptId); // 대사 파일
                return script == null || string.IsNullOrEmpty(script.Title) ? ScriptId : script.Title; // 제목 반환
            }
        }
    }

    public sealed class DiaryCgEntry // CG 한 장 (정식 CG 전까지는 대사 배경 + 캐릭터 실루엣)
    {
        public string Id { get; } // CG ID (Resources/Diary/CG/{Id}에 정식 그림)
        public string ScriptId { get; } // 이 이야기를 보면 해금
        public string CharacterId { get; } // 캐릭터 ID

        public DiaryCgEntry(string id, string scriptId, string characterId) // 항목 생성
        {
            Id = id; // ID 저장
            ScriptId = scriptId; // 해금 대사 저장
            CharacterId = characterId; // 캐릭터 저장
        }
    }

    public static class DiaryCatalog // 일기장 목록 (Day63 신규 — 시나리오 · CG · 궁극기 컷인)
    {
        public static readonly string[] Starters = { "CH_SERENA", "CH_ELLEN", "CH_LILIA", "CH_EVE" }; // 초기 4인 (결속·마을·여관 대사 보유, 합류 8인은 70일차에 추가)
        private static List<string> allCharacters; // 12인 목록 캐시

        public static IReadOnlyList<string> AllCharacters => allCharacters ?? (allCharacters = BuildAllCharacters()); // 초기 4인 + 합류 8인 (Day64 추가 — 궁극기 컷신)
        private static List<DiaryScenarioEntry> scenarios; // 시나리오 목록 캐시
        private static List<DiaryCgEntry> cgs; // CG 목록 캐시

        public static IReadOnlyList<DiaryScenarioEntry> Scenarios => scenarios ?? (scenarios = BuildScenarios()); // 시나리오 목록

        public static IReadOnlyList<DiaryCgEntry> Cgs => cgs ?? (cgs = BuildCgs()); // CG 목록

        public static string GetCategoryLabel(DiaryScenarioCategory category) // 분류 이름
        {
            switch (category) // 분류 분기
            {
                case DiaryScenarioCategory.Personal: return "개인 이벤트"; // 개인
                case DiaryScenarioCategory.Bond: return "결속"; // 결속
                case DiaryScenarioCategory.Village: return "마을"; // 마을
                case DiaryScenarioCategory.Recruit: return "동료 합류"; // 합류 (Day64)
                case DiaryScenarioCategory.Region: return "지역"; // 첫 방문 (Day65)
                default: return "특별한 밤"; // 여관
            }
        }

        private static List<DiaryScenarioEntry> BuildScenarios() // 시나리오 목록 구성 (분류 → 캐릭터 순)
        {
            List<DiaryScenarioEntry> result = new List<DiaryScenarioEntry>(); // 결과

            foreach (CharacterEventDefinition definition in CharacterEventCatalog.All) // 개인 이벤트
            {
                if (DialogueLibrary.Load(definition.ScriptId) == null) continue; // 대사 파일이 있는 화만 (2화 이후는 70일차 개인 스토리에서 추가되면 자동으로 들어옴)
                result.Add(new DiaryScenarioEntry(definition.ScriptId, definition.CharacterId, DiaryScenarioCategory.Personal)); // 추가
            }

            foreach (RegionArrivalDefinition definition in RegionVisitService.All) // 지역 첫 방문 2편 (Day65)
            {
                result.Add(new DiaryScenarioEntry(definition.ScriptId, definition.CharacterId, DiaryScenarioCategory.Region)); // 추가
            }

            foreach (RecruitDefinition definition in RecruitService.All) // 동료 합류 8편 (Day64)
            {
                result.Add(new DiaryScenarioEntry(definition.ScriptId, definition.CharacterId, DiaryScenarioCategory.Recruit)); // 추가
            }

            foreach (string characterId in Starters) // 결속 대사 1~5단계
            {
                for (int level = 1; level <= BondCatalog.MaxLevel; level++) result.Add(new DiaryScenarioEntry(BondCatalog.GetBondScriptId(characterId, level), characterId, DiaryScenarioCategory.Bond)); // 추가
            }

            foreach (string characterId in Starters) // 마을 구역 이벤트
            {
                foreach (VillageZoneInfo info in VillageZoneCatalog.All) // 구역 순회
                {
                    if (VillageActionService.HasZoneEvent(info.Zone)) result.Add(new DiaryScenarioEntry(VillageActionService.GetZoneEventScriptId(characterId, info.Zone), characterId, DiaryScenarioCategory.Village)); // 추가
                }
            }

            foreach (string characterId in Starters) // 특별한 밤
            {
                result.Add(new DiaryScenarioEntry(VillageActionService.GetInnEventScriptId(characterId), characterId, DiaryScenarioCategory.InnNight)); // 추가
            }

            return result; // 결과 반환
        }

        private static List<string> BuildAllCharacters() // 12인 목록 구성
        {
            List<string> result = new List<string>(Starters); // 초기 4인
            foreach (RecruitDefinition definition in RecruitService.All) result.Add(definition.CharacterId); // 합류 8인
            return result; // 결과 반환
        }

        private static List<DiaryCgEntry> BuildCgs() // CG 목록 (개인 1화 · 결속 5단계 · 특별한 밤 = 캐릭터당 3장)
        {
            List<DiaryCgEntry> result = new List<DiaryCgEntry>(); // 결과

            foreach (string characterId in Starters) // 초기 4인
            {
                string shortId = characterId.Substring(3); // CH_ 제거
                CharacterEventDefinition first = FirstEpisode(characterId); // 개인 1화
                if (first != null) result.Add(new DiaryCgEntry($"CG_{shortId}_EVENT1", first.ScriptId, characterId)); // 개인 1화 CG
                result.Add(new DiaryCgEntry($"CG_{shortId}_BOND5", BondCatalog.GetBondScriptId(characterId, BondCatalog.MaxLevel), characterId)); // 결속 5단계 CG
                result.Add(new DiaryCgEntry($"CG_{shortId}_INN", VillageActionService.GetInnEventScriptId(characterId), characterId)); // 특별한 밤 CG
            }

            return result; // 결과 반환
        }

        private static CharacterEventDefinition FirstEpisode(string characterId) // 캐릭터 개인 1화
        {
            foreach (CharacterEventDefinition definition in CharacterEventCatalog.All) // 이벤트 순회
            {
                if (definition.CharacterId == characterId && definition.Episode == 1) return definition; // 1화
            }

            return null; // 없음
        }
    }
}
