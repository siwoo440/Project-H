using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 상태이상 · 속성 기능
using ProjectH.SaveSystem; // 룬 기능
using ProjectH.UI; // UI 스킨 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class UiSkinArtTests // Day82 정식 UI 그림 86장과 화면 연결 테스트
    {
        [Test] // 스킨 18종이 모두 있고, 크기와 9조각 테두리가 규격과 같다
        public void EverySkinPart_HasArtMatchingSpec() // 스킨 그림 테스트
        {
            foreach (UiSkinPart part in UiSkin.Parts) // 조각 순회
            {
                Sprite sprite = UiSkin.Get(part.Key); // 스킨
                Assert.That(sprite, Is.Not.Null, $"스킨 그림 없음 : Resources/UI/Skin/{part.Key}.png"); // 존재
                Assert.That(sprite.rect.width, Is.EqualTo(part.Width), $"가로가 규격과 다름 : {part.Key}"); // 가로
                Assert.That(sprite.rect.height, Is.EqualTo(part.Height), $"세로가 규격과 다름 : {part.Key}"); // 세로
                Assert.That(sprite.border, Is.EqualTo(new Vector4(part.Border, part.Border, part.Border, part.Border)), $"테두리가 규격과 다름 : {part.Key}"); // 9조각 테두리
                Assert.That(UiSkin.IsSkinSprite(sprite), Is.True); // 정식 스킨으로 인식
                Assert.That(UiSkin.GetCornerScale(part.Key), Is.GreaterThan(0f)); // 모서리 배율
            }

            Assert.That(UiSkin.IsDark(UiSkin.BarTop), Is.True); // 남색 바
            Assert.That(UiSkin.IsDark(UiSkin.PanelLight), Is.False); // 아이보리 창
            Assert.That(UiSkin.IsSkinSprite(null), Is.False); // null 안전
        }

        [Test] // 아이콘 30 + 룬 15 + 상태이상 17 + 속성 5 + 로고가 모두 있다
        public void EveryIcon_HasArt() // 아이콘 그림 테스트
        {
            foreach (string key in UiIcon.MenuKeys) Assert.That(UiIcon.Get(key), Is.Not.Null, $"메뉴 아이콘 없음 : {key}"); // 메뉴 20
            foreach (string key in UiIcon.NavKeys) Assert.That(UiIcon.Get(key), Is.Not.Null, $"내비 아이콘 없음 : {key}"); // 내비 8
            foreach (string key in UiIcon.CurrencyKeys) Assert.That(UiIcon.Get(key), Is.Not.Null, $"재화 아이콘 없음 : {key}"); // 재화 2

            foreach (RuneKind kind in System.Enum.GetValues(typeof(RuneKind))) // 룬 15
            {
                Sprite rune = UiIcon.GetRune(kind); // 룬 그림
                Assert.That(rune, Is.Not.Null, $"룬 아이콘 없음 : {kind}"); // 존재
                Assert.That(rune.rect.width, Is.EqualTo(rune.rect.height), $"룬 아이콘이 정사각이 아님 : {kind}"); // 정사각
            }

            foreach (BattleStatusEffectId id in System.Enum.GetValues(typeof(BattleStatusEffectId))) // 상태이상 17
            {
                if (id != BattleStatusEffectId.None) Assert.That(UiIcon.GetStatus(id), Is.Not.Null, $"상태이상 아이콘 없음 : {id}"); // 존재
            }

            foreach (BattleElement element in System.Enum.GetValues(typeof(BattleElement))) // 속성 5
            {
                if (element != BattleElement.None) Assert.That(UiIcon.GetElement(element), Is.Not.Null, $"속성 아이콘 없음 : {element}"); // 존재
            }

            Sprite logo = RuntimeSpriteLoader.Load(TitleScreenVisualPatch.LogoResourcePath); // 로고
            Assert.That(logo, Is.Not.Null, "로고 없음 : Resources/UI/Logo.png"); // 존재
            Assert.That(logo.rect.width / logo.rect.height, Is.EqualTo(2f).Within(0.01f)); // 가로 2 : 세로 1
        }

        [Test] // 대응표의 스킨 이름은 모두 규격에 있다
        public void SceneMapping_PointsToRealSkinParts() // 대응표 테스트
        {
            Assert.That(UiSkinScenePatch.Mapping.Count, Is.GreaterThanOrEqualTo(12)); // 임시 그림 12종
            foreach (System.Collections.Generic.KeyValuePair<string, string> pair in UiSkinScenePatch.Mapping) Assert.That(UiSkin.TryGetPart(pair.Value, out _), Is.True, $"{pair.Key} → {pair.Value}"); // 규격에 있는 이름
        }

        [Test] // 씬에 놓인 임시 그림은 스킨으로 바뀌고, 어두운 스킨 바로 위의 어두운 글자만 밝아진다
        public void ScenePatch_ReplacesPrototypeSpritesAndKeepsTextReadable() // 씬 스킨 교체 테스트
        {
            Texture2D texture = new Texture2D(8, 8); // 임시 그림 역할
            Sprite bar = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f)); // 상단 바 임시 그림
            bar.name = "frame_topbar"; // 4일차 그림과 같은 이름
            Sprite panel = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f)); // 창 임시 그림
            panel.name = "frame_panel"; // 4일차 그림과 같은 이름
            GameObject root = new GameObject("Canvas", typeof(RectTransform)); // 씬 루트 역할

            try // 정리 보장
            {
                Image topBar = CreateImage(root.transform, "TopBar", bar); // 상단 바
                Text title = CreateText(topBar.transform, "ScreenTitle", Color.black); // 바 위의 어두운 글자
                Image chip = CreateImage(topBar.transform, "GoldChip", null); // 바 위의 칩 (자기 바탕이 있다)
                Text chipText = CreateText(chip.transform, "Value", Color.black); // 칩 위의 어두운 글자
                Image talk = CreateImage(root.transform, "TalkPanel", panel); // 밝은 창
                Text body = CreateText(talk.transform, "Body", Color.black); // 창 위의 어두운 글자

                Assert.That(UiSkinScenePatch.Apply(root.transform), Is.EqualTo(2)); // 바와 창 두 개 교체
                Assert.That(topBar.sprite, Is.SameAs(UiSkin.Get(UiSkin.BarTop))); // 상단 바 스킨
                Assert.That(topBar.type, Is.EqualTo(Image.Type.Sliced)); // 9조각
                Assert.That(talk.sprite, Is.SameAs(UiSkin.Get(UiSkin.PanelLight))); // 밝은 창 스킨
                Assert.That(title.color.r, Is.GreaterThan(0.8f)); // 남색 바 위의 글자는 밝게
                Assert.That(chipText.color.r, Is.LessThan(0.2f)); // 칩 위의 글자는 그대로
                Assert.That(body.color.r, Is.LessThan(0.2f)); // 밝은 창 위의 글자는 그대로
                Assert.That(UiSkinScenePatch.Apply(root.transform), Is.EqualTo(0)); // 다시 불러도 또 바꾸지 않는다
                Assert.That(UiSkinScenePatch.Apply(null), Is.EqualTo(0)); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(root); // 루트와 하위 제거
                Object.DestroyImmediate(bar); // 임시 그림 제거
                Object.DestroyImmediate(panel); // 임시 그림 제거
                Object.DestroyImmediate(texture); // 텍스처 제거
            }
        }

        [Test] // 타이틀 로고 : 제목 글자 자리에 로고를 넣고 글자는 비운다
        public void TitleLogo_ReplacesTheTitleText() // 로고 테스트
        {
            GameObject titleObject = new GameObject("GameTitle", typeof(RectTransform), typeof(Text)); // 제목 글자

            try // 정리 보장
            {
                Text title = titleObject.GetComponent<Text>(); // 제목
                title.text = "PROJECT H"; // 씬에 적힌 제목
                Assert.That(TitleScreenVisualPatch.ApplyLogo(title), Is.True); // 로고 적용
                Assert.That(title.text, Is.Empty); // 글자 비움
                Assert.That(titleObject.transform.Find("Logo").GetComponent<Image>().sprite, Is.Not.Null); // 로고 그림
                Assert.That(TitleScreenVisualPatch.ApplyLogo(null), Is.False); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(titleObject); // 제목 제거
            }
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite) // 테스트용 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // 이미지 객체
            imageObject.transform.SetParent(parent, false); // 부모 연결
            Image image = imageObject.GetComponent<Image>(); // 이미지
            image.sprite = sprite; // 그림
            return image; // 반환
        }

        private static Text CreateText(Transform parent, string name, Color color) // 테스트용 글자 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 글자 객체
            textObject.transform.SetParent(parent, false); // 부모 연결
            Text text = textObject.GetComponent<Text>(); // 글자
            text.color = color; // 색
            return text; // 반환
        }
    }
}
