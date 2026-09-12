using System; // 콜백 자료형
using ProjectH.SaveSystem; // 이름 규칙 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 생성 방지
    public sealed class HeroNameInputView : MonoBehaviour // 주인공 이름 입력 화면 (Day68 신규 — 새 게임에서 한 번만)
    {
        private Action<string> onConfirm; // 확인 콜백
        private InputField field; // 입력 칸

        public static HeroNameInputView Open(Action<string> confirmed) // 이름 입력 열기
        {
            GameObject root = new GameObject("HeroNameInputView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 700; // 대화 화면(600)보다 위
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            HeroNameInputView view = root.AddComponent<HeroNameInputView>(); // 컴포넌트 추가
            view.onConfirm = confirmed; // 콜백 저장
            view.Build(); // 화면 구성
            return view; // 반환
        }

        private void Build() // 화면 구성 (어두운 막 + 가운데 입력 상자)
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.86f)); // 어두운 막
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체
            Image box = RuntimeUiKit.CreateImage(transform, "Box", new Color(0.08f, 0.09f, 0.13f, 0.98f)); // 입력 상자
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.28f, 0.34f), new Vector2(0.72f, 0.66f)); // 가운데
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.92f, 0.78f, 0.42f, 0.9f); // 금색 테두리
            Text title = RuntimeUiKit.CreateText(box.transform, "Title", "당신의 이름을 알려 주세요", 30, Color.white, FontStyle.Bold); // 제목
            RuntimeUiKit.SetRect(title.rectTransform, new Vector2(0.06f, 0.70f), new Vector2(0.94f, 0.90f)); // 위
            Text hint = RuntimeUiKit.CreateText(box.transform, "Hint", $"{HeroNameService.MaxLength}자까지 · 비우면 '{HeroNameService.DefaultName}'으로 시작합니다", 17, new Color(0.80f, 0.82f, 0.88f, 1f), FontStyle.Normal).Wrap(); // 안내
            RuntimeUiKit.SetRect(hint.rectTransform, new Vector2(0.06f, 0.56f), new Vector2(0.94f, 0.70f)); // 제목 아래
            Image inputBox = RuntimeUiKit.CreateImage(box.transform, "Input", new Color(0.14f, 0.16f, 0.22f, 1f)); // 입력 칸 배경
            RuntimeUiKit.SetRect(inputBox.rectTransform, new Vector2(0.10f, 0.34f), new Vector2(0.90f, 0.54f)); // 가운데
            Text inputText = RuntimeUiKit.CreateText(inputBox.transform, "Text", string.Empty, 28, Color.white, FontStyle.Bold); // 입력 글자
            RuntimeUiKit.Stretch(inputText.rectTransform, 12f); // 여백
            Text placeholder = RuntimeUiKit.CreateText(inputBox.transform, "Placeholder", HeroNameService.DefaultName, 28, new Color(0.60f, 0.62f, 0.68f, 1f), FontStyle.Italic); // 안내 글자
            RuntimeUiKit.Stretch(placeholder.rectTransform, 12f); // 여백
            field = inputBox.gameObject.AddComponent<InputField>(); // 입력 기능
            field.textComponent = inputText; // 글자 연결
            field.placeholder = placeholder; // 안내 글자 연결
            field.characterLimit = HeroNameService.MaxLength; // 글자 수 제한
            field.lineType = InputField.LineType.SingleLine; // 한 줄
            field.onEndEdit.AddListener(_ => { if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Confirm(); }); // 엔터로 확인
            Button confirm = RuntimeUiKit.CreateButton(box.transform, "Confirm", new Color(0.78f, 0.50f, 0.18f, 1f)); // 확인 버튼
            RuntimeUiKit.SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.24f, 0.10f), new Vector2(0.76f, 0.28f)); // 아래
            Text confirmLabel = RuntimeUiKit.CreateText(confirm.transform, "Label", "이 이름으로 시작", 26, Color.white, FontStyle.Bold); // 버튼 글자
            RuntimeUiKit.Stretch(confirmLabel.rectTransform); // 버튼 채움
            confirm.onClick.AddListener(Confirm); // 확인 연결
            field.Select(); // 바로 입력 가능
            field.ActivateInputField(); // 커서 표시
        }

        private void Confirm() // 확인 : 이름 전달 후 닫기
        {
            string value = field == null ? string.Empty : field.text; // 입력 값
            Action<string> callback = onConfirm; // 콜백 복사
            onConfirm = null; // 중복 방지
            Destroy(gameObject); // 화면 닫기
            callback?.Invoke(HeroNameService.Sanitize(value)); // 이름 전달
        }
    }
}
