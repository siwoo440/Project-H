using System; // 콜백 자료형
using System.Collections.Generic; // 목록 자료형
using ProjectH.Core; // 환경 설정 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 중복 생성 방지
    public sealed class SettingsView : MonoBehaviour // 설정·접근성 화면 (Day72 신규 — ESC 메뉴에서 진입, 줄이 넘치면 스크롤)
    {
        private static readonly Color BoxColor = new Color(0.08f, 0.09f, 0.13f, 0.98f); // 화면 배경
        private static readonly Color RowColor = new Color(0.13f, 0.15f, 0.20f, 1f); // 줄 배경
        private static readonly Color StepColor = new Color(0.26f, 0.30f, 0.38f, 1f); // ◀ ▶ 버튼
        private static readonly Color OnColor = new Color(0.30f, 0.52f, 0.36f, 1f); // 켬
        private static readonly Color OffColor = new Color(0.32f, 0.34f, 0.40f, 1f); // 끔
        private static readonly Color SubColor = new Color(0.24f, 0.26f, 0.32f, 1f); // 보조 버튼
        private static readonly Color TitleColor = new Color(0.92f, 0.78f, 0.42f, 1f); // 제목 금색
        private static readonly Color HintColor = new Color(0.76f, 0.78f, 0.84f, 1f); // 안내 글자
        private const float ViewHeight = 620f; // 보이는 영역 기준 높이 (픽셀 — 이보다 길어지면 스크롤)
        private const float SectionHeight = 42f; // 구분 제목 높이
        private const float RowHeight = 76f; // 설정 줄 높이
        private const float RowGap = 10f; // 줄 사이 간격

        private Action<string> onClosed; // 닫을 때 알림
        private ScrollRect scroll; // 세로 스크롤
        private RectTransform content; // 줄이 쌓이는 영역
        private float cursor; // 다음 줄이 놓일 위치 (위에서부터, 픽셀)
        private readonly List<GameObject> rows = new List<GameObject>(); // 줄 목록

        public static SettingsView Open(Action<string> closed) // 설정 화면 열기
        {
            GameObject root = new GameObject("SettingsView", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 런타임 Canvas
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay 렌더링
            canvas.sortingOrder = 760; // ESC 메뉴(750)보다 위
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            SettingsView view = root.AddComponent<SettingsView>(); // 컴포넌트 추가
            view.onClosed = closed; // 콜백 저장
            view.Build(); // 화면 구성
            view.Refresh(); // 줄 채우기
            return view; // 반환
        }

        private void Build() // 화면 뼈대 (상자 + 스크롤 영역 + 안내)
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.84f)); // 어두운 막
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체
            Image box = RuntimeUiKit.CreateImage(transform, "Box", BoxColor); // 상자
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.20f, 0.06f), new Vector2(0.80f, 0.95f)); // 가운데
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.86f, 0.72f, 0.36f, 0.85f); // 금색 테두리
            Text header = RuntimeUiKit.CreateText(box.transform, "Header", "설정", 30, TitleColor, FontStyle.Bold); // 제목
            RuntimeUiKit.SetRect(header.rectTransform, new Vector2(0.04f, 0.92f), new Vector2(0.60f, 0.99f)); // 위
            header.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬
            Button reset = RuntimeUiKit.CreateButton(box.transform, "Reset", SubColor); // 기본값
            RuntimeUiKit.SetRect(reset.GetComponent<RectTransform>(), new Vector2(0.62f, 0.92f), new Vector2(0.80f, 0.99f)); // 위
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(reset.transform, "Label", "기본값", 18, Color.white, FontStyle.Bold).rectTransform); // 글자
            reset.onClick.AddListener(() => { GameSettings.ResetToDefault(); Refresh(); }); // 되돌리기
            Button close = RuntimeUiKit.CreateButton(box.transform, "Close", SubColor); // 닫기
            RuntimeUiKit.SetRect(close.GetComponent<RectTransform>(), new Vector2(0.82f, 0.92f), new Vector2(0.96f, 0.99f)); // 오른쪽 위
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(close.transform, "Label", "닫기", 18, Color.white, FontStyle.Bold).rectTransform); // 글자
            close.onClick.AddListener(Close); // 닫기 연결
            BuildScrollArea(box.transform); // 스크롤 영역 (줄이 넘치면 내려서 봄)
            Text hint = RuntimeUiKit.CreateText(box.transform, "Hint", "설정은 저장 파일과 따로 보관되어, 새로 시작해도 그대로 유지됩니다.  ·  휠을 굴려 더 볼 수 있어요.", 15, HintColor, FontStyle.Normal).Wrap(); // 안내
            RuntimeUiKit.SetRect(hint.rectTransform, new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.06f)); // 아래
        }

        private void BuildScrollArea(Transform parent) // 보이는 영역 + 내용 루트 + 세로 스크롤 (Day67 마을 패널과 같은 방식)
        {
            Image panel = RuntimeUiKit.CreateImage(parent, "Panel", new Color(0f, 0f, 0f, 0f)); // 스크롤 바탕 (투명)
            RuntimeUiKit.SetRect(panel.rectTransform, new Vector2(0.04f, 0.07f), new Vector2(0.96f, 0.90f)); // 제목 아래 · 안내 위
            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); // 보이는 영역 (밖으로 나간 줄은 잘림)
            viewportObject.transform.SetParent(panel.transform, false); // 바탕 하위
            RectTransform viewport = (RectTransform)viewportObject.transform; // 영역 저장
            RuntimeUiKit.Stretch(viewport); // 바탕 전체
            GameObject contentObject = new GameObject("Content", typeof(RectTransform)); // 내용 루트
            contentObject.transform.SetParent(viewport, false); // 보이는 영역 하위
            content = (RectTransform)contentObject.transform; // 내용 저장
            content.anchorMin = new Vector2(0f, 1f); // 위쪽 기준
            content.anchorMax = new Vector2(1f, 1f); // 위쪽 기준
            content.pivot = new Vector2(0.5f, 1f); // 위쪽 고정
            content.offsetMin = new Vector2(0f, content.offsetMin.y); // 좌우 여백 없음
            content.offsetMax = new Vector2(0f, content.offsetMax.y); // 좌우 여백 없음
            content.sizeDelta = new Vector2(0f, ViewHeight); // 기본 높이
            scroll = panel.gameObject.AddComponent<ScrollRect>(); // 세로 스크롤
            scroll.content = content; // 내용 연결
            scroll.viewport = viewport; // 보이는 영역 연결
            scroll.horizontal = false; // 가로 스크롤 없음
            scroll.vertical = true; // 세로 스크롤 사용
            scroll.movementType = ScrollRect.MovementType.Clamped; // 끝에서 멈춤
            scroll.scrollSensitivity = 40f; // 휠 감도
            scroll.inertia = false; // 관성 없이 바로 멈춤
        }

        private void Refresh() // 설정 줄 다시 그리기 (보던 위치는 유지)
        {
            float previous = scroll == null ? 1f : scroll.verticalNormalizedPosition; // 이전 스크롤 위치
            if (float.IsNaN(previous) || float.IsInfinity(previous)) previous = 1f; // 내용이 짧아 값이 없을 때는 맨 위
            foreach (GameObject row in rows) Destroy(row); // 이전 줄 제거
            rows.Clear(); // 목록 비움
            cursor = 0f; // 위에서부터 다시
            AddSection("소리"); // 소리
            AddStepRow("전체 음량", FormatPercent(GameSettings.MasterVolume), () => GameSettings.AddMasterVolume(-GameSettings.VolumeStep), () => GameSettings.AddMasterVolume(GameSettings.VolumeStep)); // 전체
            AddStepRow("배경음", FormatPercent(GameSettings.BgmVolume), () => GameSettings.AddBgmVolume(-GameSettings.VolumeStep), () => GameSettings.AddBgmVolume(GameSettings.VolumeStep)); // 배경음
            AddStepRow("효과음", FormatPercent(GameSettings.SfxVolume), () => GameSettings.AddSfxVolume(-GameSettings.VolumeStep), () => GameSettings.AddSfxVolume(GameSettings.VolumeStep)); // 효과음
            AddSection("대화"); // 대화
            AddStepRow("대사 속도", $"×{GameSettings.DialogueSpeed:0.00}", () => GameSettings.AddDialogueSpeed(-GameSettings.DialogueSpeedStep), () => GameSettings.AddDialogueSpeed(GameSettings.DialogueSpeedStep)); // 속도
            AddStepRow("자동 진행 대기", $"{GameSettings.AutoAdvanceSeconds:0.0}초", () => GameSettings.AddAutoAdvance(-GameSettings.AutoAdvanceStep), () => GameSettings.AddAutoAdvance(GameSettings.AutoAdvanceStep)); // 자동 진행
            AddSection("접근성"); // 접근성
            AddStepRow("글자 크기", FormatPercent(GameSettings.TextScale), () => GameSettings.AddTextScale(-GameSettings.TextScaleStep), () => GameSettings.AddTextScale(GameSettings.TextScaleStep)); // 글자 크기
            AddToggleRow("화면 흔들림 줄이기", GameSettings.ReduceShake, () => GameSettings.SetReduceShake(!GameSettings.ReduceShake)); // 흔들림
            AddToggleRow("리듬 판정 완화", GameSettings.EasyTiming, () => GameSettings.SetEasyTiming(!GameSettings.EasyTiming)); // 판정 완화
            content.sizeDelta = new Vector2(0f, Mathf.Max(ViewHeight, cursor + 12f)); // 내용 높이 (넘치면 스크롤)
            if (scroll != null) scroll.verticalNormalizedPosition = Mathf.Clamp01(previous); // 보던 위치 유지
        }

        private void AddSection(string label) // 구분 제목 한 줄
        {
            Text text = RuntimeUiKit.CreateText(content, "Section", label, 19, TitleColor, FontStyle.Bold, TextAnchor.LowerLeft); // 글자
            PlaceRow(text.rectTransform, SectionHeight); // 배치
            rows.Add(text.gameObject); // 목록 등록
        }

        private void AddStepRow(string label, string value, Action decrease, Action increase) // ◀ 값 ▶ 한 줄
        {
            Image row = RuntimeUiKit.CreateImage(content, "Row", RowColor); // 줄 배경
            PlaceRow(row.rectTransform, RowHeight); // 배치
            Text name = RuntimeUiKit.CreateText(row.transform, "Name", label, 20, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft); // 이름
            RuntimeUiKit.SetRect(name.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.55f, 1f)); // 왼쪽
            Button minus = RuntimeUiKit.CreateButton(row.transform, "Minus", StepColor); // 줄이기
            RuntimeUiKit.SetRect(minus.GetComponent<RectTransform>(), new Vector2(0.57f, 0.16f), new Vector2(0.67f, 0.84f)); // 배치
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(minus.transform, "Label", "◀", 20, Color.white, FontStyle.Bold).rectTransform); // 글자
            minus.onClick.AddListener(() => { decrease(); Refresh(); }); // 연결
            Text valueText = RuntimeUiKit.CreateText(row.transform, "Value", value, 21, Color.white, FontStyle.Bold); // 값
            RuntimeUiKit.SetRect(valueText.rectTransform, new Vector2(0.68f, 0f), new Vector2(0.84f, 1f)); // 가운데
            Button plus = RuntimeUiKit.CreateButton(row.transform, "Plus", StepColor); // 늘리기
            RuntimeUiKit.SetRect(plus.GetComponent<RectTransform>(), new Vector2(0.85f, 0.16f), new Vector2(0.95f, 0.84f)); // 배치
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(plus.transform, "Label", "▶", 20, Color.white, FontStyle.Bold).rectTransform); // 글자
            plus.onClick.AddListener(() => { increase(); Refresh(); }); // 연결
            rows.Add(row.gameObject); // 목록 등록
        }

        private void AddToggleRow(string label, bool on, Action toggle) // 켬 · 끔 한 줄
        {
            Image row = RuntimeUiKit.CreateImage(content, "Row", RowColor); // 줄 배경
            PlaceRow(row.rectTransform, RowHeight); // 배치
            Text name = RuntimeUiKit.CreateText(row.transform, "Name", label, 20, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft); // 이름
            RuntimeUiKit.SetRect(name.rectTransform, new Vector2(0.03f, 0f), new Vector2(0.66f, 1f)); // 왼쪽
            Button button = RuntimeUiKit.CreateButton(row.transform, "Toggle", on ? OnColor : OffColor); // 켬·끔 버튼
            RuntimeUiKit.SetRect(button.GetComponent<RectTransform>(), new Vector2(0.68f, 0.16f), new Vector2(0.95f, 0.84f)); // 배치
            RuntimeUiKit.Stretch(RuntimeUiKit.CreateText(button.transform, "Label", on ? "켬" : "끔", 20, Color.white, FontStyle.Bold).rectTransform); // 글자
            button.onClick.AddListener(() => { toggle(); Refresh(); }); // 연결
            rows.Add(row.gameObject); // 목록 등록
        }

        private void PlaceRow(RectTransform rect, float height) // 줄을 위에서부터 픽셀로 쌓아 배치 (넘치는 만큼 스크롤된다)
        {
            rect.anchorMin = new Vector2(0f, 1f); // 위쪽 기준
            rect.anchorMax = new Vector2(1f, 1f); // 위쪽 기준
            rect.pivot = new Vector2(0.5f, 1f); // 위쪽 고정
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y); // 좌우 여백 없음
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y); // 좌우 여백 없음
            rect.sizeDelta = new Vector2(0f, height); // 줄 높이
            rect.anchoredPosition = new Vector2(0f, -cursor); // 위에서부터 내려오며 배치
            cursor += height + RowGap; // 다음 줄 위치
        }

        private static string FormatPercent(float value) => $"{Mathf.RoundToInt(value * 100f)}%"; // 백분율 문구

        private void Close() // 닫기
        {
            Action<string> callback = onClosed; // 콜백 복사
            onClosed = null; // 중복 방지
            Destroy(gameObject); // 화면 닫기
            callback?.Invoke("설정을 저장했습니다."); // 안내 전달
        }
    }
}
