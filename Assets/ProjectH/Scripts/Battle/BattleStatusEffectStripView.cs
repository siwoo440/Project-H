using System.Collections.Generic; // 목록 자료형
using ProjectH.UI; // Runtime UI 생성 공용 헬퍼 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 상태이상 표시 방지
    public sealed class BattleStatusEffectStripView : MonoBehaviour // 상태이상 칩 가로 표시 뷰 (Day51 신규, 런타임 생성 전용)
    {
        public const int MaxVisibleChips = 5; // 동시에 표시하는 최대 칩 수
        private const float ChipSizePixels = 26f; // 칩 한 변 크기(px)
        private const float ChipSpacingPixels = 3f; // 칩 사이 간격(px)
        private const float TimerBarHeightPixels = 3f; // 잔여 시간 바 높이(px)
        private const float RefreshIntervalSeconds = 0.1f; // 상태이상 표시 갱신 간격
        private const float TimerBarReferenceSeconds = 10f; // 잔여 시간 바 기준 최대 지속시간
        private const float ElementChipOffsetPixels = 46f; // 속성 칩 좌측 고정 오프셋(px) (Day52 추가)

        private readonly List<BattleStatusEffectSnapshot> snapshotBuffer = new List<BattleStatusEffectSnapshot>(); // 상태이상 수집 재사용 버퍼 (GC 할당 방지)
        private readonly List<ChipRuntime> chips = new List<ChipRuntime>(); // 생성 칩 런타임 목록
        private string boundRuntimeId; // 표시 대상 Runtime ID
        private Text overflowText; // 초과 상태이상 표시 텍스트
        private float refreshTimer; // 갱신 간격 누적 타이머
        private ChipRuntime elementChip; // 고정 속성 칩 런타임 상태 (Day52 추가)

        private sealed class ChipRuntime // 단일 상태이상 칩 런타임 상태
        {
            public GameObject Root; // 칩 루트 객체
            public Image Background; // 칩 배경 이미지
            public Text Label; // 칩 축약 문구 텍스트
            public RectTransform TimerFill; // 잔여 시간 바 채움 RectTransform
        }

        public static BattleStatusEffectStripView AttachAbove(RectTransform anchorTarget, string runtimeId) // 지정 대상 위쪽에 상태이상 표시 부착 (아군 초상화 위 배치용)
        {
            return Attach(anchorTarget, runtimeId, true); // 위쪽 배치 상태이상 표시 반환
        }

        public static BattleStatusEffectStripView AttachBelow(RectTransform anchorTarget, string runtimeId) // 지정 대상 아래쪽에 상태이상 표시 부착 (적군 하단 배치용)
        {
            return Attach(anchorTarget, runtimeId, false); // 아래쪽 배치 상태이상 표시 반환
        }

        private static BattleStatusEffectStripView Attach(RectTransform anchorTarget, string runtimeId, bool above) // 상태이상 표시 생성 및 부착
        {
            if (anchorTarget == null) // 부착 기준 대상 확인
            {
                return null; // 부착 대상 없음 반환
            }

            BattleStatusEffectStripView existing = FindExisting(anchorTarget); // 기존 생성 상태이상 표시 조회

            if (existing != null) // 기존 표시 존재 확인
            {
                existing.Bind(runtimeId); // 표시 대상만 갱신
                return existing; // 기존 상태이상 표시 반환
            }

            GameObject stripObject = new GameObject("StatusEffectStrip", typeof(RectTransform), typeof(BattleStatusEffectStripView)); // 상태이상 표시 루트 생성
            stripObject.transform.SetParent(anchorTarget, false); // 기준 대상 자식으로 연결
            RectTransform stripRect = stripObject.GetComponent<RectTransform>(); // 상태이상 표시 RectTransform 조회
            stripRect.anchorMin = new Vector2(0f, above ? 1f : 0f); // 기준 대상 위 또는 아래 최소 앵커 설정
            stripRect.anchorMax = new Vector2(1f, above ? 1f : 0f); // 기준 대상 위 또는 아래 최대 앵커 설정
            stripRect.pivot = new Vector2(0.5f, above ? 0f : 1f); // 기준 대상 바깥 방향 피벗 설정
            stripRect.sizeDelta = new Vector2(0f, ChipSizePixels); // 칩 높이 기준 표시 높이 설정
            stripRect.anchoredPosition = new Vector2(0f, above ? ChipSpacingPixels : -ChipSpacingPixels); // 기준 대상과 간격 적용

            BattleStatusEffectStripView view = stripObject.GetComponent<BattleStatusEffectStripView>(); // 상태이상 표시 컴포넌트 조회
            view.BuildChips(); // 칩 및 초과 표시 생성
            view.Bind(runtimeId); // 표시 대상 연결
            return view; // 생성 상태이상 표시 반환
        }

        private static BattleStatusEffectStripView FindExisting(RectTransform anchorTarget) // 기준 대상의 기존 상태이상 표시 조회
        {
            for (int index = 0; index < anchorTarget.childCount; index++) // 기준 대상 자식 순회
            {
                BattleStatusEffectStripView candidate = anchorTarget.GetChild(index).GetComponent<BattleStatusEffectStripView>(); // 자식 상태이상 표시 조회

                if (candidate != null) // 상태이상 표시 존재 확인
                {
                    return candidate; // 기존 상태이상 표시 반환
                }
            }

            return null; // 기존 상태이상 표시 없음 반환
        }

        public void ShowElementChip(BattleElement element) // 고정 속성 칩 표시 (Day52 추가, 상태이상과 같은 디자인 재사용)
        {
            if (elementChip == null) // 속성 칩 존재 확인
            {
                elementChip = CreateChip(MaxVisibleChips); // 상태이상 칩과 동일한 규격으로 속성 칩 생성
                elementChip.Background.rectTransform.pivot = new Vector2(1f, 0.5f); // 속성 칩 우측 기준 피벗 설정 (상태이상 칩 왼쪽에 고정 배치)
            }

            bool visible = element != BattleElement.None; // 무속성 제외 표시 여부 판정
            elementChip.Root.SetActive(visible); // 속성 칩 표시 여부 적용

            if (!visible) // 무속성 여부 확인
            {
                return; // 속성 칩 갱신 중단
            }

            elementChip.Background.color = BattleElementAffinityTable.GetColor(element); // 속성 색상 적용
            elementChip.Label.text = BattleElementAffinityTable.GetShortLabel(element); // 속성 축약 문구 적용
            elementChip.TimerFill.anchorMax = new Vector2(1f, 1f); // 고정 표시 항목이므로 잔여 시간 바 가득 채움
            elementChip.Background.rectTransform.anchoredPosition = new Vector2(-ElementChipOffsetPixels, 0f); // 상태이상 칩 왼쪽 고정 위치 적용
        }

        public void Bind(string runtimeId) // 상태이상 표시 대상 연결
        {
            boundRuntimeId = runtimeId ?? string.Empty; // 표시 대상 Runtime ID 저장
            refreshTimer = RefreshIntervalSeconds; // 다음 Update에서 즉시 갱신되도록 타이머 설정
        }

        private void BuildChips() // 상태이상 칩 및 초과 표시 생성
        {
            for (int index = 0; index < MaxVisibleChips; index++) // 최대 표시 개수만큼 칩 생성
            {
                chips.Add(CreateChip(index)); // 칩 생성 후 목록 등록
            }

            overflowText = CreateLabel(transform, "OverflowText", 14, new Color(0.92f, 0.94f, 0.98f, 0.90f)); // 초과 상태이상 표시 텍스트 생성
            RectTransform overflowRect = overflowText.rectTransform; // 초과 표시 RectTransform 조회
            overflowRect.anchorMin = new Vector2(0.5f, 0.5f); // 초과 표시 최소 앵커 설정
            overflowRect.anchorMax = new Vector2(0.5f, 0.5f); // 초과 표시 최대 앵커 설정
            overflowRect.pivot = new Vector2(0f, 0.5f); // 초과 표시 좌측 기준 피벗 설정
            overflowRect.sizeDelta = new Vector2(30f, ChipSizePixels); // 초과 표시 크기 설정
            overflowText.gameObject.SetActive(false); // 초과 상태이상 없을 때 숨김
        }

        private ChipRuntime CreateChip(int index) // 단일 상태이상 칩 생성
        {
            Image background = RuntimeUiKit.CreateImage(transform, $"Chip_{index}", Color.white); // 칩 배경 이미지 생성
            background.raycastTarget = false; // 칩 클릭 차단 비활성화 (전투 입력 방해 방지)
            RectTransform backgroundRect = background.rectTransform; // 칩 배경 RectTransform 조회
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f); // 칩 최소 앵커 중앙 설정
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f); // 칩 최대 앵커 중앙 설정
            backgroundRect.pivot = new Vector2(0f, 0.5f); // 칩 좌측 기준 피벗 설정 (가로 나열 계산 단순화)
            backgroundRect.sizeDelta = new Vector2(ChipSizePixels, ChipSizePixels); // 칩 크기 설정

            Text label = CreateLabel(background.transform, "Label", 15, new Color(0.06f, 0.07f, 0.10f, 1f)); // 칩 축약 문구 생성
            RuntimeUiKit.Stretch(label.rectTransform); // 칩 문구 전체 확장

            Image timerBack = RuntimeUiKit.CreateImage(background.transform, "TimerBack", new Color(0.05f, 0.06f, 0.09f, 0.55f)); // 잔여 시간 바 배경 생성
            timerBack.raycastTarget = false; // 잔여 시간 바 배경 클릭 차단 비활성화
            RectTransform timerBackRect = timerBack.rectTransform; // 잔여 시간 바 배경 RectTransform 조회
            timerBackRect.anchorMin = new Vector2(0f, 0f); // 잔여 시간 바 배경 최소 앵커 하단 설정
            timerBackRect.anchorMax = new Vector2(1f, 0f); // 잔여 시간 바 배경 최대 앵커 하단 설정
            timerBackRect.pivot = new Vector2(0.5f, 0f); // 잔여 시간 바 배경 하단 피벗 설정
            timerBackRect.sizeDelta = new Vector2(0f, TimerBarHeightPixels); // 잔여 시간 바 배경 높이 설정
            timerBackRect.anchoredPosition = Vector2.zero; // 잔여 시간 바 배경 위치 초기화

            Image timerFill = RuntimeUiKit.CreateImage(timerBack.transform, "TimerFill", new Color(0.96f, 0.97f, 1f, 0.92f)); // 잔여 시간 바 채움 생성
            timerFill.raycastTarget = false; // 잔여 시간 바 채움 클릭 차단 비활성화
            RectTransform timerFillRect = timerFill.rectTransform; // 잔여 시간 바 채움 RectTransform 조회
            timerFillRect.anchorMin = Vector2.zero; // 채움 최소 앵커 좌하단 설정
            timerFillRect.anchorMax = new Vector2(1f, 1f); // 채움 최대 앵커 전체 설정
            timerFillRect.offsetMin = Vector2.zero; // 채움 최소 오프셋 초기화
            timerFillRect.offsetMax = Vector2.zero; // 채움 최대 오프셋 초기화

            background.gameObject.SetActive(false); // 상태이상 없을 때 칩 숨김

            return new ChipRuntime // 칩 런타임 상태 생성
            {
                Root = background.gameObject, // 칩 루트 객체 저장
                Background = background, // 칩 배경 이미지 저장
                Label = label, // 칩 문구 텍스트 저장
                TimerFill = timerFillRect // 잔여 시간 바 채움 저장
            };
        }

        private void Update() // 상태이상 표시 주기 갱신
        {
            refreshTimer += Time.unscaledDeltaTime; // 갱신 간격 누적 (전투 일시정지 중에도 표시 유지)

            if (refreshTimer < RefreshIntervalSeconds) // 갱신 시각 도달 여부 확인
            {
                return; // 갱신 대기 유지
            }

            refreshTimer = 0f; // 갱신 간격 타이머 초기화
            Refresh(); // 상태이상 표시 갱신
        }

        public void Refresh() // 현재 상태이상 표시 갱신
        {
            BattleSkillRuntimeState.CollectStatusEffects(boundRuntimeId, snapshotBuffer); // 대상 활성 상태이상 수집
            float cursorX = -GetTotalWidth(Mathf.Min(snapshotBuffer.Count, MaxVisibleChips)) * 0.5f; // 전체 폭 기준 좌측 시작 위치 계산 (가운데 정렬)

            for (int index = 0; index < chips.Count; index++) // 생성 칩 목록 순회
            {
                ChipRuntime chip = chips[index]; // 현재 칩 조회

                if (index >= snapshotBuffer.Count) // 표시할 상태이상 존재 확인
                {
                    chip.Root.SetActive(false); // 여분 칩 숨김
                    continue; // 다음 칩 처리
                }

                BattleStatusEffectSnapshot snapshot = snapshotBuffer[index]; // 현재 상태이상 스냅샷 조회
                chip.Root.SetActive(true); // 칩 표시
                chip.Background.color = BattleStatusEffectCatalog.GetColor(snapshot.Id); // 상태이상 분류 색상 적용
                chip.Label.text = snapshot.StackCount > 1 ? $"{BattleStatusEffectCatalog.GetShortLabel(snapshot.Id)}{snapshot.StackCount}" : BattleStatusEffectCatalog.GetShortLabel(snapshot.Id); // 중첩 수 포함 축약 문구 적용
                chip.TimerFill.anchorMax = new Vector2(GetTimerRatio(snapshot), 1f); // 앵커 확장 방식 잔여 시간 바 적용
                chip.Background.rectTransform.anchoredPosition = new Vector2(cursorX, 0f); // 가로 나열 위치 적용
                cursorX += ChipSizePixels + ChipSpacingPixels; // 다음 칩 위치 이동
            }

            RefreshOverflow(cursorX); // 초과 상태이상 표시 갱신
        }

        private void RefreshOverflow(float cursorX) // 초과 상태이상 표시 갱신
        {
            if (overflowText == null) // 초과 표시 존재 확인
            {
                return; // 초과 표시 갱신 중단
            }

            int overflow = snapshotBuffer.Count - MaxVisibleChips; // 표시 초과 상태이상 수 계산

            if (overflow <= 0) // 초과 상태이상 존재 확인
            {
                overflowText.gameObject.SetActive(false); // 초과 표시 숨김
                return; // 초과 표시 갱신 완료
            }

            overflowText.text = $"+{overflow}"; // 초과 상태이상 수 문구 적용
            overflowText.rectTransform.anchoredPosition = new Vector2(cursorX, 0f); // 마지막 칩 오른쪽 위치 적용
            overflowText.gameObject.SetActive(true); // 초과 표시 노출
        }

        private static float GetTotalWidth(int visibleCount) // 표시 칩 전체 가로 폭 계산
        {
            if (visibleCount <= 0) // 표시 칩 존재 확인
            {
                return 0f; // 빈 표시 폭 0 반환
            }

            return (visibleCount * ChipSizePixels) + ((visibleCount - 1) * ChipSpacingPixels); // 칩 크기와 간격 합계 반환
        }

        private static float GetTimerRatio(BattleStatusEffectSnapshot snapshot) // 잔여 시간 바 비율 계산
        {
            if (snapshot.IsPermanent) // 전투 종료까지 유지되는 효과 확인
            {
                return 1f; // 무한 지속 효과 가득 찬 바 반환
            }

            return Mathf.Clamp01(snapshot.RemainingSeconds / TimerBarReferenceSeconds); // 기준 지속시간 대비 잔여 비율 반환
        }

        private static Text CreateLabel(Transform parent, string name, int fontSize, Color color) // 공통 상태이상 표시 텍스트 생성
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // UI 텍스트 객체 생성
            textObject.transform.SetParent(parent, false); // UI 텍스트 부모 연결
            Text text = textObject.GetComponent<Text>(); // UI Text 컴포넌트 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.fontSize = fontSize; // UI 텍스트 크기 적용
            text.fontStyle = FontStyle.Bold; // UI 텍스트 굵기 적용
            text.color = color; // UI 텍스트 색상 적용
            text.alignment = TextAnchor.MiddleCenter; // UI 텍스트 중앙 정렬
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 좁은 영역에서도 문구 유지
            text.verticalOverflow = VerticalWrapMode.Overflow; // 세로 잘림 방지
            text.raycastTarget = false; // UI 텍스트 입력 비활성화
            return text; // 생성 UI 텍스트 반환
        }
    }
}
