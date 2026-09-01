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
        public BattleStats Stats { get; private set; } // 연결된 전투 스탯

        public void Configure(Canvas canvas, Image body, Text character, Text runtimeId, Text role, Text hp, Image hpFill) // 에디터 참조 설정
        {
            worldCanvas = canvas; // 월드 Canvas 연결
            bodyImage = body; // 임시 바디 연결
            characterText = character; // 캐릭터 이름 연결
            runtimeIdText = runtimeId; // 런타임 ID 연결
            roleText = role; // 역할 텍스트 연결
            hpText = hp; // 체력 텍스트 연결
            hpFillImage = hpFill; // 체력 게이지 연결
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
            Stats = stats; // 전투 스탯 저장

            if (Stats == null) // 전투 스탯 확인
            {
                gameObject.SetActive(false); // 잘못된 전투 유닛 숨김
                return; // 데이터 연결 중단
            }

            SetText(characterText, Stats.DisplayName); // 캐릭터 이름 표시
            SetText(runtimeIdText, Stats.RuntimeId); // 런타임 ID 표시
            SetText(roleText, GetRoleLabel(Stats.Position)); // 역할 표시

            if (bodyImage != null) // 바디 이미지 확인
            {
                bodyImage.color = GetRoleColor(Stats.Position); // 역할 기반 임시 캐릭터 색상 적용
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

        private static Color GetRoleColor(BattlePosition position) // 역할 기반 캐릭터 색상 반환
        {
            switch (position) // 캐릭터 역할 분기
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
            switch (position) // 캐릭터 역할 분기
            {
                case BattlePosition.Tank: // 탱커 역할 처리
                    return "TANK"; // 탱커 표시 반환
                case BattlePosition.Healer: // 힐러 역할 처리
                    return "HEALER"; // 힐러 표시 반환
                default: // 딜러 역할 처리
                    return "DEALER"; // 딜러 표시 반환
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
