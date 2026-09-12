using ProjectH.Core; // 전역 게임 관리자 기능
using ProjectH.Data; // 캐릭터 데이터·프로필 기능
using ProjectH.Dialogue; // 특수 화자 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class DialogueSpeakerInfo // 대화 이름표 이름·소속 (Day62 신규 — 목업 1번 이름/소속 표시)
    {
        public static string ResolveName(string speaker) // 화자 이름
        {
            if (speaker == DialogueSpeakers.Narration || string.IsNullOrEmpty(speaker)) return string.Empty; // 나레이션 이름 없음
            if (speaker == DialogueSpeakers.Hero) return ProjectH.SaveSystem.HeroNameService.Get(GameManager.Instance == null || GameManager.Instance.Save == null ? null : GameManager.Instance.Save.CurrentSave); // 주인공 이름 (Day68 — 입력한 이름)
            NpcProfile npc = NpcLineCatalog.Find(speaker); // NPC 프로필

            if (npc != null) // NPC 확인
            {
                return npc.Name; // NPC 이름
            }

            DataManager data = GameManager.Instance == null ? null : GameManager.Instance.Data; // 데이터 관리자 조회
            CharacterData character = data == null ? null : data.GetCharacter(speaker); // 캐릭터 원본 조회
            return character == null ? speaker : character.DisplayName; // 표시 이름 반환
        }

        public static string ResolveAffiliation(string speaker) // 화자 소속 (캐릭터는 프로필 출신, NPC는 직업, 미정·주인공은 빈 문자열)
        {
            if (!DialogueStageLayout.IsCharacterSpeaker(speaker)) return string.Empty; // 주인공·나레이션
            NpcProfile npc = NpcLineCatalog.Find(speaker); // NPC 프로필
            if (npc != null) return npc.Title; // NPC 직업
            string origin = CharacterProfileCatalog.Get(speaker).Origin; // 캐릭터 출신
            return origin == CharacterProfileCatalog.Unknown ? string.Empty : origin; // 미정은 표시 안 함
        }
    }
}
