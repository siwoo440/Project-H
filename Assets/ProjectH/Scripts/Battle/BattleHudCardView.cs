using ProjectH.Battle.Rhythm; // 궁극기 리듬 챌린지 기능 (Day49)
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
        private float ultimateRatio; // 현재 궁극기 게이지 비율
        private Button ultimateButton; // 궁극기 텍스트 Runtime 입력 버튼
        private Button portraitButton; // 초상화 Runtime 궁극기 입력 버튼 (Day49 추가)
        private Image portraitImage; // 초상화 배경 이미지 참조 (Day49 추가)
        [SerializeField] private Outline portraitSelectionOutline; // 선택 캐릭터 초상화 윤곽 효과
        public BattleStats Stats { get; private set; } // 연결된 전투 스탯
        public float UltimateRatio => ultimateRatio; // 현재 궁극기 게이지 비율 반환
        public bool IsUltimateReady => Stats != null && Stats.IsAlive && ultimateRatio >= 1f; // 생존 캐릭터 궁극기 Ready 상태 반환
        public bool IsUltimateButtonInteractable => ultimateButton != null && ultimateButton.interactable; // 궁극기 입력 버튼 활성 상태 반환
        public bool IsSelectionHighlighted => portraitSelectionOutline != null && portraitSelectionOutline.enabled; // HUD 초상화 선택 윤곽 상태 반환
        public BattleHudHealthState HealthState { get; private set; } = BattleHudHealthState.Normal; // 현재 HUD 체력 상태

        public void Configure(Text displayName, Text level, Text portrait, Text hp, Image hpFill, Image gaugeFill) // 기존 에디터 참조 설정
        {
            nameText = displayName; // 캐릭터 이름 연결
            levelText = level; // 캐릭터 레벨 연결
            portraitText = portrait; // 임시 초상화 연결
            EnsurePortraitSelectionOutline(); // 초상화 선택 윤곽 효과 준비
            EnsurePortraitButton(); // 초상화 궁극기 입력 버튼 준비 (Day49 추가)
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
            EnsureUltimateButton(); // 궁극기 텍스트 Runtime 버튼 연결
            RefreshSkillState(); // 스킬 자리 초기 상태 갱신
            RefreshUltimate(); // 궁극기 게이지 초기 상태 갱신
        }

        public void Bind(BattleStats stats) // HUD 전투 스탯 연결
        {
            UnbindRuntimeEvents(); // 기존 Runtime 이벤트 연결 해제
            SetSelectionHighlighted(false); // 이전 캐릭터 초상화 선택 윤곽 해제
            Stats = stats; // 전투 스탯 저장
            ultimateRatio = 0f; // 신규 연결 궁극기 게이지 초기화

            if (Stats == null) // 전투 스탯 확인
            {
                SetVisible(false); // 빈 HUD 카드 숨김
                return; // HUD 연결 중단
            }

            EnsureUltimateButton(); // 현재 HUD 궁극기 입력 버튼 준비
            Stats.HealthChanged += Refresh; // 체력 변경 시 HUD 갱신 연결
            BattleUltimateGaugeRuntimeState.GaugeChanged += HandleUltimateGaugeChanged; // 궁극기 게이지 변경 이벤트 연결
            BattleSelectionRuntimeState.SelectionChanged += HandleSelectionChanged; // 캐릭터 선택 변경 이벤트 연결
            ultimateRatio = BattleUltimateGaugeRuntimeState.GetGaugeRatio(Stats.CharacterId); // 현재 캐릭터 Runtime 게이지 비율 동기화
            SetVisible(true); // HUD 카드 표시
            SetText(nameText, Stats.DisplayName); // 캐릭터 이름 표시
            SetText(levelText, $"Lv.{Stats.Level}"); // 캐릭터 레벨 표시
            SetText(portraitText, Stats.DisplayName); // 임시 초상화 이름 표시
            EnsurePortraitSelectionOutline(); // 현재 초상화 선택 윤곽 효과 준비
            EnsurePortraitButton(); // 현재 초상화 궁극기 입력 버튼 준비 (Day49 추가)
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

            HealthState = BattleHudHealthStateEvaluator.Evaluate(Stats.CurrentHp, Stats.MaxHp); // 현재 HUD 체력 상태 계산
            SetText(hpText, Stats.IsAlive ? $"{Stats.CurrentHp}/{Stats.MaxHp}" : $"DOWN · 0/{Stats.MaxHp}"); // 현재 체력 또는 DOWN 상태 표시
            SetText(healthStateText, BattleHudHealthStateEvaluator.GetLabel(HealthState)); // 현재 체력 상태 문구 표시
            SetFill(hpFillImage, Stats.HealthRatio); // HP 게이지 비율 적용
            RefreshSkillState(); // 현재 스킬 자리 상태 갱신
            RefreshUltimate(); // 생존 상태 기반 궁극기 Ready 표시 갱신
            RefreshSelectionHighlight(); // 생존 및 선택 상태 기반 초상화 윤곽 갱신
        }

        public void SetSelectionHighlighted(bool highlighted) // HUD 초상화 선택 윤곽 표시 설정
        {
            EnsurePortraitSelectionOutline(); // 초상화 선택 윤곽 효과 준비

            if (portraitSelectionOutline != null) // 초상화 선택 윤곽 효과 존재 확인
            {
                portraitSelectionOutline.enabled = highlighted; // 선택 여부 기반 초상화 윤곽 활성 상태 적용
            }
        }

        public bool TryUseUltimate() // 현재 HUD 캐릭터 궁극기 사용 시도
        {
            if (!IsUltimateReady || Stats == null) // 생존 및 Ready 상태 확인
            {
                return false; // 사용 불가 상태 실행 차단
            }

            BattleUltimateExecutionResult result = BattleUltimateExecutor.TryExecute(Stats.CharacterId); // 현재 전투 Registry 기반 궁극기 실행

            if (!result.Succeeded) // 궁극기 실행 실패 여부 확인
            {
                Debug.LogWarning($"[Project H][ULTIMATE] {Stats.CharacterId}, {result.Message}"); // 궁극기 사용 실패 로그 출력
                return false; // 사용 실패 반환
            }

            UltimateRhythmChallengeView.Show(result.UltimateName, HandleRhythmChallengeCompleted); // 궁극기 리듬 챌린지 표시 (Day49, 결과는 표시만 하고 효과는 아직 미연동)
            return true; // 궁극기 실행 성공 반환
        }

        private void HandleRhythmChallengeCompleted(RhythmChallengeResult result) // 궁극기 리듬 챌린지 종료 처리 (Day49 판정 등급 확장)
        {
            Debug.Log($"[Project H][RHYTHM] {Stats?.CharacterId}, {result}, Accuracy={result.Accuracy:P0}"); // 리듬 챌린지 등급별 결과 로그 출력 (전투 효과 연동은 후속 Day)
        }

        public void SetUltimatePreview(float ratio) // 궁극기 게이지 UI 미리보기 설정
        {
            ultimateRatio = Mathf.Clamp01(ratio); // 궁극기 게이지 범위 보정
            RefreshUltimate(); // 궁극기 게이지 표시 갱신
        }

        private void HandleUltimateGaugeChanged(string characterId, int currentGauge) // Runtime 궁극기 게이지 변경 처리
        {
            if (Stats == null || Stats.CharacterId != characterId) // 연결 캐릭터 ID 일치 확인
            {
                return; // 다른 캐릭터 게이지 변경 무시
            }

            ultimateRatio = Mathf.Clamp01((float)currentGauge / BattleUltimateGaugeRuntimeState.MaxGauge); // 변경 게이지 HUD 비율 계산
            RefreshUltimate(); // 궁극기 게이지와 Ready 표시 갱신
        }

        private void HandleSelectionChanged(string selectedRuntimeId) // 캐릭터 선택 변경 이벤트 처리
        {
            RefreshSelectionHighlight(); // 현재 HUD 초상화 선택 윤곽 갱신
        }

        private void RefreshSelectionHighlight() // HUD 초상화 선택 윤곽 상태 반영
        {
            bool highlighted = Stats != null && Stats.IsAlive && BattleSelectionRuntimeState.IsSelected(Stats.RuntimeId); // 현재 HUD 캐릭터 생존 및 선택 여부 계산
            SetSelectionHighlighted(highlighted); // 현재 초상화 선택 윤곽 상태 적용
        }

        private void EnsurePortraitSelectionOutline() // HUD 초상화 선택 윤곽 효과 준비
        {
            if (portraitText == null) // 현재 임시 초상화 Graphic 존재 확인
            {
                return; // 초상화 없음 윤곽 준비 중단
            }

            if (portraitSelectionOutline != null && portraitSelectionOutline.gameObject == portraitText.gameObject) // 기존 초상화 윤곽 재사용 가능 여부 확인
            {
                return; // 기존 초상화 윤곽 유지
            }

            portraitSelectionOutline = portraitText.gameObject.AddComponent<Outline>(); // 현재 초상화 Graphic에 선택 윤곽 추가
            portraitSelectionOutline.effectColor = new Color(1f, 0.78f, 0.12f, 1f); // 금색 초상화 선택 윤곽 색상 적용
            portraitSelectionOutline.effectDistance = new Vector2(3f, -3f); // 초상화 선택 윤곽 선 두께 적용
            portraitSelectionOutline.useGraphicAlpha = true; // 초상화 Graphic 알파 기반 윤곽 적용
            portraitSelectionOutline.enabled = false; // 생성 직후 초상화 선택 윤곽 숨김
        }

        private void EnsurePortraitButton() // 초상화 궁극기 입력 버튼 준비 (Day49 추가 — 궁극기 준비 시 초상화 클릭으로 발동)
        {
            if (portraitText == null) // 초상화 텍스트 참조 확인
            {
                return; // 초상화 버튼 준비 중단
            }

            Transform portraitRoot = portraitText.transform.parent; // 초상화 배경(Portrait) Transform 조회

            if (portraitRoot == null) // 초상화 배경 존재 확인
            {
                return; // 초상화 버튼 준비 중단
            }

            portraitImage = portraitRoot.GetComponent<Image>(); // 초상화 배경 이미지 조회
            portraitButton = portraitRoot.GetComponent<Button>(); // 기존 초상화 버튼 조회

            if (portraitButton == null) // 기존 초상화 버튼 존재 확인
            {
                portraitButton = portraitRoot.gameObject.AddComponent<Button>(); // 초상화 배경에 Runtime 버튼 추가
            }

            if (portraitImage != null) // 초상화 배경 이미지 존재 확인
            {
                portraitButton.targetGraphic = portraitImage; // 초상화 배경을 버튼 TargetGraphic으로 연결
                portraitImage.raycastTarget = true; // 초상화 배경 클릭 판정 활성화 (기존 씬에는 장식용으로 꺼져 있었음, 원인 수정)
            }

            portraitButton.onClick.RemoveListener(HandlePortraitClicked); // 기존 동일 초상화 클릭 이벤트 제거
            portraitButton.onClick.AddListener(HandlePortraitClicked); // 초상화 클릭 궁극기 실행 이벤트 연결
        }

        private void HandlePortraitClicked() // 초상화 클릭 처리 (Day49 추가)
        {
            if (!IsUltimateReady) // 궁극기 준비 상태 확인
            {
                return; // 미준비 상태 클릭 무시
            }

            TryUseUltimate(); // 현재 HUD 캐릭터 궁극기 사용 시도
        }

        private void EnsureUltimateButton() // 궁극기 텍스트 Runtime 입력 버튼 준비
        {
            if (ultimateText == null) // 궁극기 텍스트 참조 확인
            {
                return; // 궁극기 버튼 준비 중단
            }

            ultimateButton = ultimateText.GetComponent<Button>(); // 기존 궁극기 텍스트 버튼 조회

            if (ultimateButton == null) // 기존 궁극기 버튼 존재 확인
            {
                ultimateButton = ultimateText.gameObject.AddComponent<Button>(); // 궁극기 텍스트에 Runtime 버튼 추가
            }

            ultimateText.raycastTarget = true; // 궁극기 텍스트 포인터 입력 활성화
            ultimateButton.targetGraphic = ultimateText; // 궁극기 텍스트를 버튼 TargetGraphic으로 연결
            ultimateButton.onClick.RemoveListener(HandleUltimateClicked); // 기존 동일 궁극기 클릭 이벤트 제거
            ultimateButton.onClick.AddListener(HandleUltimateClicked); // 궁극기 클릭 실행 이벤트 연결
        }

        private void HandleUltimateClicked() // 궁극기 HUD 클릭 처리
        {
            TryUseUltimate(); // 현재 HUD 캐릭터 궁극기 사용 시도
        }

        private void RefreshSkillState() // 스킬 자리 표시 갱신
        {
            bool alive = Stats == null || Stats.IsAlive; // 현재 캐릭터 생존 상태 계산

            if (skillButton != null) // 스킬 자리 버튼 확인
            {
                skillButton.interactable = false; // 블록 방식 스킬 입력과 중복되지 않도록 카드 버튼 잠금 유지
            }

            if (skillText != null) // 스킬 자리 텍스트 확인
            {
                skillText.text = alive ? "SKILL\nLOCKED" : "DOWN"; // 블록 방식 스킬과 중복되지 않는 카드 상태 표시
            }
        }

        private void RefreshUltimate() // 궁극기 게이지 표시 갱신
        {
            SetFill(gaugeFillImage, ultimateRatio); // 궁극기 게이지 비율 적용

            if (ultimateButton != null) // 궁극기 Runtime 버튼 확인
            {
                ultimateButton.interactable = IsUltimateReady; // 생존 및 Ready 상태 기반 궁극기 입력 활성화
            }

            if (portraitButton != null) // 초상화 Runtime 버튼 확인 (Day49 추가)
            {
                portraitButton.interactable = IsUltimateReady; // 생존 및 Ready 상태 기반 초상화 입력 활성화
            }

            if (portraitImage != null) // 초상화 배경 이미지 확인 (Day49 추가)
            {
                portraitImage.color = IsUltimateReady ? new Color(1f, 0.93f, 0.62f, 1f) : new Color(0.92f, 0.95f, 0.98f, 0.95f); // 궁극기 준비 상태 기반 초상화 강조 색상 적용
            }

            if (ultimateText != null) // 궁극기 게이지 텍스트 확인
            {
                int percent = Mathf.RoundToInt(ultimateRatio * 100f); // 궁극기 게이지 퍼센트 계산
                ultimateText.text = Stats != null && !Stats.IsAlive ? $"ULT {percent}% · DOWN" : IsUltimateReady ? "ULT READY" : $"ULT {percent}%"; // 생존 및 완충 상태 기반 궁극기 문구 표시
            }
        }

        private void OnDestroy() // HUD 카드 제거
        {
            UnbindRuntimeEvents(); // Runtime 이벤트 연결 해제

            if (ultimateButton != null) // 궁극기 Runtime 버튼 확인
            {
                ultimateButton.onClick.RemoveListener(HandleUltimateClicked); // 궁극기 클릭 이벤트 안전 해제
            }

            if (portraitButton != null) // 초상화 Runtime 버튼 확인 (Day49 추가)
            {
                portraitButton.onClick.RemoveListener(HandlePortraitClicked); // 초상화 클릭 이벤트 안전 해제
            }
        }

        private void UnbindRuntimeEvents() // HUD Runtime 이벤트 안전 해제
        {
            if (Stats != null) // 기존 전투 스탯 확인
            {
                Stats.HealthChanged -= Refresh; // 기존 체력 변경 이벤트 해제
            }

            BattleUltimateGaugeRuntimeState.GaugeChanged -= HandleUltimateGaugeChanged; // 궁극기 게이지 변경 이벤트 해제
            BattleSelectionRuntimeState.SelectionChanged -= HandleSelectionChanged; // 캐릭터 선택 변경 이벤트 해제
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
