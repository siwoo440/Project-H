using System; // Action 델리게이트 기능
using System.Collections.Generic; // 목록 자료형
using ProjectH.UI; // Runtime UI 생성 공용 헬퍼 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle.Rhythm // 프로젝트 전투 리듬 영역 (Day49)
{
    [DisallowMultipleComponent] // 중복 리듬 챌린지 뷰 방지
    public sealed class UltimateRhythmChallengeView : MonoBehaviour // 궁극기 리듬 챌린지 화면 (박자 순차 등장·원 축소·클릭 판정)
    {
        private static readonly Color DiscColor = new Color(0.13f, 0.42f, 0.82f, 0.80f); // 목표 원 색상
        private static readonly Color RingColor = new Color(0.72f, 0.93f, 1f, 0.95f); // 줄어드는 링 색상
        private static readonly Color BeatDotColor = new Color(0.72f, 0.93f, 1f, 0.85f); // 박자 표시 점 색상
        private const float CircleSizePixels = UltimateRhythmChallengeGenerator.CircleSizePixels; // 원 기준 크기(px) — Generator와 동일 값 공유
        private const float RingStartScale = 2.8f; // 링 시작 배율
        private const float SpawnFadeSeconds = 0.18f; // 원 등장 페이드 시간
        private const float ResultHoldSeconds = 0.45f; // 판정 결과 표시 유지 시간
        private const float FinalHoldSeconds = 1.1f; // 전체 종료 후 요약 문구 유지 시간
        private const float BeatPulseScale = 0.35f; // 박자 표시 점 최대 확대 폭

        private readonly List<CircleRuntime> circles = new List<CircleRuntime>(); // 생성 원 런타임 목록
        private Action<RhythmChallengeResult> onComplete; // 챌린지 종료 콜백
        private Text summaryText; // 종합 결과 요약 텍스트
        private Text titleText; // 챌린지 안내 제목 텍스트
        private RectTransform beatDot; // 박자 표시 점 RectTransform
        private float challengeElapsed; // 챌린지 시작 기준 경과 시간
        private int perfectCount; // 누적 Perfect 수
        private int goodCount; // 누적 Good 수
        private int missCount; // 누적 Miss 수
        private bool finished; // 전체 챌린지 종료 여부

        private sealed class CircleRuntime // 단일 원 런타임 상태
        {
            public GameObject Root; // 원 루트 객체
            public CanvasGroup Group; // 원 페이드 제어 그룹
            public RectTransform Ring; // 줄어드는 링 RectTransform
            public Image RingImage; // 줄어드는 링 이미지
            public Image DiscImage; // 목표 원 이미지
            public Text NumberText; // 순서 번호 텍스트
            public Text JudgeText; // 판정 결과 문구 텍스트
            public Button ClickButton; // 원 클릭 버튼
            public float SpawnDelay; // 등장 지연 시간
            public float Duration; // 줄어드는 소요 시간
            public float Elapsed; // 등장 이후 경과 시간
            public bool Spawned; // 등장 여부
            public bool Resolved; // 판정 완료 여부
        }

        public static UltimateRhythmChallengeView Show(string ultimateName, Action<RhythmChallengeResult> onComplete) // 궁극기 리듬 챌린지 표시 시작
        {
            GameObject canvasObject = new GameObject("UltimateRhythmChallengeRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UltimateRhythmChallengeView)); // 챌린지 Canvas 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 챌린지 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 600; // 일반 전투 HUD 위에 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 챌린지 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(UltimateRhythmChallengeGenerator.ReferenceWidth, UltimateRhythmChallengeGenerator.ReferenceHeight); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용
            UltimateRhythmChallengeView view = canvasObject.GetComponent<UltimateRhythmChallengeView>(); // 챌린지 뷰 컴포넌트 조회
            view.onComplete = onComplete; // 종료 콜백 연결
            view.BuildChallenge(ultimateName); // 챌린지 원 생성 및 배치
            return view; // 생성 챌린지 뷰 반환
        }

        private void BuildChallenge(string ultimateName) // 챌린지 구성 요소 생성
        {
            Image dim = RuntimeUiKit.CreateImage(transform, "Dim", new Color(0.02f, 0.03f, 0.06f, 0.28f)); // 배경 어둡게 표시
            RuntimeUiKit.Stretch(dim.rectTransform); // 배경 전체 화면 확장
            dim.raycastTarget = false; // 배경 클릭 차단 비활성화

            titleText = CreateText(transform, "TitleText", string.IsNullOrWhiteSpace(ultimateName) ? "TIMING!" : $"{ultimateName}", 34, new Color(1f, 1f, 1f, 0.94f)); // 챌린지 안내 제목 생성
            RuntimeUiKit.SetRect(titleText.rectTransform, new Vector2(0.25f, 0.84f), new Vector2(0.75f, 0.92f)); // 안내 제목 상단 배치

            summaryText = CreateText(transform, "SummaryText", string.Empty, 30, Color.white); // 결과 요약 텍스트 생성
            RuntimeUiKit.SetRect(summaryText.rectTransform, new Vector2(0.20f, 0.84f), new Vector2(0.80f, 0.92f)); // 결과 요약 텍스트 배치
            summaryText.gameObject.SetActive(false); // 결과 요약 초기 숨김

            BuildBeatIndicator(); // 하단 박자 표시 생성

            List<RhythmCircleData> data = UltimateRhythmChallengeGenerator.Generate(); // 궁극기 리듬 챌린지 원 데이터 생성

            for (int index = 0; index < data.Count; index++) // 생성 원 데이터 순회
            {
                CreateCircle(data[index]); // 개별 원 생성
            }
        }

        private void BuildBeatIndicator() // 하단 박자 표시 생성
        {
            Text bpmText = CreateText(transform, "BpmText", $"♪ {UltimateRhythmChallengeGenerator.BeatsPerMinute:0} BPM", 20, new Color(0.72f, 0.93f, 1f, 0.75f)); // BPM 안내 문구 생성
            RuntimeUiKit.SetRect(bpmText.rectTransform, new Vector2(0.42f, 0.185f), new Vector2(0.58f, 0.235f)); // BPM 문구 하단 배치

            Image dot = RuntimeUiKit.CreateImage(transform, "BeatDot", BeatDotColor); // 박자 표시 점 생성
            dot.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // 원형 스프라이트 적용
            dot.raycastTarget = false; // 박자 점 클릭 차단 비활성화
            beatDot = dot.rectTransform; // 박자 점 RectTransform 저장
            beatDot.anchorMin = new Vector2(0.5f, 0.165f); // 박자 점 최소 앵커 설정
            beatDot.anchorMax = new Vector2(0.5f, 0.165f); // 박자 점 최대 앵커 설정
            beatDot.pivot = new Vector2(0.5f, 0.5f); // 박자 점 중심 피벗 설정
            beatDot.sizeDelta = new Vector2(26f, 26f); // 박자 점 크기 설정
            beatDot.anchoredPosition = Vector2.zero; // 박자 점 위치 오프셋 초기화
        }

        private void CreateCircle(RhythmCircleData data) // 개별 리듬 원 생성
        {
            GameObject rootObject = new GameObject($"RhythmCircle_{data.OrderNumber}", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button)); // 원 루트 객체 생성
            rootObject.transform.SetParent(transform, false); // 챌린지 루트 부모 연결
            RectTransform rootRect = rootObject.GetComponent<RectTransform>(); // 원 루트 RectTransform 조회
            rootRect.anchorMin = data.NormalizedPosition; // 원 위치 최소 앵커 적용 (점 앵커)
            rootRect.anchorMax = data.NormalizedPosition; // 원 위치 최대 앵커 적용 (점 앵커)
            rootRect.pivot = new Vector2(0.5f, 0.5f); // 원 중심 피벗 설정
            rootRect.sizeDelta = new Vector2(CircleSizePixels, CircleSizePixels); // 원 크기 설정
            rootRect.anchoredPosition = Vector2.zero; // 원 위치 오프셋 초기화

            Image discImage = rootObject.GetComponent<Image>(); // 목표 원 이미지 조회
            discImage.sprite = RhythmCircleSpriteFactory.GetDiscSprite(); // 채움 원 스프라이트 적용
            discImage.color = DiscColor; // 목표 원 색상 적용
            discImage.raycastTarget = true; // 목표 원 클릭 판정 활성화
            discImage.alphaHitTestMinimumThreshold = 0.1f; // 원 모양 기준 클릭 판정 적용 (모서리 빈 공간 클릭 방지)

            Image ringImage = RuntimeUiKit.CreateImage(rootObject.transform, "Ring", RingColor); // 줄어드는 링 이미지 생성
            RuntimeUiKit.Stretch(ringImage.rectTransform); // 링 전체 확장
            ringImage.sprite = RhythmCircleSpriteFactory.GetRingSprite(); // 테두리 링 스프라이트 적용
            ringImage.raycastTarget = false; // 링 클릭 차단 비활성화
            ringImage.rectTransform.localScale = new Vector3(RingStartScale, RingStartScale, 1f); // 링 시작 배율 적용

            Text numberText = CreateText(rootObject.transform, "NumberText", data.OrderNumber.ToString(), 40, Color.white); // 순서 번호 텍스트 생성
            RuntimeUiKit.Stretch(numberText.rectTransform); // 순서 번호 전체 확장

            Text judgeText = CreateText(rootObject.transform, "JudgeText", string.Empty, 26, Color.white); // 판정 문구 텍스트 생성
            RuntimeUiKit.SetRect(judgeText.rectTransform, new Vector2(-0.6f, 1.05f), new Vector2(1.6f, 1.75f)); // 판정 문구 원 위쪽 배치
            judgeText.gameObject.SetActive(false); // 판정 문구 초기 숨김

            Button button = rootObject.GetComponent<Button>(); // 원 클릭 버튼 컴포넌트 조회
            button.targetGraphic = discImage; // 원 클릭 대상 그래픽 연결
            button.transition = Selectable.Transition.None; // 원 버튼 기본 전환 효과 비활성화

            CanvasGroup group = rootObject.GetComponent<CanvasGroup>(); // 원 페이드 그룹 조회
            group.alpha = 0f; // 등장 전 완전 투명 적용

            CircleRuntime runtime = new CircleRuntime // 원 런타임 상태 생성
            {
                Root = rootObject, // 원 루트 객체 저장
                Group = group, // 페이드 그룹 저장
                Ring = ringImage.rectTransform, // 링 RectTransform 저장
                RingImage = ringImage, // 링 이미지 저장
                DiscImage = discImage, // 목표 원 이미지 저장
                NumberText = numberText, // 순서 번호 텍스트 저장
                JudgeText = judgeText, // 판정 문구 텍스트 저장
                ClickButton = button, // 클릭 버튼 저장
                SpawnDelay = data.SpawnDelaySeconds, // 등장 지연 저장
                Duration = data.ShrinkSeconds, // 줄어드는 소요 시간 저장
                Elapsed = 0f, // 경과 시간 초기화
                Spawned = false, // 등장 상태 초기화
                Resolved = false // 판정 완료 상태 초기화
            };

            button.onClick.AddListener(() => HandleCircleClicked(runtime)); // 원 클릭 이벤트 연결
            rootObject.SetActive(false); // 등장 시각 전까지 숨김
            circles.Add(runtime); // 원 런타임 목록 등록
        }

        private void Update() // 챌린지 진행 갱신
        {
            challengeElapsed += Time.deltaTime; // 챌린지 경과 시간 누적
            RefreshBeatIndicator(); // 박자 표시 갱신

            if (finished) // 챌린지 종료 상태 확인
            {
                return; // 원 갱신 중단
            }

            for (int index = 0; index < circles.Count; index++) // 생성 원 목록 순회
            {
                UpdateCircle(circles[index]); // 개별 원 진행 갱신
            }

            CheckAllResolved(); // 전체 원 판정 완료 여부 확인
        }

        private void UpdateCircle(CircleRuntime circle) // 개별 원 진행 갱신
        {
            if (!circle.Spawned) // 등장 여부 확인
            {
                if (challengeElapsed < circle.SpawnDelay) // 등장 시각 도달 여부 확인
                {
                    return; // 등장 대기 유지
                }

                circle.Spawned = true; // 등장 상태 저장
                circle.Root.SetActive(true); // 원 객체 표시
            }

            if (circle.Resolved) // 이미 판정 완료 여부 확인
            {
                return; // 판정 완료 원 갱신 제외
            }

            circle.Elapsed += Time.deltaTime; // 등장 이후 경과 시간 누적
            circle.Group.alpha = Mathf.Clamp01(circle.Elapsed / SpawnFadeSeconds); // 등장 페이드 적용
            float progress = circle.Elapsed / circle.Duration; // 줄어드는 진행률 계산 (1.0이 완벽 판정 순간)
            float ringScale = Mathf.Lerp(RingStartScale, 1f, Mathf.Clamp01(progress)); // 링 배율 계산
            circle.Ring.localScale = new Vector3(ringScale, ringScale, 1f); // 링 배율 적용

            if (progress >= 1f - RhythmCircleJudge.PerfectWindow) // Perfect 구간 진입 여부 확인
            {
                circle.RingImage.color = RhythmCircleJudge.GetColor(RhythmHitResult.Perfect); // Perfect 구간 링 강조 색상 적용
            }
            else if (progress >= 1f - RhythmCircleJudge.GoodWindow) // Good 구간 진입 여부 확인
            {
                circle.RingImage.color = RhythmCircleJudge.GetColor(RhythmHitResult.Good); // Good 구간 링 강조 색상 적용
            }

            if (progress > 1f + RhythmCircleJudge.LateWindow) // 판정 허용 시간 초과 여부 확인
            {
                ResolveCircle(circle, RhythmHitResult.Miss); // 시간 초과 자동 Miss 판정
            }
        }

        private void RefreshBeatIndicator() // 박자 표시 점 갱신
        {
            if (beatDot == null) // 박자 표시 존재 확인
            {
                return; // 박자 표시 갱신 중단
            }

            float beatPhase = Mathf.Repeat(challengeElapsed / UltimateRhythmChallengeGenerator.SecondsPerBeat, 1f); // 현재 박자 진행 위상 계산
            float pulse = 1f + (BeatPulseScale * (1f - beatPhase)); // 박자 시작에서 커졌다가 줄어드는 배율 계산
            beatDot.localScale = new Vector3(pulse, pulse, 1f); // 박자 표시 점 배율 적용
        }

        private void HandleCircleClicked(CircleRuntime circle) // 원 클릭 처리
        {
            if (finished || circle.Resolved || !circle.Spawned) // 챌린지 종료·이미 판정·미등장 여부 확인
            {
                return; // 클릭 판정 중단
            }

            float progress = circle.Elapsed / circle.Duration; // 클릭 시점 줄어드는 진행률 계산
            RhythmHitResult result = RhythmCircleJudge.Evaluate(progress); // 클릭 시점 진행률 기반 판정 실행
            ResolveCircle(circle, result); // 판정 결과 적용
            CheckAllResolved(); // 전체 원 판정 완료 여부 확인
        }

        private void ResolveCircle(CircleRuntime circle, RhythmHitResult result) // 단일 원 판정 결과 적용
        {
            circle.Resolved = true; // 판정 완료 상태 저장
            circle.ClickButton.interactable = false; // 판정 완료 원 입력 비활성화
            Color resultColor = RhythmCircleJudge.GetColor(result); // 판정 결과 색상 조회
            circle.RingImage.color = resultColor; // 링 판정 색상 적용
            circle.DiscImage.color = new Color(resultColor.r, resultColor.g, resultColor.b, 0.55f); // 목표 원 판정 색상 적용
            circle.NumberText.gameObject.SetActive(false); // 순서 번호 숨김
            circle.JudgeText.text = RhythmCircleJudge.GetLabel(result); // 판정 문구 적용
            circle.JudgeText.color = resultColor; // 판정 문구 색상 적용
            circle.JudgeText.gameObject.SetActive(true); // 판정 문구 표시
            CountResult(result); // 판정 등급 집계
            Destroy(circle.Root, ResultHoldSeconds); // 판정 결과 표시 유지 후 원 객체 제거
        }

        private void CountResult(RhythmHitResult result) // 판정 등급 집계
        {
            switch (result) // 판정 결과 분기
            {
                case RhythmHitResult.Perfect: // Perfect 처리
                    perfectCount++; // Perfect 수 증가
                    break; // Perfect 분기 종료
                case RhythmHitResult.Good: // Good 처리
                    goodCount++; // Good 수 증가
                    break; // Good 분기 종료
                default: // Miss 처리
                    missCount++; // Miss 수 증가
                    break; // Miss 분기 종료
            }
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
            RhythmChallengeResult result = new RhythmChallengeResult(perfectCount, goodCount, missCount); // 종합 결과 생성
            titleText.gameObject.SetActive(false); // 안내 제목 숨김
            summaryText.text = result.ToString(); // 종합 결과 요약 문구 표시
            summaryText.color = result.MissCount == 0 ? RhythmCircleJudge.GetColor(RhythmHitResult.Perfect) : Color.white; // 전체 성공 시 강조 색상 적용
            summaryText.gameObject.SetActive(true); // 결과 요약 텍스트 표시
            onComplete?.Invoke(result); // 챌린지 종료 콜백 실행
            Destroy(gameObject, FinalHoldSeconds); // 결과 요약 유지 후 챌린지 전체 제거
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Color color) // 공통 챌린지 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline)); // UI 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // UI 텍스트 부모 연결
            Text text = textObject.GetComponent<Text>(); // UI Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.text = value; // UI 텍스트 값 적용
            text.fontSize = fontSize; // UI 텍스트 크기 적용
            text.fontStyle = FontStyle.Bold; // UI 텍스트 굵기 적용
            text.color = color; // UI 텍스트 색상 적용
            text.alignment = TextAnchor.MiddleCenter; // UI 텍스트 중앙 정렬
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 좁은 영역에서도 문구 유지
            text.verticalOverflow = VerticalWrapMode.Overflow; // 세로 잘림 방지
            text.raycastTarget = false; // UI 텍스트 입력 비활성화
            Outline outline = textObject.GetComponent<Outline>(); // 텍스트 외곽선 조회
            outline.effectColor = new Color(0.02f, 0.04f, 0.08f, 0.85f); // 어두운 외곽선 색상 적용 (배경 대비 가독성 확보)
            outline.effectDistance = new Vector2(2f, -2f); // 외곽선 두께 적용
            return text; // 생성 UI 텍스트 반환
        }
    }
}
