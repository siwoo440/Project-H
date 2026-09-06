using System.Collections.Generic; // 전투 대상 스냅샷 목록 기능
using UnityEngine; // Unity 수학과 객체 조회 기능

namespace ProjectH.Battle // 프로젝트 전투 영역
{
    public readonly struct BattleUltimateEffectExecutionResult // 궁극기 효과 실행 결과
    {
        public int AppliedEffectCount { get; } // 실제 적용 효과 종류 수
        public int AffectedTargetCount { get; } // 실제 영향 대상 누적 수

        public BattleUltimateEffectExecutionResult(int appliedEffectCount, int affectedTargetCount) // 궁극기 효과 실행 결과 생성
        {
            AppliedEffectCount = Mathf.Max(0, appliedEffectCount); // 적용 효과 수 음수 방지
            AffectedTargetCount = Mathf.Max(0, affectedTargetCount); // 영향 대상 수 음수 방지
        }
    }

    public static class BattleUltimateEffectExecutor // 초기 4인 궁극기 고유 효과 실행 기능
    {
        public const string SerenaId = "CH_SERENA"; // 세레나 캐릭터 ID
        public const string EllenId = "CH_ELLEN"; // 엘렌 캐릭터 ID
        public const string LiliaId = "CH_LILIA"; // 릴리아 캐릭터 ID
        public const string EveId = "CH_EVE"; // 이브 캐릭터 ID
        public const string SerenaUltimateName = "성광의 심판"; // 세레나 궁극기 이름
        public const string EllenUltimateName = "기사의 결의"; // 엘렌 궁극기 이름
        public const string LiliaUltimateName = "별의 낙인"; // 릴리아 궁극기 이름
        public const string EveUltimateName = "정령 폭우"; // 이브 궁극기 이름
        private const float SerenaHealRatio = 0.25f; // 세레나 아군 전체 최대 체력 회복 비율
        private const float SerenaReviveRatio = 0.25f; // 세레나 부활 기본 체력 비율
        private const float EllenPartyDamageReduction = 0.20f; // 엘렌 아군 전체 피해 감소 비율
        private const float EllenSelfDefenseBonus = 0.50f; // 엘렌 자신 방어력 증가 비율
        private const float LiliaDamageRatio = 2.60f; // 릴리아 전체 마법 피해 공격력 계수
        private const float LiliaResistanceReduction = 0.20f; // 릴리아 마법 저항 감소 비율
        private const float LiliaResistanceDuration = 10f; // 릴리아 마법 저항 감소 지속시간
        private const float EveDamageRatio = 2.20f; // 이브 전체 물리 피해 공격력 계수
        private const float EveStunDuration = 1f; // 이브 기절 지속시간
        private const string EllenPartyReductionSource = "ULT:CH_ELLEN:PARTY_DR"; // 엘렌 파티 피해 감소 출처 키
        private const string EllenDefenseSource = "ULT:CH_ELLEN:SELF_DEF"; // 엘렌 자기 방어 증가 출처 키
        private const string LiliaResistanceSource = "ULT:CH_LILIA:RES_DOWN"; // 릴리아 마법 저항 감소 출처 키
        private const string EveStunSource = "ULT:CH_EVE:STUN"; // 이브 기절 출처 키

        public static bool IsSupported(string characterId) // 초기 4인 궁극기 지원 여부 확인
        {
            return characterId == SerenaId || characterId == EllenId || characterId == LiliaId || characterId == EveId; // 초기 4인 ID 일치 여부 반환
        }

        public static string GetUltimateName(string characterId) // 캐릭터 ID 기반 궁극기 이름 조회
        {
            switch (characterId) // 캐릭터 ID 분기
            {
                case SerenaId: // 세레나 ID 처리
                    return SerenaUltimateName; // 세레나 궁극기 이름 반환
                case EllenId: // 엘렌 ID 처리
                    return EllenUltimateName; // 엘렌 궁극기 이름 반환
                case LiliaId: // 릴리아 ID 처리
                    return LiliaUltimateName; // 릴리아 궁극기 이름 반환
                case EveId: // 이브 ID 처리
                    return EveUltimateName; // 이브 궁극기 이름 반환
                default: // 미지원 캐릭터 처리
                    return string.Empty; // 미지원 궁극기 빈 이름 반환
            }
        }

        public static BattleUltimateEffectExecutionResult Execute(string characterId, BattleActor owner, BattleCombatRegistry registry) // 캐릭터 고유 궁극기 효과 실행
        {
            if (owner == null || registry == null || !owner.IsCombatReady || !owner.Stats.IsAlive) // 사용자 및 Registry 실행 가능 상태 확인
            {
                return new BattleUltimateEffectExecutionResult(0, 0); // 잘못된 실행 요청 빈 결과 반환
            }

            switch (characterId) // 캐릭터별 궁극기 효과 분기
            {
                case SerenaId: // 세레나 궁극기 처리
                    return ExecuteSerena(owner, registry); // 세레나 성광의 심판 실행 결과 반환
                case EllenId: // 엘렌 궁극기 처리
                    return ExecuteEllen(owner, registry); // 엘렌 기사의 결의 실행 결과 반환
                case LiliaId: // 릴리아 궁극기 처리
                    return ExecuteLilia(owner, registry); // 릴리아 별의 낙인 실행 결과 반환
                case EveId: // 이브 궁극기 처리
                    return ExecuteEve(owner, registry); // 이브 정령 폭우 실행 결과 반환
                default: // 미지원 궁극기 처리
                    return new BattleUltimateEffectExecutionResult(0, 0); // 미지원 궁극기 빈 결과 반환
            }
        }

        private static BattleUltimateEffectExecutionResult ExecuteSerena(BattleActor owner, BattleCombatRegistry registry) // 세레나 성광의 심판 실행
        {
            int affectedTargets = 0; // 영향 대상 누적 수 초기화
            bool healingApplied = false; // 전체 회복 적용 여부 초기화
            bool cleanseApplied = false; // 전체 정화 적용 여부 초기화

            for (int index = 0; index < registry.Actors.Count; index++) // 현재 생존 Registry 액터 순회
            {
                BattleActor target = registry.Actors[index]; // 현재 효과 대상 조회

                if (!IsLivingTeamActor(target, BattleTeam.Ally)) // 생존 아군 대상 여부 확인
                {
                    continue; // 비대상 액터 제외
                }

                int requestedHealing = Mathf.RoundToInt(target.Stats.MaxHp * SerenaHealRatio); // 최대 체력 25퍼센트 회복량 계산
                BattleHealingResult healingResult = BattleHealingResolver.Resolve(target.Stats, requestedHealing); // 공통 회복 계산 적용
                int healed = target.ApplyHealing(healingResult); // 아군 실제 회복 적용
                int removed = BattleSkillRuntimeState.RemoveDebuffs(target.Stats.RuntimeId, int.MaxValue); // 등록 상태이상 전체 제거

                if (healed > 0) // 실제 회복 발생 여부 확인
                {
                    healingApplied = true; // 전체 회복 효과 적용 기록
                }

                if (removed > 0) // 실제 상태이상 제거 여부 확인
                {
                    cleanseApplied = true; // 전체 정화 효과 적용 기록
                }

                if (healed > 0 || removed > 0) // 현재 아군 영향 여부 확인
                {
                    affectedTargets++; // 영향 대상 수 증가
                }
            }

            bool revived = TryReviveOneAlly(owner); // 같은 전투 Scene 전투불능 아군 1명 부활 시도
            int appliedEffects = (healingApplied ? 1 : 0) + (cleanseApplied ? 1 : 0) + (revived ? 1 : 0); // 실제 적용 효과 종류 수 계산
            affectedTargets += revived ? 1 : 0; // 부활 영향 대상 수 반영
            return new BattleUltimateEffectExecutionResult(appliedEffects, affectedTargets); // 세레나 궁극기 실행 결과 반환
        }

        private static BattleUltimateEffectExecutionResult ExecuteEllen(BattleActor owner, BattleCombatRegistry registry) // 엘렌 기사의 결의 실행
        {
            int partyTargets = 0; // 파티 피해 감소 적용 대상 수 초기화

            for (int index = 0; index < registry.Actors.Count; index++) // 현재 생존 Registry 액터 순회
            {
                BattleActor target = registry.Actors[index]; // 현재 효과 대상 조회

                if (!IsLivingTeamActor(target, BattleTeam.Ally)) // 생존 아군 대상 여부 확인
                {
                    continue; // 비대상 액터 제외
                }

                BattleSkillRuntimeState.AddModifier(target.Stats.RuntimeId, BattleRuntimeModifierKind.DamageReductionPercent, EllenPartyDamageReduction, float.PositiveInfinity, EllenPartyReductionSource); // 아군 전체 전투 종료까지 피해 20퍼센트 감소 적용
                partyTargets++; // 피해 감소 적용 대상 수 증가
            }

            BattleSkillRuntimeState.AddModifier(owner.Stats.RuntimeId, BattleRuntimeModifierKind.DefensePercent, EllenSelfDefenseBonus, float.PositiveInfinity, EllenDefenseSource); // 엘렌 자신 전투 종료까지 방어력 50퍼센트 증가 적용
            int appliedEffects = partyTargets > 0 ? 2 : 1; // 파티 피해 감소와 자기 방어 효과 수 계산
            return new BattleUltimateEffectExecutionResult(appliedEffects, partyTargets + 1); // 엘렌 궁극기 실행 결과 반환
        }

        private static BattleUltimateEffectExecutionResult ExecuteLilia(BattleActor owner, BattleCombatRegistry registry) // 릴리아 별의 낙인 실행
        {
            int damageTargets = 0; // 전체 피해 적용 대상 수 초기화
            int debuffTargets = 0; // 마법 저항 감소 적용 대상 수 초기화
            List<BattleActor> targets = CollectLivingActors(registry, BattleTeam.Enemy); // 사망 Registry 변경 방지용 생존 적군 스냅샷 생성

            for (int index = 0; index < targets.Count; index++) // 궁극기 대상 스냅샷 순회
            {
                BattleActor target = targets[index]; // 현재 효과 대상 조회

                if (ApplyDamage(owner, target, LiliaDamageRatio, BattleDamageType.Magic)) // 공격력 260퍼센트 마법 피해 적용 확인
                {
                    damageTargets++; // 실제 피해 대상 수 증가
                }

                if (target.IsCombatReady && target.Stats.IsAlive) // 피해 후 생존 적군 여부 확인
                {
                    BattleSkillRuntimeState.AddModifier(target.Stats.RuntimeId, BattleRuntimeModifierKind.ResistanceReductionPercent, LiliaResistanceReduction, LiliaResistanceDuration, LiliaResistanceSource); // 마법 저항 20퍼센트 감소 10초 적용
                    BattleSkillRuntimeState.RegisterRemovableDebuff(target.Stats.RuntimeId, LiliaResistanceSource); // 마법 저항 감소 정화 대상 등록
                    debuffTargets++; // 저항 감소 적용 대상 수 증가
                }
            }

            int appliedEffects = (damageTargets > 0 ? 1 : 0) + (debuffTargets > 0 ? 1 : 0); // 릴리아 실제 적용 효과 종류 수 계산
            return new BattleUltimateEffectExecutionResult(appliedEffects, damageTargets + debuffTargets); // 릴리아 궁극기 실행 결과 반환
        }

        private static BattleUltimateEffectExecutionResult ExecuteEve(BattleActor owner, BattleCombatRegistry registry) // 이브 정령 폭우 실행
        {
            int damageTargets = 0; // 전체 피해 적용 대상 수 초기화
            int stunTargets = 0; // 기절 적용 대상 수 초기화
            List<BattleActor> targets = CollectLivingActors(registry, BattleTeam.Enemy); // 사망 Registry 변경 방지용 생존 적군 스냅샷 생성

            for (int index = 0; index < targets.Count; index++) // 궁극기 대상 스냅샷 순회
            {
                BattleActor target = targets[index]; // 현재 효과 대상 조회

                if (ApplyDamage(owner, target, EveDamageRatio, BattleDamageType.Physical)) // 공격력 220퍼센트 물리 피해 적용 확인
                {
                    damageTargets++; // 실제 피해 대상 수 증가
                }

                if (target.IsCombatReady && target.Stats.IsAlive) // 피해 후 생존 적군 여부 확인
                {
                    BattleSkillRuntimeState.AddModifier(target.Stats.RuntimeId, BattleRuntimeModifierKind.Stun, 1f, EveStunDuration, EveStunSource); // 적군 1초 기절 적용
                    BattleSkillRuntimeState.RegisterRemovableDebuff(target.Stats.RuntimeId, EveStunSource); // 기절 정화 대상 등록
                    stunTargets++; // 기절 적용 대상 수 증가
                }
            }

            int appliedEffects = (damageTargets > 0 ? 1 : 0) + (stunTargets > 0 ? 1 : 0); // 이브 실제 적용 효과 종류 수 계산
            return new BattleUltimateEffectExecutionResult(appliedEffects, damageTargets + stunTargets); // 이브 궁극기 실행 결과 반환
        }

        private static bool ApplyDamage(BattleActor owner, BattleActor target, float attackRatio, BattleDamageType damageType) // 궁극기 공격력 계수 피해 공통 적용
        {
            if (owner == null || target == null || !owner.IsCombatReady || !target.IsCombatReady || !target.Stats.IsAlive) // 피해 사용자 및 대상 상태 확인
            {
                return false; // 잘못된 피해 요청 실패 반환
            }

            int power = Mathf.RoundToInt(owner.Stats.Attack * Mathf.Max(0f, attackRatio)); // 공격력 계수 기반 원본 위력 계산

            if (power <= 0) // 궁극기 원본 위력 확인
            {
                return false; // 0 위력 피해 실패 반환
            }

            BattleDamageResult damageResult = BattleDamageResolver.Resolve(new BattleDamageRequest(owner.Stats, target.Stats, damageType, power)); // 공통 피해 계산 실행
            int applied = target.ApplyDamage(damageResult); // 대상 실제 궁극기 피해 적용
            return applied > 0; // 실제 피해 성공 여부 반환
        }

        private static bool TryReviveOneAlly(BattleActor owner) // 세레나 전투불능 아군 1명 부활 시도
        {
            if (owner == null) // 세레나 사용자 확인
            {
                return false; // 사용자 없음 부활 실패 반환
            }

            BattleDeathHandler[] handlers = Resources.FindObjectsOfTypeAll<BattleDeathHandler>(); // 비활성 포함 현재 로드된 사망 처리기 조회
            BattleDeathHandler candidate = null; // 부활 후보 초기화

            for (int index = 0; index < handlers.Length; index++) // 전체 사망 처리기 순회
            {
                BattleDeathHandler handler = handlers[index]; // 현재 사망 처리기 조회

                if (handler == null || !handler.CanRevive || handler.gameObject.scene != owner.gameObject.scene) // 같은 Scene 부활 가능 아군 여부 확인
                {
                    continue; // 부활 불가 대상 제외
                }

                if (candidate == null || string.CompareOrdinal(handler.AllyStats.RuntimeId, candidate.AllyStats.RuntimeId) < 0) // Runtime ID 기준 첫 번째 전투불능 아군 확인
                {
                    candidate = handler; // 부활 후보 갱신
                }
            }

            if (candidate == null || candidate.AllyStats == null) // 최종 부활 후보 확인
            {
                return false; // 부활 대상 없음 반환
            }

            int reviveHp = Mathf.Max(1, Mathf.RoundToInt(candidate.AllyStats.MaxHp * SerenaReviveRatio)); // 최대 체력 25퍼센트 부활 체력 계산
            bool revived = candidate.TryRevive(reviveHp); // 전투불능 아군 1명 부활 실행

            if (revived) // 아군 부활 성공 여부 확인
            {
                BattleSkillRuntimeState.RemoveDebuffs(candidate.AllyStats.RuntimeId, int.MaxValue); // 부활 아군 잔여 상태이상 전체 제거
            }

            return revived; // 전투불능 아군 부활 결과 반환
        }


        private static List<BattleActor> CollectLivingActors(BattleCombatRegistry registry, BattleTeam team) // 생존 팀 액터 스냅샷 생성
        {
            List<BattleActor> targets = new List<BattleActor>(); // 생존 팀 액터 목록 생성

            if (registry == null) // Registry 존재 확인
            {
                return targets; // 빈 대상 목록 반환
            }

            for (int index = 0; index < registry.Actors.Count; index++) // 현재 Registry 액터 순회
            {
                BattleActor actor = registry.Actors[index]; // 현재 전투 액터 조회

                if (IsLivingTeamActor(actor, team)) // 생존 지정 팀 여부 확인
                {
                    targets.Add(actor); // 궁극기 대상 스냅샷 추가
                }
            }

            return targets; // 생존 팀 액터 스냅샷 반환
        }

        private static bool IsLivingTeamActor(BattleActor actor, BattleTeam team) // 생존 팀 액터 여부 확인
        {
            return actor != null && actor.Team == team && actor.IsCombatReady && actor.Stats.IsAlive; // 팀 및 전투 생존 상태 반환
        }
    }
}
