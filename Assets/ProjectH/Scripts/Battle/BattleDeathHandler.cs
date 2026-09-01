using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 공통 사망 처리 방지
    public sealed class BattleDeathHandler : MonoBehaviour // 아군 적군 공통 전투 불능 처리기
    {
        [SerializeField, Min(0f)] private float hideDelaySeconds = 0.35f; // 전투 불능 표시 유지 시간
        private BattleActor actor; // 사망 처리 전투 액터
        private BattleCombatRegistry registry; // 전투 객체 레지스트리
        private BattleBasicAttackController attackController; // 기본 공격 컨트롤러
        private BattleEnemyBrain enemyBrain; // 적군 AI Brain
        private BattleUnitView allyView; // 아군 전투 View
        private BattleEnemyView enemyView; // 적군 전투 View
        private BattleStats allyStats; // 아군 전투 스탯
        private BattleEnemyStats enemyStats; // 적군 전투 스탯
        private bool defeated; // 사망 처리 완료 상태

        public void Configure(BattleActor owner, BattleCombatRegistry combatRegistry, BattleBasicAttackController basicAttackController, BattleEnemyBrain brain, BattleUnitView unitView, BattleEnemyView monsterView) // 공통 사망 처리 참조 설정
        {
            UnbindHealthEvent(); // 기존 체력 이벤트 연결 해제
            actor = owner; // 전투 액터 연결
            registry = combatRegistry; // 전투 레지스트리 연결
            attackController = basicAttackController; // 기본 공격 컨트롤러 연결
            enemyBrain = brain; // 적군 AI Brain 연결
            allyView = unitView; // 아군 전투 View 연결
            enemyView = monsterView; // 적군 전투 View 연결
            allyStats = actor == null ? null : actor.Stats as BattleStats; // 아군 전투 스탯 변환
            enemyStats = actor == null ? null : actor.Stats as BattleEnemyStats; // 적군 전투 스탯 변환
            defeated = false; // 사망 처리 상태 초기화
            BindHealthEvent(); // 현재 전투 스탯 체력 이벤트 연결
            EvaluateDeathState(); // 초기 사망 상태 확인
        }

        public bool EvaluateDeathState() // 현재 전투 객체 사망 상태 확인 및 제외
        {
            if (defeated || actor == null || !actor.IsCombatReady || actor.Stats.IsAlive) // 신규 사망 여부 확인
            {
                return false; // 신규 사망 처리 없음 반환
            }

            defeated = true; // 사망 처리 시작 기록
            registry?.Unregister(actor); // 사망 전투 객체 Registry 즉시 제외

            if (attackController != null) // 기본 공격 컨트롤러 확인
            {
                attackController.enabled = false; // 사망 전투 객체 기본 공격 중지
            }

            if (enemyBrain != null) // 적군 AI Brain 확인
            {
                enemyBrain.enabled = false; // 사망 적군 AI 판단 중지
            }

            if (actor.Team == BattleTeam.Ally) // 아군 사망 여부 확인
            {
                allyView?.ShowDefeatedPreview(); // 아군 DOWN 표시 적용
            }
            else // 적군 사망 처리
            {
                enemyView?.ShowDefeatedPreview(); // 적군 DOWN 표시 적용
            }

            if (Application.isPlaying) // Play Mode 실행 여부 확인
            {
                StartCoroutine(HideAfterDelay()); // 짧은 DOWN 표시 후 전장 객체 숨김
            }
            else // EditMode 테스트 처리
            {
                gameObject.SetActive(false); // EditMode 사망 객체 즉시 숨김
            }

            return true; // 신규 사망 처리 완료 반환
        }

        private void BindHealthEvent() // 현재 전투 스탯 체력 이벤트 연결
        {
            if (allyStats != null) // 아군 전투 스탯 확인
            {
                allyStats.HealthChanged += HandleHealthChanged; // 아군 체력 변경 이벤트 연결
            }

            if (enemyStats != null) // 적군 전투 스탯 확인
            {
                enemyStats.HealthChanged += HandleHealthChanged; // 적군 체력 변경 이벤트 연결
            }
        }

        private void HandleHealthChanged() // 체력 변경 이벤트 처리
        {
            EvaluateDeathState(); // 체력 0 사망 여부 재확인
        }

        private IEnumerator HideAfterDelay() // 전장 사망 객체 화면 제외 지연
        {
            yield return new WaitForSecondsRealtime(hideDelaySeconds); // DOWN 표시 시간 대기

            if (gameObject != null) // 사망 GameObject 존재 확인
            {
                gameObject.SetActive(false); // 전장 사망 객체 화면 제외
            }
        }

        private void OnDisable() // 전장 객체 비활성화 처리
        {
            if (defeated) // 사망 비활성화 여부 확인
            {
                UnbindHealthEvent(); // 체력 변경 이벤트 연결 해제
            }
        }

        private void OnDestroy() // 사망 처리기 제거 처리
        {
            UnbindHealthEvent(); // 체력 변경 이벤트 연결 해제
        }

        private void UnbindHealthEvent() // 체력 변경 이벤트 안전 해제
        {
            if (allyStats != null) // 기존 아군 전투 스탯 확인
            {
                allyStats.HealthChanged -= HandleHealthChanged; // 기존 아군 체력 이벤트 해제
            }

            if (enemyStats != null) // 기존 적군 전투 스탯 확인
            {
                enemyStats.HealthChanged -= HandleHealthChanged; // 기존 적군 체력 이벤트 해제
            }
        }
    }
}
