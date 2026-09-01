using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleHudButtonLabelUtility // 전투 HUD 버튼 단일 라벨 정리 기능
    {
        public static Text EnsureSingleLabel(Button button, string defaultText) // 버튼 하위 Text를 하나만 유지하며 반환
        {
            if (button == null) // 대상 버튼 확인
            {
                return null; // 단일 라벨 정리 중단
            }

            Text[] labels = button.GetComponentsInChildren<Text>(true); // 버튼 하위 Text 목록 조회
            Text primary = labels.Length > 0 ? labels[0] : null; // 유지할 첫 번째 라벨 선택

            if (primary == null) // 기존 라벨 존재 확인
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text)); // 신규 라벨 객체 생성
                labelObject.transform.SetParent(button.transform, false); // 신규 라벨 버튼 하위 연결
                primary = labelObject.GetComponent<Text>(); // 신규 라벨 Text 조회
                primary.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            }

            for (int index = labels.Length - 1; index >= 0; index--) // 중복 라벨 역순 정리
            {
                Text label = labels[index]; // 현재 라벨 조회

                if (label == null || label == primary) // 유지 라벨 여부 확인
                {
                    continue; // 유지 대상 또는 null 라벨 제외
                }

                Object.DestroyImmediate(label.gameObject); // 중복 라벨 즉시 제거
            }

            primary.name = "Label"; // 단일 라벨 이름 통일
            primary.text = defaultText; // 단일 라벨 기본 문구 적용
            primary.alignment = TextAnchor.MiddleCenter; // 단일 라벨 중앙 정렬
            primary.alignByGeometry = true; // 단일 라벨 글리프 정렬 적용
            primary.resizeTextForBestFit = true; // 단일 라벨 자동 크기 적용
            primary.resizeTextMinSize = 10; // 단일 라벨 최소 크기 적용
            primary.raycastTarget = false; // 단일 라벨 입력 비활성화
            RectTransform rect = primary.rectTransform; // 단일 라벨 RectTransform 조회
            rect.anchorMin = Vector2.zero; // 단일 라벨 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 단일 라벨 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(4f, 4f); // 단일 라벨 최소 내부 여백 설정
            rect.offsetMax = new Vector2(-4f, -4f); // 단일 라벨 최대 내부 여백 설정
            return primary; // 단일 라벨 반환
        }
    }
}
