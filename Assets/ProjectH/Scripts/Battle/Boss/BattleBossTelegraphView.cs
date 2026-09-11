using ProjectH.UI; // Runtime UI 생성 공용 헬퍼 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle.Boss // 프로젝트 전투 보스 영역 (Day54)
{
    [DisallowMultipleComponent] // 중복 예고 표시 방지
    public sealed class BattleBossTelegraphView : MonoBehaviour // 보스 패턴 예고 표시 뷰 (Day54 신규, 런타임 생성 전용)
    {
        private const float PanelHeightPixels = 34f; // 예고 패널 높이(px)
        private const float PanelGapPixels = 6f; // 기준 대상과의 간격(px)
        private const float BarHeightPixels = 6f; // 예고 진행 바 높이(px)
        private const float ResultHoldSeconds = 0.9f; // 저지·발동 결과 문구 유지 시간
        private static readonly Color WindupColor = new Color(1f, 0.52f, 0.28f, 1f); // 예고 중 문구·바 색상 (주황)
        private static readonly Color UninterruptibleColor = new Color(0.92f, 0.26f, 0.32f, 1f); // 저지 불가 예고 색상 (붉은색)
        private static readonly Color InterruptedColor = new Color(1f, 0.86f, 0.36f, 1f); // 저지 성공 문구 색상 (금색)

        private Text labelText; // 예고 문구 텍스트
        private Image barFill; // 예고 진행 바 채움 이미지
        private RectTransform barFillRect; // 예고 진행 바 채움 RectTransform
        private GameObject barRoot; // 예고 진행 바 루트
        private float resultTimer; // 결과 문구 잔여 표시 시간

        public static BattleBossTelegraphView AttachAbove(RectTransform anchorTarget) // 지정 대상 위쪽에 예고 표시 부착 (보스 머리 위)
        {
            if (anchorTarget == null) // 부착 기준 대상 확인
            {
                return null; // 부착 대상 없음 반환
            }

            BattleBossTelegraphView existing = anchorTarget.GetComponentInChildren<BattleBossTelegraphView>(true); // 기존 예고 표시 조회

            if (existing != null) // 기존 예고 표시 존재 확인
            {
                return existing; // 기존 예고 표시 반환
            }

            GameObject root = new GameObject("BossTelegraph", typeof(RectTransform), typeof(BattleBossTelegraphView)); // 예고 표시 루트 생성
            root.transform.SetParent(anchorTarget, false); // 기준 대상 자식으로 연결
            RectTransform rect = root.GetComponent<RectTransform>(); // 예고 표시 RectTransform 조회
            rect.anchorMin = new Vector2(0f, 1f); // 기준 대상 상단 최소 앵커 설정
            rect.anchorMax = new Vector2(1f, 1f); // 기준 대상 상단 최대 앵커 설정
            rect.pivot = new Vector2(0.5f, 0f); // 기준 대상 위쪽 방향 피벗 설정
            rect.sizeDelta = new Vector2(0f, PanelHeightPixels); // 예고 패널 높이 설정
            rect.anchoredPosition = new Vector2(0f, PanelGapPixels); // 기준 대상과 간격 적용

            BattleBossTelegraphView view = root.GetComponent<BattleBossTelegraphView>(); // 예고 표시 컴포넌트 조회
            view.Build(); // 예고 표시 구성 요소 생성
            view.Hide(); // 초기 숨김 적용
            return view; // 생성 예고 표시 반환
        }

        private void Build() // 예고 표시 구성 요소 생성
        {
            GameObject textObject = new GameObject("TelegraphLabel", typeof(RectTransform), typeof(Text), typeof(Outline)); // 예고 문구 객체 생성
            textObject.transform.SetParent(transform, false); // 예고 문구 부모 연결
            RuntimeUiKit.SetRect(textObject.GetComponent<RectTransform>(), new Vector2(-0.3f, 0.30f), new Vector2(1.3f, 1f)); // 예고 문구 패널 상단 배치 (좁은 적 폭보다 넓게 허용)
            labelText = textObject.GetComponent<Text>(); // 예고 문구 Text 조회
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            labelText.fontSize = 18; // 예고 문구 크기 적용
            labelText.fontStyle = FontStyle.Bold; // 예고 문구 굵기 적용
            labelText.alignment = TextAnchor.MiddleCenter; // 예고 문구 중앙 정렬
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow; // 좁은 영역에서도 문구 유지
            labelText.verticalOverflow = VerticalWrapMode.Overflow; // 세로 잘림 방지
            labelText.raycastTarget = false; // 예고 문구 입력 비활성화
            Outline outline = textObject.GetComponent<Outline>(); // 예고 문구 외곽선 조회
            outline.effectColor = new Color(0.02f, 0.04f, 0.08f, 0.9f); // 어두운 외곽선 색상 적용
            outline.effectDistance = new Vector2(1.5f, -1.5f); // 외곽선 두께 적용

            Image barBack = RuntimeUiKit.CreateImage(transform, "TelegraphBarBack", new Color(0.08f, 0.09f, 0.13f, 0.85f)); // 예고 진행 바 배경 생성
            barBack.raycastTarget = false; // 진행 바 배경 클릭 차단 비활성화
            RectTransform barBackRect = barBack.rectTransform; // 진행 바 배경 RectTransform 조회
            barBackRect.anchorMin = new Vector2(0.1f, 0f); // 진행 바 최소 앵커 하단 설정
            barBackRect.anchorMax = new Vector2(0.9f, 0f); // 진행 바 최대 앵커 하단 설정
            barBackRect.pivot = new Vector2(0.5f, 0f); // 진행 바 하단 피벗 설정
            barBackRect.sizeDelta = new Vector2(0f, BarHeightPixels); // 진행 바 높이 설정
            barBackRect.anchoredPosition = Vector2.zero; // 진행 바 위치 초기화
            barRoot = barBack.gameObject; // 진행 바 루트 저장

            barFill = RuntimeUiKit.CreateImage(barBack.transform, "TelegraphBarFill", WindupColor); // 예고 진행 바 채움 생성
            barFill.raycastTarget = false; // 진행 바 채움 클릭 차단 비활성화
            barFillRect = barFill.rectTransform; // 진행 바 채움 RectTransform 저장
            barFillRect.anchorMin = Vector2.zero; // 채움 최소 앵커 좌하단 설정
            barFillRect.anchorMax = new Vector2(1f, 1f); // 채움 최대 앵커 가득 시작 설정
            barFillRect.offsetMin = Vector2.zero; // 채움 최소 오프셋 초기화
            barFillRect.offsetMax = Vector2.zero; // 채움 최대 오프셋 초기화
        }

        public void ShowWindup(string patternName, float progress, bool interruptible) // 패턴 예고 진행 표시
        {
            resultTimer = 0f; // 결과 문구 표시 중단
            gameObject.SetActive(true); // 예고 표시 노출
            barRoot.SetActive(true); // 진행 바 노출
            Color color = interruptible ? WindupColor : UninterruptibleColor; // 저지 가능 여부 기반 색상 결정
            labelText.text = interruptible ? $"{patternName} 준비 중" : $"{patternName} 준비 중 · 저지 불가"; // 예고 문구 적용
            labelText.color = color; // 예고 문구 색상 적용
            barFill.color = color; // 진행 바 색상 적용
            barFillRect.anchorMax = new Vector2(Mathf.Clamp01(1f - progress), 1f); // 남은 시간 비율로 줄어드는 바 적용 (앵커 확장 방식)
        }

        public void ShowInterrupted(string patternName) // 흐트러짐 저지 성공 표시
        {
            ShowResult(string.IsNullOrWhiteSpace(patternName) ? "저지!" : $"{patternName} 저지!", InterruptedColor); // 금색 저지 문구 표시
        }

        public void ShowExecuted(string patternName) // 패턴 발동 표시
        {
            ShowResult(patternName, UninterruptibleColor); // 붉은 발동 문구 표시
        }

        private void ShowResult(string message, Color color) // 결과 문구 공통 표시
        {
            gameObject.SetActive(true); // 예고 표시 노출
            barRoot.SetActive(false); // 결과 표시 중 진행 바 숨김
            labelText.text = message; // 결과 문구 적용
            labelText.color = color; // 결과 문구 색상 적용
            resultTimer = ResultHoldSeconds; // 결과 문구 유지 시간 설정
        }

        public void Hide() // 예고 표시 숨김
        {
            resultTimer = 0f; // 결과 문구 타이머 초기화
            gameObject.SetActive(false); // 예고 표시 숨김
        }

        private void Update() // 결과 문구 유지 시간 갱신
        {
            if (resultTimer <= 0f) // 결과 문구 표시 여부 확인
            {
                return; // 갱신 중단
            }

            resultTimer -= Time.unscaledDeltaTime; // 결과 문구 잔여 시간 감소 (일시정지 무관)

            if (resultTimer <= 0f) // 유지 시간 종료 확인
            {
                Hide(); // 결과 문구 숨김
            }
        }
    }
}
