using System.Collections; // 코루틴 기능
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 적군 사망 처리 방지
    public sealed class BattleEnemyDeathHandler : MonoBehaviour // 적군 전투 불능 제외 처리기
    {
        [SerializeField, Min(0f)] private float hideDelaySeconds = 0.35f; // 전투 불능 표시 유지 시간
        private BattleActor actor; // 사망 처리 전투 액터
        private BattleEnemyStats stats; // 적군 전투 스탯
        private BattleCombatRegistry registry; // 전투 객체 레지스트리
        private BattleBasicAttackController attackController; // 적군 기본 공격 컨트롤러
        private BattleEnemyBrain brain; // 적군 AI Brain
        private BattleEnemyView enemyView; // 적군 전투 View
        private bool defeated; // 사망 처리 완료 상태

        public void Configure(BattleActor owner, BattleEnemyStats enemyStats, BattleCombatRegistry combatRegistry, BattleBasicAttackController basicAttackController, BattleEnemyBrain enemyBrain, BattleEnemyView view) // 적군 사망 처리 참조 설정
        {
            Unbind(); // 기존 체력 이벤트 연결 해제
            actor = owner; // 적군 전투 액터 연결
            stats = enemyStats; // 적군 전투 스탯 연결
            registry = combatRegistry; // 전투 레지스트리 연결
            attackController = basicAttackController; // 기본 공격 컨트롤러 연결
            brain = enemyBrain; // 적군 AI Brain 연결
            enemyView = view; // 적군 전투 View 연결
            defeated = false; // 사망 처리 상태 초기화

            if (stats != null) // 적군 전투 스탯 확인
            {
                stats.HealthChanged += HandleHealthChanged; // 체력 변경 이벤트 연결
            }

            EvaluateDeathState(); // 초기 사망 상태 확인
        }

        public bool EvaluateDeathState() // 현재 적군 사망 상태 확인 및 제외
        {
            if (defeated || stats == null || stats.IsAlive) // 미사망 또는 기존 처리 상태 확인
            {
                return false; // 신규 사망 처리 없음 반환
            }

            defeated = true; // 사망 처리 시작 기록
            registry?.Unregister(actor); // 사망 적군 전투 레지스트리 즉시 제외

            if (attackController != null) // 기본 공격 컨트롤러 확인
            {
                attackController.enabled = false; // 사망 적군 기본 공격 중지
            }

            if (brain != null) // 적군 AI Brain 확인
            {
                brain.enabled = false; // 사망 적군 AI 판단 중지
            }

            enemyView?.ShowDefeatedPreview(); // 적군 전투 불능 임시 표시

            if (Application.isPlaying) // 런타임 실행 상태 확인
            {
                StartCoroutine(HideAfterDelay()); // 짧은 사망 표시 후 화면 제외 시작
            }
            else // EditMode 테스트 처리
            {
                gameObject.SetActive(false); // EditMode 사망 적군 즉시 숨김
            }

            return true; // 신규 사망 처리 완료 반환
        }

        private void HandleHealthChanged() // 적군 체력 변경 처리
        {
            EvaluateDeathState(); // 적군 사망 여부 재확인
        }

        private IEnumerator HideAfterDelay() // 적군 전투 불능 화면 제외 지연
        {
            yield return new WaitForSecondsRealtime(hideDelaySeconds); // 전투 불능 표시 시간 대기

            if (gameObject != null) // 적군 GameObject 존재 확인
            {
                gameObject.SetActive(false); // 사망 적군 화면에서 제외
            }
        }

        private void OnDisable() // 적군 객체 비활성화 처리
        {
            if (defeated) // 사망 비활성화 여부 확인
            {
                Unbind(); // 체력 변경 이벤트 연결 해제
            }
        }

        private void OnDestroy() // 적군 객체 제거 처리
        {
            Unbind(); // 체력 변경 이벤트 연결 해제
        }

        private void Unbind() // 적군 체력 이벤트 안전 해제
        {
            if (stats != null) // 기존 적군 스탯 확인
            {
                stats.HealthChanged -= HandleHealthChanged; // 기존 체력 변경 이벤트 해제
            }
        }
    }
}
