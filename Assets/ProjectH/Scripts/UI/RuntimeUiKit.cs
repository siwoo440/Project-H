using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class RuntimeUiKit // Runtime UI 생성 공용 헬퍼 (최적화 정리, 기존 각 컨트롤러 중복 구현 통합)
    {
        public static Image CreateImage(Transform parent, string name, Color color) // 공통 UI 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // UI 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // UI 이미지 부모 연결
            Image image = imageObject.GetComponent<Image>(); // UI Image 컴포넌트 조회
            image.color = color; // UI 이미지 색상 적용
            return image; // 생성 UI 이미지 반환
        }

        public static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax) // 앵커 기반 UI 배치
        {
            rect.anchorMin = anchorMin; // 최소 앵커 적용
            rect.anchorMax = anchorMax; // 최대 앵커 적용
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        public static void Stretch(RectTransform rect, float padding = 0f) // 부모 전체 영역 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 여백 적용
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 여백 적용
        }
    }
}
