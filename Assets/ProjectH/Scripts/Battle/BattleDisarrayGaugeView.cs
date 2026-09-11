using ProjectH.UI; // Runtime UI 생성 공용 헬퍼 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 흐트러짐 게이지 방지
    public sealed class BattleDisarrayGaugeView : MonoBehaviour // 흐트러짐 게이지 표시 뷰 (Day53 신규, 런타임 생성 전용)
    {
        private const float GaugeHeightPixels = 7f; // 게이지 높이(px)
        private const float GaugeGapPixels = 2f; // 체력바와의 간격(px)
        private const float RefreshIntervalSeconds = 0.1f; // 게이지 갱신 간격
        private const float NearFullRatio = 0.8f; // 깜빡임 시작 누적 비율
        private const float BlinkCyclesPerSecond = 3f; // 깜빡임 주기
        private static readonly Color BackColor = new Color(0.08f, 0.09f, 0.13f, 0.80f); // 게이지 배경 색상
        private static readonly Color FillColor = new Color(0.98f, 0.82f, 0.30f, 0.95f); // 평상시 채움 색상 (노란색)
        private static readonly Color NearFullColor = new Color(1f, 0.62f, 0.22f, 0.98f); // 임박 채움 색상 (주황색)
        private static readonly Color DisarrayedColor = new Color(0.96f, 0.28f, 0.30f, 1f); // 흐트러짐 채움 색상 (붉은색)

        private string boundRuntimeId; // 표시 대상 Runtime ID
        private Image fillImage; // 게이지 채움 이미지
        private RectTransform fillRect; // 게이지 채움 RectTransform
        private Text labelText; // 게이지 상태 문구 텍스트
        private float refreshTimer; // 갱신 간격 누적 타이머

        public static BattleDisarrayGaugeView AttachBelow(RectTransform anchorTarget, string runtimeId) // 지정 대상 아래쪽에 흐트러짐 게이지 부착
        {
            if (anchorTarget == null) // 부착 기준 대상 확인
            {
                return null; // 부착 대상 없음 반환
            }

            BattleDisarrayGaugeView existing = FindExisting(anchorTarget); // 기존 생성 게이지 조회

            if (existing != null) // 기존 게이지 존재 확인
            {
                existing.Bind(runtimeId); // 표시 대상만 갱신
                return existing; // 기존 게이지 반환
            }

            GameObject gaugeObject = new GameObject("DisarrayGauge", typeof(RectTransform), typeof(BattleDisarrayGaugeView)); // 흐트러짐 게이지 루트 생성
            gaugeObject.transform.SetParent(anchorTarget, false); // 기준 대상 자식으로 연결
            RectTransform gaugeRect = gaugeObject.GetComponent<RectTransform>(); // 게이지 RectTransform 조회
            gaugeRect.anchorMin = new Vector2(0f, 0f); // 기준 대상 하단 최소 앵커 설정
            gaugeRect.anchorMax = new Vector2(1f, 0f); // 기준 대상 하단 최대 앵커 설정
            gaugeRect.pivot = new Vector2(0.5f, 1f); // 기준 대상 아래 방향 피벗 설정
            gaugeRect.sizeDelta = new Vector2(0f, GaugeHeightPixels); // 게이지 높이 설정
            gaugeRect.anchoredPosition = new Vector2(0f, -GaugeGapPixels); // 체력바와의 간격 적용

            BattleDisarrayGaugeView view = gaugeObject.GetComponent<BattleDisarrayGaugeView>(); // 게이지 컴포넌트 조회
            view.BuildGauge(); // 게이지 구성 요소 생성
            view.Bind(runtimeId); // 표시 대상 연결
            return view; // 생성 게이지 반환
        }

        private static BattleDisarrayGaugeView FindExisting(RectTransform anchorTarget) // 기준 대상의 기존 게이지 조회
        {
            for (int index = 0; index < anchorTarget.childCount; index++) // 기준 대상 자식 순회
            {
                BattleDisarrayGaugeView candidate = anchorTarget.GetChild(index).GetComponent<BattleDisarrayGaugeView>(); // 자식 게이지 조회

                if (candidate != null) // 게이지 존재 확인
                {
                    return candidate; // 기존 게이지 반환
                }
            }

            return null; // 기존 게이지 없음 반환
        }

        public void Bind(string runtimeId) // 흐트러짐 게이지 표시 대상 연결
        {
            boundRuntimeId = runtimeId ?? string.Empty; // 표시 대상 Runtime ID 저장
            refreshTimer = RefreshIntervalSeconds; // 다음 Update에서 즉시 갱신되도록 타이머 설정
        }

        private void BuildGauge() // 흐트러짐 게이지 구성 요소 생성
        {
            Image background = RuntimeUiKit.CreateImage(transform, "GaugeBack", BackColor); // 게이지 배경 생성
            RuntimeUiKit.Stretch(background.rectTransform); // 게이지 배경 전체 확장
            background.raycastTarget = false; // 게이지 배경 클릭 차단 비활성화

            fillImage = RuntimeUiKit.CreateImage(background.transform, "GaugeFill", FillColor); // 게이지 채움 생성
            fillImage.raycastTarget = false; // 게이지 채움 클릭 차단 비활성화
            fillRect = fillImage.rectTransform; // 게이지 채움 RectTransform 저장
            fillRect.anchorMin = Vector2.zero; // 채움 최소 앵커 좌하단 설정
            fillRect.anchorMax = new Vector2(0f, 1f); // 채움 최대 앵커 0퍼센트 시작 설정 (스프라이트 없는 Filled 타입 미표시 문제 회피)
            fillRect.offsetMin = Vector2.zero; // 채움 최소 오프셋 초기화
            fillRect.offsetMax = Vector2.zero; // 채움 최대 오프셋 초기화

            labelText = CreateLabel(transform, "DisarrayLabel", 15, DisarrayedColor); // 흐트러짐 상태 문구 생성
            RectTransform labelRect = labelText.rectTransform; // 상태 문구 RectTransform 조회
            labelRect.anchorMin = new Vector2(0.5f, 0f); // 상태 문구 최소 앵커 설정
            labelRect.anchorMax = new Vector2(0.5f, 0f); // 상태 문구 최대 앵커 설정
            labelRect.pivot = new Vector2(0.5f, 1f); // 상태 문구 게이지 아래 방향 피벗 설정
            labelRect.sizeDelta = new Vector2(120f, 20f); // 상태 문구 크기 설정
            labelRect.anchoredPosition = new Vector2(0f, -GaugeGapPixels); // 상태 문구 게이지 아래 배치
            labelText.gameObject.SetActive(false); // 흐트러짐 전 상태 문구 숨김
        }

        private void Update() // 흐트러짐 게이지 주기 갱신
        {
            refreshTimer += Time.unscaledDeltaTime; // 갱신 간격 누적 (전투 일시정지 중에도 표시 유지)

            if (refreshTimer < RefreshIntervalSeconds) // 갱신 시각 도달 여부 확인
            {
                return; // 갱신 대기 유지
            }

            refreshTimer = 0f; // 갱신 간격 타이머 초기화
            Refresh(); // 흐트러짐 게이지 갱신
        }

        public void Refresh() // 현재 흐트러짐 게이지 갱신
        {
            if (fillRect == null || string.IsNullOrWhiteSpace(boundRuntimeId)) // 게이지 구성 및 표시 대상 확인
            {
                return; // 게이지 갱신 중단
            }

            bool registered = BattleDisarrayRuntimeState.IsRegistered(boundRuntimeId); // 흐트러짐 게이지 등록 여부 확인
            gameObject.SetActive(registered); // 미등록 대상 게이지 숨김

            if (!registered) // 미등록 대상 확인
            {
                return; // 게이지 갱신 중단
            }

            bool disarrayed = BattleDisarrayRuntimeState.IsDisarrayed(boundRuntimeId); // 현재 흐트러짐 상태 확인
            float ratio = BattleDisarrayRuntimeState.GetGaugeRatio(boundRuntimeId); // 흐트러짐 게이지 비율 조회
            fillRect.anchorMax = new Vector2(ratio, 1f); // 앵커 확장 방식 채움 적용
            fillImage.color = ResolveFillColor(disarrayed, ratio); // 상태별 채움 색상 적용

            if (labelText != null) // 상태 문구 존재 확인
            {
                labelText.gameObject.SetActive(disarrayed); // 흐트러짐 중에만 상태 문구 표시
                labelText.text = disarrayed ? $"흐트러짐 {BattleDisarrayRuntimeState.GetRemainingSeconds(boundRuntimeId):0.0}s" : string.Empty; // 잔여 시간 포함 상태 문구 적용
            }
        }

        private static Color ResolveFillColor(bool disarrayed, float ratio) // 흐트러짐 상태와 누적 비율 기반 채움 색상 결정
        {
            if (disarrayed) // 흐트러짐 상태 확인
            {
                return DisarrayedColor; // 흐트러짐 붉은색 반환
            }

            if (ratio < NearFullRatio) // 임박 구간 도달 여부 확인
            {
                return FillColor; // 평상시 노란색 반환
            }

            float blend = Mathf.PingPong(Time.unscaledTime * BlinkCyclesPerSecond, 1f); // 깜빡임 보간 계수 계산
            return Color.Lerp(FillColor, NearFullColor, blend); // 임박 구간 깜빡임 색상 반환
        }

        private static Text CreateLabel(Transform parent, string name, int fontSize, Color color) // 흐트러짐 상태 문구 텍스트 생성 (RuntimeUiKit 위임)
        {
            return RuntimeUiKit.CreateText(parent, name, string.Empty, fontSize, color).Overflow().Outlined(new Color(0.02f, 0.04f, 0.08f, 0.85f), new Vector2(1.5f, -1.5f)); // 넘침 허용·외곽선 텍스트 반환
        }
    }
}
