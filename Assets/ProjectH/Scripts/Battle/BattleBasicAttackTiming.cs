using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleBasicAttackTiming // 기본 공격 시간 계산 기능
    {
        public static float GetInterval(float attackSpeed) // 공격속도 기반 공격 주기 계산
        {
            return GetInterval(attackSpeed, 0f); // 둔화 없는 기본 공격 주기 반환
        }

        public static float GetInterval(float attackSpeed, float slowReduction) // 둔화 반영 공격 주기 계산 (Day51 추가)
        {
            float safeReduction = Mathf.Clamp(slowReduction, 0f, 0.95f); // 공격 속도 감소율 범위 보정 (완전 정지 방지)
            float safeAttackSpeed = Mathf.Max(0.01f, attackSpeed * (1f - safeReduction)); // 둔화 반영 최소 공격속도 보정
            return 1f / safeAttackSpeed; // 공격속도 역수 주기 반환
        }
    }
}
