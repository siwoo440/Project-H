using System.Collections.Generic; // 목록 자료형
using UnityEngine; // Unity 거리 계산 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleTargetSelector // 횡스크롤 전방 타겟 선택 기능
    {
        public static BattleActor SelectNearest(BattleActor source, IReadOnlyList<BattleActor> candidates) // 가장 가까운 생존 전방 상대 선택
        {
            if (source == null || !source.IsCombatReady || candidates == null) // 타겟 선택 입력 확인
            {
                return null; // 타겟 선택 실패 반환
            }

            BattleActor nearestAhead = null; // 가장 가까운 전방 상대 초기화
            float nearestAheadDistance = float.PositiveInfinity; // 전방 상대 최소 거리 초기화
            BattleActor nearestFallback = null; // 비정상 배치 대비 가장 가까운 상대 초기화
            float nearestFallbackDistance = float.PositiveInfinity; // 전체 상대 최소 거리 초기화

            for (int index = 0; index < candidates.Count; index++) // 후보 전투 객체 순회
            {
                BattleActor candidate = candidates[index]; // 현재 후보 조회

                if (candidate == null || !candidate.IsCombatReady || !candidate.Stats.IsAlive) // 후보 생존 및 초기화 확인
                {
                    continue; // 잘못된 후보 제외
                }

                if (candidate.Team == source.Team) // 같은 팀 여부 확인
                {
                    continue; // 같은 팀 후보 제외
                }

                float horizontalDistance = source.HorizontalDistanceTo(candidate); // 후보 가로 거리 계산

                if (horizontalDistance < nearestFallbackDistance) // 전체 상대 최소 거리 확인
                {
                    nearestFallbackDistance = horizontalDistance; // 전체 상대 최소 거리 갱신
                    nearestFallback = candidate; // 전체 상대 최단 후보 저장
                }

                float forwardDistance = source.ForwardDistanceTo(candidate); // 후보 전진 방향 거리 계산

                if (forwardDistance < 0f || forwardDistance >= nearestAheadDistance) // 뒤쪽 또는 더 먼 전방 상대 확인
                {
                    continue; // 전방 우선 타겟 제외
                }

                nearestAheadDistance = forwardDistance; // 전방 상대 최소 거리 갱신
                nearestAhead = candidate; // 가장 가까운 전방 상대 저장
            }

            return nearestAhead != null ? nearestAhead : nearestFallback; // 전방 우선 타겟 또는 안전 대체 타겟 반환
        }
    }
}
