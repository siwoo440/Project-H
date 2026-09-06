using System; // 이벤트 기능
using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    [DisallowMultipleComponent] // 중복 스킬 실행기 방지
    public sealed class BattleSkillExecutor : MonoBehaviour // 데이터 기반 실제 스킬 실행기
    {
        [SerializeField] private BattleCombatRegistry registry; // 전투 객체 레지스트리
        public event Action<BattleSkillRequest> SkillRequested; // 스킬 사용 완료 요청 이벤트

        public void Configure(BattleCombatRegistry combatRegistry) // 스킬 실행기 참조 설정
        {
            registry = combatRegistry; // 전투 레지스트리 연결
            BattleSkillRuntimeState.SetRegistry(registry); // 스킬 Runtime 상태에 현재 Registry 연결
            BattleSkillRuntimeState.ResetAll(); // 신규 전투 스킬 Runtime 상태 초기화

            if (registry != null && registry.GetComponent<BattleSkillRuntimeDriver>() == null) // 주기 효과 Runtime Driver 존재 확인
            {
                registry.gameObject.AddComponent<BattleSkillRuntimeDriver>(); // 전투 Registry 객체에 Runtime Driver 자동 추가
            }
        }

        public bool TryExecute(BattleSkillRequest request) // 블록 소비 스킬 사용 요청 실행
        {
            if (!request.IsValid || registry == null) // 스킬 요청 및 레지스트리 확인
            {
                return false; // 스킬 사용 요청 실패 반환
            }

            BattleActor owner = FindLivingOwner(request.CharacterId); // 살아있는 스킬 소유자 조회

            if (owner == null) // 스킬 소유자 생존 확인
            {
                return false; // 사망 또는 미등록 캐릭터 스킬 사용 차단
            }

            if (BattleSkillRuntimeState.IsStunned(owner.Stats.RuntimeId)) // 스킬 사용자 기절 상태 확인
            {
                return false; // 기절 중 스킬 사용 차단
            }

            ShowSkillAction(owner, request.Skill.DisplayName); // 사용하는 캐릭터 머리 위에 실제 스킬명 표시
            BattleSkillEffectExecutionResult effectResult = BattleSkillEffectExecutor.Execute(request, owner, registry); // SkillData 강화도별 실제 효과 실행
            BattlePassiveSystem.Handle(BattlePassiveEventContext.CreateSkillUsed(owner, request)); // 스킬 사용 완료 패시브 Trigger 처리
            SkillRequested?.Invoke(request); // 이후 궁극기 게이지 연결 이벤트 발생
            Debug.Log($"[Project H][SKILL] Character={request.CharacterId}, Skill={request.SkillId}, Slot={request.SkillSlot}, Enhancement={request.EnhancementLevel}, Blocks={request.BlockCount}, Effects={effectResult.AppliedEffectCount}, Targets={effectResult.AffectedTargetCount}"); // 스킬 사용 및 효과 적용 로그
            return true; // 스킬 사용 요청 성공 반환
        }

        private static void ShowSkillAction(BattleActor owner, string skillDisplayName) // 실제 스킬명 행동 텍스트 표시
        {
            if (owner == null) // 스킬 사용자 확인
            {
                return; // 스킬명 표시 중단
            }

            BattleActionDebugText actionText = owner.GetComponentInChildren<BattleActionDebugText>(true); // 사용자 하위 행동 텍스트 컴포넌트 조회

            if (actionText != null) // 행동 텍스트 컴포넌트 존재 확인
            {
                actionText.Show(BattleActionKind.Skill, skillDisplayName); // 실제 SkillData 표시 이름으로 행동 텍스트 표시
                return; // 실제 스킬명 표시 완료
            }

            owner.ShowAction(BattleActionKind.Skill); // 행동 텍스트 미탐색 시 기존 스킬 라벨 대체 표시
        }

        private BattleActor FindLivingOwner(string characterId) // 캐릭터 ID 기반 살아있는 아군 조회
        {
            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 액터 조회

                if (actor == null || actor.Team != BattleTeam.Ally || !actor.IsCombatReady || !actor.Stats.IsAlive) // 살아있는 아군 여부 확인
                {
                    continue; // 스킬 소유자 후보 제외
                }

                BattleStats stats = actor.Stats as BattleStats; // 아군 캐릭터 전투 스탯 변환

                if (stats != null && stats.CharacterId == characterId) // 요청 캐릭터 ID 일치 확인
                {
                    return actor; // 살아있는 스킬 소유자 반환
                }
            }

            return null; // 살아있는 스킬 소유자 없음 반환
        }
    }
}
