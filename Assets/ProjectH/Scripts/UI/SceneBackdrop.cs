using UnityEngine; // Unity 기본 기능
using UnityEngine.SceneManagement; // Unity 씬 기능
using UnityEngine.UI; // Image 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class SceneBackdrop // 씬에 놓인 배경 이미지를 배경 그림으로 바꾸는 공용 기능 (Day80 신규 — 전투 · 로비가 함께 쓴다)
    {
        public static Image Find(Scene scene, string objectName) // 씬에서 이름으로 배경 이미지 찾기
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(objectName)) return null; // 잘못된 입력

            foreach (GameObject root in scene.GetRootGameObjects()) // 루트 순회
            {
                foreach (Image image in root.GetComponentsInChildren<Image>(true)) // 하위 이미지 순회
                {
                    if (image.gameObject.name == objectName) return image; // 이름 일치
                }
            }

            return null; // 없음
        }

        public static bool Apply(Image background, string backgroundKey, float dimAlpha, string dimObjectName) // 배경 그림 적용 (그림이 없으면 false — 원래 배경을 그대로 둔다)
        {
            if (background == null || !DialogueArtFactory.HasBackgroundArt(backgroundKey)) return false; // 대상·그림 없음
            background.sprite = DialogueArtFactory.GetBackground(backgroundKey); // 배경 그림
            background.color = Color.white; // 원래 색 그대로
            BackgroundFit.Apply(background); // 화면 비율이 달라도 늘리지 않고 잘라서 채움
            EnsureDim(background, dimAlpha, dimObjectName); // 어둡게 덮는 막
            return true; // 적용함
        }

        private static void EnsureDim(Image background, float dimAlpha, string dimObjectName) // 배경 바로 위에 어두운 막을 한 겹 (글자와 캐릭터가 배경에 묻히지 않게)
        {
            Transform parent = background.transform.parent; // 배경의 부모
            if (parent == null || dimAlpha <= 0f || string.IsNullOrEmpty(dimObjectName) || parent.Find(dimObjectName) != null) return; // 막이 필요 없음·이미 있음
            Image dim = RuntimeUiKit.CreateImage(parent, dimObjectName, new Color(0f, 0f, 0f, dimAlpha)); // 검은 반투명 막
            dim.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(dim.rectTransform); // 화면 전체
            dim.transform.SetSiblingIndex(background.transform.GetSiblingIndex() + 1); // 배경 바로 위
        }
    }
}
