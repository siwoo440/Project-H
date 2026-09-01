using UnityEngine; // Unity 기본 기능
using UnityEngine.Events; // Unity 버튼 이벤트 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 결과 Overlay 방지
    public sealed class BattleResultOverlay : MonoBehaviour // 15일차 임시 전투 결과 Overlay
    {
        public static BattleResultOverlay ShowRuntime(BattleOutcome outcome, UnityAction returnAction) // Runtime 전투 결과 Overlay 생성
        {
            GameObject canvasObject = new GameObject("BattleResultOverlayRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(BattleResultOverlay)); // 결과 Overlay Canvas 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 결과 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 500; // 전투 UI 위 결과 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 결과 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            BattleResultOverlay overlay = canvasObject.GetComponent<BattleResultOverlay>(); // 결과 Overlay 컴포넌트 조회
            Image dim = CreateImage(canvasObject.transform, "Dim", new Color(0f, 0f, 0f, 0.64f)); // 전투 종료 배경 어둡게 표시
            Stretch(dim.rectTransform); // 어두운 배경 전체 화면 확장
            Image panel = CreateImage(canvasObject.transform, "ResultPanel", new Color(0.96f, 0.94f, 0.87f, 0.98f)); // 결과 중앙 패널 생성
            SetRect(panel.rectTransform, new Vector2(0.30f, 0.29f), new Vector2(0.70f, 0.71f)); // 결과 중앙 패널 배치
            Text title = CreateText(panel.transform, "ResultTitle", GetTitle(outcome), 56, FontStyle.Bold, new Color(0.12f, 0.20f, 0.34f, 1f)); // 승패 제목 생성
            SetRect(title.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.85f)); // 승패 제목 배치
            Text subtitle = CreateText(panel.transform, "ResultSubtitle", GetSubtitle(outcome), 24, FontStyle.Normal, new Color(0.20f, 0.26f, 0.36f, 1f)); // 승패 설명 생성
            SetRect(subtitle.rectTransform, new Vector2(0.10f, 0.38f), new Vector2(0.90f, 0.57f)); // 승패 설명 배치
            Button returnButton = CreateButton(panel.transform, "ReturnDungeonButton", "던전 선택으로"); // 던전 선택 복귀 버튼 생성
            SetRect(returnButton.GetComponent<RectTransform>(), new Vector2(0.22f, 0.13f), new Vector2(0.78f, 0.31f)); // 던전 선택 복귀 버튼 배치

            if (returnAction != null) // 복귀 이벤트 존재 확인
            {
                returnButton.onClick.AddListener(returnAction); // 던전 선택 복귀 이벤트 연결
            }

            return overlay; // 생성된 결과 Overlay 반환
        }

        private static string GetTitle(BattleOutcome outcome) // 승패 제목 반환
        {
            return outcome == BattleOutcome.Victory ? "VICTORY" : "DEFEAT"; // 승리 또는 패배 제목 반환
        }

        private static string GetSubtitle(BattleOutcome outcome) // 승패 설명 반환
        {
            return outcome == BattleOutcome.Victory ? "모든 적을 쓰러뜨렸습니다." : "파티 전원이 전투 불능이 되었습니다."; // 승패 설명 반환
        }

        private static Image CreateImage(Transform parent, string name, Color color) // 공통 이미지 생성
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image)); // UI 이미지 객체 생성
            imageObject.transform.SetParent(parent, false); // UI 부모 연결
            Image image = imageObject.GetComponent<Image>(); // UI Image 컴포넌트 조회
            image.color = color; // UI 이미지 색상 설정
            image.raycastTarget = true; // 결과 UI 입력 차단 활성화
            return image; // 생성 Image 반환
        }

        private static Text CreateText(Transform parent, string name, string value, int size, FontStyle style, Color color) // 공통 결과 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 결과 Text 객체 생성
            textObject.transform.SetParent(parent, false); // 결과 Text 부모 연결
            Text text = textObject.GetComponent<Text>(); // 결과 Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // 결과 Text 내용 설정
            text.fontSize = size; // 결과 Text 크기 설정
            text.fontStyle = style; // 결과 Text 스타일 설정
            text.color = color; // 결과 Text 색상 설정
            text.alignment = TextAnchor.MiddleCenter; // 결과 Text 중앙 정렬
            text.alignByGeometry = true; // 글리프 기준 정렬 적용
            text.resizeTextForBestFit = true; // 자동 Text 크기 적용
            text.resizeTextMinSize = 14; // 최소 Text 크기 설정
            text.resizeTextMaxSize = size; // 최대 Text 크기 설정
            text.raycastTarget = false; // 결과 Text 입력 비활성화
            return text; // 생성 Text 반환
        }

        private static Button CreateButton(Transform parent, string name, string label) // 결과 버튼 생성
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 결과 Button 객체 생성
            buttonObject.transform.SetParent(parent, false); // 결과 Button 부모 연결
            Image image = buttonObject.GetComponent<Image>(); // 결과 Button 이미지 조회
            image.color = new Color(0.18f, 0.30f, 0.48f, 1f); // 결과 Button 배경색 설정
            Button button = buttonObject.GetComponent<Button>(); // 결과 Button 컴포넌트 조회
            button.targetGraphic = image; // 결과 Button 대상 그래픽 연결
            Text text = CreateText(buttonObject.transform, "Label", label, 24, FontStyle.Bold, Color.white); // 결과 Button 라벨 생성
            Stretch(text.rectTransform, 6f); // 결과 Button 라벨 전체 확장
            return button; // 생성 Button 반환
        }

        private static void Stretch(RectTransform rect) // RectTransform 전체 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }

        private static void Stretch(RectTransform rect, float padding) // RectTransform 내부 여백 확장
        {
            rect.anchorMin = Vector2.zero; // 최소 앵커 전체 설정
            rect.anchorMax = Vector2.one; // 최대 앵커 전체 설정
            rect.offsetMin = new Vector2(padding, padding); // 최소 내부 여백 설정
            rect.offsetMax = new Vector2(-padding, -padding); // 최대 내부 여백 설정
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max) // RectTransform 앵커 배치
        {
            rect.anchorMin = min; // 최소 앵커 설정
            rect.anchorMax = max; // 최대 앵커 설정
            rect.offsetMin = Vector2.zero; // 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 최대 오프셋 초기화
        }
    }
}
