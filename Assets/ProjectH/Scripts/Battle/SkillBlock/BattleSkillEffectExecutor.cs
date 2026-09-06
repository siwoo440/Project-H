using System.Collections.Generic; // 스킬 실행 중 타겟 캐시 기능
using ProjectH.Data; // 스킬 효과 데이터 기능
using UnityEngine; // Unity 수학 기능

namespace ProjectH.Battle.SkillBlock // 스킬 블록 전투 영역
{
    public readonly struct BattleSkillEffectExecutionResult // 스킬 효과 실행 결과
    {
        public int AppliedEffectCount { get; } // 실제 적용된 효과 개수
        public int AffectedTargetCount { get; } // 실제 영향받은 대상 누적 수

        public BattleSkillEffectExecutionResult(int appliedEffectCount, int affectedTargetCount) // 스킬 효과 실행 결과 생성
        {
            AppliedEffectCount = Mathf.Max(0, appliedEffectCount); // 적용 효과 개수 음수 방지
            AffectedTargetCount = Mathf.Max(0, affectedTargetCount); // 영향 대상 개수 음수 방지
        }
    }

    public static class BattleSkillEffectExecutor // SkillData 기반 공통 스킬 효과 실행 기능
    {
        public static BattleSkillEffectExecutionResult Execute(BattleSkillRequest request, BattleActor owner, BattleCombatRegistry registry) // 스킬 요청 실제 효과 실행
        {
            if (!request.IsValid || request.Skill == null || owner == null || registry == null) // 실행 요청 및 참조 유효성 확인
            {
                return new BattleSkillEffectExecutionResult(0, 0); // 빈 실행 결과 반환
            }

            SkillEnhancementData enhancement = request.Skill.GetEnhancement(request.EnhancementLevel); // 요청 강화도 데이터 조회

            if (enhancement == null || enhancement.Effects == null || enhancement.Effects.Length == 0) // 실제 효과 목록 존재 확인
            {
                return new BattleSkillEffectExecutionResult(0, 0); // 미구현 스킬 빈 실행 결과 반환
            }

            int appliedEffects = 0; // 적용 효과 개수 초기화
            int affectedTargets = 0; // 영향 대상 누적 수 초기화
            int previousSuccessCount = 0; // 앞선 효과 성공 대상 수 초기화
            Dictionary<SkillTargetType, IReadOnlyList<BattleActor>> targetCache = new Dictionary<SkillTargetType, IReadOnlyList<BattleActor>>(); // 같은 TargetType 효과가 동일 대상을 유지하도록 타겟 캐시 생성
            BattleSkillTargetContext targetContext = default; // 주 대상 주변 효과용 실행 Context 초기화

            for (int effectIndex = 0; effectIndex < enhancement.Effects.Length; effectIndex++) // 강화도별 효과 목록 순회
            {
                SkillEffectDefinition effect = enhancement.Effects[effectIndex]; // 현재 효과 데이터 조회

                if (effect == null || effect.Kind == SkillEffectKind.None) // 효과 데이터 유효성 확인
                {
                    continue; // 빈 효과 제외
                }

                if (effect.RequirePreviousSuccess && previousSuccessCount <= 0) // 앞선 효과 성공 조건 확인
                {
                    continue; // 앞선 효과 실패 시 조건부 효과 제외
                }

                IReadOnlyList<BattleActor> targets = SelectTargets(owner, registry, effect, targetCache, targetContext); // 현재 효과 대상 목록 선택

                if (!targetContext.HasPrimary && effect.TargetType == SkillTargetType.NearestEnemy && targets.Count > 0) // 최초 단일 적 주 대상 선택 확인
                {
                    targetContext = BattleSkillTargetContext.FromPrimary(targets[0]); // 피해 전 주 대상 위치 Context 저장
                }

                int currentSuccessCount = 0; // 현재 효과 성공 대상 수 초기화

                for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++) // 현재 효과 대상 순회
                {
                    BattleActor target = targets[targetIndex]; // 현재 효과 대상 조회
                    string sourceKey = $"{request.SkillId}:{effectIndex}"; // 같은 스킬 효과 재사용 시 갱신용 출처 키 생성

                    if (ApplySingleEffect(effect, owner, target, sourceKey)) // 단일 대상 효과 적용 성공 확인
                    {
                        currentSuccessCount++; // 현재 효과 성공 대상 수 증가
                    }
                }

                if (currentSuccessCount > 0) // 현재 효과 실제 적용 여부 확인
                {
                    appliedEffects++; // 적용 효과 종류 수 증가
                    affectedTargets += currentSuccessCount; // 영향 대상 누적 수 증가
                }

                previousSuccessCount = currentSuccessCount; // 다음 조건부 효과용 현재 성공 수 저장
            }

            return new BattleSkillEffectExecutionResult(appliedEffects, affectedTargets); // 공통 스킬 효과 실행 결과 반환
        }

        private static IReadOnlyList<BattleActor> SelectTargets(BattleActor owner, BattleCombatRegistry registry, SkillEffectDefinition effect, Dictionary<SkillTargetType, IReadOnlyList<BattleActor>> targetCache, BattleSkillTargetContext context) // 효과별 대상 선택 및 캐시
        {
            if (effect.TargetType == SkillTargetType.NearbyEnemiesFromPrimary) // 주 대상 주변 범위 효과 확인
            {
                return BattleSkillEffectTargetSelector.Select(owner, registry, effect.TargetType, context, effect.Radius, effect.ExcludePrimary); // 저장된 주 대상 위치 기준 주변 적 선택
            }

            if (!targetCache.TryGetValue(effect.TargetType, out IReadOnlyList<BattleActor> targets)) // 같은 대상 종류 기존 선택 결과 확인
            {
                targets = BattleSkillEffectTargetSelector.Select(owner, registry, effect.TargetType); // 현재 효과 대상 최초 선택
                targetCache.Add(effect.TargetType, targets); // 후속 같은 TargetType 효과용 대상 목록 저장
            }

            return targets; // 현재 효과 대상 목록 반환
        }

        private static bool ApplySingleEffect(SkillEffectDefinition effect, BattleActor owner, BattleActor target, string sourceKey) // 단일 대상 스킬 효과 적용
        {
            if (effect == null || owner == null || target == null || !target.IsCombatReady || !target.Stats.IsAlive) // 단일 효과 대상 유효성 확인
            {
                return false; // 단일 효과 적용 실패 반환
            }

            switch (effect.Kind) // 실제 효과 종류 분기
            {
                case SkillEffectKind.HealMaxHpPercent: // 최대 체력 비율 회복 처리
                    return ApplyHealing(effect, target); // 회복 효과 적용 결과 반환
                case SkillEffectKind.CleanseDebuff: // 약화 효과 제거 처리
                    return ApplyCleanse(effect, target); // 정화 효과 적용 결과 반환
                case SkillEffectKind.DefensePercent: // 방어력 증가 처리
                    return ApplyModifier(effect, target, BattleRuntimeModifierKind.DefensePercent, sourceKey); // 방어 증가 Modifier 적용 결과 반환
                case SkillEffectKind.DamageReductionPercent: // 피해 감소 처리
                    return ApplyModifier(effect, target, BattleRuntimeModifierKind.DamageReductionPercent, sourceKey); // 피해 감소 Modifier 적용 결과 반환
                case SkillEffectKind.HealingReceivedPercent: // 받는 회복량 증가 처리
                    return ApplyModifier(effect, target, BattleRuntimeModifierKind.HealingReceivedPercent, sourceKey); // 회복량 증가 Modifier 적용 결과 반환
                case SkillEffectKind.Taunt: // 적군 도발 처리
                    return ApplyTaunt(effect, owner, target, sourceKey); // 적군 도발 적용 결과 반환
                case SkillEffectKind.CounterChance: // 반격 확률 처리
                    return ApplyModifier(effect, target, BattleRuntimeModifierKind.CounterChance, sourceKey); // 반격 확률 Modifier 적용 결과 반환
                case SkillEffectKind.DamageAttackRatio: // 공격력 계수 직접 피해 처리
                    return ApplyDamage(effect, owner, target); // 직접 피해 적용 결과 반환
                case SkillEffectKind.PeriodicDamageAttackRatio: // 공격력 계수 주기 피해 처리
                    return ApplyPeriodicDamage(effect, owner, target, sourceKey); // 주기 피해 등록 결과 반환
                case SkillEffectKind.ResistanceReductionPercent: // 마법 저항 감소 처리
                    return ApplyDebuffModifier(effect, target, BattleRuntimeModifierKind.ResistanceReductionPercent, sourceKey); // 저항 감소 Modifier 적용 결과 반환
                case SkillEffectKind.AccuracyReductionPercent: // 명중률 감소 처리
                    return ApplyDebuffModifier(effect, target, BattleRuntimeModifierKind.AccuracyReductionPercent, sourceKey); // 명중 감소 Modifier 적용 결과 반환
                case SkillEffectKind.Stun: // 기절 처리
                    return ApplyStun(effect, target, sourceKey); // 확률 기절 적용 결과 반환
                default: // 미지원 효과 처리
                    return false; // 미지원 효과 적용 실패 반환
            }
        }

        private static bool ApplyHealing(SkillEffectDefinition effect, BattleActor target) // 최대 체력 비율 회복 적용
        {
            float safeRatio = Mathf.Max(0f, effect.Value); // 회복 비율 음수 방지
            int requestedAmount = Mathf.RoundToInt(target.Stats.MaxHp * safeRatio); // 최대 체력 기준 요청 회복량 계산
            BattleHealingResult healingResult = BattleHealingResolver.Resolve(target.Stats, requestedAmount); // 기존 공통 회복 계산 적용
            int applied = target.ApplyHealing(healingResult); // 대상 실제 회복 적용
            return applied > 0; // 실제 회복 성공 여부 반환
        }

        private static bool ApplyCleanse(SkillEffectDefinition effect, BattleActor target) // 제거 가능한 약화 효과 정화
        {
            int removeCount = Mathf.Max(0, effect.Count); // 정화 개수 음수 방지
            int removed = BattleSkillRuntimeState.RemoveDebuffs(target.Stats.RuntimeId, removeCount); // 등록된 제거 가능 Debuff 제거
            return removed > 0; // 실제 정화 성공 여부 반환
        }

        private static bool ApplyModifier(SkillEffectDefinition effect, BattleActor target, BattleRuntimeModifierKind kind, string sourceKey) // 시간 제한 Buff Modifier 공통 적용
        {
            float safeValue = Mathf.Max(0f, effect.Value); // Modifier 수치 음수 방지
            float safeDuration = Mathf.Max(0f, effect.Duration); // Modifier 지속시간 음수 방지

            if (safeValue <= 0f || safeDuration <= 0f) // Modifier 적용값 유효성 확인
            {
                return false; // 잘못된 Modifier 적용 실패 반환
            }

            BattleSkillRuntimeState.AddModifier(target.Stats.RuntimeId, kind, safeValue, safeDuration, sourceKey); // 같은 스킬 재사용 시 갱신되는 Runtime Modifier 추가
            return true; // Modifier 적용 성공 반환
        }

        private static bool ApplyDebuffModifier(SkillEffectDefinition effect, BattleActor target, BattleRuntimeModifierKind kind, string sourceKey) // 제거 가능한 시간 제한 Debuff 적용
        {
            bool applied = ApplyModifier(effect, target, kind, sourceKey); // 공통 시간 제한 Modifier 적용

            if (applied) // Debuff Modifier 적용 성공 확인
            {
                BattleSkillRuntimeState.RegisterRemovableDebuff(target.Stats.RuntimeId, sourceKey); // 정화 가능한 Debuff 출처 등록
            }

            return applied; // Debuff Modifier 적용 결과 반환
        }

        private static bool ApplyTaunt(SkillEffectDefinition effect, BattleActor owner, BattleActor target, string sourceKey) // 적군 도발 적용
        {
            if (effect.Duration <= 0f) // 도발 지속시간 확인
            {
                return false; // 잘못된 도발 적용 실패 반환
            }

            BattleSkillRuntimeState.ApplyTaunt(target.Stats.RuntimeId, owner.Stats.RuntimeId, effect.Duration, sourceKey); // 적군에 스킬 사용자 강제 타겟 적용
            BattleSkillRuntimeState.RegisterRemovableDebuff(target.Stats.RuntimeId, sourceKey); // 정화 가능한 도발 Debuff 등록
            return true; // 도발 적용 성공 반환
        }

        private static bool ApplyDamage(SkillEffectDefinition effect, BattleActor owner, BattleActor target) // 공격력 계수 직접 피해 적용
        {
            float safeRatio = Mathf.Max(0f, effect.Value); // 공격력 계수 음수 방지
            int power = Mathf.RoundToInt(owner.Stats.Attack * safeRatio); // 공격력 기반 1Hit 위력 계산

            if (power <= 0) // 직접 피해 위력 확인
            {
                return false; // 직접 피해 적용 실패 반환
            }

            int hitCount = Mathf.Max(1, effect.Count); // 최소 1Hit 보정
            bool anyApplied = false; // 실제 피해 발생 여부 초기화
            BattleDamageType damageType = ConvertDamageType(effect.DamageType); // 데이터 피해 타입 전투 피해 타입 변환

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++) // 다단히트 횟수 순회
            {
                if (target == null || !target.IsCombatReady || !target.Stats.IsAlive) // 다단히트 중 대상 제거·사망 여부 확인
                {
                    break; // 남은 Hit 중단
                }

                BattleDamageResult result = BattleDamageResolver.Resolve(new BattleDamageRequest(owner.Stats, target.Stats, damageType, power)); // 현재 Hit 피해 계산
                int applied = target.ApplyDamage(result); // 현재 Hit 실제 피해 적용
                anyApplied |= applied > 0; // 실제 피해 발생 여부 누적
            }

            return anyApplied; // 직접 피해 성공 여부 반환
        }

        private static bool ApplyPeriodicDamage(SkillEffectDefinition effect, BattleActor owner, BattleActor target, string sourceKey) // 공격력 계수 주기 피해 등록
        {
            float safeRatio = Mathf.Max(0f, effect.Value); // Tick 공격력 계수 음수 방지
            int power = Mathf.RoundToInt(owner.Stats.Attack * safeRatio); // 공격력 기반 Tick 위력 계산
            int tickCount = Mathf.Max(1, effect.Count); // 최소 1Tick 보정
            float tickInterval = Mathf.Max(0.1f, effect.Interval); // 최소 Tick 간격 보정

            if (power <= 0) // 주기 피해 위력 확인
            {
                return false; // 주기 피해 등록 실패 반환
            }

            BattleSkillRuntimeState.AddPeriodicDamage(owner.Stats, target.Stats.RuntimeId, ConvertDamageType(effect.DamageType), power, tickInterval, tickCount, sourceKey); // 데이터 기반 DoT Runtime 등록
            return true; // 주기 피해 등록 성공 반환
        }

        private static bool ApplyStun(SkillEffectDefinition effect, BattleActor target, string sourceKey) // 확률 기절 적용
        {
            float chance = Mathf.Clamp01(effect.Chance); // 기절 확률 범위 보정
            float duration = Mathf.Max(0f, effect.Duration); // 기절 지속시간 음수 방지

            if (chance <= 0f || duration <= 0f || Random.value > chance) // 기절 발동 조건 확인
            {
                return false; // 기절 미발동 반환
            }

            BattleSkillRuntimeState.AddModifier(target.Stats.RuntimeId, BattleRuntimeModifierKind.Stun, 1f, duration, sourceKey); // 기절 Runtime Modifier 등록
            BattleSkillRuntimeState.RegisterRemovableDebuff(target.Stats.RuntimeId, sourceKey); // 정화 가능한 기절 Debuff 등록
            return true; // 기절 적용 성공 반환
        }

        private static BattleDamageType ConvertDamageType(SkillDamageType damageType) // 데이터 피해 종류를 전투 피해 종류로 변환
        {
            switch (damageType) // 데이터 피해 종류 분기
            {
                case SkillDamageType.Magic: // 마법 피해 처리
                    return BattleDamageType.Magic; // 전투 마법 피해 반환
                case SkillDamageType.True: // 방어 무시 피해 처리
                    return BattleDamageType.True; // 전투 방어 무시 피해 반환
                default: // 물리 피해 처리
                    return BattleDamageType.Physical; // 전투 물리 피해 반환
            }
        }
    }
}
