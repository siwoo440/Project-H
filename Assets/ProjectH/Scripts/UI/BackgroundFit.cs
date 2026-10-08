using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Image·비율 맞춤 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class BackgroundFit // 배경 '잘라서 채우기' (Day76 신규 — 화면 비율이 그림과 달라도 늘어나지 않고, 남는 쪽만 화면 밖으로 잘린다)
    {
        public const float DefaultAspect = 16f / 9f; // 그림이 없을 때 쓰는 기준 비율

        public static float GetAspect(Sprite sprite) // 그림의 가로 ÷ 세로
        {
            if (sprite == null || sprite.rect.height <= 0f) return DefaultAspect; // 그림 없음
            return sprite.rect.width / sprite.rect.height; // 비율 반환
        }

        public static void Apply(Image image) // 부모를 가득 덮게 맞춤 (자식이 없는 전체 화면 배경에만 쓸 것 — 크기가 부모보다 커진다)
        {
            if (image == null) return; // 대상 없음
            AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>(); // 기존 맞춤 컴포넌트
            if (fitter == null) fitter = image.gameObject.AddComponent<AspectRatioFitter>(); // 없으면 추가
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // 부모를 다 덮을 때까지 키움
            fitter.aspectRatio = GetAspect(image.sprite); // 그림 비율 적용
        }
    }
}
