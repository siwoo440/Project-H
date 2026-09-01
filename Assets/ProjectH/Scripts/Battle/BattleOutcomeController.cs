using System; // 이벤트 기능
using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 승패 컨트롤러 방지
    public sealed class BattleOutcomeController : MonoBehaviour // 전투 승패 감시 및 종료 컨트롤러
    {
        private BattleCombatRegistry registry; // 전투 객체 레지스트리
        private BattleScreenController screenController; // 전투 화면 컨트롤러
        private bool monitoring; // 승패 감시 활성 상태
        public BattleOutcome CurrentOutcome { get; private set; } = BattleOutcome.Preparing; // 현재 전투 승패 상태
        public event Action<BattleOutcome> OutcomeChanged; // 전투 승패 변경 이벤트

        public void Configure(BattleCombatRegistry combatRegistry, BattleScreenController battleScreenController) // 승패 컨트롤러 참조 설정
        {
            StopMonitoring(); // 기존 승패 감시 종료
            registry = combatRegistry; // 전투 레지스트리 연결
            screenController = battleScreenController; // 전투 화면 컨트롤러 연결
            CurrentOutcome = BattleOutcome.Preparing; // 승패 상태 준비로 초기화
        }

        public void BeginBattle() // 전투 승패 감시 시작
        {
            if (registry == null) // 전투 레지스트리 확인
            {
                CurrentOutcome = BattleOutcome.Preparing; // 레지스트리 누락 준비 상태 유지
                return; // 승패 감시 시작 중단
            }

            StopMonitoring(); // 기존 이벤트 구독 안전 해제
            CurrentOutcome = BattleOutcome.Running; // 전투 진행 상태 설정
            registry.ActorUnregistered += HandleActorUnregistered; // 전투 객체 제외 이벤트 구독
            monitoring = true; // 승패 감시 활성화
            EvaluateNow(); // 시작 시 현재 팀 생존 상태 확인
        }

        public BattleOutcome EvaluateNow() // 현재 생존 수 즉시 승패 판정
        {
            if (!monitoring || registry == null) // 승패 감시 상태 확인
            {
                return CurrentOutcome; // 현재 승패 상태 반환
            }

            int livingAllies = registry.CountLiving(BattleTeam.Ally); // 생존 아군 수 계산
            int livingEnemies = registry.CountLiving(BattleTeam.Enemy); // 생존 적군 수 계산
            BattleOutcome evaluated = BattleOutcomeEvaluator.Evaluate(livingAllies, livingEnemies); // 현재 팀 생존 수 승패 판정

            if (evaluated == BattleOutcome.Running) // 전투 진행 상태 확인
            {
                return CurrentOutcome; // 기존 진행 상태 유지
            }

            FinishBattle(evaluated); // 최종 승패 처리
            return CurrentOutcome; // 최종 승패 상태 반환
        }

        public void StopMonitoring() // 승패 감시 종료
        {
            if (registry != null) // 전투 레지스트리 존재 확인
            {
                registry.ActorUnregistered -= HandleActorUnregistered; // 전투 객체 제외 이벤트 구독 해제
            }

            monitoring = false; // 승패 감시 비활성화
        }

        private void HandleActorUnregistered(BattleActor actor) // 전투 객체 제외 이벤트 처리
        {
            if (actor == null) // 제외 전투 객체 확인
            {
                return; // 승패 판정 중단
            }

            EvaluateNow(); // 사망 제외 직후 승패 재판정
        }

        private void FinishBattle(BattleOutcome outcome) // 최종 전투 승패 처리
        {
            if (!monitoring || CurrentOutcome != BattleOutcome.Running) // 전투 종료 중복 처리 확인
            {
                return; // 중복 전투 종료 차단
            }

            CurrentOutcome = outcome; // 최종 승패 상태 저장
            StopMonitoring(); // 추가 승패 판정 중지
            StopRemainingCombatActions(); // 생존 전투 객체 행동 정지
            BattleHealthDebugController healthDebug = GetComponent<BattleHealthDebugController>(); // 체력 디버그 컨트롤러 조회

            if (healthDebug != null) // 체력 디버그 컨트롤러 확인
            {
                healthDebug.enabled = false; // 전투 종료 후 회복 디버그 중지
            }

            screenController?.HandleBattleOutcome(outcome); // 전투 화면 종료 처리
            OutcomeChanged?.Invoke(outcome); // 최종 승패 변경 이벤트 발생
        }

        private void StopRemainingCombatActions() // 남은 생존 전투 객체 행동 정지
        {
            if (registry == null) // 전투 레지스트리 확인
            {
                return; // 전투 행동 정지 중단
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 등록된 생존 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 객체 조회

                if (actor == null) // 전투 객체 존재 확인
                {
                    continue; // null 전투 객체 제외
                }

                BattleBasicAttackController attackController = actor.GetComponent<BattleBasicAttackController>(); // 기본 공격 컨트롤러 조회

                if (attackController != null) // 기본 공격 컨트롤러 확인
                {
                    attackController.enabled = false; // 전투 종료 후 기본 공격 중지
                }

                BattleEnemyBrain enemyBrain = actor.GetComponent<BattleEnemyBrain>(); // 적군 AI Brain 조회

                if (enemyBrain != null) // 적군 AI Brain 확인
                {
                    enemyBrain.enabled = false; // 전투 종료 후 적군 AI 판단 중지
                }
            }
        }

        private void OnDestroy() // 승패 컨트롤러 제거 처리
        {
            StopMonitoring(); // 승패 이벤트 구독 해제
        }
    }
}
