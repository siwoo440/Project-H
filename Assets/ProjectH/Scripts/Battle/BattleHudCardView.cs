using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 HUD 카드 방지
    public sealed class BattleHudCardView : MonoBehaviour // 하단 캐릭터 HUD 카드
    {
        [SerializeField] private Text nameText; // 캐릭터 이름 텍스트
        [SerializeField] private Text levelText; // 캐릭터 레벨 텍스트
        [SerializeField] private Text portraitText; // 임시 초상화 텍스트
        [SerializeField] private Text hpText; // 현재 체력 텍스트
        [SerializeField] private Image hpFillImage; // HP 게이지 이미지
        [SerializeField] private Image gaugeFillImage; // 스킬 게이지 자리 이미지
        public BattleStats Stats { get; private set; } // 연결된 전투 스탯

        public void Configure(Text displayName, Text level, Text portrait, Text hp, Image hpFill, Image gaugeFill) // 에디터 참조 설정
        {
            nameText = displayName; // 캐릭터 이름 연결
            levelText = level; // 캐릭터 레벨 연결
            portraitText = portrait; // 임시 초상화 연결
            hpText = hp; // 체력 텍스트 연결
            hpFillImage = hpFill; // HP 게이지 연결
            gaugeFillImage = gaugeFill; // 스킬 게이지 연결
        }

        public void Bind(BattleStats stats) // HUD 전투 스탯 연결
        {
            Stats = stats; // 전투 스탯 저장

            if (Stats == null) // 전투 스탯 확인
            {
                SetVisible(false); // 빈 HUD 카드 숨김
                return; // HUD 연결 중단
            }

            SetVisible(true); // HUD 카드 표시
            SetText(nameText, Stats.DisplayName); // 캐릭터 이름 표시
            SetText(levelText, $"Lv.{Stats.Level}"); // 캐릭터 레벨 표시
            SetText(portraitText, Stats.DisplayName); // 임시 초상화 이름 표시
            Refresh(); // 현재 HUD 상태 표시
        }

        public void SetVisible(bool visible) // HUD 카드 표시 상태 설정
        {
            gameObject.SetActive(visible); // HUD 카드 활성 상태 적용
        }

        public void Refresh() // HUD 현재 상태 갱신
        {
            if (Stats == null) // 전투 스탯 확인
            {
                return; // HUD 갱신 중단
            }

            SetText(hpText, $"{Stats.CurrentHp}/{Stats.MaxHp}"); // 현재 체력 수치 표시
            SetFill(hpFillImage, Stats.HealthRatio); // HP 게이지 비율 적용
            SetFill(gaugeFillImage, 0f); // 11일차 스킬 게이지 초기 자리 표시
        }

        private static void SetFill(Image target, float ratio) // 가로 게이지 비율 설정
        {
            if (target == null) // 게이지 이미지 확인
            {
                return; // 게이지 설정 중단
            }

            RectTransform rect = target.rectTransform; // 게이지 RectTransform 조회
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f); // 게이지 비율 적용
            rect.offsetMin = Vector2.zero; // 게이지 최소 오프셋 초기화
            rect.offsetMax = Vector2.zero; // 게이지 최대 오프셋 초기화
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
