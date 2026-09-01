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
        [SerializeField] private Image gaugeFillImage; // 궁극기 게이지 이미지
        [SerializeField] private Text healthStateText; // 체력 상태 텍스트
        [SerializeField] private Button skillButton; // 스킬 자리 버튼
        [SerializeField] private Text skillText; // 스킬 상태 텍스트
        [SerializeField] private Text ultimateText; // 궁극기 게이지 텍스트
        private float ultimateRatio; // 현재 궁극기 게이지 미리보기 비율
        public BattleStats Stats { get; private set; } // 연결된 전투 스탯
        public float UltimateRatio => ultimateRatio; // 현재 궁극기 게이지 비율 반환
        public BattleHudHealthState HealthState { get; private set; } = BattleHudHealthState.Normal; // 현재 HUD 체력 상태

        public void Configure(Text displayName, Text level, Text portrait, Text hp, Image hpFill, Image gaugeFill) // 기존 에디터 참조 설정
        {
            nameText = displayName; // 캐릭터 이름 연결
            levelText = level; // 캐릭터 레벨 연결
            portraitText = portrait; // 임시 초상화 연결
            hpText = hp; // 체력 텍스트 연결
            hpFillImage = hpFill; // HP 게이지 연결
            gaugeFillImage = gaugeFill; // 궁극기 게이지 연결
        }

        public void ConfigureExtended(Text displayName, Text level, Text portrait, Text hp, Image hpFill, Image gaugeFill, Text healthState, Button skillTarget, Text skillLabel, Text ultimateLabel) // 16일차 확장 HUD 참조 설정
        {
            Configure(displayName, level, portrait, hp, hpFill, gaugeFill); // 기존 HUD 참조 설정
            healthStateText = healthState; // 체력 상태 텍스트 연결
            skillButton = skillTarget; // 스킬 자리 버튼 연결
            skillText = skillLabel; // 스킬 상태 텍스트 연결
            ultimateText = ultimateLabel; // 궁극기 게이지 텍스트 연결
            RefreshSkillState(); // 스킬 자리 초기 상태 갱신
            RefreshUltimate(); // 궁극기 게이지 초기 상태 갱신
        }

        public void Bind(BattleStats stats) // HUD 전투 스탯 연결
        {
            UnbindHealthEvent(); // 기존 체력 이벤트 연결 해제
            Stats = stats; // 전투 스탯 저장
            ultimateRatio = 0f; // 신규 전투 궁극기 게이지 초기화

            if (Stats == null) // 전투 스탯 확인
            {
                SetVisible(false); // 빈 HUD 카드 숨김
                return; // HUD 연결 중단
            }

            Stats.HealthChanged += Refresh; // 체력 변경 시 HUD 갱신 연결
            SetVisible(true); // HUD 카드 표시
            SetText(nameText, Stats.DisplayName); // 캐릭터 이름 표시
            SetText(levelText, $"Lv.{Stats.Level}"); // 캐릭터 레벨 표시
            SetText(portraitText, Stats.DisplayName); // 임시 초상화 이름 표시
            Refresh(); // 현재 HUD 상태 표시
            RefreshUltimate(); // 현재 궁극기 게이지 표시
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

            HealthState = BattleHudHealthStateEvaluator.Evaluate(Stats.CurrentHp, Stats.MaxHp); // 현재 HUD 체력 상태 계산
            SetText(hpText, Stats.IsAlive ? $"{Stats.CurrentHp}/{Stats.MaxHp}" : $"DOWN · 0/{Stats.MaxHp}"); // 현재 체력 또는 DOWN 상태 표시
            SetText(healthStateText, BattleHudHealthStateEvaluator.GetLabel(HealthState)); // 현재 체력 상태 문구 표시
            SetFill(hpFillImage, Stats.HealthRatio); // HP 게이지 비율 적용
            RefreshSkillState(); // 현재 스킬 자리 상태 갱신
        }

        public void SetUltimatePreview(float ratio) // 궁극기 게이지 UI 미리보기 설정
        {
            ultimateRatio = Mathf.Clamp01(ratio); // 궁극기 게이지 범위 보정
            RefreshUltimate(); // 궁극기 게이지 표시 갱신
        }

        private void RefreshSkillState() // 스킬 자리 표시 갱신
        {
            bool alive = Stats == null || Stats.IsAlive; // 현재 캐릭터 생존 상태 계산

            if (skillButton != null) // 스킬 자리 버튼 확인
            {
                skillButton.interactable = false; // 16일차 스킬 입력 잠금 유지
            }

            if (skillText != null) // 스킬 자리 텍스트 확인
            {
                skillText.text = alive ? "SKILL\nLOCKED" : "DOWN"; // 17일차 연결 전 스킬 상태 표시
            }
        }

        private void RefreshUltimate() // 궁극기 게이지 표시 갱신
        {
            SetFill(gaugeFillImage, ultimateRatio); // 궁극기 게이지 비율 적용

            if (ultimateText != null) // 궁극기 게이지 텍스트 확인
            {
                int percent = Mathf.RoundToInt(ultimateRatio * 100f); // 궁극기 게이지 퍼센트 계산
                ultimateText.text = $"ULT {percent}%"; // 궁극기 게이지 퍼센트 표시
            }
        }

        private void OnDestroy() // HUD 카드 제거
        {
            UnbindHealthEvent(); // 체력 이벤트 연결 해제
        }

        private void UnbindHealthEvent() // HUD 체력 이벤트 안전 해제
        {
            if (Stats != null) // 기존 전투 스탯 확인
            {
                Stats.HealthChanged -= Refresh; // 기존 체력 변경 이벤트 해제
            }
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
