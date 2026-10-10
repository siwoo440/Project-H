using NUnit.Framework; // NUnit 테스트 기능
using ProjectH.SaveSystem; // 골드 문구 기능
using ProjectH.UI; // UI 스킨 공용 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text · Button 기능

namespace ProjectH.Tests.EditMode // 편집 모드 테스트 영역
{
    public sealed class UiSkinScreenTests // Day84 상태 색 · 내비 아이콘 · 재화 아이콘 · 아이템 칸 테스트
    {
        private GameObject root; // 테스트 화면 루트

        [SetUp] // 테스트 준비 표시
        public void SetUp() // 빈 루트 준비
        {
            root = new GameObject("Canvas", typeof(RectTransform)); // 루트
        }

        [TearDown] // 테스트 정리 표시
        public void TearDown() // 루트 제거
        {
            Object.DestroyImmediate(root); // 루트와 하위 제거
        }

        [Test] // 상태 색 : 스킨을 입힌 그림에는 옅은 기를, 색 상자에는 예전 색을 넣는다
        public void SetState_UsesTintOnSkinAndPlainColorOnBoxes() // 상태 색 테스트
        {
            Color plainSelected = new Color(0.45f, 0.22f, 0.10f, 1f); // 예전 선택 색
            Color plainNormal = new Color(0.15f, 0.12f, 0.12f, 1f); // 예전 기본 색
            Image box = RuntimeUiKit.CreateImage(root.transform, "Box", plainNormal); // 색 상자
            UiSkinKit.SetSelected(box, true, plainSelected, plainNormal); // 고름
            Assert.That(box.color, Is.EqualTo(plainSelected)); // 색 상자는 예전 색

            Image card = RuntimeUiKit.CreateImage(root.transform, "Card", plainNormal); // 카드
            Assert.That(UiSkinKit.Panel(card, UiSkin.Slot), Is.True); // 스킨 적용
            UiSkinKit.SetSelected(card, true, plainSelected, plainNormal); // 고름
            Assert.That(card.color, Is.EqualTo(UiSkinKit.SelectedTint)); // 금빛 기
            UiSkinKit.SetSelected(card, false, plainSelected, plainNormal); // 고르지 않음
            Assert.That(card.color, Is.EqualTo(Color.white)); // 그림 색 그대로
            Assert.That(() => UiSkinKit.SetState(null, Color.white, Color.white), Throws.Nothing); // null 안전
        }

        [Test] // 색감 남기기 : 어두운 버튼 색도 밝고 옅은 기가 된다 (그림이 까맣게 죽지 않는다)
        public void Hint_TurnsDarkButtonColorsIntoLightTints() // 옅은 기 테스트
        {
            Color green = UiSkinKit.Hint(new Color(0.36f, 0.62f, 0.40f, 1f)); // 초록 버튼
            Assert.That(green.g, Is.GreaterThan(green.r)); // 초록 기가 남음
            Assert.That(Mathf.Min(green.r, Mathf.Min(green.g, green.b)), Is.GreaterThan(0.7f)); // 충분히 밝음
            Assert.That(UiSkinKit.Hint(Color.black), Is.EqualTo(Color.white)); // 검정은 기 없음
        }

        [Test] // 로비 내비 : 글자에 맞는 아이콘을 버튼 위쪽에 한 번만 올리고, 모르는 버튼은 그대로 둔다
        public void LobbyNavIcons_AddOneIconPerKnownButton() // 내비 아이콘 테스트
        {
            GameObject navigation = new GameObject(LobbyNavIcons.NavigationName, typeof(RectTransform)); // 하단 내비
            navigation.transform.SetParent(root.transform, false); // 루트 아래
            Button adventure = CreateButton(navigation.transform, "Nav_모험", "모험"); // 아는 버튼
            Button unknown = CreateButton(navigation.transform, "Nav_기타", "기타"); // 모르는 버튼

            Assert.That(LobbyNavIcons.Apply(root.transform), Is.EqualTo(1)); // 하나만 올림
            Assert.That(adventure.transform.Find(LobbyNavIcons.IconName), Is.Not.Null); // 아이콘
            Assert.That(adventure.transform.Find("Label").GetComponent<Text>().rectTransform.anchorMax.y, Is.LessThan(0.5f)); // 글자는 아래쪽
            Assert.That(unknown.transform.Find(LobbyNavIcons.IconName), Is.Null); // 그대로
            Assert.That(LobbyNavIcons.Apply(root.transform), Is.EqualTo(0)); // 다시 불러도 또 올리지 않음
            Assert.That(LobbyNavIcons.Apply(null), Is.EqualTo(0)); // null 안전
            Assert.That(LobbyNavIcons.GetKey("대장간"), Is.EqualTo("nav_blacksmith")); // 대응표
            Assert.That(LobbyNavIcons.GetKey("없는 버튼"), Is.Null); // 없는 글자
        }

        [Test] // 재화 아이콘 : 글자 왼쪽에 한 번만 만들고, 아이콘이 있으면 숫자만 보여 준다
        public void CurrencyIcon_IsAddedOnceAndShortensTheLabel() // 재화 아이콘 테스트
        {
            Image chip = RuntimeUiKit.CreateImage(root.transform, "GoldChip", Color.white); // 재화 칩
            Text value = RuntimeUiKit.CreateText(chip.transform, "Value", string.Empty, 18, Color.black); // 재화 글자
            Assert.That(UiSkinKit.CurrencyIcon(value, "gold"), Is.True); // 아이콘 추가
            Assert.That(UiSkinKit.CurrencyIcon(value, "gold"), Is.True); // 이미 있음
            Assert.That(chip.transform.childCount, Is.EqualTo(2)); // 글자 + 아이콘 하나
            Assert.That(value.rectTransform.anchorMin.x, Is.GreaterThan(0.2f)); // 글자는 아이콘 오른쪽
            Assert.That(UiSkinKit.CurrencyIcon(null, "gold"), Is.False); // null 안전
            Assert.That(GoldCurrencyService.Format(5548, true), Is.EqualTo("5,548")); // 아이콘이 있으면 숫자만
            Assert.That(GoldCurrencyService.Format(5548, false), Is.EqualTo("골드  5,548")); // 없으면 글자와 함께
        }

        [Test] // 아이템 칸 : 정식 칸 그림을 쓰고, 등급 색은 밝게 섞어 테두리에 입힌다
        public void ItemIcon_UsesSlotSkinWithBrightGradeTint() // 아이템 칸 테스트
        {
            Image frame = ItemIconView.Create(root.transform, "Icon"); // 아이콘 칸
            Assert.That(frame.sprite, Is.SameAs(UiSkin.Get(UiSkin.Slot))); // 정식 칸
            Assert.That(frame.transform.Find("Inner").gameObject.activeSelf, Is.False); // 안쪽 어두운 칸은 그림에 있다
            ItemIconView.Apply(frame, null, null); // 빈 칸
            Assert.That(frame.color.r, Is.GreaterThan(0.5f)); // 어두운 색을 그대로 곱하지 않는다
        }

        [Test] // 룬 칸 : 아이템 칸과 같은 정식 칸 그림을 쓰고 그림 색 그대로 둔다 (Day85)
        public void RuneIcon_UsesSlotSkin() // 룬 칸 테스트
        {
            Image frame = RuneIconView.Create(root.transform, "Rune"); // 룬 칸
            Assert.That(frame.sprite, Is.SameAs(UiSkin.Get(UiSkin.Slot))); // 정식 칸
            Assert.That(frame.color, Is.EqualTo(Color.white)); // 어두운 상자 색을 곱하지 않는다
            Assert.That(() => RuneIconView.Apply(frame, null), Throws.Nothing); // 빈 칸도 안전
        }

        [Test] // 옅은 상태 색(선물 취향 · 보상 단계)은 카드 그림 위에 그대로 입혀도 서로 구분된다 (Day85)
        public void PastelStateColors_StayDistinctOnCards() // 옅은 상태 색 테스트
        {
            Color love = new Color(1f, 0.80f, 0.86f, 1f); // 아주 좋아함 분홍
            Color like = new Color(0.84f, 0.94f, 0.82f, 1f); // 좋아함 연두
            Image loveCard = RuntimeUiKit.CreateImage(root.transform, "Love", love); // 분홍 카드
            Image likeCard = RuntimeUiKit.CreateImage(root.transform, "Like", like); // 연두 카드
            Assert.That(UiSkinKit.Panel(loveCard, UiSkin.Card, love), Is.True); // 카드 + 분홍
            Assert.That(UiSkinKit.Panel(likeCard, UiSkin.Card, like), Is.True); // 카드 + 연두
            Assert.That(loveCard.sprite, Is.SameAs(likeCard.sprite)); // 같은 카드 그림
            Assert.That(loveCard.color, Is.Not.EqualTo(likeCard.color)); // 색은 서로 다름
            Assert.That(loveCard.color.r, Is.GreaterThan(loveCard.color.g)); // 분홍은 붉은 쪽
            Assert.That(likeCard.color.g, Is.GreaterThan(likeCard.color.r)); // 연두는 초록 쪽
        }

        private static Button CreateButton(Transform parent, string name, string label) // 테스트용 버튼
        {
            Button button = RuntimeUiKit.CreateButton(parent, name, Color.white); // 버튼
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(button.transform, "Label", label, 21, Color.black).rectTransform); // 글자
            return button; // 반환
        }
    }
}
