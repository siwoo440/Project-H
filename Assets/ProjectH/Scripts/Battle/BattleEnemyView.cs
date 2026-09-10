using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 적군 뷰 방지
    public sealed class BattleEnemyView : MonoBehaviour // 적군 전투 표시 뷰
    {
        [SerializeField] private Canvas worldCanvas; // 월드 공간 UI Canvas
        [SerializeField] private Image bodyImage; // 적군 임시 바디 이미지
        [SerializeField] private Text nameText; // 적군 표시 이름
        [SerializeField] private Text runtimeIdText; // 적군 런타임 ID
        [SerializeField] private Text hpText; // 적군 체력 수치
        [SerializeField] private Image hpFillImage; // 적군 체력 게이지
        [SerializeField] private BattleActor actor; // 공통 전투 액터
        [SerializeField] private BattleActionDebugText actionDebugText; // 머리 위 행동 텍스트
        [SerializeField] private bool showDebugInfo; // 적군 Runtime 개발 정보 표시 여부
        public BattleEnemyStats Stats { get; private set; } // 연결된 적 전투 스탯
        public BattleActor Actor => actor; // 공통 전투 액터 반환
        private BattleStatusEffectStripView statusStrip; // 적군 하단 상태이상 표시 (Day51 추가)

        public void Configure(Canvas canvas, Image body, Text displayName, Text runtimeId, Text hp, Image hpFill, BattleActor battleActor, BattleActionDebugText debugText) // 에디터 참조 설정
        {
            worldCanvas = canvas; // 월드 Canvas 연결
            bodyImage = body; // 적군 바디 연결
            nameText = displayName; // 적군 이름 연결
            runtimeIdText = runtimeId; // 적군 런타임 ID 연결
            hpText = hp; // 적군 체력 텍스트 연결
            hpFillImage = hpFill; // 적군 체력 게이지 연결
            actor = battleActor != null ? battleActor : GetComponent<BattleActor>(); // 전투 액터 조회

            if (actor == null) // 전투 액터 존재 확인
            {
                actor = gameObject.AddComponent<BattleActor>(); // 적군 전투 액터 자동 추가
            }

            actionDebugText = debugText; // 행동 디버그 텍스트 연결
            actor.ConfigureVisuals(bodyImage, actionDebugText); // 적군 액터 시각 참조 연결
        }

        public void SetWorldCamera(Camera targetCamera) // 월드 UI 카메라 연결
        {
            if (worldCanvas != null) // 월드 Canvas 확인
            {
                worldCanvas.worldCamera = targetCamera; // 적군 월드 UI 카메라 적용
            }
        }

        public void Bind(BattleEnemyStats stats) // 적군 전투 스탯 연결
        {
            UnbindHealthEvent(); // 기존 체력 이벤트 연결 해제
            Stats = stats; // 적군 전투 스탯 저장

            if (Stats == null) // 적군 전투 스탯 확인
            {
                gameObject.SetActive(false); // 잘못된 적군 뷰 숨김
                return; // 적군 데이터 연결 중단
            }

            Stats.HealthChanged += Refresh; // 체력 변경 시 적군 View 갱신 연결
            Color enemyColor = new Color(0.58f, 0.28f, 0.20f, 0.96f); // 임시 적군 기본색 생성
            SetText(nameText, Stats.DisplayName); // 적군 표시 이름 적용
            SetText(runtimeIdText, $"{Stats.RuntimeId} · {Stats.AIType.ToString().ToUpperInvariant()}"); // 적군 런타임 ID 및 AI 유형 적용
            SetDebugInfoVisible(showDebugInfo); // 적군 Runtime 개발 정보 표시 상태 적용

            if (bodyImage != null) // 적군 바디 이미지 확인
            {
                bodyImage.color = enemyColor; // 적군 기본색 적용
            }

            if (actor != null) // 적군 전투 액터 확인
            {
                actor.SetBodyColor(enemyColor); // 적군 액터 기본 바디색 적용
                actor.Initialize(BattleTeam.Enemy, Stats, transform.position); // 적군 전투 액터 초기화
            }

            EnsureStatusStrip(); // 적군 하단 상태이상 표시 준비 (Day51 추가)
            Refresh(); // 적군 현재 상태 표시
        }

        private void EnsureStatusStrip() // 적군 하단 상태이상 표시 준비 (Day51 추가)
        {
            RectTransform anchorRect = GetStatusAnchorRect(); // 상태이상 표시 부착 기준 RectTransform 조회

            if (anchorRect == null || Stats == null) // 부착 기준 및 적군 스탯 확인
            {
                return; // 상태이상 표시 준비 중단
            }

            if (statusStrip != null) // 기존 상태이상 표시 존재 확인
            {
                statusStrip.Bind(Stats.RuntimeId); // 표시 대상만 갱신
                RefreshElementChip(); // 적군 속성 칩 표시 갱신 (Day52 추가)
                return; // 중복 생성 차단
            }

            statusStrip = BattleStatusEffectStripView.AttachBelow(anchorRect, Stats.RuntimeId); // 적군 아래쪽에 상태이상 표시 부착
            RefreshElementChip(); // 적군 속성 칩 표시 갱신 (Day52 추가)
        }

        private void RefreshElementChip() // 적군 속성 칩 표시 갱신 (Day52 추가)
        {
            if (statusStrip == null || Stats == null) // 상태이상 표시 및 적군 스탯 확인
            {
                return; // 속성 칩 갱신 중단
            }

            statusStrip.ShowElementChip(BattleElementRuntimeState.GetElement(Stats.RuntimeId)); // 등록 속성 기반 속성 칩 표시
        }

        private RectTransform GetStatusAnchorRect() // 상태이상 표시 부착 기준 RectTransform 조회 (Day51 추가)
        {
            if (worldCanvas != null) // 적군 월드 Canvas 확인
            {
                RectTransform canvasRect = worldCanvas.transform as RectTransform; // 월드 Canvas RectTransform 변환

                if (canvasRect != null) // 월드 Canvas RectTransform 확인
                {
                    return canvasRect; // 적군 전체 영역 기준 반환
                }
            }

            return hpFillImage == null ? null : hpFillImage.rectTransform; // 월드 Canvas 부재 시 체력 게이지 기준 반환
        }

        public void Refresh() // 적군 전투 표시 갱신
        {
            if (Stats == null) // 적군 전투 스탯 확인
            {
                return; // 적군 표시 갱신 중단
            }

            SetText(hpText, $"{Stats.CurrentHp} / {Stats.MaxHp}"); // 적군 현재 체력 표시

            if (hpFillImage != null) // 적군 체력 게이지 확인
            {
                RectTransform fillRect = hpFillImage.rectTransform; // 적군 체력 게이지 조회
                fillRect.anchorMax = new Vector2(Stats.HealthRatio, 1f); // 적군 체력 비율 적용
                fillRect.offsetMin = Vector2.zero; // 체력 게이지 최소 오프셋 초기화
                fillRect.offsetMax = Vector2.zero; // 체력 게이지 최대 오프셋 초기화
            }
        }

        public void SetDebugInfoVisible(bool visible) // 적군 Runtime 개발 정보 표시 설정
        {
            showDebugInfo = visible; // 적군 Runtime 개발 정보 표시 상태 저장

            if (runtimeIdText != null) // 적군 Runtime ID 텍스트 확인
            {
                runtimeIdText.enabled = visible; // 적군 Runtime 개발 정보 표시 상태 적용
            }
        }

        public void ShowDefeatedPreview() // 적군 전투 불능 임시 표시
        {
            if (Stats == null) // 적군 전투 스탯 확인
            {
                return; // 전투 불능 표시 중단
            }

            SetText(nameText, $"[DOWN] {Stats.DisplayName}"); // 적군 전투 불능 이름 표시
            SetText(runtimeIdText, $"{Stats.RuntimeId} · {Stats.AIType.ToString().ToUpperInvariant()} · DOWN"); // 적군 전투 불능 상태 표시
            SetText(hpText, $"0 / {Stats.MaxHp}"); // 적군 전투 불능 체력 표시

            if (hpFillImage != null) // 적군 체력 게이지 확인
            {
                RectTransform fillRect = hpFillImage.rectTransform; // 적군 체력 게이지 조회
                fillRect.anchorMax = new Vector2(0f, 1f); // 적군 체력 게이지 0 적용
                fillRect.offsetMin = Vector2.zero; // 적군 체력 게이지 최소 오프셋 초기화
                fillRect.offsetMax = Vector2.zero; // 적군 체력 게이지 최대 오프셋 초기화
            }

            if (bodyImage != null) // 적군 바디 이미지 확인
            {
                Color defeatedColor = bodyImage.color; // 현재 적군 바디 색상 복사
                defeatedColor.a = 0.45f; // 전투 불능 투명도 적용
                bodyImage.color = defeatedColor; // 전투 불능 바디 색상 적용
            }
        }

        private void OnDestroy() // 적군 View 제거
        {
            UnbindHealthEvent(); // 체력 이벤트 연결 해제
        }

        private void UnbindHealthEvent() // 적군 체력 이벤트 안전 해제
        {
            if (Stats != null) // 기존 적군 전투 스탯 확인
            {
                Stats.HealthChanged -= Refresh; // 기존 체력 변경 이벤트 해제
            }
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
