using ProjectH.Data; // 적군 AI 유형 기능
using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    [DisallowMultipleComponent] // 중복 적군 AI Brain 방지
    public sealed class BattleEnemyBrain : MonoBehaviour // 적군 타겟 판단 전용 Brain
    {
        private BattleActor actor; // AI 소유 전투 액터
        private BattleCombatRegistry registry; // 전투 객체 레지스트리
        public EnemyAIType AIType { get; private set; } // 현재 적군 AI 유형
        public BattleActor DesiredTarget { get; private set; } // AI가 노리는 희망 타겟
        public BattleActor CurrentTarget { get; private set; } // 전선 차단 반영 실제 타겟
        public string DebugSummary => $"{AIType} · DESIRE={GetRuntimeId(DesiredTarget)} · ACTUAL={GetRuntimeId(CurrentTarget)}"; // AI 디버그 요약 반환

        public void Configure(BattleActor owner, BattleCombatRegistry combatRegistry, EnemyAIType aiType) // 적군 AI Brain 초기화
        {
            actor = owner; // AI 소유 전투 액터 연결
            registry = combatRegistry; // 전투 객체 레지스트리 연결
            AIType = aiType; // 적군 AI 유형 저장
            DesiredTarget = null; // 희망 타겟 초기화
            CurrentTarget = null; // 실제 타겟 초기화
        }

        public BattleActor SelectTarget() // 현재 AI 정책 기반 실제 공격 타겟 선택
        {
            if (!enabled || actor == null || registry == null || !actor.IsCombatReady || !actor.Stats.IsAlive) // AI 판단 가능 상태 확인
            {
                DesiredTarget = null; // 희망 타겟 초기화
                CurrentTarget = null; // 실제 타겟 초기화
                return null; // AI 타겟 없음 반환
            }

            DesiredTarget = BattleEnemyTargetPolicy.SelectDesiredTarget(actor, registry.Actors, AIType); // AI 유형별 희망 타겟 선택
            CurrentTarget = BattleFrontBlockerResolver.Resolve(actor, DesiredTarget, registry.Actors); // 전선 차단 반영 실제 타겟 선택
            return CurrentTarget; // 실제 공격 타겟 반환
        }

        private static string GetRuntimeId(BattleActor target) // 디버그 타겟 런타임 ID 반환
        {
            return target == null || !target.IsCombatReady ? "NONE" : target.Stats.RuntimeId; // 타겟 런타임 ID 또는 없음 반환
        }
    }
}
