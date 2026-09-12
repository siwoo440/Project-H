using System; // 콜백 자료형
using System.Collections; // 코루틴 기능 (Day71 추가 — 별 반짝임)
using ProjectH.SaveSystem; // 이름 규칙 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 신규 Input System 키 입력 기능 (Day69 수정 — 구형 Input 클래스는 이 프로젝트에서 사용할 수 없음)
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 생성 방지
    public sealed class HeroNameInputView : MonoBehaviour // 주인공 이름 입력 화면 (Day68 신규 — 새 게임에서 한 번만)
    {
        private const int TwinkleCount = 16; // 반짝이는 별 수 (Day71 추가)

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

        private void Build() // 화면 구성 (우주 배경 + 가운데 입력 상자)
        {
            BuildStarfield(); // 밤하늘 배경 (Day71 추가 — 어두운 막 대신 별이 있는 우주)
            Image box = RuntimeUiKit.CreateImage(transform, "Box", new Color(0.06f, 0.07f, 0.14f, 0.90f)); // 입력 상자 (Day71 — 별이 비치도록 살짝 투명한 남색)
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
            field.onEndEdit.AddListener(_ => { if (IsSubmitPressed()) Confirm(); }); // 엔터로 확인 (칸 밖을 눌러 포커스가 풀린 경우에는 확인하지 않음)
            Button confirm = RuntimeUiKit.CreateButton(box.transform, "Confirm", new Color(0.78f, 0.50f, 0.18f, 1f)); // 확인 버튼
            RuntimeUiKit.SetRect(confirm.GetComponent<RectTransform>(), new Vector2(0.24f, 0.10f), new Vector2(0.76f, 0.28f)); // 아래
            Text confirmLabel = RuntimeUiKit.CreateText(confirm.transform, "Label", "이 이름으로 시작", 26, Color.white, FontStyle.Bold); // 버튼 글자
            RuntimeUiKit.Stretch(confirmLabel.rectTransform); // 버튼 채움
            confirm.onClick.AddListener(Confirm); // 확인 연결
            field.Select(); // 바로 입력 가능
            field.ActivateInputField(); // 커서 표시
        }

        private void BuildStarfield() // 우주 배경 구성 (별 그림 + 천천히 커지는 움직임 + 반짝이는 별 + 어둡게 덮기)
        {
            Image sky = RuntimeUiKit.CreateImage(transform, "Starfield", Color.white); // 밤하늘 그림
            sky.sprite = StarfieldArt.GetBackground(); // 별·성운 그림
            sky.type = Image.Type.Simple; // 그대로 채움
            sky.preserveAspect = false; // 화면 비율에 맞춰 늘림
            RuntimeUiKit.Stretch(sky.rectTransform); // 전체
            StartCoroutine(DriftRoutine(sky.rectTransform)); // 아주 느린 확대·축소 (우주가 살아 있는 느낌)

            for (int index = 0; index < TwinkleCount; index++) // 반짝이는 별 배치
            {
                Image star = RuntimeUiKit.CreateImage(sky.transform, "Twinkle", Color.white); // 별 하나
                star.sprite = StarfieldArt.GetStarGlow(); // 반짝임 그림
                star.raycastTarget = false; // 입력 통과
                float size = UnityEngine.Random.Range(0.012f, 0.030f); // 별 크기 (화면 비율)
                float x = UnityEngine.Random.Range(0.04f, 0.96f); // 가로 위치
                float y = UnityEngine.Random.Range(0.06f, 0.94f); // 세로 위치
                RuntimeUiKit.SetRect(star.rectTransform, new Vector2(x - size, y - (size * 1.8f)), new Vector2(x + size, y + (size * 1.8f))); // 배치 (세로가 더 긴 화면 보정)
                star.color = PickTwinkleColor(); // 별 색
                StartCoroutine(TwinkleRoutine(star)); // 깜빡임
            }

            Image veil = RuntimeUiKit.CreateImage(transform, "Veil", new Color(0.02f, 0.02f, 0.06f, 0.42f)); // 글자가 잘 보이도록 살짝 덮기
            veil.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(veil.rectTransform); // 전체
        }

        private static Color PickTwinkleColor() // 반짝이는 별 색 (대부분 흰색, 가끔 푸르거나 노랗게)
        {
            int roll = UnityEngine.Random.Range(0, 10); // 주사위
            if (roll < 6) return new Color(1f, 1f, 1f, 0f); // 흰색 (투명하게 시작)
            if (roll < 8) return new Color(0.74f, 0.86f, 1f, 0f); // 푸른 별
            return new Color(1f, 0.90f, 0.70f, 0f); // 노란 별
        }

        private static IEnumerator TwinkleRoutine(Image star) // 별 하나가 서서히 밝아졌다 사라지기를 반복
        {
            Color color = star.color; // 별 색
            yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 3f)); // 별마다 시작 시점을 다르게

            while (star != null) // 화면이 닫힐 때까지
            {
                float peak = UnityEngine.Random.Range(0.45f, 1f); // 이번 반짝임의 최대 밝기
                float rise = UnityEngine.Random.Range(0.8f, 1.8f); // 밝아지는 시간
                float fall = UnityEngine.Random.Range(1.0f, 2.4f); // 어두워지는 시간
                yield return FadeStar(star, color, 0f, peak, rise); // 밝아짐
                yield return FadeStar(star, color, peak, 0f, fall); // 어두워짐
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.3f, 2.2f)); // 쉬는 시간
            }
        }

        private static IEnumerator FadeStar(Image star, Color color, float from, float to, float duration) // 별 밝기 바꾸기
        {
            float elapsed = 0f; // 경과 시간

            while (elapsed < duration && star != null) // 시간 동안
            {
                elapsed += Time.deltaTime; // 시간 누적
                color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)); // 밝기 계산
                star.color = color; // 반영
                yield return null; // 다음 프레임
            }
        }

        private static IEnumerator DriftRoutine(RectTransform sky) // 배경이 아주 천천히 커졌다 작아짐 (정지 화면처럼 보이지 않게)
        {
            float elapsed = 0f; // 경과 시간

            while (sky != null) // 화면이 닫힐 때까지
            {
                elapsed += Time.deltaTime; // 시간 누적
                float scale = 1.02f + (Mathf.Sin(elapsed * 0.18f) * 0.02f); // 1.00 ~ 1.04 사이
                sky.localScale = new Vector3(scale, scale, 1f); // 반영
                yield return null; // 다음 프레임
            }
        }

        private static bool IsSubmitPressed() // 엔터 키를 눌러 입력을 마쳤는지 (신규 Input System)
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드
            return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame); // 엔터 확인
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
