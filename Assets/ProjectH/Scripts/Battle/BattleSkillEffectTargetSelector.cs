using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 스킬 대상 종류 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleSkillEffectTargetSelector // 데이터 기반 스킬 효과 타겟 선택 기능
    {
        public static IReadOnlyList<BattleActor> Select(BattleActor owner, BattleCombatRegistry registry, SkillTargetType targetType) // 스킬 효과 대상 목록 선택
        {
            List<BattleActor> result = new List<BattleActor>(); // 스킬 대상 결과 목록 생성

            if (owner == null || registry == null || !owner.IsCombatReady || !owner.Stats.IsAlive) // 스킬 사용자 및 Registry 확인
            {
                return result; // 빈 대상 목록 반환
            }

            switch (targetType) // 스킬 대상 종류 분기
            {
                case SkillTargetType.Self: // 자기 자신 대상 처리
                    result.Add(owner); // 스킬 사용자 자신 추가
                    break; // 자기 자신 대상 처리 종료
                case SkillTargetType.NearestEnemy: // 가장 가까운 적 대상 처리
                    BattleActor nearest = registry.FindNearestOpponent(owner); // 가장 가까운 생존 적 조회

                    if (IsLivingActor(nearest) && nearest.Team != owner.Team) // 가장 가까운 적 유효성 확인
                    {
                        result.Add(nearest); // 가장 가까운 적 대상 추가
                    }

                    break; // 가장 가까운 적 대상 처리 종료
                case SkillTargetType.LowestHpAlly: // 최저 체력 아군 대상 처리
                    BattleActor lowest = FindLowestHpAlly(owner, registry); // 체력 비율이 가장 낮은 아군 조회

                    if (lowest != null) // 최저 체력 아군 존재 확인
                    {
                        result.Add(lowest); // 최저 체력 아군 대상 추가
                    }

                    break; // 최저 체력 아군 대상 처리 종료
                case SkillTargetType.AllEnemies: // 모든 적 대상 처리
                    AddLivingTeam(registry, owner.Team == BattleTeam.Ally ? BattleTeam.Enemy : BattleTeam.Ally, result); // 상대 팀 생존 객체 전체 추가
                    break; // 모든 적 대상 처리 종료
                case SkillTargetType.AllAllies: // 모든 아군 대상 처리
                    AddLivingTeam(registry, owner.Team, result); // 같은 팀 생존 객체 전체 추가
                    break; // 모든 아군 대상 처리 종료
            }

            return result; // 스킬 효과 대상 목록 반환
        }

        private static BattleActor FindLowestHpAlly(BattleActor owner, BattleCombatRegistry registry) // 체력 비율이 가장 낮은 생존 아군 조회
        {
            BattleActor best = null; // 최저 체력 아군 초기화
            float bestRatio = float.PositiveInfinity; // 최저 체력 비율 초기화

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 객체 조회

                if (!IsLivingActor(actor) || actor.Team != owner.Team) // 생존 같은 팀 여부 확인
                {
                    continue; // 대상 후보 제외
                }

                float ratio = actor.Stats.MaxHp <= 0 ? 1f : (float)actor.Stats.CurrentHp / actor.Stats.MaxHp; // 현재 체력 비율 계산

                if (ratio < bestRatio) // 더 낮은 체력 비율 여부 확인
                {
                    best = actor; // 최저 체력 아군 갱신
                    bestRatio = ratio; // 최저 체력 비율 갱신
                }
            }

            return best; // 최저 체력 아군 반환
        }

        private static void AddLivingTeam(BattleCombatRegistry registry, BattleTeam team, List<BattleActor> result) // 지정 팀 생존 객체 전체 추가
        {
            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 객체 조회

                if (IsLivingActor(actor) && actor.Team == team) // 생존 지정 팀 여부 확인
                {
                    result.Add(actor); // 스킬 효과 대상 추가
                }
            }
        }

        private static bool IsLivingActor(BattleActor actor) // 생존 전투 객체 여부 확인
        {
            return actor != null && actor.IsCombatReady && actor.Stats.IsAlive; // 전투 초기화 및 생존 여부 반환
        }
    }
}
