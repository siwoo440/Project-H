using System.Collections.Generic; // 목록 자료형
using ProjectH.Data; // 적군 AI 유형 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleEnemyTargetPolicy // 적군 AI 희망 타겟 정책
    {
        public static BattleActor SelectDesiredTarget(BattleActor source, IReadOnlyList<BattleActor> candidates, EnemyAIType aiType) // AI 유형별 희망 타겟 선택
        {
            if (source == null || !source.IsCombatReady || candidates == null) // 타겟 선택 입력 확인
            {
                return null; // 타겟 선택 실패 반환
            }

            switch (aiType) // 적군 AI 유형 분기
            {
                case EnemyAIType.Rush: // 돌격형 타겟 처리
                    return SelectRearTarget(source, candidates); // 가장 깊은 후열 희망 타겟 반환
                case EnemyAIType.Ranged: // 원거리형 타겟 처리
                    return BattleTargetSelector.SelectNearest(source, candidates); // 가장 가까운 전선 타겟 반환
                default: // 일반 및 미구현 확장 AI 처리
                    return BattleTargetSelector.SelectNearest(source, candidates); // 기본 전선 타겟 반환
            }
        }

        private static BattleActor SelectRearTarget(BattleActor source, IReadOnlyList<BattleActor> candidates) // 전진 방향 가장 깊은 상대 선택
        {
            BattleActor rearTarget = null; // 후열 희망 타겟 초기화
            float deepestForwardDistance = float.NegativeInfinity; // 가장 깊은 전진 거리 초기화
            BattleActor fallback = null; // 비정상 배치 대체 타겟 초기화
            float fallbackDistance = float.PositiveInfinity; // 대체 타겟 거리 초기화

            for (int index = 0; index < candidates.Count; index++) // 전투 객체 후보 순회
            {
                BattleActor candidate = candidates[index]; // 현재 후보 조회

                if (!IsLivingOpponent(source, candidate)) // 생존 상대 여부 확인
                {
                    continue; // 잘못된 후보 제외
                }

                float horizontalDistance = source.HorizontalDistanceTo(candidate); // 후보 가로 거리 계산

                if (horizontalDistance < fallbackDistance) // 가장 가까운 대체 후보 확인
                {
                    fallbackDistance = horizontalDistance; // 대체 후보 거리 갱신
                    fallback = candidate; // 대체 후보 저장
                }

                float forwardDistance = source.ForwardDistanceTo(candidate); // 전진 방향 거리 계산

                if (forwardDistance < 0f || forwardDistance <= deepestForwardDistance) // 뒤쪽 또는 덜 깊은 후보 확인
                {
                    continue; // 후열 희망 후보 제외
                }

                deepestForwardDistance = forwardDistance; // 가장 깊은 전진 거리 갱신
                rearTarget = candidate; // 후열 희망 타겟 저장
            }

            return rearTarget != null ? rearTarget : fallback; // 후열 희망 타겟 또는 안전 대체 타겟 반환
        }

        private static bool IsLivingOpponent(BattleActor source, BattleActor candidate) // 생존 상대 후보 여부 확인
        {
            return candidate != null && candidate.IsCombatReady && candidate.Stats.IsAlive && candidate.Team != source.Team; // 생존 상대 여부 반환
        }
    }
}
