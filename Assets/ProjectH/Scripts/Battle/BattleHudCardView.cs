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
        private BattleUltimateBeginResult pendingUltimate; // 리듬 챌린지 대기 중인 궁극기 선행 단계 결과 (Day50 추가)
        private BattleTimeController pausedTimeController; // 챌린지 동안 일시정지시킨 전투 시간 컨트롤러 (Day50 추가)
        private bool ultimateChallengeActive; // 리듬 챌린지 진행 여부 (Day50 추가)
        private BattleStatusEffectStripView statusStrip; // 초상화 위 상태이상 표시 (Day51 추가)
        private Text slotNumberText; // 초상화 좌상단 숫자키 슬롯 번호 텍스트 (Day54 추가)
        private Text bondBadgeText; // 초상화 우상단 결속 단계 배지 텍스트 (Day59 추가)
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
            RefreshBondBadge(); // 결속 단계 배지 표시 (Day59 추가)
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

        public bool TryUseUltimate() // 현재 HUD 캐릭터 궁극기 사용 시도 (Day50 리듬 성적 연동 흐름)
        {
            if (!IsUltimateReady || Stats == null || ultimateChallengeActive) // 생존·Ready·챌린지 중복 실행 여부 확인
            {
                return false; // 사용 불가 상태 실행 차단
            }

            BattleUltimateBeginResult begin = BattleUltimateExecutor.TryBeginUltimate(Stats.CharacterId); // 궁극기 검증과 게이지 소비만 선행 실행

            if (!begin.Succeeded) // 궁극기 선행 단계 실패 여부 확인
            {
                Debug.LogWarning($"[Project H][ULTIMATE] {Stats.CharacterId}, {begin.Message}"); // 궁극기 사용 실패 로그 출력
                return false; // 사용 실패 반환
            }

            pendingUltimate = begin; // 챌린지 종료 후 사용할 선행 단계 결과 보관
            ultimateChallengeActive = true; // 리듬 챌린지 진행 상태 기록
            PauseBattleForChallenge(); // 챌린지 집중을 위한 전투 일시정지 적용
            string characterId = Stats.CharacterId; // 컷인 종료 시점용 캐릭터 ID 보관 (Day59 추가)
            UltimateCutInInfo cutIn = UltimateCutInCatalog.Build(characterId, Stats.DisplayName, begin.UltimateName, BattleBondRuntimeState.GetLevel(characterId)); // 컷인 정보 생성 (Day59 추가)
            UltimateCutInView.Show(cutIn, () => ShowRhythmChallenge(begin.UltimateName, characterId)); // 궁극기 컷인 → 끝나면 리듬 챌린지 (Day59 추가)
            return true; // 궁극기 선행 단계 성공 반환
        }

        private void ShowRhythmChallenge(string ultimateName, string characterId) // 컷인 뒤 리듬 챌린지 표시 (Day59 추가 — 결속 3단계 Perfect 보너스 전달)
        {
            if (this == null) // HUD 제거 확인 (컷인 도중 씬 전환 대비)
            {
                return; // 표시 중단
            }

            UltimateRhythmChallengeView.Show(ultimateName, HandleRhythmChallengeCompleted, BattleBondRuntimeState.GetPerfectBonusPerHit(characterId)); // 궁극기 리듬 챌린지 표시 (Day50, 종료 시 성적 배율로 효과 실행)
        }

        private void RefreshBondBadge() // 초상화 우상단 결속 단계 배지 갱신 (Day59 추가)
        {
            RectTransform portraitRect = portraitText == null ? null : portraitText.transform.parent as RectTransform; // 초상화 배경 조회

            if (portraitRect == null || Stats == null) // 초상화·스탯 확인
            {
                return; // 갱신 중단
            }

            if (bondBadgeText == null) // 배지 생성 여부 확인
            {
                bondBadgeText = CreateSlotNumberBadge(portraitRect); // 숫자키 배지와 같은 모양으로 생성
                RectTransform badge = (RectTransform)bondBadgeText.transform.parent; // 배지 배경 조회
                badge.gameObject.name = "BondBadge"; // 이름 구분
                badge.anchorMin = new Vector2(1f, 1f); // 우상단 앵커
                badge.anchorMax = new Vector2(1f, 1f); // 우상단 앵커
                badge.pivot = new Vector2(1f, 1f); // 우상단 피벗
                badge.sizeDelta = new Vector2(34f, 24f); // 배지 크기
                badge.anchoredPosition = new Vector2(-5f, -5f); // 안쪽 여백
                badge.GetComponent<Image>().color = new Color(0.24f, 0.14f, 0.34f, 0.90f); // 보라색 배경
                bondBadgeText.color = new Color(1f, 0.84f, 0.36f, 1f); // 금색 글자
                bondBadgeText.fontSize = 15; // 글자 크기
            }

            int level = BattleBondRuntimeState.GetLevel(Stats.CharacterId); // 결속 단계 조회
            bondBadgeText.transform.parent.gameObject.SetActive(level > 0); // 결속 전이면 숨김
            bondBadgeText.text = $"◆{level}"; // 단계 표시
        }

        private void PauseBattleForChallenge() // 리듬 챌린지 동안 전투 일시정지 적용 (Day50 추가)
        {
            BattleTimeController controller = FindFirstObjectByType<BattleTimeController>(); // 현재 Scene 전투 시간 컨트롤러 조회

            if (controller == null || controller.IsPaused) // 컨트롤러 부재 또는 이미 일시정지 상태 확인
            {
                pausedTimeController = null; // 이 챌린지가 일시정지시킨 대상 없음 기록 (사용자 일시정지 상태 보존)
                return; // 일시정지 적용 중단
            }

            controller.SetPaused(true); // 전투 일시정지 적용
            pausedTimeController = controller; // 챌린지 종료 시 복구할 컨트롤러 보관
        }

        private void ResumeBattleAfterChallenge() // 리듬 챌린지 종료 후 전투 재개 (Day50 추가)
        {
            if (pausedTimeController != null) // 이 챌린지가 일시정지시킨 컨트롤러 확인
            {
                pausedTimeController.SetPaused(false); // 전투 진행 상태 복구
                pausedTimeController = null; // 복구 대상 참조 해제
            }
        }

        private void HandleRhythmChallengeCompleted(RhythmChallengeResult result) // 궁극기 리듬 챌린지 종료 처리 (Day50 판정 등급 전투 효과 연동)
        {
            try // 효과 실행 중 예외와 무관하게 전투 재개를 보장
            {
                float powerMultiplier = RhythmPowerScaler.Evaluate(result, BattleBondRuntimeState.GetPerfectBonusPerHit(pendingUltimate.CharacterId)); // 리듬 성적·결속 Perfect 보너스 기반 궁극기 위력 배율 계산 (Day59 수정)
                BattleUltimateExecutionResult execution = BattleUltimateExecutor.ExecuteUltimateEffect(pendingUltimate, powerMultiplier); // 계산 배율로 궁극기 효과 실행
                Debug.Log($"[Project H][RHYTHM] {Stats?.CharacterId}, {result}, Accuracy={result.Accuracy:P0}, MaxCombo={result.MaxCombo}, FullCombo={result.IsFullCombo}, Power=x{powerMultiplier:0.00}"); // 리듬 챌린지 결과와 적용 배율 로그 출력

                if (!execution.Succeeded) // 궁극기 효과 실행 실패 여부 확인
                {
                    Debug.LogWarning($"[Project H][ULTIMATE] {Stats?.CharacterId}, {execution.Message}"); // 효과 미적용 사유 로그 출력
                }
            }
            finally // 성공 여부와 무관한 마무리 처리
            {
                ResumeBattleAfterChallenge(); // 전투 재개 보장
                pendingUltimate = default; // 대기 궁극기 선행 결과 초기화
                ultimateChallengeActive = false; // 리듬 챌린지 진행 상태 해제
                RefreshUltimate(); // 소비된 게이지 기준 궁극기 표시 갱신
            }
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
            EnsureStatusStrip(portraitRoot as RectTransform); // 초상화 위 상태이상 표시 준비 (Day51 추가)
        }

        public void SetSlotNumber(int slotNumber) // 초상화 좌상단 숫자키 슬롯 번호 표시 (Day54 추가)
        {
            RectTransform portraitRect = portraitText == null ? null : portraitText.transform.parent as RectTransform; // 초상화 배경 RectTransform 조회

            if (portraitRect == null) // 초상화 배경 존재 확인
            {
                return; // 슬롯 번호 표시 중단
            }

            if (slotNumberText == null) // 슬롯 번호 텍스트 생성 여부 확인
            {
                slotNumberText = CreateSlotNumberBadge(portraitRect); // 슬롯 번호 배지 생성
            }

            bool visible = slotNumber > 0; // 유효 슬롯 번호 여부 판정
            slotNumberText.transform.parent.gameObject.SetActive(visible); // 배지 표시 여부 적용
            slotNumberText.text = visible ? slotNumber.ToString() : string.Empty; // 슬롯 번호 문구 적용
        }

        private static Text CreateSlotNumberBadge(RectTransform portraitRect) // 초상화 좌상단 슬롯 번호 배지 생성 (Day54 추가)
        {
            GameObject badgeObject = new GameObject("SlotNumberBadge", typeof(RectTransform), typeof(Image)); // 배지 배경 객체 생성
            badgeObject.transform.SetParent(portraitRect, false); // 초상화 배경 자식으로 연결
            RectTransform badgeRect = badgeObject.GetComponent<RectTransform>(); // 배지 RectTransform 조회
            badgeRect.anchorMin = new Vector2(0f, 1f); // 초상화 좌상단 최소 앵커 설정
            badgeRect.anchorMax = new Vector2(0f, 1f); // 초상화 좌상단 최대 앵커 설정
            badgeRect.pivot = new Vector2(0f, 1f); // 좌상단 기준 피벗 설정
            badgeRect.sizeDelta = new Vector2(24f, 24f); // 배지 크기 설정
            badgeRect.anchoredPosition = new Vector2(5f, -5f); // 초상화 테두리 안쪽 여백 적용
            Image badgeImage = badgeObject.GetComponent<Image>(); // 배지 배경 이미지 조회
            badgeImage.color = new Color(0.10f, 0.15f, 0.26f, 0.88f); // 남색 배지 배경 색상 적용
            badgeImage.raycastTarget = false; // 배지 클릭 차단 비활성화 (초상화 궁극기 버튼 입력 유지)

            GameObject textObject = new GameObject("SlotNumberText", typeof(RectTransform), typeof(Text), typeof(Outline)); // 슬롯 번호 텍스트 객체 생성
            textObject.transform.SetParent(badgeObject.transform, false); // 배지 자식으로 연결
            RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 조회
            textRect.anchorMin = Vector2.zero; // 텍스트 최소 앵커 전체 설정
            textRect.anchorMax = Vector2.one; // 텍스트 최대 앵커 전체 설정
            textRect.offsetMin = Vector2.zero; // 텍스트 최소 오프셋 초기화
            textRect.offsetMax = Vector2.zero; // 텍스트 최대 오프셋 초기화
            Text text = textObject.GetComponent<Text>(); // 슬롯 번호 Text 조회
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 적용
            text.fontSize = 17; // 슬롯 번호 크기 적용
            text.fontStyle = FontStyle.Bold; // 슬롯 번호 굵기 적용
            text.color = Color.white; // 슬롯 번호 흰색 적용
            text.alignment = TextAnchor.MiddleCenter; // 슬롯 번호 중앙 정렬
            text.horizontalOverflow = HorizontalWrapMode.Overflow; // 좁은 영역에서도 숫자 유지
            text.verticalOverflow = VerticalWrapMode.Overflow; // 세로 잘림 방지
            text.raycastTarget = false; // 슬롯 번호 입력 비활성화 (초상화 궁극기 버튼 입력 유지)
            Outline outline = textObject.GetComponent<Outline>(); // 슬롯 번호 외곽선 조회
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f); // 어두운 외곽선 색상 적용
            outline.effectDistance = new Vector2(1f, -1f); // 외곽선 두께 적용
            return text; // 생성 슬롯 번호 텍스트 반환
        }

        private void EnsureStatusStrip(RectTransform portraitRect) // 초상화 위 상태이상 표시 준비 (Day51 추가)
        {
            if (portraitRect == null) // 초상화 RectTransform 확인
            {
                return; // 상태이상 표시 준비 중단
            }

            string runtimeId = Stats == null ? string.Empty : Stats.RuntimeId; // 현재 HUD 캐릭터 Runtime ID 조회
            if (statusStrip != null) // 기존 상태이상 표시 존재 확인
            {
                statusStrip.Bind(runtimeId); // 표시 대상만 갱신
                return; // 중복 생성 차단
            }

            statusStrip = BattleStatusEffectStripView.AttachAbove(portraitRect, runtimeId); // 초상화 위쪽에 상태이상 표시 부착
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
            ResumeBattleAfterChallenge(); // 챌린지 도중 HUD 제거 시 전투 정지 상태 잔존 방지 (Day50 추가)
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
