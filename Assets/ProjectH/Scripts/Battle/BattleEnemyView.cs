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
        public BattleEnemyStats Stats { get; private set; } // 연결된 적 전투 스탯
        public BattleActor Actor => actor; // 공통 전투 액터 반환

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
            SetText(runtimeIdText, Stats.RuntimeId); // 적군 런타임 ID 적용

            if (bodyImage != null) // 적군 바디 이미지 확인
            {
                bodyImage.color = enemyColor; // 적군 기본색 적용
            }

            if (actor != null) // 적군 전투 액터 확인
            {
                actor.SetBodyColor(enemyColor); // 적군 액터 기본 바디색 적용
                actor.Initialize(BattleTeam.Enemy, Stats, transform.position); // 적군 전투 액터 초기화
            }

            Refresh(); // 적군 현재 상태 표시
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
