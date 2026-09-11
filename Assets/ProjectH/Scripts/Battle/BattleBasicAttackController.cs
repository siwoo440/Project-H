using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 기본 공격 컨트롤러 방지
    public sealed class BattleBasicAttackController : MonoBehaviour // 전선 전진형 자동 기본 공격 컨트롤러
    {
        [SerializeField] private BattleActor actor; // 기본 공격 실행 전투 객체
        [SerializeField] private BattleCombatRegistry registry; // 전투 객체 레지스트리
        [SerializeField, Min(0.01f)] private float attackPreviewSeconds = 0.18f; // 공격 적중 전 미리보기 시간
        private BattleEnemyBrain enemyBrain; // 적군 전용 AI Brain
        private BattleActor currentTarget; // 현재 공격 대상
        private BattleAttackState state = BattleAttackState.Idle; // 현재 공격 상태
        private float stateTimer; // 현재 상태 시간
        private bool manualMoveSuspended; // 수동 이동 중 자동 전투 일시정지 상태
        public BattleAttackState State => state; // 현재 공격 상태 반환
        public BattleActor CurrentTarget => currentTarget; // 현재 공격 대상 반환
        public bool IsManualMoveSuspended => manualMoveSuspended; // 수동 이동 자동 전투 정지 여부 반환

        public void Configure(BattleActor owner, BattleCombatRegistry combatRegistry) // 기존 공통 기본 공격 참조 설정
        {
            Configure(owner, combatRegistry, null); // 적군 AI 미사용 공통 설정 호출
        }

        public void Configure(BattleActor owner, BattleCombatRegistry combatRegistry, BattleEnemyBrain brain) // 적군 AI 포함 기본 공격 참조 설정
        {
            actor = owner; // 공격 실행 전투 객체 연결
            registry = combatRegistry; // 전투 레지스트리 연결
            enemyBrain = brain; // 적군 AI Brain 연결
            currentTarget = null; // 현재 타겟 초기화
            state = BattleAttackState.Idle; // 공격 상태 초기화
            stateTimer = 0f; // 공격 상태 시간 초기화
            manualMoveSuspended = false; // 수동 이동 정지 상태 초기화
            RegisterManualMoveSupport(); // 아군 수동 이동 입력 시스템 자동 연결
        }

        public void BeginManualMove() // 수동 이동 시작 시 자동 전투 일시정지
        {
            manualMoveSuspended = true; // 수동 이동 자동 전투 정지 활성화
            ResetTarget(); // 기존 공격 타겟 및 진행 상태 초기화
        }

        public void EndManualMove() // 수동 이동 종료 후 자동 전투 복귀
        {
            manualMoveSuspended = false; // 수동 이동 자동 전투 정지 해제
            ResetTarget(); // 새 위치 기준 타겟 재탐색 준비
        }

        private void Update() // 자동 기본 공격 상태 갱신
        {
            if (actor == null || registry == null || !actor.IsCombatReady || !actor.Stats.IsAlive) // 공격 실행 가능 상태 확인
            {
                return; // 자동 기본 공격 중단
            }

            if (manualMoveSuspended) // 수동 이동 중 자동 전투 정지 여부 확인
            {
                return; // 수동 이동 종료까지 자동 이동 및 공격 중단
            }

            if (BattleSkillRuntimeState.IsStunned(actor.Stats.RuntimeId)) // 현재 기절 상태 확인
            {
                return; // 기절 지속 중 이동·공격·상태 시간 진행 중단
            }

            switch (state) // 현재 공격 상태 분기
            {
                case BattleAttackState.Approach: // 전선 전진 상태 처리
                    UpdateApproach(); // 현재 타겟 방향 전진
                    break; // 전진 상태 처리 종료
                case BattleAttackState.Attack: // 기본 공격 상태 처리
                    UpdateAttack(); // 기본 공격 갱신
                    break; // 공격 상태 처리 종료
                case BattleAttackState.Cooldown: // 공격 대기 상태 처리
                    UpdateCooldown(); // 공격 대기 갱신
                    break; // 공격 대기 상태 처리 종료
                default: // 타겟 탐색 상태 처리
                    AcquireTarget(); // 새로운 타겟 탐색
                    break; // 타겟 탐색 상태 처리 종료
            }
        }

        private void AcquireTarget() // 새로운 기본 공격 타겟 탐색
        {
            currentTarget = SelectTarget(); // 아군 또는 적군 AI 기준 타겟 선택

            if (!IsTargetValid()) // 타겟 유효성 확인
            {
                ResetTarget(); // 타겟 및 상태 초기화
                return; // 타겟 탐색 중단
            }

            if (actor.IsWithinAttackRange(currentTarget)) // 현재 공격 사거리 확인
            {
                BeginAttack(); // 제자리 기본 공격 시작
                return; // 타겟 탐색 종료
            }

            state = BattleAttackState.Approach; // 전선 전진 상태 전환
        }

        private void UpdateApproach() // 현재 타겟 방향 전선 전진
        {
            if (!IsTargetValid()) // 현재 타겟 유효성 확인
            {
                AcquireTarget(); // 사망 타겟 대신 새 타겟 즉시 탐색
                return; // 전진 처리 중단
            }

            if (actor.IsWithinAttackRange(currentTarget)) // 이동 전 공격 사거리 확인
            {
                BeginAttack(); // 현재 타겟 기본 공격 시작
                return; // 전진 처리 종료
            }

            if (actor.Team == BattleTeam.Ally) // 아군 현재 위치 중심 추적 여부 확인
            {
                actor.MoveToward(currentTarget, Time.deltaTime); // 아군 최근접 적 방향 좌우 이동 허용
            }
            else // 적군 기존 전진 규칙 처리
            {
                actor.MoveForwardToward(currentTarget, Time.deltaTime); // 적군 기존 전방 정지선 이동 유지
            }

            if (actor.IsWithinAttackRange(currentTarget)) // 이동 후 공격 사거리 진입 확인
            {
                BeginAttack(); // 현재 타겟 기본 공격 시작
            }
        }

        private void BeginAttack() // 기본 공격 실행 시작
        {
            state = BattleAttackState.Attack; // 기본 공격 상태 설정
            stateTimer = attackPreviewSeconds; // 공격 적중 대기 시간 설정
            actor.ShowAction(BattleActionKind.BasicAttack); // 머리 위 공격 디버그 텍스트 표시
        }

        private void UpdateAttack() // 기본 공격 적중 타이밍 갱신
        {
            if (!IsTargetValid()) // 현재 타겟 유효성 확인
            {
                AcquireTarget(); // 사라진 적 대신 다음 적 즉시 탐색
                return; // 공격 처리 중단
            }

            stateTimer -= Time.deltaTime; // 공격 적중 대기 시간 감소

            if (stateTimer > 0f) // 공격 적중 대기 확인
            {
                return; // 공격 적중 대기 유지
            }

            float hitChance = BattleSkillRuntimeState.GetEffectiveAccuracy(actor.Stats); // 명중 감소 반영 최종 기본 공격 명중률 계산

            if (Random.value <= hitChance) // 기본 공격 명중 판정
            {
                float criticalChance = BattlePassiveSystem.GetBasicAttackCriticalChance(actor); // 기본 및 패시브 반영 치명타율 계산
                bool isCritical = Random.value <= criticalChance; // 기본 공격 치명타 판정
                float powerMultiplier = isCritical ? BattlePassiveSystem.GetBasicAttackCriticalDamageMultiplier() : 1f; // 치명타 위력 배율 계산
                BattleDamageResult damageResult = BattleDamageResolver.ResolveBasicAttack(actor.Stats, currentTarget.Stats, powerMultiplier); // 치명타 배율 반영 기본 공격 피해 계산
                int appliedDamage = currentTarget.ApplyDamage(damageResult); // 대상 실제 체력 감소 적용
                BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateBasicAttackHit(actor, currentTarget, appliedDamage)); // 기본 공격 적중 패시브 Trigger 처리

                if (isCritical) // 치명타 발생 확인
                {
                    BattlePassiveSystem.HandleBasicAttackCritical(actor, currentTarget); // 치명타 결속 스킬 처리 (Day59 이브 정령의 화살)
                    Debug.Log($"[Project H][CRIT] {actor.Stats.RuntimeId} -> {currentTarget.Stats.RuntimeId}, Chance={criticalChance:0.00}, Damage={appliedDamage}"); // 기본 공격 치명타 로그
                }
            }
            else // 기본 공격 빗나감 처리
            {
                BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateBasicAttackMiss(actor, currentTarget)); // 기본 공격 빗나감 패시브 Trigger 처리
                Debug.Log($"[Project H][MISS] {actor.Stats.RuntimeId} -> {currentTarget.Stats.RuntimeId}, Accuracy={hitChance:0.00}"); // 명중 감소 기반 빗나감 디버그 로그
            }

            state = BattleAttackState.Cooldown; // 현재 위치에서 공격 대기 상태 전환
            float hastedAttackSpeed = actor.Stats.AttackSpeed * BattleSkillRuntimeState.GetAttackSpeedMultiplier(actor.Stats.RuntimeId); // 가속 반영 공격속도 계산 (Day54 추가, 가속 없으면 1.0배)
            stateTimer = BattleBasicAttackTiming.GetInterval(hastedAttackSpeed, BattleSkillRuntimeState.GetAttackSpeedReduction(actor.Stats.RuntimeId)); // 가속·둔화 반영 공격속도 기반 다음 공격 시간 설정 (Day51·54 수정)
        }

        private void UpdateCooldown() // 현재 전선 위치에서 다음 기본 공격 대기
        {
            if (!IsTargetValid()) // 현재 타겟 생존 여부 확인
            {
                AcquireTarget(); // 사망 또는 제외된 타겟 즉시 교체
                return; // 공격 대기 중단
            }

            stateTimer -= Time.deltaTime; // 공격 대기 시간 감소

            if (stateTimer > 0f) // 공격 대기 남은 시간 확인
            {
                return; // 공격 대기 유지
            }

            if (actor.IsWithinAttackRange(currentTarget)) // 현재 타겟 공격 사거리 확인
            {
                BeginAttack(); // 같은 타겟 연속 기본 공격 시작
                return; // 공격 대기 종료
            }

            state = BattleAttackState.Approach; // 현재 타겟이 멀면 다시 전진
        }

        private BattleActor SelectTarget() // 현재 전투 객체의 타겟 정책 선택
        {
            if (enemyBrain != null && enemyBrain.enabled) // 적군 AI Brain 사용 가능 여부 확인
            {
                return enemyBrain.SelectTarget(); // 적군 AI 판단 타겟 반환
            }

            return registry.FindNearestOpponent(actor); // 아군 현재 위치 기준 가장 가까운 타겟 반환
        }

        private bool IsTargetValid() // 현재 타겟 유효성 확인
        {
            return currentTarget != null && currentTarget.IsCombatReady && currentTarget.Stats.IsAlive && currentTarget.Team != actor.Team; // 생존 상대 타겟 여부 반환
        }

        private void ResetTarget() // 타겟 및 공격 상태 초기화
        {
            currentTarget = null; // 현재 타겟 초기화
            state = BattleAttackState.Idle; // 타겟 탐색 대기 상태 설정
            stateTimer = 0f; // 상태 시간 초기화
        }

        private void RegisterManualMoveSupport() // 아군 수동 이동 시스템 자동 연결
        {
            if (actor == null || actor.Team != BattleTeam.Ally || registry == null) // 아군 및 Registry 연결 상태 확인
            {
                return; // 적군 또는 잘못된 참조 수동 이동 연결 제외
            }

            BattleManualMoveController manualMoveController = registry.GetComponent<BattleManualMoveController>(); // Registry의 기존 수동 이동 컨트롤러 조회

            if (manualMoveController == null) // 기존 수동 이동 컨트롤러 존재 확인
            {
                manualMoveController = registry.gameObject.AddComponent<BattleManualMoveController>(); // Registry 객체에 수동 이동 컨트롤러 자동 추가
            }

            manualMoveController.Configure(registry, Camera.main); // 현재 Registry 및 전장 카메라 연결
            manualMoveController.RegisterAlly(actor, this); // 파티 생성 순서 기반 숫자키 슬롯 등록
        }
    }
}
