using System; // 콜백 델리게이트 기능
using ProjectH.UI; // 공용 UI 생성·컷인 아트 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 컷인 방지
    public sealed class UltimateCutInView : MonoBehaviour // 궁극기 컷인 연출 (Day59 신규 — 일러스트 + 궁극기 이름, 리듬 챌린지 직전)
    {
        private const int SortingOrder = 620; // 리듬 챌린지(600) 위
        private const float BandWidth = 2300f; // 사선 띠 가로 길이
        private const float BandHeight = 250f; // 사선 띠 세로 길이
        private const float PortraitRestX = -430f; // 일러스트 정지 위치 (띠 기준)
        private static readonly Color Gold = new Color(1f, 0.84f, 0.36f, 1f); // 결속 금색

        public static bool Enabled = true; // 컷인 사용 여부 (Day71 설정 일차에 켜기·끄기 연결)

        private readonly UltimateCutInTimeline timeline = new UltimateCutInTimeline(); // 시간 진행 계산
        private CanvasGroup group; // 전체 투명도
        private RectTransform band; // 사선 띠
        private RectTransform portrait; // 일러스트
        private RectTransform nameRect; // 궁극기 이름
        private Action onFinished; // 종료 콜백 (리듬 챌린지 시작)
        private bool finished; // 중복 종료 방지

        public static UltimateCutInView Show(UltimateCutInInfo info, Action finishedCallback) // 컷인 표시 (꺼져 있으면 바로 콜백)
        {
            if (!Enabled) // 설정 확인
            {
                finishedCallback?.Invoke(); // 컷인 없이 진행
                return null; // 표시 안 함
            }

            GameObject root = new GameObject("UltimateCutIn", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup)); // 컷인 Canvas 생성
            Canvas canvas = root.GetComponent<Canvas>(); // Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay
            canvas.sortingOrder = SortingOrder; // 정렬 순서
            CanvasScaler scaler = root.GetComponent<CanvasScaler>(); // 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기준
            scaler.referenceResolution = new Vector2(1600f, 900f); // 기준 해상도
            scaler.matchWidthOrHeight = 0.5f; // 가로·세로 중간
            UltimateCutInView view = root.AddComponent<UltimateCutInView>(); // 컷인 컴포넌트 추가
            view.onFinished = finishedCallback; // 종료 콜백 저장
            view.group = root.GetComponent<CanvasGroup>(); // 투명도 그룹 저장
            view.Build(info); // 화면 구성
            view.Apply(); // 첫 프레임 상태 적용
            return view; // 컷인 반환
        }

        private void Build(UltimateCutInInfo info) // 화면 구성
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.55f)); // 전투 화면 어둡게
            RuntimeUiKit.Stretch(dim.rectTransform); // 전체 화면
            Button skip = RuntimeUiKit.CreateButton(transform, "SkipCatcher", new Color(0f, 0f, 0f, 0f), false); // 클릭 스킵 영역 (투명)
            RuntimeUiKit.Stretch((RectTransform)skip.transform); // 전체 화면
            skip.onClick.AddListener(timeline.Skip); // 클릭 → 스킵

            Image bandImage = RuntimeUiKit.CreateImage(transform, "Band", Color.Lerp(info.Tint, Color.black, 0.55f)); // 캐릭터 색 사선 띠
            bandImage.raycastTarget = false; // 입력 통과
            band = bandImage.rectTransform; // 띠 저장
            band.anchorMin = band.anchorMax = new Vector2(0.5f, 0.55f); // 화면 중앙보다 약간 위
            band.sizeDelta = new Vector2(BandWidth, BandHeight); // 띠 크기
            band.localRotation = Quaternion.Euler(0f, 0f, 7f); // 사선 기울기
            bandImage.gameObject.AddComponent<Mask>().showMaskGraphic = true; // 띠 밖 일러스트 잘라내기 (가슴 위만 표시)
            Color edge = info.IsBondBurst ? Gold : new Color(1f, 1f, 1f, 0.85f); // 테두리 색 (결속 5단계 금색)
            CreateEdge("TopEdge", new Vector2(0f, 1f), edge); // 윗선
            CreateEdge("BottomEdge", new Vector2(0f, 0f), edge); // 아랫선

            bool placeholder; // 임시 이미지 여부
            Image art = RuntimeUiKit.CreateImage(band, "Portrait", Color.white); // 일러스트
            art.sprite = DialogueArtFactory.GetCutIn(info.CharacterId, out placeholder); // 컷인 → 대화 스탠딩 → 임시 실루엣 순서로 조회
            art.color = placeholder ? info.Tint : Color.white; // 임시 실루엣은 캐릭터 색
            art.preserveAspect = true; // 비율 유지
            art.raycastTarget = false; // 입력 통과
            portrait = art.rectTransform; // 일러스트 저장
            portrait.sizeDelta = new Vector2(380f, 760f); // 세로로 긴 일러스트 (띠 안에는 가슴 위만 보임)
            portrait.anchoredPosition = new Vector2(PortraitRestX, -160f); // 머리가 띠 가운데 오도록 아래로 배치

            Text title = RuntimeUiKit.CreateText(band, "UltimateName", info.UltimateName, 66, Color.white, FontStyle.Bold, TextAnchor.MiddleLeft).Overflow().Outlined(new Color(0f, 0f, 0f, 0.7f), new Vector2(3f, -3f)); // 궁극기 이름
            nameRect = title.rectTransform; // 이름 저장
            nameRect.sizeDelta = new Vector2(900f, 110f); // 이름 영역
            nameRect.anchoredPosition = new Vector2(220f, 22f); // 띠 오른쪽 배치
            Text sub = RuntimeUiKit.CreateText(band, "SubTitle", info.SubTitle, 24, info.IsBondBurst ? Gold : new Color(1f, 1f, 1f, 0.85f), FontStyle.Bold, TextAnchor.MiddleLeft).Overflow().Outlined(new Color(0f, 0f, 0f, 0.6f), new Vector2(2f, -2f)); // 캐릭터 이름 또는 결속 스킬
            sub.rectTransform.sizeDelta = new Vector2(900f, 40f); // 부제 영역
            sub.rectTransform.anchoredPosition = new Vector2(220f, -52f); // 이름 아래 배치

            if (!string.IsNullOrEmpty(info.Line)) // 기합 대사 확인
            {
                Text line = RuntimeUiKit.CreateText(transform, "Line", $"“{info.Line}”", 28, Color.white, FontStyle.Italic).Overflow().Outlined(new Color(0f, 0f, 0f, 0.7f), new Vector2(2f, -2f)); // 기합 대사
                RuntimeUiKit.SetRect(line.rectTransform, new Vector2(0.1f, 0.20f), new Vector2(0.9f, 0.30f)); // 띠 아래 배치
            }
        }

        private void CreateEdge(string name, Vector2 anchorY, Color color) // 띠 위·아래 테두리 선
        {
            Image edge = RuntimeUiKit.CreateImage(band, name, color); // 선 생성
            edge.raycastTarget = false; // 입력 통과
            RectTransform rect = edge.rectTransform; // 선 영역
            rect.anchorMin = new Vector2(0f, anchorY.y); // 가로 전체
            rect.anchorMax = new Vector2(1f, anchorY.y); // 가로 전체
            rect.pivot = new Vector2(0.5f, anchorY.y); // 안쪽으로 두께
            rect.sizeDelta = new Vector2(0f, 6f); // 두께 6
            rect.anchoredPosition = Vector2.zero; // 가장자리 배치
        }

        private void Update() // 시간 진행 (전투 일시정지 중이므로 unscaled)
        {
            if (finished) // 종료 확인
            {
                return; // 처리 없음
            }

            timeline.Advance(Time.unscaledDeltaTime); // 시간 진행
            Apply(); // 화면 반영

            if (timeline.IsDone) // 종료 확인
            {
                Finish(); // 종료 처리
            }
        }

        private void Apply() // 타임라인 → 화면 반영
        {
            float enter = 1f - Mathf.Pow(1f - timeline.EnterProgress, 3f); // 등장 감속 곡선
            float exit = timeline.ExitProgress; // 퇴장 진행률
            group.alpha = timeline.Visibility; // 전체 투명도
            band.localScale = new Vector3(1f, Mathf.Lerp(0.05f, 1f, enter) * Mathf.Lerp(1f, 0.1f, exit), 1f); // 띠가 펼쳐졌다가 접힘
            portrait.anchoredPosition = new Vector2(Mathf.Lerp(-1150f, PortraitRestX, enter) + (40f * timeline.HoldProgress) + (320f * exit), portrait.anchoredPosition.y); // 왼쪽에서 들어와 천천히 밀리다 오른쪽으로 빠짐
            float punch = Mathf.Clamp01((timeline.Elapsed - 0.18f) / 0.22f); // 이름 등장 진행률
            nameRect.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, punch); // 이름이 크게 나타났다가 제자리
        }

        private void Finish() // 종료 → 리듬 챌린지 시작
        {
            if (finished) // 중복 확인
            {
                return; // 무시
            }

            finished = true; // 종료 기록
            onFinished?.Invoke(); // 다음 단계 진행
            Destroy(gameObject); // 컷인 제거
        }
    }
}
