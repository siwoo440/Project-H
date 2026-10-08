using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text · Button 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class UiSkinKit // 화면 부품에 스킨을 입히는 공용 기능 (Day83 신규 — 스킨 파일이 없으면 false를 돌려주고 색 상자를 그대로 둔다)
    {
        public static readonly Color TextOnLight = new Color(0.13f, 0.14f, 0.18f, 1f); // 밝은 조각 위의 글자색
        public static readonly Color TextOnDark = new Color(0.96f, 0.95f, 0.90f, 1f); // 어두운 조각 위의 글자색
        public static readonly Color DangerTint = new Color(1f, 0.66f, 0.66f, 1f); // 나가기 · 지우기처럼 조심할 버튼에 입히는 붉은 기
        public static readonly Color DimTint = new Color(0.62f, 0.64f, 0.70f, 1f); // 빈 칸 · 잠긴 칸에 입히는 어두운 기

        public static bool Panel(Image image, string key) => Panel(image, key, Color.white); // 창 · 카드 · 칸 (그림 색 그대로)

        public static bool Panel(Image image, string key, Color tint) // 창 · 카드 · 칸 : 스킨을 입히고, 코드로 그리던 외곽선은 끈다 (스킨에 테두리가 있다)
        {
            if (!UiSkin.Apply(image, key)) return false; // 대상 · 스킨 없음
            image.color = tint; // 원래의 상자 색 대신 그림 색 (필요하면 옅은 기만)
            foreach (Outline outline in image.GetComponents<Outline>()) outline.enabled = false; // 외곽선 끄기 (그림 위에 겹치면 테두리가 번져 보인다)
            return true; // 적용함
        }

        public static bool Button(Button button, string key) => Button(button, key, Color.white); // 버튼 (그림 색 그대로)

        public static bool Button(Button button, string key, Color tint) // 버튼 : 스킨을 입히고 버튼 바로 아래의 글자를 바탕 밝기에 맞춘다 (글자를 만든 뒤에 부른다)
        {
            if (button == null || !Panel(GetImage(button), key, tint)) return false; // 대상 · 스킨 없음
            Color textColor = UiSkin.IsDark(key) ? TextOnDark : TextOnLight; // 바탕에 맞는 글자색

            foreach (Transform child in button.transform) // 버튼 바로 아래만 (카드 안의 여러 글자는 건드리지 않는다)
            {
                Text label = child.GetComponent<Text>(); // 글자
                if (label != null) label.color = new Color(textColor.r, textColor.g, textColor.b, label.color.a); // 글자색 맞춤
            }

            return true; // 적용함
        }

        public static bool Toggle(Button button, bool on) // 켬 · 끔 · 선택된 탭 : 켜짐은 밝은 금색, 꺼짐은 어두운 청회색
        {
            return Button(button, on ? UiSkin.ButtonTabOn : UiSkin.ButtonTabOff); // 그림 교체 (글자색도 함께)
        }

        public static Image Glyph(Button button, string iconKey, float padding = 10f) // 버튼의 글자 기호(◀ ▶ ✕)를 아이콘으로 (아이콘이 없으면 null — 글자 그대로)
        {
            Sprite sprite = button == null ? null : UiIcon.Get(iconKey); // 아이콘
            if (sprite == null) return null; // 대상 · 아이콘 없음
            Text label = FindLabel(button); // 버튼 글자
            Image icon = RuntimeUiKit.CreateImage(button.transform, "Icon", label == null ? Color.white : label.color); // 아이콘 (글자와 같은 색)
            icon.sprite = sprite; // 그림 적용
            icon.raycastTarget = false; // 클릭은 버튼이 받는다
            icon.preserveAspect = true; // 비율 유지
            RuntimeUiKit.Stretch(icon.rectTransform, padding); // 버튼 안쪽
            if (label != null) label.text = string.Empty; // 글자 기호 비움
            return icon; // 아이콘 반환
        }

        public static Image LeadIcon(Button button, string iconKey, string text) // "◀  로비" 같은 글자 버튼 : 왼쪽에 아이콘을 두고 글자는 기호를 뺀 문구로 (아이콘이 없으면 null — 그대로)
        {
            Sprite sprite = button == null ? null : UiIcon.Get(iconKey); // 아이콘
            Text label = button == null ? null : FindLabel(button); // 버튼 글자
            if (sprite == null || label == null) return null; // 대상 · 아이콘 · 글자 없음
            Image icon = RuntimeUiKit.CreateImage(button.transform, "Icon", label.color); // 아이콘 (글자와 같은 색)
            icon.sprite = sprite; // 그림 적용
            icon.raycastTarget = false; // 클릭은 버튼이 받는다
            icon.preserveAspect = true; // 비율 유지
            RuntimeUiKit.SetRect(icon.rectTransform, new Vector2(0.06f, 0.20f), new Vector2(0.28f, 0.80f)); // 버튼 왼쪽
            label.text = text; // 기호를 뺀 문구
            RectTransform labelRect = label.rectTransform; // 글자 영역
            labelRect.anchorMin = new Vector2(0.26f, labelRect.anchorMin.y); // 아이콘 오른쪽으로
            return icon; // 아이콘 반환
        }

        private static Image GetImage(Button button) // 버튼의 바탕 이미지
        {
            Image target = button.targetGraphic as Image; // 색 전환 대상
            return target != null ? target : button.GetComponent<Image>(); // 없으면 버튼의 이미지
        }

        private static Text FindLabel(Button button) // 버튼 바로 아래의 첫 글자
        {
            foreach (Transform child in button.transform) // 자식 순회
            {
                Text label = child.GetComponent<Text>(); // 글자
                if (label != null) return label; // 첫 글자
            }

            return null; // 없음
        }
    }
}
