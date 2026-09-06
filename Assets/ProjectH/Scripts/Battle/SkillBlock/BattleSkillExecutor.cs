using System; // 이벤트 기능
using UnityEngine; // Unity 컴포넌트 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    [DisallowMultipleComponent] // 중복 스킬 실행기 방지
    public sealed class BattleSkillExecutor : MonoBehaviour // 17일차 스킬 사용 요청 전달 실행기
    {
        [SerializeField] private BattleCombatRegistry registry; // 전투 객체 레지스트리
        public event Action<BattleSkillRequest> SkillRequested; // 실제 스킬 효과 연결용 요청 이벤트

        public void Configure(BattleCombatRegistry combatRegistry) // 스킬 실행기 참조 설정
        {
            registry = combatRegistry; // 전투 레지스트리 연결
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

            owner.ShowAction(BattleActionKind.Skill); // 기존 스킬 행동 디버그 표시
            SkillRequested?.Invoke(request); // 18~19일차 실제 효과 연결 이벤트 발생
            Debug.Log($"[Project H][SKILL] Character={request.CharacterId}, Skill={request.SkillId}, Slot={request.SkillSlot}, Enhancement={request.EnhancementLevel}, Blocks={request.BlockCount}"); // 스킬 종류와 강화도 사용 로그
            return true; // 스킬 사용 요청 성공 반환
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
