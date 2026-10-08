using System.Collections.Generic; // 사전 자료형
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image · Text · Button 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class LobbyNavIcons // 로비 하단 내비 버튼의 아이콘 (Day84 신규 — 글자만 있던 버튼에 아이콘을 올리고 글자는 아래로. 아이콘이 없는 버튼은 그대로)
    {
        public const string NavigationName = "BottomNavigation"; // 로비 씬의 하단 내비 묶음 이름
        public const string IconName = "NavIcon"; // 버튼 안에 만드는 아이콘 이름

        private static readonly Dictionary<string, string> Keys = new Dictionary<string, string> // 버튼 글자 → 아이콘 이름
        {
            { "모험", "nav_adventure" }, // 모험
            { "캐릭터", "nav_character" }, // 캐릭터
            { "파티", "nav_party" }, // 파티
            { "가방", "nav_bag" }, // 가방
            { "상점", "nav_shop" }, // 상점
            { "대장간", "nav_blacksmith" }, // 대장간
            { "마을", "nav_village" }, // 마을
            { "일기장", "nav_diary" } // 일기장
        };

        public static string GetKey(string label) // 버튼 글자의 아이콘 이름 (없으면 null)
        {
            return !string.IsNullOrEmpty(label) && Keys.TryGetValue(label.Trim(), out string key) ? key : null; // 대응표 조회
        }

        public static int Apply(Transform lobbyRoot) // 하단 내비 버튼에 아이콘 올리기 (여러 번 불러도 한 번만 만든다. 올린 개수 반환)
        {
            Transform navigation = lobbyRoot == null ? null : lobbyRoot.Find(NavigationName); // 하단 내비
            if (navigation == null) return 0; // 내비 없음
            int added = 0; // 올린 개수

            foreach (Button button in navigation.GetComponentsInChildren<Button>(true)) // 내비 버튼 순회
            {
                if (button.transform.Find(IconName) != null) continue; // 이미 올림
                Text label = FindLabel(button); // 버튼 글자
                Sprite sprite = label == null ? null : UiIcon.Get(GetKey(label.text)); // 아이콘
                if (sprite == null) continue; // 글자 · 아이콘 없음
                Image icon = RuntimeUiKit.CreateImage(button.transform, IconName, Color.white); // 아이콘
                icon.sprite = sprite; // 그림 적용
                icon.raycastTarget = false; // 클릭은 버튼이 받는다
                icon.preserveAspect = true; // 비율 유지
                RuntimeUiKit.SetRect(icon.rectTransform, new Vector2(0.18f, 0.36f), new Vector2(0.82f, 0.95f)); // 버튼 위쪽
                RuntimeUiKit.SetRect(label.rectTransform, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.38f)); // 글자는 아래쪽
                label.alignment = TextAnchor.MiddleCenter; // 가운데 정렬
                label.resizeTextForBestFit = true; // 좁아진 자리에 맞춰 줄임
                label.resizeTextMinSize = 10; // 가장 작은 글자
                label.resizeTextMaxSize = label.fontSize; // 가장 큰 글자는 원래 크기
                added++; // 집계
            }

            return added; // 개수 반환
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
