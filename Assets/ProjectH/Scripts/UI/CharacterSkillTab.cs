using ProjectH.Battle; // 궁극기 이름·컷인 대사 기능
using ProjectH.Data; // 캐릭터·스킬·프로필 데이터 기능
using ProjectH.SaveSystem; // 결속 스킬 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 탭 방지
    public sealed class CharacterSkillTab : MonoBehaviour // 캐릭터 창 [스킬] 탭 (Day60 신규 — 스킬 1~3 · 궁극기 · 패시브 · 결속 스킬)
    {
        private Text skillText; // 스킬 목록 문구

        public static CharacterSkillTab Create(RectTransform root) // 탭 생성
        {
            CharacterSkillTab tab = root.gameObject.AddComponent<CharacterSkillTab>(); // 컴포넌트 추가
            Image box = RuntimeUiKit.CreateImage(root, "SkillBox", new Color(0.93f, 0.93f, 0.95f, 1f)); // 바탕
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f)); // 배치
            tab.skillText = RuntimeUiKit.CreateText(box.transform, "Skills", string.Empty, 17, new Color(0.12f, 0.12f, 0.16f, 1f), FontStyle.Normal, TextAnchor.UpperLeft).Wrap(); // 목록 문구
            tab.skillText.supportRichText = true; // 색·굵게 태그
            tab.skillText.lineSpacing = 1.15f; // 줄 간격
            RuntimeUiKit.Stretch(tab.skillText.rectTransform, 18f); // 여백
            return tab; // 탭 반환
        }

        public void Refresh(CharacterData characterData, CharacterSaveData characterSave) // 스킬 목록 표시
        {
            if (characterData == null || characterSave == null) // 데이터 확인
            {
                skillText.text = "캐릭터 데이터를 찾을 수 없습니다."; // 안내
                return; // 종료
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(); // 문구 조립

            for (int slot = 1; slot <= 3; slot++) // 스킬 1~3
            {
                SkillData skill = characterData.GetSkill(slot); // 스킬 조회
                builder.Append($"<b><color=#2B5C9E>스킬 {slot}</color>  {(skill == null ? "준비 중" : skill.DisplayName)}</b>\n{(skill == null ? string.Empty : skill.Description)}\n\n"); // 스킬 줄
            }

            string ultimate = BattleUltimateEffectExecutor.GetUltimateName(characterData.Id); // 궁극기 이름
            builder.Append($"<b><color=#B8860B>궁극기</color>  {(string.IsNullOrEmpty(ultimate) ? "준비 중" : ultimate)}</b>\n{UltimateCutInCatalog.GetLine(characterData.Id)}\n\n"); // 궁극기 줄
            builder.Append($"<b><color=#2E8B57>패시브</color></b>  {GetPassiveText(characterData.Id)}\n\n"); // 패시브 줄
            BondSkillDefinition bond = BondCatalog.GetSkill(characterData.Id); // 결속 스킬
            builder.Append(bond == null ? "<b><color=#7A4FB0>결속 스킬</color></b>  추가 캐릭터 일차에 연결" : $"<b><color=#7A4FB0>결속 스킬</color>  「{bond.SkillName}」</b>  {(BondCatalog.HasBondSkill(characterSave.BondLevel) ? "[사용 중]" : "(결속 5단계 해금)")}\n{bond.SkillText}"); // 결속 스킬 줄
            skillText.text = builder.ToString(); // 적용
        }

        private static string GetPassiveText(string characterId) // 초기 4인 패시브 설명 (BattlePassiveSystem 동작 기준)
        {
            switch (characterId) // 캐릭터 분기
            {
                case "CH_SERENA": return "아군 체력 30% 이하 시 최대 체력 6% 보호막 (12초마다)"; // 세레나
                case "CH_ELLEN": return "체력 40% 이하 시 방어력 +20% 10초 (20초마다)"; // 엘렌
                case "CH_LILIA": return "광역 스킬 사용 시 적 전체 방어력 -5% 6초"; // 릴리아
                case "CH_EVE": return "기본 공격 5회 연속 적중 시 치명타율 +4% 10초"; // 이브
                default: return "추가 캐릭터 일차에 연결"; // 기타
            }
        }
    }
}
