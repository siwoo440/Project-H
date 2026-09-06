using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 기본 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleTargetSelector // 현재 위치 기준 기본 공격 타겟 선택기
    {
        public static BattleActor SelectNearest(BattleActor source, IReadOnlyList<BattleActor> candidates) // 현재 위치 기준 가장 가까운 생존 상대 선택
        {
            if (source == null || !source.IsCombatReady || candidates == null) // 소스 및 후보 목록 확인
            {
                return null; // 타겟 선택 실패
            }

            BattleActor nearest = null; // 현재 위치 기준 가장 가까운 상대
            float nearestDistance = float.PositiveInfinity; // 현재 가장 가까운 가로 거리

            for (int index = 0; index < candidates.Count; index++) // 후보 목록 순회
            {
                BattleActor candidate = candidates[index]; // 현재 후보 조회

                if (!IsValidTarget(source, candidate)) // 공격 가능 후보 확인
                {
                    continue; // 잘못된 후보 제외
                }

                float horizontalDistance = source.HorizontalDistanceTo(candidate); // 현재 위치 기준 후보 가로 거리 계산

                if (horizontalDistance < nearestDistance) // 현재 최근접 거리 비교
                {
                    nearestDistance = horizontalDistance; // 최근접 거리 갱신
                    nearest = candidate; // 최근접 상대 갱신
                }
            }

            return nearest; // 방향과 무관한 현재 위치 기준 최근접 상대 반환
        }

        private static bool IsValidTarget(BattleActor source, BattleActor candidate) // 공격 대상 유효성 확인
        {
            return candidate != null && candidate != source && candidate.IsCombatReady && candidate.Stats.IsAlive && candidate.Team != source.Team; // 생존한 반대 팀 여부 반환
        }
    }
}
