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
        public BattleElement Element { get; } // 공격 속성 (Day52 추가, None이면 시전자 속성 상속)

        public BattleDamageRequest(IBattleCombatantStats attacker, IBattleCombatantStats target, BattleDamageType type, int power) // 피해 요청 생성 (무속성 호환 생성자)
            : this(attacker, target, type, power, BattleElement.None) // 무속성 지정으로 확장 생성자에 위임
        {
        }

        public BattleDamageRequest(IBattleCombatantStats attacker, IBattleCombatantStats target, BattleDamageType type, int power, BattleElement element) // 속성 지정 피해 요청 생성 (Day52 추가)
        {
            Attacker = attacker; // 공격자 저장
            Target = target; // 대상 저장
            Type = type; // 피해 종류 저장
            Power = power; // 공격 원본 위력 저장
            Element = element; // 공격 속성 저장
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
        public BattleElement Element { get; } // 적용된 공격 속성 (Day52 추가)
        public BattleElementAffinity Affinity { get; } // 속성 상성 판정 결과 (Day52 추가)

        public BattleDamageResult(BattleDamageType type, string attackerRuntimeId, string targetRuntimeId, int rawPower, int mitigation, int damage) // 피해 결과 생성 (무속성 호환 생성자)
            : this(type, attackerRuntimeId, targetRuntimeId, rawPower, mitigation, damage, BattleElement.None, BattleElementAffinity.Neutral) // 무속성 상성 없음으로 확장 생성자에 위임
        {
        }

        public BattleDamageResult(BattleDamageType type, string attackerRuntimeId, string targetRuntimeId, int rawPower, int mitigation, int damage, BattleElement element, BattleElementAffinity affinity) // 속성 포함 피해 결과 생성 (Day52 추가)
        {
            Type = type; // 피해 종류 저장
            AttackerRuntimeId = attackerRuntimeId ?? string.Empty; // 공격자 런타임 ID 저장
            TargetRuntimeId = targetRuntimeId ?? string.Empty; // 대상 런타임 ID 저장
            RawPower = Mathf.Max(0, rawPower); // 원본 위력 음수 방지
            Mitigation = Mathf.Max(0, mitigation); // 방어 수치 음수 방지
            Damage = Mathf.Max(0, damage); // 최종 피해 음수 방지
            Element = element; // 적용 공격 속성 저장
            Affinity = affinity; // 속성 상성 판정 저장
        }
    }

    public static class BattleDamageResolver // 공통 피해 계산 기능
    {
        public static BattleDamageResult ResolveBasicAttack(IBattleCombatantStats attacker, IBattleCombatantStats target) // 기본 공격 피해 계산
        {
            return ResolveBasicAttack(attacker, target, 1f); // 기본 1배 위력 기본 공격 계산 반환
        }

        public static BattleDamageResult ResolveBasicAttack(IBattleCombatantStats attacker, IBattleCombatantStats target, float powerMultiplier) // 배율 적용 기본 공격 피해 계산
        {
            if (attacker == null) // 공격자 확인
            {
                throw new ArgumentNullException(nameof(attacker)); // 공격자 누락 예외 발생
            }

            float safeMultiplier = Mathf.Max(0f, powerMultiplier); // 공격 배율 음수 방지
            int power = Mathf.RoundToInt(attacker.Attack * safeMultiplier); // 공격력 기반 최종 원본 위력 계산
            return Resolve(new BattleDamageRequest(attacker, target, BattleDamageType.Physical, power)); // 물리 기본 공격 피해 계산 반환
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

            float attackMultiplier = BattleSkillRuntimeState.GetAttackMultiplier(request.Attacker.RuntimeId); // 공격력 증가 버프 배율 조회 (Day51 추가, 버프가 없으면 1.0이라 기존 계산과 동일)
            int rawPower = Mathf.Max(0, Mathf.RoundToInt(request.Power * attackMultiplier)); // 공격력 버프 반영 원본 위력 보정

            BattleElement attackElement = BattleElementRuntimeState.ResolveAttackElement(request.Element, request.Attacker.RuntimeId); // 공격 속성 결정 (미지정 시 시전자 속성 상속)
            BattleElementAffinity affinity = BattleElementRuntimeState.EvaluateAgainst(attackElement, request.Target.RuntimeId); // 대상 속성 기준 상성 판정

            if (!request.Target.IsAlive) // 대상 생존 상태 확인
            {
                return new BattleDamageResult(request.Type, request.Attacker.RuntimeId, request.Target.RuntimeId, rawPower, 0, 0, attackElement, affinity); // 전투 불능 대상 피해 0 반환
            }

            int mitigation = GetMitigation(request.Target, request.Type); // 피해 종류별 Modifier 반영 방어 수치 계산
            int damage = request.Type == BattleDamageType.True ? rawPower : Mathf.Max(1, rawPower - mitigation); // 방어 적용 기본 피해량 계산

            if (request.Type != BattleDamageType.True && damage > 0) // 일반 피해 및 양수 피해 확인
            {
                float reduction = BattleSkillRuntimeState.GetDamageReduction(request.Target.RuntimeId); // 스킬 기반 최종 피해 감소율 조회
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f - reduction))); // 최종 피해 감소 적용
            }

            if (affinity != BattleElementAffinity.Neutral && damage > 0) // 상성 적용 대상 및 양수 피해 확인
            {
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * BattleElementAffinityTable.GetMultiplier(affinity))); // 최종 피해에 속성 상성 배율 적용 (Day52 추가)
            }

            return new BattleDamageResult(request.Type, request.Attacker.RuntimeId, request.Target.RuntimeId, rawPower, mitigation, damage, attackElement, affinity); // 속성 상성 포함 피해 결과 반환
        }

        private static int GetMitigation(IBattleCombatantStats target, BattleDamageType type) // 피해 종류별 방어 수치 반환
        {
            switch (type) // 피해 종류 분기
            {
                case BattleDamageType.Magic: // 마법 피해 처리
                    return BattleSkillRuntimeState.GetEffectiveResistance(target); // 스킬 저항 감소 반영 마법 저항력 반환
                case BattleDamageType.True: // 방어 무시 피해 처리
                    return 0; // 방어 수치 미적용
                default: // 물리 피해 처리
                    int effectiveDefense = BattleSkillRuntimeState.GetEffectiveDefense(target); // 스킬 방어 증가 반영 물리 방어력 조회
                    float passiveReduction = BattlePassiveRuntimeState.GetDefenseReduction(target.RuntimeId); // 패시브 방어 감소율 조회
                    return Mathf.Max(0, Mathf.RoundToInt(effectiveDefense * (1f - passiveReduction))); // 패시브 방어 감소 반영 최종 방어력 반환
            }
        }
    }
}
