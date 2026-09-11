using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // Unity UI 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 전투 액터 방지
    public sealed class BattleActor : MonoBehaviour // 공통 전투 행동 객체
    {
        [SerializeField] private Image bodyImage; // 임시 전투 바디 이미지
        [SerializeField] private BattleActionDebugText actionDebugText; // 머리 위 행동 디버그 텍스트
        [SerializeField] private BattleFloatingValueText floatingValueText; // 피해 회복 숫자 디버그 텍스트
        private Color baseBodyColor = Color.white; // 기본 바디 색상
        private Coroutine flashRoutine; // 피격 미리보기 코루틴
        [SerializeField] private Outline selectionOutline; // 선택 캐릭터 바디 윤곽 효과
        public BattleTeam Team { get; private set; } // 전투 팀
        public IBattleCombatantStats Stats { get; private set; } // 공통 전투 스탯
        public Vector3 HomePosition { get; private set; } // 최초 진형 위치
        public bool IsCombatReady => Stats != null; // 전투 초기화 완료 여부
        public bool IsSelectionHighlighted => selectionOutline != null && selectionOutline.enabled; // 선택 캐릭터 윤곽 활성 상태 반환
        public float ForwardDirection => Team == BattleTeam.Ally ? 1f : -1f; // 팀별 전진 방향 반환

        public void ConfigureVisuals(Image body, BattleActionDebugText debugText) // 이전 전투 시각 참조 설정
        {
            ConfigureVisuals(body, debugText, floatingValueText); // 기존 체력 변화 텍스트 유지 설정
        }

        public void ConfigureVisuals(Image body, BattleActionDebugText debugText, BattleFloatingValueText valueText) // 체력 변화 포함 전투 시각 참조 설정
        {
            bodyImage = body; // 전투 바디 이미지 연결
            actionDebugText = debugText; // 행동 디버그 텍스트 연결
            floatingValueText = valueText; // 체력 변화 텍스트 연결

            if (bodyImage != null) // 전투 바디 이미지 확인
            {
                baseBodyColor = bodyImage.color; // 현재 바디 색상 저장
            }

            EnsureSelectionOutline(); // 선택 캐릭터 바디 윤곽 효과 준비
            RefreshSelectionHighlight(); // 현재 선택 상태 바디 윤곽 반영
        }

        public void Initialize(BattleTeam team, IBattleCombatantStats stats, Vector3 homePosition) // 전투 액터 초기화
        {
            Team = team; // 전투 팀 저장
            Stats = stats; // 공통 전투 스탯 저장
            HomePosition = homePosition; // 최초 진형 위치 저장
            BattleSelectionRuntimeState.SelectionChanged -= HandleSelectionChanged; // 기존 선택 변경 이벤트 중복 구독 제거
            BattleSelectionRuntimeState.SelectionChanged += HandleSelectionChanged; // 선택 변경 이벤트 구독
            EnsureSelectionOutline(); // 전투 초기화 시 바디 윤곽 효과 준비
            RefreshSelectionHighlight(); // 현재 선택 상태 바디 윤곽 반영
        }

        public void SetSelectionHighlighted(bool highlighted) // 전장 캐릭터 선택 윤곽 표시 설정
        {
            EnsureSelectionOutline(); // 선택 윤곽 효과 준비

            if (selectionOutline != null) // 선택 윤곽 효과 존재 확인
            {
                selectionOutline.enabled = highlighted; // 선택 여부 기반 윤곽 활성 상태 적용
            }
        }

        public void SetBodyColor(Color color) // 전투 바디 기본색 설정
        {
            baseBodyColor = color; // 기본 바디 색상 저장

            if (bodyImage != null) // 전투 바디 이미지 확인
            {
                bodyImage.color = color; // 전투 바디 색상 적용
            }
        }

        public float HorizontalDistanceTo(BattleActor target) // 다른 전투 객체와 가로 거리 계산
        {
            if (target == null) // 대상 전투 객체 확인
            {
                return float.PositiveInfinity; // 대상 없음 거리 반환
            }

            return Mathf.Abs(target.transform.position.x - transform.position.x); // 가로 절대 거리 반환
        }

        public float ForwardDistanceTo(BattleActor target) // 전진 방향 기준 상대 거리 계산
        {
            if (target == null) // 대상 전투 객체 확인
            {
                return float.PositiveInfinity; // 대상 없음 거리 반환
            }

            return (target.transform.position.x - transform.position.x) * ForwardDirection; // 전진 방향 상대 거리 반환
        }

        public bool IsOpponentAhead(BattleActor target) // 상대가 전방에 있는지 확인
        {
            return target != null && ForwardDistanceTo(target) >= 0f; // 전진 방향 앞쪽 여부 반환
        }

        public bool IsWithinAttackRange(BattleActor target) // 현재 기본 공격 사거리 확인
        {
            if (!IsCombatReady || target == null) // 전투 상태 및 타겟 확인
            {
                return false; // 공격 사거리 판정 실패
            }

            float safeRange = Mathf.Max(0.2f, Stats.AttackRange); // 최소 공격 사거리 보정
            return HorizontalDistanceTo(target) <= safeRange; // 횡스크롤 가로 사거리 판정 반환
        }

        public void MoveForwardToward(BattleActor target, float deltaTime) // 전방 타겟 방향 횡스크롤 이동
        {
            if (!IsCombatReady || target == null) // 전투 상태 및 타겟 확인
            {
                return; // 전방 이동 중단
            }

            if (!IsOpponentAhead(target)) // 타겟 전방 여부 확인
            {
                return; // 이미 지나친 타겟 방향 이동 차단
            }

            float safeRange = Mathf.Max(0.2f, Stats.AttackRange); // 최소 공격 사거리 보정
            float stopX = target.transform.position.x - (ForwardDirection * safeRange); // 타겟 앞 공격 정지선 계산
            float currentX = transform.position.x; // 현재 X 위치 저장
            float maxStep = Mathf.Max(0.01f, Stats.MoveSpeed) * Mathf.Max(0f, deltaTime); // 프레임 최대 전진 거리 계산
            float nextX = Mathf.MoveTowards(currentX, stopX, maxStep); // 공격 정지선까지 전진 위치 계산

            if (Team == BattleTeam.Ally) // 아군 전진 처리
            {
                nextX = Mathf.Min(nextX, stopX); // 적군 정지선 초과 이동 방지
            }
            else // 적군 전진 처리
            {
                nextX = Mathf.Max(nextX, stopX); // 아군 정지선 초과 이동 방지
            }

            Vector3 nextPosition = transform.position; // 현재 위치 복사
            nextPosition.x = nextX; // 가로 전진 위치 적용
            transform.position = nextPosition; // 전투 객체 전진 위치 저장
        }

        public void MoveToward(BattleActor target, float deltaTime) // 현재 위치에서 방향과 무관하게 타겟 쪽 횡스크롤 이동
        {
            if (!IsCombatReady || target == null) // 전투 상태 및 타겟 확인
            {
                return; // 방향 무관 이동 중단
            }

            float safeRange = Mathf.Max(0.2f, Stats.AttackRange); // 최소 공격 사거리 보정
            float currentX = transform.position.x; // 현재 X 위치 저장
            float targetX = target.transform.position.x; // 타겟 X 위치 저장
            float signedDistance = targetX - currentX; // 현재 위치 기준 타겟 방향 거리 계산

            if (Mathf.Abs(signedDistance) <= safeRange) // 이미 공격 사거리 내부 여부 확인
            {
                return; // 추가 이동 불필요 처리
            }

            float direction = Mathf.Sign(signedDistance); // 현재 위치에서 타겟 방향 계산
            float stopX = targetX - (direction * safeRange); // 현재 접근 방향 기준 공격 정지선 계산
            float maxStep = Mathf.Max(0.01f, Stats.MoveSpeed) * Mathf.Max(0f, deltaTime); // 프레임 최대 이동 거리 계산
            Vector3 nextPosition = transform.position; // 현재 위치 복사
            nextPosition.x = Mathf.MoveTowards(currentX, stopX, maxStep); // 좌우 방향 모두 허용한 정지선 이동 계산
            transform.position = nextPosition; // 최근접 타겟 방향 위치 저장
        }

        public int ApplyDamage(BattleDamageResult result) // 계산 완료 피해 적용
        {
            if (!IsCombatReady || !Stats.IsAlive) // 전투 상태 및 생존 여부 확인
            {
                return 0; // 피해 적용 중단
            }

            if (result.TargetRuntimeId != Stats.RuntimeId) // 피해 대상 ID 일치 확인
            {
                return 0; // 잘못된 대상 피해 차단
            }

            IBattleMutableCombatantStats mutableStats = Stats as IBattleMutableCombatantStats; // 변경 가능 전투 스탯 변환

            if (mutableStats == null) // 변경 가능 전투 스탯 확인
            {
                return 0; // 체력 변경 불가 대상 차단
            }

            if (BattleSkillRuntimeState.IsInvulnerable(Stats.RuntimeId)) // 무적 상태 확인 (Day54 추가, 보스 페이즈 전환 등)
            {
                FlashHitPreview(); // 피격 표시만 유지
                return 0; // 무적 중 피해 무시
            }

            int remainingDamage = BattlePassiveRuntimeState.AbsorbShield(Stats.RuntimeId, result.Damage, out int absorbedDamage); // 패시브 보호막 우선 피해 흡수

            if (absorbedDamage > 0) // 보호막 피해 흡수 확인
            {
                FlashHitPreview(); // 보호막 피격 표시
                Debug.Log($"[Project H][SHIELD] {Stats.RuntimeId}, Absorbed={absorbedDamage}, Remaining={remainingDamage}"); // 보호막 흡수 로그
            }

            if (remainingDamage <= 0) // 보호막 완전 흡수 확인
            {
                return 0; // 체력 피해 없음 반환
            }

            int applied = mutableStats.TakeDamage(remainingDamage); // 실제 피해 적용

            if (applied <= 0) // 실제 피해 발생 확인
            {
                return 0; // 피해 시각 처리 중단
            }

            FlashHitPreview(); // 피해 피격 표시
            floatingValueText?.ShowDamage(applied, result.Affinity); // 속성 상성 반영 피해 숫자 표시 (Day52 수정)
            AccumulateDisarray(result); // 실제 피해 발생 시에만 흐트러짐 누적 (Day53 추가)
            BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateDamageTaken(this, applied)); // 피해 수신 패시브 Trigger 처리

            if (Stats.IsAlive) // 피해 후 생존 여부 확인
            {
                BattleSkillRuntimeState.TryCounter(this, result); // 활성 반격 효과 기반 원 공격자 반격 시도
            }

            return applied; // 실제 피해량 반환
        }

        private void AccumulateDisarray(BattleDamageResult result) // 흐트러짐 게이지 누적 및 발생 처리 (Day53 추가)
        {
            if (Stats == null || !BattleDisarrayRuntimeState.IsRegistered(Stats.RuntimeId)) // 흐트러짐 게이지 등록 대상 확인
            {
                return; // 흐트러짐 누적 중단 (아군은 미등록이라 자연히 제외됨)
            }

            if (string.IsNullOrWhiteSpace(result.AttackerRuntimeId)) // 지속 피해 등 공격자 없는 피해 확인
            {
                return; // 지속 피해는 흐트러짐을 누적시키지 않음 (방치 흐트러짐 방지)
            }

            if (!BattleDisarrayRuntimeState.AddDisarray(Stats.RuntimeId, BattleDisarrayRuntimeState.GetHitAmount(result.Affinity))) // 상성 반영 누적 후 발생 여부 확인
            {
                return; // 흐트러짐 미발생 처리
            }

            BattleSkillRuntimeState.AddModifier(Stats.RuntimeId, BattleRuntimeModifierKind.Stun, 1f, BattleDisarrayRuntimeState.DisarrayedSeconds, BattleDisarrayRuntimeState.DisarraySourceKey); // 기존 기절 게이트를 재사용한 경직 적용
            BattleDisarrayPresentation.Announce(this); // 흐트러짐 발생 연출 실행
            Debug.Log($"[Project H][DISARRAY] {Stats.RuntimeId}, Count={BattleDisarrayRuntimeState.GetDisarrayCount(Stats.RuntimeId)}, NextMax={BattleDisarrayRuntimeState.GetMaxGauge(Stats.RuntimeId)}"); // 흐트러짐 발생 로그 출력
        }

        public int ApplyHealing(BattleHealingResult result) // 계산 완료 회복 적용
        {
            if (!IsCombatReady || !Stats.IsAlive) // 전투 상태 및 생존 여부 확인
            {
                return 0; // 일반 회복 부활 차단
            }

            if (result.TargetRuntimeId != Stats.RuntimeId) // 회복 대상 ID 일치 확인
            {
                return 0; // 잘못된 대상 회복 차단
            }

            IBattleMutableCombatantStats mutableStats = Stats as IBattleMutableCombatantStats; // 변경 가능 전투 스탯 변환

            if (mutableStats == null) // 변경 가능 전투 스탯 확인
            {
                return 0; // 체력 변경 불가 대상 차단
            }

            int applied = mutableStats.Heal(result.Healing); // 실제 회복 적용

            if (applied > 0) // 실제 회복 발생 확인
            {
                floatingValueText?.ShowHealing(applied); // 회복 숫자 표시
            }

            return applied; // 실제 회복량 반환
        }

        public void ShowAction(BattleActionKind actionKind) // 머리 위 행동 텍스트 표시
        {
            actionDebugText?.Show(actionKind); // 행동 디버그 텍스트 호출
        }

        public void FlashHitPreview() // 기본 공격 적중 미리보기 표시
        {
            if (bodyImage == null) // 전투 바디 이미지 확인
            {
                return; // 적중 미리보기 중단
            }

            if (flashRoutine != null) // 기존 피격 코루틴 확인
            {
                StopCoroutine(flashRoutine); // 기존 피격 코루틴 중단
            }

            flashRoutine = StartCoroutine(FlashRoutine()); // 피격 미리보기 코루틴 시작
        }

        private IEnumerator FlashRoutine() // 피격 미리보기 색상 변화
        {
            bodyImage.color = Color.white; // 피격 순간 흰색 표시
            yield return new WaitForSeconds(0.08f); // 짧은 피격 표시 대기
            bodyImage.color = baseBodyColor; // 기본 바디 색상 복원
            flashRoutine = null; // 피격 코루틴 참조 초기화
        }

        private void HandleSelectionChanged(string selectedRuntimeId) // 전투 선택 변경 이벤트 처리
        {
            RefreshSelectionHighlight(); // 현재 액터 선택 윤곽 상태 갱신
        }

        private void RefreshSelectionHighlight() // 현재 액터 선택 윤곽 상태 반영
        {
            bool highlighted = Team == BattleTeam.Ally && Stats != null && Stats.IsAlive && BattleSelectionRuntimeState.IsSelected(Stats.RuntimeId); // 생존 아군 선택 여부 계산
            SetSelectionHighlighted(highlighted); // 현재 바디 선택 윤곽 상태 적용
        }

        private void EnsureSelectionOutline() // 전장 캐릭터 선택 윤곽 효과 준비
        {
            if (bodyImage == null) // 캐릭터 바디 이미지 존재 확인
            {
                return; // 바디 없음 윤곽 준비 중단
            }

            if (selectionOutline != null && selectionOutline.gameObject == bodyImage.gameObject) // 기존 선택 윤곽 재사용 가능 여부 확인
            {
                return; // 기존 선택 윤곽 유지
            }

            selectionOutline = bodyImage.gameObject.AddComponent<Outline>(); // 캐릭터 바디 전용 선택 윤곽 추가
            selectionOutline.effectColor = new Color(1f, 0.78f, 0.12f, 1f); // 금색 선택 윤곽 색상 적용
            selectionOutline.effectDistance = new Vector2(3f, -3f); // 선택 윤곽 선 두께 적용
            selectionOutline.useGraphicAlpha = true; // 캐릭터 이미지 알파 기반 윤곽 적용
            selectionOutline.enabled = false; // 생성 직후 선택 윤곽 숨김
        }

        private void OnDestroy() // 전투 액터 제거 처리
        {
            BattleSelectionRuntimeState.SelectionChanged -= HandleSelectionChanged; // 선택 변경 이벤트 구독 해제
        }
    }
}
