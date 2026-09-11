using ProjectH.SaveSystem; // 시간 진행 알림 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.UI // 프로젝트 UI 영역
{
    [DisallowMultipleComponent] // 전환 연출 중복 방지
    public sealed class TimeTransitionView : MonoBehaviour // 시간 전환 연출 (Day62 추가 — 왼쪽 시간대 그림이 바뀌고, 오른쪽 글자가 "아침"에서 "점심"으로 넘어감)
    {
        private const int SortingOrder = 900; // 대화(600) 위, 로딩창(1000) 아래
        private const float FadeInEnd = 0.30f; // 나타남 끝
        private const float RollStart = 0.70f; // 글자 넘김 시작
        private const float RollEnd = 1.35f; // 글자 넘김 끝
        private const float FadeOutStart = 2.40f; // 사라짐 시작
        private const float End = 2.80f; // 연출 끝
        private static TimeTransitionView active; // 재생 중인 연출

        private GameTimeChange change; // 표시할 변화 (연속 진행은 합쳐짐)
        private float time; // 경과 시간
        private CanvasGroup group; // 전체 투명도
        private Image fromArt; // 이전 시간대 그림
        private Image toArt; // 다음 시간대 그림
        private Outline artFrame; // 그림 테두리
        private Text fromPhase; // 이전 시간대 글자
        private Text toPhase; // 다음 시간대 글자
        private Text fromDay; // 이전 일차 글자
        private Text toDay; // 다음 일차 글자
        private Text subtitle; // 한 줄 문구

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 플레이 시작 시 구독
        private static void Register() // 시간 진행 알림 구독
        {
            GameTimeService.TimeAdvanced -= OnTimeAdvanced; // 중복 구독 방지 (도메인 리로드 비활성 대비)
            GameTimeService.TimeAdvanced += OnTimeAdvanced; // 구독
        }

        private static void OnTimeAdvanced(GameTimeChange next) // 시간이 진행되면 연출 재생 (재생 중이면 이어 붙임)
        {
            if (!Application.isPlaying) return; // 편집 모드 테스트에서는 연출 없음
            if (active != null) active.Retarget(next); // 재생 중 → 합치거나 이어서 넘김
            else active = Create(next); // 새 연출
        }

        private static TimeTransitionView Create(GameTimeChange change) // 연출 화면 생성
        {
            GameObject root = new GameObject("TimeTransition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup)); // 연출 Canvas
            DontDestroyOnLoad(root); // 씬이 바뀌어도 끝까지 재생
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // Overlay
            canvas.sortingOrder = SortingOrder; // 정렬 순서
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기준
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 중간
            TimeTransitionView view = root.AddComponent<TimeTransitionView>(); // 컴포넌트
            view.group = root.GetComponent<CanvasGroup>(); // 투명도 그룹
            view.change = change; // 변화 저장
            view.Build(); // 화면 구성
            view.ApplyContent(); // 글자·그림 적용
            view.Tick(0f); // 첫 프레임 상태
            return view; // 반환
        }

        private void Build() // 화면 구성 : 어두운 막 + 가운데 띠 (왼쪽 그림 · 오른쪽 일차/시간대 글자)
        {
            Button dim = RuntimeUiKit.CreateButton(transform, "Dim", new Color(0f, 0f, 0f, 0.45f), false); // 뒤 화면 어둡게 (클릭하면 빨리 넘김)
            RuntimeUiKit.Stretch((RectTransform)dim.transform); // 전체 화면
            dim.onClick.AddListener(Skip); // 빨리 넘기기
            Image band = RuntimeUiKit.CreateImage(transform, "Band", new Color(0.04f, 0.05f, 0.08f, 0.94f)); // 가운데 띠
            band.raycastTarget = false; // 입력 통과 (막이 받음)
            RuntimeUiKit.SetRect(band.rectTransform, new Vector2(0f, 0.33f), new Vector2(1f, 0.67f)); // 화면 가운데
            Image topLine = RuntimeUiKit.CreateImage(band.transform, "TopLine", new Color(1f, 1f, 1f, 0.35f)); // 윗선
            topLine.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(topLine.rectTransform, new Vector2(0f, 0.99f), new Vector2(1f, 1f)); // 위
            Image bottomLine = RuntimeUiKit.CreateImage(band.transform, "BottomLine", new Color(1f, 1f, 1f, 0.35f)); // 아랫선
            bottomLine.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(bottomLine.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.01f)); // 아래

            Image frame = RuntimeUiKit.CreateImage(transform, "ArtFrame", new Color(0f, 0f, 0f, 1f)); // 그림 틀
            frame.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(frame.rectTransform, new Vector2(0.20f, 0.355f), new Vector2(0.37f, 0.645f)); // 왼쪽 (약 270×260)
            artFrame = frame.gameObject.AddComponent<Outline>(); // 시간대 색 테두리
            artFrame.effectDistance = new Vector2(3f, -3f); // 테두리 두께
            fromArt = CreateArt(frame.transform, "FromArt"); // 이전 그림
            toArt = CreateArt(frame.transform, "ToArt"); // 다음 그림 (위에 겹쳐 서서히 나타남)

            fromDay = CreateRollText("Day", new Vector2(0.40f, 0.555f), new Vector2(0.80f, 0.625f), 26, out toDay); // 일차 줄 (날이 바뀌면 함께 넘어감)
            fromPhase = CreateRollText("Phase", new Vector2(0.40f, 0.415f), new Vector2(0.80f, 0.56f), 78, out toPhase); // 시간대 줄 (크게)
            subtitle = RuntimeUiKit.CreateText(transform, "Subtitle", string.Empty, 20, new Color(0.86f, 0.88f, 0.92f, 1f), FontStyle.Normal, TextAnchor.MiddleLeft).Overflow(); // 한 줄 문구
            subtitle.raycastTarget = false; // 입력 통과
            RuntimeUiKit.SetRect(subtitle.rectTransform, new Vector2(0.40f, 0.36f), new Vector2(0.80f, 0.415f)); // 아래
        }

        private static Image CreateArt(Transform parent, string name) // 그림 한 장
        {
            Image image = RuntimeUiKit.CreateImage(parent, name, Color.white); // 그림
            image.raycastTarget = false; // 입력 통과
            image.preserveAspect = true; // 비율 유지
            RuntimeUiKit.Stretch(image.rectTransform); // 틀 채움
            return image; // 반환
        }

        private Text CreateRollText(string name, Vector2 min, Vector2 max, int size, out Text next) // 넘어가는 글자 한 줄 (가려진 창 안에 이전·다음 글자 두 개)
        {
            GameObject viewport = new GameObject(name + "Viewport", typeof(RectTransform), typeof(RectMask2D)); // 글자가 창 밖으로 나가면 가림
            viewport.transform.SetParent(transform, false); // 부모 연결
            RuntimeUiKit.SetRect((RectTransform)viewport.transform, min, max); // 줄 위치
            Text current = RuntimeUiKit.CreateText(viewport.transform, "From", string.Empty, size, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft).Overflow(); // 이전 글자
            current.raycastTarget = false; // 입력 통과
            next = RuntimeUiKit.CreateText(viewport.transform, "To", string.Empty, size, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft).Overflow(); // 다음 글자
            next.raycastTarget = false; // 입력 통과
            return current; // 이전 글자 반환
        }

        private void ApplyContent() // 변화 내용으로 글자·그림·색 채우기
        {
            fromArt.sprite = TimeOfDayArt.Get(change.FromPhase); // 이전 그림
            toArt.sprite = TimeOfDayArt.Get(change.ToPhase); // 다음 그림
            fromPhase.text = GameTimeService.GetPhaseLabel(change.FromPhase); // 이전 시간대
            toPhase.text = GameTimeService.GetPhaseLabel(change.ToPhase); // 다음 시간대
            fromDay.text = $"DAY {change.FromDay}"; // 이전 일차
            toDay.text = $"DAY {change.ToDay}"; // 다음 일차
            subtitle.text = TimeOfDayArt.GetSubtitle(change.ToPhase) + (change.DayChanged ? "   새로운 하루 · 활력이 모두 회복되었다" : string.Empty); // 문구 (날이 바뀌면 안내 추가)
        }

        private void Retarget(GameTimeChange next) // 재생 중 시간이 또 진행됨
        {
            if (time < RollStart) // 아직 글자가 넘어가기 전 (예: 잠자기의 저녁→밤→아침)
            {
                change = change.Merge(next); // 한 번의 전환으로 합침
            }
            else // 이미 넘어간 뒤
            {
                change = new GameTimeChange(change.ToDay, change.ToPhase, next.ToDay, next.ToPhase); // 현재 표시에서 다음으로
                time = FadeInEnd; // 띠는 보인 채로 다시 넘김
            }

            ApplyContent(); // 내용 갱신
        }

        private void Skip() // 클릭 : 넘기는 중이면 결과로, 결과 표시 중이면 바로 사라짐
        {
            time = time < RollEnd ? RollEnd : Mathf.Max(time, FadeOutStart); // 빨리 진행
        }

        private void Update() // 연출 진행 (일시정지와 무관한 시간)
        {
            time += Time.unscaledDeltaTime; // 경과 누적
            Tick(time); // 상태 적용

            if (time >= End) // 끝
            {
                if (active == this) active = null; // 재생 중 표시 해제
                Destroy(gameObject); // 제거
            }
        }

        private void Tick(float t) // 시간에 따른 화면 상태
        {
            group.alpha = t < FadeInEnd ? t / FadeInEnd : t > FadeOutStart ? 1f - Mathf.Clamp01((t - FadeOutStart) / (End - FadeOutStart)) : 1f; // 나타남·사라짐
            float roll = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(RollStart, RollEnd, t)); // 넘김 진행도 (부드럽게)
            toArt.color = new Color(1f, 1f, 1f, roll); // 다음 그림이 서서히 덮음
            artFrame.effectColor = Color.Lerp(TimeOfDayArt.GetAccent(change.FromPhase), TimeOfDayArt.GetAccent(change.ToPhase), roll); // 테두리 색 변화
            Roll(fromPhase, toPhase, roll, TimeOfDayArt.GetAccent(change.FromPhase), TimeOfDayArt.GetAccent(change.ToPhase)); // 시간대 글자 넘김
            Color dayColor = new Color(1f, 0.82f, 0.40f, 1f); // 일차 금색
            Roll(fromDay, toDay, change.DayChanged ? roll : 1f, dayColor, dayColor); // 날이 바뀔 때만 일차도 넘김
            subtitle.color = new Color(subtitle.color.r, subtitle.color.g, subtitle.color.b, Mathf.InverseLerp(RollEnd - 0.15f, RollEnd + 0.30f, t)); // 넘긴 뒤 문구 나타남
        }

        private static void Roll(Text from, Text to, float progress, Color fromColor, Color toColor) // 이전 글자는 위로 빠지고 다음 글자는 아래에서 올라옴
        {
            SetOffset(from.rectTransform, progress); // 이전 : 0 → 위로 한 줄
            SetOffset(to.rectTransform, progress - 1f); // 다음 : 아래 한 줄 → 0
            from.color = new Color(fromColor.r, fromColor.g, fromColor.b, 1f - progress); // 이전 흐려짐
            to.color = new Color(toColor.r, toColor.g, toColor.b, progress); // 다음 선명해짐
        }

        private static void SetOffset(RectTransform rect, float lines) // 줄 높이 단위로 세로 이동 (앵커로 이동해 크기와 무관)
        {
            rect.anchorMin = new Vector2(0f, lines); // 아래 기준
            rect.anchorMax = new Vector2(1f, 1f + lines); // 위 기준
            rect.offsetMin = Vector2.zero; // 여백 초기화
            rect.offsetMax = Vector2.zero; // 여백 초기화
        }

        private void OnDestroy() // 씬 종료 등으로 사라질 때
        {
            if (active == this) active = null; // 재생 중 표시 해제
        }
    }
}
