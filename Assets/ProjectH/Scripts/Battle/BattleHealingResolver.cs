using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BattleHealingResult // 회복 계산 결과
    {
        public string TargetRuntimeId { get; } // 회복 대상 런타임 ID
        public int RequestedAmount { get; } // 요청 회복량
        public int Healing { get; } // 실제 가능 회복량

        public BattleHealingResult(string targetRuntimeId, int requestedAmount, int healing) // 회복 결과 생성
        {
            TargetRuntimeId = targetRuntimeId ?? string.Empty; // 회복 대상 런타임 ID 저장
            RequestedAmount = Mathf.Max(0, requestedAmount); // 요청 회복량 음수 방지
            Healing = Mathf.Max(0, healing); // 실제 회복량 음수 방지
        }
    }

    public static class BattleHealingResolver // 공통 회복 계산 기능
    {
        public static BattleHealingResult Resolve(IBattleCombatantStats target, int amount) // 일반 회복량 계산
        {
            int safeAmount = Mathf.Max(0, amount); // 회복 요청량 음수 방지

            if (target == null) // 회복 대상 확인
            {
                return new BattleHealingResult(string.Empty, safeAmount, 0); // 대상 없음 회복 0 반환
            }

            if (!target.IsAlive) // 전투 불능 대상 확인
            {
                return new BattleHealingResult(target.RuntimeId, safeAmount, 0); // 일반 회복 부활 차단
            }

            int missingHp = Mathf.Max(0, target.MaxHp - target.CurrentHp); // 손실 체력 계산
            int healing = Mathf.Min(safeAmount, missingHp); // 최대 체력 범위 회복량 계산
            return new BattleHealingResult(target.RuntimeId, safeAmount, healing); // 회복 결과 반환
        }
    }
}
