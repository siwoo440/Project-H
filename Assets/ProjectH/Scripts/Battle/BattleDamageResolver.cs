using System; // 예외 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BattleDamageRequest // 피해 계산 요청
    {
        public IBattleCombatantStats Attacker { get; } // 공격자 전투 스탯
        public IBattleCombatantStats Target { get; } // 대상 전투 스탯
        public BattleDamageType Type { get; } // 피해 종류
        public int Power { get; } // 공격 원본 위력

        public BattleDamageRequest(IBattleCombatantStats attacker, IBattleCombatantStats target, BattleDamageType type, int power) // 피해 요청 생성
        {
            Attacker = attacker; // 공격자 저장
            Target = target; // 대상 저장
            Type = type; // 피해 종류 저장
            Power = power; // 공격 원본 위력 저장
        }
    }

    public readonly struct BattleDamageResult // 피해 계산 결과
    {
        public BattleDamageType Type { get; } // 피해 종류
        public string AttackerRuntimeId { get; } // 공격자 런타임 ID
        public string TargetRuntimeId { get; } // 대상 런타임 ID
        public int RawPower { get; } // 방어 적용 전 위력
        public int Mitigation { get; } // 적용 방어 수치
        public int Damage { get; } // 최종 피해량

        public BattleDamageResult(BattleDamageType type, string attackerRuntimeId, string targetRuntimeId, int rawPower, int mitigation, int damage) // 피해 결과 생성
        {
            Type = type; // 피해 종류 저장
            AttackerRuntimeId = attackerRuntimeId ?? string.Empty; // 공격자 런타임 ID 저장
            TargetRuntimeId = targetRuntimeId ?? string.Empty; // 대상 런타임 ID 저장
            RawPower = Mathf.Max(0, rawPower); // 원본 위력 음수 방지
            Mitigation = Mathf.Max(0, mitigation); // 방어 수치 음수 방지
            Damage = Mathf.Max(0, damage); // 최종 피해 음수 방지
        }
    }

    public static class BattleDamageResolver // 공통 피해 계산 기능
    {
        public static BattleDamageResult ResolveBasicAttack(IBattleCombatantStats attacker, IBattleCombatantStats target) // 기본 공격 피해 계산
        {
            if (attacker == null) // 공격자 확인
            {
                throw new ArgumentNullException(nameof(attacker)); // 공격자 누락 예외 발생
            }

            return Resolve(new BattleDamageRequest(attacker, target, BattleDamageType.Physical, attacker.Attack)); // 물리 기본 공격 피해 계산 반환
        }

        public static BattleDamageResult Resolve(BattleDamageRequest request) // 피해 요청 계산
        {
            if (request.Attacker == null) // 공격자 확인
            {
                throw new ArgumentNullException(nameof(request.Attacker)); // 공격자 누락 예외 발생
            }

            if (request.Target == null) // 대상 확인
            {
                throw new ArgumentNullException(nameof(request.Target)); // 대상 누락 예외 발생
            }

            int rawPower = Mathf.Max(0, request.Power); // 공격 원본 위력 보정

            if (!request.Target.IsAlive) // 대상 생존 상태 확인
            {
                return new BattleDamageResult(request.Type, request.Attacker.RuntimeId, request.Target.RuntimeId, rawPower, 0, 0); // 전투 불능 대상 피해 0 반환
            }

            int mitigation = GetMitigation(request.Target, request.Type); // 피해 종류별 방어 수치 계산
            int damage = request.Type == BattleDamageType.True ? rawPower : Mathf.Max(1, rawPower - mitigation); // 최종 피해량 계산
            return new BattleDamageResult(request.Type, request.Attacker.RuntimeId, request.Target.RuntimeId, rawPower, mitigation, damage); // 피해 결과 반환
        }

        private static int GetMitigation(IBattleCombatantStats target, BattleDamageType type) // 피해 종류별 방어 수치 반환
        {
            switch (type) // 피해 종류 분기
            {
                case BattleDamageType.Magic: // 마법 피해 처리
                    return target is IBattleResistanceStats resistanceStats ? Mathf.Max(0, resistanceStats.Resistance) : Mathf.Max(0, target.Defense); // 저항력 또는 방어력 대체 반환
                case BattleDamageType.True: // 방어 무시 피해 처리
                    return 0; // 방어 수치 미적용
                default: // 물리 피해 처리
                    return Mathf.Max(0, target.Defense); // 물리 방어력 반환
            }
        }
    }
}
