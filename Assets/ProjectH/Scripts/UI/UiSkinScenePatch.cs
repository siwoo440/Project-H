using System.Collections.Generic; // 사전 자료형
using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // 씬 전환 이벤트 기능
using UnityEngine.UI; // Image · Text 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class UiSkinScenePatch // 씬에 미리 놓인 임시 UI 그림(4일차 Prototype)을 정식 스킨으로 바꾸는 연결 (Day82 신규 — 씬 파일은 건드리지 않는다. 스킨 파일이 없으면 임시 그림 그대로)
    {
        public static readonly Color LightTextColor = new Color(0.96f, 0.95f, 0.90f, 1f); // 어두운 스킨 위에 올릴 글자색

        private static readonly Dictionary<string, string> Map = new Dictionary<string, string> // 임시 그림 이름 → 스킨 이름
        {
            { "frame_panel", UiSkin.PanelLight }, // 크림색 창 → 밝은 창
            { "frame_topbar", UiSkin.BarTop }, // 상단 바 (밝음 → 남색 : 바 위의 글자는 밝게 바꾼다)
            { "frame_bottombar", UiSkin.BarBottom }, // 하단 바
            { "frame_portrait", UiSkin.Card }, // 초상화 틀 → 카드
            { "frame_party_slot", UiSkin.Card }, // 파티 칸 · 전투 캐릭터 카드 → 카드
            { "frame_battle_portrait", UiSkin.Card }, // 전투 초상화 틀 → 카드
            { "frame_dungeon_card", UiSkin.Card }, // 던전 카드 → 카드
            { "frame_reward", UiSkin.Card }, // 보상 칸 → 카드
            { "button_primary", UiSkin.ButtonPrimary }, // 주요 버튼
            { "button_secondary", UiSkin.ButtonSecondary }, // 보조 버튼
            { "button_small", UiSkin.ButtonSecondary }, // 작은 버튼 · 재화 칩 → 보조 버튼
            { "button_nav", UiSkin.ButtonTabOn } // 내비 · 편성 버튼 → 밝은 금색 (코드가 이 버튼에 어두운 글자와 선택 색을 입히므로 밝은 스킨을 쓴다)
        };

        public static IReadOnlyDictionary<string, string> Mapping => Map; // 대응표 (점검 · 테스트용)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 로드 전 이벤트 구독 지정
        private static void Register() // 씬 로드 이벤트 등록
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // 중복 구독 방지
            SceneManager.sceneLoaded += OnSceneLoaded; // 모든 씬에서 스킨 교체
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 처리
        {
            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 순회
            {
                Apply(root.transform); // 하위 그림 교체
            }
        }

        public static int Apply(Transform root) // 하위의 임시 그림을 스킨으로 교체 (꺼져 있는 것 포함, 바꾼 개수 반환)
        {
            if (root == null) return 0; // 대상 없음
            int changed = 0; // 바꾼 개수

            foreach (Image image in root.GetComponentsInChildren<Image>(true)) // 그림 순회
            {
                if (image.sprite == null || UiSkin.IsSkinSprite(image.sprite)) continue; // 그림 없음 · 이미 정식 스킨 (임시 그림과 이름이 같은 조각이 있어 먼저 거른다)
                if (!Map.TryGetValue(image.sprite.name, out string key)) continue; // 임시 그림이 아님
                if (!UiSkin.Apply(image, key)) continue; // 스킨 파일 없음
                if (UiSkin.IsDark(key)) LightenTexts(image.transform); // 어두운 스킨 위의 어두운 글자는 밝게
                changed++; // 집계
            }

            return changed; // 개수 반환
        }

        private static void LightenTexts(Transform owner) // 이 그림 바로 위에 놓인 어두운 글자를 밝게 (자기 바탕을 가진 자식 아래의 글자는 그 바탕이 정한다)
        {
            foreach (Transform child in owner) // 자식 순회
            {
                Image background = child.GetComponent<Image>(); // 자식의 바탕
                if (background != null && background.color.a > 0.5f) continue; // 자기 바탕이 있는 자식 (버튼 · 칩)
                Text text = child.GetComponent<Text>(); // 글자
                if (text != null && GetLuminance(text.color) < 0.45f) text.color = new Color(LightTextColor.r, LightTextColor.g, LightTextColor.b, text.color.a); // 어두운 글자만 밝게
                LightenTexts(child); // 바탕이 없는 묶음 아래도 확인
            }
        }

        private static float GetLuminance(Color color) => (0.299f * color.r) + (0.587f * color.g) + (0.114f * color.b); // 밝기
    }
}
