using ProjectH.Data; // 캐릭터 역할 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 전투 유닛 뷰 방지
    public sealed class BattleUnitView : MonoBehaviour // 전장 캐릭터 표시 뷰
    {
        [SerializeField] private Canvas worldCanvas; // 월드 공간 UI Canvas
        [SerializeField] private Image bodyImage; // 캐릭터 임시 바디 이미지
        [SerializeField] private Text characterText; // 캐릭터 이름 표시
        [SerializeField] private Text runtimeIdText; // 런타임 ID 표시
        [SerializeField] private Text roleText; // 캐릭터 역할 표시
        [SerializeField] private Text hpText; // 체력 수치 표시
        [SerializeField] private Image hpFillImage; // 체력 게이지 이미지
        [SerializeField] private BattleActor actor; // 공통 전투 액터
        [SerializeField] private BattleActionDebugText actionDebugText; // 머리 위 행동 텍스트
        public BattleStats Stats { get; private set; } // 연결된 전투 스탯
        public BattleActor Actor => actor; // 공통 전투 액터 반환

        public void Configure(Canvas canvas, Image body, Text character, Text runtimeId, Text role, Text hp, Image hpFill) // 11일차 호환 에디터 참조 설정
        {
            Configure(canvas, body, character, runtimeId, role, hp, hpFill, null, null); // 확장 참조 설정 호출
        }

        public void Configure(Canvas canvas, Image body, Text character, Text runtimeId, Text role, Text hp, Image hpFill, BattleActor battleActor, BattleActionDebugText debugText) // 12일차 확장 에디터 참조 설정
        {
            worldCanvas = canvas; // 월드 Canvas 연결
            bodyImage = body; // 임시 바디 연결
            characterText = character; // 캐릭터 이름 연결
            runtimeIdText = runtimeId; // 런타임 ID 연결
            roleText = role; // 역할 텍스트 연결
            hpText = hp; // 체력 텍스트 연결
            hpFillImage = hpFill; // 체력 게이지 연결
            actor = battleActor != null ? battleActor : GetComponent<BattleActor>(); // 기존 전투 액터 조회

            if (actor == null) // 전투 액터 존재 확인
            {
                actor = gameObject.AddComponent<BattleActor>(); // 호환 전투 액터 자동 추가
            }

            actionDebugText = debugText; // 행동 디버그 텍스트 연결
            actor.ConfigureVisuals(bodyImage, actionDebugText); // 전투 액터 시각 참조 연결
        }

        public void SetWorldCamera(Camera targetCamera) // 월드 UI 카메라 연결
        {
            if (worldCanvas != null) // 월드 Canvas 확인
            {
                worldCanvas.worldCamera = targetCamera; // 월드 UI 카메라 적용
            }
        }

        public void Bind(BattleStats stats) // 전투 스탯 연결
        {
            UnbindHealthEvent(); // 기존 체력 이벤트 연결 해제
            Stats = stats; // 전투 스탯 저장

            if (Stats == null) // 전투 스탯 확인
            {
                gameObject.SetActive(false); // 잘못된 전투 유닛 숨김
                return; // 데이터 연결 중단
            }

            Stats.HealthChanged += Refresh; // 체력 변경 시 전장 View 갱신 연결
            Color roleColor = GetRoleColor(Stats.Position); // 역할 기반 캐릭터 색상 계산
            SetText(characterText, Stats.DisplayName); // 캐릭터 이름 표시
            SetText(runtimeIdText, Stats.RuntimeId); // 런타임 ID 표시
            SetText(roleText, GetRoleLabel(Stats.Position)); // 역할 표시

            if (bodyImage != null) // 바디 이미지 확인
            {
                bodyImage.color = roleColor; // 역할 기반 임시 캐릭터 색상 적용
            }

            if (actor != null) // 전투 액터 확인
            {
                actor.SetBodyColor(roleColor); // 전투 액터 기본 바디색 적용
                actor.Initialize(BattleTeam.Ally, Stats, transform.position); // 아군 전투 액터 초기화
            }

            Refresh(); // 현재 전투 상태 표시
        }

        public void Refresh() // 전투 유닛 표시 갱신
        {
            if (Stats == null) // 전투 스탯 확인
            {
                return; // 표시 갱신 중단
            }

            SetText(hpText, $"{Stats.CurrentHp} / {Stats.MaxHp}"); // 현재 체력 표시

            if (hpFillImage != null) // 체력 게이지 확인
            {
                RectTransform fillRect = hpFillImage.rectTransform; // 체력 게이지 RectTransform 조회
                fillRect.anchorMax = new Vector2(Stats.HealthRatio, 1f); // 현재 체력 비율 적용
                fillRect.offsetMin = Vector2.zero; // 체력 게이지 최소 오프셋 초기화
                fillRect.offsetMax = Vector2.zero; // 체력 게이지 최대 오프셋 초기화
            }
        }

        public void ShowDefeatedPreview() // 아군 전투 불능 임시 표시
        {
            if (Stats == null) // 아군 전투 스탯 확인
            {
                return; // 아군 전투 불능 표시 중단
            }

            SetText(characterText, $"[DOWN] {Stats.DisplayName}"); // 아군 전투 불능 이름 표시
            SetText(runtimeIdText, $"{Stats.RuntimeId} · DOWN"); // 아군 전투 불능 상태 표시
            SetText(hpText, $"0 / {Stats.MaxHp}"); // 아군 전투 불능 체력 표시

            if (hpFillImage != null) // 아군 체력 게이지 확인
            {
                RectTransform fillRect = hpFillImage.rectTransform; // 아군 체력 게이지 조회
                fillRect.anchorMax = new Vector2(0f, 1f); // 아군 체력 게이지 0 적용
                fillRect.offsetMin = Vector2.zero; // 아군 체력 게이지 최소 오프셋 초기화
                fillRect.offsetMax = Vector2.zero; // 아군 체력 게이지 최대 오프셋 초기화
            }

            if (bodyImage != null) // 아군 바디 이미지 확인
            {
                Color defeatedColor = bodyImage.color; // 현재 아군 바디 색상 복사
                defeatedColor.a = 0.45f; // 전투 불능 투명도 적용
                bodyImage.color = defeatedColor; // 전투 불능 바디 색상 적용
            }
        }

        public void ShowDebugAction(BattleActionKind actionKind) // 전투 행동 디버그 텍스트 표시
        {
            actor?.ShowAction(actionKind); // 공통 전투 액터 행동 텍스트 호출
        }

        private void OnDestroy() // 전투 유닛 View 제거
        {
            UnbindHealthEvent(); // 체력 이벤트 연결 해제
        }

        private void UnbindHealthEvent() // 체력 이벤트 안전 해제
        {
            if (Stats != null) // 기존 전투 스탯 확인
            {
                Stats.HealthChanged -= Refresh; // 기존 체력 변경 이벤트 해제
            }
        }

        private static Color GetRoleColor(BattlePosition position) // 역할 기반 캐릭터 색상 반환
        {
            switch (position) // 역할 종류 분기
            {
                case BattlePosition.Tank: // 탱커 역할 처리
                    return new Color(0.28f, 0.50f, 0.82f, 0.95f); // 탱커 파랑 반환
                case BattlePosition.Healer: // 힐러 역할 처리
                    return new Color(0.30f, 0.70f, 0.48f, 0.95f); // 힐러 초록 반환
                default: // 딜러 역할 처리
                    return new Color(0.80f, 0.38f, 0.46f, 0.95f); // 딜러 붉은색 반환
            }
        }

        private static string GetRoleLabel(BattlePosition position) // 역할 표시 이름 반환
        {
            switch (position) // 역할 종류 분기
            {
                case BattlePosition.Tank: // 탱커 역할 처리
                    return "TANK"; // 탱커 라벨 반환
                case BattlePosition.Healer: // 힐러 역할 처리
                    return "HEALER"; // 힐러 라벨 반환
                default: // 딜러 역할 처리
                    return "DEALER"; // 딜러 라벨 반환
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
