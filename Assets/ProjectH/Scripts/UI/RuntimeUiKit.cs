using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    public static class RuntimeUiKit // Runtime UI 생성 공용 헬퍼 (최적화 정리, 기존 각 컨트롤러 중복 구현 통합)
    {
        private const string BuiltinFontName = "LegacyRuntime.ttf"; // Unity 기본 폰트 이름
        public const string GameFontResourcePath = "Fonts/GameFont"; // 정식 폰트 Resources 경로 (Day73 추가 — 여기에 폰트를 넣으면 전체 글자가 바뀐다)
        private static Font defaultFont; // 기본 폰트 캐시 (매 텍스트 생성마다 조회하지 않도록)

        public static Font DefaultFont // 기본 폰트 반환 (정식 폰트 → Unity 기본 폰트)
        {
            get
            {
                if (defaultFont == null) // 캐시 확인
                {
                    defaultFont = Resources.Load<Font>(GameFontResourcePath); // 정식 폰트 우선 (Day73 추가)
                }

                if (defaultFont == null) // 정식 폰트 없음
                {
                    defaultFont = Resources.GetBuiltinResource<Font>(BuiltinFontName); // 기본 폰트 1회 조회
                }

                return defaultFont; // 캐시 폰트 반환
            }
        }

        public static Image CreateImage(Transform parent, string name, Color color) // 공통 UI 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // UI 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // UI 이미지 부모 연결
            Image image = imageObject.GetComponent<Image>(); // UI Image 컴포넌트 조회
            image.color = color; // UI 이미지 색상 적용
            return image; // 생성 UI 이미지 반환
        }

        public static Text CreateText(Transform parent, string name, string value, int fontSize, Color color, FontStyle style = FontStyle.Bold, TextAnchor alignment = TextAnchor.MiddleCenter) // 공통 UI 텍스트 생성 (기본 설정만 적용, 파일별 차이는 체인 옵션으로 추가)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // UI 텍스트 부모 연결
            Text text = textObject.GetComponent<Text>(); // UI Text 컴포넌트 조회
            text.font = DefaultFont; // 기본 폰트 적용
            text.text = value ?? string.Empty; // 문구 적용
            text.fontSize = fontSize; // 크기 적용
            text.fontStyle = style; // 굵기 적용
            text.color = color; // 색상 적용
            text.alignment = alignment; // 정렬 적용
            text.raycastTarget = false; // 텍스트 클릭 차단 비활성화 (버튼 입력 방해 방지)
            return text; // 생성 UI 텍스트 반환
        }

        public static Text Overflow(this Text text) // 좁은 영역에서도 잘리지 않도록 가로·세로 넘침 허용
        {
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 가로 넘침 허용
            text.verticalOverflow = VerticalWrapMode.Overflow; // 세로 넘침 허용
            return text; // 체인 반환
        }

        public static Text Wrap(this Text text) // 가로 줄바꿈·세로 잘림
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 가로 줄바꿈 적용
            text.verticalOverflow = VerticalWrapMode.Truncate; // 세로 영역 제한
            return text; // 체인 반환
        }

        public static Text BestFit(this Text text, int minSize) // 영역에 맞춰 글자 크기 자동 축소 (최대는 지정 크기)
        {
            text.resizeTextForBestFit = true; // 자동 크기 맞춤 활성화
            text.resizeTextMinSize = minSize; // 최소 크기 적용
            text.resizeTextMaxSize = text.fontSize; // 최대 크기는 지정 크기
            return text; // 체인 반환
        }

        public static Text AlignByGeometry(this Text text) // 글리프 실제 모양 기준 정렬
        {
            text.alignByGeometry = true; // 모양 기준 정렬 활성화
            return text; // 체인 반환
        }

        public static Text Outlined(this Text text, Color color, Vector2 distance) // 외곽선 추가 (밝은 배경 가독성)
        {
            Outline outline = text.gameObject.AddComponent<Outline>(); // 외곽선 컴포넌트 추가
            outline.effectColor = color; // 외곽선 색상 적용
            outline.effectDistance = distance; // 외곽선 두께 적용
            return text; // 체인 반환
        }

        public static Button CreateButton(Transform parent, string name, Color color, bool assignTargetGraphic = true) // 공통 UI 버튼 생성 (라벨은 호출 측에서 추가)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // UI 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // UI 버튼 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // UI 버튼 이미지 조회
            image.color = color; // UI 버튼 색상 적용
            Button button = buttonObject.GetComponent<Button>(); // UI Button 컴포넌트 조회

            if (assignTargetGraphic) // 색 전환 대상 연결 여부 확인 (캐릭터 화면은 기존 동작 유지를 위해 미연결)
            {
                button.targetGraphic = image; // 버튼 색 전환 대상 연결
            }

            button.onClick.AddListener(() => ProjectH.Core.AudioService.PlaySfx(ProjectH.Core.AudioCatalog.SfxClick)); // 누를 때 소리 (Day73 추가 — 여기 한 곳에서 모든 런타임 버튼에 적용)
            return button; // 생성 UI 버튼 반환
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
