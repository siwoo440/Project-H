using System; // Action 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.UI; // Runtime UI 생성 공용 헬퍼 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    [DisallowMultipleComponent] // 중복 리듬 챌린지 뷰 방지
    public sealed class UltimateRhythmChallengeView : MonoBehaviour // 궁극기 리듬 챌린지 화면 (원 생성·축소·클릭 판정)
    {
        private static readonly Color RingColor = new Color(0.97f, 0.82f, 0.22f, 0.95f); // 줄어드는 원 색상
        private static readonly Color TargetColor = new Color(0.20f, 0.55f, 0.90f, 0.55f); // 목표 크기 원 색상
        private static readonly Color HitColor = new Color(0.30f, 0.85f, 0.42f, 0.95f); // Hit 판정 색상
        private static readonly Color MissColor = new Color(0.85f, 0.25f, 0.25f, 0.95f); // Miss 판정 색상
        private const float CircleSizePixels = UltimateRhythmChallengeGenerator.CircleSizePixels; // 원 기준 크기(px) — Generator와 동일 값 공유 (겹침 계산과 불일치 방지)
        private const float RingStartScale = 2.6f; // 원 시작 배율
        private const float ResultHoldSeconds = 0.28f; // 판정 결과 표시 유지 시간
        private const float FinalHoldSeconds = 0.9f; // 전체 종료 후 요약 문구 유지 시간

        private readonly List<CircleRuntime> circles = new List<CircleRuntime>(); // 생성 원 런타임 목록
        private Action<int, int> onComplete; // 챌린지 종료 콜백 (Hit 수, 전체 수)
        private Text summaryText; // 종합 결과 요약 텍스트
        private int hitCount; // 누적 Hit 수
        private bool finished; // 전체 챌린지 종료 여부

        private sealed class CircleRuntime // 단일 원 런타임 상태
        {
            public RectTransform Ring; // 줄어드는 원 RectTransform
            public Image RingImage; // 줄어드는 원 이미지
            public Button ClickButton; // 원 클릭 버튼
            public float Duration; // 줄어드는 소요 시간
            public float Elapsed; // 경과 시간
            public bool Resolved; // 판정 완료 여부
        }

        public static UltimateRhythmChallengeView Show(string ultimateName, Action<int, int> onComplete) // 궁극기 리듬 챌린지 표시 시작
        {
            GameObject canvasObject = new GameObject("UltimateRhythmChallengeRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UltimateRhythmChallengeView)); // 챌린지 Canvas 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 챌린지 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 600; // 일반 전투 HUD 위에 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 챌린지 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            UltimateRhythmChallengeView view = canvasObject.GetComponent<UltimateRhythmChallengeView>(); // 챌린지 뷰 컴포넌트 조회
            view.onComplete = onComplete; // 종료 콜백 연결
            view.BuildChallenge(ultimateName); // 챌린지 원 생성 및 배치
            return view; // 생성 챌린지 뷰 반환
        }

        private void BuildChallenge(string ultimateName) // 챌린지 원 생성 및 배치
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", new Color(0f, 0f, 0f, 0.12f)); // 배경 살짝 어둡게 표시
            RuntimeUiKit.Stretch(dim.rectTransform); // 배경 전체 화면 확장
            dim.raycastTarget = false; // 배경 클릭 차단 비활성화

            Text titleText = CreateText("TitleText", string.IsNullOrWhiteSpace(ultimateName) ? "궁극기 타이밍!" : $"{ultimateName} · 타이밍!", 30, new Color(1f, 1f, 1f, 0.92f)); // 챌린지 안내 제목 생성
            RuntimeUiKit.SetRect(titleText.rectTransform, new Vector2(0.25f, 0.82f), new Vector2(0.75f, 0.90f)); // 안내 제목 상단 배치

            summaryText = CreateText("SummaryText", string.Empty, 26, Color.white); // 결과 요약 텍스트 생성
            RuntimeUiKit.SetRect(summaryText.rectTransform, new Vector2(0.25f, 0.82f), new Vector2(0.75f, 0.90f)); // 결과 요약 텍스트 동일 위치 배치
            summaryText.gameObject.SetActive(false); // 결과 요약 초기 숨김

            List<RhythmCircleData> data = UltimateRhythmChallengeGenerator.Generate(); // 궁극기 리듬 챌린지 원 데이터 생성

            for (int index = 0; index < data.Count; index++) // 생성 원 데이터 순회
            {
                CreateCircle(data[index]); // 개별 원 생성
            }
        }

        private void CreateCircle(RhythmCircleData data) // 개별 리듬 원 생성
        {
            GameObject rootObject = new GameObject($"RhythmCircle_{circles.Count}", typeof(RectTransform), typeof(Image), typeof(Button)); // 원 클릭 루트 객체 생성
            rootObject.transform.SetParent(transform, false); // 챌린지 루트 부모 연결
            RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 원 루트 RectTransform 조회
            rootRect.anchorMin = data.NormalizedPosition; // 원 위치 최소 앵커 적용 (점 앵커)
            rootRect.anchorMax = data.NormalizedPosition; // 원 위치 최대 앵커 적용 (점 앵커)
            rootRect.pivot = new Vector2(0.5f, 0.5f); // 원 중심 피벗 설정
            rootRect.sizeDelta = new Vector2(CircleSizePixels, CircleSizePixels); // 원 클릭 영역 크기 설정
            rootRect.anchoredPosition = Vector2.zero; // 원 위치 오프셋 초기화
            Image rootImage = rootObject.GetComponent<Image>(); // 원 루트 클릭 이미지 조회
            rootImage.color = new Color(1f, 1f, 1f, 0.01f); // 원 루트 투명 클릭 판정 영역 적용

            Image targetImage = RuntimeUiKit.CreateImage(rootObject.transform, "Target", TargetColor); // 목표 크기 원 이미지 생성
            RuntimeUiKit.Stretch(targetImage.rectTransform); // 목표 원 전체 확장
            targetImage.raycastTarget = false; // 목표 원 클릭 차단 비활성화

            Image ringImage = RuntimeUiKit.CreateImage(rootObject.transform, "Ring", RingColor); // 줄어드는 원 이미지 생성
            RuntimeUiKit.Stretch(ringImage.rectTransform); // 줄어드는 원 전체 확장
            ringImage.raycastTarget = false; // 줄어드는 원 클릭 차단 비활성화
            ringImage.rectTransform.localScale = new Vector3(RingStartScale, RingStartScale, 1f); // 줄어드는 원 시작 배율 적용

            Button button = rootObject.GetComponent<Button>(); // 원 클릭 버튼 컴포넌트 조회
            button.targetGraphic = rootImage; // 원 클릭 대상 그래픽 연결
            button.transition = Selectable.Transition.None; // 원 버튼 기본 전환 효과 비활성화

            CircleRuntime runtime = new CircleRuntime // 원 런타임 상태 생성
            {
                Ring = ringImage.rectTransform, // 줄어드는 원 RectTransform 저장
                RingImage = ringImage, // 줄어드는 원 이미지 저장
                ClickButton = button, // 클릭 버튼 저장
                Duration = data.ShrinkSeconds, // 줄어드는 소요 시간 저장
                Elapsed = 0f, // 경과 시간 초기화
                Resolved = false // 판정 완료 상태 초기화
            };

            button.onClick.AddListener(() => HandleCircleClicked(runtime, targetImage)); // 원 클릭 이벤트 연결
            circles.Add(runtime); // 원 런타임 목록 등록
        }

        private void Update() // 챌린지 원 진행 갱신
        {
            if (finished) // 챌린지 종료 상태 확인
            {
                return; // 갱신 중단
            }

            for (int index = 0; index < circles.Count; index++) // 생성 원 목록 순회
            {
                CircleRuntime circle = circles[index]; // 현재 원 런타임 조회

                if (circle.Resolved) // 이미 판정 완료 여부 확인
                {
                    continue; // 판정 완료 원 갱신 제외
                }

                circle.Elapsed += Time.deltaTime; // 경과 시간 누적
                float progress = Mathf.Clamp01(circle.Elapsed / circle.Duration); // 줄어드는 진행률 계산
                circle.Ring.localScale = new Vector3(Mathf.Lerp(RingStartScale, 1f, progress), Mathf.Lerp(RingStartScale, 1f, progress), 1f); // 줄어드는 원 배율 적용

                if (circle.Elapsed >= circle.Duration) // 시간 초과 여부 확인
                {
                    ResolveCircle(circle, RhythmHitResult.Miss); // 시간 초과 자동 Miss 판정
                }
            }

            CheckAllResolved(); // 전체 원 판정 완료 여부 확인
        }

        private void HandleCircleClicked(CircleRuntime circle, Image targetImage) // 원 클릭 처리
        {
            if (finished || circle.Resolved) // 챌린지 종료 또는 이미 판정된 원 확인
            {
                return; // 클릭 판정 중단
            }

            float progress = Mathf.Clamp01(circle.Elapsed / circle.Duration); // 클릭 시점 줄어드는 진행률 계산
            RhythmHitResult result = RhythmCircleJudge.Evaluate(progress); // 클릭 시점 진행률 기반 판정 실행
            ResolveCircle(circle, result); // 판정 결과 적용
            CheckAllResolved(); // 전체 원 판정 완료 여부 확인
        }

        private void ResolveCircle(CircleRuntime circle, RhythmHitResult result) // 단일 원 판정 결과 적용
        {
            circle.Resolved = true; // 판정 완료 상태 저장
            circle.ClickButton.interactable = false; // 판정 완료 원 입력 비활성화
            circle.RingImage.color = result == RhythmHitResult.Hit ? HitColor : MissColor; // 판정 결과 색상 적용

            if (result == RhythmHitResult.Hit) // Hit 판정 여부 확인
            {
                hitCount++; // 누적 Hit 수 증가
            }

            Destroy(circle.Ring.gameObject.transform.parent.gameObject, ResultHoldSeconds); // 판정 결과 표시 유지 후 원 객체 제거
        }

        private void CheckAllResolved() // 전체 원 판정 완료 여부 확인 및 종료 처리
        {
            if (finished) // 이미 종료 상태 확인
            {
                return; // 중복 종료 차단
            }

            for (int index = 0; index < circles.Count; index++) // 생성 원 목록 순회
            {
                if (!circles[index].Resolved) // 미판정 원 존재 확인
                {
                    return; // 전체 종료 대기
                }
            }

            finished = true; // 챌린지 종료 상태 저장
            int total = circles.Count; // 전체 원 개수 조회
            SetText(summaryText, $"HIT {hitCount} / {total}"); // 종합 결과 요약 문구 표시
            summaryText.gameObject.SetActive(true); // 결과 요약 텍스트 표시
            onComplete?.Invoke(hitCount, total); // 챌린지 종료 콜백 실행
            Destroy(gameObject, FinalHoldSeconds); // 결과 요약 유지 후 챌린지 전체 제거
        }

        private Text CreateText(string name, string value, int fontSize, Color color) // 공통 챌린지 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI 텍스트 객체 생성
            textObject.transform.SetParent(transform, false); // UI 텍스트 부모 연결
            Text text = textObject.GetComponent<Text>(); // UI Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // UI 텍스트 값 적용
            text.fontSize = fontSize; // UI 텍스트 크기 적용
            text.fontStyle = FontStyle.Bold; // UI 텍스트 굵기 적용
            text.color = color; // UI 텍스트 색상 적용
            text.alignment = TextAnchor.MiddleCenter; // UI 텍스트 중앙 정렬
            text.raycastTarget = false; // UI 텍스트 입력 비활성화
            return text; // 생성 UI 텍스트 반환
        }

        private static void SetText(Text target, string value) // 텍스트 안전 설정
        {
            if (target != null) // 텍스트 참조 확인
            {
                target.text = value; // 텍스트 값 적용
            }
        }
    }
}
