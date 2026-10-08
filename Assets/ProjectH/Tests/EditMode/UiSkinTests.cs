using System.Collections.Generic; // 집합 자료형
using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.Battle; // 상태이상 · 속성 기능
using ProjectH.SaveSystem; // 룬 · 골드 기능
using ProjectH.UI; // UI 스킨 · 초상화 자리 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class UiSkinTests // Day81 UI 스킨 규격 · 초상화 자리 · 재화 문구 테스트
    {
        [Test] // 스킨 규격 : 이름이 겹치지 않고, 크기는 4의 배수이며, 테두리는 그림의 절반보다 작다
        public void SkinParts_AreValidForNineSlice() // 스킨 규격 테스트
        {
            HashSet<string> keys = new HashSet<string>(); // 이름 집합
            Assert.That(UiSkin.Parts.Count, Is.EqualTo(18)); // 18종 (그림 요청문과 같은 수)

            foreach (UiSkinPart part in UiSkin.Parts) // 조각 순회
            {
                Assert.That(keys.Add(part.Key), Is.True, $"이름이 겹침 : {part.Key}"); // 고유한 이름
                Assert.That(part.Width % 4, Is.EqualTo(0), $"가로가 4의 배수가 아님 : {part.Key}"); // 압축되는 크기
                Assert.That(part.Height % 4, Is.EqualTo(0), $"세로가 4의 배수가 아님 : {part.Key}"); // 압축되는 크기
                Assert.That(part.Border * 2, Is.LessThan(Mathf.Min(part.Width, part.Height)), $"테두리가 너무 두꺼움 : {part.Key}"); // 가운데가 남아야 늘일 수 있다
                Assert.That(part.Usage, Is.Not.Empty, $"쓰는 곳이 비어 있음 : {part.Key}"); // 설명
                Assert.That(UiSkin.TryGetPart(part.Key, out UiSkinPart found), Is.True); // 이름으로 조회
                Assert.That(found.Border, Is.EqualTo(part.Border)); // 같은 규격
            }
        }

        [Test] // 스킨 파일이 없으면 null을 돌려주고 이미지는 그대로 둔다. 파일이 있으면 규격대로 9조각이 된다
        public void Skin_WithoutFile_LeavesImageAsIs() // 스킨 적용 테스트
        {
            GameObject imageObject = new GameObject("Panel", typeof(RectTransform), typeof(Image)); // 패널 역할

            try // 정리 보장
            {
                Image image = imageObject.GetComponent<Image>(); // 패널 이미지
                Assert.That(UiSkin.Get("no_such_part"), Is.Null); // 규격에 없는 이름
                Assert.That(UiSkin.Apply(image, "no_such_part"), Is.False); // 적용하지 않음
                Assert.That(image.sprite, Is.Null); // 색 상자 그대로
                Assert.That(UiSkin.Apply(null, UiSkin.PanelLight), Is.False); // null 안전

                foreach (UiSkinPart part in UiSkin.Parts) // 조각 순회
                {
                    Sprite sprite = UiSkin.Get(part.Key); // 스킨 (아직 없으면 null)
                    if (sprite == null) continue; // 그림이 오기 전
                    float expected = part.Border * (sprite.rect.width / part.Width); // 받은 크기에 맞춘 테두리
                    Assert.That(sprite.border.x, Is.EqualTo(expected).Within(0.5f), $"테두리가 규격과 다름 : {part.Key}"); // 9조각 테두리
                    Assert.That(sprite.rect.width / sprite.rect.height, Is.EqualTo(part.Width / (float)part.Height).Within(0.02f), $"비율이 규격과 다름 : {part.Key}"); // 비율
                }
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(imageObject); // 패널 제거
            }
        }

        [Test] // 아이콘 이름은 서로 겹치지 않고, 룬 · 상태이상 · 속성 아이콘은 코드의 이름을 그대로 파일 이름으로 쓴다
        public void Icons_UseStableFileNames() // 아이콘 이름 테스트
        {
            HashSet<string> keys = new HashSet<string>(); // 이름 집합
            foreach (string key in UiIcon.MenuKeys) Assert.That(keys.Add(key), Is.True, key); // 메뉴 아이콘
            foreach (string key in UiIcon.NavKeys) Assert.That(keys.Add(key), Is.True, key); // 내비 아이콘
            foreach (string key in UiIcon.CurrencyKeys) Assert.That(keys.Add(key), Is.True, key); // 재화 아이콘
            Assert.That(keys.Count, Is.EqualTo(30)); // 20 + 8 + 2 (그림 요청문과 같은 수)
            Assert.That(UiIcon.Get("no_such_icon"), Is.Null); // 없는 아이콘
            Assert.That(UiIcon.Get(null), Is.Null); // null 안전
            Assert.That(UiIcon.GetRunePath(RuneKind.Power), Is.EqualTo("Icons/Runes/Power")); // 룬
            Assert.That(UiIcon.GetStatusPath(BattleStatusEffectId.Burn), Is.EqualTo("Icons/Status/Burn")); // 상태이상
            Assert.That(UiIcon.GetElementPath(BattleElement.Fire), Is.EqualTo("Icons/Elements/Fire")); // 속성
            Assert.That(UiIcon.GetStatus(BattleStatusEffectId.None), Is.Null); // 상태이상 없음
            Assert.That(UiIcon.GetElement(BattleElement.None), Is.Null); // 무속성
            Assert.That(System.Enum.GetValues(typeof(RuneKind)).Length, Is.EqualTo(15)); // 룬 15종 (요청문과 같은 수)
            Assert.That(System.Enum.GetValues(typeof(BattleStatusEffectId)).Length - 1, Is.EqualTo(17)); // 상태이상 17종 (None 제외)
            Assert.That(System.Enum.GetValues(typeof(BattleElement)).Length - 1, Is.EqualTo(5)); // 속성 5종 (None 제외)
        }

        [Test] // 상반신 영역 : 가로와 머리 위 높이는 초상화와 같고 아래로만 길어진다
        public void BustRect_ExtendsPortraitDownward() // 상반신 영역 테스트
        {
            Rect sprite = new Rect(0f, 0f, 1024f, 1536f); // 스탠딩 크기
            Assert.That(StandingFaceCatalog.TryGet("CH_SERENA", out StandingFace face), Is.True); // 얼굴 위치
            Rect square = StandingFaceCatalog.GetPortraitRect(face, sprite); // 초상화
            Rect bust = StandingFaceCatalog.GetBustRect(face, sprite, 0.75f); // 상반신
            Assert.That(bust.width, Is.EqualTo(square.width)); // 같은 가로
            Assert.That(bust.yMax, Is.EqualTo(square.yMax).Within(0.01f)); // 같은 머리 위 높이
            Assert.That(bust.height, Is.GreaterThan(square.height)); // 아래로 길다
            Assert.That(bust.width / bust.height, Is.EqualTo(0.75f).Within(0.02f)); // 요청한 비율
            Assert.That(bust.yMin, Is.GreaterThanOrEqualTo(sprite.yMin)); // 그림 안
            Assert.That(StandingFaceCatalog.GetBustRect(face, sprite, 1f), Is.EqualTo(square)); // 정사각 칸은 초상화 그대로
            Assert.That(StandingFaceCatalog.GetBustRect(face, sprite, 0f), Is.EqualTo(square)); // 잘못된 비율
        }

        [Test] // 초상화 자리 : 그림은 글자 바로 뒤에 한 번만 만들고, 빈 자리로 돌아가면 숨긴다
        public void PortraitSlot_PutsArtBehindThePlaceholderText() // 초상화 자리 테스트
        {
            GameObject frame = new GameObject("PortraitFrame", typeof(RectTransform)); // 초상화 칸
            GameObject textObject = new GameObject("PortraitText", typeof(RectTransform), typeof(Text)); // 이름 글자
            textObject.transform.SetParent(frame.transform, false); // 칸 아래

            try // 정리 보장
            {
                Text placeholder = textObject.GetComponent<Text>(); // 이름 글자
                Assert.That(PortraitSlot.Apply(placeholder, "CH_NOT_EXIST", true), Is.Null); // 그림 없는 캐릭터
                Assert.That(frame.transform.childCount, Is.EqualTo(1)); // 아무것도 만들지 않음

                Image bust = PortraitSlot.Apply(placeholder, "CH_SERENA", true); // 상반신
                Assert.That(bust, Is.Not.Null); // 그림 있음
                Assert.That(bust.sprite.rect.height, Is.GreaterThan(bust.sprite.rect.width)); // 세로로 긴 상반신
                Assert.That(frame.transform.childCount, Is.EqualTo(2)); // 그림 틀 + 글자
                Assert.That(frame.transform.GetChild(0).name, Is.EqualTo(PortraitSlot.HolderName)); // 그림이 글자보다 뒤
                Assert.That(frame.transform.GetChild(1), Is.SameAs(textObject.transform)); // 글자가 위

                Image face = PortraitSlot.Apply(placeholder, "CH_ELLEN", false); // 다른 캐릭터의 얼굴
                Assert.That(face, Is.SameAs(bust)); // 같은 그림 틀을 다시 쓴다
                Assert.That(face.sprite.rect.width, Is.EqualTo(face.sprite.rect.height)); // 정사각 얼굴
                Assert.That(frame.transform.childCount, Is.EqualTo(2)); // 늘어나지 않음

                PortraitSlot.Clear(placeholder); // 빈 자리
                Assert.That(frame.transform.GetChild(0).gameObject.activeSelf, Is.False); // 그림 숨김
                Assert.That(() => PortraitSlot.Clear(null), Throws.Nothing); // null 안전
                Assert.That(PortraitSlot.Apply(null, "CH_SERENA", true), Is.Null); // null 안전
            }
            finally // 임시 객체 정리
            {
                Object.DestroyImmediate(frame); // 칸과 하위 제거
            }
        }

        [Test] // 골드 문구는 세 자리마다 쉼표를 찍고, 음수는 0으로 보여 준다
        public void GoldLabel_UsesThousandsSeparator() // 재화 문구 테스트
        {
            Assert.That(GoldCurrencyService.FormatLabel(5548), Is.EqualTo("골드  5,548")); // 쉼표
            Assert.That(GoldCurrencyService.FormatLabel(0), Is.EqualTo("골드  0")); // 0
            Assert.That(GoldCurrencyService.FormatLabel(-10), Is.EqualTo("골드  0")); // 음수 없음
            Assert.That(GoldCurrencyService.FormatLabel(1234567), Is.EqualTo("골드  1,234,567")); // 큰 수
        }
    }
}
