using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.UI; // UI 스킨 공용 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text · Button 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class UiSkinKitTests // Day83 화면 부품에 스킨을 입히는 공용 기능 테스트
    {
        private GameObject root; // 테스트 화면 루트

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 빈 루트 준비
        {
            root = new GameObject("Root", typeof(RectTransform)); // 루트
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 루트 제거
        {
            Object.DestroyImmediate(root); // 루트와 하위 제거
        }

        [Test] // 창 : 스킨을 입히면 상자 색 대신 그림 색을 쓰고, 코드로 그리던 외곽선은 꺼진다
        public void Panel_AppliesSkinAndTurnsOffCodeOutline() // 창 스킨 테스트
        {
            Image panel = RuntimeUiKit.CreateImage(root.transform, "Panel", new Color(0.08f, 0.09f, 0.13f, 0.98f)); // 어두운 상자
            Outline outline = panel.gameObject.AddComponent<Outline>(); // 코드 외곽선
            Assert.That(UiSkinKit.Panel(panel, UiSkin.PanelDark), Is.True); // 적용
            Assert.That(panel.sprite, Is.SameAs(UiSkin.Get(UiSkin.PanelDark))); // 스킨 그림
            Assert.That(panel.type, Is.EqualTo(Image.Type.Sliced)); // 9조각
            Assert.That(panel.color, Is.EqualTo(Color.white)); // 그림 색 그대로
            Assert.That(outline.enabled, Is.False); // 외곽선 꺼짐
        }

        [Test] // 스킨이 없는 이름이면 아무것도 바꾸지 않는다
        public void Panel_WithoutSkin_ChangesNothing() // 스킨 없음 테스트
        {
            Color original = new Color(0.2f, 0.3f, 0.4f, 1f); // 원래 색
            Image panel = RuntimeUiKit.CreateImage(root.transform, "Panel", original); // 상자
            Outline outline = panel.gameObject.AddComponent<Outline>(); // 코드 외곽선
            Assert.That(UiSkinKit.Panel(panel, "no_such_part"), Is.False); // 적용하지 않음
            Assert.That(panel.sprite, Is.Null); // 색 상자 그대로
            Assert.That(panel.color, Is.EqualTo(original)); // 색 그대로
            Assert.That(outline.enabled, Is.True); // 외곽선 그대로
            Assert.That(UiSkinKit.Panel(null, UiSkin.PanelDark), Is.False); // null 안전
            Assert.That(UiSkinKit.Button(null, UiSkin.ButtonPrimary), Is.False); // null 안전
        }

        [Test] // 버튼 : 바탕 밝기에 맞춰 버튼 바로 아래의 글자색만 바꾼다
        public void Button_MatchesLabelColorToTheSkinTone() // 버튼 글자색 테스트
        {
            Button dark = CreateButton("Dark", "닫기", Color.black); // 어두운 버튼이 될 것
            Assert.That(UiSkinKit.Button(dark, UiSkin.ButtonTabOff), Is.True); // 어두운 청회색
            Assert.That(GetLabel(dark).color.r, Is.GreaterThan(0.8f)); // 밝은 글자

            Button light = CreateButton("Light", "구매", Color.white); // 밝은 버튼이 될 것
            Text inner = RuntimeUiKit.CreateText(GetLabel(light).transform, "Inner", "안쪽", 12, Color.white); // 글자 아래의 글자 (버튼 바로 아래가 아님)
            Assert.That(UiSkinKit.Button(light, UiSkin.ButtonPrimary), Is.True); // 금빛 주황
            Assert.That(GetLabel(light).color.r, Is.LessThan(0.3f)); // 어두운 글자
            Assert.That(inner.color.r, Is.GreaterThan(0.9f)); // 바로 아래가 아닌 글자는 그대로

            Button danger = CreateButton("Danger", "비우기", Color.white); // 조심할 버튼
            Assert.That(UiSkinKit.Button(danger, UiSkin.ButtonTabOff, UiSkinKit.DangerTint), Is.True); // 붉은 기
            Assert.That(danger.GetComponent<Image>().color, Is.EqualTo(UiSkinKit.DangerTint)); // 기 적용
        }

        [Test] // 켬 · 끔 : 그림을 바꾸고 글자색도 따라 바뀐다
        public void Toggle_SwapsSpriteAndLabelColor() // 선택 전환 테스트
        {
            Button tab = CreateButton("Tab", "켬", Color.white); // 탭
            Assert.That(UiSkinKit.Toggle(tab, true), Is.True); // 켬
            Assert.That(tab.GetComponent<Image>().sprite, Is.SameAs(UiSkin.Get(UiSkin.ButtonTabOn))); // 밝은 금색 그림
            Assert.That(GetLabel(tab).color.r, Is.LessThan(0.3f)); // 어두운 글자
            Assert.That(UiSkinKit.Toggle(tab, false), Is.True); // 끔
            Assert.That(tab.GetComponent<Image>().sprite, Is.SameAs(UiSkin.Get(UiSkin.ButtonTabOff))); // 어두운 청회색 그림
            Assert.That(GetLabel(tab).color.r, Is.GreaterThan(0.8f)); // 밝은 글자
        }

        [Test] // 글자 기호를 아이콘으로 : 글자는 비우고 같은 색의 아이콘을 넣는다. 아이콘이 없으면 글자 그대로
        public void Glyph_ReplacesSymbolTextWithIcon() // 아이콘 교체 테스트
        {
            Button previous = CreateButton("Prev", "◀", Color.white); // 이전 버튼
            Image icon = UiSkinKit.Glyph(previous, "arrow_left"); // 아이콘으로
            Assert.That(icon, Is.Not.Null); // 아이콘 있음
            Assert.That(icon.sprite, Is.Not.Null); // 그림
            Assert.That(GetLabel(previous).text, Is.Empty); // 글자 기호 비움

            Button unknown = CreateButton("Unknown", "?", Color.white); // 아이콘이 없는 버튼
            Assert.That(UiSkinKit.Glyph(unknown, "no_such_icon"), Is.Null); // 아이콘 없음
            Assert.That(GetLabel(unknown).text, Is.EqualTo("?")); // 글자 그대로
        }

        [Test] // 글자 앞 아이콘 : "◀  로비"는 아이콘 + "로비"가 되고 글자는 아이콘 오른쪽으로 밀린다
        public void LeadIcon_KeepsTheWordAndMovesItRight() // 앞 아이콘 테스트
        {
            Button back = CreateButton("Back", "◀  로비", Color.white); // 뒤로 버튼
            Image icon = UiSkinKit.LeadIcon(back, "back", "로비"); // 앞 아이콘
            Assert.That(icon, Is.Not.Null); // 아이콘 있음
            Assert.That(GetLabel(back).text, Is.EqualTo("로비")); // 기호를 뺀 문구
            Assert.That(GetLabel(back).rectTransform.anchorMin.x, Is.GreaterThan(0.2f)); // 아이콘 오른쪽
            Assert.That(UiSkinKit.LeadIcon(back, "no_such_icon", "로비"), Is.Null); // 아이콘 없음
        }

        private Button CreateButton(string name, string label, Color labelColor) // 테스트용 버튼 (바탕 + 바로 아래 글자)
        {
            Button button = RuntimeUiKit.CreateButton(root.transform, name, new Color(0.3f, 0.3f, 0.4f, 1f)); // 버튼
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(button.transform, "Label", label, 18, labelColor).rectTransform); // 글자
            return button; // 반환
        }

        private static Text GetLabel(Button button) // 버튼 바로 아래의 글자
        {
            return button.transform.Find("Label").GetComponent<Text>(); // 글자
        }
    }
}
