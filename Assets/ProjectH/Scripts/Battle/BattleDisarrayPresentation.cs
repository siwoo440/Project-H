using System.Collections; // 코루틴 기능
using ProjectH.UI; // Runtime UI 생성 공용 헬퍼 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 흐트러짐 연출 컨트롤러 방지
    public sealed class BattleDisarrayPresentation : MonoBehaviour // 흐트러짐 발생 중앙 연출 컨트롤러 (Day53 신규)
    {
        private const float FlashSeconds = 0.18f; // 화면 번쩍임 유지 시간
        private const float HoldSeconds = 0.55f; // 중앙 문구 유지 시간
        private const float FadeSeconds = 0.25f; // 중앙 문구 사라짐 시간
        private static readonly Color FlashColor = new Color(0.98f, 0.72f, 0.22f, 0.30f); // 흐트러짐 번쩍임 색상
        private static readonly Color TextColor = new Color(1f, 0.86f, 0.36f, 1f); // 흐트러짐 문구 색상

        private static BattleDisarrayPresentation instance; // 현재 Scene 흐트러짐 연출 컨트롤러
        private Image flashImage; // 화면 번쩍임 이미지
        private Text announceText; // 중앙 흐트러짐 문구 텍스트
        private CanvasGroup announceGroup; // 중앙 문구 페이드 그룹
        private Coroutine playRoutine; // 연출 진행 코루틴 참조

        public static void Announce(BattleActor target) // 흐트러짐 발생 연출 실행
        {
            if (target == null) // 흐트러짐 대상 확인
            {
                return; // 연출 실행 중단
            }

            BattleDisarrayPresentation controller = EnsureRuntime(); // 연출 컨트롤러 확보

            if (controller == null) // 연출 컨트롤러 확인
            {
                return; // 연출 실행 중단
            }

            controller.Play(target.Stats == null ? string.Empty : target.Stats.DisplayName); // 대상 이름 기반 연출 시작
        }

        private static BattleDisarrayPresentation EnsureRuntime() // 흐트러짐 연출 컨트롤러 확보
        {
            if (instance != null) // 기존 컨트롤러 존재 확인
            {
                return instance; // 기존 컨트롤러 반환
            }

            GameObject canvasObject = new GameObject("DisarrayPresentationRuntime", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(BattleDisarrayPresentation)); // 연출 Canvas 생성
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // 연출 Canvas 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 Overlay 렌더링 설정
            canvas.sortingOrder = 560; // 일반 전투 HUD 위·리듬 챌린지 아래 표시 설정
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // 연출 CanvasScaler 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 화면 크기 기반 UI 스케일 설정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 화면 비율 대응 방식 설정
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 중간 스케일 적용

            instance = canvasObject.GetComponent<BattleDisarrayPresentation>(); // 연출 컨트롤러 조회
            instance.Build(); // 연출 구성 요소 생성
            return instance; // 생성 컨트롤러 반환
        }

        private void Build() // 흐트러짐 연출 구성 요소 생성
        {
            flashImage = RuntimeUiKit.CreateImage(transform, "DisarrayFlash", new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f)); // 화면 번쩍임 이미지 생성
            RuntimeUiKit.Stretch(flashImage.rectTransform); // 번쩍임 전체 화면 확장
            flashImage.raycastTarget = false; // 번쩍임 클릭 차단 비활성화 (전투 입력 방해 방지)

            GameObject announceObject = new GameObject("DisarrayAnnounceText", typeof(RectTransform), typeof(CanvasGroup), typeof(Text), typeof(Outline)); // 중앙 문구 객체 생성
            announceObject.transform.SetParent(transform, false); // 중앙 문구 부모 연결
            RuntimeUiKit.SetRect(announceObject.GetComponent<RectTransform>(), new Vector2(0.22f, 0.60f), new Vector2(0.78f, 0.70f)); // 중앙 문구 화면 중앙 상단 배치

            announceText = announceObject.GetComponent<Text>(); // 중앙 문구 Text 조회
            announceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            announceText.fontSize = 46; // 중앙 문구 크기 적용
            announceText.fontStyle = FontStyle.Bold; // 중앙 문구 굵기 적용
            announceText.color = TextColor; // 중앙 문구 색상 적용
            announceText.alignment = TextAnchor.MiddleCenter; // 중앙 문구 중앙 정렬
            announceText.horizontalOverflow = HorizontalWrapMode.Overflow; // 좁은 영역에서도 문구 유지
            announceText.verticalOverflow = VerticalWrapMode.Overflow; // 세로 잘림 방지
            announceText.raycastTarget = false; // 중앙 문구 입력 비활성화

            Outline outline = announceObject.GetComponent<Outline>(); // 중앙 문구 외곽선 조회
            outline.effectColor = new Color(0.02f, 0.04f, 0.08f, 0.88f); // 어두운 외곽선 색상 적용
            outline.effectDistance = new Vector2(2.5f, -2.5f); // 외곽선 두께 적용

            announceGroup = announceObject.GetComponent<CanvasGroup>(); // 중앙 문구 페이드 그룹 조회
            announceGroup.alpha = 0f; // 연출 전 완전 투명 적용
            announceGroup.blocksRaycasts = false; // 중앙 문구 입력 차단 비활성화
        }

        private void Play(string targetName) // 흐트러짐 연출 시작
        {
            if (announceText == null) // 연출 구성 확인
            {
                return; // 연출 시작 중단
            }

            announceText.text = string.IsNullOrWhiteSpace(targetName) ? "흐트러짐!" : $"{targetName} 흐트러짐!"; // 대상 이름 포함 연출 문구 적용

            if (playRoutine != null) // 기존 연출 코루틴 확인
            {
                StopCoroutine(playRoutine); // 기존 연출 코루틴 중단
            }

            playRoutine = StartCoroutine(PlayRoutine()); // 흐트러짐 연출 코루틴 시작
        }

        private IEnumerator PlayRoutine() // 흐트러짐 연출 코루틴 (번쩍임 + 중앙 문구)
        {
            announceGroup.alpha = 1f; // 중앙 문구 즉시 표시
            float elapsed = 0f; // 번쩍임 경과 시간 초기화

            while (elapsed < FlashSeconds) // 번쩍임 유지 시간 동안 반복
            {
                elapsed += Time.unscaledDeltaTime; // 번쩍임 경과 시간 누적 (일시정지 무관 연출)
                float alpha = Mathf.Lerp(FlashColor.a, 0f, Mathf.Clamp01(elapsed / FlashSeconds)); // 번쩍임 알파 감쇠 계산
                flashImage.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, alpha); // 번쩍임 알파 적용
                yield return null; // 다음 프레임 대기
            }

            flashImage.color = new Color(FlashColor.r, FlashColor.g, FlashColor.b, 0f); // 번쩍임 완전 제거
            float hold = 0f; // 중앙 문구 유지 시간 초기화

            while (hold < HoldSeconds) // 중앙 문구 유지 시간 동안 반복
            {
                hold += Time.unscaledDeltaTime; // 유지 시간 누적
                yield return null; // 다음 프레임 대기
            }

            float fade = 0f; // 중앙 문구 사라짐 시간 초기화

            while (fade < FadeSeconds) // 중앙 문구 사라짐 시간 동안 반복
            {
                fade += Time.unscaledDeltaTime; // 사라짐 시간 누적
                announceGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(fade / FadeSeconds)); // 중앙 문구 알파 감쇠 적용
                yield return null; // 다음 프레임 대기
            }

            announceGroup.alpha = 0f; // 중앙 문구 완전 숨김
            playRoutine = null; // 연출 코루틴 참조 초기화
        }

        private void OnDestroy() // 흐트러짐 연출 컨트롤러 제거 처리
        {
            if (instance == this) // 현재 등록 컨트롤러 여부 확인
            {
                instance = null; // 연출 컨트롤러 참조 해제 (씬 전환 후 재생성 보장)
            }
        }
    }
}
