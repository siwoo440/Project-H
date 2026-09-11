using ProjectH.Data; // 캐릭터·스킬·프로필 데이터 기능
using ProjectH.SaveSystem; // 캐릭터 저장 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 탭 방지
    public sealed class CharacterProfileTab : MonoBehaviour // 캐릭터 창 [프로필] 탭 (Day60 신규 — 목업 : 이름·CV·키/몸무게·3사이즈·스토리)
    {
        private Text nameText; // 캐릭터 이름
        private Text voiceText; // CV
        private Text bodyText; // 키 / 몸무게
        private Text sizeText; // 3사이즈
        private Text storyText; // 스토리

        public static CharacterProfileTab Create(RectTransform root) // 탭 생성
        {
            CharacterProfileTab tab = root.gameObject.AddComponent<CharacterProfileTab>(); // 컴포넌트 추가
            tab.nameText = CreateBox(root, "Name", new Vector2(0.02f, 0.86f), new Vector2(0.62f, 0.98f), 24, TextAnchor.MiddleCenter); // 이름 칸
            tab.voiceText = CreateBox(root, "Voice", new Vector2(0.66f, 0.86f), new Vector2(0.98f, 0.98f), 22, TextAnchor.MiddleCenter); // CV 칸
            tab.bodyText = CreateBox(root, "Body", new Vector2(0.02f, 0.72f), new Vector2(0.49f, 0.83f), 18, TextAnchor.MiddleCenter); // 키/몸무게 칸
            tab.sizeText = CreateBox(root, "Size", new Vector2(0.51f, 0.72f), new Vector2(0.98f, 0.83f), 18, TextAnchor.MiddleCenter); // 3사이즈 칸
            tab.storyText = CreateBox(root, "Story", new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.69f), 19, TextAnchor.UpperLeft); // 스토리 칸
            tab.storyText.lineSpacing = 1.2f; // 줄 간격
            return tab; // 탭 반환
        }

        public void Refresh(CharacterData characterData, CharacterSaveData characterSave) // 선택 캐릭터 프로필 표시
        {
            string id = characterSave == null ? string.Empty : characterSave.CharacterId; // 캐릭터 ID
            CharacterProfile profile = CharacterProfileCatalog.Get(id); // 프로필 조회
            nameText.text = characterData == null ? id : characterData.DisplayName; // 이름
            voiceText.text = $"CV. {profile.Voice}"; // 성우
            bodyText.text = $"키 / 몸무게 : {profile.Height} / {profile.Weight}"; // 키·몸무게
            sizeText.text = $"3 사이즈 : {profile.ThreeSize}"; // 3사이즈
            storyText.text = $"<b>나이</b>  {profile.Age}     <b>출신</b>  {profile.Origin}\n\n{profile.Story}"; // 기본 정보 + 스토리
        }

        private static Text CreateBox(RectTransform root, string name, Vector2 min, Vector2 max, int size, TextAnchor anchor) // 목업 스타일 회색 칸 + 문구
        {
            Image box = RuntimeUiKit.CreateImage(root, name + "Box", new Color(0.82f, 0.82f, 0.84f, 1f)); // 회색 칸
            RuntimeUiKit.SetRect(box.rectTransform, min, max); // 배치
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.25f, 0.25f, 0.3f, 0.7f); // 테두리
            Text text = RuntimeUiKit.CreateText(box.transform, name, string.Empty, size, new Color(0.12f, 0.12f, 0.15f, 1f), FontStyle.Normal, anchor).Wrap(); // 문구
            text.supportRichText = true; // 굵게 태그
            RuntimeUiKit.Stretch(text.rectTransform, 14f); // 여백
            return text; // 문구 반환
        }
    }
}
