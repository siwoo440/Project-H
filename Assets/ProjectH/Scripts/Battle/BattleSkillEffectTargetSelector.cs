using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 스킬 대상 종류 기능
using UnityEngine; // Unity 위치 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BattleSkillTargetContext // 한 번의 스킬 실행에서 유지되는 주 대상 위치 Context
    {
        public bool HasPrimary { get; } // 주 대상 존재 여부
        public string PrimaryRuntimeId { get; } // 주 대상 Runtime ID
        public Vector3 PrimaryPosition { get; } // 주 대상 선택 시점 위치

        public BattleSkillTargetContext(bool hasPrimary, string primaryRuntimeId, Vector3 primaryPosition) // 스킬 주 대상 Context 생성
        {
            HasPrimary = hasPrimary; // 주 대상 존재 여부 저장
            PrimaryRuntimeId = primaryRuntimeId ?? string.Empty; // 주 대상 Runtime ID 저장
            PrimaryPosition = primaryPosition; // 주 대상 위치 저장
        }

        public static BattleSkillTargetContext FromPrimary(BattleActor primary) // BattleActor 기반 주 대상 Context 생성
        {
            if (primary == null || !primary.IsCombatReady) // 주 대상 유효성 확인
            {
                return default; // 빈 주 대상 Context 반환
            }

            return new BattleSkillTargetContext(true, primary.Stats.RuntimeId, primary.transform.position); // 주 대상 ID와 선택 시점 위치 Context 반환
        }
    }

    public static class BattleSkillEffectTargetSelector // 데이터 기반 스킬 효과 타겟 선택 기능
    {
        public static IReadOnlyList<BattleActor> Select(BattleActor owner, BattleCombatRegistry registry, SkillTargetType targetType) // 기존 공통 대상 선택 호환 함수
        {
            return Select(owner, registry, targetType, default, 0f, false); // 확장 대상 선택 함수 호출
        }

        public static IReadOnlyList<BattleActor> Select(BattleActor owner, BattleCombatRegistry registry, SkillTargetType targetType, BattleSkillTargetContext context, float radius, bool excludePrimary) // 19일차 범위 Context 포함 대상 선택
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
                    AddNearestEnemy(owner, registry, result); // 가장 가까운 생존 적 추가
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
                case SkillTargetType.LineEnemies: // 직선 관통 적 대상 처리
                    AddLineEnemies(owner, registry, result); // 전방 적 전체 거리순 추가
                    break; // 직선 적 대상 처리 종료
                case SkillTargetType.NearbyEnemiesFromPrimary: // 주 대상 주변 적 처리
                    AddNearbyEnemies(owner, registry, context, radius, excludePrimary, result); // 주 대상 위치 기준 주변 적 추가
                    break; // 주변 적 대상 처리 종료
            }

            return result; // 스킬 효과 대상 목록 반환
        }

        private static void AddNearestEnemy(BattleActor owner, BattleCombatRegistry registry, List<BattleActor> result) // 가장 가까운 적 추가
        {
            BattleActor nearest = registry.FindNearestOpponent(owner); // 가장 가까운 생존 적 조회

            if (IsLivingActor(nearest) && nearest.Team != owner.Team) // 가장 가까운 적 유효성 확인
            {
                result.Add(nearest); // 가장 가까운 적 대상 추가
            }
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

        private static void AddLineEnemies(BattleActor owner, BattleCombatRegistry registry, List<BattleActor> result) // 횡스크롤 직선 적 전체 선택
        {
            BattleTeam enemyTeam = owner.Team == BattleTeam.Ally ? BattleTeam.Enemy : BattleTeam.Ally; // 상대 팀 계산

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 객체 조회

                if (!IsLivingActor(actor) || actor.Team != enemyTeam || !owner.IsOpponentAhead(actor)) // 생존 전방 적 여부 확인
                {
                    continue; // 직선 전방 대상 제외
                }

                result.Add(actor); // 직선 전방 적 추가
            }

            if (result.Count == 0) // 비정상 배치로 전방 적이 없는 경우 확인
            {
                AddLivingTeam(registry, enemyTeam, result); // 모든 생존 상대를 안전 대체 대상으로 추가
            }

            result.Sort((left, right) => owner.ForwardDistanceTo(left).CompareTo(owner.ForwardDistanceTo(right))); // 전진 방향 거리순 정렬
        }

        private static void AddNearbyEnemies(BattleActor owner, BattleCombatRegistry registry, BattleSkillTargetContext context, float radius, bool excludePrimary, List<BattleActor> result) // 주 대상 위치 기준 주변 적 선택
        {
            if (!context.HasPrimary || radius <= 0f) // 주 대상 Context 및 반경 확인
            {
                return; // 주변 적 선택 중단
            }

            BattleTeam enemyTeam = owner.Team == BattleTeam.Ally ? BattleTeam.Enemy : BattleTeam.Ally; // 상대 팀 계산

            for (int index = 0; index < registry.Actors.Count; index++) // 전체 전투 객체 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 객체 조회

                if (!IsLivingActor(actor) || actor.Team != enemyTeam) // 생존 상대 여부 확인
                {
                    continue; // 주변 적 후보 제외
                }

                if (excludePrimary && actor.Stats.RuntimeId == context.PrimaryRuntimeId) // 주 대상 제외 옵션 확인
                {
                    continue; // 주 대상 주변 피해 제외
                }

                float horizontalDistance = Mathf.Abs(actor.transform.position.x - context.PrimaryPosition.x); // 주 대상 선택 시점 X 기준 거리 계산

                if (horizontalDistance <= radius) // 주변 폭발 반경 포함 여부 확인
                {
                    result.Add(actor); // 주변 적 대상 추가
                }
            }

            result.Sort((left, right) => Mathf.Abs(left.transform.position.x - context.PrimaryPosition.x).CompareTo(Mathf.Abs(right.transform.position.x - context.PrimaryPosition.x))); // 주 대상 위치 거리순 정렬
        }

        private static bool IsLivingActor(BattleActor actor) // 생존 전투 객체 여부 확인
        {
            return actor != null && actor.IsCombatReady && actor.Stats.IsAlive; // 전투 초기화 및 생존 여부 반환
        }
    }
}
