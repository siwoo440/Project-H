using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public static class BattleBasicAttackTiming // 기본 공격 시간 계산 기능
    {
        public static float GetInterval(float attackSpeed) // 공격속도 기반 공격 주기 계산
        {
            float safeAttackSpeed = Mathf.Max(0.01f, attackSpeed); // 최소 공격속도 보정
            return 1f / safeAttackSpeed; // 공격속도 역수 주기 반환
        }
    }
}
