using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 기본 공격 컨트롤러 방지
    public sealed class BattleBasicAttackController : MonoBehaviour // 전선 전진형 자동 기본 공격 컨트롤러
    {
        [SerializeField] private BattleActor actor; // 기본 공격 실행 전투 객체
        [SerializeField] private BattleCombatRegistry registry; // 전투 객체 레지스트리
        [SerializeField, Min(0.01f)] private float attackPreviewSeconds = 0.18f; // 공격 적중 전 미리보기 시간
        private BattleActor currentTarget; // 현재 공격 대상
        private BattleAttackState state = BattleAttackState.Idle; // 현재 공격 상태
        private float stateTimer; // 현재 상태 시간
        public BattleAttackState State => state; // 현재 공격 상태 반환
        public BattleActor CurrentTarget => currentTarget; // 현재 공격 대상 반환

        public void Configure(BattleActor owner, BattleCombatRegistry combatRegistry) // 런타임 기본 공격 참조 설정
        {
            actor = owner; // 공격 실행 전투 객체 연결
            registry = combatRegistry; // 전투 레지스트리 연결
            currentTarget = null; // 현재 타겟 초기화
            state = BattleAttackState.Idle; // 공격 상태 초기화
            stateTimer = 0f; // 공격 상태 시간 초기화
        }

        private void Update() // 자동 기본 공격 상태 갱신
        {
            if (actor == null || registry == null || !actor.IsCombatReady || !actor.Stats.IsAlive) // 공격 실행 가능 상태 확인
            {
                return; // 자동 기본 공격 중단
            }

            switch (state) // 현재 공격 상태 분기
            {
                case BattleAttackState.Approach: // 전선 전진 상태 처리
                    UpdateApproach(); // 가장 가까운 적 방향 전진
                    break; // 전진 상태 처리 종료
                case BattleAttackState.Attack: // 기본 공격 상태 처리
                    UpdateAttack(); // 기본 공격 갱신
                    break; // 기본 공격 상태 처리 종료
                case BattleAttackState.Cooldown: // 공격 대기 상태 처리
                    UpdateCooldown(); // 공격 대기 갱신
                    break; // 공격 대기 상태 처리 종료
                default: // 타겟 탐색 상태 처리
                    AcquireTarget(); // 새로운 전방 타겟 탐색
                    break; // 타겟 탐색 상태 처리 종료
            }
        }

        private void AcquireTarget() // 새로운 기본 공격 타겟 탐색
        {
            currentTarget = registry.FindNearestOpponent(actor); // 가장 가까운 생존 전방 상대 조회

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

        private void UpdateApproach() // 가장 가까운 적 방향 전선 전진
        {
            if (!IsTargetValid()) // 현재 타겟 유효성 확인
            {
                AcquireTarget(); // 새 전방 타겟 즉시 탐색
                return; // 전진 처리 중단
            }

            BattleActor nearestOpponent = registry.FindNearestOpponent(actor); // 현재 위치 기준 가장 가까운 상대 재확인

            if (nearestOpponent != null && nearestOpponent != currentTarget) // 더 가까운 전방 상대 등장 확인
            {
                currentTarget = nearestOpponent; // 가장 가까운 전방 상대 타겟 갱신
            }

            if (actor.IsWithinAttackRange(currentTarget)) // 이동 전 공격 사거리 확인
            {
                BeginAttack(); // 전방 상대 기본 공격 시작
                return; // 전진 처리 종료
            }

            actor.MoveForwardToward(currentTarget, Time.deltaTime); // 타겟 정지선까지 가로 전진

            if (actor.IsWithinAttackRange(currentTarget)) // 이동 후 공격 사거리 진입 확인
            {
                BeginAttack(); // 전방 상대 기본 공격 시작
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

            BattleDamageResult damageResult = BattleDamageResolver.ResolveBasicAttack(actor.Stats, currentTarget.Stats); // 기본 공격 실제 피해 계산
            currentTarget.ApplyDamage(damageResult); // 대상 실제 체력 감소 적용
            state = BattleAttackState.Cooldown; // 현재 위치에서 공격 대기 상태 전환
            stateTimer = BattleBasicAttackTiming.GetInterval(actor.Stats.AttackSpeed); // 공격속도 기반 다음 공격 시간 설정
        }

        private void UpdateCooldown() // 현재 전선 위치에서 다음 기본 공격 대기
        {
            stateTimer -= Time.deltaTime; // 공격 대기 시간 감소

            if (stateTimer > 0f) // 공격 대기 남은 시간 확인
            {
                return; // 공격 대기 유지
            }

            currentTarget = registry.FindNearestOpponent(actor); // 다음 행동의 가장 가까운 생존 상대 재선택

            if (!IsTargetValid()) // 새로운 타겟 유효성 확인
            {
                ResetTarget(); // 적군 없음 상태 초기화
                return; // 공격 대기 처리 종료
            }

            if (actor.IsWithinAttackRange(currentTarget)) // 다음 타겟 공격 사거리 확인
            {
                BeginAttack(); // 현재 전선에서 연속 기본 공격 시작
                return; // 공격 대기 종료
            }

            state = BattleAttackState.Approach; // 타겟이 멀면 다시 전진
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
    }
}
