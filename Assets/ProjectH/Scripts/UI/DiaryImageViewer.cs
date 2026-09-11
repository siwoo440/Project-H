using System; // 콜백 델리게이트 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 마우스 휠·드래그·키보드 입력 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 크게 보기 창 중복 방지
    public sealed class DiaryImageViewer : MonoBehaviour // 일기장 크게 보기 (Day63 추가 — 정지 컷신 : 전체 화면 · 휠 확대/축소 · 드래그 이동 · 닫기 / CG·컷신 : 가운데 "재생하겠습니까?" 네/아니요만)
    {
        public const float MinZoom = 1f; // 최소 배율 (화면에 딱 맞음)
        public const float MaxZoom = 4f; // 최대 배율
        private const float ZoomStep = 1.15f; // 휠 한 칸 배율
        private static readonly Color PanelColor = new Color(0.97f, 0.94f, 0.86f, 0.97f); // 질문 상자 종이색
        private static readonly Color InkColor = new Color(0.20f, 0.15f, 0.12f, 1f); // 잉크
        private static readonly Color ButtonColor = new Color(0.52f, 0.30f, 0.18f, 1f); // 버튼 갈색

        private RectTransform frame; // 그림 영역 (확대 기준)
        private RectTransform zoomRoot; // 확대·이동되는 그림 묶음
        private Image image; // 그림
        private Image standing; // 임시 CG 실루엣
        private Text zoomText; // 배율 표시
        private GameObject closeButton; // 닫기 버튼 (정지 컷신만)
        private bool imageMode; // 그림 보기 모드 (false = 재생 질문 모드)
        private GameObject prompt; // "재생하겠습니까?" 상자
        private Text promptText; // 질문 글자
        private Canvas canvas; // 소속 캔버스 (드래그 픽셀 → 캔버스 단위)
        private Action onPlay; // [네] 동작
        private float zoom = MinZoom; // 현재 배율

        public bool IsOpen => gameObject.activeSelf; // 열림 여부
        public float Zoom => zoom; // 현재 배율
        public bool IsImageMode => imageMode; // 그림 보기 모드 여부 (테스트용)

        public static DiaryImageViewer Create(Transform parent) // 창 생성 (처음엔 숨김)
        {
            Image dim = RuntimeUiKit.CreateImage(parent, "ImageViewer", new Color(0f, 0f, 0f, 0.96f)); // 검은 전체 막 (뒤 클릭 차단)
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체 화면
            DiaryImageViewer viewer = dim.gameObject.AddComponent<DiaryImageViewer>(); // 컴포넌트
            viewer.canvas = parent.GetComponentInParent<Canvas>(); // 캔버스
            viewer.Build(); // 구성
            dim.gameObject.SetActive(false); // 숨김
            return viewer; // 반환
        }

        private void Build() // 그림 영역 · 닫기 · 배율 안내 · 질문 상자
        {
            GameObject frameObject = new GameObject("Frame", typeof(RectTransform), typeof(RectMask2D)); // 확대해도 화면 밖은 가림
            frameObject.transform.SetParent(transform, false); // 부모 연결
            frame = (RectTransform)frameObject.transform; // 저장
            RuntimeUiKit.SetRect(frame, new Vector2(0.02f, 0.03f), new Vector2(0.98f, 0.97f)); // 거의 전체
            GameObject zoomObject = new GameObject("ZoomRoot", typeof(RectTransform)); // 확대 묶음
            zoomObject.transform.SetParent(frame, false); // 영역 안
            zoomRoot = (RectTransform)zoomObject.transform; // 저장
            RuntimeUiKit.Stretch(zoomRoot); // 영역 채움
            image = RuntimeUiKit.CreateImage(zoomRoot, "Image", Color.white); // 그림
            image.preserveAspect = true; // 비율 유지
            image.raycastTarget = false; // 입력 통과
            RuntimeUiKit.Stretch(image.rectTransform); // 채움
            standing = RuntimeUiKit.CreateImage(zoomRoot, "Standing", Color.white); // 실루엣
            standing.preserveAspect = true; // 비율 유지
            standing.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(standing.rectTransform, new Vector2(0.32f, 0f), new Vector2(0.68f, 1f)); // 가운데
            Button close = CreateButton(transform, "Close", "✕  닫기", new Color(0.25f, 0.20f, 0.18f, 0.92f), Color.white); // 닫기
            RuntimeUiKit.SetRect((RectTransform)close.transform, new Vector2(0.88f, 0.92f), new Vector2(0.985f, 0.98f)); // 오른쪽 위
            close.onClick.AddListener(Close); // 닫기
            closeButton = close.gameObject; // 저장 (정지 컷신에서만 표시)
            zoomText = RuntimeUiKit.CreateText(transform, "ZoomText", string.Empty, 16, new Color(1f, 1f, 1f, 0.75f), FontStyle.Normal, TextAnchor.MiddleLeft).Outlined(new Color(0f, 0f, 0f, 0.8f), new Vector2(1f, -1f)); // 배율 안내
            zoomText.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(zoomText.rectTransform, new Vector2(0.015f, 0.92f), new Vector2(0.60f, 0.98f)); // 왼쪽 위
            BuildPrompt(); // 질문 상자
        }

        private void BuildPrompt() // "CG를 재생하겠습니까?" [네] [아니요]
        {
            Image box = RuntimeUiKit.CreateImage(transform, "PlayPrompt", PanelColor); // 상자
            RuntimeUiKit.SetRect(box.rectTransform, new Vector2(0.33f, 0.40f), new Vector2(0.67f, 0.60f)); // 화면 가운데
            box.gameObject.AddComponent<Outline>().effectColor = new Color(0.52f, 0.30f, 0.18f, 0.9f); // 테두리
            promptText = RuntimeUiKit.CreateText(box.transform, "Question", string.Empty, 22, InkColor, FontStyle.Bold); // 질문
            RuntimeUiKit.SetRect(promptText.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.96f)); // 위
            Button yes = CreateButton(box.transform, "Yes", "네", ButtonColor, Color.white); // 네
            RuntimeUiKit.SetRect((RectTransform)yes.transform, new Vector2(0.12f, 0.10f), new Vector2(0.46f, 0.46f)); // 왼쪽
            yes.onClick.AddListener(Play); // 재생
            Button no = CreateButton(box.transform, "No", "아니요", new Color(0.46f, 0.42f, 0.38f, 1f), Color.white); // 아니요
            RuntimeUiKit.SetRect((RectTransform)no.transform, new Vector2(0.54f, 0.10f), new Vector2(0.88f, 0.46f)); // 오른쪽
            no.onClick.AddListener(Close); // 아니요 → 바로 닫기 (닫기 버튼 없이)
            prompt = box.gameObject; // 저장
        }

        public void OpenImage(Sprite sprite, string standingCharacterId, Color tint) // 정지 컷신 : 전체 화면 그림 + 안내 글자 + 닫기 (휠 확대/축소 · 드래그 이동)
        {
            image.sprite = sprite; // 그림
            image.color = tint; // 색
            standing.gameObject.SetActive(!string.IsNullOrEmpty(standingCharacterId)); // 임시 실루엣 여부

            if (standing.gameObject.activeSelf) // 실루엣 적용
            {
                standing.sprite = DialogueArtFactory.GetStanding(standingCharacterId, null, out bool placeholder); // 스탠딩
                standing.color = placeholder ? DialogueArtFactory.GetCharacterTint(standingCharacterId) : Color.white; // 색
            }

            SetMode(true); // 그림 보기 모드
            onPlay = null; // 재생 없음
            SetZoom(MinZoom, Vector2.zero); // 배율 초기화
            zoomRoot.anchoredPosition = Vector2.zero; // 가운데
            Show(); // 표시
        }

        public void AskPlay(string question, Action play) // CG·컷신 : 그림 없이 가운데 질문만 ([네] 재생 · [아니요] 닫기)
        {
            SetMode(false); // 재생 질문 모드
            onPlay = play; // 재생 동작
            promptText.text = question ?? string.Empty; // 질문
            Show(); // 표시
        }

        private void SetMode(bool image) // 모드별 표시 요소 (그림·안내·닫기 ↔ 질문)
        {
            imageMode = image; // 모드 저장
            frame.gameObject.SetActive(image); // 그림
            zoomText.gameObject.SetActive(image); // 왼쪽 위 안내 글자
            closeButton.SetActive(image); // 닫기 버튼
            prompt.SetActive(!image); // 가운데 질문
            GetComponent<Image>().color = image ? new Color(0f, 0f, 0f, 0.96f) : new Color(0f, 0f, 0f, 0.55f); // 질문 모드는 뒤 화면이 살짝 보이게
        }

        private void Show() // 창 표시
        {
            gameObject.SetActive(true); // 표시
            transform.SetAsLastSibling(); // 맨 위
        }

        public void Close() => gameObject.SetActive(false); // 닫기

        private void Play() // [네] : 창을 닫고 재생
        {
            Action play = onPlay; // 동작 보관
            Close(); // 창 닫기
            play?.Invoke(); // 재생
        }

        private void Update() // 휠 확대/축소 · 드래그 이동 · ESC/오른쪽 클릭 닫기
        {
            Mouse mouse = Mouse.current; // 마우스
            Keyboard keyboard = Keyboard.current; // 키보드

            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) || (mouse != null && mouse.rightButton.wasPressedThisFrame)) // 닫기 입력
            {
                Close(); // 닫기
                return; // 종료
            }

            if (mouse == null || !imageMode) return; // 마우스 없음 · 질문 모드에서는 확대 없음
            float scroll = mouse.scroll.ReadValue().y; // 휠 값

            if (Mathf.Abs(scroll) > 0.01f) // 휠을 굴림
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(frame, mouse.position.ReadValue(), null, out Vector2 cursor); // 커서 위치 (영역 기준)
                SetZoom(zoom * (scroll > 0f ? ZoomStep : 1f / ZoomStep), cursor); // 커서 쪽으로 확대·축소
            }

            if (mouse.leftButton.isPressed && zoom > MinZoom + 0.001f) // 확대 상태에서 끌기
            {
                float scale = canvas == null ? 1f : canvas.scaleFactor; // 화면 픽셀 → 캔버스 단위
                zoomRoot.anchoredPosition = ClampPan(zoomRoot.anchoredPosition + (mouse.delta.ReadValue() / Mathf.Max(0.01f, scale))); // 그림 이동
            }
        }

        private void SetZoom(float next, Vector2 cursor) // 배율 적용 (커서 아래 지점이 그대로 머물게)
        {
            float clamped = Mathf.Clamp(next, MinZoom, MaxZoom); // 범위 보정
            Vector2 pan = zoomRoot.anchoredPosition; // 현재 이동
            pan = cursor - ((cursor - pan) * (clamped / zoom)); // 커서 기준 확대
            zoom = clamped; // 배율 저장
            zoomRoot.localScale = new Vector3(zoom, zoom, 1f); // 확대
            zoomRoot.anchoredPosition = ClampPan(pan); // 이동 보정
            zoomText.text = $"×{zoom:0.0}   휠 : 확대 · 축소   드래그 : 이동   ESC · 오른쪽 클릭 : 닫기"; // 안내
        }

        private Vector2 ClampPan(Vector2 pan) // 그림이 화면 밖으로 빠지지 않게 이동 범위 제한
        {
            Vector2 size = frame.rect.size; // 영역 크기
            float maxX = (zoom - 1f) * size.x * 0.5f; // 가로 한계
            float maxY = (zoom - 1f) * size.y * 0.5f; // 세로 한계
            return new Vector2(Mathf.Clamp(pan.x, -maxX, maxX), Mathf.Clamp(pan.y, -maxY, maxY)); // 보정
        }

        private static Button CreateButton(Transform parent, string name, string label, Color color, Color labelColor) // 버튼 생성
        {
            Button button = RuntimeUiKit.CreateButton(parent, name, color); // 버튼
            Text text = RuntimeUiKit.CreateText(button.transform, "Label", label, 20, labelColor).BestFit(11); // 글자
            RuntimeUiKit.Stretch(text.rectTransform, 6f); // 채움
            return button; // 반환
        }
    }
}
