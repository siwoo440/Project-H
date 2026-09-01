using System.Collections.Generic; // 목록 자료형

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleFrontBlockerResolver // 전선 통과 방지 타겟 계산 기능
    {
        public static BattleActor Resolve(BattleActor source, BattleActor desiredTarget, IReadOnlyList<BattleActor> candidates) // 희망 타겟 앞 차단 상대 계산
        {
            if (source == null || desiredTarget == null || candidates == null) // 전선 차단 입력 확인
            {
                return desiredTarget; // 기존 희망 타겟 반환
            }

            float desiredDistance = source.ForwardDistanceTo(desiredTarget); // 희망 타겟 전진 방향 거리 계산

            if (desiredDistance < 0f) // 희망 타겟 뒤쪽 배치 확인
            {
                return BattleTargetSelector.SelectNearest(source, candidates); // 가장 가까운 상대 안전 반환
            }

            BattleActor blocker = desiredTarget; // 기본 실제 타겟을 희망 타겟으로 설정
            float blockerDistance = desiredDistance; // 기본 차단 거리 설정

            for (int index = 0; index < candidates.Count; index++) // 전투 객체 후보 순회
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

                float forwardDistance = source.ForwardDistanceTo(candidate); // 후보 전진 방향 거리 계산

                if (forwardDistance < 0f || forwardDistance > desiredDistance) // 희망 타겟 뒤쪽 또는 공격자 뒤쪽 후보 확인
                {
                    continue; // 차단 후보 제외
                }

                if (forwardDistance >= blockerDistance) // 현재 차단 상대보다 먼 후보 확인
                {
                    continue; // 더 먼 차단 후보 제외
                }

                blockerDistance = forwardDistance; // 가장 가까운 차단 거리 갱신
                blocker = candidate; // 실제 차단 타겟 갱신
            }

            return blocker; // 실제 공격 가능한 전방 타겟 반환
        }
    }
}
